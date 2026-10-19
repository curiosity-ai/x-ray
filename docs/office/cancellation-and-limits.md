---
label: Cancellation and limits
icon: hourglass-end
order: 2
tags: [office, cancellation, limits, timeout]
---

# Cancellation and limits

## Cancellation

Every call that does real work has an overload taking a `CancellationToken`, and throws
`OperationCanceledException` when it fires:

- `OfficeDocument.Open`, `OpenAsync`, `Extract` and `ExtractAsync`
- `IPaginatedDocument.Layout`
- `PdfRenderer.Render` and `RenderDocument`
- `RasterRenderer.RenderToPng`, `Render`, `RenderPages` and `Rasterise`
- `SheetHtmlWriter.Write`
- `MarkdownWriter.ToMarkdown` and `XhtmlWriter.ToXhtml`
- `IDocumentReader.Read` for the readers used directly

The token reaches the loops that bound latency through a per-thread operation scope, so reader,
layout and renderer signatures stay as they were. Zip parts are wrapped in a stream that polls the
token; parsers, the word paginator and table layouter, the line breaker, every shaper call, sheet
pagination, slide shapes, the metafile readers and both drawing sinks check it. The
[family convention](/getting-started/cancellation-and-streaming) is the same for every package.

## Limits on a layout

`LayoutOptions` bounds what a hostile or pathological document can cost:

| Option | Default | Effect |
|---|---|---|
| `MaxPages` | 0 (unlimited) | Stop paginating at this many pages. |
| `Timeout` | `null` | A wall-clock limit on layout. Rendering the pages is bounded by its own token. |
| `MaxImagePixels` | 0 (unlimited) | The largest embedded picture, in pixels, that is decoded when the pages are rendered. |
| `OversizedImages` | `Skip` | What happens to a picture over the limit: `Skip` leaves it out; `Downscale` decodes JPEG at 1/2, 1/4 or 1/8 scale and leaves out pictures whose codec cannot do that cheaply. |

A header is read to learn a picture's size before any pixel is, so a poster-sized scan costs a
header read and not a gigabyte. The limit travels with the laid-out pages, so the PDF and raster
renderers apply it without being told. A renderer's own `MaxImagePixels` option applies as well, and
the stricter limit wins.

```csharp
using XRay.Office;
using XRay.Office.Core.Documents;
using XRay.Office.Core.Threading;
using XRay.Office.Rendering.Pdf;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    using IDocument doc = OfficeDocument.Open("report.docx", cts.Token);
    IPageSequence pages = ((IPaginatedDocument)doc).Layout(
        new LayoutOptions
        {
            MaxPages = 200,                                  // stop paginating here
            Timeout = TimeSpan.FromSeconds(10),              // wall clock, layout only
            MaxImagePixels = 25_000_000,                     // skip pictures larger than this, unread
            OversizedImages = OversizedImagePolicy.Skip,     // or Downscale
        },
        cts.Token);

    // Written straight to the stream as it is produced; a temp file is the natural destination.
    using FileStream pdf = File.Create("report.pdf");
    new PdfRenderer().Render(doc, pages, pdf, cts.Token);
}
catch (LayoutTimeoutException timeout)
{
    Console.WriteLine($"layout took longer than {timeout.Timeout}");   // the document is slow
}
catch (OperationCanceledException)
{
    Console.WriteLine("the caller cancelled");
}
```

A layout that outran `Timeout` throws `LayoutTimeoutException`, which derives from
`OperationCanceledException`. One `catch` covers both, and a `catch` placed before it tells them
apart, so a caller can distinguish "I cancelled" from "the document took too long".

## Streamed output

- **PDF.** `PdfRenderer` writes objects to the stream as it produces them, with no whole-file
  buffer, and the destination does not need to seek. A cancelled render leaves what it had
  written in the stream, which is not a valid PDF, so discard it.
- **Raster.** `RasterRenderer.RenderPages(pages, destinationFor, format, token)` asks you for a
  stream per page. One page's bitmap is resident at a time: it is encoded into its stream and
  released before the next is drawn.
- **Sheet HTML.** `SheetHtmlWriter.Write` takes a stream, and a token.

```csharp
using XRay.Office;
using XRay.Office.Core.Documents;
using XRay.Office.Rendering.Raster;

using IDocument doc = OfficeDocument.Open("deck.pptx", token);
IPageSequence pages = ((IPaginatedDocument)doc).Layout(null, token);

new RasterRenderer(new RasterRenderOptions { Dpi = 96 }).RenderPages(
    Enumerable.Range(0, pages.Count).Select(i => pages[i]),
    page => File.Create($"slide-{page.Index + 1}.png"),
    RasterFormat.Png,
    token);
```
