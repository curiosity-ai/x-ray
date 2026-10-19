---
label: Messages and conversion
icon: envelope
order: 2
tags: [email, msg, eml, cfb, mapi]
---

# Messages and conversion

## Two levels

| Level | Types | Use |
|---|---|---|
| Message | `MapiMessage`, `MapiRecipient`, `MapiAttachment` | Create, edit, save and reload messages. Subject, bodies, sender, recipients, attachments, embedded messages and arbitrary MAPI properties. |
| MSG structure | `MsgReader`, `MsgWriter`, `MsgDocument` | The property streams and the recipient and attachment storages of a `.msg`, for inspection or repair. |
| Compound file | `CfbReader`, `CfbWriter`, `CfbDocument` | Any OLE compound file: walk storages and streams, build and write new ones. |

## Loading

```csharp
using XRay.Email.Msg;

var fromMsg = MapiMessage.FromFile("sample.msg");             // MSG
var fromEml = MapiMessage.LoadFromEml("incoming.eml");        // EML
var either  = MapiMessage.Load("unknown.bin", token);         // MSG or EML, detected from the bytes

Console.WriteLine(either.SourceFormat);                       // Msg, Eml or Created
```

`FromFile` and a seekable `FromStream` read an MSG on demand rather than copying it.
`MapiMessage.Load(Stream | path)` detects the format from the bytes. EML parsing never throws:
the MIME reader handles multipart structures, nested `message/rfc822` parts, quoted-printable
and base64, RFC 2047 encoded words in any charset the platform provides, raw UTF-8 headers
(RFC 6532), RFC 2231 parameters, address groups and comments, and obsolete date syntax. Damaged
input degrades instead of failing: missing closing boundaries, truncated base64, broken header
folding.

## Creating and converting

```csharp
using XRay.Email.Msg;

var message = MapiMessage.Create("Quarterly report", "Please find the report attached.");
message.SenderName = "Alice";
message.SenderEmailAddress = "alice@example.com";
message.AddRecipient("bob@example.com", "Bob");
message.AddRecipient("carol@example.com", "Carol", MapiMessage.RecipientTypeCc);
message.AddAttachment("report.pdf", File.ReadAllBytes("report.pdf"), "application/pdf");

message.Save("report.msg");
message.SaveToEml("report.eml");

// Convert an EML file to MSG.
MapiMessage.LoadFromEml("message.eml").Save("message.msg");
```

`SaveToEml` writes any message as MIME with non-ASCII headers and file names encoded.
Attachments are written base64, text included, so bytes stay exact. MIME boundaries carry a
random token, so compare EML output structurally and not byte for byte.

MSG writing follows MS-OXMSG. String properties are stored in the message's code page
(`PR_MESSAGE_CODEPAGE`, then `PR_INTERNET_CPID`, else Windows-1252).

## Exchange addresses

Outlook often stores the sender and recipients of internal mail as X.500 names rather than SMTP
addresses. `MapiMessage.SenderSmtpAddress` and `MapiRecipient.SmtpAddress` resolve the SMTP address
where the message carries one.

## Envelope helpers

Typed access to the properties a mail UI shows:

```csharp
using XRay.Email.Msg;

var message = MapiMessage.Load(path, token);

MapiMessageFlags? flags = message.MessageFlags;         // PR_MESSAGE_FLAGS
bool read   = message.IsRead;
bool draft  = message.IsDraft;                          // MSGFLAG_UNSENT
MapiImportance? importance   = message.Importance;      // Low, Normal, High
MapiSensitivity? sensitivity = message.Sensitivity;

DateTime? sent     = message.SentTime;                  // client submit time, else submit time, else delivery time (UTC)
DateTime? created  = message.CreationTime;
DateTime? modified = message.LastModificationTime;

// Any other property, by id.
string? subject = message.GetPropertyString((ushort)CommonMessagePropertyId.Subject);
int? flagsValue = message.GetPropertyInt32((ushort)CommonMessagePropertyId.MessageFlags);
bool has        = message.HasProperty((ushort)CommonMessagePropertyId.InternetMessageId);
```

The other typed readers are `GetPropertyBoolean`, `GetPropertyDateTime`, `GetPropertyBytes` and the
generic `GetPropertyValue<T>`. Calendar and contact properties have no dedicated API; they are
reachable through these.

## Compound files

`CfbReader` and `CfbWriter` read and write compound files (versions 3 and 4) independently of MSG:

```csharp
using XRay.Email.Cfb;

using var cfb = CfbReader.FromFile("message.msg");
foreach (var (depth, entry) in cfb.IterTree())
{
    Console.WriteLine($"{new string(' ', depth * 2)}{entry.Name} ({entry.StreamSize} bytes)");
}
```

## Damaged files

Malformed MSG input raises `CfbException` or `MsgException`, or is read leniently with an entry in
`MapiMessage.ValidationIssues` (trailing bytes in a property stream, counts that do not match, a
missing `__nameid` storage). Pass `strict: true` to `FromFile` or `FromStream` to reject those
instead. No other exception type escapes loading.
