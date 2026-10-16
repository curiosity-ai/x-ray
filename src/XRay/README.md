![X-Ray](https://raw.githubusercontent.com/curiosity-ai/x-ray/main/assets/logo.png)

# X-Ray

[![NuGet](https://img.shields.io/nuget/v/X-Ray?label=NuGet&color=5c2d91&logo=nuget&logoColor=white)](https://www.nuget.org/packages/X-Ray/)
[![Downloads](https://img.shields.io/nuget/dt/X-Ray?label=downloads&color=5c2d91)](https://www.nuget.org/packages/X-Ray/)
[![Docs](https://img.shields.io/badge/docs-x--ray.curiosity.ai-007ec6)](https://x-ray.curiosity.ai/)

The umbrella package for the **X-Ray** family of .NET libraries: pure C# packages for content
extraction, for rendering documents to PDF and images, and for reading specific file types.

**This package contains no code.** Installing it brings in every X-Ray package, so a consumer
who wants the whole family writes one line instead of tracking the list as it grows:

```sh
dotnet add package X-Ray
```

## What it installs

| Package | What it does |
|---|---|
| [`X-Ray.Content`](https://www.nuget.org/packages/X-Ray.Content/) | Content extraction for search and LLM use: text, Markdown, HTML, JSON, metadata, tables and images from PDF, Office, HTML, email, archives and 90+ more formats. |
| [`X-Ray.Office`](https://www.nuget.org/packages/X-Ray.Office/) | Reads Word, Excel and PowerPoint documents (docx, xlsx, pptx, doc, xls, ppt, odt, ods, odp, rtf and more) and renders them to PDF and images, following LibreOffice's layout. |
| [`X-Ray.Psd`](https://www.nuget.org/packages/X-Ray.Psd/) | Photoshop PSD/PSB: layer tree, compositing to PNG/JPEG, and text extraction. No dependencies outside the BCL. |
| [`X-Ray.Psd.Skia`](https://www.nuget.org/packages/X-Ray.Psd.Skia/) | SkiaSharp interop for X-Ray.Psd (`SKBitmap`, `SKImage`, WebP and other Skia encoders). |
| [`X-Ray.Psd.Text`](https://www.nuget.org/packages/X-Ray.Psd.Text/) | Re-renders PSD type layers from their text with SkiaSharp and HarfBuzz. |
| [`X-Ray.Email`](https://www.nuget.org/packages/X-Ray.Email/) | Outlook MSG, EML and Compound File Binary containers: read, create, convert. |
| [`X-Ray.PS`](https://www.nuget.org/packages/X-Ray.PS/) | PostScript, EPS and XPS: reading, and conversion to PDF and images. |
| [`X-Ray.OneNote`](https://www.nuget.org/packages/X-Ray.OneNote/) | OneNote `.one` files: pages, text, images, tables, and export to PDF. |

A member that has not been published yet is not a dependency of this package; it is added by
the first umbrella release after it ships.

## Or take just the one you need

Every member is independently installable, and nothing is lost by doing so. The umbrella is a
convenience, not a prerequisite:

```sh
dotnet add package X-Ray.Content
```

Prefer this if you only need one member: it keeps your dependency graph to what you use, and a
new family member cannot arrive in your build without you asking for it.

## Versioning

Versions are CalVer, `yy.M.<build>`. Each member is published by its own pipeline; each `X-Ray`
release depends on the newest version of every member that was on nuget.org when it was built.
Update `X-Ray` to move every member forward at once, or reference a member directly at a newer
version to move just that one.

## Links

- [Documentation](https://x-ray.curiosity.ai/)
- [Source](https://github.com/curiosity-ai/x-ray), and each member's own repository from the docs
- [Issues](https://github.com/curiosity-ai/x-ray/issues)

## License

`MIT AND Apache-2.0 AND MPL-2.0`. The expression covers what this package installs:

- `X-Ray.Content` is MIT, except a handful of files derived from Apache-2.0-only Rust crates,
  which keep that license.
- `X-Ray.Office` is MPL-2.0, matching the LibreOffice source it is derived from.
- `X-Ray.Psd`, `X-Ray.Psd.Skia`, `X-Ray.Psd.Text`, `X-Ray.Email`, `X-Ray.PS` and `X-Ray.OneNote`
  are MIT.

Each member's package carries its own license and notices.
