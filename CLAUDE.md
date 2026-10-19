# X-Ray: the family root

X-Ray is a family of pure C# .NET 10 NuGet packages from Curiosity GmbH for working with
documents: content extraction for search and LLMs, rendering to PDF and images, and in-depth
readers for specific file types. This repository (`curiosity-ai/x-ray`) is the family's root:
the `X-Ray` umbrella package and the notes and skills shared by every repository (the docs are in
`curiosity-ai/documentation`, see below). Read this file before working anywhere in the family; each member repository has
its own `CLAUDE.md` for its code, and that file wins inside that repository.

Keep this file under 15 KB. Procedures belong in `.claude/skills/<name>/SKILL.md`.

## Repository map

| Repository | Local clone (agent sessions) | Packages | What it is |
|---|---|---|---|
| `curiosity-ai/x-ray` | `/home/user/x-ray` | `X-Ray` | This repo: umbrella package, shared skills and notes. |
| `curiosity-ai/documentation` | `/home/user/documentation` | none | All of Curiosity's docs; the family's are in its `x-ray/` folder (see Documentation below). |
| `curiosity-ai/x-ray-content` | `/home/user/x-ray-content` | `X-Ray.Content` | Content extraction (text, Markdown, HTML, JSON, metadata, tables, images; 90+ formats). Native C# port of the Rust `xberg` engine; C# in `dotnet/`, Rust upstream verbatim in `.reference/`. |
| `curiosity-ai/x-ray-office` | `/home/user/x-ray-office` | `X-Ray.Office`, `X-Ray.Office.*` | C# port of LibreOffice's Writer/Calc/Impress readers and layout, rendering doc/docx/xls/xlsx/ppt/pptx/odt/ods/odp/rtf to PDF and images. C# in `dotnet/`; the surrounding LibreOffice tree is reference only, never built. Formerly "Paperless". Default branch `master`. |
| `curiosity-ai/x-ray-others` | `/home/user/x-ray-others` | `X-Ray.Psd` (+ `.Skia`, `.Text`), `X-Ray.Email`, `X-Ray.Ics`, `X-Ray.PS` (+ `.Skia`), `X-Ray.OneNote` | Single-format packages, one top-level folder each, sharing one build skeleton. |
| `curiosity-ai/test_documents` | `/home/user/test_documents` | none | Shared fixture corpus: fork of `xberg-io/test_documents`, one folder per format, binaries in git. |

What each `x-ray-others` package is:

- `X-Ray.Psd`: PSD/PSB reading, compositing, text extraction; no dependencies. `X-Ray.Psd.Skia`
  (SkiaSharp interop) and `X-Ray.Psd.Text` (type-layer re-rendering, SkiaSharp + HarfBuzz) are
  optional add-ons. Ported from the Patchy image editor.
- `X-Ray.Email`: MSG/EML/CFB reading and writing, MSG to EML conversion, HTML preview, body
  fallbacks, attachments as files.
- `X-Ray.Ics`: iCalendar (RFC 5545) and vCalendar 1.0: reader, typed model, recurrence expansion,
  extraction of an appointment to HTML, text and Markdown, writer. Written from the specifications,
  not a port; no dependencies; MIT.
- `X-Ray.PS`: PostScript/EPS/XPS reading, conversion to PDF and images, interpreter limits.
  `X-Ray.PS.Skia` (SkiaSharp) is the optional raster writer that reproduces the original's output.
- `X-Ray.OneNote`: OneNote `.one` reading, text, PDF and HTML export, best-effort notebook table of
  contents.

`X-Ray.Office` is the facade of the `X-Ray.Office.*` libraries (Core, Containers, Text, Vector,
Rendering, Markup, Ooxml, OpenDocument, MsBinary, WordProcessing, Spreadsheets, Presentations); the
umbrella lists the facade only, which brings the rest.

Every package takes a `CancellationToken` last on public methods that do real work, throws
`OperationCanceledException` and nothing else when cancelled, and writes large outputs to a
`Stream`. The docs page `x-ray/getting-started/cancellation-and-streaming` (in `curiosity-ai/documentation`) states the convention.

Some members are ports of third-party open-source code (X-Ray.Ics is not: it was written from the RFCs). Credit an upstream the way its license
requires (keep its LICENSE and notices), but **do not name the vendor of the email, PS/XPS and
OneNote upstreams in anything you write** (code, docs, commits, package metadata). Describe them
as "the MIT-licensed library it was ported from".

## Naming

- **Package id**: hyphenated, `X-Ray.<Name>` (`X-Ray.Content`, `X-Ray.Psd.Skia`).
- **Assembly, root namespace, project and folder**: no hyphen, `XRay.<Name>` (`XRay.Content`),
  because a hyphen is not a valid C# identifier. In a csproj: `<PackageId>X-Ray.Psd</PackageId>`
  with the project named `XRay.Psd.csproj`.
- The family is "X-Ray" in prose. Never "Xray", "XRay" (except as a code identifier) or "X-ray".
- Abbreviations keep their case: `X-Ray.PS`, `XRay.PS`.

## Shared conventions

- **Target `net10.0`**, `LangVersion latest`, nullable and implicit usings on. Zero build
  warnings; fix the cause, and scope any suppression to one line or one code.
- **Pure managed core.** A package's default code path uses no P/Invoke and no native library.
  Native dependencies (SkiaSharp, HarfBuzzSharp, PDFium) go in a separate package
  (`X-Ray.Psd.Skia`) or behind an opt-in option that is off by default (X-Ray.Content's OCR),
  and the package README says which. Rendering-centric packages (X-Ray.Office) document their
  native dependencies up front.
- **Package metadata** on every packable project: `Authors`/`Company` Curiosity GmbH, the family
  icon `logo.png` packed at the package root (from `assets/logo.png`; the same image in every
  repository), a `README.md` packed and named by `PackageReadmeFile`, `RepositoryUrl`, and a
  `PackageLicenseExpression` that covers derived code. NuGet READMEs link images by absolute
  `raw.githubusercontent.com` URL; nuget.org renders neither relative image paths nor raw HTML.
- **Licences.** Keep every upstream `LICENSE`/`NOTICE` file next to the code derived from it,
  and keep the package's expression honest: X-Ray.Content is `MIT AND Apache-2.0` (a few files
  derive from Apache-2.0-only Rust crates), X-Ray.Office is `MPL-2.0` (LibreOffice-derived),
  the x-ray-others packages are `MIT`. A new dependency's licence must be compatible with the
  package's.
- **Reference sources stay pristine.** A port keeps the code it came from as read-only reference
  (`.reference/` in x-ray-content, the LibreOffice tree in x-ray-office). Never edit or build it.
- **Tests** use xUnit (v3 with Microsoft.Testing.Platform where set up). Fixtures come from
  `curiosity-ai/test_documents` (see below); never hardcode a machine path.
- **Commits**: short subject line describing the change; only verified, finished work.

## Versioning and publishing

- Every package is versioned **CalVer `yy.M.<buildId % 65536>`**, computed by its repository's
  Azure DevOps pipeline (`.devops/*.yml`, `windows-latest`, `UseDotNet@2` 10.x, one `NuGetCommand@2` glob push, no NuGetToolInstaller step),
  which packs and pushes to nuget.org through the **`nuget-curiosity-org`** service connection.
  x-ray-others and x-ray-office each publish all their packages from one pipeline at one version. Checked-in
  `<Version>` values (`0.1.0-alpha`) are placeholders for local builds.
- Pipelines trigger on `main` (or `master`) with a path filter, so unrelated changes do not
  publish.
- Order: **members first, umbrella last.** The umbrella pins whatever is on nuget.org when its
  pipeline runs. See the `publish-packages` skill.

## The umbrella package (`src/XRay`)

`X-Ray` contains no code; it depends on every member, so `dotnet add package X-Ray` installs the
family. Members live in other repositories, so they are **PackageReferences**, not
ProjectReferences.

- `src/XRay/Members.props` is **the only list of members**: one
  `<XRayMember Include="<id>" Version="$(<Property>)" />` per package. `XRay.csproj` turns it
  into PackageReferences (`PrivateAssets="none"`, so a member's assets flow as if installed
  directly).
- `.devops/resolve-member-versions.ps1` reads that list, asks
  `https://api.nuget.org/v3-flatcontainer/<lowercased id>/index.json` for each id, takes the
  newest stable version, and hands the pipeline `-p:<Property>=<version>` arguments. Locally it
  prints them: `dotnet pack src/XRay -c Release -o artifacts $(pwsh .devops/resolve-member-versions.ps1)`.
- **Unpublished members are skipped with a warning**, not failed: the umbrella ships while the
  family is still arriving, and the next run after a member's first release picks it up. The
  resolver warns per member, and the project warns again (`XRAY001`). To fail instead, run the
  pipeline with `strictMembers` ticked (resolver `-Strict`) or build with
  `-p:XRayRequireAllMembers=true` (`XRAY002`). Packing with no member at all is always an
  error (`XRAY003`). Any other lookup failure (network, bad response) fails the run.
- **Pinning one member**: set a pipeline variable named after its property
  (`XRayOfficeVersion=26.9.1234`); the resolver keeps it instead of looking it up.
- `IncludeBuildOutput=false` (no empty `XRay.dll`), `NU5128` suppressed (it is the warning for a
  package with dependencies and no `lib/`, which is what a metapackage is),
  `SuppressDependenciesWhenPacking=false` (the dependencies are the payload).
- Its licence expression covers what it installs: `MIT AND Apache-2.0 AND MPL-2.0 AND OFL-1.1` (no `Bitstream-Vera`: nuget.org rejects it, so the DejaVu and DjVu fonts' licence is documented in the packages instead). Update it if
  a member with a new licence joins.
- List a member's facade or root package (`X-Ray.Office`, which brings `X-Ray.Office.*`), plus
  optional add-ons a consumer would otherwise have to know about (`X-Ray.Psd.Skia`).

**Adding a family member** is one line in `Members.props`, a row in `README.md` and
`src/XRay/README.md`, a docs section (in `curiosity-ai/documentation`), and (if the licence is new) the expression. The
`add-family-member` skill has the whole procedure, including the member's own repository side.

## Test fixtures: `curiosity-ai/test_documents`

One folder per format (`docx/`, `pdf/`, `email/`, `ics/`, `psd/`, `ps/`, `xps/`, `onenote/`, ...).
Binaries are committed in git (no LFS). Third-party sets carry a `LICENSE` and a `README.md`
naming their source. Repositories consume it as a git submodule at `test_documents/`
(`git submodule update --init --depth 1 test_documents`); tests resolve the root from
`XRAY_TEST_DOCUMENTS` or by walking up to a directory named `test_documents`, and skip when it is
absent. X-Ray.Content's golden `*-results-rust.json` files are generated locally, never
committed. See the `test-documents` skill.

## Documentation: `curiosity-ai/documentation`, folder `x-ray/`

**The family's documentation is not in this repository.** It lives in the `x-ray/` folder of
[curiosity-ai/documentation](https://github.com/curiosity-ai/documentation) (clone:
`/home/user/documentation` in agent sessions), built and published with the rest of Curiosity's
docs at <https://docs.curiosity.ai/x-ray/>. No repository in the family builds or publishes docs.

- **A change to a package's public API, options, defaults, behavior, limits, licence, install
  instructions or supported formats is not finished until its page in that folder is updated.**
  Open the documentation PR against `curiosity-ai/documentation` (never against a source
  repository, and never against an upstream), link it from the code PR, and say so in the code PR
  if no page needs to change. Every `CLAUDE.md` in the family carries this rule.
- Sections: `getting-started/`, one folder per package (`content/`, `office/`, `psd/`, `email/`,
  `ics/`, `ps/`, `onenote/`), `contributing/`. A new family member needs a folder there too (see
  the `add-family-member` and `docs-site` skills).
- Links inside it are root-relative with the `/x-ray` prefix (`/x-ray/content/`); code samples
  use the package's real API; tone is reference docs, not marketing. The documentation
  repository's own `.claude/CLAUDE.md` has the Neko component reference and the tone rules.

## Layout of this repository

```
src/XRay/                 XRay.csproj, Members.props, README.md (the package README)
assets/logo.png           family icon
.devops/                  build-x-ray-nuget.yml, resolve-member-versions.ps1 (Azure DevOps, the umbrella)
Directory.Build.props     shared metadata and icon packing
.claude/skills/           add-family-member, publish-packages, test-documents, docs-site
```

Commands:

```sh
dotnet build src/XRay -c Release                       # warns XRAY001 per member without versions
dotnet pack src/XRay -c Release -o artifacts $(pwsh .devops/resolve-member-versions.ps1)
```
