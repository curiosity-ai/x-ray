---
label: Recurrence
icon: refresh
order: 3
tags: [ics, rrule, recurrence]
---

# Recurrence

## Rules

`RecurrenceRule` parses an `RRULE` and holds its parts: `Frequency`, `Interval`, `Count`,
`Until`, `BySecond`, `ByMinute`, `ByHour`, `ByDay` (with ordinals), `ByMonthDay`, `ByYearDay`,
`ByWeekNo`, `ByMonth`, `BySetPos` and `WeekStart`. Frequencies run from `SECONDLY` to `YEARLY`.

```csharp
using XRay.Ics;

var rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=2TU;COUNT=6");
Console.WriteLine(rule.Frequency);   // Monthly
Console.WriteLine(rule);             // the RRULE text
Console.WriteLine(rule.Describe());  // an English summary

// TryParse does not throw on a rule it cannot read.
if (!RecurrenceRule.TryParse("FREQ=SOMETIMES", out _))
{
    Console.WriteLine("not a rule");
}

// The wall-clock instants of a rule, from a start (UNTIL and COUNT end the sequence).
foreach (var instant in rule.Enumerate(new DateTime(2026, 1, 1, 9, 0, 0)))
{
    Console.WriteLine(instant);
}
```

`Enumerate` produces the instants that match the rule. By RFC 5545 a recurrence set always
includes `DTSTART` as its first instance, so `Enumerate` alone can omit it when the start does not
match the rule; use `Expand` to get the full set.

## Expanding events

`CalendarEvent.Expand(from, to, limit, floatingZone, cancellationToken)` returns the instances
that overlap `[from, to)`. It combines the rules with `RDATE` and `EXDATE` and keeps daylight
saving time, so a 09:00 meeting stays at 09:00 wall-clock across a change.

`Calendar.Expand` does the same for every event and also applies `RECURRENCE-ID` overrides and
cancellations. Each `IcsOccurrence` carries the `Event` it came from, its `Start` and `End`, and
`IsOverride`.

```csharp
using XRay.Ics;

var calendar = Calendar.LoadFile("team.ics");

var from = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
var to   = from.AddMonths(2);

foreach (var occurrence in calendar.Expand(from, to, limit: 500, floatingZone: TimeZoneInfo.Utc, cancellationToken: token))
{
    var marker = occurrence.IsOverride ? " (changed)" : "";
    Console.WriteLine($"{occurrence.Start}  {occurrence.Event.Summary}{marker}");
}
```

Floating times and dates are read in `floatingZone`, UTC by default.

## Where expansion stops

Every recurrence loop terminates, so a rule that never matches cannot hang a caller. Expansion
ends at `COUNT`, at `UNTIL`, at year 9999, or after a million periods, and it returns no more
than `limit` instances.

## Known gaps

- A `VALUE=PERIOD` `RDATE` contributes only its start.
- Overrides with `RANGE=THISANDFUTURE` are not applied.
- `VAVAILABILITY`, `VPOLL` and the other components of RFC 7953 and RFC 9073 stay in the raw
  tree and have no typed view.
