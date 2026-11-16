namespace Bdtheque.Documentation.Tests;

/// <summary>
/// scripts/Get-TechnicalDocumentationImpact.ps1 -Check, as the CI runs it on a pull request: a page that describes a
/// changed file, by a pattern of its front matter or by a link, is changed too, or a commit says why it need not be.
/// </summary>
public sealed class ImpactScriptTests : IAsyncLifetime
{
    private GitFixture _git = null!;

    public async Task InitializeAsync()
    {
        _git = await GitFixture.CreateAsync();
        _git.Write("src/Gates/Gate.cs", "class Gate { }");
        _git.Write("src/Linked.cs", "class Linked { }");
        _git.Write("src/Other.cs", "class Other { }");
        _git.Write("docs/tech/index.md", "# Documentation\n\n* [Portes](portes.md) - Les portes.\n");
        _git.Write("docs/tech/portes.md",
            "---\ntitle: 'Portes'\ndescription: 'Les portes.'\ncode:\n  - 'src/Gates/**'\n---\n\n# Portes\n\nVoir [Linked](../../src/Linked.cs) et le [README](../../README.md).\n");
        _git.Write(".speckit/regle.md", "# Règle\n");
        _git.Write("README.md", "# Projet\n");
        await _git.CommitAsync("base");
        await _git.GitAsync("switch", "--quiet", "--create", "change");
    }

    public async Task DisposeAsync() => await _git.DisposeAsync();

    [Fact]
    public async Task FileDescribedByAPattern_ChangedAlone_FailsNamingThePage()
    {
        _git.Write("src/Gates/Gate.cs", "class Gate { bool Open; }");
        await _git.CommitAsync("change the gate");

        var result = await CheckAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("docs/tech/portes.md [unchanged] <- src/Gates/Gate.cs", result.Output, StringComparison.Ordinal);
        Assert.Contains("Documentation-technique", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileDescribedByALink_ChangedAlone_Fails()
    {
        _git.Write("src/Linked.cs", "class Linked { int Count; }");
        await _git.CommitAsync("change the linked file");

        var result = await CheckAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("docs/tech/portes.md [unchanged] <- src/Linked.cs", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileChanged_WithItsPage_Succeeds()
    {
        _git.Write("src/Gates/Gate.cs", "class Gate { bool Open; }");
        _git.Write("docs/tech/portes.md",
            "---\ntitle: 'Portes'\ndescription: 'Les portes.'\ncode:\n  - 'src/Gates/**'\n---\n\n# Portes\n\nUne porte s'ouvre.\n");
        await _git.CommitAsync("change the gate and its page");

        var result = await CheckAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Contains("docs/tech/portes.md [updated]", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileChanged_WithATrailerThatSaysWhy_Succeeds()
    {
        _git.Write("src/Gates/Gate.cs", "class Gate { }\n");
        await _git.CommitAsync("style: final newline\n\nDocumentation-technique: fin de fichier seule, rien de décrit ne change.");

        var result = await CheckAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Contains("fin de fichier seule, rien de décrit ne change.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileMoved_CountsAtItsFormerPath()
    {
        Directory.CreateDirectory(Path.Combine(_git.Root, "src", "Moved"));
        await _git.GitAsync("mv", "src/Gates/Gate.cs", "src/Moved/Gate.cs");
        await _git.CommitAsync("move the gate");

        var result = await CheckAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("docs/tech/portes.md [unchanged] <- src/Gates/Gate.cs", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileDeleted_CountsLikeAChange()
    {
        await _git.GitAsync("rm", "--quiet", "src/Gates/Gate.cs");
        await _git.CommitAsync("delete the gate");

        Assert.False((await CheckAsync()).Succeeded);
    }

    [Fact]
    public async Task FileThatNoPageDescribes_IsReportedWithoutFailing()
    {
        _git.Write("src/Other.cs", "class Other { int Size; }");
        _git.Write("src/New.cs", "class New { }");
        await _git.CommitAsync("change files no page describes");

        var result = await CheckAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Contains("Changed files that no page describes", result.Output, StringComparison.Ordinal);
        Assert.Contains("src/New.cs", result.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(".speckit/regle.md")]
    [InlineData("README.md")]
    public async Task DocumentChanged_IsNoFileThePagesDescribe(string document)
    {
        _git.Write(document, "# Modifié\n");
        await _git.CommitAsync("change a document");

        var result = await CheckAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Contains("No page describes a changed file.", result.Output, StringComparison.Ordinal);
    }

    private Task<ProcessResult> CheckAsync() =>
        Processes.PowerShellAsync(Repository.Script("Get-TechnicalDocumentationImpact.ps1"),
            ["-Root", _git.Root, "-Base", "main", "-Head", "HEAD", "-Check"], _git.Root, TimeSpan.FromMinutes(1));
}
