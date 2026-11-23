using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Http;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// What a list of the consultation keeps for the visit (fonctionnel/structure-de-l-application.md, Retour à une liste): the
/// address of the fil d'Ariane of a record, the list as it was left, and that of the entry of the menu, its filters only.
/// </summary>
public sealed class ListMemoryTests
{
    private static readonly BrowserVisit Visit = new(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3));

    private const string Left = $"{AppPaths.Albums}?q=tintin&entry=T&state=InCollection&genreId=01900000-0000-7000-8000-0000000000a2&page=2";

    [Fact]
    public void FromARecord_TheListIsAsLeft_FromTheMenu_ItsFiltersOnly()
    {
        var memory = Memory(Kept(Visit, Left), path: AppPaths.Album(Guid.NewGuid()));

        Assert.Equal(Left, memory.BackHref(AppPaths.Albums));
        Assert.Equal($"{AppPaths.Albums}?state=InCollection&genreId=01900000-0000-7000-8000-0000000000a2", memory.MenuHref(AppPaths.Albums));
    }

    [Fact]
    public void OnTheListItself_TheMenuKeepsTheFiltersItPresents()
    {
        var memory = Memory(Kept(Visit, Left), path: AppPaths.Albums, query: "?q=lotus&state=PurchaseIntent&filters=open");

        Assert.Equal($"{AppPaths.Albums}?state=PurchaseIntent", memory.MenuHref(AppPaths.Albums));
    }

    [Fact]
    public void ListKept_DuringTheRequest_IsThatOfTheFollowingAddresses()
    {
        var memory = Memory(cookie: null, path: AppPaths.Album(Guid.NewGuid()));

        memory.Keep(new CatalogAlbumListQuery(Text: "lotus", Entry: "L", FiltersOpen: true));

        Assert.Equal($"{AppPaths.Albums}?q=lotus&entry=L", memory.BackHref(AppPaths.Albums));
    }

    [Fact]
    public async Task ListKept_IsWrittenOnceTheResponseStarts_ForTheVisit_InASessionCookie()
    {
        var request = Request(cookie: null, path: AppPaths.Albums, query: "?q=lotus&entry=L");
        var memory = new ListMemory(request.Accessor, new VisitCookie(request.Accessor));

        memory.Keep(new CatalogAlbumListQuery(Text: "tintin"));
        memory.Keep(new CatalogAlbumListQuery(Text: "lotus", Entry: "L"));
        await request.StartResponseAsync();

        var written = Assert.Single(request.SetCookies, cookie => cookie.StartsWith("Bdtheque.List.albums=", StringComparison.Ordinal));
        Assert.Equal($"2026-11-03|{AppPaths.Albums}?q=lotus&entry=L", Uri.UnescapeDataString(written.Split(';')[0]["Bdtheque.List.albums=".Length..]));
        Assert.DoesNotContain("expires", written, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("max-age", written, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    // Nothing kept, or by another visit: the list from its start.
    [InlineData(null)]
    [InlineData("2026-10-01|/albums?state=InCollection")]
    // Not an address of the list: never followed.
    [InlineData("2026-11-03|https://example.com/albums?state=InCollection")]
    [InlineData("2026-11-03|/albumsx?state=InCollection")]
    [InlineData("2026-11-03")]
    public void WithoutAnAddressOfTheListLeftInTheVisit_TheListFromItsStart(string? cookie)
    {
        var memory = Memory(cookie, path: AppPaths.Album(Guid.NewGuid()));

        Assert.Equal(AppPaths.Albums, memory.BackHref(AppPaths.Albums));
        Assert.Equal(AppPaths.Albums, memory.MenuHref(AppPaths.Albums));
    }

    [Fact]
    public void EveryOtherPageOfTheMenu_IsLeftAsItIs()
    {
        var memory = Memory(Kept(Visit, Left), path: AppPaths.Home);

        Assert.Equal(AppPaths.Home, memory.MenuHref(AppPaths.Home));
    }

    private static string Kept(BrowserVisit visit, string address) => $"{visit.Start:yyyy-MM-dd}|{address}";

    private static ListMemory Memory(string? cookie, string path, string query = "")
    {
        var request = Request(cookie, path, query);
        return new ListMemory(request.Accessor, new VisitCookie(request.Accessor));
    }

    private static TestRequest Request(string? cookie, string path, string query)
    {
        var cookies = new Dictionary<string, string> { [BrowserVisit.CookieName] = Visit.Format() };
        if (cookie is not null)
            cookies["Bdtheque.List.albums"] = cookie;
        var request = new TestRequest(cookies, authenticated: false);
        request.Context.Request.Path = path;
        request.Context.Request.QueryString = new QueryString(query);
        return request;
    }
}
