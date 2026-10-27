#Requires -Version 7.3
<#
.SYNOPSIS
    Starts the version of a pull request on this machine, for its functional validation.

.DESCRIPTION
    Validation environment of choix-implementation/deploiement.md § Environnement de validation fonctionnelle:
    the whole application (frontend, api, db), built from the sources of the pull request by Docker alone,
    with the validation data. Each start begins from an empty database, so that nothing is left from one
    validation to the next: starting again resets the environment. Stopped by Stop-Validation.ps1.

    Requires git and a running Docker service (Docker Desktop, Rancher Desktop), nothing else: neither the
    .NET SDK nor any file to edit.

.EXAMPLE
    ./scripts/Start-Validation.ps1 -PullRequest 245

.EXAMPLE
    ./scripts/Start-Validation.ps1 -PullRequest 245 -Empty
    Without the validation data: the home screen of an empty collection.
#>
[CmdletBinding(DefaultParameterSetName = 'Ref')]
param(
    # The pull request whose head commit is validated.
    [Parameter(Mandatory, ParameterSetName = 'PullRequest', Position = 0)]
    [int] $PullRequest,

    # Or any commit of the repository (branch, tag, hash): the current one by default.
    [Parameter(ParameterSetName = 'Ref')]
    [string] $Ref = 'HEAD',

    # Starts without the validation data.
    [switch] $Empty,

    # Keeps the data of the environment already started, rather than starting again from an empty database.
    [switch] $KeepData
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$repository = Split-Path $PSScriptRoot -Parent
$sources = Join-Path ([IO.Path]::GetTempPath()) 'bdtheque-validation'

if ($PullRequest) {
    git -C $repository fetch --quiet origin "pull/$PullRequest/head"
    $Ref = 'FETCH_HEAD'
}
$commit = git -C $repository rev-parse --verify "$Ref^{commit}"

# The sources of that commit alone, away from the working copy: its uncommitted changes, its branch and its
# other files play no part in what is validated.
if (Test-Path $sources) {
    Remove-Item $sources -Recurse -Force
}
New-Item -ItemType Directory $sources | Out-Null
$archive = Join-Path $sources 'sources.tar'
git -C $repository archive --format=tar --output $archive $commit
tar -xf $archive -C $sources
Remove-Item $archive

$compose = @(
    'compose', '--project-name', 'bdtheque-validation', '--project-directory', $sources,
    '--file', (Join-Path $sources 'docker-compose.yml'), '--file', (Join-Path $sources 'docker/validation/compose.yaml'),
    '--env-file', (Join-Path $sources 'docker/validation/validation.env')
)

if (-not $KeepData) {
    docker @compose down --volumes --remove-orphans
}
docker @compose up --detach --build --wait
if (-not $KeepData -and -not $Empty) {
    docker @compose run --rm --build validation-data
}

$settings = Get-Content (Join-Path $sources 'docker/validation/validation.env') |
    Where-Object { $_ -match '^\w+=' } |
    ConvertFrom-StringData
Write-Host ''
Write-Host "Version validated: $commit"
Write-Host "Application:       http://localhost:$($settings.FRONTEND_PORT)"
Write-Host "API documentation: http://localhost:$($settings.API_PORT)/scalar/v1"
Write-Host 'Stop: ./scripts/Stop-Validation.ps1'
