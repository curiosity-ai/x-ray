---
label: Introduction
icon: house-chimney
order: 1
layout: default
---

[!hero
    title="X-Ray"
    subtitle="Pure C# NuGet packages for .NET 10: extract content from documents for search and LLMs, render office documents to PDF and images, and read Photoshop, email, iCalendar, PostScript/XPS and OneNote files, without installing an office suite or any other application."
    badge-text="X-Ray on NuGet"
    badge-link="https://www.nuget.org/packages/X-Ray/"
    cta1-text="Get started"
    cta1-link="/getting-started/"
    cta2-text="Pick a package"
    cta2-link="/getting-started/choosing-a-package"
]

# What X-Ray is

X-Ray is a family of .NET libraries from [Curiosity](https://curiosity.ai) for working with
the files people actually have: Office documents, PDFs, email, Photoshop files, scans,
PostScript, notebooks. Each package is written in managed C# and targets `net10.0`. Where a
package needs a native library (SkiaSharp for rasterising, HarfBuzz for shaping, PDFium for
OCR), that dependency is in an optional package or an opt-in code path, so the core of each
package runs anywhere .NET runs.

Several members are ports of established engines rather than new designs: X-Ray.Content
follows the Rust `xberg` extraction engine, X-Ray.Office follows LibreOffice's layout, and
X-Ray.Psd follows the PSD engine of the Patchy image editor. Each port is checked against its
reference implementation's output.

```sh
dotnet add package X-Ray            # the whole family
dotnet add package X-Ray.Content    # or only the member you need
```

---

## The packages

:::: card-grid

::: card {variant="grid" icon="file-export" title="X-Ray.Content" link="/content/"}
Text, Markdown, HTML, Djot and JSON from 90+ formats: Office, PDF, HTML, email, archives, ebooks, data files. Metadata, tables, images and a structured element tree, for search indexes and LLM pipelines.
:::

::: card {variant="grid" icon="file-word" title="X-Ray.Office" link="/office/"}
Reads Word, Excel and PowerPoint documents, modern and legacy (docx, xlsx, pptx, doc, xls, ppt, odt, ods, odp, rtf) and renders them to PDF and images with LibreOffice's layout rules.
:::

::: card {variant="grid" icon="file-psd" title="X-Ray.Psd" link="/psd/"}
Photoshop PSD and PSB: the layer tree, compositing to PNG or JPEG, and all text. Optional `X-Ray.Psd.Skia` and `X-Ray.Psd.Text` add Skia interop and type-layer re-rendering.
:::

::: card {variant="grid" icon="envelope" title="X-Ray.Email" link="/email/"}
Outlook `.msg`, `.eml` and Compound File Binary containers: read, create, edit and convert between MSG and EML, with an HTML preview and attachments as files.
:::

::: card {variant="grid" icon="calendar" title="X-Ray.Ics" link="/ics/"}
iCalendar and vCalendar files: events, to-dos, time zones, recurrence expansion, extraction of an appointment to HTML, text and Markdown, and a writer.
:::

::: card {variant="grid" icon="file-eps" title="X-Ray.PS" link="/ps/"}
PostScript, EPS and XPS documents: read them and convert them to PDF or raster images, with limits for hostile input and an optional Skia raster writer.
:::

::: card {variant="grid" icon="notebook" title="X-Ray.OneNote" link="/onenote/"}
OneNote `.one` files: pages, rich text, images, attachments and tables, and export to PDF and HTML.
:::

::::

---

## How the packages fit together

| Package | Answers | Output |
|---|---|---|
| [X-Ray.Content](/content/) | "What does this document say?" | Text and markup, metadata, tables, an element tree |
| [X-Ray.Office](/office/) | "What does this document look like?" | PDF pages and raster images |
| [X-Ray.Psd](/psd/), [X-Ray.Email](/email/), [X-Ray.Ics](/ics/), [X-Ray.PS](/ps/), [X-Ray.OneNote](/onenote/) | "Give me the object model of this one format." | A typed document model, plus rendering or conversion where the format has one |

X-Ray.Content is the broad one: it covers many formats at the level a search index or a
language model needs. The format packages go deep on one family of files each, with a full
object model, writing where it makes sense (email), and rendering.

The [`X-Ray`](/getting-started/installation#the-umbrella-package) package installs every
member. It contains no code.

---

## Pick your path

:::: card-grid
::: card {variant="grid" gradient="true" icon="rocket" title="Get started" link="/getting-started/"}
Install the umbrella or a single member, and check the requirements.
:::
::: card {variant="grid" gradient="true" icon="list-check" title="Choose a package" link="/getting-started/choosing-a-package"}
Which package to reach for, by task and by file type.
:::
::: card {variant="grid" gradient="true" icon="hourglass-end" title="Cancellation and streaming" link="/getting-started/cancellation-and-streaming"}
The convention every package follows for tokens, `OperationCanceledException` and stream outputs.
:::
::: card {variant="grid" gradient="true" icon="code-branch" title="Contributing" link="/contributing/"}
How the repositories are laid out, how packages are versioned and published, and how to work on these docs.
:::
::: card {variant="grid" gradient="true" icon="brands-github" title="Source" link="https://github.com/curiosity-ai/x-ray"}
The family's root repository: the umbrella package, these docs, and links to every member.
:::
::::
