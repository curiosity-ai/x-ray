---
label: Extraction and renderings
icon: file-export
order: 4
tags: [ics, extraction, html, markdown]
---

# Extraction and renderings

`IcsExtractor` answers the question a search indexer asks of an invitation: who sent it, who
is invited, what is it about, and what is attached. It works on the first component of the
calendar (`Calendar.Primary`).

```csharp
using XRay.Ics;

IcsExtractResult appointment = IcsExtractor.ExtractFile("meeting.ics");
// Also: IcsExtractor.Extract(Stream | byte[] | string, options, token)
```

## What `IcsExtractResult` holds

| Member | Content |
|---|---|
| `Summary`, `Description`, `HtmlDescription`, `Location` | The subject, body and place. |
| `CreatedDate` | `CREATED`, else `DTSTAMP`. |
| `Start`, `End` | `IcsDateTime` values. |
| `Organizer` | A `CalendarAddress`, or `null`. |
| `Attendees`, `OptionalAttendees`, `Resources` | Required and optional participants, and rooms or equipment. |
| `Attachments` | `CalendarAttachment` values. Inline ones carry data; references have a `Uri`. |
| `Method` | The iTIP method (`Request`, `Cancel` and so on). |
| `Calendar`, `Component` | The whole parsed calendar and the component that was extracted. |

## Renderings

The result renders the whole calendar three ways:

```csharp
using XRay.Ics;

var appointment = IcsExtractor.ExtractFile("meeting.ics");

string html     = appointment.ToHtml();
string text     = appointment.ToPlainText();
string markdown = appointment.ToMarkdown();

// Stream the HTML instead of building a string.
using var page = File.Create("meeting.html");
appointment.ToHtml(page, new IcsRenderOptions { DisplayTimeZone = TimeZoneInfo.Utc }, token);
```

`ToHtml` writes one self-contained page: the subject, a table (When, with the recurrence
summarised, Where, Organizer, Required, Optional, Resources, Categories), the description with
its line breaks kept and its markup escaped, and the attachment list. It has no script, no
external references and no timestamps, and it does not depend on the machine, so output can be
compared in tests. The page is UTF-8 and English.

Plain text and Markdown cover every component of the calendar, while the HTML page covers the
first one, unless `IcsRenderOptions.AllComponents` says otherwise.

| `IcsRenderOptions` | Default | Effect |
|---|---|---|
| `DisplayTimeZone` | `null` | Convert times to this zone and label them with its offset. Without it each time keeps its own zone. |
| `FloatingTimeZone` | `null` (UTC) | The zone in which floating times and unresolved zones are read when converting to `DisplayTimeZone`. |
| `AllComponents` | `null` | `true` renders every event, to-do and journal entry; `false` only the first. Left `null`, HTML shows the first and plain text and Markdown show all. |
| `IncludeAttachments` | `true` | List attachments. |
| `Culture` | invariant | Culture for day and month names. The invariant default is English and keeps output deterministic. |

Options for the extractor itself are `IcsExtractOptions`, with a `Parse` property
([`IcsParseOptions`](/ics/writing-and-limits#limits)) and a `Render` property that become the
defaults of the result's `To*` methods.

## X-Ray.Content

The other X-Ray packages do not read `.ics`. Calling `IcsExtractor` yourself and indexing
`ToPlainText()` or `ToMarkdown()` is the route for calendar files in a pipeline that otherwise
uses [X-Ray.Content](/content/).
