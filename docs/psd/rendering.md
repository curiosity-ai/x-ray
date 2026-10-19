---
label: Rendering coverage
icon: picture
order: 2
tags: [psd, rendering, effects, adjustments, color]
---

# Rendering coverage

`PsdDocument.Render()` defaults to `RenderSource.Auto`: the merged image Photoshop saved, when the
file's version-info resource says it is real, and the layer compositor otherwise. A document saved
without "Maximize Compatibility" carries a placeholder merged image, so the compositor renders it.
`RenderSource.Layers`, or a `LayerVisibility` override, always uses the compositor.

The compositor works on straight-alpha floating point planes with SIMD kernels. Its rules were
calibrated against Photoshop's own renderings of the test files, and the project reports an
accuracy figure per fixture. This page lists what the compositor draws and where it is known to
differ. The repository's `docs/rendering.md` and `TODO.md` have the details.

## What is drawn

| Area | Covered |
|---|---|
| Blending | All 27 blend modes, opacity, Fill (with Photoshop's special handling on the modes that have it), Dissolve, Blend If, channel restrictions. |
| Structure | Pass Through and isolated groups, clipping runs, hidden layers. |
| Knockout | Shallow and deep, on layers, groups and clipping runs. Exact on the fixtures that carry it. |
| Masks | Raster masks and vector masks, with density and feather. |
| Fills | Solid colour, gradient and pattern fill layers, including noise gradients. Gradient fills snap to Photoshop's pixel grid, dither, and mix stops in the document's colour model. |
| Vector shapes | Boolean shapes, and vector strokes with width, alignment, caps, joins, miter limit, dashes, and solid, gradient or pattern paint. |
| Layer effects | Drop shadow, outer and inner glow, inner shadow, colour, gradient and pattern overlay, satin, strokes, bevel and emboss (all styles), contours, gradient glows, and the Photoshop 5 effects block, on layers, groups and clipping bases. |
| Adjustments | Levels, Curves, Hue/Saturation, Brightness/Contrast, Exposure, Invert, Threshold and Posterize, plus Color Balance, Photo Filter, Vibrance, Channel Mixer, Gradient Map and Black and White. |
| Colour modes | RGB, grayscale, bitmap, indexed, CMYK, Lab, duotone and multichannel at 1, 8, 16 and 32 bits. |
| Colour management | An embedded ICC profile converts gray, RGB, indexed and CMYK pixels and stored colours to sRGB (`PsdLoadOptions.ColorManagement`, on by default). |
| Large files | Files of 2 GB and more are memory-mapped and parsed with 64-bit offsets, and render in bands. See [Streams, cancellation and large files](/psd/streams-and-cancellation#bands). |

The adjustment layers that go beyond what the original Patchy renderer did (Color Balance, Photo
Filter, Vibrance, Channel Mixer, Gradient Map, Black and White) are fitted to a small number of
Photoshop captures. Their accuracy is stated per kind in the repository's `docs/adjustments.md`.

## Type layers

The core package draws a type layer from the pixels Photoshop stored with it. For files that store
none, or to redraw text with other fonts, `X-Ray.Psd.Text` re-renders type layers from their text
model with HarfBuzz shaping: point, box, vertical and on-path text, runs, leading, tracking, glyph
scales, alignment, transforms and Warp Text. Plug it in through `RenderOptions.TextRasterizer`.

## Not drawn, or approximate

- **Selective Color** is parsed and modelled but not rendered.
- **Color Lookup** parses `.cube`, `.3dl`, `.csp` and ICC lookups but is not rendered by default.
- **Effect Noise and Jitter** render as zero.
- **Layer comps** are readable (`PsdLayer.Metadata`, `PsdImageResources.LayerComps`), but a layer
  comp is not applied when rendering.
- **Colour space of blending.** Layers convert to sRGB when decoded and blend there, while
  Photoshop blends in the document's space. Non-linear modes differ in wide-gamut documents.
- **8-bit rounding.** The floating point pipeline stays within one level of Photoshop's integer
  kernels for single layers. Long stacks are not bit-exact.
- In CMYK documents, Hue/Saturation, Color Balance and the other colour-mixing adjustments run in
  RGB.
- Photoshop's own dither and noise sequences are unknown, so dithered gradients and noise differ
  in their individual values.

A document saved with "Maximize Compatibility" avoids all of these, because the merged image is
Photoshop's own.

## Hiding and replacing layers

```csharp
using XRay.Psd;
using XRay.Psd.Rendering;

var document = PsdDocument.Load("poster.psd");

// Render from the layers, without the type layers and without hidden ones.
var image = document.Render(new RenderOptions
{
    Source = RenderSource.Layers,
    LayerVisibility = layer => layer.IsVisible && layer.Kind != PsdLayerKind.Text,
});
image.SavePng("without-text.png");
```
