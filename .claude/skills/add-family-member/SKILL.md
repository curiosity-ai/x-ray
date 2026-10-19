---
name: add-family-member
description: Create a new X-Ray package and wire it into the family - project layout, csproj metadata, licence, CalVer pipeline, the umbrella's Members.props entry, README rows and a docs section. Use when adding a package to any X-Ray repository (x-ray-content, x-ray-office, x-ray-others), when splitting an optional add-on out of an existing package, or when a published package is missing from the X-Ray umbrella.
---

# Add a package to the X-Ray family

A new package touches two repositories: the one it lives in, and `curiosity-ai/x-ray` (umbrella,
READMEs, docs). Do the member side first; the umbrella only picks up what is on nuget.org.

## Checklist

A member is not finished until every item is done. The last two are the ones that get forgotten.

- [ ] Name chosen and `nuget.org/packages/<id>` checked free (section 1).
- [ ] Project, csproj metadata, licence, README (sections 2 and 3).
- [ ] A pipeline in the member's repository that publishes to nuget.org (section 4).
- [ ] `src/XRay/Members.props` line, README rows, `XRay.csproj` description and tags (section 5).
- [ ] **Add the docs section** (`x-ray/<name>/` in curiosity-ai/documentation, section 6): an overview and topic pages, the card, the
      installation and choosing rows, the header link. Every code sample compiled against the
      package's real API.
- [ ] **Publish in order**: the member's pipeline first, then the umbrella (section 7).

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

A family member is only a member once its pipeline **publishes to nuget.org**: the umbrella resolves
versions from nuget.org, so a package that exists only as a project in a repository, or only on a
private feed, is not installable through `X-Ray` and the resolver skips it.

Each repository has the pipeline that publishes its packages, and a new package goes into it:

| Repository | Pipeline | To add a package |
|---|---|---|
| `x-ray-others` | one for the whole repository, `.devops/build-x-ray-others-nuget.yml`, all packages at one version | Add the project to the variables, the restore and build lists and `packList`, and a path filter for its folder. |
| `x-ray-content` | `.devops/build-x-ray-nuget.yml` | Add the project. |
| `x-ray-office` | `.devops/build-x-ray-office-nuget.yml`, the whole set at one version | Add the library to its lists and to the facade's references. |

For a repository with no pipeline yet, copy `curiosity-ai/x-ray-content/.devops/build-x-ray-nuget.yml`:
`windows-latest`, a PowerShell step computing CalVer `yy.M.<buildId % 65536>` into `targetVersion`,
`UseDotNet@2` 10.x, restore/build/pack with `/p:Version=$(targetVersion)`, and one
`NuGetCommand@2` push of `**/*.nupkg` with `publishFeedCredentials: 'nuget-curiosity-org'` (the shape
curiosity-ai/sentence-transformers-sharp uses). Three things that failed once and are easy to repeat:

- **No `NuGetToolInstaller` step, and no `.snupkg` symbol packages.** The installer put nuget.exe 6.4
  on the path, which refuses the API-key service connection ("require NuGet 7.6 or later"); the push
  task's own nuget.exe accepts it. A symbol package beside each `.nupkg` is a second push, and in
  x-ray-others and x-ray-office the first package went out and the push then stopped. Set
  `IncludeSymbols` false and `DebugType` embedded.
- **Let the build write the packages** (`/p:GeneratePackageOnBuild=true`, push
  `**/bin/<config>/*.nupkg`) instead of a `pack` step over a project list: a semicolon-separated
  `packagesToPack` packed only the first project, so one package of eight or thirteen was pushed.
- **A path filter** so only the package's own folder, and the shared build files, trigger a run. A
  pipeline that serves a whole repository publishes every package at the new version, which is fine:
  ids do not collide, and a package whose code did not change just gets a new number.

## 5. Register it with the umbrella (curiosity-ai/x-ray)

1. `src/XRay/Members.props`: add one line, with a property name derived from the id:

   ```xml
   <XRayMember Include="X-Ray.Djvu" Version="$(XRayDjvuVersion)" />
   ```

   Nothing else in the build or pipeline changes; the resolver reads this file.
2. If the licence is new to the family, extend `PackageLicenseExpression` in
   `src/XRay/XRay.csproj` and the licence lists in `README.md`, `src/XRay/README.md` and
   `x-ray/getting-started/installation.md` in curiosity-ai/documentation.
3. Add the package to the tables in `README.md` and `src/XRay/README.md`, and to the
   `Description` and `PackageTags` of `XRay.csproj`.
4. Update `CLAUDE.md`'s repository map if it is in a new repository.

## 6. Docs section (curiosity-ai/documentation, `x-ray/`)

The docs live in the `x-ray/` folder of curiosity-ai/documentation, not here. Open a PR there:

- `x-ray/<short-name>/index.yml` (`label: X-Ray.Djvu`, `icon`, `order`) and `index.md` with an
  overview, the install command, a quick start from the real API, and the licence. Use a
  `!!! warning Stub` block for anything you could not verify.
- Add a card to `x-ray/index.md`, rows to `x-ray/getting-started/installation.md` (install list,
  namespace table, native dependencies, licences) and `choosing-a-package.md`.
- Links carry the `/x-ray` prefix. Build and check from the documentation repository root:
  `Neko check-links -i .`. See the `docs-site` skill.

## 7. Publish

The order is fixed: **members first, then the umbrella.**

1. The member's pipeline runs on its repository's `main` (or `master`) and pushes the package to
   nuget.org. A pipeline with a path filter runs only when its own folder changed.
2. Wait until nuget.org lists the package. Verify with
   `curl -s https://api.nuget.org/v3-flatcontainer/x-ray.djvu/index.json`. The listing can lag the
   push by minutes.
3. Then run the umbrella pipeline so `X-Ray` depends on it. The resolver takes the newest *stable*
   version listed for each id.

Running the umbrella first is not an error, only a missed member: an id with no published version
is left out of the package with warning `XRAY001`, and the next umbrella run after the member ships
picks it up. Tick `strictMembers` (or pass `-p:XRayRequireAllMembers=true`) when a release must
contain every member. A docs-only change to the member's page can be published at any time: the
docs are built by curiosity-ai/documentation, independent of both. See `publish-packages`.

## Lessons from adding X-Ray.Ics

- The property in `Members.props` is `XRay` plus the id without hyphens and dots, then `Version`
  (`X-Ray.PS.Skia` is `XRayPSSkiaVersion`). The resolver reads the file, so a typo is a silently
  unresolved member, not an error.
- A package can have a licence the umbrella's expression already covers (X-Ray.Ics is MIT) or
  one it does not. Check the member's csproj, not only its `Directory.Build.props`: X-Ray.PS
  overrides the shared MIT with `MIT AND OFL-1.1` for the fonts it bundles.
- Write the docs from the source and compile the samples. Keep a scratch console project outside
  the repository that references the member's projects by path, extract the fenced `csharp`
  blocks of the new pages into it, and build. A sample that does not compile is a page that
  lies.
- Docs link to each other root-relative, extensionless and with the `/x-ray` prefix; check them
  with `Neko check-links -i .` in curiosity-ai/documentation, and grep the built page for the `id` of
  a heading you link to when the checker skips anchors.
- Do not write a vendor's name that a licence forbids you to repeat; see the rule in `CLAUDE.md`.

