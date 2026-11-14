using Bdtheque.Frontend.Components.Pages;
using Bdtheque.Frontend.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The page of an address that leads nowhere: a request that cannot succeed as the data stand, a functional error
/// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md), presented by the single component of
/// the errors (choix-implementation/socle/frontend.md, Présentation).
/// </summary>
public sealed class NotFoundPageTests : BunitContext
{
    public NotFoundPageTests() => Services.AddLocalization();

    [Fact]
    public void Page_PresentsAFunctionalError_UnderItsTitle()
    {
        var page = Render<NotFound>();

        Assert.Equal("Page introuvable", page.Find("h1").TextContent.Trim());
        var alert = page.Find("[role=alert]");
        Assert.Equal("functional", alert.GetAttribute("data-error-category"));
        Assert.False(string.IsNullOrWhiteSpace(alert.QuerySelector("[data-error-text]")?.TextContent));
    }

    // Its category named for what happened, not for a record modified in the meantime, which the name of the category says.
    [Fact]
    public void Error_IsNamedForAnAddressThatLeadsNowhere()
    {
        var page = Render<NotFound>();

        Assert.NotEqual("Données modifiées entre-temps", page.Find("[data-error-label]").TextContent.Trim());
    }

    [Fact]
    public void Error_LeadsHome()
    {
        var page = Render<NotFound>();

        Assert.Equal(AppPaths.Home, page.Find("[role=alert] .error-actions a").GetAttribute("href"));
    }
}
