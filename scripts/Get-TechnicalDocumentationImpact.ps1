#Requires -Version 7.3
<#
.SYNOPSIS
    Lists the pages of the technical documentation (docs/tech) that the changes of a branch may make wrong, and, with
    -Check, fails when one of them is left unchanged without a stated reason.

.DESCRIPTION
    Each change evaluates its impact on the technical documentation (gestion-projet/gouvernance/documentation-technique.md):
    this script finds the pages that describe a changed file, through the `code` patterns of their front matter and
    their links to the files of the repository, a link to a folder covering what it holds.

    -Check is the check of the CI: a page that describes a changed file and is not changed itself requires a commit of
    the range to carry the Git trailer `Documentation-technique: <reason>`, which says why no page has to change. The
    changed files that no page describes are only reported: the documentation is partial
    (docs/tech/documentation/couverture.md).

    Changed files are compared without rename detection, so that a moved file counts at its old path as at its new one.
    The .speckit, normative, and docs/tech itself are no files a page describes.

.EXAMPLE
    ./scripts/Get-TechnicalDocumentationImpact.ps1
    The pages that the changes of the branch since origin/main touch, uncommitted and untracked changes included.

.EXAMPLE
    ./scripts/Get-TechnicalDocumentationImpact.ps1 -Base HEAD^1 -Head HEAD -Commits HEAD^1..HEAD^2 -Check
    The check of the CI, on the merge commit of a pull request.
#>
[CmdletBinding()]
param(
    # What the changes are compared with.
    [string] $Base = 'origin/main',

    # The commit whose changes are evaluated; without it, the working copy, uncommitted and untracked changes included,
    # compared with the point where it left the base.
    [string] $Head,

    # The commits whose messages may carry the trailer; by default those between the base and the head.
    [string] $Commits,

    # Fails when a page that describes a changed file is unchanged, without a trailer that says why.
    [switch] $Check,

    # Root of the repository: this one by default.
    [string] $Root = (Split-Path $PSScriptRoot -Parent)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$trailer = 'Documentation-technique'
$Root = (Resolve-Path $Root).Path
$documentation = 'docs/tech/'

if ($Head) {
    $changed = @(git -C $Root diff --name-only --no-renames $Base $Head)
    if (-not $Commits) { $Commits = "$Base..$Head" }
}
else {
    $mergeBase = git -C $Root merge-base $Base HEAD
    $changed = @(
        git -C $Root diff --name-only --no-renames $mergeBase
        git -C $Root ls-files --others --exclude-standard
    )
    if (-not $Commits) { $Commits = "$mergeBase..HEAD" }
}
$changed = @($changed | Where-Object { $_ } | Sort-Object -Unique)
$described = @($changed | Where-Object { -not $_.StartsWith($documentation) -and -not $_.StartsWith('.speckit/') })

# Patterns of the front matter: '**' crosses folders, '*' does not.
function ConvertTo-GlobRegex([string] $pattern) {
    $escaped = [regex]::Escape($pattern).Replace('\*\*', "`u{1}").Replace('\*', '[^/]*').Replace("`u{1}", '.*')
    [regex] "^$escaped$"
}

function Get-CodePattern([string] $path) {
    $inHeader = $false; $inList = $false
    foreach ($line in [IO.File]::ReadLines($path)) {
        if ($line -eq '---') { if ($inHeader) { return } else { $inHeader = $true; continue } }
        if (-not $inHeader) { return }
        if ($line -match '^code:') { $inList = $true; continue }
        if ($inList -and $line -match "^\s+-\s+'?(?<glob>[^']+)'?\s*$") { $Matches['glob']; continue }
        $inList = $false
    }
}

# Links to the files of the repository outside docs/tech, out of the fences; a folder covers what it holds.
function Get-LinkPattern([string] $path) {
    $inFence = $false
    foreach ($line in [IO.File]::ReadLines($path)) {
        if ($line -match '^\s*```') { $inFence = -not $inFence; continue }
        if ($inFence) { continue }
        foreach ($link in [regex]::Matches($line, '\]\((?<href>[^)\s#]+)(#[^)]*)?\)')) {
            $href = $link.Groups['href'].Value
            if ($href -match '^[a-z]+:') { continue }
            $target = [IO.Path]::GetFullPath((Join-Path (Split-Path $path -Parent) $href))
            if (-not $target.StartsWith($Root)) { continue }
            $relative = [IO.Path]::GetRelativePath($Root, $target).Replace('\', '/')
            if ($relative.StartsWith($documentation) -or $relative.StartsWith('.speckit/')) { continue }
            if (Test-Path $target -PathType Container) { "$($relative.TrimEnd('/'))/**" } else { $relative }
        }
    }
}

$pages = foreach ($file in Get-ChildItem (Join-Path $Root $documentation) -Recurse -Filter *.md -File -ErrorAction SilentlyContinue) {
    if ($file.Name -eq 'index.md') { continue }
    $patterns = @(@(Get-CodePattern $file.FullName) + @(Get-LinkPattern $file.FullName) | Sort-Object -Unique |
            ForEach-Object { ConvertTo-GlobRegex $_ })
    [pscustomobject]@{
        Page = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
        Patterns = $patterns
    }
}

$impact = @(foreach ($page in $pages) {
        $files = @($described | Where-Object { $f = $_; $page.Patterns | Where-Object { $_.IsMatch($f) } })
        if ($files) {
            [pscustomobject]@{ Page = $page.Page; Updated = $changed -contains $page.Page; Files = $files }
        }
    })
$uncovered = @($described | Where-Object { $f = $_; -not ($impact | Where-Object { $_.Files -contains $f }) })

if ($impact) {
    Write-Host 'Pages that describe a changed file:'
    foreach ($entry in $impact | Sort-Object Page) {
        Write-Host "  $($entry.Page) [$(if ($entry.Updated) { 'updated' } else { 'unchanged' })] <- $($entry.Files -join ', ')"
    }
}
else {
    Write-Host 'No page describes a changed file.'
}
if ($uncovered) {
    Write-Host 'Changed files that no page describes (the documentation is partial; a page to write?):'
    $uncovered | ForEach-Object { Write-Host "  $_" }
}

if (-not $Check) { return }

$unchanged = @($impact | Where-Object { -not $_.Updated })
if (-not $unchanged) {
    Write-Host 'Every page that describes a changed file is changed too.'
    return
}

$reasons = @(git -C $Root log --format="%(trailers:key=$trailer,valueonly,separator=%x0A)" $Commits | Where-Object { $_.Trim() })
if ($reasons) {
    Write-Host "Unchanged pages justified by the trailer '$($trailer):':"
    $reasons | ForEach-Object { Write-Host "  $_" }
    return
}

Write-Error ("These pages describe a changed file and are unchanged: $(($unchanged.Page) -join ', '). Update them, or, if " +
    "none has to change, say why in a commit of the pull request with the trailer '$($trailer): <reason>' " +
    '(docs/tech/documentation/consulter-et-maintenir.md, Maintenir).')
