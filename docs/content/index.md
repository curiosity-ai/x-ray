---
label: Overview
icon: file-export
order: 1
tags: [extraction, search, llm]
---

# X-Ray.Content

X-Ray.Content extracts the content of a document: its text, rendered as plain text, Markdown,
HTML, Djot or JSON, plus metadata, tables, images, URIs and per-page content. It reads 97 MIME
types across 128 file extensions with 48 extractors. It is meant for search indexes and LLM
pipelines, where what matters is what a document says rather than how it is laid out.

It is a native C# port of the Rust [`xberg`](https://github.com/xberg-io/xberg) extraction
engine, checked against xberg's output on a shared fixture corpus. Extraction is pure managed
code. The optional OCR pass is the one exception, and it is off by default.

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-content"](https://github.com/curiosity-ai/x-ray-content)
[!ref icon="box-open" target="blank" text="X-Ray.Content on NuGet"](https://www.nuget.org/packages/X-Ray.Content/)

## Install

```sh
dotnet add package X-Ray.Content
```

## Quick start

```csharp
using XRay.Content.Core;

var extractor = new Extractor();

var result = extractor.Extract(
    ExtractInput.FromUri("quarterly-report.pdf"),
    new ExtractionConfig { OutputFormat = OutputFormat.Markdown });

var doc = result.Results[0];

Console.WriteLine(doc.MimeType);      // application/pdf
Console.WriteLine(doc.Content);       // the rendered Markdown
Console.WriteLine(doc.Metadata.Title);
Console.WriteLine(doc.Tables.Count);
```

When the file is already in memory, pass the bytes and their MIME type:

```csharp
using XRay.Content.Core;

var input = ExtractInput.FromBytes(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
```

`Extract` does not throw for a bad document. A failure is reported in `result.Errors` with a
type and a code, and an unsupported MIME type comes back as an empty document with a
`ProcessingWarning`. `ExtractAsync` takes the same arguments. Both take a `CancellationToken` as an extra last argument; see [Cancellation](/content/cancellation).

## Output formats

`ExtractionConfig.OutputFormat` selects how `ExtractedDocument.Content` is rendered. The
extracted document model is the same for every format; only the rendering differs.

| Format | Value |
|---|---|
| Plain text (default) | `OutputFormat.Plain` |
| Markdown (GFM) | `OutputFormat.Markdown` |
| HTML | `OutputFormat.Html` |
| Djot | `OutputFormat.Djot` |
| JSON section tree | `OutputFormat.Json` |
| Structured element list | `OutputFormat.Structured` |
| DocTags | `OutputFormat.DocTags` |

Related options:

- `ExtractedDocument.PlainContent` holds the plain-text rendering alongside `Content`, at no
  extra cost, for callers that index text and display markup.
- `IncludeDocumentStructure = true` adds the heading tree in `ExtractedDocument.Document`.
- `RenderPagesInOutputFormat = true` renders each entry of `ExtractedDocument.Pages` in the
  chosen output format too, so you can keep the Markdown and know which page each part is on.
- `ResultFormat.ElementBased` returns the flat element stream instead of one rendered string.

## Formats it reads

- **Office**: Word, Excel, PowerPoint (OOXML and legacy binary), OpenDocument, RTF,
  WordPerfect, HWP/HWPX, iWork
- **PDF**: text, geometry, tables, form fields, annotations, embedded files, bookmarks, page labels, XMP, and encrypted documents opened with a password (see [PDF](/content/pdf))
- **Markup and web**: HTML, XML, Markdown, MDX, Djot, AsciiDoc, reStructuredText, Org, Typst,
  LaTeX, DocBook, JATS, OPML
- **Email**: EML, MSG, PST
- **Books**: EPUB, FictionBook
- **Data**: CSV/TSV, DBF, JSON/JSONL, YAML, TOML, Jupyter notebooks
- **Academic**: BibTeX and citation formats
- **Subtitles**: WebVTT
- **Images**: metadata and EXIF (pixels are read only by the OCR pass)
- **Photoshop**: PSD/PSB layer structure and type-layer text, through [X-Ray.Psd](/psd/); see [Photoshop documents](/content/photoshop)
- **Archives**: ZIP, TAR, 7z, GZip, extracted recursively as child documents
- **Source code**: identified by extension or shebang

## Optional OCR

Off by default. Enabling it is the only way the package loads native code:

```csharp
using XRay.Content.Core;

var config = new ExtractionConfig
{
    Ocr = new OcrOptions { Mode = OcrMode.ScanOnly },   // or OcrMode.AllImages
};
```

| Mode | What it recognises |
|---|---|
| `Disabled` (default) | Nothing. No model is loaded and no native library is touched. |
| `ScanOnly` | PDF pages the scan detector flags; their text is appended to the page. |
| `AllImages` | Every embedded image above a size threshold, with its text inserted after the image, plus the `ScanOnly` behaviour. |

OCR only adds text; native text is never replaced. It is never fatal: a missing model, an
undecodable image or a recognition that exceeds `PerImageTimeout` becomes a
`ProcessingWarning`. Recognised tables come back as tables in `ExtractedDocument.Tables`.

The recognizer is [PaddleOCR](https://github.com/curiosity-ai/PaddleOCR/), which brings
SkiaSharp, and PDFium for rasterising scanned pages. Nothing is downloaded during extraction:
point `OcrOptions.ModelDirectory` at a checkpoint you have already fetched.

## More in this section

:::: card-grid
::: card {variant="grid" icon="file-pdf" title="PDF options" link="/content/pdf"}
Passwords and encryption, annotations, embedded files, form fields, outline, and the error types.
:::
::: card {variant="grid" icon="hourglass-end" title="Cancellation" link="/content/cancellation"}
The token overloads, the ambient token contract, and what cannot be interrupted.
:::
::: card {variant="grid" icon="file-psd" title="Photoshop documents" link="/content/photoshop"}
How a PSD or PSB becomes headings, text and metadata.
:::
::::

## Differences from xberg

The port aims for output identical to xberg's, with documented exceptions. The two that change
results are the optional OCR pass above, which uses a different engine from xberg's, and
Excel number formatting: numeric cells are rendered through their number format (`3.2%`,
`$31,200`) rather than as the raw value, controlled by `XRayOptions.ExcelNumberFormats`.

The port also adds features xberg does not have. Photoshop files are read by an extractor of
their own, since xberg has none. The PDF reader gained annotations, embedded files and password
handling, and extraction can be cancelled. None of these change the output for documents that do
not use them. Audio and video transcription, embeddings, chunking, keyword extraction and the
server modes of xberg are out of scope.

The repository's
[CLAUDE.md](https://github.com/curiosity-ai/x-ray-content/blob/main/CLAUDE.md) describes the
architecture, scope and every deviation in detail.
