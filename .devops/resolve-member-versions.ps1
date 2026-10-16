<#
.SYNOPSIS
  Finds the newest published version of every X-Ray family member, for the umbrella package.

.DESCRIPTION
  Reads src/XRay/Members.props, the single list of members, where each line is

      <XRayMember Include="X-Ray.Content" Version="$(XRayContentVersion)" />

  For each member it asks nuget.org's flat container
  (https://api.nuget.org/v3-flatcontainer/<lowercased id>/index.json) for the published versions
  and takes the newest stable one (the last stable entry; the list is in SemVer order). The result
  is the MSBuild property the line names, set to that version.

  A member nuget.org does not know yet (HTTP 404) is skipped with a warning and left out of the
  umbrella; -Strict turns that into a failure. A member whose property is already set in the
  environment (a pipeline variable such as XRayContentVersion, set at queue time) keeps that value
  and is not looked up, which is how one member is pinned to an older version.

  On Azure Pipelines (TF_BUILD set) the script sets two pipeline variables:
    memberArgs   -p:XRayContentVersion=26.9.5114 -p:...   for restore/build `arguments`
    memberProps  XRayContentVersion=26.9.5114;...         for the pack task's `buildProperties`
  Anywhere else it prints the `-p:` arguments, so a local pack can use them:

      dotnet pack src/XRay -c Release $(pwsh .devops/resolve-member-versions.ps1)

  Any other failure (network error, unexpected status, unreadable response) stops the script:
  an umbrella silently missing a member it should have is worse than a failed run.

.PARAMETER MembersFile
  Path to Members.props. Defaults to src/XRay/Members.props next to this script's parent.

.PARAMETER Strict
  Fail when any member has no published version, instead of skipping it.
#>
[CmdletBinding()]
param(
    [string] $MembersFile = (Join-Path $PSScriptRoot '..' 'src' 'XRay' 'Members.props'),
    [switch] $Strict
)

$ErrorActionPreference = 'Stop'
$onAzure = [bool] $env:TF_BUILD

# Progress goes to the log on Azure and to stderr locally, so that stdout carries nothing but the
# -p: arguments and `$(pwsh .devops/resolve-member-versions.ps1)` can be spliced into a command.
function Write-Info([string] $message) {
    if ($onAzure) { Write-Host $message }
    else { [Console]::Error.WriteLine($message) }
}

function Write-PipelineWarning([string] $message) {
    if ($onAzure) { Write-Host "##vso[task.logissue type=warning]$message" }
    else { [Console]::Error.WriteLine("warning: $message") }
}

function Write-PipelineError([string] $message) {
    if ($onAzure) { Write-Host "##vso[task.logissue type=error]$message" }
    [Console]::Error.WriteLine("error: $message")
    exit 1
}

[xml] $props = Get-Content -Raw -LiteralPath $MembersFile
$members = @($props.Project.ItemGroup.XRayMember)
if ($members.Count -eq 0) { Write-PipelineError "No <XRayMember> items in $MembersFile." }

$resolved = [ordered] @{}
$missing = @()

foreach ($member in $members) {
    $id = $member.Include
    if ($member.Version -notmatch '^\$\((\w+)\)$') {
        Write-PipelineError "Member $id in $MembersFile must have Version=`"`$(SomeProperty)`", found `"$($member.Version)`"."
    }
    $property = $Matches[1]

    $pinned = [Environment]::GetEnvironmentVariable($property)
    if (-not $pinned) { $pinned = [Environment]::GetEnvironmentVariable($property.ToUpperInvariant()) }
    if ($pinned) {
        Write-Info "$id -> $pinned (pinned by `$$property)"
        $resolved[$property] = $pinned
        continue
    }

    $url = "https://api.nuget.org/v3-flatcontainer/$($id.ToLowerInvariant())/index.json"
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq 404) {
            $missing += $id
            continue
        }
        Write-PipelineError "Could not read $url : $($_.Exception.Message)"
    }

    $versions = @($response.versions)
    $stable = @($versions | Where-Object { $_ -notmatch '-' })
    $latest = if ($stable.Count -gt 0) { $stable[-1] } elseif ($versions.Count -gt 0) { $versions[-1] } else { $null }
    if (-not $latest) {
        $missing += $id
        continue
    }

    Write-Info "$id -> $latest"
    $resolved[$property] = $latest
}

foreach ($id in $missing) {
    $text = "$id has no version on nuget.org yet; it is left out of the X-Ray umbrella."
    if ($Strict) { Write-PipelineError "$text (-Strict)" }
    Write-PipelineWarning $text
}

if ($resolved.Count -eq 0) {
    Write-PipelineError "No member of the X-Ray family is published, so the umbrella would install nothing."
}

$memberArgs = ($resolved.GetEnumerator() | ForEach-Object { "-p:$($_.Key)=$($_.Value)" }) -join ' '
$memberProps = ($resolved.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ';'

if ($onAzure) {
    Write-Host "##vso[task.setvariable variable=memberArgs;]$memberArgs"
    Write-Host "##vso[task.setvariable variable=memberProps;]$memberProps"
    Write-Host "memberArgs = $memberArgs"
}
else {
    Write-Output $memberArgs
}
