using Bdtheque.Frontend.Components.Lists;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The entry of a row of a list of the administration (visuel/listes/principes-communs.md, Ligne), shared by the lists.
/// </summary>
public sealed class RowEntryTests : BunitContext
{
    [Fact]
    public void RowEntry_IsItsTextAlone_NoLink()
    {
        // The entry of a row is no link, in any list (visuel/listes/principes-communs.md, Entrée en texte simple), neither
        // itself nor around nor inside its text.
        var entry = Render<RowEntry>(parameters => parameters.AddChildContent("Les Bijoux de la Castafiore"));

        var element = entry.Find(".row-entry");
        Assert.Equal("SPAN", element.TagName);
        Assert.Equal("Les Bijoux de la Castafiore", element.TextContent);
        Assert.Empty(entry.FindAll("a"));
    }

    [Fact]
    public void Detail_InSmallUnderTheEntry()
    {
        var entry = Render<RowEntry>(parameters => parameters.Add(p => p.Detail, "Hergé").AddChildContent("Tintin au Tibet"));

        Assert.Equal(
            [("row-entry", "Tintin au Tibet"), ("row-entry-detail", "Hergé")],
            entry.Nodes.OfType<AngleSharp.Dom.IElement>().Select(element => (element.ClassName, element.TextContent)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    // The column of the entry is a column of a card: a detail that is missing is not shown, neither a dash nor a mention, nor an
    // empty line (visuel/charte-graphique/convivialite-et-efficacite.md, #476).
    public void Detail_Missing_IsNotShown(string? detail)
    {
        var entry = Render<RowEntry>(parameters => parameters.Add(p => p.Detail, detail).AddChildContent("Le Grand Fossé"));

        Assert.Equal(["row-entry"], entry.Nodes.OfType<AngleSharp.Dom.IElement>().Select(element => element.ClassName));
    }
}
