---
name: publish-packages
description: How X-Ray packages are versioned and pushed to nuget.org by their Azure DevOps pipelines, and in what order - members first, then the X-Ray umbrella, which pins the newest published members. Use when releasing a package, when a package or the umbrella shows the wrong version on nuget.org, when the umbrella is missing a member, when editing a .devops pipeline, or when packing the umbrella locally.
---

# Publishing X-Ray packages

## How a member publishes

Each repository has an Azure DevOps pipeline under `.devops/` (x-ray-content:
`build-x-ray-nuget.yml`). The shape is the same everywhere:

1. `PowerShell@2` computes `targetVersion = yy.M.<BUILD_BUILDID % 65536>` (CalVer; the modulo
   keeps the last part within a 16-bit version component) and sets it with
   `##vso[task.setvariable variable=targetVersion;]`.
2. `UseDotNet@2` installs the .NET 10 SDK (`version: 10.x`).
3. `DotNetCoreCLI@2` restore, build with `/p:Version=$(targetVersion)`, pack with
   `versioningScheme: 'off'`, `nobuild: true`, `buildProperties: 'Version="$(targetVersion)"'`.
4. `NuGetCommand@2` push of `$(Build.ArtifactStagingDirectory)/**/*.nupkg` with
   `nuGetFeedType: external`, `publishFeedCredentials: 'nuget-curiosity-org'`.

Triggers are `main` (x-ray-office: `master`) with a path filter, so a commit that does not touch
the package does not publish a new version. Versions only go up; there are no prerelease
suffixes. Packages built in the same run share one version.

Gotchas:

- `packagesToPack` is a pattern input split on **semicolons only**; `projects` (restore/build)
  is one path per line. Build a `packList` variable joined with `;` for pack.
- Packing with `nobuild: true` reuses the build's output, so every MSBuild property that
  changes the package (version, member versions) must go to build **and** pack.
- A ProjectReference to another packable project becomes a package dependency at the version
  that project is packed with, so packages that reference each other must ship in one run.

## How the umbrella publishes (curiosity-ai/x-ray)

`.devops/build-x-ray-nuget.yml` adds one step before restore: `resolve-member-versions.ps1`
reads `src/XRay/Members.props`, asks
`https://api.nuget.org/v3-flatcontainer/<lowercased id>/index.json` for each member, takes the
newest stable version, and sets `memberArgs` (`-p:XRayContentVersion=26.9.5114 ...`) and
`memberProps` (`XRayContentVersion=26.9.5114;...`) for restore/build and pack.

- Unpublished member (404): warning, left out of this release. Tick `strictMembers` when
  queueing to fail instead.
- Pin a member: set a pipeline variable named after its property, e.g.
  `XRayOfficeVersion=26.9.1234`.
- No member resolved, or a lookup fails for another reason: the run fails.

## Order

1. Merge and let each changed member's pipeline publish.
2. Wait until nuget.org lists the new version (the flat container can lag the push by several
   minutes):

   ```sh
   curl -s https://api.nuget.org/v3-flatcontainer/x-ray.content/index.json | tail -5
   ```

3. Queue the umbrella pipeline. Its log prints `<id> -> <version>` per member; check the list
   before the push step.

The umbrella does not rebuild on member releases by itself. Re-run it whenever a member ships
a version consumers of `X-Ray` should get.

## Packing the umbrella locally

```sh
# newest published members (needs PowerShell 7: dotnet tool install --global PowerShell)
dotnet pack src/XRay -c Release -o artifacts $(pwsh .devops/resolve-member-versions.ps1)

# against unpublished builds: pack the member into a local feed first
dotnet pack <member>/src/XRay.Psd -c Release -p:PackageId=X-Ray.Psd -p:Version=0.1.0-local -o local-feed
dotnet restore src/XRay -p:XRayPsdVersion=0.1.0-local -p:XRayContentVersion=26.9.5114 \
  --source ./local-feed --source https://api.nuget.org/v3/index.json
dotnet pack src/XRay -c Release --no-restore -p:XRayPsdVersion=0.1.0-local -p:XRayContentVersion=26.9.5114 -o artifacts
```

`local-feed/` and `artifacts/` are gitignored. Inspect the result with
`unzip -p artifacts/X-Ray.*.nupkg X-Ray.nuspec`: the `<dependencies>` group must list every
member you passed, each with `include="All"`.

## Diagnosing

| Symptom | Cause |
|---|---|
| `warning XRAY001: no version for X-Ray.Foo` | Property not passed; the member is left out. |
| `error XRAY003` | No member has a version; nothing to pack. |
| `NU1101 Unable to find package X-Ray.Foo` | A version was passed that no configured source has; add the local feed or fix the version. |
| Umbrella on nuget.org lacks a new member | Umbrella pipeline ran before the member was indexed; re-run it. |
