#Requires -Version 7
<#
.SYNOPSIS
    Lists the newer versions available for every technology pinned in the repository, the known
    vulnerabilities of its NuGet packages and the open Dependabot alerts.

.DESCRIPTION
    Version watch of gestion-projet/depot.md § Mises à jour des dépendances, run by the agent at each
    resumption of work. It only reads official sources and changes nothing: the upgrade itself, with
    its review, is the agent's work.

    Covered: NuGet packages (Directory.Packages.props) and local .NET tools, .NET SDK and runtime
    images, PostgreSQL image, GitHub Actions of the workflows. Requires dotnet and an authenticated gh.
#>
[CmdletBinding()]
param(
    # Versions published more recently are flagged, not hidden: depot.md waits 7 days before taking them.
    [int] $MinimumAgeDays = 7
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$now = [DateTimeOffset]::UtcNow

function Get-UpdateKind([version] $current, [version] $latest) {
    if ($latest.Major -ne $current.Major) { 'major' }
    elseif ($latest.Minor -ne $current.Minor) { 'minor' }
    else { 'patch' }
}

function New-Update([string] $component, [string] $current, [string] $latest, [version] $currentVersion, [version] $latestVersion, $published) {
    [pscustomobject]@{
        Component = $component
        Current   = $current
        Latest    = $latest
        Kind      = Get-UpdateKind $currentVersion $latestVersion
        Published = if ($published) { ([DateTimeOffset] $published).ToString('yyyy-MM-dd') } else { '' }
        TooRecent = [bool] ($published -and ($now - [DateTimeOffset] $published).TotalDays -lt $MinimumAgeDays)
    }
}

function Get-NuGetPublished([string] $id, [string] $version) {
    (Invoke-RestMethod "https://api.nuget.org/v3/registration5-semver1/$($id.ToLowerInvariant())/$($version.ToLowerInvariant()).json").published
}

$updates = [System.Collections.Generic.List[object]]::new()

# NuGet packages: central package management resolves one version per package for the whole solution.
$outdated = dotnet list (Join-Path $root 'Bdtheque.slnx') package --outdated --format json | ConvertFrom-Json
$outdated.projects.frameworks.topLevelPackages |
    Where-Object { $_ } |
    Sort-Object id -Unique |
    ForEach-Object {
        $updates.Add((New-Update "NuGet $($_.id)" $_.resolvedVersion $_.latestVersion $_.resolvedVersion $_.latestVersion (Get-NuGetPublished $_.id $_.latestVersion)))
    }

# Local .NET tools (dotnet-tools.json), absent from `dotnet list package`.
$tools = (Get-Content (Join-Path $root 'dotnet-tools.json') -Raw | ConvertFrom-Json).tools
foreach ($tool in $tools.PSObject.Properties) {
    $versions = (Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$($tool.Name.ToLowerInvariant())/index.json").versions |
        Where-Object { $_ -notmatch '-' }
    $latest = $versions[-1]
    if ([version] $latest -gt [version] $tool.Value.version) {
        $updates.Add((New-Update "dotnet tool $($tool.Name)" $tool.Value.version $latest $tool.Value.version $latest (Get-NuGetPublished $tool.Name $latest)))
    }
}

# .NET images: the official release metadata gives the latest runtime and SDK of the channel, with
# their release date; the next channel is listed so that a new LTS shows up.
$dockerfile = Get-Content (Join-Path $root 'docker/api/Dockerfile') -Raw
$sdk = [regex]::Match($dockerfile, 'dotnet/sdk:(\d+\.\d+\.\d+)').Groups[1].Value
$runtime = [regex]::Match($dockerfile, 'dotnet/aspnet:(\d+\.\d+\.\d+)').Groups[1].Value
$channel = ([version] $runtime).ToString(2)
$release = Invoke-RestMethod "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/$channel/releases.json"
if ([version] $release.'latest-sdk' -gt [version] $sdk) {
    $updates.Add((New-Update 'Image dotnet/sdk' $sdk $release.'latest-sdk' $sdk $release.'latest-sdk' $release.'latest-release-date'))
}
if ([version] $release.'latest-release' -gt [version] $runtime) {
    $updates.Add((New-Update 'Image dotnet/aspnet' $runtime $release.'latest-release' $runtime $release.'latest-release' $release.'latest-release-date'))
}
(Invoke-RestMethod 'https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json').'releases-index' |
    Where-Object { [version] $_.'channel-version' -gt [version] $channel -and $_.'release-type' -eq 'lts' -and $_.'support-phase' -ne 'preview' } |
    ForEach-Object {
        $updates.Add((New-Update ".NET $($_.'channel-version') (LTS)" $runtime $_.'latest-release' $runtime $_.'latest-release' $_.'latest-release-date'))
    }

# PostgreSQL image: same Alpine flavour, newest patch of the current major and newest major.
$compose = Get-Content (Join-Path $root 'docker-compose.yml') -Raw
$pgTag = [regex]::Match($compose, 'image:\s*postgres:(\S+)').Groups[1].Value
$pgVersion = [version] ($pgTag -replace '-alpine.*$', '')
$pgTags = (Invoke-RestMethod 'https://hub.docker.com/v2/repositories/library/postgres/tags?page_size=100&name=-alpine&ordering=last_updated').results |
    Where-Object { $_.name -match '^(\d+\.\d+)-alpine(\d+\.\d+)$' } |
    ForEach-Object { [pscustomobject]@{ Name = $_.name; Version = [version] $Matches[1]; Alpine = [version] $Matches[2]; Updated = $_.last_updated } }
$pgTags |
    Where-Object { $_.Version -gt $pgVersion } |
    Group-Object { $_.Version.Major -eq $pgVersion.Major } |
    ForEach-Object {
        $best = $_.Group | Sort-Object Version, Alpine -Descending | Select-Object -First 1
        $updates.Add((New-Update 'Image postgres' $pgTag $best.Name $pgVersion $best.Version $best.Updated))
    }
$pgAlpine = [version] ($pgTag -replace '^.*-alpine', '')
$newerAlpine = $pgTags | Where-Object { $_.Version -eq $pgVersion -and $_.Alpine -gt $pgAlpine } | Sort-Object Alpine -Descending | Select-Object -First 1
if ($newerAlpine) {
    $updates.Add((New-Update 'Image postgres (Alpine)' $pgTag $newerAlpine.Name $pgAlpine $newerAlpine.Alpine $newerAlpine.Updated))
}

# GitHub Actions: each action is pinned to an exact release of its repository.
Get-ChildItem (Join-Path $root '.github/workflows') -Filter *.yml |
    Select-String -Pattern 'uses:\s*([\w.-]+/[\w.-]+)@v(\d+\.\d+\.\d+)' |
    ForEach-Object { [pscustomobject]@{ Repo = $_.Matches[0].Groups[1].Value; Version = $_.Matches[0].Groups[2].Value } } |
    Sort-Object Repo, Version -Unique |
    ForEach-Object {
        $latest = gh api "repos/$($_.Repo)/releases/latest" | ConvertFrom-Json
        $latestVersion = $latest.tag_name.TrimStart('v')
        if ([version] $latestVersion -gt [version] $_.Version) {
            $updates.Add((New-Update "Action $($_.Repo)" "v$($_.Version)" $latest.tag_name $_.Version $latestVersion $latest.published_at))
        }
    }

$updates | Sort-Object Kind, Component | Format-Table -AutoSize | Out-String -Width 200 | Write-Output

# Known vulnerabilities of NuGet packages, transitive ones included. GitHub's dependency graph cannot
# serve here: it does not read the versions of Directory.Packages.props (central package management).
$vulnerable = dotnet list (Join-Path $root 'Bdtheque.slnx') package --vulnerable --include-transitive --format json | ConvertFrom-Json
$nugetVulnerabilities = $vulnerable.projects.frameworks |
    ForEach-Object { @($_.topLevelPackages) + @($_.transitivePackages) } |
    Where-Object { $_ } |
    ForEach-Object {
        $package = $_
        $package.vulnerabilities | ForEach-Object {
            [pscustomobject]@{ Package = $package.id; Version = $package.resolvedVersion; Severity = $_.severity; Advisory = $_.advisoryurl }
        }
    } |
    Sort-Object Package, Advisory -Unique
if (-not $nugetVulnerabilities) {
    Write-Output 'Vulnerable NuGet packages: none'
}
else {
    Write-Output "Vulnerable NuGet packages: $(@($nugetVulnerabilities).Count)"
    $nugetVulnerabilities | Format-Table -AutoSize | Out-String -Width 200 | Write-Output
}

# Dependabot alerts: in practice, the GitHub Actions of the workflows, whose versions the dependency graph reads.
$alerts = gh api 'repos/{owner}/{repo}/dependabot/alerts?state=open&per_page=100' | ConvertFrom-Json
if ($alerts.Count -eq 0) {
    Write-Output 'Open Dependabot alerts: none'
}
else {
    Write-Output "Open Dependabot alerts: $($alerts.Count)"
    $alerts | ForEach-Object {
        [pscustomobject]@{
            Package  = $_.dependency.package.name
            Severity = $_.security_advisory.severity
            Fixed    = $_.security_vulnerability.first_patched_version.identifier
            Advisory = $_.security_advisory.ghsa_id
        }
    } | Format-Table -AutoSize | Out-String -Width 200 | Write-Output
}
