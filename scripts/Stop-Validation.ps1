#Requires -Version 7.3
<#
.SYNOPSIS
    Stops the validation environment started by Start-Validation.ps1, and removes its data.

.DESCRIPTION
    Validation environment of choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md:
    removes its containers (the technical documentation among them), networks and data, and the sources extracted for it. The images built for it are
    kept: the next validation rebuilds only what its pull request changes.

    Refused while a start or another stop of the environment is in progress, from whatever clone or worktree: it would
    remove the containers and the sources of a start while it builds them.
#>
[CmdletBinding()]
param(
    # Keeps the data (database and visuals) for the next start with -KeepData.
    [switch] $KeepData
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Import-Module (Join-Path $PSScriptRoot 'ValidationEnvironment.psm1') -Force

$lock = Lock-ValidationEnvironment 'a stop'
try {
    $versions = @(Get-ValidationVersion)

    # The project is found by its name, its containers, networks and volumes carrying it.
    $arguments = @('compose', '--project-name', $ProjectName, 'down', '--remove-orphans')
    if (-not $KeepData) {
        $arguments += '--volumes'
    }
    docker @arguments

    # Extracted again at each start.
    $sources = Get-ValidationSources
    if (Test-Path $sources) {
        Remove-Item $sources -Recurse -Force
    }
}
finally {
    Unlock-ValidationEnvironment $lock
}

# What was stopped: the environment may have been started from another clone or worktree.
foreach ($version in $versions) {
    Write-Host "Stopped: $(Format-ValidationVersion $version)"
}
