using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components;
using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The welcome screen of the first request of a browser (visuel/ecran-accueil/): all of it in the response, what its script
/// needs carried by its root, a browser without a script sent to the page.
/// </summary>
public sealed class WelcomeScreenTests : BunitContext
{
    private readonly TestRequest _request = new();

    public WelcomeScreenTests()
    {
        Services.AddLocalization();
        Services.AddSingleton(_request.Accessor);
        Services.AddScoped<VisitCookie>();
        Services.AddScoped<AudienceOpposition>();
        _request.Context.Request.Path = "/albums";
        _request.Context.Request.QueryString = new("?entry=B");
    }

    [Fact]
    public void Screen_AnnouncesTheOpeningOfTheCollection_AsAStatus()
    {
        var screen = Render();

        var status = screen.Find("main");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Contains("Ouverture de la collection", status.TextContent, StringComparison.Ordinal);
        Assert.Equal("BDthèque", screen.Find(".name").TextContent);
        Assert.Equal("true", screen.Find("svg").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Screen_DownloadsNothing()
    {
        // Its styles and its drawing are in the response: neither a stylesheet, a script, an image nor a font to download.
        var screen = Render();

        Assert.Empty(screen.FindAll("link, script[src], img, image, use[href]"));
        Assert.DoesNotContain("@font-face", screen.Find("style").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("url(http", screen.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Screen_GivesItsScriptTheCookies_AndTheMarkedAddressOfThePage()
    {
        var root = Render().Find("html");

        Assert.Equal(BrowserTimeZone.CookieName, root.GetAttribute("data-time-zone-cookie"));
        Assert.Equal(AudienceDevice.TouchScreenCookieName, root.GetAttribute("data-touch-screen-cookie"));
        Assert.Equal($"/albums?entry=B&{BrowserWelcome.SkipParameter}=", root.GetAttribute("data-skip"));
        Assert.Null(root.GetAttribute("data-audience"));
    }

    [Fact]
    public void Screen_OfAVisitorWhoOpposesTheAudienceMeasurement_TellsItsScript()
    {
        _request.Context.Request.Headers.Cookie = $"{AudienceOpposition.CookieName}=1";

        Assert.Equal("opposed", Render().Find("html").GetAttribute("data-audience"));
    }

    [Fact]
    public void BrowserWithoutAScript_IsSentToTheMarkedPage()
    {
        var refresh = Render().Find("noscript").InnerHtml;

        Assert.Contains("http-equiv=\"refresh\"", refresh, StringComparison.Ordinal);
        Assert.Contains($"url=/albums?entry=B&amp;{BrowserWelcome.SkipParameter}=", refresh, StringComparison.Ordinal);
    }

    [Fact]
    public void Screen_IsNeverKept()
    {
        // Kept, the same address would bring the screen again once the browser has written its cookies.
        Render();

        Assert.Equal("no-store", _request.Context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public void Animation_IsLeftOut_WithReducedMotion() =>
        Assert.Contains("prefers-reduced-motion: reduce", Render().Find("style").TextContent, StringComparison.Ordinal);

    private IRenderedComponent<WelcomeScreen> Render() =>
        Render<WelcomeScreen>(parameters => parameters.AddCascadingValue<HttpContext>(_request.Context));
}
