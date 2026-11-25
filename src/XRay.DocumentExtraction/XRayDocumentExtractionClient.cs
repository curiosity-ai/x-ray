using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using XRay.Content.Core;

namespace XRay.DocumentExtraction;

/// <summary>
/// A <see cref="DocumentExtractionClient"/> backed by X-Ray.Content: reads a document from a
/// stream, in any of the formats X-Ray.Content supports, and returns it as the provider-neutral
/// <c>ExtractionDocument</c> tree of Microsoft.Extensions.DocumentExtraction.
/// </summary>
/// <remarks>
/// The default code path is pure managed and makes no network call. Scanned pages are recognised
/// only when the caller opts in through <see cref="XRayDocumentExtractionOptions.Configure"/>
/// (X-Ray.Content's OCR is off by default and pulls in native libraries).
/// </remarks>
public sealed class XRayDocumentExtractionClient : DocumentExtractionClient
{
    private readonly Extractor _extractor;
    private readonly ExtractionConfig _config;
    private readonly bool _includeImageBytes;

    /// <summary>Creates a client with X-Ray.Content's defaults.</summary>
    public XRayDocumentExtractionClient() : this(null) { }

    /// <summary>Creates a client.</summary>
    /// <param name="options">Provider settings, or <see langword="null"/> for the defaults. They are
    /// copied at construction, so changing the object afterwards has no effect on this client.</param>
    public XRayDocumentExtractionClient(XRayDocumentExtractionOptions? options)
    {
        var config = new ExtractionConfig();
        options?.Configure?.Invoke(config);

        // The mapping reads the document structure tree, and, to carry an image's bytes, the images.
        config.IncludeDocumentStructure = true;
        _includeImageBytes = options?.IncludeImageBytes ?? false;
        if (_includeImageBytes)
        {
            config.ExtractImages = true;
        }

        _config = config;
        _extractor = new Extractor();
    }

    /// <inheritdoc />
    public override async Task<ExtractionDocument> ExtractAsync(
        Stream source,
        string identifier,
        string mediaType,
        DocumentExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

        // Per-call options are snapshotted before the first await, as the base class requires.
        if (options?.ExtractorName is { } name)
        {
            throw new NotSupportedException(
                $"The X-Ray provider has a single extractor and does not accept ExtractorName '{name}'.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        var input = ExtractInput.FromBytes(buffer.ToArray(), NormalizeMediaType(mediaType), identifier);
        var result = await _extractor.ExtractAsync(input, _config, cancellationToken).ConfigureAwait(false);

        if (result.Results.Count == 0)
        {
            var message = result.Errors.Count > 0
                ? result.Errors[0].Message
                : "X-Ray.Content returned no result.";
            throw new InvalidDataException($"Could not extract '{identifier}': {message}");
        }

        var extracted = result.Results[0];

        // X-Ray.Content answers a format it has no extractor for with an empty document and a
        // "registry" warning. A caller of the abstraction should be told, not handed nothing.
        if (extracted.ProcessingWarnings.Any(w => w.Source == "registry"))
        {
            throw new NotSupportedException(
                $"X-Ray.Content cannot extract '{identifier}': no extractor handles media type '{extracted.MimeType}'.");
        }

        return ExtractionDocumentBuilder.Build(identifier, extracted, _includeImageBytes);
    }

    // "text/plain; charset=utf-8" reaches X-Ray.Content as "text/plain".
    private static string NormalizeMediaType(string mediaType)
    {
        var semicolon = mediaType.IndexOf(';');
        return (semicolon < 0 ? mediaType : mediaType[..semicolon]).Trim();
    }
}
