---
label: Writing, limits and cancellation
icon: pencil
order: 5
tags: [ics, writer, limits, cancellation]
---

# Writing, limits and cancellation

## Writing a calendar

`Calendar.Save(Stream)` writes RFC 5545 text: CRLF line ends, UTF-8 without a BOM, lines folded at
75 octets without splitting a character, TEXT and parameter values escaped.

```csharp
using XRay.Ics;

var calendar = new Calendar { MethodText = "REQUEST" };
var meeting = new CalendarEvent
{
    Uid = Guid.NewGuid() + "@example.com",
    Summary = "Planning",
    Start = new IcsDateTime(new DateTime(2026, 5, 4, 9, 0, 0), IcsDateTimeKind.Utc),
    End = new IcsDateTime(new DateTime(2026, 5, 4, 10, 0, 0), IcsDateTimeKind.Utc),
    Organizer = new CalendarAddress("boss@example.com", "The Boss"),
};
meeting.AddAttendee(new CalendarAddress("ana@example.com", "Ana"));
meeting.SetRecurrence(RecurrenceRule.Parse("FREQ=WEEKLY;COUNT=4"));
calendar.Events.Add(meeting);

await using var output = File.Create("planning.ics");
calendar.Save(output);
```

`Save(string path)`, `ToBytes()` and `ToIcsString()` are thin wrappers over the stream form.
`CalendarAttachment.WriteTo(Stream)` and `IcsExtractResult.ToHtml(Stream)` likewise write
without buffering the whole output.

A calendar loaded from vCalendar 1.0 is saved with quoted-printable markers removed, since the
text is plain after loading, but it keeps `VERSION:1.0`. Convert deliberately if you need RFC 5545
output.

## Limits

`IcsParseOptions` bounds what a hostile file can ask for:

| Option | Default |
|---|---|
| `MaxInputBytes` | 256 MiB |
| `MaxLineLength` | 64 Mi characters (a logical line, after unfolding) |
| `MaxDepth` | 16 levels of component nesting; unknown deeper components are skipped |
| `MaxComponents` | 1,000,000 |
| `MaxPropertiesPerComponent` | 100,000 |
| `FallbackEncoding` | the encoding used for bytes that are not UTF-8 |

Exceeding a limit raises `IcsFormatException`. Recurrence expansion has its own ceilings, listed
under [Recurrence](/ics/recurrence#where-expansion-stops).

Loading, expanding, rendering and saving raise only `IcsFormatException` (no calendar at all, or a
limit exceeded) and `OperationCanceledException`. Any other exception is a bug in the library.

## Cancellation

Every public method that does real work takes a `CancellationToken` as its last parameter,
optional where it is not the only argument. The token is checked per 1,024 lines while reading,
per 256 periods and per instance while expanding, per component while rendering and saving, and
per 8 KiB block of base64. A cancelled call throws `OperationCanceledException`, never wrapped in
another exception. The [family convention](/getting-started/cancellation-and-streaming) is the
same for every package.

```csharp
using XRay.Ics;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    var calendar = Calendar.LoadFile("huge.ics", cancellationToken: timeout.Token);
    foreach (var occurrence in calendar.Expand(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1), cancellationToken: timeout.Token))
    {
        Console.WriteLine(occurrence.Start);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("took too long");
}
```

## Compatibility notes

- `\"` is not an escape in iCalendar TEXT and is kept; `\:` is unescaped, as common producers
  write it.
- `ISO-8859-1` and `US-ASCII` charsets are read as Windows-1252, which is what such files mean.
- Several `VCALENDAR` objects in one input are merged into the first by `Calendar.Load`;
  `Calendar.LoadAll` keeps them apart.
- The rendering is English only.
