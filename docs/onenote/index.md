---
label: Overview
icon: notebook
order: 1
tags: [onenote, pdf]
---

# X-Ray.OneNote

X-Ray.OneNote reads Microsoft OneNote `.one` section files: pages and their metadata, rich
text and hyperlinks, images, attached files, tables, tags and numbered lists. It can export
pages to PDF. It needs no OneNote installation or COM automation. It is a C# port of an
MIT-licensed Python OneNote reader.

!!! warning Stub
This package is being built. This page describes its scope; the quick start will be added with
the first release, from the package's real API.
!!!

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-others"](https://github.com/curiosity-ai/x-ray-others)
[!ref icon="box-open" target="blank" text="X-Ray.OneNote on NuGet"](https://www.nuget.org/packages/X-Ray.OneNote/)

## Install

```sh
dotnet add package X-Ray.OneNote
```

## Scope

- Parses the MS-ONE / OneStore binary container of a `.one` file, from a path or a stream.
- Exposes a document tree: a document holds pages; a page holds outlines; outlines hold rich
  text, images, tables and attached files.
- Reads page metadata (title, author, creation time, level) and page history.
- Exports pages to PDF.

## Quick start

To be written from the first release.

## License

MIT.
