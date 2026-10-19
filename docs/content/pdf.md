---
label: PDF options
icon: file-pdf
order: 2
tags: [pdf, encryption, annotations, attachments]
---

# PDF options

The PDF reader extracts text, geometry, tables, form fields, bookmarks, page labels and XMP
metadata. `ExtractionConfig.Pdf` (`PdfOptions`) controls the parts that need a decision: passwords,
annotations and embedded files. Every option is off or inert for a document that does not use the
feature, so none of them changes the output for an ordinary PDF.

```csharp
using XRay.Content.Core;

var config = new ExtractionConfig
{
    OutputFormat = OutputFormat.Markdown,
    Pdf = new PdfOptions
    {
        Password = "secret",
        ExtractAnnotations = true,
        ExtractEmbeddedFiles = true,
        AttachEmbeddedFileBytes = false,
    },
};
```

| `PdfOptions` | Default | Effect |
|---|---|---|
| `Password` | `null` | Password for an encrypted document, tried first, as the user password and then as the owner password. |
| `OwnerPassword` | `null` | An owner password, tried after `Password`. |
| `Passwords` | `null` | Further passwords to try, in order. The empty password is always tried last. |
| `ExtractAnnotations` | `false` | Extract page annotations into `ExtractedDocument.Annotations`, append them to the rendered content, and surface link URIs in `Uris`. |
| `ExtractEmbeddedFiles` | `true` | Extract embedded documents through the normal pipeline into `ExtractedDocument.Children`, and list them in `Attachments`. |
| `MaxEmbeddedFileBytes` | `null` | Largest decompressed size of one embedded file that is extracted. |
| `AttachEmbeddedFileBytes` | `false` | Keep each embedded file's bytes on `ExtractedAttachment.Data`. Off because a PDF can carry hundreds of megabytes of attachments. |

The passwords are never serialised, so a logged or stored configuration does not leak them.

## Encrypted documents

The standard security handler is supported for revisions V1, V2, V4 (RC4 or AES-128 through crypt
filters) and V5 (AES-256, revisions 5 and 6). Authentication is verified, not guessed: the
password is checked against the document's own validation data, so a wrong password is a typed
failure rather than garbled text. Both the user and the owner password are accepted. A document that only
restricts copying or printing opens with the empty password and needs none.

A document using a handler other than the standard one is not decrypted and keeps the older
warning. `/Identity` crypt filters, cleartext metadata, `/EmbeddedFile` filters and `/Crypt` stream
filters are honoured per stream.

### Errors

A password failure is reported as an error item, not an exception:

| `ErrorType` | `Code` | When |
|---|---|---|
| `password_required` | 1010 | The document is encrypted and no password was supplied (the empty password did not open it). |
| `invalid_password` | 1011 | Passwords were supplied and none of them opened the document. |

```csharp
using XRay.Content.Core;

var result = new Extractor().Extract(
    ExtractInput.FromUri("locked.pdf"),
    new ExtractionConfig { Pdf = new PdfOptions { Passwords = ["first guess", "second guess"] } });

foreach (var error in result.Errors)
{
    if (error.ErrorType == "password_required")
    {
        Console.WriteLine("ask the user for a password");
    }
    else if (error.ErrorType == "invalid_password")
    {
        Console.WriteLine("that password did not work");
    }
}
```

The internal exception, `PdfPasswordRequiredException`, has `PasswordWasSupplied` and
`Encryption`. It is deliberately not an `InvalidDataException`, which is what a corrupt file fails
with. The `Extractor` envelope converts it to the error items above.

For a document that opened, `Metadata.Format.Payload` is a `PdfMetadata` with the encryption facts:
`IsEncrypted`, `RequiresPassword`, `AuthenticatedAs` (`user`, including the empty password, or
`owner`), `EncryptionAlgorithm` (`RC4`, `AESV2`, `AESV3` or `Identity`), `EncryptionKeyBits` and
`Permissions` (print, modify, copy, annotate, fill forms, accessibility extraction, assemble and
high quality print).

## Annotations

With `ExtractAnnotations = true`, comments, highlights, stamps, links and the other annotation
types are read into `ExtractedDocument.Annotations`:

```csharp
using XRay.Content.Core;

var result = new Extractor().Extract(
    ExtractInput.FromUri("reviewed.pdf"),
    new ExtractionConfig { Pdf = new PdfOptions { ExtractAnnotations = true } });

foreach (var annotation in result.Results[0].Annotations ?? [])
{
    Console.WriteLine($"p{annotation.PageNumber} {annotation.AnnotationType} by {annotation.Author}: {annotation.Content}");
    if (annotation.MarkedText is { } marked)
    {
        Console.WriteLine($"   on the text: {marked}");
    }
}

foreach (var uri in result.Results[0].Uris ?? [])
{
    Console.WriteLine($"{uri.Url} (page {uri.Page})");
}
```

A `PdfAnnotation` has the `AnnotationType`, `Subtype`, `Content`, `PageNumber`, `BoundingBox`,
`Author`, `Created`, `Modified`, `Color`, `Subject`, `QuadPoints`, `MarkedText`, `Flags`,
`IsVisible`, `Destination` and the page's size, box and rotation. `MarkedText` is the text a
highlight, underline or strike-out covers, recovered by intersecting the annotation's quad points
with the page's word boxes. The annotations are also appended to the rendered content as an
"Annotations" section in plain text, Markdown, HTML and Djot, and in the JSON tree.

Link annotations and outline entries become `Uris`. Not ported from the Rust crate: using
free-text annotations as the body of a page that has none.

## Embedded files

Attachments in the `/EmbeddedFiles` name tree, file-attachment annotations, PDF portfolio members
and the catalog's `/AF` entries are extracted by default.

```csharp
using XRay.Content.Core;

var config = new ExtractionConfig
{
    Pdf = new PdfOptions { MaxEmbeddedFileBytes = 50_000_000, AttachEmbeddedFileBytes = true },
};
var document = new Extractor().Extract(ExtractInput.FromUri("portfolio.pdf"), config).Results[0];

foreach (var attachment in document.Attachments ?? [])
{
    Console.WriteLine($"{attachment.Name} {attachment.MimeType} {attachment.Size} bytes from {attachment.Source}");
}

foreach (var child in document.Children ?? [])
{
    Console.WriteLine($"{child.Path}: {child.Result.Content.Length} characters of {child.MimeType}");
}
```

`Attachments` lists each file with its name, type, size, compressed size, description,
`/AFRelationship`, dates, where it came from (`Source`) and, when `AttachEmbeddedFileBytes` is on,
its bytes. `Children` holds each file extracted as a document of its own.

Extraction is bounded. Recursion shares the archive depth limit (3), a file is decoded under the
tightest of the per-file cap, what is left of the archive size budget, and the compression ratio
limit times the stored size, so a bomb is stopped while inflating.

## Form fields and document facts

`ExtractedDocument.FormFields` lists the interactive form fields (text, checkbox, radio, choice,
button, signature) with their values, options, page and rectangle. `PdfMetadata.Outline` holds the
bookmarks as a flat list with `Title`, `Level` and `Page`. The catalog's `/Lang` goes to
`Metadata.Language`, and non-standard Info entries and extra XMP properties go to
`Metadata.Additional`.
