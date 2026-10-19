---
label: Photoshop documents
icon: file-psd
order: 4
tags: [psd, photoshop]
---

# Photoshop documents

X-Ray.Content reads `.psd` and `.psb` files, MIME type `image/vnd.adobe.photoshop`. The Rust
engine X-Ray.Content follows has no Photoshop extractor, so this one is an addition, not a port. It
reads the file through [X-Ray.Psd](/psd/) and uses only its structural API. It never decodes or
renders pixels, so a multi-gigabyte PSB stays cheap to extract.

```csharp
using XRay.Content.Core;

var result = new Extractor().Extract(
    ExtractInput.FromUri("poster.psd"),
    new ExtractionConfig { OutputFormat = OutputFormat.Markdown });

Console.WriteLine(result.Results[0].Content);
```

An invalid file raises the usual parse failure, reported in `result.Errors`.

## How the document is organised

The output follows the layer tree as Photoshop's Layers panel shows it, top to bottom.

- The title is the XMP or IPTC title, else the file name without its extension.
- Top-level layers are second-level headings. A group's children are one level deeper. Past the
  sixth level a heading stays at six, so every layer is still a section of the JSON tree. With no
  title at all, layers start at first-level headings.
- Every layer gets a heading, pixel layers and adjustment layers included, and hidden layers are
  kept. Extraction reports what the file holds, not what a render shows.
- A type layer's text follows its heading, one paragraph per Photoshop paragraph, nested under that
  layer's section in the document structure. Where the global `Txt2` text engine data and the
  layer's own record disagree, the `Txt2` version is used, since that is what Photoshop displays.
  A `Txt2` object that no layer owns goes directly under the title.
- The layer tree of an embedded PSD or PSB smart object nests under the smart object layer's
  heading, up to four levels deep. A damaged one becomes a processing warning from the `psd` source.

## Metadata

Document facts are metadata, not body text:

- Image metadata: width, height, `PSD` or `PSB` as the format, and EXIF from image resource 1058.
- XMP and IPTC fields go to the title, subject, authors and keywords.
- `Metadata.Additional` holds the colour mode, bit depth, channel count, resolution, ICC profile,
  channel and path names, layer comps, slices (whose URLs also become `Uris`), fonts, linked file
  names and the layer counts of the outer document.

A PSD or PSB is detected by its `8BPS` signature or the `psd` and `psb` extensions.

## Cancellation

The extractor checks the token between layers and before each stage. The `X-Ray.Psd` `Load` and
`ExtractText` calls themselves are not interruptible until that package ships token overloads, so
cancellation of a single very large file waits for the call in progress. See
[Cancellation](/content/cancellation#what-cannot-be-interrupted).

## Other tools

Use X-Ray.Psd directly to composite or export a layer, to read the typed image resources, or to
redraw type layers.
