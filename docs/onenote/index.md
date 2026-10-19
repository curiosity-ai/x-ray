---
label: Overview
icon: notebook
order: 1
tags: [onenote, pdf, html]
---

# X-Ray.OneNote

X-Ray.OneNote reads Microsoft OneNote section files (`.one`). It parses the MS-ONESTORE revision
store and the MS-ONE object model into a tree of pages, outlines, formatted rich text, images,
tables, attached files, note tags and numbered lists, reconstructs page history, and exports
pages to PDF and HTML. It reads a notebook's table of contents file (`.onetoc2`) on a best-effort
basis. The package has no NuGet dependencies and no native code, and does not need OneNote or COM
automation.

It is a C# port of an MIT-licensed open-source Python OneNote reader. The DOM and the PDF export
were checked field by field against the reader's output on its twelve test files. The HTML
export, the GUIDs and the table of contents reader are additions of this package.

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-others"](https://github.com/curiosity-ai/x-ray-others)
[!ref icon="box-open" target="blank" text="X-Ray.OneNote on NuGet"](https://www.nuget.org/packages/X-Ray.OneNote/)

## Install

```sh
dotnet add package X-Ray.OneNote
```

The namespaces are `XRay.OneNote` (the document model) and `XRay.OneNote.Saving` (export
options and writers).

## Quick start

```csharp
using XRay.OneNote;
using XRay.OneNote.Saving;

// Load a section from a path or a stream.
var document = new Document("Notes.one");
Console.WriteLine(document.DisplayName);

// Walk the pages and their content.
foreach (var page in document.Pages)
{
    Console.WriteLine($"# {page.Title?.TitleText?.Text}  (level {page.Level}, by {page.Author})");

    foreach (var outline in page.GetChildNodes<Outline>())
    {
        foreach (var richText in outline.GetChildNodes<RichText>())
        {
            Console.WriteLine(richText.Text);
        }
    }
}

// Extract all text, including tables and titles.
var text = string.Join("\n", document.GetChildNodes<RichText>().Select(rt => rt.Text));

// Embedded pictures and files carry their bytes.
foreach (var image in document.GetChildNodes<Image>())
{
    File.WriteAllBytes(image.FileName ?? "image.bin", image.Bytes);
}

// Export to PDF, or to one HTML document.
document.Save("Notes.pdf");
document.Save("Notes.html");
```

## What it reads

The reader handles OneNote 2010 and later section files in the MS-ONESTORE format that a `.one`
file has on disk. It does not read OneNote 2007 sections, encrypted sections or `.onepkg`
packages, and it does not write `.one` files. The OneNote web (MS-FSSHTTPB) packaging and other
containers around the section data are not covered by this page: the package README lists the
containers each release supports, and it is the authority for the version you install. The
[limitations page](/onenote/limits) has the complete list.

## Pages in this section

:::: card-grid
::: card {variant="grid" icon="notebook" title="Document model" link="/onenote/model"}
`Document`, `Page`, `Outline`, `RichText`, images, tables, tags, visitors and page history.
:::
::: card {variant="grid" icon="file-export" title="PDF and HTML export" link="/onenote/export"}
`PdfSaveOptions`, `HtmlSaveOptions`, callbacks for pictures, files and pages, fonts.
:::
::: card {variant="grid" icon="folder" title="Sections and notebooks" link="/onenote/notebooks"}
GUIDs, `NotebookToc` and how a section finds its notebook.
:::
::: card {variant="grid" icon="hourglass-end" title="Cancellation, errors and limits" link="/onenote/limits"}
Tokens, exceptions, hostile input and what is not supported.
:::
::::

## X-Ray.OneNote or X-Ray.Content?

[X-Ray.Content](/content/) does not read OneNote files. X-Ray.OneNote gives you the page
structure, the formatting and the embedded files, and writes PDF and HTML.

## License

MIT. See the repository for the licence and third-party notices of the code it was ported from.
