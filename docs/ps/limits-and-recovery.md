---
label: Limits, recovery and cancellation
icon: shield-check
order: 3
tags: [postscript, limits, cancellation, errors]
---

# Limits, recovery and cancellation

PostScript is a programming language, so a hostile or broken file can loop forever or ask for
gigabytes. XPS is XML in a ZIP package, with its own failure modes. These controls are for a host
that converts files it does not trust.

## Cancellation

Every public conversion has an overload with a `CancellationToken` as its last parameter. The
interpreter, the decoders, the rasteriser, the PDF writer and the XPS renderer poll the token, per
row, record or batch of objects. A cancelled call throws `OperationCanceledException`. It is never
turned into a warning or a package exception, and `SuppressErrors` does not suppress it. See the
[family convention](/getting-started/cancellation-and-streaming).

What cannot be interrupted: the Type 1 font resource patterns in the interpreter (linear time but
not polled), the XML parser while it reads a single part, and the CLR's own zlib.

## Interpreter limits

`PsInterpreterLimits` bounds a PostScript job even when the caller passes no token. Set it on
`PdfSaveOptions.Limits` or `ImageSaveOptions.Limits`.

| Limit | Default | Bounds |
|---|---|---|
| `MaxOperators` | 400 million | Executed objects and procedure calls. A loop like `{} loop` reaches it in a few seconds. |
| `MaxOperandStackDepth` | 500,000 | The operand stack. |
| `MaxExecutionDepth` | 985 | Procedure call nesting. This is the recursion limit of the original library, so the same programs fail the same way. |
| `MaxNestingDepth` | 10,000 | Nesting of `{ }` in the source. |
| `MaxMemoryBytes` | 1 GiB | Each single allocation the job asks for, and growth of the managed heap since it started. `null` disables both. |
| `MaxSuppressedErrors` | 1,000 | Errors that `SuppressErrors` absorbs before the job stops. |

`PsInterpreterLimits.Default` and `PsInterpreterLimits.Unlimited` are provided. Exceeding a limit
throws `PsResourceLimitError`, which PostScript's `stopped` operator does not catch, so a program
cannot hide a runaway loop behind it. For rasterisation, `MaxMemoryBytes` also caps the page buffer;
without `Limits` that cap is 4 GiB.

## Recovering from errors

By default the first error throws. With `SuppressErrors = true`, conversion keeps what it can and
records a `ConversionWarning` for each recovered error in the options' `Warnings` list.

- **PostScript**: the page being drawn is abandoned but kept as drawn so far, source objects are
  skipped up to the next `showpage`, that `showpage` runs, and interpretation continues. An
  exceeded limit ends the job and keeps the pages produced so far.
- **XPS**: an element that fails is skipped and its half-drawn commands are rolled back. A page
  whose XML cannot be read is skipped.

```csharp
using XRay.PS;
using XRay.PS.PostScript;
using XRay.PS.Xps;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
var options = new PdfSaveOptions
{
    JpegQuality = 75,          // re-encode raw 8-bit gray/RGB images as JPEG when that is smaller
    SuppressErrors = true,     // keep what was drawn, record a warning, go on after the next showpage
    Limits = new PsInterpreterLimits { MaxOperators = 50_000_000 },
};

using var file = File.Create("drawing.pdf");
PsDocument.FromFile("drawing.ps", timeout.Token).ToPdf(file, options, timeout.Token);
foreach (var warning in options.Warnings)
{
    Console.WriteLine($"{warning.Source} {warning.Code}: {warning.Message}");
}

// XPS: the same, with an optional page limit.
XpsDocument.FromFile("report.xps", timeout.Token).ToPdf(file, options, maxPages: 50, timeout.Token);
```

`ConversionWarning` is a record with `Source`, `Code`, `Message` and an optional `PageNumber`.
`Warnings` is appended to and never cleared, so use a fresh options object for each document.

## Which exceptions to expect

Loading and conversion may throw only `PsError` subclasses (`PsResourceLimitError` is one),
`XpsFormatException`, `HostErrorException`, argument and invalid-operation errors, and
`OperationCanceledException`. A truncation and mutation test suite over PostScript, XPS and image
inputs guards this.
