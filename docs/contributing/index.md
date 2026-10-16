---
label: Development
icon: code-branch
order: 1
---

# Development

This page is for people working on the X-Ray packages themselves. Each repository has its own
`CLAUDE.md` with the architecture and rules for that code; this page covers what is shared.

## Repositories

| Repository | Contents | Packages |
|---|---|---|
| [curiosity-ai/x-ray](https://github.com/curiosity-ai/x-ray) | The umbrella package, these docs, shared development notes | `X-Ray` |
| [curiosity-ai/x-ray-content](https://github.com/curiosity-ai/x-ray-content) | Content extraction, a C# port of the Rust xberg engine (C# under `dotnet/`) | `X-Ray.Content` |
| [curiosity-ai/x-ray-office](https://github.com/curiosity-ai/x-ray-office) | A C# port of LibreOffice's reading and layout, in `dotnet/` of a LibreOffice source tree | `X-Ray.Office`, `X-Ray.Office.*` |
| [curiosity-ai/x-ray-others](https://github.com/curiosity-ai/x-ray-others) | Single-format packages, one folder each | `X-Ray.Psd`, `X-Ray.Psd.Skia`, `X-Ray.Psd.Text`, `X-Ray.Email`, `X-Ray.PS`, `X-Ray.OneNote` |
| [curiosity-ai/test_documents](https://github.com/curiosity-ai/test_documents) | The shared fixture corpus, one folder per format | (none) |

Ports keep the code they were ported from as a read-only reference inside the repository
(`.reference/` in x-ray-content, the LibreOffice tree around `dotnet/` in x-ray-office), so a
behaviour question can be answered from the original source.

## Shared conventions

- **Target** `net10.0`, nullable enabled, zero build warnings.
- **Package ids are hyphenated** (`X-Ray.Content`), assemblies and namespaces are not
  (`XRay.Content`).
- **The core of a package is managed code.** A native dependency (SkiaSharp, HarfBuzzSharp,
  PDFium) goes in a separate package or behind an opt-in option, never into the default path.
- **Versions are CalVer**, `yy.M.<buildId % 65536>`, set by each repository's Azure DevOps
  pipeline, which packs and pushes to nuget.org through the `nuget-curiosity-org` service
  connection.
- **Every package carries the family metadata**: author Curiosity GmbH, the family icon, a
  README packed into the package, and a license expression that covers derived code.
- **Upstream licenses are kept.** A port keeps the original project's LICENSE and notices next
  to the code derived from it.

## Publishing order

Members publish first, each from its own pipeline. The umbrella publishes after them: its
pipeline asks nuget.org for the newest version of every member and pins those. Re-run the
umbrella's pipeline after a member ships to carry the new version into `X-Ray`.

## Adding a package to the family

1. Create the package in the repository it belongs to, with the family metadata above.
2. Add a CalVer pipeline for it, or add it to the repository's existing one.
3. Add one line to `src/XRay/Members.props` in curiosity-ai/x-ray.
4. Add a row to the package tables in the x-ray `README.md`, `src/XRay/README.md`, and these
   docs (the landing page and [Installation](/getting-started/installation)).
5. Add a section to these docs with an overview, the install command and a quick start.

## Test fixtures

Fixtures live in [curiosity-ai/test_documents](https://github.com/curiosity-ai/test_documents),
a fork of xberg's corpus with the X-Ray additions, consumed by each repository as a git
submodule at `test_documents/`. Third-party fixture sets keep their license file and a README
naming their source.

## These docs

See [Working on the docs](/contributing/docs).
