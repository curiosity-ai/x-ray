---
label: Sections and notebooks
icon: folder
order: 4
tags: [onenote, notebook, guid, toc]
---

# Sections and notebooks

A OneNote notebook is a folder of section files (`.one`) with a table of contents file
(`.onetoc2`) that lists them. The section files carry identities that tie them to the table of
contents.

## Identities

| Member | Source |
|---|---|
| `Document.Guid` | The section file's identity, the header `guidFile`. A table of contents lists a section under this value. |
| `Document.NotebookGuid` | The identity of the table of contents file the section belongs to (the header `guidAncestor`). |
| `Document.EntityGuid` | The section node's `NotebookManagementEntityGuid`, when stored. |
| `Document.CreationTime` | The section node's creation time stamp (UTC), when stored. |

```csharp
using XRay.OneNote;

var section = new Document("Notes.one");
Console.WriteLine($"{section.DisplayName}: {section.Guid} in notebook {section.NotebookGuid}");
```

## The table of contents

`NotebookToc.Load` reads a `.onetoc2` file: its `Guid`, `DisplayName`, `Color`, `EnableHistory`
and the ordered entries. Each `NotebookTocEntry` has a `FileName`, a `Guid`, an `OrderingId` and a
`Color`; an entry without a GUID is a folder (`IsFolder`). `Sections` lists the entries that are
sections.

```csharp
using XRay.OneNote;

var toc = NotebookToc.Load("Notebook/Open Notebook.onetoc2", token);
Console.WriteLine($"{toc.DisplayName} ({toc.Guid})");

foreach (var entry in toc.Sections)
{
    Console.WriteLine($"  {entry.FileName}  {entry.Guid}");
}

// Match a section to its place in the notebook.
var section = new Document("Notebook/Notes.one");
var position = toc.Sections.ToList().FindIndex(entry => entry.Guid == section.Guid);
Console.WriteLine(position);
```

!!! warning Best effort
The property ids of the table of contents are not in the published specifications this package
was built from, so entries are recognised by property type, and only the colour property id is
known. The reader has been tested against synthetic files, not against a file written by OneNote.
Treat its output as a hint, and match sections by `Document.Guid` rather than by position.
!!!

`.onepkg` packages (a CAB archive of a notebook) are not supported.
