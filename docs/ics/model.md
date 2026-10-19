---
label: Reading and the model
icon: calendar
order: 2
tags: [ics, model, time zones, attendees]
---

# Reading and the model

## Loading

`Calendar` loads from a path, a stream, a byte array or a string. Every loader takes optional
[`IcsParseOptions`](/ics/writing-and-limits#limits) and a `CancellationToken`.

```csharp
using XRay.Ics;

var fromFile   = Calendar.LoadFile("team.ics");
var fromStream = Calendar.Load(File.OpenRead("team.ics"));
var fromBytes  = Calendar.Load(bytes);
var fromText   = Calendar.Parse("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nEND:VCALENDAR\r\n");

// Several VCALENDAR objects in one input are merged into the first by Load;
// LoadAll keeps them apart.
IReadOnlyList<Calendar> all = Calendar.LoadAll(File.OpenRead("feed.ics"));
```

The reader is tolerant:

- CRLF, LF and CR line ends can mix in one file; lines fold with a space or a tab.
- Quoted parameter values and RFC 6868 carets, TEXT unescaping, `X-` properties and
  vCard-style groups are kept.
- Encodings are detected: UTF-8 with or without a BOM, UTF-16 and UTF-32, and legacy bytes
  through Windows-1252 or the code page a line names with `CHARSET=`.
- Damaged input is read as far as it goes (unclosed components, a stray `END`, garbage lines,
  truncation). What was skipped is listed in `Calendar.Warnings`.

Only input that is not a calendar at all, or that exceeds a limit, raises `IcsFormatException`.

vCalendar 1.0 is read too: `ENCODING=QUOTED-PRINTABLE` with soft line breaks, `CHARSET=` on any
line, bare parameters, `TZ`, the `DALARM`, `AALARM`, `PALARM` and `MALARM` alarms, and the
`D1 #5`, `W1 MO WE`, `MD1 1 15` style of recurrence rule. `Calendar.IsVCalendar1` tells which
version was read.

## The typed model

`Calendar` exposes `Events`, `Todos`, `Journals`, `FreeBusies` and `TimeZones`, plus the
calendar-level `Version`, `ProductId`, `Method`, `Name` and `Description`. `Calendar.Primary` is
the first event, else the first to-do, else the first journal entry.

Components derive from `CalendarComponent`:

| Member | Content |
|---|---|
| `Uid`, `Summary`, `Description`, `HtmlDescription` | Text. `HtmlDescription` is Outlook's `X-ALT-DESC` HTML body when there is one. |
| `Start`, `Created`, `LastModified`, `Stamp`, `RecurrenceId` | `IcsDateTime` values. |
| `Status`, `Classification`, `Sequence`, `Url`, `Comment`, `Categories` | Scalars and lists. |
| `Organizer`, `Attendees`, `RequiredAttendees`, `OptionalAttendees`, `NonParticipants`, `Chairs`, `Resources` | `CalendarAddress` values, split by role and user type. |
| `Attachments` | `CalendarAttachment` values. |
| `RecurrenceRules`, `RecurrenceDates`, `ExceptionDates`, `IsRecurring` | The recurrence set, see [Recurrence](/ics/recurrence). |

`CalendarEvent` adds `Location`, `Geo`, `End`, `Duration`, `IsAllDay`, `Priority`,
`Transparency` and `Alarms`.

The typed model is a view over the raw tree. `Calendar.Raw` and each component's `Raw` hold every
property and parameter as read, setters write to `Raw`, and getters parse on demand. A load
followed by a save therefore loses nothing, including properties the typed model has no name
for. Typed accessors never throw: a value that cannot be read is `null`. The wire form of a
value, with TEXT escapes in place, is `IcsProperty.Value`; use `TextValue` for the unescaped
text.

## Times and time zones

`IcsDateTime` has a `Kind`: `Date` (all-day), `Floating`, `Utc` or `Zoned`.

- `Value` is the wall-clock reading as written.
- `Offset` is the UTC offset when it can be known: UTC, and zones the calendar's own `VTIMEZONE`
  rules or the system can resolve.
- `ToDateTimeOffset(floatingZone)` reads floating times and dates in the zone you choose, UTC
  by default. `ToLocalTime(zone)` converts.
- A zone nothing can resolve keeps its wall clock with `Offset == null` rather than a guess.

A `TZID` resolves through the calendar's `VTIMEZONE` rules first, then through IANA ids,
Windows zone names and Mozilla-style ids by way of `TimeZoneInfo`. Windows zone names come from a
built-in table of the common ones, not from CLDR data. `X-WR-TIMEZONE` is exposed as
`Calendar.DefaultTimeZoneId` but is not applied to floating times.

## Attendees

`CalendarAddress` carries `CommonName`, `Email` (a `mailto:` prefix removed, and the `EMAIL`
parameter used when the value is not an address), `Role`, `Status` (participation status),
`Rsvp`, `UserType`, `SentBy`, `Directory`, and the delegation and membership lists.
`Address` is the e-mail address or the stripped URI, and `DisplayName` falls back to it when
there is no common name.

## Attachments

`CalendarAttachment` covers inline base64 (`FILENAME`, `X-FILENAME`, `FMTTYPE`), `data:` URIs
and URI references.

```csharp
using XRay.Ics;

var appointment = IcsExtractor.ExtractFile("meeting.ics");
foreach (var attachment in appointment.Attachments.Where(a => !string.IsNullOrWhiteSpace(a.Name)))
{
    if (!attachment.HasData)
    {
        Console.WriteLine($"{attachment.Name}: reference to {attachment.Uri}");   // IsInline is false
        continue;
    }

    await using var output = File.Create(Path.Combine(folder, attachment.Name!));
    attachment.WriteTo(output, token);          // decodes block by block, no second copy
}
```

`GetBytes()` returns the decoded content and `Open()` a read-only stream over it. `Size` is
`null` for a reference, and `DeclaredSize` is the `SIZE` parameter when the file states one.
A damaged base64 payload decodes as far as it is valid.
