using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Theming;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Http;

namespace Bdtheque.Frontend.Tests.Theming;

/// <summary>
/// The colours of the day of the page a request renders, read before it is rendered (choix-implementation/socle/themes.md):
/// for a theme painted by them only, never for the welcome screen, which has no theme.
/// </summary>
public sealed class PageDayColorsTests
{
    private static readonly CoverPalette Palette = new("#F7E98E", "#B85AA2", "#5A3A1A");

    [Fact]
    public async Task ThemePaintedByTheDay_HasTheColoursOfTheDay()
    {
        var colors = ColorsOf(ThemeCatalog.Couverture, welcomeDue: false);

        await colors.LoadAsync(CancellationToken.None);

        Assert.Equal(DayPalette.Of(Palette).Style, colors.Style);
    }

    [Fact]
    public async Task OtherTheme_HasNone_AndAsksForNone()
    {
        var source = new Source();
        var colors = new PageDayColors(source, new Fixed(ThemeCatalog.Planche), WelcomeOf(due: false));

        await colors.LoadAsync(CancellationToken.None);

        Assert.Null(colors.Style);
        Assert.Equal(0, source.Asked);
    }

    [Fact]
    public async Task WelcomeScreen_HasNone()
    {
        var colors = ColorsOf(ThemeCatalog.Couverture, welcomeDue: true);

        await colors.LoadAsync(CancellationToken.None);

        Assert.Null(colors.Style);
    }

    private static PageDayColors ColorsOf(Theme theme, bool welcomeDue) =>
        new(new Source(), new Fixed(theme), WelcomeOf(welcomeDue));

    // The welcome screen is due for the navigation to a page of a browser that has not given its time zone.
    private static BrowserWelcome WelcomeOf(bool due)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Accept = "text/html";
        if (!due)
            context.Request.Headers.Cookie = $"{BrowserTimeZone.CookieName}=Europe%2FParis";
        return new BrowserWelcome(new HttpContextAccessor { HttpContext = context });
    }

    private sealed class Fixed(Theme theme) : IThemeSelector
    {
        public Theme Current => theme;
    }

    private sealed class Source : IDayColorsSource
    {
        public int Asked { get; private set; }

        public async Task<CoverPalette?> GetAsync(CancellationToken cancellationToken = default)
        {
            Asked++;
            await Task.Yield();
            return Palette;
        }
    }
}
