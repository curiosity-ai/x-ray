---
label: Overview
icon: rocket
order: 1
---

# Get started

Every X-Ray package is a NuGet package targeting `net10.0`. Install the umbrella to get the
whole family, or install the one member you need.

```sh
dotnet add package X-Ray
```

:::: card-grid
::: card {variant="grid" icon="download" title="Installation" link="/getting-started/installation"}
The umbrella package and the individual members, requirements, and which packages bring native code.
:::
::: card {variant="grid" icon="list-check" title="Choose a package" link="/getting-started/choosing-a-package"}
Which package to use for a task or a file type, and when to combine two.
:::
::: card {variant="grid" icon="hourglass-end" title="Cancellation and streaming" link="/getting-started/cancellation-and-streaming"}
How every package takes a `CancellationToken`, reports cancellation and writes large results to streams.
:::
::::

## A first extraction

X-Ray.Content reads the widest range of formats, so it is the usual starting point. This reads
a document and prints it as Markdown:

```csharp
using XRay.Content.Core;

var result = new Extractor().Extract(
    ExtractInput.FromUri("quarterly-report.pdf"),
    new ExtractionConfig { OutputFormat = OutputFormat.Markdown });

var doc = result.Results[0];
Console.WriteLine(doc.MimeType);   // application/pdf
Console.WriteLine(doc.Content);    // the document as Markdown
```

The [X-Ray.Content](/content/) section covers output formats, metadata, tables and the
optional OCR pass. Each of the other packages has a section of its own with a quick start.
