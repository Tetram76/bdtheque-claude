namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The technical documentation is read through its maps and kept by every change (gestion-projet/gouvernance/
/// documentation-technique.md): a page missing from its map, a broken link, a file or a symbol that no longer exists, or a
/// diagram without its question, would mislead the agent who reads it. Run with the tests of the repository, so that a
/// change of code that breaks a page fails, whatever it changes.
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed class TechnicalDocumentationStructureTests : IDisposable
{
    private readonly string _repository = Path.Combine(Path.GetTempPath(), $"bdtheque-docs-{Guid.NewGuid():N}");

    public TechnicalDocumentationStructureTests()
    {
        Directory.CreateDirectory(_repository);
        Write("src/App/Gate.cs", "public sealed class Gate { public bool IsOpen() => true; }");
        Write("docs/tech/index.md", "# Documentation\n\n* [Domaine](domaine/index.md) - Le domaine.\n");
        Write("docs/tech/domaine/index.md", "# Domaine\n\n* [Page](page.md) - Ce que dit la page.\n");
        WritePage("Page", "Ce que dit la page.", "Voir [`Gate.IsOpen`](../../../src/App/Gate.cs).");
    }

    // Every violation named in full, each on its line: the message is what tells the author what to fix.
    [Fact]
    public void Documentation_OfTheRepository_IsWellFormed()
    {
        var violations = new TechnicalDocumentationChecker(RepositoryFiles.Root).Check();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, ["Technical documentation (docs/tech):", .. violations]));
    }

    [Fact]
    public void Fixture_WellFormed_HasNoViolation() =>
        Assert.Empty(Check());

    [Fact]
    public void Page_NotListedInItsMap_IsReported()
    {
        Write("docs/tech/domaine/autre.md", "---\ntitle: 'Autre'\ndescription: 'Une autre.'\n---\n\n# Autre\n");

        Assert.Contains("docs/tech/domaine/index.md: autre.md is not listed", Check());
    }

    [Fact]
    public void MapEntry_WithAnotherDescriptionThanItsPage_IsReported()
    {
        Write("docs/tech/domaine/index.md", "# Domaine\n\n* [Page](page.md) - Une autre description.\n");

        Assert.Contains("docs/tech/domaine/index.md: the entry of page.md does not give its description", Check());
    }

    [Fact]
    public void MapEntry_ToAMissingPage_IsReported()
    {
        Write("docs/tech/domaine/index.md", "# Domaine\n\n* [Page](page.md) - Ce que dit la page.\n* [Absente](absente.md) - Rien.\n");

        Assert.Contains("docs/tech/domaine/index.md: lists absente.md, which does not exist", Check());
    }

    [Fact]
    public void Folder_WithoutMap_IsReported()
    {
        File.Delete(Path.Combine(_repository, "docs/tech/domaine/index.md"));

        Assert.Contains("docs/tech/domaine: no index.md", Check());
    }

    [Fact]
    public void Page_WithoutFrontMatter_IsReported()
    {
        Write("docs/tech/domaine/page.md", "# Page\n");

        Assert.Contains("docs/tech/domaine/page.md: no front matter", Check());
    }

    [Fact]
    public void Page_WhoseHeadingIsNotItsTitle_IsReported()
    {
        Write("docs/tech/domaine/page.md", "---\ntitle: 'Page'\ndescription: 'Ce que dit la page.'\n---\n\n# Autre titre\n");

        Assert.Contains("docs/tech/domaine/page.md: its heading is not its title 'Page'", Check());
    }

    [Fact]
    public void Link_ToAMissingFile_IsReported()
    {
        WritePage("Page", "Ce que dit la page.", "Voir [le code](../../../src/App/Absent.cs).");

        Assert.Contains("docs/tech/domaine/page.md: ../../../src/App/Absent.cs designates nothing", Check());
    }

    [Fact]
    public void Link_ToAMissingSection_IsReported()
    {
        WritePage("Page", "Ce que dit la page.", "Voir [la carte](../index.md#absente).");

        Assert.Contains("docs/tech/domaine/page.md: ../index.md#absente designates no section", Check());
    }

    [Fact]
    public void Link_FromTheRoot_IsReported()
    {
        WritePage("Page", "Ce que dit la page.", "Voir [le code](/src/App/Gate.cs).");

        Assert.Contains("docs/tech/domaine/page.md: /src/App/Gate.cs starts from the root, which its reader resolves elsewhere", Check());
    }

    [Fact]
    public void LinkToCode_NamingASymbolAbsentFromIt_IsReported()
    {
        WritePage("Page", "Ce que dit la page.", "Voir [`Gate.Close`](../../../src/App/Gate.cs).");

        Assert.Contains("docs/tech/domaine/page.md: Gate.Close is not in ../../../src/App/Gate.cs", Check());
    }

    [Fact]
    public void LinkToCode_NamedByItsFile_IsNoSymbol() =>
        Assert.Empty(CheckWith("Voir [`Gate.cs`](../../../src/App/Gate.cs), [`src/App/`](../../../src/App/)."));

    [Fact]
    public void LinkWrittenAsCode_IsAnExample() =>
        Assert.Empty(CheckWith("Une forme : `[texte](../../../src/App/Absent.cs)`."));

    [Fact]
    public void CodePattern_DesignatingNoFile_IsReported()
    {
        Write("docs/tech/domaine/page.md",
            "---\ntitle: 'Page'\ndescription: 'Ce que dit la page.'\ncode:\n  - 'src/Absent/**'\n---\n\n# Page\n");

        Assert.Contains("docs/tech/domaine/page.md: code 'src/Absent/**' designates no file", Check());
    }

    [Fact]
    public void Diagram_WithoutItsQuestion_IsReported()
    {
        WritePage("Page", "Ce que dit la page.", "Un schéma.\n\n```mermaid\nflowchart LR\n    a --> b\n```");

        Assert.Contains("docs/tech/domaine/page.md: a diagram is not introduced by a paragraph that asks its question", Check());
    }

    [Fact]
    public void Diagram_PrecededByItsQuestion_IsWellFormed() =>
        Assert.Empty(CheckWith("Ce schéma, tracé à la main, répond à une seule question : quel composant appelle quel autre ?\n\n```mermaid\nflowchart LR\n    a --> b\n```"));

    [Fact]
    public void Diagram_WhoseQuestionIsNotInItsIntroduction_IsReported()
    {
        WritePage("Page", "Ce que dit la page.", "Qui appelle qui ?\n\nLe schéma suit.\n\n```mermaid\nflowchart LR\n    a --> b\n```");

        Assert.Contains("docs/tech/domaine/page.md: a diagram is not introduced by a paragraph that asks its question", Check());
    }

    public void Dispose() => Directory.Delete(_repository, recursive: true);

    private IReadOnlyList<string> Check() => new TechnicalDocumentationChecker(_repository).Check();

    private IReadOnlyList<string> CheckWith(string body)
    {
        WritePage("Page", "Ce que dit la page.", body);
        return Check();
    }

    private void WritePage(string title, string description, string body) =>
        Write("docs/tech/domaine/page.md", $"---\ntitle: '{title}'\ndescription: '{description}'\n---\n\n# {title}\n\n{body}\n");

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_repository, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
