using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the list of the genres of the administration, carried by its address: what differs from the list of the
/// albums (AlbumListQueryTests), a list without filters nor navigation by initial whose genres are changed in their row and
/// added in a frame above it (visuel/listes/saisie-selon-l-entite.md), its paths and the functions of its actions.
/// </summary>
public sealed class GenreListQueryTests
{
    private static readonly Guid Genre = Guid.Parse("01900000-0000-7000-8000-000000000003");

    [Fact]
    public void Href_CarriesTheSearchThePage_AndTheGenreChangedInItsRowOrTheFrameOfANewOne()
    {
        Assert.Equal($"{AppPaths.AdminGenres}?q=ave&page=2", new GenreListQuery(Text: "ave", Page: 2).Href());
        Assert.Equal($"{AppPaths.AdminGenres}?q=ave&edit={Genre}", new GenreListQuery(Text: "ave", Edit: Genre).Href());
        Assert.Equal($"{AppPaths.AdminGenres}?new=open", new GenreListQuery(Adding: true).Href());
        Assert.Equal(AppPaths.AdminGenres, new GenreListQuery().Href());
    }

    [Fact]
    public void Paths_OfTheChangeAndOfTheCreation_OpenTheListOnThem()
    {
        Assert.Equal(AppPaths.AdminNewGenre, new GenreListQuery(Adding: true).Href());
        Assert.Equal(new GenreListQuery(Edit: Genre).Href(), AppPaths.AdminEditGenre(Genre));
        Assert.Equal(AppPaths.AdminEditGenre(Genre), AppPaths.AdminEdit(Contracts.Enums.RecordType.Genre, Genre));
    }

    [Fact]
    public void ApiPath_AsksThePageOfTheListOfTheGenres()
    {
        var query = new GenreListQuery(Text: "ave", Edit: Genre);

        Assert.Equal("/admin/genres?q=ave&page=1&pageSize=50", query.ApiPath());
        Assert.Equal("/admin/genres?q=ave&pageSize=11", query.MergeCandidatesApiPath("ave", 10));
    }

    [Fact]
    public void Parse_ReadsBackTheAddressOfTheList_LeavingOutTheCriteriaOfOtherLists()
    {
        var query = new GenreListQuery(Text: "ave", Page: 3, Edit: Genre);

        Assert.Equal(query, GenreListQuery.Parse(new Uri($"http://frontend{query.Href()}").Query));
        Assert.Equal(new GenreListQuery(Adding: true), GenreListQuery.Parse("?new=open"));
        Assert.Equal(new GenreListQuery(), GenreListQuery.Parse($"?entry=B&genreId={Genre}&filters=open&page=0&new=yes&edit=x"));
    }

    [Theory]
    [InlineData("q=ave", "admin.genres.search", "q=ave")]
    [InlineData("q=ave&page=2", "admin.genres.page", "q=ave&page=2")]
    [InlineData("delete=01900000-0000-7000-8000-000000000003", "admin.genres.deletion", "genre=01900000-0000-7000-8000-000000000003")]
    [InlineData("merge=01900000-0000-7000-8000-000000000003&mergeQ=pol", "admin.genres.merge", "genre=01900000-0000-7000-8000-000000000003&q=pol")]
    [InlineData("q=ave&edit=01900000-0000-7000-8000-000000000003", "admin.genres.edit", "genre=01900000-0000-7000-8000-000000000003")]
    [InlineData("new=open", "admin.genres.new", null)]
    public void Action_IsThatOfTheListOfTheGenres(string address, string function, string? parameters)
    {
        var action = GenreListQuery.Parse(address).Action!.Value;

        Assert.Equal((function, parameters), (action.Function.Key, action.Parameters));
        Assert.Contains(action.Function, AudienceFunctions.Followed);
    }

    [Fact]
    public void Action_OfTheWholeList_IsNone()
    {
        Assert.Null(new GenreListQuery().Action);
    }
}
