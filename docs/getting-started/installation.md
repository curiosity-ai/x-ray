---
label: Installation
icon: download
order: 2
---

# Installation

## Requirements

| Requirement | Value |
|---|---|
| .NET | **.NET 10** (`net10.0`). The packages do not multi-target older frameworks. |
| Operating system | Any that .NET 10 supports. Pure managed packages need nothing else. |
| Native libraries | Only for the packages and options listed in [Native dependencies](#native-dependencies). |

## The umbrella package

[`X-Ray`](https://www.nuget.org/packages/X-Ray/) contains no code. It depends on every member
of the family, so one reference brings in all of them:

+++ .NET CLI
```sh
dotnet add package X-Ray
```
+++ PackageReference
```xml
<PackageReference Include="X-Ray" Version="*" />
```
+++

Each `X-Ray` release depends on the newest version of every member that was published when it
was built. A member that has not been published yet is not a dependency; the first `X-Ray`
release after it ships adds it.

## Individual packages

Install only the members you use:

```sh
dotnet add package X-Ray.Content    # content extraction
dotnet add package X-Ray.Office     # Word, Excel, PowerPoint: read and render to PDF/images
dotnet add package X-Ray.Psd        # Photoshop PSD/PSB
dotnet add package X-Ray.Psd.Skia   # optional: SkiaSharp interop for X-Ray.Psd
dotnet add package X-Ray.Psd.Text   # optional: re-render PSD type layers
dotnet add package X-Ray.Email      # MSG, EML, CFB
dotnet add package X-Ray.Ics        # iCalendar and vCalendar: events, recurrence, writing
dotnet add package X-Ray.PS         # PostScript, EPS, XPS
dotnet add package X-Ray.PS.Skia    # optional: SkiaSharp raster output for X-Ray.PS
dotnet add package X-Ray.OneNote    # OneNote .one
```

Nothing is lost by referencing a member directly. Two reasons to prefer it:

- Your dependency graph holds only what you use. X-Ray.Office, for example, brings SkiaSharp
  and HarfBuzzSharp for rendering; a service that only extracts text does not need them.
- A new family member cannot arrive in your build without you asking for it.

You can also mix the two: reference `X-Ray`, and add a direct reference to one member at a
newer version than the umbrella pins. NuGet resolves the higher version.

## Package ids and namespaces

Package ids are hyphenated; assemblies and namespaces are not, because a hyphen is not valid
in a C# identifier:

| Package id | Namespace |
|---|---|
| `X-Ray.Content` | `XRay.Content` |
| `X-Ray.Office` | `XRay.Office` |
| `X-Ray.Psd`, `X-Ray.Psd.Skia`, `X-Ray.Psd.Text` | `XRay.Psd`, `XRay.Psd.Skia`, `XRay.Psd.Text` |
| `X-Ray.Email` | `XRay.Email` |
| `X-Ray.Ics` | `XRay.Ics` |
| `X-Ray.PS`, `X-Ray.PS.Skia` | `XRay.PS`, `XRay.PS.Skia` |
| `X-Ray.OneNote` | `XRay.OneNote` |

## Native dependencies

The core of every package is managed code. Native code enters only here:

| Package or option | Native dependency | Why |
|---|---|---|
| X-Ray.Content with OCR enabled (`OcrOptions.Mode` other than `Disabled`) | SkiaSharp, PDFium | Image decoding and rasterising scanned PDF pages. Off by default; not loaded unless enabled. |
| X-Ray.Office rendering | SkiaSharp, HarfBuzzSharp | Rasterising pages and shaping text the way LibreOffice does. |
| X-Ray.Psd.Skia | SkiaSharp | `SKBitmap`/`SKImage` interop and Skia's encoders. |
| X-Ray.Psd.Text | SkiaSharp, HarfBuzzSharp | Drawing type layers from their text. |
| X-Ray.PS.Skia | SkiaSharp | Skia raster output for PostScript, EPS and XPS pages. |

`X-Ray.Psd`, `X-Ray.Email`, `X-Ray.Ics`, `X-Ray.PS` and `X-Ray.OneNote` have no dependencies outside
the .NET base class library.

## Versions

Versions are CalVer: `yy.M.<build>`, for example `26.9.5114`. Every package is published by
its own pipeline, so members' versions differ from each other and from `X-Ray`'s. There are no
prerelease suffixes; a higher version is a newer build.

## Licenses

| Package | License |
|---|---|
| X-Ray.Content | MIT AND Apache-2.0 (a few files derive from Apache-2.0-only Rust crates) |
| X-Ray.Office | MPL-2.0 (derived from LibreOffice) |
| X-Ray.Psd, X-Ray.Psd.Skia, X-Ray.Psd.Text, X-Ray.Email, X-Ray.Ics, X-Ray.PS.Skia, X-Ray.OneNote | MIT |
| X-Ray.PS | MIT AND OFL-1.1 (it bundles Liberation Serif under OFL-1.1 and DjVu Dingbats under the Bitstream Vera licence) |
| X-Ray (umbrella) | MIT AND Apache-2.0 AND MPL-2.0 AND OFL-1.1, covering what it installs |
