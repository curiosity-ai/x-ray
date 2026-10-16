---
label: Overview
icon: envelope
order: 1
tags: [email, msg, eml, cfb]
---

# X-Ray.Email

X-Ray.Email reads, creates and edits Outlook `.msg` files, converts between MSG and EML, and
reads and writes the Compound File Binary (CFB) containers MSG files are stored in. It is pure
managed code with no package dependencies and needs no Outlook installation.

!!! warning Stub
This page is a first outline, written while the package is being brought into the family. The
type names below are those of the code it is ported from; the namespaces follow the family
convention (`XRay.Email.*`) and may differ in the first release. Check the package README for
the final names.
!!!

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-others"](https://github.com/curiosity-ai/x-ray-others)
[!ref icon="box-open" target="blank" text="X-Ray.Email on NuGet"](https://www.nuget.org/packages/X-Ray.Email/)

## Install

```sh
dotnet add package X-Ray.Email
```

## What it does

| Layer | Types | Use |
|---|---|---|
| Messages | `MapiMessage`, `MapiRecipient`, `MapiAttachment` | Create, edit, save and reload messages; read and set MAPI properties; MSG to EML and back |
| MSG structure | `MsgReader`, `MsgWriter`, `MsgDocument` | The property streams, recipient and attachment storages of a `.msg`, for inspection or repair |
| CFB containers | `CfbReader`, `CfbWriter`, `CfbDocument` | Any OLE compound file: walk storages and streams, build and write new ones |

EML parsing and writing use a MIME implementation inside the package, not an external parser.
Structural problems found while parsing are listed in `ValidationIssues` rather than thrown.

## Quick start

Read a message:

```csharp
using XRay.Email.Msg;

using var stream = File.OpenRead("sample.msg");
var message = MapiMessage.FromStream(stream);

Console.WriteLine(message.Subject);
foreach (var attachment in message.Attachments)
{
    Console.WriteLine(attachment.Filename);
}
```

Create a message, save it as MSG, and convert it to EML:

```csharp
var message = MapiMessage.Create(subject: "Quarterly report", body: "Attached.");
message.AddRecipient("ana@example.com", "Ana");
message.AddAttachment("report.pdf", File.ReadAllBytes("report.pdf"), "application/pdf");

message.Save("report.msg");
message.SaveToEml("report.eml");
```

Load an `.eml` into the same model:

```csharp
var fromEml = MapiMessage.LoadFromEml("incoming.eml");
fromEml.Save("incoming.msg");
```

## X-Ray.Email or X-Ray.Content?

[X-Ray.Content](/content/) extracts the text, headers and attachment list of EML, MSG and PST
files for search. Use X-Ray.Email when you need the message as an object: to edit it, to write
MSG or EML, or to read MAPI properties.

## License

MIT.
