using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Pages;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The information of the visitor on the audience measurement, and their choice (fonctionnel/mesure-audience.md, Information
/// et opposition), told apart by role and accessible name.
/// </summary>
public sealed class AudienceInformationTests : BunitContext
{
    private static readonly BrowserVisit Visit = new(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3));

    private readonly FakeAudienceApi _api = new();

    public AudienceInformationTests() => Services.AddLocalization();

    [Fact]
    public void Page_InformsOfThePurpose_TheDataRecorded_AndHowLongTheyAreKept()
    {
        var page = RenderFor(new TestRequest(Cookies()));

        Assert.Equal("Mesure d'audience", page.Find("h1").TextContent.Trim());
        Assert.Equal(["Données relevées", "Durées de conservation", "Votre choix"], page.FindAll("h2").Select(h => h.TextContent.Trim()));
        Assert.Contains("ni adresse IP", page.Markup);
        Assert.Contains("25 mois", page.Markup);
        Assert.Contains("13 mois", page.Markup);
    }

    [Fact]
    public void Visitor_WhoseVisitsAreRecorded_CanOppose()
    {
        var page = RenderFor(new TestRequest(Cookies()));

        var form = page.Find("form[method='post']");
        Assert.Equal("oppose", form.QuerySelector("input[type='hidden'][name='Choice']")?.GetAttribute("value"));
        Assert.Equal("Refuser la mesure d'audience", form.QuerySelector("button[type='submit']")?.TextContent.Trim());
        Assert.Contains("Vos visites sont relevées", page.Find("#audience-choice").TextContent);
    }

    [Fact]
    public void Visitor_WhoOpposes_CanComeBackOnTheirChoice()
    {
        var page = RenderFor(new TestRequest(Cookies(opposed: true)));

        var form = page.Find("form[method='post']");
        Assert.Equal("withdraw", form.QuerySelector("input[type='hidden'][name='Choice']")?.GetAttribute("value"));
        Assert.Equal("Accepter de nouveau la mesure d'audience", form.QuerySelector("button[type='submit']")?.TextContent.Trim());
        Assert.Contains("aucune de vos visites n'est relevée", page.Find("#audience-choice").TextContent);
    }

    [Fact]
    public void Display_OfThePage_IsRecorded_UnlessTheVisitorOpposes()
    {
        RenderFor(new TestRequest(Cookies()));

        Assert.Equal([AudienceFunctions.AudienceInformation.Key], _api.Functions);
    }

    [Fact]
    public void Display_OfThePage_ForAVisitorWhoOpposes_IsNotRecorded()
    {
        RenderFor(new TestRequest(Cookies(opposed: true)));

        Assert.Empty(_api.Sent);
    }

    private IRenderedComponent<AudienceInformation> RenderFor(TestRequest request)
    {
        _api.AddServices(Services, request);
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AudienceInformation);
        return Render<AudienceInformation>(parameters => parameters.AddCascadingValue(request.Context));
    }

    private static Dictionary<string, string> Cookies(bool opposed = false)
    {
        var cookies = new Dictionary<string, string> { [BrowserVisit.CookieName] = Visit.Format() };
        if (opposed)
            cookies[AudienceOpposition.CookieName] = "1";
        return cookies;
    }
}
