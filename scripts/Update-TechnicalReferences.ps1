#Requires -Version 7.3
<#
.SYNOPSIS
    Generates the references of the technical documentation (docs/tech/references) from the sources that are
    authoritative for them, or checks that the versioned references are those the sources give.

.DESCRIPTION
    A generated reference is never written by hand (choix-implementation/documentation-technique.md): it is produced
    here, committed with the change that alters its source, and checked by the CI, which runs this script with -Check.
    The check compares the whole set of generated pages: a page that differs, one missing, and one left over that the
    sources no longer produce (a page carrying `generated:` in its front matter) each fail it, untracked files
    included, since the comparison reads the folder itself.

    Generated today: the references between the projects of the solution (Bdtheque.slnx, ProjectReference).

.EXAMPLE
    ./scripts/Update-TechnicalReferences.ps1

.EXAMPLE
    ./scripts/Update-TechnicalReferences.ps1 -Check
#>
[CmdletBinding()]
param(
    # Fails, naming each difference, instead of writing.
    [switch] $Check,

    # Root of the repository whose references are generated: this one by default.
    [string] $Root = (Split-Path $PSScriptRoot -Parent)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = (Resolve-Path $Root).Path
$references = Join-Path $Root 'docs/tech/references'
$generator = 'scripts/Update-TechnicalReferences.ps1'

function Get-ProjectGraph {
    $solution = [xml] (Get-Content -Raw (Join-Path $Root 'Bdtheque.slnx'))
    foreach ($path in @($solution.SelectNodes('//Project') | ForEach-Object { $_.GetAttribute('Path') }) | Sort-Object) {
        $file = Join-Path $Root $path
        $project = [xml] (Get-Content -Raw $file)
        $referenced = @($project.SelectNodes('//ProjectReference') | ForEach-Object {
                [IO.Path]::GetFileNameWithoutExtension($_.GetAttribute('Include').Replace('\', '/'))
            } | Sort-Object -Unique)
        [pscustomobject]@{
            Name = [IO.Path]::GetFileNameWithoutExtension($path)
            Folder = $path.Split('/')[0]
            References = $referenced
        }
    }
}

function New-ProjectDependencyPage {
    $projects = @(Get-ProjectGraph)
    $id = { param($name) $name.Replace('.', '_') }
    $description = 'Références entre les projets de la solution, générées depuis ses fichiers de projet ; pour un fait exact sur qui référence qui.'
    $lines = [Collections.Generic.List[string]]::new()
    $lines.AddRange([string[]] @(
            '---'
            "title: 'Dépendances des projets'"
            "description: '$description'"
            'code:'
            "  - 'Bdtheque.slnx'"
            "  - 'src/*/*.csproj'"
            "  - 'tests/*/*.csproj'"
            "generated: '$generator'"
            '---'
            ''
            '# Dépendances des projets'
            ''
            "Page générée par [``Update-TechnicalReferences.ps1``](../../../$generator) depuis [``Bdtheque.slnx``](../../../Bdtheque.slnx) et les références (``ProjectReference``) de chaque fichier de projet : elle ne se modifie pas à la main. Le rôle de chaque projet est décrit dans [Projets de la solution](../architecture/projets-de-la-solution.md)."
            ''
            'Schémas générés : ils reproduisent exactement les références directes, sans les références transitives, en deux vues pour rester lisibles.'
            ''
        ))
    $views = @(
        @{ Folder = 'src'; Question = 'Question du schéma : *quel projet de l''application référence directement quel autre ?*' }
        @{ Folder = 'tests'; Question = 'Question du schéma : *quels projets chaque projet de tests référence-t-il directement ?*' }
    )
    foreach ($view in $views) {
        $sources = @($projects | Where-Object Folder -EQ $view.Folder)
        if (-not $sources) { continue }
        $lines.AddRange([string[]] @($view.Question, '', '```mermaid', 'flowchart LR'))
        $shown = @($sources.Name) + @($sources.References) | Sort-Object -Unique
        foreach ($name in $shown) { $lines.Add("    $(& $id $name)[""$name""]") }
        foreach ($project in $sources) {
            foreach ($reference in $project.References) { $lines.Add("    $(& $id $project.Name) --> $(& $id $reference)") }
        }
        $lines.AddRange([string[]] @('```', ''))
    }
    $lines.AddRange([string[]] @(
            '| Projet | Dossier | Référence |'
            '| --- | --- | --- |'
        ))
    foreach ($project in $projects) {
        $referenced = if ($project.References) { ($project.References | ForEach-Object { "``$_``" }) -join ', ' } else { '—' }
        $lines.Add("| ``$($project.Name)`` | ``$($project.Folder)`` | $referenced |")
    }
    [pscustomobject]@{ Name = 'dependances-des-projets.md'; Content = ($lines -join "`n") + "`n" }
}

$pages = @(New-ProjectDependencyPage)

# The pages this script generated before: those whose front matter names it.
$existing = @(Get-ChildItem $references -Filter *.md -File -ErrorAction SilentlyContinue |
        Where-Object { (Get-Content $_.FullName -TotalCount 12) -contains "generated: '$generator'" } |
        ForEach-Object Name)

if ($Check) {
    $differences = @(
        foreach ($page in $pages) {
            $file = Join-Path $references $page.Name
            if (-not (Test-Path $file)) { "missing: docs/tech/references/$($page.Name)" }
            elseif ([IO.File]::ReadAllText($file).Replace("`r`n", "`n") -cne $page.Content) { "outdated: docs/tech/references/$($page.Name)" }
        }
        foreach ($name in $existing | Where-Object { $_ -notin $pages.Name }) { "left over: docs/tech/references/$name" }
    )
    if ($differences) {
        $differences | ForEach-Object { Write-Host $_ }
        Write-Error "The generated references differ from their sources: run $generator and commit the result."
    }
    Write-Host 'The generated references are those of their sources.'
    return
}

New-Item -ItemType Directory $references -Force | Out-Null
foreach ($page in $pages) {
    [IO.File]::WriteAllText((Join-Path $references $page.Name), $page.Content, [Text.UTF8Encoding]::new($false))
}
foreach ($name in $existing | Where-Object { $_ -notin $pages.Name }) {
    Remove-Item (Join-Path $references $name)
}
