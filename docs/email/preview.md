---
label: HTML preview and bodies
icon: eye
order: 3
tags: [email, html, preview, rtf]
---

# HTML preview and bodies

## The preview

`message.ToHtml()` renders one self-contained, deterministic HTML document in the layout Outlook
displays: a header block (From, Sent, To, Cc, Bcc, Subject, Attachments) above the body.
`WriteHtml(Stream, ...)` writes the same document to a stream. Scripts, event handlers and
scripting URLs are removed by default, `cid:` images are inlined as `data:` URIs, and the output
is UTF-8 with the charset declared.

```csharp
using XRay.Email.Msg;

var message = MapiMessage.Load(path, token);

string html = message.ToHtml(new MailHtmlOptions
{
    LinkifyPlainText = true,
    TimeZone = TimeZoneInfo.Utc,
}, token);

// Straight to a stream, as a fragment for embedding in your own page.
using var fragment = File.Create("body.html");
message.WriteHtml(fragment, new MailHtmlOptions { FullDocument = false, IncludeHeader = false }, token);
```

The output has no timestamps and does not depend on the machine (UTC unless `TimeZone` is set,
invariant culture, LF line endings), so it can be compared in tests.

### Options

| `MailHtmlOptions` | Default | Effect |
|---|---|---|
| `SanitizeHtml` | on | Removes `script`, `iframe`, `object`, `embed`, `applet`, `base`, meta refresh, event handler attributes, `javascript:`, `vbscript:` and `data:` URLs (other than images), CSS `expression()`, `behavior` and `@import`. |
| `EmbedInlineImages` | on | `cid:` references, and an attachment's Content-Location, become `data:` URIs. |
| `MaxInlineImageBytes` | 10 MiB | Larger images keep their `cid:` reference. |
| `IncludeHeader` | on | The From, Sent, To, Cc, Bcc, Subject and Attachments block. |
| `ListInlineAttachments` | off | List the attachments the body already shows. |
| `LinkifyPlainText` | off | Link http, https and www addresses in a plain text body. |
| `FullDocument` | on | Doctype, head and body. Off gives a fragment. |
| `DateFormat`, `TimeZone`, `ShowTimeZone`, `Culture` | Outlook format, UTC, on, invariant | The Sent line. |

!!! warning Sanitising is display hygiene
The sanitiser is a deny-list for display, not a security boundary for untrusted mail. Serve the
page from an origin that holds nothing, or in a sandboxed frame.
!!!

## Which body is used

The preview and the "best body" methods choose the same way, in one place:

1. The HTML body.
2. The RTF body. Encapsulated HTML in RTF comes back as the original HTML, as specified in
   MS-OXRTFEX. Other RTF is rendered with its character formatting, colours, fields, tables and
   PNG or JPEG pictures. An RTF that only wraps plain text defers to the plain text body.
3. The plain text body, escaped, with its whitespace preserved.

`GetPreferredBodyKind()` reports which one a message resolves to (`None`, `PlainText`, `Html`
or `Rtf`).

## Body fallbacks

`GetBestTextBody()` and `GetBestHtmlBody()` give a body whichever one the message stores:

```csharp
using XRay.Email.Msg;

var message = MapiMessage.Load(path, token);

string? text = message.GetBestTextBody(token);   // PR_BODY, else derived from HTML, else from RTF
string? html = message.GetBestHtmlBody(token);   // HTML, else the original HTML of an RTF, else from plain text
string? rtf  = message.GetRtfBody(token);        // the decompressed RTF itself (PR_RTF_COMPRESSED)

Console.WriteLine(message.GetPreferredBodyKind(token));
```

The `Body`, `HtmlBody` and `RtfBody` properties stay what the file stores. `RtfBody` is the RTF
itself, and `GetBestHtmlBody` and `ToHtml` are the converted forms.

The RTF converter handles `\ansicpg` and per-font `\fcharset` code pages, skips `\bin`, caps
group depth, and draws only PNG and JPEG pictures.
