using System.Diagnostics;

namespace Bdtheque.Documentation.Tests;

/// <summary>The result of a process: its exit code and everything it wrote, output and errors interleaved.</summary>
internal sealed record ProcessResult(int ExitCode, string Output)
{
    public bool Succeeded => ExitCode == 0;
}

/// <summary>
/// Runs the scripts of the repository and git as the agent and the CI run them: each run is bounded, and a process that
/// outlives its bound is killed with its children, never left running.
/// </summary>
internal static class Processes
{
    public static Task<ProcessResult> PowerShellAsync(
        string script, IEnumerable<string> arguments, string workingDirectory, TimeSpan timeout, IReadOnlyDictionary<string, string>? environment = null) =>
        RunAsync("pwsh", ["-NoLogo", "-NoProfile", "-NonInteractive", "-File", script, .. arguments], workingDirectory, timeout, environment);

    public static async Task<string> GitAsync(string repository, params string[] arguments)
    {
        var result = await RunAsync("git", arguments, repository, TimeSpan.FromSeconds(30));
        return result.Succeeded ? result.Output : throw new InvalidOperationException($"git {string.Join(' ', arguments)}: {result.Output}");
    }

    public static async Task<ProcessResult> RunAsync(
        string program, IEnumerable<string> arguments, string workingDirectory, TimeSpan timeout, IReadOnlyDictionary<string, string>? environment = null)
    {
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // What git and the scripts write, French text included, whatever the code page of the machine.
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        // Messages of git and of the scripts in English whatever the machine, for the assertions that read them.
        start.Environment["LC_ALL"] = "C";
        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
            start.Environment[name] = value;

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"{program} could not be started.");
        var output = new System.Text.StringBuilder();
        var gate = new Lock();
        process.OutputDataReceived += (_, line) => { lock (gate) output.AppendLine(line.Data); };
        process.ErrorDataReceived += (_, line) => { lock (gate) output.AppendLine(line.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var deadline = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{program} {string.Join(' ', start.ArgumentList)} did not end within {timeout}.");
        }

        // Every line read once the process has ended (documentation of Process.WaitForExit).
        await process.WaitForExitAsync(CancellationToken.None);
        lock (gate)
            return new ProcessResult(process.ExitCode, output.ToString());
    }
}
