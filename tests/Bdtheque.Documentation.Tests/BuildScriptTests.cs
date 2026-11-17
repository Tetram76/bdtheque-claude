namespace Bdtheque.Documentation.Tests;

/// <summary>
/// scripts/Build-TechnicalDocumentation.ps1 as a developer runs it on a new machine, where the local tools of
/// dotnet-tools.json were never restored: the command that docs/tech/documentation/consulter-et-maintenir.md gives is
/// enough, with nothing to run before it.
/// </summary>
public sealed class BuildScriptTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"bdtheque-build-script-{Guid.NewGuid():N}");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task Build_OnAMachineWhoseToolsWereNeverRestored_Succeeds()
    {
        var source = Path.Combine(_folder, "docs", "tech");
        Directory.CreateDirectory(source);
        File.Copy(Path.Combine(Repository.Root, "docs", "tech", "docfx.json"), Path.Combine(source, "docfx.json"));
        File.WriteAllText(Path.Combine(source, "index.md"), "# Documentation\n\nUne documentation sans page.\n");
        var output = Path.Combine(_folder, "site");

        // A home of the .NET CLI of its own resolves no local tool yet, as on a new machine. It would also move the folder
        // of the NuGet packages, which stays that of this machine: the test downloads no package the machine already has.
        var environment = new Dictionary<string, string>
        {
            ["DOTNET_CLI_HOME"] = Path.Combine(_folder, "home"),
            ["NUGET_PACKAGES"] = await GlobalPackagesFolderAsync(),
        };
        var build = await Processes.PowerShellAsync(
            Repository.Script("Build-TechnicalDocumentation.ps1"), ["-Source", source, "-Output", output, "-Commit", "fixture"],
            Repository.Root, DocumentationSite.BuildTimeout, environment);

        Assert.True(build.Succeeded, build.Output);
        Assert.True(File.Exists(Path.Combine(output, "index.html")), build.Output);
    }

    private static async Task<string> GlobalPackagesFolderAsync()
    {
        const string label = "global-packages: ";
        var locals = await Processes.RunAsync("dotnet", ["nuget", "locals", "global-packages", "--list"], Repository.Root, TimeSpan.FromSeconds(30));
        var line = locals.Output.Split('\n').Select(text => text.Trim()).FirstOrDefault(text => text.StartsWith(label, StringComparison.Ordinal));
        return locals.Succeeded && line is not null
            ? line[label.Length..]
            : throw new InvalidOperationException($"The folder of the NuGet packages could not be read:{Environment.NewLine}{locals.Output}");
    }
}
