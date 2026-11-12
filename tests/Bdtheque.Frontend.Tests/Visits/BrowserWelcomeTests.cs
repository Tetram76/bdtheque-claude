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

    [Fact]
    public void NavigationOfABrowser_OverPlainHttp_GetsTheScreen()
    {
        // A browser sends the metadata of a navigation (Sec-Fetch-*) to a secure origin only (HTTPS, localhost): not to the
        // NAS reached in HTTP on the internal network.
        var request = new TestRequest();
        request.Context.Request.Headers.Accept = HtmlOfANavigation;

        Assert.True(BrowserWelcome.IsDueFor(request.Context.Request));
    }

    [Theory]
    [InlineData("text/html; blazor-enhanced-nav=on", null, null)] // An enhanced navigation of Blazor, over HTTP.
    [InlineData("text/html; blazor-enhanced-nav=on", "cors", "empty")] // The same, to a secure origin.
    [InlineData("image/avif,image/webp,*/*;q=0.8", null, null)] // A medium, or an address the server does not know, over HTTP.
    [InlineData(HtmlOfANavigation, "no-cors", "image")] // A medium, to a secure origin, whatever it accepts.
    [InlineData(HtmlOfANavigation, "navigate", "iframe")] // A page in a frame, the browser running the page around it.
    [InlineData("*/*", null, null)] // A client that does not ask for a page (a script, a check of the service).
    public void Request_ThatIsNotTheNavigationOfABrowserToAPage_GetsThePage(string accept, string? mode, string? destination)
    {
        var request = new TestRequest();
        request.Context.Request.Headers.Accept = accept;
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

    // What a browser accepts when it navigates to a page (Chromium).
    private const string HtmlOfANavigation =
        "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8";

    // The navigation of a browser to a page, to a secure origin, which receives the metadata of the navigation.
    private static TestRequest Navigation(IReadOnlyDictionary<string, string>? cookies = null)
    {
        var request = new TestRequest(cookies);
        request.Context.Request.Headers.Accept = HtmlOfANavigation;
        request.Context.Request.Headers["Sec-Fetch-Mode"] = "navigate";
        request.Context.Request.Headers["Sec-Fetch-Dest"] = "document";
        return request;
    }
}
