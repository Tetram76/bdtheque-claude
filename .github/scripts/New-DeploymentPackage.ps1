#Requires -Version 7
<#
.SYNOPSIS
    Builds the deployment package of a version: bdtheque-<version>.zip, attached to its release.

.DESCRIPTION
    The package holds what deploying the version takes, without the sources: docker-compose.yml, its images pointing at
    the public images of the version on ghcr.io and nothing left to build, .env.example without the tag of the images,
    the deployment procedure and the notes of the release (gestion-projet/depot/versions-et-releases.md,
    choix-implementation/deploiement/publication-des-images-docker.md). Each change made to the files of the repository
    must apply exactly as expected: a file that no longer has the expected form fails the script rather than producing
    a package that would deploy something else.

.PARAMETER Version
    Number of the version, without its `v` (e.g. 0.1.0).

.PARAMETER ReleaseNotes
    File holding the notes of the release of the version, as published, copied into the package.

.PARAMETER OutputDirectory
    Directory the archive is written to.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $ReleaseNotes,
    [Parameter(Mandatory)] [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($Version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') {
    throw "'$Version' is not the number of a version (<major>.<minor>.<patch>, without its v)."
}

$root = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$name = "bdtheque-$Version"

# Replaces the matches of a pattern, which must match exactly as many times as expected.
function Edit-Exactly([string] $Text, [string] $Pattern, [string] $Replacement, [int] $Count, [string] $File) {
    $found = [regex]::Matches($Text, $Pattern, 'Multiline').Count
    if ($found -ne $Count) {
        throw "$File no longer has the expected form: '$Pattern' matches $found times instead of $Count."
    }
    [regex]::Replace($Text, $Pattern, $Replacement, 'Multiline')
}

$compose = Get-Content -Raw (Join-Path $root 'docker-compose.yml')
$compose = Edit-Exactly $compose 'image: tetram76/bdtheque-(api|frontend):\$\{BDTHEQUE_TAG:-nightly\}' `
    "image: ghcr.io/tetram76/bdtheque-`$1:$Version" 2 'docker-compose.yml'
# Without the sources, a build section could not run: the images are always pulled.
$compose = Edit-Exactly $compose '^    build:\r?\n(?:      .*\r?\n)+' '' 2 'docker-compose.yml'

# The tag is fixed by the compose file of the package: its comment and its variable go together.
$environment = Get-Content -Raw (Join-Path $root '.env.example')
$environment = Edit-Exactly $environment '(?:^#.*\r?\n)*^BDTHEQUE_TAG=.*\r?\n(?:\r?\n)?' '' 1 '.env.example'

# The procedure goes with this version: its links lead to the sources of its tag, never to those of `main`.
$procedure = Get-Content -Raw (Join-Path $root '.github' 'deployment' 'DEPLOIEMENT.md')
$procedure = $procedure.Replace('{{VERSION}}', $Version)
if ($procedure.Contains('{{')) {
    throw '.github/deployment/DEPLOIEMENT.md holds a placeholder other than {{VERSION}}.'
}

# A package is never attached without the notes of its version.
$notes = Get-Content -Raw $ReleaseNotes
if ([string]::IsNullOrWhiteSpace($notes)) {
    throw "The release notes ($ReleaseNotes) are empty."
}

$staging = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName())
try {
    $folder = New-Item -ItemType Directory -Path (Join-Path $staging $name)
    Set-Content -NoNewline -Path (Join-Path $folder 'docker-compose.yml') -Value $compose
    Set-Content -NoNewline -Path (Join-Path $folder '.env.example') -Value $environment
    Set-Content -NoNewline -Path (Join-Path $folder 'DEPLOIEMENT.md') -Value $procedure
    Set-Content -NoNewline -Path (Join-Path $folder 'NOTES-DE-VERSION.md') -Value $notes

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $archive = Join-Path (Resolve-Path $OutputDirectory).Path "$name.zip"
    Remove-Item -Force -ErrorAction Ignore $archive
    # Entries named with forward slashes, under the folder of the version, whatever the system.
    [IO.Compression.ZipFile]::CreateFromDirectory($folder.FullName, $archive, 'Optimal', $true)
    $archive
}
finally {
    Remove-Item -Recurse -Force -ErrorAction Ignore $staging
}
