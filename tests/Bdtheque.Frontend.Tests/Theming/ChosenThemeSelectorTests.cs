using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Theming;
using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Tests.Theming;

/// <summary>
/// The theme chosen by the address of the page, in the functional validation environment only
/// (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md): it becomes the theme of the visit.
/// </summary>
public sealed class ChosenThemeSelectorTests
{
    [Theory]
    [InlineData("planche")]
    [InlineData("gazette")]
    [InlineData("base")]
    public void ThemeInTheAddress_IsTheThemeOfTheVisit_AndKeptByItsCookie(string key)
    {
        var request = new TestRequest();
        request.Context.Request.QueryString = new($"?theme={key}");
        var cookie = new VisitCookie(request.Accessor);

        var theme = Selector(request, cookie).Current;

        Assert.Equal(key, theme.Key);
        Assert.Equal(key, cookie.Current!.ThemeKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?theme=unknown")]
    public void WithoutAnOfferedThemeInTheAddress_TheThemeOfTheVisitIsKept(string query)
    {
        var request = new TestRequest(new Dictionary<string, string>
        {
            [BrowserVisit.CookieName] = new BrowserVisit(Today, "gazette", Today).Format(),
        });
        request.Context.Request.QueryString = new(query);
        var cookie = new VisitCookie(request.Accessor);

        Assert.Equal("gazette", Selector(request, cookie).Current.Key);
    }

    private static readonly DateOnly Today = new(2026, 11, 6);

    private static ChosenThemeSelector Selector(TestRequest request, VisitCookie cookie) => new(
        request.Accessor, cookie,
        new VisitThemeSelector(cookie, new BrowserTimeZone { Name = "Europe/Paris" }, new FixedClock(new DateTimeOffset(2026, 11, 6, 9, 0, 0, TimeSpan.Zero))));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
