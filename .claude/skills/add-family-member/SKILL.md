---
name: add-family-member
description: Create a new X-Ray package and wire it into the family - project layout, csproj metadata, licence, CalVer pipeline, the umbrella's Members.props entry, README rows and a docs section. Use when adding a package to any X-Ray repository (x-ray-content, x-ray-office, x-ray-others), when splitting an optional add-on out of an existing package, or when a published package is missing from the X-Ray umbrella.
---

# Add a package to the X-Ray family

A new package touches two repositories: the one it lives in, and `curiosity-ai/x-ray` (umbrella,
READMEs, docs). Do the member side first; the umbrella only picks up what is on nuget.org.

## 1. Choose names and a home

| What | Rule | Example |
|---|---|---|
| Package id | `X-Ray.<Name>`, hyphenated | `X-Ray.Djvu` |
| Project, assembly, root namespace, folder | `XRay.<Name>`, no hyphen | `XRay.Djvu` |
| Optional add-on with a native dependency | its own package, `X-Ray.<Name>.<Extra>` | `X-Ray.Psd.Skia` |

Home: a single-format reader goes in `curiosity-ai/x-ray-others` as a new top-level folder; a
part of extraction goes in `x-ray-content`; an office format in `x-ray-office`. Check
`nuget.org/packages/<id>` is free before committing to the name.

## 2. Project layout (x-ray-others shape)

```
XRay.<Name>/
  Directory.Build.props        imports ../Directory.Build.props, adds package-specific settings
  XRay.<Name>.slnx
  src/XRay.<Name>/XRay.<Name>.csproj
  src/XRay.<Name>/README.md    the NuGet README
  tests/XRay.<Name>.Tests/
  LICENSE / NOTICE             upstream's, when the code is a port
  CLAUDE.md                    architecture and rules for this package
```

The repository root's `Directory.Build.props` already sets `net10.0`, `Authors`/`Company`
Curiosity GmbH, `RepositoryUrl`, symbols, and `Directory.Build.targets` packs `assets/logo.png`
for every project with `IsPackable=true`. If your folder adds its own `Directory.Build.targets`,
import the root one from it.

## 3. The csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>XRay.Djvu</RootNamespace>
    <AssemblyName>XRay.Djvu</AssemblyName>
    <PackageId>X-Ray.Djvu</PackageId>
    <IsPackable>true</IsPackable>
    <Description>Reads DjVu documents: pages, text layer, and rendering to images. Part of the X-Ray family.</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageTags>x-ray;djvu;document</PackageTags>
  </PropertyGroup>
  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="/" />
  </ItemGroup>
</Project>
```

- **Licence**: the expression must cover any derived code (`MIT AND Apache-2.0` if some files
  derive from Apache-2.0 code). Keep the upstream `LICENSE`/`NOTICE` in the package folder and
  pack it if the licence requires distribution with binaries.
- **Pure managed core.** If the package needs SkiaSharp/HarfBuzz/PDFium, split that code into
  `X-Ray.<Name>.<Extra>` or put it behind an opt-in option that is off by default.
- **README**: link the logo as
  `https://raw.githubusercontent.com/curiosity-ai/<repo>/<branch>/assets/logo.png`; no raw HTML,
  no relative images (nuget.org renders neither). Include install, a quick start from the real
  API, and the licence.
- Ported from a vendor-branded open-source library? Do not carry the vendor's name into
  anything you write; call it "the MIT-licensed library it was ported from".

## 4. The pipeline

Add the project to the repository's `.devops/*.yml` NuGet pipeline, or copy
`curiosity-ai/x-ray-content/.devops/build-x-ray-nuget.yml` for a new one: `windows-latest`,
a PowerShell step computing CalVer `yy.M.<buildId % 65536>` into `targetVersion`,
`UseDotNet@2` 10.x, restore/build/pack with `/p:Version=$(targetVersion)`, and
`NuGetCommand@2` push with `publishFeedCredentials: 'nuget-curiosity-org'`. Give it a path filter
so only this package's folder triggers it. `packagesToPack` is split on semicolons only; a
newline-separated list matches nothing.

## 5. Register it with the umbrella (curiosity-ai/x-ray)

1. `src/XRay/Members.props`: add one line, with a property name derived from the id:

   ```xml
   <XRayMember Include="X-Ray.Djvu" Version="$(XRayDjvuVersion)" />
   ```

   Nothing else in the build or pipeline changes; the resolver reads this file.
2. If the licence is new to the family, extend `PackageLicenseExpression` in
   `src/XRay/XRay.csproj` and the licence lists in `README.md`, `src/XRay/README.md` and
   `docs/getting-started/installation.md`.
3. Add the package to the tables in `README.md` and `src/XRay/README.md`, and to the
   `Description` and `PackageTags` of `XRay.csproj`.
4. Update `CLAUDE.md`'s repository map if it is in a new repository.

## 6. Docs section (curiosity-ai/x-ray `docs/`)

- `docs/<short-name>/index.yml` (`label: X-Ray.Djvu`, `icon`, `order`) and `index.md` with an
  overview, the install command, a quick start from the real API, and the licence. Use a
  `!!! warning Stub` block for anything you could not verify.
- Add a card to `docs/index.md`, rows to `docs/getting-started/installation.md` (install list,
  namespace table, native dependencies, licences) and `choosing-a-package.md`, and a header link
  under "Packages" in `docs/neko.yml`.
- Build and check: `./build.sh && Neko check-links -i docs`. See the `docs-site` skill.

## 7. Publish

Member pipeline first. Once nuget.org lists the package, run the umbrella pipeline so `X-Ray`
depends on it. Verify with
`curl -s https://api.nuget.org/v3-flatcontainer/x-ray.djvu/index.json`. See `publish-packages`.
