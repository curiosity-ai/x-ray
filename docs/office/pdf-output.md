---
label: PDF output
icon: file-pdf
order: 3
tags: [office, pdf, links, outline, metadata]
---

# PDF output

`PdfRenderer` is a hand-written PDF writer with subset font embedding, searchable text and a
`ToUnicode` map built from the shaping cluster map. Its output was compared operator for operator
with LibreOffice's own PDF export on a set of documents in four formats.

## What the file carries

Render with the overload that takes the document, so the file is written with the document's own
facts:

```csharp
using XRay.Office;
using XRay.Office.Core.Documents;
using XRay.Office.Rendering.Pdf;

using IDocument doc = OfficeDocument.Open("report.docx", token);
IPageSequence pages = ((IPaginatedDocument)doc).Layout(null, token);

using FileStream output = File.Create("report.pdf");
new PdfRenderer(new PdfRenderOptions { Language = "en-US" }).Render(doc, pages, output, token);
```

The PDF has:

- **Document information and XMP**: the title, author, subject, keywords and the application that
  made the source, from `IDocument.Metadata`, in the information dictionary and in an XMP packet.
- **Language**: the catalogue's `/Lang`, from `PdfRenderOptions.Language` or the document's own.
- **Links**: a `/Link` annotation with a `/URI` action for every hyperlink, and annotations with a
  destination for every link to a bookmark, cross-reference or table-of-contents entry, with a name
  tree of the destinations.
- **An outline**: the bookmarks panel, built from the headings of a word-processing document, the
  sheets of a workbook and the slides of a deck. A slide is called by its name when the deck gives
  one, else by the first line of its text, else "Slide N".
- **Page labels**, where the printed page numbers differ from the pages' positions, as after a
  title page or a restarted section.
- A file identifier (`/ID`).

Links, outline and labels are declared by the layout to the renderer, from DOCX, ODT, RTF and DOC
documents, from sheet names, and from slide titles.

## Options

| `PdfRenderOptions` | Default | Effect |
|---|---|---|
| `WriteDocumentInfo` | `true` | The information dictionary. |
| `WriteXmp` | `true` | The XMP packet. |
| `WriteLinks` | `true` | Link annotations and destinations. |
| `WriteOutline` | `true` | The bookmarks panel. |
| `WritePageLabels` | `true` | `/PageLabels`. |
| `Language` | `null` | The `/Lang` tag; taken from the document when `null`. |
| `Metadata` | `null` | The document metadata to write; set automatically by the overloads that take an `IDocument`. |
| `EmbedFonts` | `true` | Embed subset fonts. |
| `PassThroughJpeg` | `true` | Copy JPEG pictures without re-encoding. |
| `CreationDate` | `null` | A fixed timestamp for reproducible output. |
| `Producer` | `X-Ray.Office` | The producer string. |
| `MaxImagePixels`, `OversizedImages` | `0`, `Skip` | Picture limits; see [Cancellation and limits](/office/cancellation-and-limits). |
| `FontProvider` | file fonts | Where the bytes of a face come from. |

The overload `Render(pages, stream)` without a document writes the producer and date only, plus
whatever you put in `Metadata`.

## Other outputs

`RasterRenderer` writes PNG (lossless and deterministic) and JPEG at a chosen DPI over SkiaSharp.
`SheetHtmlWriter` writes a workbook as HTML. `MarkdownWriter` and `XhtmlWriter` write the
extracted content tree as GitHub-flavoured Markdown and XHTML.
