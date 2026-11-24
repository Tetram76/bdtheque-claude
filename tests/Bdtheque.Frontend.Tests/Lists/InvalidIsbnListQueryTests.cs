using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the list of the ISBN to check, carried by its address: those of the list of the editions
/// (EditionListQueryTests), and the state of the errors it presents, those to correct by default, which is no filter.
/// </summary>
public sealed class InvalidIsbnListQueryTests
{
    private static readonly Guid Album = Guid.Parse("01900000-0000-7000-8000-000000000001");

    [Fact]
    public void Href_CarriesTheStateFirstAmongItsFilters_TheErrorsToCorrectBeingNone()
    {
        var query = new InvalidIsbnListQuery(Text: "978-2", Entry: "L", State: InvalidIsbnState.All, AlbumId: Album, Page: 2);

        Assert.Equal($"{AppPaths.AdminInvalidIsbns}?q=978-2&entry=L&state=All&albumId={Album}&page=2", query.Href());
        Assert.Equal(2, query.ActiveFilterCount);
        Assert.Equal(AppPaths.AdminInvalidIsbns, new InvalidIsbnListQuery(State: InvalidIsbnState.ToCorrect).Href());
        Assert.Equal(0, new InvalidIsbnListQuery(State: InvalidIsbnState.ToCorrect).ActiveFilterCount);
    }

    [Fact]
    public void ApiPath_AsksThePageOfTheListOfTheIsbnToCheck()
    {
        var query = new InvalidIsbnListQuery(Text: "978", State: InvalidIsbnState.Accepted);

        Assert.Equal("/admin/editions/invalid-isbns?q=978&state=Accepted&page=1&pageSize=50", query.ApiPath());
    }

    [Theory]
    [InlineData("?state=All", InvalidIsbnState.All)]
    [InlineData("?state=Accepted", InvalidIsbnState.Accepted)]
    [InlineData("?state=ToCorrect", InvalidIsbnState.ToCorrect)]
    [InlineData("?state=2", InvalidIsbnState.ToCorrect)]
    [InlineData("?state=nope", InvalidIsbnState.ToCorrect)]
    [InlineData("", InvalidIsbnState.ToCorrect)]
    public void Parse_ReadsTheStateByItsName_TheErrorsToCorrectOtherwise(string address, InvalidIsbnState shown)
    {
        Assert.Equal(shown, InvalidIsbnListQuery.Parse(address).ShownState);
    }

    [Fact]
    public void Parse_ReadsBackTheAddressOfTheList()
    {
        var query = new InvalidIsbnListQuery(Text: "978-2", Entry: "L", State: InvalidIsbnState.All, AlbumId: Album, Page: 3, FiltersOpen: true);

        Assert.Equal(query, InvalidIsbnListQuery.Parse(new Uri($"http://frontend{query.Href()}").Query));
    }

    [Theory]
    [InlineData("q=978-2", "admin.invalid-isbns.search", "q=978-2")]
    [InlineData("q=978-2&entry=L", "admin.invalid-isbns.initial", "q=978-2&entry=L")]
    [InlineData("state=All", "admin.invalid-isbns.filter", "state=All")]
    [InlineData("q=978&page=2", "admin.invalid-isbns.page", "q=978&page=2")]
    public void Action_IsThatOfTheListOfTheIsbnToCheck(string address, string function, string parameters)
    {
        var action = InvalidIsbnListQuery.Parse(address).Action!.Value;

        Assert.Equal((function, parameters), (action.Function.Key, action.Parameters));
        Assert.Contains(action.Function, AudienceFunctions.Followed);
    }

    [Fact]
    public void WithoutFilters_KeepsTheSearchAndTheEntry_BackToTheErrorsToCorrect()
    {
        var query = new InvalidIsbnListQuery(Text: "978-2", Entry: "L", State: InvalidIsbnState.All, AlbumId: Album, Page: 3);

        Assert.Equal(new InvalidIsbnListQuery(Text: "978-2", Entry: "L"), query.WithoutFilters());
    }
}
