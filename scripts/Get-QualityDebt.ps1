#Requires -Version 7.3
<#
.SYNOPSIS
    Lists the quality debt along the way that bears on the files a branch changes.

.DESCRIPTION
    The weekly quality audit keeps its entries in the tracking issue of the quality harness (labels quality-audit and
    harness). Those of its section "Au fil de l'eau" are never treated on their own: a chantier fixes them in its pull
    request when it changes their file anyway (gestion-projet/depot/dette-qualite-au-fil-de-l-eau.md). This script lists
    them, from the files changed since the branch left the base branch, uncommitted and untracked changes included, each
    under its file.

    An entry is written in full, as the issue holds it: it is what scripts/Remove-QualityDebt.ps1 takes once the pull
    request is merged. The listing is text, read by agents, whose output is never a console.

.EXAMPLE
    ./scripts/Get-QualityDebt.ps1

.EXAMPLE
    ./scripts/Get-QualityDebt.ps1 -Base origin/main
#>
[CmdletBinding()]
param(
    # Branch the changes are compared with.
    [string] $Base = 'origin/main'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# A failed git or gh must stop the script: a listing made without the issue would read as "no debt".
$PSNativeCommandUseErrorActionPreference = $true
# gh writes UTF-8, which PowerShell decodes with this encoding, and the entries hold non-ASCII text (→, accents) that
# must come out as the issue holds them, to be given back to Remove-QualityDebt.ps1.
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)

# Found by both labels, never by its title: quality-audit alone also designates the chantiers of the harness.
$issue = @(gh issue list --label quality-audit --label harness --state all --limit 1 --json body | ConvertFrom-Json)
$body = if ($issue) { $issue[0].body } else { '' }

$inList = $false
$entries = foreach ($line in $body -split '\r?\n') {
    if ($line -eq '<!-- list -->') { $inList = $true; continue }
    if ($line -eq '<!-- end -->') { $inList = $false; continue }
    if ($inList -and $line -and -not $line.StartsWith('```')) { $line }
}

$root = git rev-parse --show-toplevel
$mergeBase = git -C $root merge-base $Base HEAD
# Without rename detection, a moved file counts at its former path, where the audit last saw its entries, as at its new one.
# The line-ending warnings of a working copy under core.autocrlf say nothing of which files changed.
$changed = @(
    git -C $root -c core.safecrlf=false diff --name-only --no-renames $mergeBase
    git -C $root ls-files --others --exclude-standard
) | Where-Object { $_ } | Sort-Object -Unique

# The forms the audit writes (.github/workflows/quality-audit.yml): "mutant <file> <mutator>: …" and
# "style <rule> <file>".
function Get-EntryFile([string] $entry) {
    $words = $entry -split ' ', 4
    if ($words[0] -eq 'mutant') { $words[1] } else { $words[2] }
}

$debt = @($entries | Where-Object { (Get-EntryFile $_) -in $changed } | Group-Object { Get-EntryFile $_ })

# An empty listing would read as "nothing to fix" as well as "nothing written": the absence is said.
if (-not $debt) {
    'No quality debt along the way on a changed file.'
    return
}
'Quality debt along the way on a changed file (each fixed in this pull request, removed from the tracking issue once merged):'
foreach ($file in $debt | Sort-Object Name) {
    $file.Name
    $file.Group | Sort-Object | ForEach-Object { "  $_" }
}
