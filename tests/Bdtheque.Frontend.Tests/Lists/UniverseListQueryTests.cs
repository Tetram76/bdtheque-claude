using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the list of the universes of the administration, carried by its address: a list without filters nor
/// navigation by initial whose universes are entered in a side panel above it (visuel/listes/saisie-selon-l-entite.md), its
/// paths and the functions of its actions. What it shares with the list of the genres is pinned by GenreListQueryTests.
/// </summary>
public sealed class UniverseListQueryTests
{
    private static readonly Guid Universe = Guid.Parse("01900000-0000-7000-8000-000000000004");

    [Fact]
    public void Href_CarriesTheSearchThePage_AndThePanelOpen()
    {
        Assert.Equal($"{AppPaths.AdminUniverses}?q=cor&page=2&edit={Universe}", new UniverseListQuery(Text: "cor", Page: 2, Edit: Universe).Href());
        Assert.Equal($"{AppPaths.AdminUniverses}?new=open", new UniverseListQuery(Adding: true).Href());
    }

    [Fact]
    public void Paths_OfTheChangeAndOfTheCreation_OpenThePanelOverTheList()
    {
        Assert.Equal(AppPaths.AdminNewUniverse, new UniverseListQuery(Adding: true).Href());
        Assert.Equal(new UniverseListQuery(Edit: Universe).Href(), AppPaths.AdminEditUniverse(Universe));
        Assert.Equal(AppPaths.AdminEditUniverse(Universe), AppPaths.AdminEdit(Contracts.Enums.RecordType.Universe, Universe, null));
    }

    [Fact]
    public void ApiPath_AsksThePageOfTheListOfTheUniverses()
    {
        Assert.Equal("/admin/universes?q=cor&page=1&pageSize=50", new UniverseListQuery(Text: "cor", Adding: true).ApiPath());
    }

    [Fact]
    public void Parse_ReadsBackTheAddressOfTheList()
    {
        var query = new UniverseListQuery(Text: "cor", Page: 3, Delete: Universe);

        Assert.Equal(query, UniverseListQuery.Parse(new Uri($"http://frontend{query.Href()}").Query));
        Assert.Equal(new UniverseListQuery(Edit: Universe), UniverseListQuery.Parse($"?edit={Universe}&entry=C"));
    }

    [Theory]
    [InlineData("q=cor", "admin.universes.search", "q=cor")]
    [InlineData("page=2", "admin.universes.page", "page=2")]
    [InlineData("delete=01900000-0000-7000-8000-000000000004", "admin.universes.deletion", "universe=01900000-0000-7000-8000-000000000004")]
    [InlineData("merge=01900000-0000-7000-8000-000000000004", "admin.universes.merge", "universe=01900000-0000-7000-8000-000000000004")]
    [InlineData("edit=01900000-0000-7000-8000-000000000004", "admin.universes.edit", "universe=01900000-0000-7000-8000-000000000004")]
    [InlineData("q=cor&new=open", "admin.universes.new", null)]
    public void Action_IsThatOfTheListOfTheUniverses(string address, string function, string? parameters)
    {
        var action = UniverseListQuery.Parse(address).Action!.Value;

        Assert.Equal((function, parameters), (action.Function.Key, action.Parameters));
        Assert.Contains(action.Function, AudienceFunctions.Followed);
    }
}
