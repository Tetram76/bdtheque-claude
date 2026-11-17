namespace Bdtheque.Documentation.Tests;

/// <summary>
/// scripts/Update-TechnicalReferences.ps1 -Check, as the CI runs it: the generated references are those their sources
/// give, a page changed, missing or left over each failing it, untracked files included.
/// </summary>
public sealed class ReferenceScriptTests : IAsyncLifetime
{
    private const string Page = "docs/tech/references/dependances-des-projets.md";

    private GitFixture _repository = null!;

    public async Task InitializeAsync()
    {
        _repository = await GitFixture.CreateAsync();
        _repository.Write("Bdtheque.slnx",
            "<Solution>\n  <Project Path=\"src/App/App.csproj\" />\n  <Project Path=\"src/Core/Core.csproj\" />\n</Solution>\n");
        _repository.Write("src/App/App.csproj",
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <ProjectReference Include=\"..\\Core\\Core.csproj\" />\n  </ItemGroup>\n</Project>\n");
        _repository.Write("src/Core/Core.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        // A repository without the API, whose contract its tests generate.
        var generation = await RunAsync("-NoOpenApiContract", "-NoDatabaseSchema");
        Assert.True(generation.Succeeded, generation.Output);
    }

    public async Task DisposeAsync() => await _repository.DisposeAsync();

    [Fact]
    public async Task References_JustGenerated_AreThoseOfTheirSources()
    {
        var result = await RunAsync("-Check");

        Assert.True(result.Succeeded, result.Output);
        Assert.Contains("App --> Core", File.ReadAllText(Path.Combine(_repository.Root, Page)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reference_ChangedByHand_IsOutdated()
    {
        File.AppendAllText(Path.Combine(_repository.Root, Page), "Ajouté à la main.\n");

        await AssertFailsWithAsync($"outdated: {Page}");
    }

    [Fact]
    public async Task Source_Changed_MakesTheReferenceOutdated()
    {
        _repository.Write("src/Core/Core.csproj",
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <ProjectReference Include=\"..\\App\\App.csproj\" />\n  </ItemGroup>\n</Project>\n");

        await AssertFailsWithAsync($"outdated: {Page}");
    }

    [Fact]
    public async Task Reference_Deleted_IsMissing()
    {
        File.Delete(Path.Combine(_repository.Root, Page));

        await AssertFailsWithAsync($"missing: {Page}");
    }

    [Fact]
    public async Task GeneratedPage_TheSourcesNoLongerGive_IsLeftOver()
    {
        _repository.Write("docs/tech/references/ancienne.md", "---\ntitle: 'Ancienne'\ngenerated: 'scripts/Update-TechnicalReferences.ps1'\n---\n");

        await AssertFailsWithAsync("left over: docs/tech/references/ancienne.md");
    }

    [Fact]
    public async Task SchemaOfTheDatabase_IsLeftToTheTests()
    {
        _repository.Write("docs/tech/references/schema-de-la-base.md",
            "---\ntitle: 'Schéma de la base'\ngenerated: 'scripts/Update-TechnicalReferences.ps1'\n---\n");

        var result = await RunAsync("-Check");

        Assert.True(result.Succeeded, result.Output);
    }

    private async Task AssertFailsWithAsync(string difference)
    {
        var result = await RunAsync("-Check");

        Assert.False(result.Succeeded);
        Assert.Contains(difference, result.Output, StringComparison.Ordinal);
    }

    private Task<ProcessResult> RunAsync(params string[] arguments) =>
        Processes.PowerShellAsync(Repository.Script("Update-TechnicalReferences.ps1"), ["-Root", _repository.Root, .. arguments],
            _repository.Root, TimeSpan.FromMinutes(1));
}
