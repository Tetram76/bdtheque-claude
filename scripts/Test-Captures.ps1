#Requires -Version 7.3
<#
.SYNOPSIS
    Compares the captures of the screens with their references, or updates the references, in a Linux container.

.DESCRIPTION
    Reference captures of choix-implementation/socle/themes.md: the screens are captured in the
    environment of the CI (Linux, the Chromium of Playwright), the only one where a same page always gives the same
    pixels; on another system, the capture tests are skipped. This script runs them in the official image of Playwright
    for .NET, at the version of the Microsoft.Playwright package, whose Chromium and its system dependencies are
    installed already, on a copy of the working tree, uncommitted changes included.

    A failed comparison leaves the capture taken and its difference image in
    tests/Bdtheque.EndToEnd.Tests/TestResults/captures. With -Update, the captures replace the references, to be
    reviewed before they are committed: the pull request then shows each changed capture before and after.

    Requires a running Docker service (Docker Desktop, Rancher Desktop), whose socket the container uses to start the
    PostgreSQL database of the tests.

.EXAMPLE
    ./scripts/Test-Captures.ps1

.EXAMPLE
    ./scripts/Test-Captures.ps1 -Update

.EXAMPLE
    ./scripts/Test-Captures.ps1 -Update -Filter admin-logout, AdminHome

    Takes again only the captures of the logout page and of the home page of the administration, in every theme and
    width; the other references are left as they are.
#>
[CmdletBinding()]
param(
    # Makes the captures taken the new references, instead of comparing them.
    [switch] $Update,

    # Takes only the captures whose test name contains one of these texts: a screen of ScreenCaptureTests
    # ('admin-logout'), a class of its own ('AdminHome'), a theme ('gazette'). All the captures when omitted.
    [string[]] $Filter
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path $PSScriptRoot -Parent
$project = 'tests/Bdtheque.EndToEnd.Tests'

# The test filter of VSTest: the captures, narrowed to the names that contain one of the texts asked. Its operators and
# the quotes of the shell are refused rather than escaped: a name of a screen, a class or a theme never has them.
$testFilter = 'FullyQualifiedName~Bdtheque.EndToEnd.Tests.Captures.'
if ($Filter) {
    $invalid = $Filter | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -match "[()&|=!~\\'`"]" }
    if ($invalid) { throw "Invalid capture filter: '$($invalid -join "', '")'." }
    $testFilter += '&(' + (($Filter | ForEach-Object { "DisplayName~$_" }) -join '|') + ')'
}

# The image of Playwright at the version of the package that the tests drive: Playwright finds only the browsers of its
# own version. Its .NET SDK follows Playwright, not the SDK of the Dockerfiles, which global.json accepts.
$playwright = Select-String -Path (Join-Path $root 'Directory.Packages.props') -Pattern 'Include="Microsoft\.Playwright" Version="([^"]+)"' |
    Select-Object -First 1 | ForEach-Object { $_.Matches[0].Groups[1].Value }
if (-not $playwright) { throw 'The version of Microsoft.Playwright was not found in Directory.Packages.props.' }

# Bounds of a run (gestion-projet/gouvernance/regles-de-travail-de-l-agent.md, durée d'une commande). A test longer than a
# minute is stuck: a capture takes about a second, the longest test a few seconds, and each action of the browser gives up
# after 30 s; VSTest then stops the run. The whole run, from the start of the container to its end, lasts about a minute
# and a half (copy and build, 40 s; the 78 captures, 30 s), three minutes when the packages of the NuGet volume are
# restored again: past four minutes and a half, the container is stopped, whatever holds it (e.g. a Docker engine that no
# longer answers), rather than left running without an end.
$hangTimeout = '1min'
$runTimeout = [TimeSpan]::FromMinutes(4.5)

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
status=0
dotnet test $project --configuration Release --no-build --filter '$testFilter' --blame-hang-timeout $hangTimeout || status=`$?
if [ -d $project/TestResults/captures ]; then mkdir -p /repo/$project/TestResults && cp -r $project/TestResults/captures /repo/$project/TestResults/; fi
if [ "$($Update.IsPresent)" = "True" ]; then rm -rf /repo/$project/Captures/References && cp -r $project/Captures/References /repo/$project/Captures/; fi
exit `$status
"@ -replace "`r", ''

$image = "mcr.microsoft.com/playwright/dotnet:v$playwright-noble"
# Pulled apart from the run, which its bound does not cover: a first pull downloads well over a gigabyte.
docker pull --quiet $image | Out-Null

$container = "bdtheque-captures-$PID"
$arguments = @(
    'run', '--rm', '--name', $container,
    # As the documentation of Playwright recommends for its image: an init process, which reaps the processes of the
    # browser, and the shared memory of the host, without which Chromium runs out of memory and crashes.
    '--init', '--ipc=host',
    '--volume', "${root}:/repo",
    '--volume', '/var/run/docker.sock:/var/run/docker.sock',
    '--volume', 'bdtheque-captures-nuget:/root/.nuget/packages',
    # The database of the tests is a sibling container, reached through the ports it publishes on the host.
    '--add-host', 'host.docker.internal:host-gateway',
    '--env', 'TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal',
    '--env', "BDTHEQUE_UPDATE_CAPTURES=$(if ($Update) { '1' } else { '0' })",
    $image,
    'bash', '-c', $inside
)

$PSNativeCommandUseErrorActionPreference = $false
# Each argument passed as it is (the script of the container spans several lines), the output left on the console.
$startInfo = [System.Diagnostics.ProcessStartInfo]::new('docker')
$arguments | ForEach-Object { $startInfo.ArgumentList.Add($_) }
$run = [System.Diagnostics.Process]::Start($startInfo)
try {
    if (-not $run.WaitForExit($runTimeout)) {
        docker kill $container | Out-Null
        $run.WaitForExit()
        throw "The captures did not end within $($runTimeout): the container was stopped. Check the Docker engine before running them again."
    }
    $status = $run.ExitCode
}
finally {
    $run.Dispose()
}

if ($Update) {
    Write-Host 'References updated: review the changes of' (Join-Path $project 'Captures/References') 'before committing them.'
}
elseif ($status -ne 0 -and (Test-Path $results)) {
    Write-Host "Captures that differ from their reference, with their difference image: $results"
}
exit $status
