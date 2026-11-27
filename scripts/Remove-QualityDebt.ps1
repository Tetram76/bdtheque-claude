#Requires -Version 7.3
<#
.SYNOPSIS
    Removes from the tracking issue of the quality harness the entries a merged pull request has fixed or rejected.

.DESCRIPTION
    The tracking issue is a state, not a history: an entry whose treatment is acquired, once the pull request that fixes
    it or carries its rejection is merged on main, is removed from its description at once
    (gestion-projet/revue-tickets/issues.md, tracking issues of the harnesses;
    gestion-projet/depot/dette-qualite-au-fil-de-l-eau.md).

    The description is read just before it is written, and only the lines of the given entries are removed, whatever
    their section: every other line, entries a session has removed meanwhile included, is written back as read. An
    entry that the issue does not hold stops the script before any change, as a mistyped entry would otherwise be left
    in the issue without anyone noticing.

.EXAMPLE
    ./scripts/Remove-QualityDebt.ps1 -Entry 'style IDE0005 src/Bdtheque.Domain/Entities/Album.cs'
#>
[CmdletBinding()]
param(
    # Entries to remove, each in full, as scripts/Get-QualityDebt.ps1 lists them: one per string, or several on as many
    # lines of one string, as copied from its listing, indentation included.
    [Parameter(Mandatory)]
    [string[]] $Entry
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
# gh writes UTF-8, which PowerShell decodes with this encoding: entries with non-ASCII text (→, accents) must be read
# as the issue holds them to be found, and the rest of the description written back unchanged.
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)

# Found by both labels, never by its title: quality-audit alone also designates the chantiers of the harness.
$issue = @(gh issue list --label quality-audit --label harness --state all --limit 1 --json number,body | ConvertFrom-Json)
if (-not $issue) { throw 'No tracking issue of the quality harness (labels quality-audit and harness).' }
$number = $issue[0].number
$lines = $issue[0].body -split '\r?\n'

$wanted = @($Entry -split '\r?\n' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$missing = @($wanted | Where-Object { $_ -notin $lines })
if ($missing) { throw "Not in the tracking issue #${number}: $($missing -join '; ')" }

$file = New-TemporaryFile
try {
    [IO.File]::WriteAllText($file.FullName, (($lines | Where-Object { $_ -notin $wanted }) -join "`n"), [Text.UTF8Encoding]::new($false))
    gh issue edit $number --body-file $file.FullName | Out-Null
}
finally {
    Remove-Item $file.FullName
}
"Removed from the tracking issue #${number}:"
$wanted | ForEach-Object { "  $_" }
