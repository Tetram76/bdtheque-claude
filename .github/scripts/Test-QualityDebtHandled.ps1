#Requires -Version 7.3
<#
.SYNOPSIS
    Fails while an entry along the way of the quality tracking issue, on a file a pull request changes, is not handled
    in its description.

.DESCRIPTION
    A chantier fixes the debt along the way of the files it changes anyway (gestion-projet/depot/dette-qualite-au-fil-de-l-eau.md).
    Run by .github/workflows/quality-debt.yml on every pull request, whatever environment its session works in, this
    check lists the entries along the way on its changed files, a moved file counting at its former path, where the
    audit saw its entries, as at its new one; each must be handled in its description, between the markers
    <!-- quality-debt --> and <!-- end-quality-debt -->, by a line "fixed <entry>", "rejected <entry>" or
    "deferred #<ticket> <entry>". The check fails on any entry left out, and writes the lines to add.

.EXAMPLE
    ./.github/scripts/Test-QualityDebtHandled.ps1 -PullRequest 682
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [int] $PullRequest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'QualityDebt.psm1') -Force

$files = @(Invoke-Gh api --paginate "repos/{owner}/{repo}/pulls/$PullRequest/files?per_page=100" --jq '.[] | .filename, (.previous_filename // empty)')
$body = (Invoke-Gh api "repos/{owner}/{repo}/pulls/$PullRequest" --jq '.body // ""') -join "`n"
$debt = @(Get-DebtAlongTheWay (Get-TrackingIssue).body | Where-Object { (Get-EntryFile $_) -in $files })
$states = @{}
Get-HandledDebt $body | ForEach-Object { $states[$_.Entry] = $_.State }
$missing = @($debt | Where-Object { -not $states.ContainsKey($_) })

$report = if (-not $debt) {
    'No quality debt along the way on a file this pull request changes.'
}
else {
    'Quality debt along the way on a file this pull request changes, each fixed in it, or rejected or deferred:'
    $debt | ForEach-Object { "  $(if ($states.ContainsKey($_)) { $states[$_] } else { 'NOT HANDLED' }): $_" }
}
if ($missing) {
    $report = @($report) + @(
        ''
        'Handle each entry, then say what was done with it in the description of the pull request, between these'
        'markers, by "fixed", "rejected" or "deferred #<ticket>" in place of the placeholder:'
        '<!-- quality-debt -->'
        '```text'
        $missing | ForEach-Object { "<fixed|rejected|deferred #ticket> $_" }
        '```'
        '<!-- end-quality-debt -->'
        'Editing the description runs this check again.'
    )
}
$report
if ($env:GITHUB_STEP_SUMMARY) { $report | Add-Content $env:GITHUB_STEP_SUMMARY }
if ($missing) { exit 1 }
