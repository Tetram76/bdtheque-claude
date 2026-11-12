using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Http;

namespace Bdtheque.Frontend.Tests.Visits;

/// <summary>
/// The requests the welcome screen answers instead of their page: the navigation of a browser to a page, before it gave
/// its time zone (choix-implementation/socle/transverse/internationalisation.md, écran d'accueil).
/// </summary>
public sealed class BrowserWelcomeTests
{
    [Fact]
    public void NavigationOfABrowser_ThatHasNotGivenItsTimeZone_GetsTheScreen() =>
        Assert.True(BrowserWelcome.IsDueFor(Navigation().Context.Request));

    [Fact]
    public void Browser_ThatGaveItsTimeZone_GetsThePage() =>
        Assert.False(BrowserWelcome.IsDueFor(Navigation(new Dictionary<string, string> { [BrowserTimeZone.CookieName] = "Europe/Paris" }).Context.Request));

    [Fact]
    public void Browser_MarkedAfterTheScreen_GetsThePage_NeverTheScreenAgain()
    {
        // A browser that refused the cookie, or runs no script, comes back marked.
        var request = Navigation();
        request.Context.Request.QueryString = new($"?entry=B&{BrowserWelcome.SkipParameter}=");

        Assert.False(BrowserWelcome.IsDueFor(request.Context.Request));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("HEAD")]
    public void Request_OtherThanAGet_GetsThePage(string method)
    {
        // A form submitted, whose page the browser displayed, and so ran its scripts.
        var request = Navigation();
        request.Context.Request.Method = method;

        Assert.False(BrowserWelcome.IsDueFor(request.Context.Request));
    }

    [Theory]
    [InlineData("cors", "empty")] // An enhanced navigation of Blazor, a request of a page already displayed.
    [InlineData("no-cors", "image")] // A medium.
    [InlineData("navigate", "iframe")] // A page in a frame, the browser running the page around it.
    [InlineData(null, null)] // A client that is not a browser (robot, preview of a link), which sends no metadata of the navigation.
    public void Request_ThatIsNotTheNavigationOfABrowserToAPage_GetsThePage(string? mode, string? destination)
    {
        var request = new TestRequest();
        if (mode is not null)
            request.Context.Request.Headers["Sec-Fetch-Mode"] = mode;
        if (destination is not null)
            request.Context.Request.Headers["Sec-Fetch-Dest"] = destination;

        Assert.False(BrowserWelcome.IsDueFor(request.Context.Request));
    }

    [Fact]
    public void SkipAddress_IsTheAddressOfTheRequest_Marked()
    {
        var request = Navigation();
        request.Context.Request.PathBase = "/bd";
        request.Context.Request.Path = "/albums";
        request.Context.Request.QueryString = new("?search=Blake%20%26%20Mortimer&entry=B");

        Assert.Equal($"/bd/albums?search=Blake%20%26%20Mortimer&entry=B&{BrowserWelcome.SkipParameter}=", BrowserWelcome.SkipAddress(request.Context.Request));
    }

    [Fact]
    public void Decision_IsTheRequestOne_TakenOnce()
    {
        var request = Navigation();
        var welcome = new BrowserWelcome(request.Accessor);

        Assert.True(welcome.IsDue);
        request.Context.Request.Headers.Cookie = $"{BrowserTimeZone.CookieName}=Europe%2FParis";
        Assert.True(welcome.IsDue);
    }

    [Fact]
    public void WithoutRequest_NoScreen() =>
        Assert.False(new BrowserWelcome(new HttpContextAccessor()).IsDue);

    // The navigation of a browser to a page, as Fetch Metadata describes it.
    private static TestRequest Navigation(IReadOnlyDictionary<string, string>? cookies = null)
    {
        var request = new TestRequest(cookies);
        request.Context.Request.Headers["Sec-Fetch-Mode"] = "navigate";
        request.Context.Request.Headers["Sec-Fetch-Dest"] = "document";
        return request;
    }
}
