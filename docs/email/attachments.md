---
label: Attachments
icon: clip
order: 4
tags: [email, attachments]
---

# Attachments

`MapiMessage.Attachments` is the raw model: one `MapiAttachment` per attachment storage or MIME
part. `GetAttachmentsFlattened()` is the adapter for code that wants files.

```csharp
using XRay.Email.Msg;

var message = MapiMessage.Load(path, token);

var options = new AttachmentFlattenOptions
{
    MaxDepth = 2,                                   // also list attachments inside embedded messages
    EmbeddedFormat = EmbeddedMessageFormat.Auto,    // .msg for an MSG source, .eml for an EML source
    IncludeInline = true,
    IncludeHidden = true,
};

foreach (var attachment in message.GetAttachmentsFlattened(options, token))
{
    Console.WriteLine($"{attachment.Path}: {attachment.Name} {attachment.ContentType} {attachment.Size} bytes");

    if (!attachment.HasContent) continue;           // an OLE storage attachment reads as empty

    await using var file = File.Create(Path.Combine(folder, attachment.Name));
    await attachment.CopyToAsync(file, token);
}
```

## What a `MessageAttachment` gives you

| Member | Content |
|---|---|
| `Name` | A file name that is safe on every platform and unique within the list, case-insensitively (`a.png`, `a (2).png`). Capped at 120 characters. |
| `ContentType` | The declared type, else one sniffed from image bytes, else one derived from the file name. An embedded message is `application/vnd.ms-outlook` or `message/rfc822`. |
| `Size`, `GetSize(token)` | The size in bytes. For an embedded message, the size of its serialisation. |
| `ContentId` | The content id without angle brackets, or `null`. |
| `IsInline`, `IsHidden` | Inline when it has a content id that the HTML body references as `cid:`, or is hidden, or is flagged as an MHTML reference; or when its Content-Location appears in the HTML body. `IsHidden` is `PR_ATTACHMENT_HIDDEN`. |
| `IsEmbeddedMessage`, `EmbeddedMessage` | Whether it is a message, and that message. |
| `Depth`, `Path` | Where it sits when embedded messages are flattened. |
| `Owner`, `Source` | The message it belongs to and the underlying `MapiAttachment`. |
| `OpenRead()`, `CopyTo()`, `CopyToAsync()` | Read it or copy it to a stream. |

## Embedded messages

An embedded message is named from its subject and serialised on demand, as MSG or as EML. With
`MaxDepth` 0 it is one entry. Each further level also lists the attachments inside embedded
messages, depth first, right after the embedded message's own entry, up to a cap of 32 levels.
`IncludeEmbeddedMessages = false` leaves the embedded messages themselves out of the list. The
attachments inside them are still listed when `MaxDepth` reaches them.

## Limits

An attachment that is an OLE storage rather than a file (an embedded Office object, for example)
reads as empty, and `HasContent` is `false`.
