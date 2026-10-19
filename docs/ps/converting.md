---
label: Converting documents
icon: file-export
order: 2
tags: [postscript, eps, xps, pdf, images, skia]
---

# Converting documents

## Loading

| Class | Loaders |
|---|---|
| `PsDocument` (PostScript and EPS) | `FromFile`, `FromBytes`, `FromStream`, each with an overload that takes a `CancellationToken` |
| `XpsDocument` | `FromFile`, `FromBytes`, `FromStream`, each with a token overload |

The document type is the input format, so you choose between `PsDocument` and `XpsDocument`
yourself.

## PDF

```csharp
using XRay.PS;
using XRay.PS.PostScript;
using XRay.PS.Xps;

byte[] fromPs  = PsDocument.FromFile("drawing.ps").ToPdf();
byte[] fromXps = XpsDocument.FromFile("report.xps").ToPdf(maxPages: 50);

// Straight into a stream, so a large PDF never exists as one array.
using var file = File.Create("drawing.pdf");
PsDocument.FromFile("drawing.ps").ToPdf(file, new PdfSaveOptions { JpegQuality = 75 }, token);
```

`PdfSaveOptions`:

| Option | Effect |
|---|---|
| `NoCompression` | Write content streams uncompressed, for inspection. |
| `AdditionalFontsFolder` | A folder of extra TrueType or OpenType fonts consulted before system fonts. |
| `JpegQuality` | Re-encode embedded raw 8-bit gray and RGB images as JPEG at this quality (1 to 100) when that is smaller than the deflated pixels. `null` passes images through unchanged. Masks, CMYK and already compressed images are never touched. |
| `MaskRasterizer` | XPS only: the rasteriser for opacity-masked content. The managed one when `null`; a `SkiaRasterWriter` for Skia. |
| `SuppressErrors`, `Warnings` | Recover from errors, see [Limits and recovery](/ps/limits-and-recovery). |
| `Limits` | Ceilings on a PostScript job. |

The PDF writer builds each object lazily and writes it in object-number order, keeping only the
cross-reference offsets in memory. The render model itself stays in memory.

## Images

```csharp
using XRay.PS;
using XRay.PS.PostScript;
using XRay.PS.Xps;

var options = new ImageSaveOptions(ImageFormat.Png, dpi: 150) { OpaqueBackground = true };

byte[] firstPage = PsDocument.FromFile("drawing.ps").ToImage(options);

var xps = XpsDocument.FromFile("report.xps");
List<byte[]> pages = xps.ToImages(options);

// One stream per page, opened just before that page is written.
xps.ToImages(options, page => File.Create($"page{page + 1}.png"), maxPages: null, cancellationToken: token);

// The first page into a stream.
using var single = File.Create("first.png");
xps.ToImage(options, single, cancellationToken: token);
```

`ToImage` converts the first page; `ToImages` converts every page. For PostScript, the
`ToImages(options, streamForPage, token)` overload takes the page index from zero.

`ImageSaveOptions`:

| Option | Default | Effect |
|---|---|---|
| `Format` | `Png` | `Png`, `Jpeg`, `Bmp` or `Tiff`. `ImageSaveOptions.ParseFormat("jpg")` parses a name. |
| `Dpi` | 96 | Output resolution. |
| `OpaqueBackground` | `false` | Paint a white background; PNG is transparent otherwise. |
| `PreserveFractionalPageSize` | `false` | Keep fractional page sizes instead of snapping to whole pixels. |
| `JpegQuality` | `null` | 1 to 100; the writer's default when `null` (75 for the managed writer, 80 for the Skia writer). |
| `RasterWriter` | `null` | A custom `IRasterWriter`; the managed renderer when `null`. |
| `AdditionalFontsFolder` | `null` | Extra fonts folder. |
| `SuppressErrors`, `Warnings`, `Limits` | | See [Limits and recovery](/ps/limits-and-recovery). `Limits.MaxMemoryBytes` also caps the page's pixel buffer, which is 4 GiB when no limits are set. |

PNG is encoded straight into the output stream. The JPEG, BMP and TIFF encoders need the whole
image, so those formats are encoded in memory first and then written.

## Fonts

Standard PostScript font names resolve to the bundled Liberation Serif and Dingbats fonts or to
fonts installed on the system. Set `AdditionalFontsFolder` on the save options to add a folder.

## The render model

`BuildRenderModel` returns the page content both input kinds are converted into before they are
written, for code that wants to inspect it or write it somewhere else. `PsDocument` also takes
`PsInterpreterLimits` and a suppress-errors flag in an overload.

## Building and editing documents

`PsDocument.Create`, `AddPage`, `InsertPage`, `RemovePage`, `GetPage(i).Canvas` and `Save()`
write PostScript or EPS; `GetXmp`, `SetXmp` and `RemoveXmp` handle XMP metadata.
`XpsDocument.Create`, `AddPage`, `InsertPage`, `RemovePage`, `Save()` and the print ticket methods
do the same for XPS. `ImageToEps.Convert` and `ConvertFile` wrap a raster image in EPS, with
overloads that write to a stream.

## Skia raster output

The core raster renderer is managed. For PostScript and EPS it reproduces, pixel for pixel, the pure
Python renderer of the original library: identical on all 400 convertible inputs. The original's
default writer is Skia, though, and its output differs slightly from the managed one: anti-aliasing,
glyph rasterisation, gradients and image resampling. `X-Ray.PS.Skia` provides `SkiaRasterWriter`,
which draws the same render model with Skia and reproduces the original's default output.

```csharp
using XRay.PS;
using XRay.PS.PostScript;
using XRay.PS.Skia;
using XRay.PS.Xps;

var options = new ImageSaveOptions(ImageFormat.Png, dpi: 150) { RasterWriter = new SkiaRasterWriter() };

byte[] png = PsDocument.FromFile("input.ps").ToImage(options);

var xps = XpsDocument.FromFile("input.xps");
List<byte[]> pages = xps.ToImages(options);

// XPS opacity masks are rasterized while building the PDF; Skia can do that too.
byte[] pdf = xps.ToPdf(new PdfSaveOptions { MaskRasterizer = new SkiaRasterWriter() });
```

Against the original's Skia output, the Skia writer was identical on 398 of 400 PostScript inputs
(largest mean difference 0.005), and on XPS 74 of 85 pages were identical and 84 of 85 within 1% of
pixels. The managed writer on XPS pages with images is further off, because the original needs
Skia for XPS images. Use the Skia writer when XPS raster output matters.

PNG and JPEG are drawn with Skia. TIFF and BMP output, and documents whose images Skia cannot take
safely (1-pixel images, CCITT data, samples other than 8 bits, full-page soft masks), fall back to
the managed renderer. A writer instance keeps no state between calls and can be shared.

On Linux, also reference a SkiaSharp native asset package, for example
`SkiaSharp.NativeAssets.Linux.NoDependencies`.

## Differences from the Python original

- JPEG XR images are not decoded.
- ICC-profile colours in XPS (`ContextColor`) use the original's fallback formula.
- Cancellation, stream outputs, interpreter limits, error suppression and JPEG re-encoding are
  additions with no counterpart in the original. Output is unchanged when they are not used.
