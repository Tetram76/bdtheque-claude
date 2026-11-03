using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Layout;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Layout;

/// <summary>
/// The foot of every page: the link to the information on the audience measurement, and its state for this browser, told
/// by a word, not by a color alone.
/// </summary>
public sealed class AppFooterTests : BunitContext
{
    public AppFooterTests() => Services.AddLocalization();

    [Fact]
    public void Measurement_ActiveForThisBrowser_IsSaidNextToTheLink()
    {
        var footer = RenderFor(new TestRequest());

        Assert.Equal("Mesure d'audience", footer.Find($"a[href='{AppPaths.AudienceInformation}']").TextContent.Trim());
        var state = footer.Find(".audience-state");
        Assert.Equal(("active", "active"), (state.GetAttribute("data-state"), state.TextContent.Trim()));
    }

    [Fact]
    public void Measurement_RefusedByThisBrowser_IsSaidNextToTheLink()
    {
        var footer = RenderFor(new TestRequest(new Dictionary<string, string> { [AudienceOpposition.CookieName] = "1" }));

        var state = footer.Find(".audience-state");
        Assert.Equal(("opposed", "refusée"), (state.GetAttribute("data-state"), state.TextContent.Trim()));
    }

    private IRenderedComponent<AppFooter> RenderFor(TestRequest request)
    {
        new FakeAudienceApi().AddServices(Services, request);
        return Render<AppFooter>();
    }
}
