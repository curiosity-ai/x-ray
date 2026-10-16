---
label: Overview
icon: file-psd
order: 1
tags: [psd, photoshop, rendering, text]
---

# X-Ray.Psd

X-Ray.Psd reads Photoshop documents (PSD and PSB), renders them to PNG or JPEG, and extracts
their text. The core package has no dependencies outside the .NET base class library, and its
hot paths use `System.Numerics.Vector<T>` SIMD. It is a port of the PSD engine in Patchy, a
C++ image editor, whose renders are checked against Photoshop's own.

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-others"](https://github.com/curiosity-ai/x-ray-others)
[!ref icon="box-open" target="blank" text="X-Ray.Psd on NuGet"](https://www.nuget.org/packages/X-Ray.Psd/)

## Packages

| Package | Adds | Dependencies |
|---|---|---|
| `X-Ray.Psd` | Reading, compositing, PNG/JPEG encoding, text extraction | None |
| `X-Ray.Psd.Skia` | `SKBitmap`/`SKImage` interop, Skia encoders such as WebP | SkiaSharp |
| `X-Ray.Psd.Text` | Re-rendering type layers from their text | SkiaSharp, HarfBuzzSharp |

```sh
dotnet add package X-Ray.Psd
dotnet add package X-Ray.Psd.Skia   # optional
dotnet add package X-Ray.Psd.Text   # optional
```

## What it reads

- PSD and PSB, 1/8/16/32 bits per channel, in RGB, grayscale, bitmap, indexed, CMYK, Lab,
  duotone and multichannel modes.
- Raw, RLE, ZIP and ZIP-with-prediction channel data.
- The layer tree with groups, blend modes, opacity, fill, clipping, raster masks and vector
  masks; layer effects; adjustment layers; solid, gradient and pattern fills.
- Text: layer and group names, type-layer content with fonts and style runs, channel and path
  names, slices, XMP and IPTC metadata, and text inside embedded PSD/PSB smart objects.
- Typed views of common image resources: resolution, guides and grid, thumbnail, slices, layer
  comps, version info, ICC profile.

Pixel data stays compressed until rendering asks for it, so text extraction and inspection do
not decode pixels.

## Quick start

```csharp
using XRay.Psd;
using XRay.Psd.Rendering;

var document = PsdDocument.Load("poster.psd");

// Render and save. RenderSource.Auto (the default) uses the merged image Photoshop
// saved when there is one, and the layer compositor otherwise.
document.Render().SavePng("poster.png");
document.Render(new RenderOptions { Background = PsdColor.White }).SaveJpeg("poster.jpg", quality: 85);

// All text, in Layers panel order.
foreach (var item in document.ExtractText().Items)
{
    Console.WriteLine($"{item.Kind}: {item.Source}: {item.Text}");
}

// Walk the layer tree.
foreach (var layer in document.EnumerateLayersTopDown())
{
    Console.WriteLine($"{layer.Path} {layer.Kind} {layer.BlendMode} {layer.Bounds}");
    if (layer.Text is { } text)
    {
        Console.WriteLine($"  \"{text.Text}\" in {string.Join(", ", text.Fonts)}");
    }
}
```

Hide layers while rendering, for example every type layer:

```csharp
var withoutText = document.Render(new RenderOptions
{
    LayerVisibility = layer => layer.IsVisible && layer.Kind != PsdLayerKind.Text,
});
```

### Re-rendering type layers

`X-Ray.Psd.Text` draws type layers from their text model, for files that store no pixels for
them or when the text should be drawn with other fonts:

```csharp
using XRay.Psd.Text;

var fonts = new FontCollection();
fonts.AddFile("fonts/LiberationSans-Regular.ttf");

using var rasterizer = new TextLayerRasterizer(new TextRenderSettings
{
    FontResolver = new CompositeFontResolver(fonts, SystemFontResolver.Default),
});

document.Render(new RenderOptions { TextRasterizer = rasterizer }).SavePng("filled-in.png");
```

## Rendering coverage

The layer compositor follows Photoshop's blending rules for groups, clipping, masks, fills,
layer effects (shadows, glows, overlays, satin, strokes, bevel and emboss) and the common
adjustments (Levels, Curves, Hue/Saturation, Brightness/Contrast, Exposure, Invert, Threshold,
Posterize). Not yet rendered from layers: vector strokes on shape layers, Blend If, and some
adjustments (Color Balance, Vibrance, Black and White, Channel Mixer, Selective Color, Gradient
Map, Photo Filter). Documents saved with "Maximize Compatibility" render exactly, through the
merged image.

## License

MIT.
