using Bdtheque.Frontend.Components.Layout;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Layout;

/// <summary>
/// The banner that the script of Blazor shows when an interactive component fails: a technical error, presented by the
/// single component of the errors (choix-implementation/socle/frontend.md, Présentation), with the elements the script
/// looks for (its id, the classes of its actions), which only the tests in a browser exercise (UnhandledErrorTests).
/// </summary>
public sealed class UnhandledErrorBannerTests : BunitContext
{
    public UnhandledErrorBannerTests() => Services.AddLocalization();

    [Fact]
    public void Banner_PresentsATechnicalError()
    {
        var banner = Render<UnhandledErrorBanner>();

        var alert = banner.Find("#blazor-error-ui [role=alert]");
        Assert.Equal("technical", alert.GetAttribute("data-error-category"));
        Assert.False(string.IsNullOrWhiteSpace(alert.QuerySelector("[data-error-text]")?.TextContent));
    }

    [Fact]
    public void Error_OffersToReloadThePage_AndToDismissIt()
    {
        var banner = Render<UnhandledErrorBanner>();

        Assert.Equal("Recharger la page", banner.Find("#blazor-error-ui .error-actions button.reload").TextContent.Trim());
        Assert.Equal("Fermer", banner.Find("#blazor-error-ui .error-actions button.dismiss").GetAttribute("title"));
    }
}
