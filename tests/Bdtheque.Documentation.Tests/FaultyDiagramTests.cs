namespace Bdtheque.Documentation.Tests;

/// <summary>A documentation of its own, with a diagram Mermaid cannot read and one too wide to be read.</summary>
public sealed class FaultySite : IAsyncLifetime
{
    private readonly string _repository = Path.Combine(Path.GetTempPath(), $"bdtheque-faulty-docs-{Guid.NewGuid():N}");

    internal DocumentationSite Site { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var source = Path.Combine(_repository, "docs", "tech");
        Directory.CreateDirectory(source);
        File.Copy(Path.Combine(Repository.Root, "docs", "tech", "docfx.json"), Path.Combine(source, "docfx.json"));
        File.WriteAllText(Path.Combine(source, "index.md"),
            "# Documentation\n\n* [Invalide](invalide.md) - Un schéma invalide.\n* [Illisible](illisible.md) - Un schéma trop large.\n");
        File.WriteAllText(Path.Combine(source, "invalide.md"),
            Page("Invalide", "Un schéma invalide.", "flowchart LR\n    a -->> -- b ((("));
        var row = string.Join(" --> ", Enumerable.Range(1, 16).Select(node => $"n{node}[\"Étape numéro {node}\"]"));
        File.WriteAllText(Path.Combine(source, "illisible.md"), Page("Illisible", "Un schéma trop large.", $"flowchart LR\n    {row}"));
        Site = await DocumentationSite.BuildAsync(source, "fixture");
    }

    public async Task DisposeAsync()
    {
        await Site.DisposeAsync();
        Directory.Delete(_repository, recursive: true);
    }

    private static string Page(string title, string description, string diagram) =>
        $"---\ntitle: '{title}'\ndescription: '{description}'\n---\n\n# {title}\n\nQue montre ce schéma, écrit pour le test ?\n\n```mermaid\n{diagram}\n```\n";
}

/// <summary>
/// The check of the rendering fails on what a syntax check alone would let pass or would not see: an error of Mermaid,
/// and a valid diagram shrunk under the threshold of legibility.
/// </summary>
public sealed class FaultyDiagramTests(FaultySite fixture) : IClassFixture<FaultySite>
{
    [Fact]
    public async Task Diagram_ThatMermaidCannotRead_IsReported() =>
        Assert.Contains("invalide.html: diagram 1 is not rendered, Mermaid reports an error", await fixture.Site.CheckPageAsync("invalide.html"));

    [Fact]
    public async Task Diagram_TooWideToBeRead_IsReported()
    {
        var violations = await fixture.Site.CheckPageAsync("illisible.html");

        Assert.Contains(violations, violation => violation.StartsWith("illisible.html: diagram 1 is shrunk to", StringComparison.Ordinal));
    }
}
