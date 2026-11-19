#Requires -Version 7.4
<#
.SYNOPSIS
    The validation environment of this machine, as Start-Validation.ps1 and Stop-Validation.ps1 share it.

.DESCRIPTION
    Validation environment of choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md. Every
    clone and worktree of the repository starts the same environment: one Compose project, fixed ports, fixed image
    names, one folder of extracted sources. Two starts, or a start and a stop, that ran at the same time would mix
    their steps: one would extract its sources, or remove the containers, while the other builds. Each of them
    therefore holds the lock of the environment from its first step to its last, and the containers carry the version
    they run, so that a start never takes over an environment that runs another version.
#>

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

# The Compose project of the environment, whose containers, networks and volumes carry its name.
$ProjectName = 'bdtheque-validation'

# The label that names, on each container, the version it runs (New-ValidationVersionOverride).
$VersionLabel = 'bdtheque.validation.version'

# The lock and the description of its holder, beside the extracted sources: in the temporary folder of the user,
# which every clone and worktree of the repository shares.
$lockPath = Join-Path ([IO.Path]::GetTempPath()) "$ProjectName.lock"
$holderPath = Join-Path ([IO.Path]::GetTempPath()) "$ProjectName.holder"

function Get-ValidationSources {
    <# The folder of the sources extracted for the environment. #>
    Join-Path ([IO.Path]::GetTempPath()) $ProjectName
}

function Lock-ValidationEnvironment {
    <#
    .SYNOPSIS
        Takes the lock of the environment for an operation, or refuses at once when another one holds it.
    .OUTPUTS
        The lock, to release with Unlock-ValidationEnvironment once the operation has ended.
    #>
    param(
        # What the operation is, as a second operation refused tells it.
        [Parameter(Mandatory)] [string] $Operation
    )

    try {
        # FileShare.None: a sharing violation on Windows, an exclusive flock on Linux and macOS. The system releases it
        # when the process ends, however it ends: a start interrupted or killed never leaves the environment locked.
        $lock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    }
    catch [IO.IOException] {
        # Refused rather than waited for: the operation in progress may be a start of another version, which the
        # one refused would then replace while it is being validated.
        $holder = Get-Content $holderPath -Raw -ErrorAction Ignore
        $holder = if ($holder) { $holder.Trim() } else { 'another start or stop of it' }
        throw "The validation environment is busy: $holder is in progress. Run this command again once it has ended."
    }

    Set-Content $holderPath "$Operation, started at $(Get-Date -Format 'HH:mm:ss') from $(Split-Path $PSScriptRoot -Parent) (process $PID)"
    $lock
}

function Unlock-ValidationEnvironment {
    <# Releases the lock that Lock-ValidationEnvironment took. #>
    param([Parameter(Mandatory)] [IO.FileStream] $Lock)

    Remove-Item $holderPath -ErrorAction Ignore
    $Lock.Dispose()
}

function Get-ValidationVersion {
    <#
    .SYNOPSIS
        The versions that the containers of the environment run: none when it is stopped.
    .OUTPUTS
        Each distinct version, empty for a container started before the environment named its version.
    #>
    # The containers that Compose created for the project: those carry com.docker.compose.oneoff, unlike a container
    # created by hand from one of its images, which inherits only the project label that Compose gives the images it builds.
    docker ps --all --filter "label=com.docker.compose.project=$ProjectName" --filter 'label=com.docker.compose.oneoff' --format '{{.Labels}}' |
        ForEach-Object {
            $label = $_ -split ',' | Where-Object { $_.StartsWith("$VersionLabel=") }
            if ($label) { $label.Substring($VersionLabel.Length + 1) } else { '' }
        } |
        Sort-Object -Unique
}

function New-ValidationVersionOverride {
    <#
    .SYNOPSIS
        Writes the Compose file that labels every service of the environment with the version it runs.
    .DESCRIPTION
        Written by the scripts rather than read from the sources of the commit validated: a commit older than the
        lock, or a pull request that has not merged it, has no such label in its own Compose files, and its
        environment would then never be recognised as its own. Only the services that those files declare are
        labelled, the older ones lacking some (e.g. the technical documentation).
    .OUTPUTS
        The path of the file, to add to the Compose files of the environment.
    #>
    param(
        # The arguments of docker that name the project and its Compose files.
        [Parameter(Mandatory)] [string[]] $Compose,
        [Parameter(Mandatory)] [string] $Version,
        [Parameter(Mandatory)] [string] $Path
    )

    # The services of every profile, the data loader among them.
    $services = docker @Compose --profile '*' config --services
    $lines = @('services:') + @($services | ForEach-Object { "  ${_}:", '    labels:', "      ${VersionLabel}: '$Version'" })
    Set-Content $Path $lines
    $Path
}

function Format-ValidationVersion {
    <# A version of the environment, as the user reads it. #>
    param([AllowEmptyString()] [string] $Version)

    switch -Regex ($Version) {
        '^pull-request:(\d+)$' { "pull request #$($Matches[1])" }
        '^commit:(\w+)$' { "commit $($Matches[1])" }
        default { 'a version started before the environment named its version' }
    }
}

Export-ModuleMember -Function * -Variable ProjectName
