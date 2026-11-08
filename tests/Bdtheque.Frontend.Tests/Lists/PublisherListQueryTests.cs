using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the list of the publishers of the administration, carried by its address: what differs from the list of
/// the albums (AlbumListQueryTests), a list without filters nor navigation by initial, its paths and the functions of its
/// actions.
/// </summary>
public sealed class PublisherListQueryTests
{
    private static readonly Guid Publisher = Guid.Parse("01900000-0000-7000-8000-000000000003");

    [Fact]
    public void Href_CarriesTheSearchAndThePage()
    {
        Assert.Equal($"{AppPaths.AdminPublishers}?q=dar&page=2", new PublisherListQuery(Text: "dar", Page: 2).Href());
        Assert.Equal(AppPaths.AdminPublishers, new PublisherListQuery().Href());
        Assert.Equal(0, new PublisherListQuery(Text: "dar").ActiveFilterCount);
    }

    [Fact]
    public void ApiPath_AsksThePageOfTheListOfThePublishers()
    {
        var query = new PublisherListQuery(Text: "dar", Delete: Guid.NewGuid());

        Assert.Equal("/admin/publishers?q=dar&page=1&pageSize=50", query.ApiPath());
        Assert.Equal("/admin/publishers?q=dar&pageSize=11", query.MergeCandidatesApiPath("dar", 10));
    }

    [Fact]
    public void Parse_ReadsBackTheAddressOfTheList_LeavingOutTheCriteriaOfOtherLists()
    {
        var query = new PublisherListQuery(Text: "dar", Page: 3, Merge: Publisher, MergeText: "cas");

        Assert.Equal(query, PublisherListQuery.Parse(new Uri($"http://frontend{query.Href()}").Query));
        // Neither navigation by initial nor filters: an address written by hand that carries them gives the whole list.
        Assert.Equal(new PublisherListQuery(), PublisherListQuery.Parse($"?entry=B&genreId={Publisher}&filters=open&page=0"));
    }

    [Theory]
    [InlineData("q=dar", "admin.publishers.search", "q=dar")]
    [InlineData("q=dar&page=2", "admin.publishers.page", "q=dar&page=2")]
    [InlineData("delete=01900000-0000-7000-8000-000000000003", "admin.publishers.deletion", "publisher=01900000-0000-7000-8000-000000000003")]
    [InlineData("merge=01900000-0000-7000-8000-000000000003&mergeQ=cas", "admin.publishers.merge", "publisher=01900000-0000-7000-8000-000000000003&q=cas")]
    public void Action_IsThatOfTheListOfThePublishers(string address, string function, string parameters)
    {
        var action = PublisherListQuery.Parse(address).Action!.Value;

        Assert.Equal((function, parameters), (action.Function.Key, action.Parameters));
        Assert.Contains(action.Function, AudienceFunctions.Followed);
    }

    [Fact]
    public void Action_OfTheWholeList_IsNone()
    {
        Assert.Null(new PublisherListQuery().Action);
    }
}
