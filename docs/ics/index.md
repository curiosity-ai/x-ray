---
label: Overview
icon: calendar
order: 1
tags: [ics, icalendar, vcalendar, calendar, recurrence]
---

# X-Ray.Ics

X-Ray.Ics reads, extracts, expands and writes iCalendar files (`.ics`, RFC 5545, with the iTIP
basics of RFC 5546) and vCalendar 1.0 files. It has typed events, to-dos, journal entries and
free/busy components, resolves time zones, expands recurrence rules, writes calendars back
out, and turns an appointment into HTML, plain text or Markdown. The package has no
dependencies outside the .NET base class library and needs no calendar application.

It was written from the specifications and from the shape of real exports, not ported from
another library. Its expectations are checked against Python's `icalendar` and `dateutil` on a
fixture corpus of several hundred files.

[!ref icon="brands-github" target="blank" text="curiosity-ai/x-ray-others"](https://github.com/curiosity-ai/x-ray-others)
[!ref icon="box-open" target="blank" text="X-Ray.Ics on NuGet"](https://www.nuget.org/packages/X-Ray.Ics/)

## Install

```sh
dotnet add package X-Ray.Ics
```

The namespace is `XRay.Ics`.

## Quick start

Read what an indexer needs from an appointment:

```csharp
using XRay.Ics;

IcsExtractResult appointment = IcsExtractor.ExtractFile("meeting.ics");

Console.WriteLine(appointment.Summary);
Console.WriteLine(appointment.CreatedDate);                       // CREATED, else DTSTAMP
Console.WriteLine($"{appointment.Organizer?.CommonName} <{appointment.Organizer?.Address}>");
foreach (var person in appointment.Attendees)         Console.WriteLine($"  to: {person}");
foreach (var person in appointment.OptionalAttendees) Console.WriteLine($"  cc: {person}");
Console.WriteLine(appointment.Description);

string html = appointment.ToHtml();                                // the meeting view
string text = appointment.ToPlainText();
string markdown = appointment.ToMarkdown();
```

Work with the calendar itself, including recurring events:

```csharp
using XRay.Ics;

var calendar = Calendar.LoadFile("team.ics", cancellationToken: token);
foreach (var item in calendar.Events)
{
    Console.WriteLine($"{item.Summary}: {item.Start} - {item.End} ({item.Method})");
}

// Instances in a window, with RECURRENCE-ID overrides applied.
foreach (var occurrence in calendar.Expand(DateTimeOffset.Now, DateTimeOffset.Now.AddMonths(3), limit: 200))
{
    Console.WriteLine($"{occurrence.Start}  {occurrence.Event.Summary}");
}
```

## Pages in this section

:::: card-grid
::: card {variant="grid" icon="calendar" title="Reading and the model" link="/ics/model"}
The tolerant reader, typed components, times and time zones, attendees, attachments.
:::
::: card {variant="grid" icon="refresh" title="Recurrence" link="/ics/recurrence"}
`RecurrenceRule`, `Expand`, RDATE and EXDATE, overrides, and where expansion stops.
:::
::: card {variant="grid" icon="file-export" title="Extraction and renderings" link="/ics/extraction"}
`IcsExtractor`, the extracted fields, HTML, plain text and Markdown.
:::
::: card {variant="grid" icon="pencil" title="Writing, limits and cancellation" link="/ics/writing-and-limits"}
`Calendar.Save`, parse limits, cancellation, streams and compatibility notes.
:::
::::

## X-Ray.Ics or X-Ray.Content?

[X-Ray.Content](/content/) has no calendar extractor. Use X-Ray.Ics when you need an
appointment's organizer, attendees, recurrence or attachments, or when you must produce an
`.ics` file. A pipeline that indexes mail can use [X-Ray.Email](/email/) for the message and
X-Ray.Ics for an attached invitation.

## License

MIT.
