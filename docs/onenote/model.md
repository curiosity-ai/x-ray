---
label: Document model
icon: notebook
order: 2
tags: [onenote, dom, history]
---

# Document model

## Loading

```csharp
using XRay.OneNote;

var fromPath   = new Document("Notes.one");
var fromStream = new Document(File.OpenRead("Notes.one"));

// The static forms take a token.
var loaded = Document.Load("Notes.one", new LoadOptions { LoadHistory = true }, token);
```

Loading takes a path or a stream. `LoadOptions` has `LoadHistory` (see below) and
`DocumentPassword`, which is rejected: encrypted sections are not supported.

## The tree

| Node | Content |
|---|---|
| `Document` | The section: `DisplayName`, `Guid`, `NotebookGuid`, `EntityGuid`, `CreationTime`, and its pages in section order, with subpages included. |
| `Page` | `Title`, `Author`, `CreationTime`, `LastModifiedTime`, `Level`. |
| `Title` | `TitleText`, `TitleDate`, `TitleTime`, each a `RichText`. |
| `Outline` | A note container: offsets and maximum width, holding `OutlineElement` nodes. |
| `OutlineElement` | One paragraph level. Nested elements form indented levels, and `NumberList` marks list items. |
| `RichText` | `Text`, `TextRuns` (each with a `TextStyle`: font, size, colour, highlight, bold, italic, underline, strikethrough, super and subscript, hyperlink, language), `ParagraphStyle`, `Alignment`, `Tags`. |
| `Image` | `Bytes`, `FileName`, size, `Alignment`, alternative text, hyperlink, `Tags`. |
| `Table`, `TableRow`, `TableCell` | Rows of cells that hold outline content, `Columns` with widths, `Tags`. |
| `AttachedFile` | `Bytes`, `FileName`, `Tags`. |
| `NoteTag` | `Label`, `Icon` (shape), `Status`, colours, creation and completion times. |

Traverse with `CompositeNode.GetChildNodes<T>()`, which returns every descendant of a type in
document order, by enumerating a node's children, or with a `DocumentVisitor` passed to `Accept`.
The visitor also visits the rows and cells of tables.

```csharp
using XRay.OneNote;

var document = new Document("Notes.one");

foreach (var table in document.GetChildNodes<Table>())
{
    foreach (var row in table.OfType<TableRow>())
    {
        var cells = row.OfType<TableCell>().Select(cell => string.Join(" ", cell.GetChildNodes<RichText>().Select(rt => rt.Text)));
        Console.WriteLine(string.Join(" | ", cells));
    }
}
```

## Page history

Load with `LoadHistory = true` to reconstruct earlier versions of each page from the revision
store. `Document.GetPageHistory(page)` returns them oldest first, with the current version in
`Current`.

```csharp
using XRay.OneNote;

var document = new Document("Notes.one", new LoadOptions { LoadHistory = true });
var page = document.Pages.First();
foreach (var version in document.GetPageHistory(page))
{
    Console.WriteLine(version.LastModifiedTime);
}
```

Without `LoadHistory`, `GetPageHistory` returns a history that holds only the current page.

## What is not modelled

Ink, equations, page backgrounds as drawing content, and outline positions on the page are not in
the model. Merged cells and header rows of tables are not modelled either. Images and attachments
that OneNote stores outside the section file, in a `onefiles` folder next to it, are listed with
empty `Bytes`.
