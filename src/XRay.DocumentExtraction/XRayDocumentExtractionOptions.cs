using XRay.Content.Core;

namespace XRay.DocumentExtraction;

/// <summary>Settings of an <see cref="XRayDocumentExtractionClient"/>.</summary>
public sealed class XRayDocumentExtractionOptions
{
    /// <summary>
    /// Adjusts the X-Ray.Content configuration, for example to turn on the opt-in OCR pass or to set
    /// PDF passwords. Run once, when the client is constructed. The client always sets
    /// <c>IncludeDocumentStructure</c>, which the mapping needs.
    /// </summary>
    public Action<ExtractionConfig>? Configure { get; set; }

    /// <summary>
    /// Carry the bytes of embedded images in the result (as <c>DataContent</c> on the image
    /// element). Off by default: it holds every image of the document in memory.
    /// </summary>
    public bool IncludeImageBytes { get; set; }
}
