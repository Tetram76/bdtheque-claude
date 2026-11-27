#Requires -Version 7.3
<#
.SYNOPSIS
    The quality debt along the way, as the tracking issue and the description of a pull request hold it.

.DESCRIPTION
    Shared by the two steps of .github/workflows/quality-debt.yml (gestion-projet/depot/dette-qualite-au-fil-de-l-eau.md):
    the check of a pull request, which fails while an entry along the way on a file it changes is not handled in its
    description, and the removal, at its merge, of the entries it fixed or rejected from the tracking issue.

    Every gh call goes through Invoke-Gh, which fails on a failed call: a listing made without the issue or the pull
    request would read as "no debt", the very failure the check exists to prevent.
#>
Set-StrictMode -Version Latest

# gh writes UTF-8, which PowerShell decodes with this encoding: the entries of the mutants hold non-ASCII text (→) that
# must be read as the issue and the description hold it, to be compared and removed.
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)

function Invoke-Gh {
    # Reset first: a command that is not a native program, as gh may be in the tests, leaves it as it was.
    $global:LASTEXITCODE = 0
    $output = & gh @args
    if ($LASTEXITCODE -ne 0) { throw "gh $($args -join ' ') failed (exit code $LASTEXITCODE)." }
    $output
}

# Found by both labels, never by its title: quality-audit alone also designates the chantiers of the harness.
function Get-TrackingIssue {
    $issues = @(Invoke-Gh issue list --label quality-audit --label harness --state all --limit 1 --json 'number,body' | ConvertFrom-Json)
    if (-not $issues) { throw 'No tracking issue of the quality harness (labels quality-audit and harness).' }
    $issues[0]
}

# The lines between two markers of a description, the code fence they hold excepted.
function Get-MarkedLines([string] $Text, [string] $Start, [string] $End) {
    $inside = $false
    foreach ($line in $Text -split '\r?\n') {
        if ($line.Trim() -eq $Start) { $inside = $true; continue }
        if ($line.Trim() -eq $End) { $inside = $false; continue }
        if ($inside -and $line.Trim() -and -not $line.Trim().StartsWith('```')) { $line.Trim() }
    }
}

# The section "Au fil de l'eau" that .github/workflows/quality-audit.yml writes, between its markers.
function Get-DebtAlongTheWay([string] $IssueBody) {
    Get-MarkedLines $IssueBody '<!-- list -->' '<!-- end -->'
}

# The forms the audit writes: "mutant <file> <mutator>: …" and "style <rule> <file>".
function Get-EntryFile([string] $Entry) {
    $words = $Entry -split ' ', 4
    if ($words[0] -eq 'mutant') { $words[1] } else { $words[2] }
}

# What a pull request did with each entry, in its description, one line each between the markers:
# "fixed <entry>", "rejected <entry>" or "deferred #<ticket> <entry>".
function Get-HandledDebt([string] $PullRequestBody) {
    foreach ($line in Get-MarkedLines $PullRequestBody '<!-- quality-debt -->' '<!-- end-quality-debt -->') {
        if ($line -match '^(?<state>fixed|rejected|deferred #\d+) (?<entry>.+)$') {
            [pscustomobject]@{ State = $Matches['state']; Entry = $Matches['entry'] }
        }
    }
}

Export-ModuleMember -Function Invoke-Gh, Get-TrackingIssue, Get-DebtAlongTheWay, Get-EntryFile, Get-HandledDebt
