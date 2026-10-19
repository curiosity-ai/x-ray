---
label: Streams, cancellation and large files
icon: hourglass-end
order: 3
tags: [psd, cancellation, streams, bands, performance]
---

# Streams, cancellation and large files

## Cancellation

Every call that does real work takes a `CancellationToken` as its last parameter: `PsdDocument.Load`,
`LoadAsync`, `ExtractText`, `Render`, `RenderBands`, `RenderToPng`, `RenderToJpeg` and the thumbnail
writers, and in `X-Ray.Psd.Text` the text layout and rasterisation. The token is observed while the
file is read, at every layer record and tagged block the parser walks, at every channel decode,
between layers, at every strip of the parallel passes (no new strip starts once it is cancelled),
per band, and per paragraph, line and glyph in `X-Ray.Psd.Text`.

A cancelled call throws `OperationCanceledException`, never `PsdFormatException`. A render that
completes is bit-identical with or without a token. The
[family convention](/getting-started/cancellation-and-streaming) is the same for every package.

```csharp
using XRay.Psd;
using XRay.Psd.Rendering;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var token = timeout.Token;

using var document = PsdDocument.Load("poster.psd", new PsdLoadOptions { MemoryMap = true }, token);
var text = document.ExtractText(null, token);

// JPEG and PNG straight into the caller's stream (a temp file, a response body).
using var jpeg = File.Create("poster.jpg");
document.RenderToJpeg(jpeg, quality: 60, cancellationToken: token);
using var png = File.Create("poster.png");
document.RenderToPng(png, new RenderOptions { Source = RenderSource.Layers }, XRay.Psd.Codecs.PngCompressionLevel.Default, token);

// The thumbnail Photoshop stored: a stored JPEG is copied byte for byte, no decode or re-encode.
if (document.Resources.Thumbnail is { } thumbnail)
{
    using var small = File.Create("thumb.jpg");
    thumbnail.WriteJpeg(small, quality: 80, token);
}
```

## Stream outputs

`RenderToPng(Stream | path)` and `RenderToJpeg(Stream)` write the encoded image into the stream you
give them, so a rendered poster does not exist as one array. The PNG encoder filters rows in
parallel and deflates in segments of about one megabyte, each written as its own IDAT chunk; the
file does not depend on the band height or on the degree of parallelism.

`RenderToPng` writes RGB only when the result is opaque by construction (a `Background` is set, or
a merged image without transparency) and RGBA otherwise, because the encoder must choose before the
first row. `ToPng` on a rendered image drops alpha whenever every pixel happens to be opaque.

`PsdThumbnail.WriteJpeg` copies a stored JPEG thumbnail byte for byte and returns `true`. A thumbnail
stored as raw RGB is encoded at the quality you pass and the method returns `false`.

## Bands

Large documents render in horizontal bands, holding one band of the canvas at a time. The bands
are bit-identical to the same rows of `Render`, and they work past the size one canvas can hold
(about 46000 x 46000 pixels, and about 23000 x 23000 for one `RgbaImage`).

```csharp
using XRay.Psd;
using XRay.Psd.Rendering;

using var mapped = PsdDocument.Load("huge.psb", new PsdLoadOptions { MemoryMap = true });

// Stream straight into a PNG (RGBA, or RGB when a background or an opaque merged image makes it opaque).
mapped.RenderToPng("huge.png", new RenderOptions { Source = RenderSource.Layers });

// Or take the bands yourself, top to bottom.
foreach (var band in mapped.RenderBands(new RenderOptions { BandHeight = 512 }))
{
    Console.WriteLine($"{band.Top}: {band.Image.Height} rows");   // band.Image is document-wide
}
```

`RenderOptions.BandHeight` defaults to about four million pixels per band. Memory is bounded by the
band for documents built from plain layers, masks, adjustments and fills. Layers with effects,
groups with effects, feathered masks, stroked shapes and re-rendered type keep their whole-extent
planes, so a document whose styled layer is itself past about 46000 x 46000 pixels cannot render.
Effect planes are recomputed for every band a styled layer crosses, so effect-heavy documents
prefer tall bands.

## Loading

```csharp
using XRay.Psd;
using XRay.Psd.Rendering;

// Read asynchronously, or map a large file instead of reading it (dispose to release the mapping).
using var input = File.OpenRead("poster.psd");
var fromStream = await PsdDocument.LoadAsync(input, cancellationToken: token);
using var mapped = PsdDocument.Load("huge.psb", new PsdLoadOptions { MemoryMap = true });

// Rendering uses every processor by default; the result is the same at any degree.
var image = mapped.Render(new RenderOptions { MaxDegreeOfParallelism = 1 });
```

- `Load(Stream)` reads a seekable stream straight into one array of the remaining length.
  Overloads with a token also read in 1 MiB steps, so a cancelled load of a large file stops
  reading.
- `PsdLoadOptions.MemoryMap` maps the file read-only. Opening a large document then reads its
  records and little else, and channel data stays on disk until a render decodes it. The document
  owns the mapping and must be disposed; reading pixel data after `Dispose` throws
  `ObjectDisposedException`.
- Files of 2 GB and more are always memory-mapped and parsed with 64-bit offsets. Channels, the
  merged image and 16 and 32-bit layer blocks past 2 GB are read in pieces. Other global blocks
  past 2 GB, an embedded smart object for example, are still skipped.
- `MaxDegreeOfParallelism` sets how many processors a render uses. The result is identical at any
  degree. A server that renders many documents at once may prefer 1 and parallelism across
  documents.
