using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the list of the editions of the administration, carried by its address: what differs from the list of the
/// albums (AlbumListQueryTests), its filters, its paths, the navigation by initial of its albums, and the functions of its
/// actions.
/// </summary>
public sealed class EditionListQueryTests
{
    private static readonly Guid Album = Guid.Parse("01900000-0000-7000-8000-000000000001");
    private static readonly Guid Collection = Guid.Parse("01900000-0000-7000-8000-000000000002");
    private static readonly Guid Series = Guid.Parse("01900000-0000-7000-8000-000000000004");

    [Fact]
    public void Href_CarriesItsFiltersInAFixedOrder()
    {
        var query = new EditionListQuery(
            Text: "978-2", Entry: "L", SeriesId: Series, AlbumId: Album, PublisherCollectionId: Collection, Page: 2);

        Assert.Equal(
            $"{AppPaths.AdminEditions}?q=978-2&entry=L&seriesId={Series}&albumId={Album}&publisherCollectionId={Collection}&page=2",
            query.Href());
        Assert.Equal(AppPaths.AdminEditions, new EditionListQuery().Href());
        // The filters chosen, neither the search nor the entry.
        Assert.Equal(3, query.ActiveFilterCount);
    }

    [Fact]
    public void ApiPath_AsksThePageOfTheListOfTheEditions()
    {
        var query = new EditionListQuery(Text: "978-2", Entry: "L", AlbumId: Album, Delete: Guid.NewGuid());

        Assert.Equal($"/admin/editions?q=978-2&entry=L&albumId={Album}&page=1&pageSize=50", query.ApiPath());
        Assert.Equal("/admin/editions?q=978&pageSize=11", query.MergeCandidatesApiPath("978", 10));
    }

    [Fact]
    public void Parse_ReadsBackTheAddressOfTheList_LeavingOutWhatItCannotRead()
    {
        var query = new EditionListQuery(Text: "978-2", Entry: "L", AlbumId: Album, Page: 3, FiltersOpen: true);

        Assert.Equal(query, EditionListQuery.Parse(new Uri($"http://frontend{query.Href()}").Query));
        Assert.Equal(new EditionListQuery(), EditionListQuery.Parse($"?albumId=nope&entry=AB&page=0&authorId={Album}"));
    }

    [Theory]
    [InlineData("q=978-2", "admin.editions.search", "q=978-2")]
    [InlineData("q=978-2&entry=L", "admin.editions.initial", "q=978-2&entry=L")]
    [InlineData("albumId=01900000-0000-7000-8000-000000000001", "admin.editions.filter", "albumId=01900000-0000-7000-8000-000000000001")]
    [InlineData("q=978&page=2", "admin.editions.page", "q=978&page=2")]
    [InlineData("delete=01900000-0000-7000-8000-000000000003", "admin.editions.deletion", "edition=01900000-0000-7000-8000-000000000003")]
    [InlineData("merge=01900000-0000-7000-8000-000000000003&mergeQ=978", "admin.editions.merge", "edition=01900000-0000-7000-8000-000000000003&q=978")]
    public void Action_IsThatOfTheListOfTheEditions(string address, string function, string parameters)
    {
        var action = EditionListQuery.Parse(address).Action!.Value;

        Assert.Equal((function, parameters), (action.Function.Key, action.Parameters));
        Assert.Contains(action.Function, AudienceFunctions.Followed);
    }

    [Fact]
    public void WithoutFilters_KeepsTheSearchAndTheEntry_BackOnTheFirstPage()
    {
        var query = new EditionListQuery(Text: "978-2", Entry: "L", AlbumId: Album, Page: 3, FiltersOpen: true);

        Assert.Equal(new EditionListQuery(Text: "978-2", Entry: "L"), query.WithoutFilters());
    }

    [Fact]
    public void Action_OfTheWholeList_IsNone()
    {
        Assert.Null(new EditionListQuery().Action);
    }
}
