using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the list of the albums of the administration, carried by its address: the address of the list, the
/// query of the API, and the action of the list the audience measurement records.
/// </summary>
public sealed class AlbumListQueryTests
{
    private static readonly Guid Series = Guid.Parse("01900000-0000-7000-8000-000000000001");
    private static readonly Guid Genre = Guid.Parse("01900000-0000-7000-8000-000000000002");

    [Fact]
    public void Href_OfTheWholeList_IsThePathOfTheList()
    {
        Assert.Equal(AppPaths.AdminAlbums, AlbumListQuery.All.Href());
    }

    [Fact]
    public void Href_CarriesEveryCriterionInAFixedOrder_AndLeavesOutTheFirstPage()
    {
        var query = AlbumListQuery.All with
        {
            Text = "Tintin & co", Entry = "#", SeriesId = Series, GenreId = Genre, State = AlbumCollectionState.InCollection, Page = 3,
        };

        Assert.Equal(
            $"{AppPaths.AdminAlbums}?q=Tintin%20%26%20co&entry=%23&seriesId={Series}&genreId={Genre}&state=InCollection&page=3",
            query.Href());
        Assert.Equal($"{AppPaths.AdminAlbums}?q=Tintin%20%26%20co", (AlbumListQuery.All with { Text = "Tintin & co", Page = 1 }).Href());
    }

    [Fact]
    public void Href_KeepsTheFiltersOpenAndTheDialogs()
    {
        var album = Guid.NewGuid();

        Assert.Equal($"{AppPaths.AdminAlbums}?filters=open", (AlbumListQuery.All with { FiltersOpen = true }).Href());
        Assert.Equal($"{AppPaths.AdminAlbums}?delete={album}", (AlbumListQuery.All with { Delete = album }).Href());
        Assert.Equal(
            $"{AppPaths.AdminAlbums}?merge={album}&mergeQ=barra",
            (AlbumListQuery.All with { Merge = album, MergeText = "barra" }).Href());
    }

    [Fact]
    public void ApiPath_AsksThePageOfTheListWithItsCriteria_WithoutWhatOnlyThePageUses()
    {
        var query = AlbumListQuery.All with
        {
            Text = "lotus", SeriesId = Series, State = AlbumCollectionState.PurchaseIntent, Page = 2, FiltersOpen = true, Delete = Guid.NewGuid(),
        };

        Assert.Equal($"/admin/albums?q=lotus&seriesId={Series}&state=PurchaseIntent&page=2&pageSize=50", query.ApiPath());
    }

    [Fact]
    public void ActiveFilterCount_CountsTheCrossFiltersAndTheState_NeitherTheSearchNorTheEntry()
    {
        Assert.Equal(0, (AlbumListQuery.All with { Text = "lotus", Entry = "L" }).ActiveFilterCount);
        Assert.Equal(2, (AlbumListQuery.All with { GenreId = Genre, State = AlbumCollectionState.InCollection }).ActiveFilterCount);
    }

    [Fact]
    public void WithoutFilters_KeepsTheSearchAndTheEntry_AndReturnsToTheFirstPage()
    {
        var query = AlbumListQuery.All with { Text = "lotus", Entry = "L", SeriesId = Series, State = AlbumCollectionState.InCollection, Page = 4 };

        Assert.Equal(AlbumListQuery.All with { Text = "lotus", Entry = "L" }, query.WithoutFilters());
    }

    [Theory]
    [InlineData("q=lotus", "admin.albums.search", "q=lotus")]
    [InlineData("q=lotus&genreId=01900000-0000-7000-8000-000000000002", "admin.albums.filter", "q=lotus&genreId=01900000-0000-7000-8000-000000000002")]
    [InlineData("q=lotus&entry=L", "admin.albums.initial", "q=lotus&entry=L")]
    [InlineData("entry=L&page=2", "admin.albums.page", "entry=L&page=2")]
    public void Action_IsThatOfTheLastControlUsed_WithTheCriteriaOfTheList(string criteria, string function, string parameters)
    {
        var action = AlbumListQuery.Parse(criteria).Action;

        Assert.Equal((function, parameters), (action!.Value.Function.Key, action.Value.Parameters));
    }

    [Fact]
    public void Action_OfTheWholeList_IsNone_AndADialogIsTheActionOfItsAlbum()
    {
        var album = Guid.NewGuid();

        Assert.Null(AlbumListQuery.All.Action);
        Assert.Equal((AudienceFunctions.AdminAlbumsDeletion, $"album={album}"), (AlbumListQuery.All with { Delete = album, Page = 2 }).Action);
        Assert.Equal((AudienceFunctions.AdminAlbumsMerge, $"album={album}&q=barra"), (AlbumListQuery.All with { Merge = album, MergeText = "barra" }).Action);
    }

    [Fact]
    public void Parse_ReadsTheAddressOfTheList_AndIgnoresWhatItDoesNotKnow()
    {
        var query = AlbumListQuery.Parse($"q=Tintin%20%26%20co&entry=%23&seriesId={Series}&state=InCollection&page=3&filters=open&other=1");

        Assert.Equal(
            AlbumListQuery.All with
            {
                Text = "Tintin & co", Entry = "#", SeriesId = Series, State = AlbumCollectionState.InCollection, Page = 3, FiltersOpen = true,
            },
            query);
    }

    [Theory]
    [InlineData("entry=Z9")]
    [InlineData("seriesId=lotus")]
    [InlineData("state=Owned")]
    [InlineData("page=0")]
    [InlineData("page=x")]
    public void Parse_LeavesOutACriterionItCannotRead(string criteria)
    {
        Assert.Equal(AlbumListQuery.All, AlbumListQuery.Parse(criteria));
    }

    [Theory]
    [InlineData("q=tintin&entry=B&genreId=01900000-0000-7000-8000-000000000002", EmptyListReason.Search)]
    [InlineData("entry=B&genreId=01900000-0000-7000-8000-000000000002", EmptyListReason.Filters)]
    [InlineData("entry=B", EmptyListReason.Entry)]
    [InlineData("page=3", EmptyListReason.Nothing)]
    [InlineData("", EmptyListReason.Nothing)]
    public void EmptyReason_IsTheMostPreciseOfTheCriteria_ThenTheEntryOfTheNavigation(string criteria, EmptyListReason reason)
    {
        Assert.Equal(reason, AlbumListQuery.Parse(criteria).EmptyReason);
    }
}
