---
label: Cancellation
icon: hourglass-end
order: 3
tags: [cancellation, token, extraction]
---

# Cancellation

`Extractor.Extract` and `ExtractAsync` take a `CancellationToken` as an extra last parameter.

```csharp
using XRay.Content.Core;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var extractor = new Extractor();

try
{
    var result = extractor.Extract(
        ExtractInput.FromUri("huge-report.pdf"),
        new ExtractionConfig { OutputFormat = OutputFormat.Markdown },
        timeout.Token);

    Console.WriteLine(result.Results[0].Content.Length);
}
catch (OperationCanceledException)
{
    Console.WriteLine("extraction was cancelled");
}

// The async form runs on the thread pool; a token that is already cancelled yields a cancelled task.
var asyncResult = await extractor.ExtractAsync(ExtractInput.FromUri("report.docx"), new ExtractionConfig(), timeout.Token);
```

## The contract

- A cancelled call throws `OperationCanceledException`. It is never folded into
  `ExtractionResult.Errors` or into a processing warning, so a caller can tell a cancelled
  extraction from a failed one.
- A token that is already cancelled throws before any input is read.
- A call with a token that completes returns exactly what the call without one returns.
- The token is polled by MIME detection, every extractor's page, part, record and entry loops, the
  layout and neural network stages, the OCR pass and the render stages. A cancelled call returns
  within a bounded slice of work and not at the end of the document.
- OCR links the caller's token into each image's own timeout. The timeout abandons one image and
  adds a warning. The caller's cancellation abandons the whole extraction and throws.

## How the token reaches the code

The token is ambient. `Extractor.Extract` installs it in an `AsyncLocal` for the extent of the
call, and anything that runs inside it, on the calling thread, a `Task.Run` or a `Parallel.For`
worker, reads it from there. No extractor signature carries it. A nested `Extract` call that passes
no token, such as an archive extracting a member or a PDF extracting an attachment, keeps the outer
token in force.

For code that calls the lower-level API directly, `CancellableApi` provides token counterparts as
extension and static methods: `IExtractor.Extract(bytes, mimeType, config, token)`,
`DeriveExtractionResult`, `RenderPlain`, `RenderMarkdown`, `RenderHtml`, `RenderDjot`, `RenderJson`,
`ProcessQrCodes`, `DetectQrCodes` and `DetectMimeTypeFromBytes`.

## What cannot be interrupted

One call into a library that has no token runs to its end, and the latency of a cancellation there
is that one call. These are:

- `System.Text.Json` parsing a single large JSON document.
- SkiaSharp decoding one image.
- The `X-Ray.Psd` `Load` and `ExtractText` calls, until that package ships token overloads. The
  Photoshop extractor checks the token between layers and before each stage.
- PDFium rasterising one page, in the OCR pass.

Everything else polls: loops over pages, parts, records, archive entries, spreadsheet rows,
elements, and (every 256 to 65,536 iterations) per-byte loops, plus the PDF operator loops and
filter decoders, the ONNX session, and the derive and render stages.

## Writing code that stays cancellable

If you add a loop or a `catch` to the library, two rules keep cancellation working: poll
`Cancel.ThrowIfRequested()` or `Cancel.Every(i, mask)` in loops over input-controlled counts, and
give every best-effort `catch` the filter `when (Cancel.NotCancelling)`, so a swallowed
exception cannot eat a cancellation. The repository's `CLAUDE.md` has the complete contract and
the tests that enforce it.
