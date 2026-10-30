#Requires -Version 7.3
<#
.SYNOPSIS
    Compares the captures of the screens with their references, or updates the references, in a Linux container.

.DESCRIPTION
    Reference captures of choix-implementation/socle.md § Thèmes : mise en œuvre: the screens are captured in the
    environment of the CI (Linux, the Chromium of Playwright), the only one where a same page always gives the same
    pixels; on another system, the capture tests are skipped. This script runs them in the SDK image of the
    Dockerfiles, on a copy of the working tree, uncommitted changes included.

    A failed comparison leaves the capture taken and its difference image in
    tests/Bdtheque.EndToEnd.Tests/TestResults/captures. With -Update, the captures replace the references, to be
    reviewed before they are committed: the pull request then shows each changed capture before and after.

    Requires a running Docker service (Docker Desktop, Rancher Desktop), whose socket the container uses to start the
    PostgreSQL database of the tests.

.EXAMPLE
    ./scripts/Test-Captures.ps1

.EXAMPLE
    ./scripts/Test-Captures.ps1 -Update
#>
[CmdletBinding()]
param(
    # Makes the captures taken the new references, instead of comparing them.
    [switch] $Update
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path $PSScriptRoot -Parent
$project = 'tests/Bdtheque.EndToEnd.Tests'

# The SDK of the Dockerfiles, which SdkVersionTests keeps aligned with the CI.
$sdk = Select-String -Path (Join-Path $root 'docker/api/Dockerfile') -Pattern 'mcr\.microsoft\.com/dotnet/sdk:(\S+)' |
    Select-Object -First 1 | ForEach-Object { $_.Matches[0].Groups[1].Value }
if (-not $sdk) { throw 'The SDK image of docker/api/Dockerfile was not found.' }

$results = Join-Path $root "$project/TestResults/captures"
if (Test-Path $results) { Remove-Item $results -Recurse -Force }

# Copied rather than built in place: the build outputs of the machine stay out of the container, and the container's
# out of the working tree. Only the references (with -Update) and the results of a failed comparison come back.
$inside = @"
set -e
mkdir -p /work
tar -C /repo --exclude=./.git --exclude='*/bin' --exclude='*/obj' --exclude='*/TestResults' -cf - . | tar -C /work -xf -
cd /work
dotnet build $project --configuration Release
pwsh $project/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
status=0
dotnet test $project --configuration Release --no-build --filter 'FullyQualifiedName~Bdtheque.EndToEnd.Tests.Captures.' || status=`$?
if [ -d $project/TestResults/captures ]; then mkdir -p /repo/$project/TestResults && cp -r $project/TestResults/captures /repo/$project/TestResults/; fi
if [ "$($Update.IsPresent)" = "True" ]; then rm -rf /repo/$project/Captures/References && cp -r $project/Captures/References /repo/$project/Captures/; fi
exit `$status
"@ -replace "`r", ''

$arguments = @(
    'run', '--rm',
    '--volume', "${root}:/repo",
    '--volume', '/var/run/docker.sock:/var/run/docker.sock',
    '--volume', 'bdtheque-captures-nuget:/root/.nuget/packages',
    '--volume', 'bdtheque-captures-playwright:/root/.cache/ms-playwright',
    # The database of the tests is a sibling container, reached through the ports it publishes on the host.
    '--add-host', 'host.docker.internal:host-gateway',
    '--env', 'TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal',
    '--env', "BDTHEQUE_UPDATE_CAPTURES=$(if ($Update) { '1' } else { '0' })",
    "mcr.microsoft.com/dotnet/sdk:$sdk",
    'bash', '-c', $inside
)

$PSNativeCommandUseErrorActionPreference = $false
docker @arguments
$status = $LASTEXITCODE

if ($Update) {
    Write-Host 'References updated: review the changes of' (Join-Path $project 'Captures/References') 'before committing them.'
}
elseif ($status -ne 0 -and (Test-Path $results)) {
    Write-Host "Captures that differ from their reference, with their difference image: $results"
}
exit $status
