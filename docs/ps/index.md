---
label: Overview
icon: file-eps
order: 1
tags: [postscript, eps, xps, pdf]
---

# X-Ray.PS

X-Ray.PS reads PostScript, Encapsulated PostScript (EPS) and XPS documents and converts them
to PDF or raster images (PNG, JPEG, BMP, TIFF), without Ghostscript or Adobe tooling. It is a
C# port of an MIT-licensed Python library for the same formats.

!!! warning Stub
This package is being built. This page describes its scope; the quick start will be added with
the first release, from the package's real API.
!!!

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-others"](https://github.com/curiosity-ai/x-ray-others)
[!ref icon="box-open" target="blank" text="X-Ray.PS on NuGet"](https://www.nuget.org/packages/X-Ray.PS/)

## Install

```sh
dotnet add package X-Ray.PS
```

## Scope

| Input | Output |
|---|---|
| PostScript (`.ps`) | PDF, PNG, JPEG, BMP, TIFF |
| EPS (`.eps`) | PDF, PNG, JPEG, BMP, TIFF |
| XPS (`.xps`) | PDF, PNG, JPEG, BMP, TIFF |

The source library is organised as a document per format (a PostScript document, an XPS
document), a render model built from it, and writers for PDF and raster output. The C# port is
expected to follow the same split.

## Quick start

To be written from the first release.

## License

MIT.
