---
label: Overview
icon: envelope
order: 1
tags: [email, msg, eml, cfb]
---

# X-Ray.Email

X-Ray.Email reads, creates and edits Outlook `.msg` files and EML (MIME) messages, converts
between the two, and reads and writes the Compound File Binary (CFB) containers MSG files are
stored in. It renders a message as a self-contained HTML preview, picks the best body whichever
one a message stores, and lists attachments as files. The package has no dependencies outside the
.NET base class library and needs no Outlook installation.

The MSG and EML code is derived from an MIT-licensed open-source C# toolkit. Since the port it
has been tested against the specifications (MS-CFB, MS-OXMSG, MS-OXCMSG, MS-OXRTFCP, RFC 5322,
RFC 2045 to 2047, RFC 2231, RFC 6532), and against Python's `email` package and the `extract_msg`
library on a fixture corpus.

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-others"](https://github.com/curiosity-ai/x-ray-others)
[!ref icon="box-open" target="blank" text="X-Ray.Email on NuGet"](https://www.nuget.org/packages/X-Ray.Email/)

## Install

```sh
dotnet add package X-Ray.Email
```

The namespaces are `XRay.Email.Msg` (messages, preview, attachments) and `XRay.Email.Cfb`
(compound files).

## Quick start

Read a message, whichever format it is in:

```csharp
using XRay.Email.Msg;

var message = path.EndsWith(".eml", StringComparison.OrdinalIgnoreCase)
    ? MapiMessage.LoadFromEml(path)
    : MapiMessage.FromFile(path);

Console.WriteLine(message.Subject);
Console.WriteLine($"{message.SenderName} <{message.SenderSmtpAddress}>");
foreach (var recipient in message.Recipients)
{
    Console.WriteLine($"  to: {recipient.DisplayName} <{recipient.SmtpAddress ?? recipient.EmailAddress}>");
}

Console.WriteLine(message.Body ?? message.HtmlBody ?? message.RtfBody);
foreach (var attachment in message.Attachments)
{
    Console.WriteLine(attachment.IsEmbeddedMessage
        ? $"  message: {attachment.EmbeddedMessage?.Subject}"
        : $"  file: {attachment.Filename} ({attachment.Data.Length} bytes)");
}
```

Preview it as HTML, take the best text body, and store the attachments as files:

```csharp
using XRay.Email.Msg;

using var input = File.OpenRead(path);
var message = MapiMessage.Load(input, token);   // MSG or EML, detected from the bytes

await using (var html = File.Create("preview.html"))
{
    message.WriteHtml(html, new MailHtmlOptions { LinkifyPlainText = true }, token);
}

string? text = message.GetBestTextBody(token);   // plain text, derived if need be
bool unread = !message.IsRead;                   // PR_MESSAGE_FLAGS
DateTime? sent = message.SentTime;               // UTC

foreach (var attachment in message.GetAttachmentsFlattened(new AttachmentFlattenOptions { MaxDepth = 2 }, token))
{
    await using var file = File.Create(Path.Combine(folder, attachment.Name));
    await attachment.CopyToAsync(file, token);   // an embedded message is written as .msg or .eml
}
```

## Pages in this section

:::: card-grid
::: card {variant="grid" icon="envelope" title="Messages and conversion" link="/email/messages"}
`MapiMessage`, MSG and EML, creating messages, envelope helpers, CFB containers.
:::
::: card {variant="grid" icon="eye" title="HTML preview and bodies" link="/email/preview"}
`ToHtml` and its options, the body fallbacks, sanitising.
:::
::: card {variant="grid" icon="clip" title="Attachments" link="/email/attachments"}
`GetAttachmentsFlattened`, safe names, embedded messages, depth.
:::
::: card {variant="grid" icon="hourglass-end" title="Streams, cancellation and limits" link="/email/streams-and-cancellation"}
Reading and writing without whole-file buffers, tokens, errors, and what is not supported.
:::
::::

## X-Ray.Email or X-Ray.Content?

[X-Ray.Content](/content/) extracts the text, headers and attachment list of EML, MSG and PST
files for search. Use X-Ray.Email when you need the message as an object: to edit it, to write
MSG or EML, to read MAPI properties, to render a preview, or to store attachments as files.
X-Ray.Email does not read PST or OST mailboxes.

## License

MIT. The licence and copyright notice of the code it is derived from ship in the package.
