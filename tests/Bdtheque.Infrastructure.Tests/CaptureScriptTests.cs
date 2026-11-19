namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// What scripts/Test-Captures.ps1 -Update says at its end follows the run of its container: the references are announced
/// updated only when the run succeeded, and a failed run says they were not, keeping its exit code. The script runs on a
/// copy of its own, in a temporary root, with a fake <c>docker</c> first on the path: neither the working tree nor Docker
/// is reached.
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed class CaptureScriptTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("bdtheque-captures-tests-").FullName;

    public CaptureScriptTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "scripts"));
        File.Copy(Path.Combine(RepositoryFiles.Root, "scripts", "Test-Captures.ps1"), Path.Combine(_root, "scripts", "Test-Captures.ps1"));
        File.WriteAllText(Path.Combine(_root, "Directory.Packages.props"),
            "<Project><ItemGroup><PackageVersion Include=\"Microsoft.Playwright\" Version=\"1.0.0\" /></ItemGroup></Project>");
    }

    [UnixFact]
    public async Task Update_AnnouncesTheReferencesUpdated_WhenTheRunSucceeds()
    {
        var result = await RunUpdateAsync(containerExitCode: 0);

        Assert.True(result.Succeeded, result.Output);
        Assert.Contains("References updated: review the changes of", result.Output);
    }

    [UnixFact]
    public async Task Update_SaysTheReferencesWereNotUpdated_WhenTheRunFails()
    {
        var result = await RunUpdateAsync(containerExitCode: 1);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("References not updated", result.Output);
        Assert.DoesNotContain("References updated", result.Output);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    // The script starts the container through the path, as Process.Start does: a shell script stands for docker, the pull
    // succeeding and the run ending with the code given.
    private async Task<ProcessResult> RunUpdateAsync(int containerExitCode)
    {
        var bin = Directory.CreateDirectory(Path.Combine(_root, "bin")).FullName;
        var docker = Path.Combine(bin, "docker");
        await File.WriteAllTextAsync(docker, $"#!/bin/sh\ncase \"$1\" in run) exit {containerExitCode} ;; *) exit 0 ;; esac\n");
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(docker, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var path = $"{bin}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";
        return await Processes.PowerShellAsync(
            Path.Combine(_root, "scripts", "Test-Captures.ps1"), ["-Update"], _root, TimeSpan.FromMinutes(1),
            new Dictionary<string, string> { ["PATH"] = path });
    }

    /// <summary>
    /// A fact that needs a shell script to stand for docker, which Process.Start finds on the path only outside Windows,
    /// where it looks for an executable file. The CI runs it on Linux.
    /// </summary>
    private sealed class UnixFactAttribute : FactAttribute
    {
        public UnixFactAttribute()
        {
            if (OperatingSystem.IsWindows())
                Skip = "A shell script stands for docker: Unix only.";
        }
    }
}
