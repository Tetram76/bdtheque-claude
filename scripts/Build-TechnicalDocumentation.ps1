#Requires -Version 7.4
<#
.SYNOPSIS
    Builds the site of the technical documentation (docs/tech) with DocFX, and serves it on demand.

.DESCRIPTION
    The technical documentation is Markdown read as such, by agents and on GitHub (gestion-projet/gouvernance/
    documentation-technique.md); this site is only another view of it, built from the same files
    (choix-implementation/documentation-technique.md). The sources are copied to a staging folder, never changed in
    place, where:
    - the navigation of the site (toc.yml) is generated from the maps (index.md), which alone are written by hand;
    - a link that leaves docs/tech (code, .speckit, README) is rewritten to the same file on GitHub, at the commit
      documented, the site holding no other file of the repository.
    The commit is written at the foot of each page, so that a site is never taken for that of another version.

    DocFX is the local .NET tool of dotnet-tools.json, restored here first: on a new machine, the .NET SDK and PowerShell
    are enough. Warnings fail the build (a broken link among others).

.EXAMPLE
    ./scripts/Build-TechnicalDocumentation.ps1 -Serve
    Builds the documentation of the working copy and serves it on http://localhost:8182.

.EXAMPLE
    ./scripts/Build-TechnicalDocumentation.ps1 -Output ./site -Commit 0123abc
#>
[CmdletBinding()]
param(
    # Folder of the documentation: docs/tech of this repository by default.
    [string] $Source,

    # Folder of the site, emptied first.
    [string] $Output = (Join-Path ([IO.Path]::GetTempPath()) 'bdtheque-docs-site'),

    # Commit documented, at the foot of each page and in the links to GitHub: that of the working copy by default.
    [string] $Commit,

    # Serves the site once built, until stopped.
    [switch] $Serve,

    # Address the site is served on: localhost only, unless a container serves it to its host.
    [string] $HostName = 'localhost',

    [int] $Port = 8182
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$repositoryUrl = 'https://github.com/Tetram76/bdtheque-claude'
$root = Split-Path $PSScriptRoot -Parent
if (-not $Source) { $Source = Join-Path $root 'docs/tech' }
$Source = (Resolve-Path $Source).Path
$repositoryRoot = Split-Path (Split-Path $Source -Parent) -Parent

if (-not $Commit) {
    # Outside a Git repository (sources extracted without their history), the version is unknown.
    try {
        $Commit = git -C $repositoryRoot rev-parse HEAD 2>$null
        # Uncommitted changes are part of what is built: the commit alone would not describe it.
        if (git -C $repositoryRoot status --porcelain -- $Source) {
            $Commit += ' + modifications de la copie de travail'
        }
    }
    catch {
        $Commit = 'unknown'
    }
}
$reference = ($Commit -split ' ')[0]
if ($reference -eq 'unknown') { $reference = 'main' }

# An entry of a map: '* [Title](path) - description'.
$entryPattern = [regex] '^\* \[(?<title>[^\]]+)\]\((?<path>[^)]+)\)'

function Get-MapEntry([string] $folder) {
    foreach ($line in [IO.File]::ReadLines((Join-Path $folder 'index.md'))) {
        $entry = $entryPattern.Match($line)
        if ($entry.Success) { [pscustomobject]@{ Title = $entry.Groups['title'].Value; Path = $entry.Groups['path'].Value } }
    }
}

# The tree of the navigation, from the maps: a sub-map becomes a node, under the title its parent map gives it.
function Get-TocItem([string] $folder, [string] $prefix, [int] $depth) {
    $indent = '  ' * $depth
    foreach ($entry in Get-MapEntry $folder) {
        $title = $entry.Title.Replace("'", "''")
        "$indent- name: '$title'"
        "$indent  href: '$prefix$($entry.Path)'"
        if ($entry.Path -match '^(?<sub>[^/]+)/index\.md$') {
            $sub = $Matches['sub']
            "$indent  items:"
            Get-TocItem (Join-Path $folder $sub) "$prefix$sub/" ($depth + 1)
        }
    }
}

# Markdown links outside the fences: a fence holds a diagram or an example, never a link to rewrite.
$linkPattern = [regex] '\]\((?<href>[^)\s]+)\)'

# The address on GitHub of a relative link that leaves docs/tech, or nothing for a link that stays in it (DocFX resolves
# it) or is no relative link. The target is not read: the image of the validation environment holds docs/tech alone, and
# the tests of the repository check that every target exists.
function Get-GitHubAddress([string] $markdownFile, [string] $href) {
    if ($href -match '^[a-z]+:' -or $href.StartsWith('#')) { return }
    $parts = $href.Split('#', 2)
    $target = [IO.Path]::GetFullPath((Join-Path (Split-Path $markdownFile -Parent) $parts[0]))
    if ($target.StartsWith($Source + [IO.Path]::DirectorySeparatorChar) -or $target -eq $Source) { return }
    $path = [IO.Path]::GetRelativePath($repositoryRoot, $target).Replace('\', '/')
    $kind = if ($parts[0].EndsWith('/')) { 'tree' } else { 'blob' }
    $anchor = if ($parts.Count -eq 2) { "#$($parts[1])" } else { '' }
    "$repositoryUrl/$kind/$reference/$path$anchor"
}

# A link written as code is an example, not a link.
function Convert-LinkOfLine([string] $markdownFile, [string] $line) {
    $codeSpans = @([regex]::Matches($line, '`[^`]*`'))
    $linkPattern.Replace($line, {
        param($match)
        $inCode = $codeSpans | Where-Object { $_.Index -lt $match.Index -and $match.Index -lt $_.Index + $_.Length }
        $address = if (-not $inCode) { Get-GitHubAddress $markdownFile $match.Groups['href'].Value }
        if ($address) { "]($address)" } else { $match.Value }
    })
}

function Convert-Link([string] $markdownFile, [string] $text) {
    $inFence = $false
    $lines = foreach ($line in $text -split "`n") {
        if ($line -match '^\s*```') { $inFence = -not $inFence }
        if ($inFence -or $line -match '^\s*```') { $line } else { Convert-LinkOfLine $markdownFile $line }
    }
    $lines -join "`n"
}

$staging = Join-Path ([IO.Path]::GetTempPath()) "bdtheque-docs-staging-$PID"
try {
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
    New-Item -ItemType Directory $staging | Out-Null
    foreach ($file in Get-ChildItem $Source -Recurse -File) {
        $relative = [IO.Path]::GetRelativePath($Source, $file.FullName)
        $destination = Join-Path $staging $relative
        New-Item -ItemType Directory (Split-Path $destination -Parent) -Force | Out-Null
        if ($file.Extension -eq '.md') {
            $text = [IO.File]::ReadAllText($file.FullName).Replace("`r`n", "`n")
            [IO.File]::WriteAllText($destination, (Convert-Link $file.FullName $text))
        }
        else {
            Copy-Item $file.FullName $destination
        }
    }

    $toc = @('items:', "- name: 'Accueil'", "  href: 'index.md'") + @(Get-TocItem $Source '' 0)
    [IO.File]::WriteAllLines((Join-Path $staging 'toc.yml'), $toc)

    $Output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
    if (Test-Path $Output) { Remove-Item $Output -Recurse -Force }
    $footer = "Documentation technique de BDthèque, version $Commit. Elle décrit l'implémentation ; le .speckit fait foi."
    # The local tool is found from the folder of dotnet-tools.json. `dotnet tool run` never restores it: on a machine
    # where it was never restored, it only asks for the restore, which does nothing once done.
    Push-Location $root
    try {
        dotnet tool restore
        dotnet tool run docfx build (Join-Path $staging 'docfx.json') --output $Output --warningsAsErrors --disableGitFeatures `
            --metadata "_appFooter=$footer"
    }
    finally {
        Pop-Location
    }
}
finally {
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
}

Write-Host "Site: $Output"
if ($Serve) {
    Push-Location $root
    try {
        dotnet tool run docfx serve $Output --hostname $HostName --port $Port
    }
    finally {
        Pop-Location
    }
}
