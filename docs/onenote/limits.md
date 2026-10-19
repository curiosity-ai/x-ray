---
label: Cancellation, errors and limits
icon: hourglass-end
order: 5
tags: [onenote, cancellation, errors, limitations]
---

# Cancellation, errors and limits

## Cancellation

`Document.Load(stream | path, options, token)`, the `Save` overloads that take a token,
`HtmlWriter` and `NotebookToc.Load` check the token while they parse, lay out and write, and throw
`OperationCanceledException`. Loops that walk file nodes, revisions, object groups, the logical
graph, pages, outlines, rows and image rows all check it. `SaveOptions.CancellationToken` does the
same for an options object that is passed around; it is combined with the token passed to `Save`.

```csharp
using XRay.OneNote;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
try
{
    var document = Document.Load("Notes.one", null, timeout.Token);
    document.Save("Notes.pdf", timeout.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("cancelled");
}
```

Saving to a path deletes the partial file when the save is cancelled or fails. Saving to a stream
may leave partial output in it. See the
[family convention](/getting-started/cancellation-and-streaming).

## Errors

| Exception | Raised when |
|---|---|
| `FileCorruptedException` | The input is damaged or truncated. |
| `UnsupportedFileFormatException` | The input is not a OneNote section this library can read. `FileFormat` says what it was detected as. |
| `IncorrectPasswordException` | The section is password protected, or a `DocumentPassword` was given. Encrypted sections are not supported. |
| `UnsupportedSaveFormatException` | A save target other than PDF or HTML. |
| `IncorrectDocumentStructureException` | The tree does not have the shape an operation needs. |

All derive from `OneNoteException`. Malformed input loads best-effort where it can (property sets
stop at the damage, file node lists stop at a bad node) or throws one of these. Cycles and
pathological sizes are bounded: fragment chains, revision chains, the depth and node budget of the
logical graph, and array and ID range counts. A truncation and mutation test suite guards the
loader.

## Limitations

- Reads OneNote 2010 and later section files. OneNote 2007 sections, encrypted sections and
  `.onepkg` packages are not supported, and the library does not write `.one` files.
- Containers other than a plain `.one` section file, the MS-FSSHTTPB (OneNote web) packaging
  included, are not documented here. The package README lists what each release reads.
- Images and attachments that OneNote keeps outside the section file are listed with empty
  `Bytes`.
- Ink, equations and outline positions are not modelled. The PDF export lays outlines out in
  reading order.
- PDF images: TIFF, EMF, WMF and other formats print the file name instead.
- The notebook table of contents reader is best effort, see
  [Sections and notebooks](/onenote/notebooks).
