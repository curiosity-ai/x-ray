---
label: Overview
icon: file-eps
order: 1
tags: [postscript, eps, xps, pdf]
---

# X-Ray.PS

X-Ray.PS reads PostScript, Encapsulated PostScript (EPS) and XPS documents and converts them to PDF
or raster images (PNG, JPEG, BMP and TIFF), in pure managed C#. It contains a PostScript
interpreter, an XPS reader and writer, a render model shared by both inputs, a PDF writer with font
embedding, and a raster renderer with its own encoders. It needs no Ghostscript or Adobe tooling.

It is a port of an MIT-licensed open-source Python PostScript and XPS library. The port was
compared with the original on 583 PostScript and EPS inputs and 62 XPS inputs: success and failure
agree on every one, and the render model, PDF page count, sizes and text agree on every input that
converts. See [Converting documents](/ps/converting) for how the raster output compares.

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-others"](https://github.com/curiosity-ai/x-ray-others)
[!ref icon="box-open" target="blank" text="X-Ray.PS on NuGet"](https://www.nuget.org/packages/X-Ray.PS/)
[!ref icon="box-open" target="blank" text="X-Ray.PS.Skia on NuGet"](https://www.nuget.org/packages/X-Ray.PS.Skia/)

## Install

```sh
dotnet add package X-Ray.PS
dotnet add package X-Ray.PS.Skia   # optional: SkiaSharp raster output
```

The namespaces are `XRay.PS` (options and shared types), `XRay.PS.PostScript`, `XRay.PS.Xps` and
`XRay.PS.Skia`. The core package has no native dependencies; `X-Ray.PS.Skia` brings SkiaSharp.

## Scope

| Input | Output |
|---|---|
| PostScript (`.ps`) | PDF, PNG, JPEG, BMP, TIFF |
| EPS (`.eps`) | PDF, PNG, JPEG, BMP, TIFF |
| XPS (`.xps`) | PDF, PNG, JPEG, BMP, TIFF |

It can also build and edit PostScript, EPS and XPS documents, and wrap a PNG, JPEG, BMP or TIFF in
EPS.

## Quick start

```csharp
using XRay.PS;
using XRay.PS.PostScript;
using XRay.PS.Xps;

// PostScript or EPS to PDF and PNG
var ps = PsDocument.FromFile("drawing.ps");
File.WriteAllBytes("drawing.pdf", ps.ToPdf());
File.WriteAllBytes("drawing.png", ps.ToImage(new ImageSaveOptions(ImageFormat.Png, dpi: 150)));

// XPS to PDF, and one image per page
var xps = XpsDocument.FromFile("report.xps");
File.WriteAllBytes("report.pdf", xps.ToPdf());
List<byte[]> pages = xps.ToImages(new ImageSaveOptions(ImageFormat.Png, dpi: 96));

// Build and edit documents
var doc = PsDocument.Create(isEps: true, width: 200, height: 100);
doc.GetPage(0).Canvas.DrawText("Hello", 10, 10, "Helvetica", 12);
File.WriteAllBytes("hello.eps", doc.Save());

// Wrap a PNG, JPEG, BMP or TIFF in EPS
File.WriteAllBytes("logo.eps", ImageToEps.ConvertFile("logo.png"));
```

## Pages in this section

:::: card-grid
::: card {variant="grid" icon="file-export" title="Converting documents" link="/ps/converting"}
PDF and image output, streams, JPEG quality, fonts, the render model, and the Skia add-on.
:::
::: card {variant="grid" icon="shield-check" title="Limits, recovery and cancellation" link="/ps/limits-and-recovery"}
`PsInterpreterLimits`, `SuppressErrors`, warnings, and how a cancelled call ends.
:::
::::

## X-Ray.PS or X-Ray.Office?

X-Ray.Office renders Word, Excel and PowerPoint documents. X-Ray.PS renders PostScript, EPS and XPS.
[X-Ray.Content](/content/) does not read these formats.

## License

MIT, with the bundled Liberation Serif (OFL-1.1) and DjVu Dingbats (Bitstream Vera licence) fonts
under their own terms, so the package's licence expression is `MIT AND OFL-1.1`.
The `NOTICE` file in the package lists every third-party item, the ICC profile included.
