using Bdtheque.Frontend.Components.Lists;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// A number of a column of a list of the administration (visuel/charte-graphique/convivialite-et-efficacite.md, nombre nul),
/// shared by the lists.
/// </summary>
public sealed class ListCountTests : BunitContext
{
    public ListCountTests() => Services.AddLocalization();

    [Fact]
    public void ListCount_Zero_IsNeitherZeroNorAMention_ButTheDashOfAMissingData_ReadAsItsMention()
    {
        var count = Render<ListCount>(parameters => parameters
            .Add(p => p.Value, 0)
            .Add(p => p.None, "Aucun album")
            .AddChildContent("Albums 0"));

        Assert.Equal("—", count.Find(".not-set [aria-hidden=true]").TextContent);
        Assert.Equal("Aucun album", count.Find(".not-set .visually-hidden").TextContent);
        Assert.DoesNotContain("0", count.Markup);
    }

    [Fact]
    public void ListCount_AnyOtherNumber_IsWhatThePageWrites()
    {
        var count = Render<ListCount>(parameters => parameters
            .Add(p => p.Value, 24)
            .Add(p => p.None, "Aucun album")
            .AddChildContent("Albums 24"));

        Assert.Equal("Albums 24", count.Markup);
    }
}
