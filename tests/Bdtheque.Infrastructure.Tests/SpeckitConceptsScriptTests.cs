namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// scripts/Get-SpeckitConcepts.ps1 as an agent runs it before each push (AGENTS.md), its output redirected: every
/// concept whose applies_to covers a changed file is written, with each of those files in full, whatever the width of a
/// console, which a redirected output does not have (gestion-projet/gouvernance/maintenance-du-speckit.md, controls).
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed class SpeckitConceptsScriptTests : IAsyncLifetime
{
    // Longer than any console line, so that a table cut to a console's width would lose it.
    private const string LongPath =
        "src/Bdtheque.Frontend/Components/Administration/Editions/Visuals/EditionVisualsUploadPanelWithItsPreview.razor";

    private GitFixture _git = null!;

    public async Task InitializeAsync()
    {
        _git = await GitFixture.CreateAsync();
        _git.Write(".speckit/index.md", "# Spec\n");
        _git.Write(".speckit/frontend.md",
            "---\ntype: Choix d'implémentation\ntitle: 'Frontend'\ndescription: 'Le frontend.'\napplies_to:\n  - 'src/Bdtheque.Frontend/**'\n---\n\n# Frontend\n");
        _git.Write(".speckit/outillage.md",
            "---\ntype: Choix d'implémentation\ntitle: 'Outillage'\ndescription: 'Les tests.'\napplies_to:\n  - 'tests/**'\n---\n\n# Outillage\n");
        _git.Write("src/Bdtheque.Frontend/App.razor", "<p>App</p>\n");
        _git.Write("README.md", "# Projet\n");
        await _git.CommitAsync("base");
        await _git.GitAsync("switch", "--quiet", "--create", "change");
    }

    public async Task DisposeAsync() => await _git.DisposeAsync();

    [Fact]
    public async Task ChangedFiles_AreListedUnderTheConceptThatGovernsThem()
    {
        _git.Write(LongPath, "<p>Visuals</p>\n");
        _git.Write("src/Bdtheque.Frontend/App.razor", "<p>App changed</p>\n");
        await _git.CommitAsync("change the frontend");
        // Uncommitted and untracked changes count too: they are pushed once committed.
        _git.Write("tests/Bdtheque.Frontend.Tests/AppTests.cs", "class AppTests { }\n");

        var result = await RunAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Equal(
            [
                "Concepts that govern a changed file (each to read before pushing):",
                "frontend.md",
                "  src/Bdtheque.Frontend/App.razor",
                $"  {LongPath}",
                "outillage.md",
                "  tests/Bdtheque.Frontend.Tests/AppTests.cs",
            ],
            Lines(result.Output));
    }

    [Fact]
    public async Task NoGovernedFile_IsSaidExplicitly()
    {
        _git.Write("README.md", "# Projet modifié\n");
        await _git.CommitAsync("change a file no concept governs");

        var result = await RunAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Equal(["No concept governs a changed file."], Lines(result.Output));
    }

    // The indentation of a file under its concept is kept; blank lines are not part of the listing.
    private static string[] Lines(string output) =>
        [.. output.Split('\n').Select(line => line.TrimEnd()).Where(line => line.Length > 0)];

    private Task<ProcessResult> RunAsync() =>
        Processes.PowerShellAsync(
            Path.Combine(RepositoryFiles.Root, "scripts", "Get-SpeckitConcepts.ps1"), ["-Base", "main"], _git.Root,
            TimeSpan.FromMinutes(1));
}
