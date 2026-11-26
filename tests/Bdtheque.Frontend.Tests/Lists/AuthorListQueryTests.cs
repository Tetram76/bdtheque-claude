using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the list of the authors of the administration, carried by its address: what differs from the list of the
/// albums (AlbumListQueryTests), a list without filters, the panel of an author it opens, its paths and the functions of its
/// actions.
/// </summary>
public sealed class AuthorListQueryTests
{
    private static readonly Guid Author = Guid.Parse("01900000-0000-7000-8000-000000000003");

    [Fact]
    public void Href_CarriesItsSearchItsEntryAndItsPage_WithoutFilters()
    {
        var query = new AuthorListQuery(Text: "gib", Entry: "G", Page: 2);

        Assert.Equal($"{AppPaths.AdminAuthors}?q=gib&entry=G&page=2", query.Href());
        Assert.Equal(AppPaths.AdminAuthors, new AuthorListQuery().Href());
        Assert.Equal(0, query.ActiveFilterCount);
    }

    [Fact]
    public void Href_CarriesThePanelOpen_WithTheCriteriaOfTheList()
    {
        Assert.Equal($"{AppPaths.AdminAuthors}?q=gib&entry=G&edit={Author}", new AuthorListQuery(Text: "gib", Entry: "G", Edit: Author).Href());
        Assert.Equal($"{AppPaths.AdminAuthors}?entry=G&new=open", new AuthorListQuery(Entry: "G", Adding: true).Href());
    }

    [Fact]
    public void Paths_OfTheChangeAndOfTheCreation_OpenThePanelOverTheList()
    {
        Assert.Equal(AppPaths.AdminNewAuthor, new AuthorListQuery(Adding: true).Href());
        Assert.Equal(new AuthorListQuery(Edit: Author).Href(), AppPaths.AdminEditAuthor(Author));
        Assert.Equal(AppPaths.AdminEditAuthor(Author), AppPaths.AdminEdit(Contracts.Enums.RecordType.Author, Author, null));
    }

    [Fact]
    public void ApiPath_AsksThePageOfTheListOfTheAuthors()
    {
        var query = new AuthorListQuery(Text: "gib", Entry: "G", Delete: Author);

        Assert.Equal("/admin/authors?q=gib&entry=G&page=1&pageSize=50", query.ApiPath());
        Assert.Equal("/admin/authors?q=gi&pageSize=11", query.MergeCandidatesApiPath("gi", 10));
    }

    [Fact]
    public void Parse_ReadsBackTheAddressOfTheList_LeavingOutWhatItCannotRead()
    {
        var query = new AuthorListQuery(Text: "gib", Entry: "@", Page: 3);

        Assert.Equal(query, AuthorListQuery.Parse(new Uri($"http://frontend{query.Href()}").Query));
        Assert.Equal(new AuthorListQuery(Entry: "G", Edit: Author), AuthorListQuery.Parse($"?entry=G&edit={Author}"));
        Assert.Equal(new AuthorListQuery(Adding: true), AuthorListQuery.Parse("?new=open"));
        // A list without filters has no panel to open, nor filter to read.
        Assert.Equal(new AuthorListQuery(), AuthorListQuery.Parse($"?filters=open&genreId={Author}&entry=AB&page=0"));
    }

    [Theory]
    [InlineData("q=gib", "admin.authors.search", "q=gib")]
    [InlineData("q=gib&entry=G", "admin.authors.initial", "q=gib&entry=G")]
    [InlineData("entry=G&page=2", "admin.authors.page", "entry=G&page=2")]
    [InlineData("delete=01900000-0000-7000-8000-000000000003", "admin.authors.deletion", "author=01900000-0000-7000-8000-000000000003")]
    [InlineData("merge=01900000-0000-7000-8000-000000000003&mergeQ=gi", "admin.authors.merge", "author=01900000-0000-7000-8000-000000000003&q=gi")]
    [InlineData("q=gib&edit=01900000-0000-7000-8000-000000000003", "admin.authors.edit", "author=01900000-0000-7000-8000-000000000003")]
    [InlineData("entry=G&new=open", "admin.authors.new", null)]
    public void Action_IsThatOfTheListOfTheAuthors(string address, string function, string? parameters)
    {
        var action = AuthorListQuery.Parse(address).Action!.Value;

        Assert.Equal((function, parameters), (action.Function.Key, action.Parameters));
        Assert.Contains(action.Function, AudienceFunctions.Followed);
    }

    [Fact]
    public void Action_OfTheWholeList_IsNone()
    {
        Assert.Null(new AuthorListQuery().Action);
    }
}
