# PowerShell counterpart of `timeout <seconds> gh …`, which PowerShell lacks:
# dot-source this file, then call Invoke-GhBounded. Returns gh's output lines;
# a failed or hung call throws, so it is never read as data.
function Invoke-GhBounded {
    param(
        [Parameter(Mandatory)][double] $Seconds,
        [Parameter(Mandatory)][string[]] $GhArgs
    )
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new('gh')
    # ArgumentList: jq filters and bodies reach gh as-is, without shell quoting
    foreach ($argument in $GhArgs) { $startInfo.ArgumentList.Add($argument) }
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $startInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    # gh expands {owner}/{repo} from the repository of its working directory
    $startInfo.WorkingDirectory = (Get-Location).ProviderPath
    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        # both streams read asynchronously, to avoid a deadlock on a full pipe
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit([timespan]::FromSeconds($Seconds))) {
            $process.Kill($true)
            $process.WaitForExit()
            throw "gh timed out after $Seconds s"
        }
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "gh failed ($($process.ExitCode)): $($stderr.Result.Trim())" }
        @($stdout.Result -split "`r?`n" | Where-Object { $_ -ne '' })
    }
    finally {
        $process.Dispose()
    }
}
