---
label: Streams, cancellation and limits
icon: hourglass-end
order: 5
tags: [email, streams, cancellation]
---

# Streams, cancellation and limits

## Streams

- **Reading.** `FromFile` and a seekable `FromStream` read MSG sectors on demand instead of
  copying the file. A non-seekable stream is spooled to a temporary file, deleted when the message
  is disposed, and never held whole in memory. The parsed message still holds its properties and
  attachment bytes.
- **Writing.** `Save(Stream)`, `SaveToEml(Stream)`, `MsgWriter.Write` and `CfbWriter.Write` write
  straight to the stream: an MSG a sector at a time, an EML with attachments base64-encoded block
  by block. The `byte[]` forms are thin wrappers.
- **EML parsing** works from one block, since decoded parts stay in memory anyway. `LoadFromEml`
  reads the stream into one exactly sized array.
- **Attachments** are copied with `CopyTo` and `CopyToAsync`, and `OpenRead` returns a stream.
- **Preview.** `WriteHtml(Stream)` writes the document without building it as a string first.

```csharp
using XRay.Email.Msg;

var message = MapiMessage.LoadFromEml("big.eml");

// Straight to disk, no byte[] for the whole file.
await using var target = File.Create("big.msg");
message.Save(target, token);

// AddAttachment also takes a stream.
await using var data = File.OpenRead("report.pdf");
message.AddAttachment("report.pdf", data);
```

## Cancellation

Every load, save, convert, render and copy method takes a `CancellationToken` as its last
parameter and stops promptly with `OperationCanceledException`, which is never swallowed or
turned into a package exception. The token is checked at loop boundaries: CFB sector chains every
256 sectors, MIME parts and lines, writer sectors every 64, base64 blocks, HTML tokens every 4,096
and RTF control tokens every 4,096. The
[family convention](/getting-started/cancellation-and-streaming) applies.

```csharp
using XRay.Email.Msg;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    var message = MapiMessage.Load(path, timeout.Token);
    var html = message.ToHtml(null, timeout.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("gave up");
}
```

## Errors and hostile input

Malformed MSG input raises `CfbException` or `MsgException`, or is read leniently and reported in
`ValidationIssues`. EML parsing does not throw. Recursion over file structure is bounded: MIME
nesting has a depth limit, the MSG storage tree has a depth limit and an ancestor check, and the
CFB tree walk keeps a visited set. A truncation and mutation test suite guards these paths.

## What is not supported

- **S/MIME**: signed and encrypted messages (`smime.p7m`). Such a message shows whatever body it
  carries, and the opaque part comes through as an attachment.
- **TNEF**: `winmail.dat` is an attachment, not unpacked.
- **Mail servers**: no IMAP, POP3 or SMTP.
- **PST and OST mailboxes**. Use [X-Ray.Content](/content/) to extract text from a PST.
- **OLE storage attachments** read as empty.
