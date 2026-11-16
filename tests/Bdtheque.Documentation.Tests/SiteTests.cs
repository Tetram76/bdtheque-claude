namespace Bdtheque.Documentation.Tests;

/// <summary>The site of this repository's docs/tech, as the validation environment serves it.</summary>
public sealed class RepositorySite : IAsyncLifetime
{
    public const string Commit = "0123abcd";

    internal DocumentationSite Site { get; private set; } = null!;

    public async Task InitializeAsync() => Site = await DocumentationSite.BuildAsync(Path.Combine(Repository.Root, "docs", "tech"), Commit);

    public async Task DisposeAsync() => await Site.DisposeAsync();
}

/// <summary>
/// The site of the technical documentation (choix-implementation/documentation-technique.md): built without a warning of
/// DocFX (a broken link among others, which fails the build of the fixture), every diagram rendered and readable, nothing
/// requested off the machine, and the version documented named on each page.
/// </summary>
public sealed class SiteTests(RepositorySite fixture) : IClassFixture<RepositorySite>
{
    public static TheoryData<string> Pages() =>
        [.. Directory.EnumerateFiles(Path.Combine(Repository.Root, "docs", "tech"), "*.md", SearchOption.AllDirectories)
            .Select(page => Path.ChangeExtension(Path.GetRelativePath(Path.Combine(Repository.Root, "docs", "tech"), page), ".html").Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Page_RendersItsDiagramsReadably_WithoutNetwork(string page)
    {
        var violations = await fixture.Site.CheckPageAsync(page);

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void EveryPage_NamesTheCommitDocumented() =>
        Assert.All(Directory.EnumerateFiles(fixture.Site.Output, "*.html", SearchOption.AllDirectories)
                .Where(page => Path.GetFileName(page) != "toc.html"),
            page => Assert.Contains(RepositorySite.Commit, File.ReadAllText(page), StringComparison.Ordinal));

    [Fact]
    public void LinkOutOfTheDocumentation_LeadsToTheSameFileOnGitHub_AtTheCommitDocumented()
    {
        var page = File.ReadAllText(Path.Combine(fixture.Site.Output, "architecture", "vue-d-ensemble.html"));

        Assert.Contains($"https://github.com/Tetram76/bdtheque-claude/blob/{RepositorySite.Commit}/docker-compose.yml", page, StringComparison.Ordinal);
    }
}
