---
label: Cancellation and streaming
icon: hourglass-end
order: 4
tags: [cancellation, streams, convention]
---

# Cancellation and streaming across packages

Every X-Ray package that does non-trivial work follows the same three rules, so code written
against one carries over to the others.

1. **The token comes last.** A public method that does real work has an overload that takes a
   `CancellationToken` as its last parameter. Where the token is the only extra argument it may
   default to `default`.
2. **Cancellation is an `OperationCanceledException`.** A cancelled call throws it and nothing
   else. It is never wrapped in a package exception, never turned into a warning or an error item,
   and never swallowed by a recovery option such as `SuppressErrors`. A token that is already
   cancelled throws before the work starts.
3. **Large outputs go to a `Stream`.** Where a result can be large (a PDF, a PNG, an MSG, an HTML
   page), there is an overload that writes to a stream you provide, so the whole result need not
   exist as one array. The `byte[]` and `string` forms are thin wrappers over the stream forms.

A call that completes with a token returns the same result it returns without one.

## Where each package stands

| Package | Token overloads | Stream outputs | Page |
|---|---|---|---|
| X-Ray.Content | `Extractor.Extract`, `ExtractAsync` | Not applicable: the result is a document model | [Cancellation](/content/cancellation) |
| X-Ray.Office | `OfficeDocument.Open`, `Extract`, `Layout`, `PdfRenderer`, `RasterRenderer`, `SheetHtmlWriter`, the markup writers | PDF, raster pages and sheet HTML | [Cancellation and limits](/office/cancellation-and-limits) |
| X-Ray.Psd | `Load`, `LoadAsync`, `ExtractText`, `Render`, `RenderBands`, the encoders | PNG, JPEG, thumbnail, and bands | [Streams, cancellation and large files](/psd/streams-and-cancellation) |
| X-Ray.Email | Every load, save, convert, render and copy | MSG, EML, HTML preview, attachments | [Streams, cancellation and limits](/email/streams-and-cancellation) |
| X-Ray.Ics | Every load, expand, render and save | `Save`, `ToHtml`, `WriteTo` | [Writing, limits and cancellation](/ics/writing-and-limits) |
| X-Ray.PS | Every load and conversion | PDF, PNG and per-page images, EPS | [Limits, recovery and cancellation](/ps/limits-and-recovery) |
| X-Ray.OneNote | `Document.Load`, `Save`, `HtmlWriter`, `NotebookToc.Load` | PDF and HTML | [Cancellation, errors and limits](/onenote/limits) |

## The pattern

```csharp
using XRay.Ics;
using XRay.Office;
using XRay.Office.Core.Documents;
using XRay.Office.Rendering.Pdf;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var token = timeout.Token;

try
{
    // The token is the last argument everywhere.
    var calendar = Calendar.LoadFile("team.ics", cancellationToken: token);

    using IDocument document = OfficeDocument.Open("report.docx", token);
    IPageSequence pages = ((IPaginatedDocument)document).Layout(null, token);

    // The result goes to a stream, here a file, instead of one array.
    using var pdf = File.Create("report.pdf");
    new PdfRenderer().Render(document, pages, pdf, token);
}
catch (OperationCanceledException)
{
    // The same catch for every package. A partial output stream is not a valid file: discard it.
    Console.WriteLine("cancelled or timed out");
}
```

Two details differ:

- **A distinct timeout type.** X-Ray.Office's `LayoutOptions.Timeout` throws
  `LayoutTimeoutException`, a subtype of `OperationCanceledException`, so a caller can tell a
  document that took too long from a caller that cancelled.
- **Partial output.** A cancelled stream write leaves what it had written in the stream. X-Ray.OneNote
  deletes a file it was saving to by path when the save fails or is cancelled; with a stream it
  cannot, so discard the stream.

## What cannot be interrupted

A token is polled by the library's own loops. One call into a library that has no token runs to its
end. Each package page says which calls these are. For X-Ray.Content they are a single
`System.Text.Json` parse of one large document, SkiaSharp decoding one image, `X-Ray.Psd`'s `Load`
and `ExtractText`, and PDFium rasterising one page. For X-Ray.PS they are the Type 1 font resource
patterns, the XML parser for one part and the CLR's own zlib.

## Limits beside cancellation

A token covers a caller that loses patience. Limits cover a file that is hostile on arrival, and
they work without a token: parse limits in X-Ray.Ics, interpreter limits in X-Ray.PS, layout limits
in X-Ray.Office, bomb and recursion ceilings in X-Ray.Content, and the structural budgets in
X-Ray.OneNote. Use both when you convert files you do not control.
