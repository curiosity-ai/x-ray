---
label: Overview
icon: plug
order: 1
tags: [documentextraction, microsoft-extensions, ingestion, provider, preview]
---

# X-Ray.DocumentExtraction

A provider for `Microsoft.Extensions.DocumentExtraction`, the .NET document extraction abstraction
(`DocumentExtractionClient`), backed by [X-Ray.Content](https://www.nuget.org/packages/X-Ray.Content/).
Give it a stream and a media type, get back the provider-neutral `ExtractionDocument`: nested
sections, headings, text, tables with addressable cells, images, and page references with regions.
Any code written against the abstraction, such as middleware, the data ingestion pipeline or a
provider-switching host, can use X-Ray.Content's 90+ formats without knowing about it.

The default path is pure managed code and calls no service. OCR is available through
X-Ray.Content's opt-in pass and is off unless you configure it.

> **Preview.** The abstraction is experimental (`EXTEXP0019`, `MEDE0001`) and this package is
> published with a `-preview` version suffix. Expect changes when the abstraction changes.

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray"](https://github.com/curiosity-ai/x-ray)
[!ref icon="box-open" target="blank" text="X-Ray.DocumentExtraction on NuGet"](https://www.nuget.org/packages/X-Ray.DocumentExtraction/)

## Install

```sh
dotnet add package X-Ray.DocumentExtraction --prerelease
```

The namespace is `XRay.DocumentExtraction`. For middleware (`UseLogging`, the builder, dependency
injection helpers) also add `Microsoft.Extensions.DocumentExtraction`.

## Quick start

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using XRay.DocumentExtraction;

DocumentExtractionClient client = new XRayDocumentExtractionClient();

await using var file = File.OpenRead("report.docx");
ExtractionDocument document = await client.ExtractAsync(
    file,
    "report.docx",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    cancellationToken: CancellationToken.None);

foreach (var element in document.EnumerateContent())
{
    switch (element)
    {
        case ExtractionDocumentTextElement { Content: TextContent text } t:
            Console.WriteLine($"{t.Role ?? "text"}: {text.Text}");
            break;
        case ExtractionDocumentTableElement table:
            Console.WriteLine($"table {table.Cells.RowCount} x {table.Cells.ColumnCount}");
            break;
    }
}
```

## Configuration

```csharp
var client = new XRayDocumentExtractionClient(new XRayDocumentExtractionOptions
{
    IncludeImageBytes = true,
    Configure = config => config.Pdf.Password = "secret",
});
```

`Configure` receives X-Ray.Content's `ExtractionConfig` once, at construction. The client always
turns on the document structure, which the mapping reads.

## What maps to what

| X-Ray.Content | Abstraction |
|---|---|
| Title | text element, role `title` |
| Heading, level n | section opened at level n, first element a text element with role `heading` and `HeadingLevel` n |
| Slide | section with the slide's title |
| Paragraph | text element |
| List item, code, formula, footnote | text element, roles `listItem`, `code`, `formula`, `footnote` |
| Header and footer layers | text element, roles `pageHeader` and `pageFooter` |
| Table | table element: cells with row and column spans, role `columnHeader` or `content` |
| Image | image element: alternative text, source, bytes with `IncludeImageBytes` |
| Page number and bounding box | source reference: a page (`page-N`) and the box as a four-point polygon |
| Title, authors, dates, language | `ExtractionDocument.Metadata` |

The native result is on `RawRepresentation` (an `XRay.Content.Types.ExtractedDocument`) for anything
the abstraction does not model.

Errors: a format without an extractor throws `NotSupportedException`; a document that cannot be
read throws `InvalidDataException`; a cancelled token throws `OperationCanceledException`.
`DocumentExtractionOptions.ExtractorName` is rejected, since the provider has one extractor.

## Licence

MIT AND Apache-2.0, the expression of X-Ray.Content, whose code this package runs.
