#Requires -Version 7.3
<#
.SYNOPSIS
    Removes from the quality tracking issue the entries that the pull request merged by a commit of main fixed or
    rejected.

.DESCRIPTION
    The tracking issue is a state, not a history: an entry whose treatment is acquired, once the pull request that fixes
    it or carries its rejection is merged, is removed at once (gestion-projet/revue-tickets/issues.md, tracking issues of
    the harnesses). Run by .github/workflows/quality-debt.yml on every push to main, this step reads the description of
    the merged pull request and removes the entries it marks "fixed" or "rejected"; a deferred one stays until the pull
    request that fixes it. Only those lines are removed, the description being read just before it is written.

    GitHub replaces a description whole and supports no conditional write: the workflow runs the removals one after
    the other. A removal and the rewrite of the weekly audit can still overlap, the last write putting back what the
    other removed; the next audit, which rewrites the description from what it still finds, drops such an entry.

.EXAMPLE
    ./.github/scripts/Remove-QualityDebt.ps1 -Commit 0559e003b6e9e5048371ca912be01269db0110cb
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Commit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'QualityDebt.psm1') -Force

$pullRequests = @(Invoke-Gh api "repos/{owner}/{repo}/commits/$Commit/pulls" --jq '.[] | select(.merged_at != null) | .number')
if (-not $pullRequests) {
    "No merged pull request for $Commit."
    return
}
$body = (Invoke-Gh api "repos/{owner}/{repo}/pulls/$($pullRequests[0])" --jq '.body // ""') -join "`n"
$done = @(Get-HandledDebt $body | Where-Object State -In 'fixed', 'rejected' | ForEach-Object Entry)

$issue = Get-TrackingIssue
$lines = $issue.body -split '\r?\n'
$removed = @($done | Where-Object { $_ -in $lines })
if (-not $removed) {
    "Pull request #$($pullRequests[0]) removes no entry from the tracking issue #$($issue.number)."
    return
}

$file = New-TemporaryFile
try {
    [IO.File]::WriteAllText($file.FullName, (($lines | Where-Object { $_ -notin $removed }) -join "`n"), [Text.UTF8Encoding]::new($false))
    Invoke-Gh issue edit $issue.number --body-file $file.FullName | Out-Null
}
finally {
    Remove-Item $file.FullName
}
"Removed from the tracking issue #$($issue.number), as handled by pull request #$($pullRequests[0]):"
$removed | ForEach-Object { "  $_" }
