#Requires -Version 7.3
<#
.SYNOPSIS
    Lists the concepts of the .speckit that govern the files a branch changes.

.DESCRIPTION
    The .speckit is read through its map, never in full (AGENTS.md, reading rules): before each push, the concepts
    whose applies_to covers a changed file must have been read. This script lists them, from the files changed since
    the branch left the base branch, uncommitted and untracked changes included, each with the changed files it
    governs.

    The listing is text, a concept per line followed by its files, one per line: it is read by agents, whose output is
    never a console, and a table left to the formatting of the host is cut to the width of a console, or not written
    at all without one.

    A concept that governs no existing code has no applies_to: it is found through the map, never by this script.

.EXAMPLE
    ./scripts/Get-SpeckitConcepts.ps1

.EXAMPLE
    ./scripts/Get-SpeckitConcepts.ps1 -Base origin/main
#>
[CmdletBinding()]
param(
    # Branch the changes are compared with.
    [string] $Base = 'origin/main'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = git rev-parse --show-toplevel
$speckit = Join-Path $root '.speckit'
$mergeBase = git -C $root merge-base $Base HEAD
$changed = @(
    git -C $root diff --name-only $mergeBase
    git -C $root ls-files --others --exclude-standard
) | Where-Object { $_ -and $_ -notlike '.speckit/*' } | Sort-Object -Unique

# applies_to patterns: '**' crosses folders, '*' does not.
function ConvertTo-GlobRegex([string] $pattern) {
    $escaped = [regex]::Escape($pattern).Replace('\*\*', "`u{1}").Replace('\*', '[^/]*').Replace("`u{1}", '.*')
    [regex] "^$escaped$"
}

function Get-AppliesTo([string] $path) {
    $inHeader = $false; $inList = $false
    foreach ($line in [IO.File]::ReadLines($path)) {
        if ($line -eq '---') { if ($inHeader) { return } else { $inHeader = $true; continue } }
        if (-not $inHeader) { return }
        if ($line -match '^applies_to:') { $inList = $true; continue }
        if ($inList -and $line -match "^\s+-\s+'?(?<glob>[^']+)'?\s*$") { $Matches['glob']; continue }
        $inList = $false
    }
}

$concepts = @(foreach ($file in Get-ChildItem $speckit -Recurse -Filter *.md) {
        $patterns = @(Get-AppliesTo $file.FullName | ForEach-Object { ConvertTo-GlobRegex $_ })
        if (-not $patterns) { continue }
        $governed = @($changed | Where-Object { $f = $_; $patterns | Where-Object { $_.IsMatch($f) } })
        if ($governed) {
            [pscustomobject]@{
                Concept = [IO.Path]::GetRelativePath($speckit, $file.FullName).Replace('\', '/')
                Files = $governed
            }
        }
    })

# An empty listing would read as "nothing to read" as well as "nothing written": the absence is said.
if (-not $concepts) {
    'No concept governs a changed file.'
    return
}
'Concepts that govern a changed file (each to read before pushing):'
foreach ($entry in $concepts | Sort-Object Concept) {
    $entry.Concept
    $entry.Files | ForEach-Object { "  $_" }
}
