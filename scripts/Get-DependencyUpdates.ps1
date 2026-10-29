#Requires -Version 7
<#
.SYNOPSIS
    Lists the newer versions available for every technology pinned in the repository, the known
    vulnerabilities of its NuGet packages and the result of the daily security audit.

.DESCRIPTION
    Version watch of gestion-projet/depot.md § Mises à jour des dépendances, run by the agent when work
    resumes, at most once per run of the daily security audit. It only reads official sources and changes nothing: the upgrade itself, with
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
    # The SemVer 2.0.0 hive: the SemVer 1.0.0 one omits any version that depends on a SemVer 2.0.0
    # package (e.g. bunit 2.13.3), and answers 404 for it.
    (Invoke-RestMethod "https://api.nuget.org/v3/registration5-gz-semver2/$($id.ToLowerInvariant())/$($version.ToLowerInvariant()).json").published
}

$updates = [System.Collections.Generic.List[object]]::new()

# Reports the newest release of the major version in use, then, apart, the newest major version: a
# newer major kept aside (e.g. a .NET release that is not LTS) must not hide the fixes of the current one.
# $releases: objects with Version ([version]), Name (as written in the repository) and Published, or a
# scriptblock returning the publication date of a release, so that only the reported ones are looked up.
function Add-Update([string] $component, [string] $current, [version] $currentVersion, $releases) {
    $newer = @($releases | Where-Object { $_.Version -gt $currentVersion } | Sort-Object Version)
    $sameMajor = $newer | Where-Object { $_.Version.Major -eq $currentVersion.Major } | Select-Object -Last 1
    $newestMajor = $newer | Where-Object { $_.Version.Major -gt $currentVersion.Major } | Select-Object -Last 1
    foreach ($release in @($sameMajor, $newestMajor) | Where-Object { $_ }) {
        $published = if ($release.Published -is [scriptblock]) { & $release.Published } else { $release.Published }
        $updates.Add((New-Update $component $current $release.Name $currentVersion $release.Version $published))
    }
}

function Get-NuGetReleases([string] $id) {
    # GetNewClosure() runs the scriptblock in a dynamic module that does not see the script's functions:
    # the lookup is captured as a variable instead.
    $getPublished = ${function:Get-NuGetPublished}
    (Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$($id.ToLowerInvariant())/index.json").versions |
        Where-Object { $_ -notmatch '-' } |
        ForEach-Object {
            $name = $_
            [pscustomobject]@{ Version = [version] $name; Name = $name; Published = { & $getPublished $id $name }.GetNewClosure() }
        }
}

# NuGet packages: central package management gives one version per package (Directory.Packages.props).
([xml] (Get-Content (Join-Path $root 'Directory.Packages.props') -Raw)).Project.ItemGroup.PackageVersion |
    Where-Object { $_ } |
    ForEach-Object { Add-Update "NuGet $($_.Include)" $_.Version $_.Version (Get-NuGetReleases $_.Include) }

# Local .NET tools (dotnet-tools.json).
$tools = (Get-Content (Join-Path $root 'dotnet-tools.json') -Raw | ConvertFrom-Json).tools
foreach ($tool in $tools.PSObject.Properties) {
    Add-Update "dotnet tool $($tool.Name)" $tool.Value.version $tool.Value.version (Get-NuGetReleases $tool.Name)
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

# The commit a tag designates, through the tag object of an annotated tag.
function Get-TagCommit([string] $repo, [string] $tag) {
    $target = (gh api "repos/$repo/git/ref/tags/$tag" | ConvertFrom-Json).object
    if ($target.type -eq 'tag') { $target = (gh api "repos/$repo/git/tags/$($target.sha)" | ConvertFrom-Json).object }
    $target.sha
}

# GitHub Actions: each action is pinned to the commit of an exact release of its repository, named in a
# comment. A commit that is no longer the one of its release stops the watch: the comment would lie.
Get-ChildItem (Join-Path $root '.github/workflows') -Filter *.yml |
    Select-String -Pattern 'uses:\s*([\w.-]+/[\w.-]+)@([0-9a-f]{40}) # v(\d+\.\d+\.\d+)' |
    ForEach-Object { [pscustomobject]@{ Repo = $_.Matches[0].Groups[1].Value; Commit = $_.Matches[0].Groups[2].Value; Version = $_.Matches[0].Groups[3].Value } } |
    Sort-Object Repo, Commit, Version -Unique |
    ForEach-Object {
        $released = Get-TagCommit $_.Repo "v$($_.Version)"
        if ($released -ne $_.Commit) {
            throw "Action $($_.Repo): commit $($_.Commit) is not the one of v$($_.Version) ($released)."
        }
        $releases = gh api "repos/$($_.Repo)/releases?per_page=100" | ConvertFrom-Json |
            Where-Object { -not $_.draft -and -not $_.prerelease -and $_.tag_name -match '^v(\d+\.\d+\.\d+)$' } |
            ForEach-Object { [pscustomobject]@{ Version = [version] $Matches[1]; Name = $_.tag_name; Published = $_.published_at } }
        Add-Update "Action $($_.Repo)" "v$($_.Version)" $_.Version $releases
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

# Daily security audit (security-audit.yml): every known vulnerability of the NuGet packages, of the
# images and of the actions, kept in its "Vulnérabilités connues" issue whether or not the repository is
# being worked on.
$audit = gh run list --workflow security-audit.yml --branch main --limit 1 --json conclusion,createdAt,url 2>$null | ConvertFrom-Json
if (-not $audit) {
    Write-Output 'Daily security audit: no run yet'
}
else {
    Write-Output "Daily security audit: $($audit[0].conclusion) ($($audit[0].createdAt.ToString('yyyy-MM-dd'))) $($audit[0].url)"
    $issue = gh issue list --label security-audit --state all --limit 1 --json number,state,body,url | ConvertFrom-Json
    if ($issue) {
        $known = [regex]::Match($issue[0].body, '(?s)```text\r?\n(.*?)```').Groups[1].Value.Trim()
        Write-Output "Known vulnerabilities ($($issue[0].state), $($issue[0].url)):"
        Write-Output $(if ($known) { $known } else { 'none' })
    }
}
