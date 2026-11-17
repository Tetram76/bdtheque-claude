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

    Generated today: the references between the projects of the solution (Bdtheque.slnx, ProjectReference), and the
    OpenAPI contract of the API (openapi-v1.json), which the endpoints produce in the test host of the API. That host
    applies the migrations to a PostgreSQL database, so Docker is required to write the contract; -Check leaves it out:
    OpenApiContractTests, in the tests of the API, compares it whenever a source changes. The schema of the database
    (schema-de-la-base.md) likewise: read from the catalog of a migrated database by DatabaseSchemaReferenceTests, which
    writes it here and compares it in the tests of the infrastructure, never in -Check.

.EXAMPLE
    ./scripts/Update-TechnicalReferences.ps1

.EXAMPLE
    ./scripts/Update-TechnicalReferences.ps1 -Check

    Checks the pages generated here, the OpenAPI contract and the schema of the database excepted.
#>
[CmdletBinding()]
param(
    # Fails, naming each difference, instead of writing.
    [switch] $Check,

    # Leaves the OpenAPI contract out of the generation: on a machine without Docker, or in a repository without the API.
    [switch] $NoOpenApiContract,

    # Leaves the schema of the database out of the generation: on a machine without Docker, or in a repository without it.
    [switch] $NoDatabaseSchema,

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
    $description = 'Cette page, générée depuis les fichiers de projet, montre quel projet de la solution référence quel autre ; à lire pour un fait exact sur les dépendances entre projets.'
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
            "Cette page est générée par [``Update-TechnicalReferences.ps1``](../../../$generator), qui lit la liste des projets dans [``Bdtheque.slnx``](../../../Bdtheque.slnx) puis les références de projet (``ProjectReference``) déclarées dans chaque fichier de projet ; elle ne se modifie donc jamais à la main. Le rôle de chaque projet, et la façon dont ils communiquent à l'exécution, sont expliqués dans [Projets de la solution](../architecture/projets-de-la-solution.md)."
            ''
            'Les deux schémas suivants sont générés : ils reproduisent exactement les références directes de chaque projet, sans les références transitives, et séparent les projets de l''application de ceux des tests pour rester lisibles.'
            ''
        ))
    $views = @(
        @{ Folder = 'src'; Question = 'Le premier répond à une question : quel projet de l''application référence directement quel autre ?' }
        @{ Folder = 'tests'; Question = 'Le second répond à une autre : quels projets chaque projet de tests référence-t-il directement ?' }
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

# The contract is the document the API host produces (OpenApiContractTests), never a second declaration: the test writes
# it where it is told, then compares it with the versioned file, which it then is.
function Update-OpenApiContract {
    $env:BDTHEQUE_OPENAPI_CONTRACT_OUTPUT = Join-Path $references 'openapi-v1.json'
    try {
        dotnet test (Join-Path $Root 'tests/Bdtheque.Api.Tests') --configuration Release `
            --filter 'FullyQualifiedName~Bdtheque.Api.Tests.OpenApiContractTests' | Out-Host
        if ($LASTEXITCODE -ne 0) { Write-Error 'The OpenAPI contract could not be generated: see the output of the test above.' }
    }
    finally {
        Remove-Item Env:BDTHEQUE_OPENAPI_CONTRACT_OUTPUT
    }
}

# The schema is the one a migrated database gives (DatabaseSchemaReferenceTests), written by that test where it is told.
function Update-DatabaseSchema {
    $env:BDTHEQUE_DATABASE_SCHEMA_OUTPUT = Join-Path $references $databaseSchemaPage
    try {
        dotnet test (Join-Path $Root 'tests/Bdtheque.Infrastructure.Tests') --configuration Release `
            --filter 'FullyQualifiedName~Bdtheque.Infrastructure.Tests.DatabaseSchemaReferenceTests' | Out-Host
        if ($LASTEXITCODE -ne 0) { Write-Error 'The schema of the database could not be generated: see the output of the test above.' }
    }
    finally {
        Remove-Item Env:BDTHEQUE_DATABASE_SCHEMA_OUTPUT
    }
}

# Generated from a migrated database by its test, never by this script itself: neither compared nor left over here.
$databaseSchemaPage = 'schema-de-la-base.md'

$pages = @(New-ProjectDependencyPage)

# The pages this script generated before: those whose front matter names it.
$existing = @(Get-ChildItem $references -Filter *.md -File -ErrorAction SilentlyContinue |
        Where-Object { (Get-Content $_.FullName -TotalCount 12) -contains "generated: '$generator'" } |
        ForEach-Object Name |
        Where-Object { $_ -ne $databaseSchemaPage })

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
if (-not $NoOpenApiContract) { Update-OpenApiContract }
if (-not $NoDatabaseSchema) { Update-DatabaseSchema }
