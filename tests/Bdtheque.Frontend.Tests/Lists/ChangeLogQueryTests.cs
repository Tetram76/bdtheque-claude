using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the change log, carried by its address (visuel/journal/journal-des-modifications.md): its search, the type of
/// record, the operation, the period and a single record; no navigation by initial.
/// </summary>
public sealed class ChangeLogQueryTests
{
    private static readonly Guid Record = Guid.Parse("01900000-0000-7000-8000-000000000001");

    [Fact]
    public void Href_CarriesItsCriteriaInTheirOrder_ItsDaysAsAnInputOfTypeDateGivesThem()
    {
        var query = new ChangeLogQuery("Astérix", RecordType.Album, ChangeLogOperation.Modification, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 25), Record, 2);

        Assert.Equal(
            $"{AppPaths.AdminChangeLog}?q=Ast%C3%A9rix&recordType=Album&operation=Modification&from=2026-11-01&to=2026-11-25&record={Record}&page=2",
            query.Href());
        Assert.Equal(query, ChangeLogQuery.Parse(new Uri($"http://frontend{query.Href()}").Query));
    }

    [Fact]
    // A criterion it cannot read, which only an address written by hand carries, is left out; so is an entry, which it has not.
    public void Parse_LeavesOutWhatItCannotRead()
    {
        Assert.Equal(new ChangeLogQuery(), ChangeLogQuery.Parse("?from=25/11/2026&recordType=3&operation=Nothing&record=1&entry=B"));
    }

    [Fact]
    // The days in the time zone of the browser, which the API resolves; universal time as long as the browser has not given it.
    public void ApiPath_WithTheTimeZoneOfTheBrowser()
    {
        var query = new ChangeLogQuery(From: new DateOnly(2026, 11, 1));

        Assert.Equal("/admin/change-log?from=2026-11-01&page=1&pageSize=50&timeZone=Europe%2FParis", query.ApiPath("Europe/Paris"));
        Assert.Equal("/admin/change-log?from=2026-11-01&page=1&pageSize=50", query.ApiPath(null));
    }

    [Theory]
    [InlineData("q=Western", "admin.change-log.search")]
    [InlineData("q=Western&record=01900000-0000-7000-8000-000000000001", "admin.change-log.filter")]
    [InlineData("to=2026-11-25", "admin.change-log.filter")]
    [InlineData("operation=Creation&page=3", "admin.change-log.page")]
    public void Action_OfTheLastControlUsed(string address, string function)
    {
        Assert.Equal(function, ChangeLogQuery.Parse(address).Action!.Value.Function.Key);
    }

    [Fact]
    // « Tout effacer » keeps the search, the history of a record included among the filters cleared.
    public void WithoutFilters_KeepsTheSearch()
    {
        var query = new ChangeLogQuery("Western", RecordType.Genre, RecordId: Record, Page: 3);

        Assert.Equal($"{AppPaths.AdminChangeLog}?q=Western", query.WithoutFilters().Href());
        Assert.Equal(2, query.ActiveFilterCount);
    }
}
