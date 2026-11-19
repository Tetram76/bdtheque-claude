#Requires -Version 7.4
<#
.SYNOPSIS
    Starts the version of a pull request on this machine, for its functional validation.

.DESCRIPTION
    Validation environment of choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md:
    the whole application (frontend, api, db), built from the sources of the pull request by Docker alone,
    with the validation data, and the technical documentation of the same commit (docs). Each start begins from an empty database, so that nothing is left from one
    validation to the next: starting again resets the environment. Stopped by Stop-Validation.ps1.

    The machine has one validation environment, whatever the clone or worktree it is started from: a start is refused
    while another start or a stop is in progress, and while the environment runs another version, until it is stopped.

    Requires git and a running Docker service (Docker Desktop, Rancher Desktop), nothing else: neither the
    .NET SDK nor any file to edit.

.EXAMPLE
    ./scripts/Start-Validation.ps1 -PullRequest 245

.EXAMPLE
    ./scripts/Start-Validation.ps1 -PullRequest 245 -Empty
    Without the validation data: the home screen of an empty collection.

.EXAMPLE
    ./scripts/Start-Validation.ps1 -PullRequest 245 -PaleCoverSpotlight
    With the pale cover of the validation data in the spotlight, rather than the edition the application draws.
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
    [switch] $KeepData,

    # Loads the validation data with their pale cover in the spotlight of the days around the start, the most demanding
    # case of the theme « La couverture du jour », rather than leaving the draw to the application.
    [switch] $PaleCoverSpotlight
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
# The spotlight is set by the load of the data, which neither -Empty nor -KeepData runs.
if ($PaleCoverSpotlight -and ($Empty -or $KeepData)) {
    throw '-PaleCoverSpotlight loads the validation data: it cannot go with -Empty nor -KeepData.'
}
Import-Module (Join-Path $PSScriptRoot 'ValidationEnvironment.psm1') -Force
$repository = Split-Path $PSScriptRoot -Parent
$sources = Get-ValidationSources

# Held from the first step to the last: the extraction of the sources, the build of the images and the load of the
# data of a start are those of a single version.
$lock = Lock-ValidationEnvironment "the start of $(if ($PullRequest) { "pull request #$PullRequest" } else { "'$Ref'" })"
try {
    if ($PullRequest) {
        git -C $repository fetch --quiet origin "pull/$PullRequest/head"
        $Ref = 'FETCH_HEAD'
    }
    $commit = git -C $repository rev-parse --verify "$Ref^{commit}"

    # A pull request keeps its version from one commit to the next, so that starting it again after a push resets
    # the environment; any other commit is a version of its own.
    $version = if ($PullRequest) { "pull-request:$PullRequest" } else { "commit:$commit" }
    $other = Get-ValidationVersion | Where-Object { $_ -ne $version } | Select-Object -First 1
    if ($null -ne $other) {
        throw "The validation environment runs $(Format-ValidationVersion $other). Stop it with ./scripts/Stop-Validation.ps1 once its validation is over, then run this command again."
    }

    # The sources of that commit alone, away from the working copy: its uncommitted changes, its branch and its
    # other files play no part in what is validated.
    if (Test-Path $sources) {
        Remove-Item $sources -Recurse -Force
    }
    New-Item -ItemType Directory $sources | Out-Null
    # Streamed from git to tar, with no archive in between to remove: on Windows, a process outside the script (such as
    # the real-time scan of an antivirus) may open a file just written, and removing the archive at once failed now and
    # then, the file "being used by another process" (#552). PowerShell 7.4 passes the bytes of a native command to the
    # next one unchanged.
    git -C $repository archive --format=tar $commit | tar -xf - -C $sources

    # The commit validated, which the technical documentation names on each page (docker/validation/compose.yaml): the
    # extracted sources have no history to read it from.
    $env:SOURCE_COMMIT = $commit

    $compose = @(
        'compose', '--project-name', $ProjectName, '--project-directory', $sources,
        '--file', (Join-Path $sources 'docker-compose.yml'), '--file', (Join-Path $sources 'docker/validation/compose.yaml'),
        '--env-file', (Join-Path $sources 'docker/validation/validation.env')
    )
    $compose += '--file', (New-ValidationVersionOverride $compose $version (Join-Path $sources 'validation-version.yaml'))

    if (-not $KeepData) {
        docker @compose down --volumes --remove-orphans
    }
    docker @compose up --detach --build --wait
    if (-not $KeepData -and -not $Empty) {
        $load = @(if ($PaleCoverSpotlight) { '--pale-cover-spotlight' })
        docker @compose run --rm --build validation-data @load
    }

    # Read before the lock is released, which lets another start replace the sources.
    $settings = Get-Content (Join-Path $sources 'docker/validation/validation.env') |
        Where-Object { $_ -match '^\w+=' } |
        ConvertFrom-StringData
}
finally {
    Unlock-ValidationEnvironment $lock
}

Write-Host ''
# The pull request validated, if any: the commit alone does not tell which one is on screen.
if ($PullRequest) {
    Write-Host "Pull request:      #$PullRequest"
}
Write-Host "Version validated: $commit"
Write-Host "Application:       http://localhost:$($settings.FRONTEND_PORT)"
Write-Host "API documentation: http://localhost:$($settings.API_PORT)/scalar/v1"
# The versions validated before the technical documentation have no service for it.
if ($settings.DOCS_PORT) {
    Write-Host "Technical docs:    http://localhost:$($settings.DOCS_PORT)/"
}
# The account of the administration, fixed and fictitious, for the versions that have one.
if ($settings.ADMIN_LOGIN) {
    Write-Host "Administration:    login '$($settings.ADMIN_LOGIN)', password '$($settings.ADMIN_PASSWORD)'"
}
Write-Host 'Stop: ./scripts/Stop-Validation.ps1'
