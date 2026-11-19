using System.Text.RegularExpressions;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The machine has one validation environment, whatever the clone or worktree it is started from: a start or a stop
/// refuses to run while another one holds the lock of the environment, before it touches the sources, the images or the
/// containers that the other one is building (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md).
/// Neither git nor Docker is reached: the refusal comes first.
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed partial class ValidationEnvironmentLockTests : IDisposable
{
    private const string Holder = "the start of pull request #1, started at 18:59:02 from another worktree (process 1)";

    // A temporary folder of its own, as the scripts see it: the lock, its holder and the sources are there.
    private readonly string _temp = Directory.CreateTempSubdirectory("bdtheque-lock-tests-").FullName;

    [Theory]
    [InlineData("Start-Validation.ps1")]
    [InlineData("Stop-Validation.ps1")]
    public async Task Operation_IsRefused_WhileAnotherOneHoldsTheLock(string script)
    {
        var sourcesOfTheOther = Path.Combine(_temp, "bdtheque-validation", "docker-compose.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcesOfTheOther)!);
        await File.WriteAllTextAsync(sourcesOfTheOther, "sources of the start in progress");
        await File.WriteAllTextAsync(Path.Combine(_temp, "bdtheque-validation.holder"), Holder);

        ProcessResult result;
        using (File.Open(Path.Combine(_temp, "bdtheque-validation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            result = await RunAsync(script);

        Assert.False(result.Succeeded, result.Output);
        // PowerShell wraps the message of an error at the width of its output.
        Assert.Contains($"The validation environment is busy: {Holder} is in progress.", Whitespace().Replace(result.Output, " "));
        Assert.Equal("sources of the start in progress", await File.ReadAllTextAsync(sourcesOfTheOther));
    }

    [Fact]
    public async Task Lock_IsReleased_WhenTheOperationEnds()
    {
        // A stop refused by Docker still releases the lock it took: the next operation is not refused as busy.
        var environment = new Dictionary<string, string>(TemporaryFolder) { ["PATH"] = _temp };

        var first = await RunAsync("Stop-Validation.ps1", environment);
        var second = await RunAsync("Stop-Validation.ps1", environment);

        Assert.False(first.Succeeded, first.Output);
        Assert.DoesNotContain("busy", second.Output);
        Assert.False(File.Exists(Path.Combine(_temp, "bdtheque-validation.holder")));
    }

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    // The temporary folder of .NET: TMP (then TEMP) on Windows, TMPDIR on Linux and macOS.
    private Dictionary<string, string> TemporaryFolder => new() { ["TMP"] = _temp, ["TEMP"] = _temp, ["TMPDIR"] = _temp };

    private Task<ProcessResult> RunAsync(string script, IReadOnlyDictionary<string, string>? environment = null) =>
        Processes.PowerShellAsync(
            Path.Combine(RepositoryFiles.Root, "scripts", script), [], RepositoryFiles.Root, TimeSpan.FromMinutes(1),
            environment ?? TemporaryFolder);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
