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
        // « Modifier » alone opens the record (visuel/liste-albums/contenu.md, Actions d'une ligne): the entry is no link,
        // neither itself nor around nor inside its text.
        var entry = Render<RowEntry>(parameters => parameters.AddChildContent("Les Bijoux de la Castafiore"));

        var element = entry.Find(".row-entry");
        Assert.Equal("SPAN", element.TagName);
        Assert.Equal("Les Bijoux de la Castafiore", element.TextContent);
        Assert.Empty(entry.FindAll("a"));
    }
}
