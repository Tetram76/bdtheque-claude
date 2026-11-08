using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the list of the series of the administration, carried by its address: what differs from the list of the
/// albums (AlbumListQueryTests), its filters, its paths and the functions of its actions.
/// </summary>
public sealed class SeriesListQueryTests
{
    private static readonly Guid Author = Guid.Parse("01900000-0000-7000-8000-000000000001");
    private static readonly Guid Universe = Guid.Parse("01900000-0000-7000-8000-000000000002");

    [Fact]
    public void Href_CarriesItsFiltersInAFixedOrder()
    {
        var query = new SeriesListQuery(Text: "barra", Entry: "B", AuthorId: Author, UniverseId: Universe, Page: 2);

        Assert.Equal($"{AppPaths.AdminSeries}?q=barra&entry=B&authorId={Author}&universeId={Universe}&page=2", query.Href());
        Assert.Equal(AppPaths.AdminSeries, new SeriesListQuery().Href());
        // The filters chosen, neither the search nor the entry.
        Assert.Equal(2, query.ActiveFilterCount);
    }

    [Fact]
    public void ApiPath_AsksThePageOfTheListOfTheSeries()
    {
        var query = new SeriesListQuery(Text: "barra", AuthorId: Author, Delete: Guid.NewGuid());

        Assert.Equal($"/admin/series?q=barra&authorId={Author}&page=1&pageSize=50", query.ApiPath());
        Assert.Equal("/admin/series?q=bl&pageSize=11", query.MergeCandidatesApiPath("bl", 10));
    }

    [Fact]
    public void Parse_ReadsBackTheAddressOfTheList_LeavingOutWhatItCannotRead()
    {
        var query = new SeriesListQuery(Text: "barra", Entry: "#", AuthorId: Author, Page: 3, FiltersOpen: true);

        Assert.Equal(query, SeriesListQuery.Parse(new Uri($"http://frontend{query.Href()}").Query));
        Assert.Equal(new SeriesListQuery(), SeriesListQuery.Parse($"?authorId=nope&entry=AB&page=0&seriesId={Author}"));
    }

    [Theory]
    [InlineData("q=barra", "admin.series.search", "q=barra")]
    [InlineData("authorId=01900000-0000-7000-8000-000000000001", "admin.series.filter", "authorId=01900000-0000-7000-8000-000000000001")]
    [InlineData("q=barra&entry=B", "admin.series.initial", "q=barra&entry=B")]
    [InlineData("entry=B&page=2", "admin.series.page", "entry=B&page=2")]
    [InlineData("delete=01900000-0000-7000-8000-000000000003", "admin.series.deletion", "series=01900000-0000-7000-8000-000000000003")]
    [InlineData("merge=01900000-0000-7000-8000-000000000003&mergeQ=bl", "admin.series.merge", "series=01900000-0000-7000-8000-000000000003&q=bl")]
    public void Action_IsThatOfTheListOfTheSeries(string address, string function, string parameters)
    {
        var action = SeriesListQuery.Parse(address).Action!.Value;

        Assert.Equal((function, parameters), (action.Function.Key, action.Parameters));
        Assert.Contains(action.Function, AudienceFunctions.Followed);
    }

    [Fact]
    public void Action_OfTheWholeList_IsNone()
    {
        Assert.Null(new SeriesListQuery().Action);
    }
}
