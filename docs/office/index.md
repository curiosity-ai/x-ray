---
label: Overview
icon: file-word
order: 1
tags: [office, rendering, pdf]
---

# X-Ray.Office

X-Ray.Office reads word-processing, spreadsheet and presentation documents and renders them
to PDF and images. It is a C# port of the relevant parts of LibreOffice (Writer, Calc and
Impress), developed against headless LibreOffice as the reference: a rendered page is compared
with LibreOffice's rendering of the same file. It needs no LibreOffice process, office install
or COM interop.

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-office"](https://github.com/curiosity-ai/x-ray-office)
[!ref icon="box-open" target="blank" text="X-Ray.Office on NuGet"](https://www.nuget.org/packages/X-Ray.Office/)

## Formats

| Family | Formats |
|---|---|
| Word processing | `docx docm dotx dotm`, `doc dot`, `rtf`, `odt ott fodt`, `sxw stw` |
| Spreadsheets | `xlsx xlsm xltx xltm xlsb`, `xls xlt`, `ods ots fods`, `csv`, `sxc stc` |
| Presentations | `pptx pptm potx potm ppsx ppsm`, `ppt pot pps`, `odp otp fodp`, `sxi sti` |

The format is always identified from the file's content, not its extension. Macro-enabled
files are read as data: the package reports that macros are present and never runs them.
Draw, Math and Base documents, and writing any office format, are out of scope.

## Install

```sh
dotnet add package X-Ray.Office
```

`X-Ray.Office` is the facade: it identifies a file and dispatches to the right reader, and it
brings the `X-Ray.Office.*` libraries it needs. A service that only needs one family (for
example only spreadsheets) can reference that library directly instead.

Rendering uses SkiaSharp for rasterising and HarfBuzzSharp for text shaping, the shaper
LibreOffice uses.

## Quick start

Extract the content, without layout or fonts:

```csharp
using XRay.Office;
using XRay.Office.Core.Extraction;
using XRay.Office.Markup;

ContentDocument content = OfficeDocument.Extract("report.docx");
string text = content.GetText();
string markdown = MarkdownWriter.ToMarkdown(content);
```

Render: open the document, lay it out (a separate, deferred step), then draw pages:

```csharp
using XRay.Office;
using XRay.Office.Core.Documents;
using XRay.Office.Rendering.Pdf;
using XRay.Office.Rendering.Raster;

using IDocument doc = OfficeDocument.Open("deck.pptx");
IPageSequence pages = ((IPaginatedDocument)doc).Layout();

// One page to PNG.
using (var png = File.Create("slide1.png"))
{
    new RasterRenderer(new RasterRenderOptions { Dpi = 150 }).RenderToPng(pages[0], png);
}

// Every page to PDF.
using var pdf = File.Create("deck.pdf");
new PdfRenderer().Render(pages, pdf);
```

A workbook can also be written as HTML, one table per sheet, in the shape Calc's own HTML
filter writes:

```csharp
using XRay.Office;
using XRay.Office.Core.Documents;
using XRay.Office.Spreadsheets.Html;
using XRay.Office.Spreadsheets.Layout;

using IDocument book = OfficeDocument.Open("report.xlsx");
SpreadsheetPages sheets = (SpreadsheetPages)((IPaginatedDocument)book).Layout();
SheetHtmlWriter.Write(sheets, File.Create("report.html"));
```

To check what a file is without reading it, `Identify` reads only the container header:

```csharp
using XRay.Office;
using XRay.Office.Core.Formats;

FormatIdentification format = OfficeDocument.Identify("unknown.bin");
Console.WriteLine(format.IsEncrypted);
```

## More in this section

:::: card-grid
::: card {variant="grid" icon="hourglass-end" title="Cancellation and limits" link="/office/cancellation-and-limits"}
Tokens, `LayoutOptions.Timeout`, `MaxPages`, `MaxImagePixels`, and streamed output.
:::
::: card {variant="grid" icon="file-pdf" title="PDF output" link="/office/pdf-output"}
Metadata, links, bookmarks, the outline, page labels and `PdfRenderOptions`.
:::
::: card {variant="grid" icon="check-circle" title="Fidelity and gaps" link="/office/fidelity"}
How close the rendering is to LibreOffice's, what is measured, and the encryption gap.
:::
::::

## X-Ray.Office or X-Ray.Content?

Both read office documents. [X-Ray.Content](/content/) extracts text, Markdown and tables for
search and LLM use across 90+ formats. X-Ray.Office lays the document out and draws it, so use
it when you need pages: PDF conversion, thumbnails, previews.

## License

MPL-2.0, matching the LibreOffice source it is derived from.
