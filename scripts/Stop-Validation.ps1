#Requires -Version 7.3
<#
.SYNOPSIS
    Stops the validation environment started by Start-Validation.ps1, and removes its data.

.DESCRIPTION
    Validation environment of choix-implementation/deploiement.md § Environnement de validation fonctionnelle.
    The images built for it are kept: the next validation rebuilds only what its pull request changes.
#>
[CmdletBinding()]
param(
    # Keeps the data (database and visuals) for the next start with -KeepData.
    [switch] $KeepData
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

# The project is found by its name, its containers, networks and volumes carrying it.
$arguments = @('compose', '--project-name', 'bdtheque-validation', 'down', '--remove-orphans')
if (-not $KeepData) {
    $arguments += '--volumes'
}
docker @arguments
