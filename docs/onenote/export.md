---
label: PDF and HTML export
icon: file-export
order: 3
tags: [onenote, pdf, html]
---

# PDF and HTML export

`Document.Save` writes PDF or HTML, to a path or to a stream. A path ending in `.pdf` writes PDF,
`.html` or `.htm` writes one HTML document, and any other extension raises
`UnsupportedSaveFormatException`. `Save(path, format)` and `Save(stream, options)` name the format
or the options directly.

## PDF

The PDF export writes titles, outlines with list numbering, formatted runs, clickable hyperlinks,
note tag glyphs, images (PNG, JPEG, GIF and BMP), attached file names and tables split across
pages. It lays the content out in reading order, not at the positions OneNote shows. TIFF and
other picture formats are not drawn; their file name is printed instead.

```csharp
using XRay.OneNote;
using XRay.OneNote.Saving;

var document = new Document("Notes.one");

// The first page only, to a stream.
document.Save(Stream.Null, new PdfSaveOptions { PageIndex = 0, PageCount = 1 });

// Everything, straight into a file stream, with a deadline.
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
using var pdf = File.Create("Notes.pdf");
document.Save(pdf, new PdfSaveOptions(), timeout.Token);
```

`PdfSaveOptions` shares `PageIndex`, `PageCount`, `FontsSubsystem` and `CancellationToken` with
`HtmlSaveOptions` through `SaveOptions`.

### Fonts

Text in Latin-1 uses the PDF standard fonts. Text outside Latin-1 (Cyrillic, for example) is drawn
with a TrueType font that is embedded as a subset. The library looks for one in the usual Windows,
Linux and macOS font directories. To control where fonts come from:

```csharp
using XRay.OneNote;
using XRay.OneNote.Saving;

var document = new Document("Notes.one");

var fonts = new FontsSubsystem { SearchSystemFontDirectories = false };
fonts.FontDirectories.Add("/path/to/fonts");   // e.g. DejaVuSans.ttf, LiberationSans-Regular.ttf, arial.ttf
document.Save("Notes.pdf", new PdfSaveOptions { FontsSubsystem = fonts });
```

When no suitable font is found, characters the standard fonts cannot show are written as `?`.
`FontsSubsystem.UseTrueTypeForAllText` embeds a TrueType font for all text, and the environment
variable `XRAY_ONENOTE_PDF_USE_SYSTEM_FONTS` turns that on by default.

## HTML

The HTML export writes HTML 5: UTF-8, no script, deterministic output (the same document and
options always produce the same bytes), with stable `on-` class names and the CSS in a `<style>`
element.

- Outlines keep their horizontal offset and width, in document order.
- Outline elements nest with their list markers.
- Runs carry bold, italic, underline, strikethrough, super and subscript, colour, highlight, font
  and size.
- Links are anchors, and only `http`, `https`, `mailto`, `ftp`, `tel`, `onenote` and `file`
  targets are written.
- Tags are inline markers. Tables, pictures and attached files are written in place.
- Hidden text stays in the markup with the class `on-hidden`; `ShowHiddenText` shows it.

Ink and other items the document model does not hold are not written.

### One document or one per page

`HtmlSaveOptions.PerPage` is `true` by default: each page becomes an HTML document. With a path
target, the pages of a multi-page section go to `name_000.html`, `name_001.html` and so on beside
the path. With a stream target there is no single destination, so a `PageSavingCallback` supplies
each page's stream. `Document.Save("Notes.html")` and `Save(path, SaveFormat.Html)` set
`PerPage = false` and write one document with an `<article class="on-page">` per page.

### Callbacks

```csharp
using XRay.OneNote;
using XRay.OneNote.Saving;

var document = new Document("Notes.one");

// One HTML document per page, pictures served from your own URLs, each page into your own stream.
document.Save(Stream.Null, new HtmlSaveOptions
{
    ImageSavingCallback = args => args.Url = $"./api/blob/get/{args.PageIndex}-{args.ImageIndex}",
    PageSavingCallback = args => args.OutputStream = File.Create($"page-{args.PageIndex}.html"),
});

// One document, everything embedded (pictures as data: URIs).
document.Save("Notes.html");
```

The callbacks receive:

| Callback | Arguments |
|---|---|
| `ImageSavingCallback` | `ImageSavingArgs`: the `Image` node, `Data`, `Format`, `Extension`, `MimeType`, `FileName`, `SuggestedFileName`, `PageIndex`, `ImageIndex`, `GlobalImageIndex`. Set `Url` to choose the `src`, `OutputStream` to receive the bytes (with `KeepStreamOpen` to keep it open), or `SkipEmbedding` to leave the picture out. |
| `FileSavingCallback` | `FileSavingArgs`: the `AttachedFile`, `FileName`, `Size`, `PageIndex`, `FileIndex`. Set `Url`, or `OutputStream` to receive the bytes. |
| `PageSavingCallback` | `PageSavingArgs`: `PageIndex`, `Page`, `Title`, `Level`. Set `OutputStream` for the page's document. |

Other options:

| `HtmlSaveOptions` | Default | Effect |
|---|---|---|
| `EmbedCss` | `true` | The CSS goes in a `<style>` element. When `false`, the page links `StyleSheetUrl` (`onenote.css`), and `HtmlWriter.StyleSheet` is the CSS to serve. |
| `EmbedImages` | `true` | Pictures become `data:` URIs. A URL chosen by the callback takes precedence. |
| `EmbedAttachments` | `false` | Attached files become `data:` links. Off because attachments can be large. |
| `ShowHiddenText` | `false` | Show runs formatted as hidden. |
| `Language` | `null` | The value of `<html lang>`. `null` omits the attribute. |
| `ExportFonts` | `false` | Fonts are never exported; setting it to `true` throws `NotSupportedException`. |

`HtmlWriter.WritePage` and `HtmlWriter.WriteDocument` are the lower-level entry points.

## Streams

PDF and HTML are written to the stream as they are produced. Saving to a path that fails or is
cancelled deletes the files it created.
