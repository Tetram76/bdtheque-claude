extern alias frontend;

using frontend::Bdtheque.Frontend.Formatting;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Visits;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

[Collection(ApplicationCollection.Name)]
public sealed class ThemeTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    // A page of the consultation that presents no date and whose content depends on no data: that of the home page depends
    // on the collection the other tests change (the dashboard is rendered in the time zone of the browser).
    private const string Page = AppPaths.NotYetImplemented;

    [Fact]
    public async Task Page_IsPresentedInAnOfferedTheme_WithTheContractsOfTheBaseTheme()
    {
        var page = await NewPageAsync();

        await PageReadiness.GotoAsync(page, Page);

        var theme = await page.Locator("html").GetAttributeAsync("data-theme");
        Assert.Contains(ThemeCatalog.Offered, offered => offered.Key == theme);
        var background = await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--color-background').trim()");
        Assert.NotEmpty(background);
    }

    [Fact]
    public async Task FirstPage_StartsTheVisit_InASessionCookie_WithTheThemeOfThePage()
    {
        var page = await NewPageAsync();

        await page.GotoAsync(Page);

        // A session cookie: shared by every tab and window of the browser, gone with its session (fonctionnel/mesure-audience.md, Visites).
        var cookie = await VisitCookieAsync(page);
        Assert.Equal(-1, cookie.Expires);
        Assert.True(cookie.HttpOnly);
        Assert.Equal(await page.Locator("html").GetAttributeAsync("data-theme"), VisitOf(cookie).ThemeKey);
    }

    [Fact]
    public async Task Visit_KeepsItsTheme_InEveryPageAndTabOfTheBrowser()
    {
        var page = await PageInAsync("Europe/Paris");
        await PageReadiness.GotoAsync(page, Page);
        await page.GotoAsync(Page);
        var visit = (await VisitCookieAsync(page)).Value;

        var tab = await page.Context.NewPageAsync();
        await tab.GotoAsync(Page);

        Assert.Equal(visit, (await VisitCookieAsync(tab)).Value);
        Assert.Equal(VisitOf(await VisitCookieAsync(tab)).ThemeKey, await tab.Locator("html").GetAttributeAsync("data-theme"));
    }

    // The date of the theme depends on whether the first page, rendered without the time zone of the browser, is rendered
    // again once it is known (choix-implementation/socle/transverse/internationalisation.md): each case starts from the
    // visit its first page left, in a theme fixed by the test rather than drawn (the draw itself: VisitThemeTests).
    [Fact]
    public async Task DateOfTheTheme_IsTheDateOfTheBrowser_FromTheNextPage_InAThemeThatDoesNotDependOnTheTimeZone()
    {
        var page = await PageOfAVisitStartedInAsync(ThemeCatalog.Base, "Pacific/Kiritimati");

        await PageReadiness.GotoAsync(page, Page);
        // The first page, not rendered again: its request did not bring the time zone, and its visit has no date yet.
        Assert.Null(await page.Locator("html").GetAttributeAsync("data-time-zone"));
        Assert.Null(VisitOf(await VisitCookieAsync(page)).ThemeDate);
        await page.GotoAsync(Page);

        // The date of the browser, without a new draw: the theme does not change in the middle of the first date.
        var visit = VisitOf(await VisitCookieAsync(page));
        Assert.Equal((ThemeCatalog.Base.Key, TodayIn("Pacific/Kiritimati")), (visit.ThemeKey, visit.ThemeDate));
    }

    [Fact]
    public async Task DateOfTheTheme_IsTheDateOfTheBrowser_FromTheFirstPage_InAThemePaintedByTheDay()
    {
        var page = await PageOfAVisitStartedInAsync(ThemeCatalog.Couverture, "Pacific/Kiritimati");

        await page.GotoAsync(Page);
        // The first page, rendered again once the browser gave its time zone: its colours are those of the date of the browser.
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-time-zone", "Pacific/Kiritimati");

        var visit = VisitOf(await VisitCookieAsync(page));
        Assert.Equal((ThemeCatalog.Couverture.Key, TodayIn("Pacific/Kiritimati")), (visit.ThemeKey, visit.ThemeDate));
    }

    [Fact]
    public async Task Visit_ThatGoesOnAtANewDate_DrawsItsThemeAgain_AndGoesOn()
    {
        var page = await PageInAsync("Europe/Paris");
        var today = TodayIn("Europe/Paris");
        var start = today.AddDays(-3);
        await page.Context.AddCookiesAsync([
            Cookie(BrowserTimeZone.CookieName, "Europe/Paris"),
            Cookie(BrowserVisit.CookieName, new BrowserVisit(start, ThemeCatalog.Base.Key, today.AddDays(-1)).Format()),
        ]);

        await page.GotoAsync(Page);

        // Drawn again among the offered themes, possibly the same one, for the new date; the visit goes on.
        var visit = VisitOf(await VisitCookieAsync(page));
        Assert.Equal((start, today), (visit.Start, visit.ThemeDate));
        Assert.Contains(ThemeCatalog.Offered, offered => offered.Key == visit.ThemeKey);
        Assert.Equal(visit.ThemeKey, await page.Locator("html").GetAttributeAsync("data-theme"));
    }

    [Fact]
    public async Task Visit_WhoseThemeIsNoLongerOffered_IsPresentedInAnOfferedTheme()
    {
        var page = await PageInAsync("Europe/Paris");
        var today = TodayIn("Europe/Paris");
        await page.Context.AddCookiesAsync([
            Cookie(BrowserTimeZone.CookieName, "Europe/Paris"),
            Cookie(BrowserVisit.CookieName, new BrowserVisit(today, "withdrawn", today).Format()),
        ]);

        await page.GotoAsync(Page);

        var theme = await page.Locator("html").GetAttributeAsync("data-theme");
        Assert.Contains(ThemeCatalog.Offered, offered => offered.Key == theme);
        // The visit goes on, and with it the visit of the audience measurement, which its page records.
        var visit = VisitOf(await VisitCookieAsync(page));
        Assert.Equal(new BrowserVisit(today, theme!, today), visit with { AudienceVisit = null });
    }

    [Fact]
    public async Task Page_UsesTheFontsServedByTheApplication()
    {
        // The font of the base theme; those of « La planche encrée » are checked with its drawings (PlancheThemeTests).
        var page = await NewPageInThemeAsync(ThemeCatalog.Base.Key, new ViewportSize { Width = 1440, Height = 900 });

        await PageReadiness.GotoAsync(page, Page);

        Assert.True(await page.EvaluateAsync<bool>("async () => (await document.fonts.ready, document.fonts.check('16px Inter'))"),
            "The font of the base theme is not loaded.");
        Assert.True(await page.EvaluateAsync<bool>(
            "() => [...document.fonts].some(font => font.family.replaceAll('\"', '') === 'Inter' && font.status === 'loaded')"),
            "No face of the font of the base theme was loaded from the Application.");
    }

    private async Task<IPage> PageInAsync(string timeZone)
    {
        var context = await NewContextAsync(new BrowserNewContextOptions
        {
            TimezoneId = timeZone,
        });
        return await context.NewPageAsync();
    }

    // A browser whose visit started in the given theme, before it gave its time zone (no cookie of it yet), as its first
    // page leaves it. In Kiribati, already the next day while it is still the day before in universal time for a part of
    // each day: the date of the theme is then that of the browser, not of the server.
    private async Task<IPage> PageOfAVisitStartedInAsync(Theme theme, string timeZone)
    {
        var page = await PageInAsync(timeZone);
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        await page.Context.AddCookiesAsync([Cookie(BrowserVisit.CookieName, new BrowserVisit(start, theme.Key, ThemeDate: null).Format())]);
        return page;
    }

    private Cookie Cookie(string name, string value) => new() { Name = name, Value = value, Url = Application.BaseAddress.ToString() };

    private static async Task<BrowserContextCookiesResult> VisitCookieAsync(IPage page) =>
        (await page.Context.CookiesAsync()).Single(cookie => cookie.Name == BrowserVisit.CookieName);

    private static BrowserVisit VisitOf(BrowserContextCookiesResult cookie) =>
        BrowserVisit.Parse(Uri.UnescapeDataString(cookie.Value)) ?? throw new InvalidOperationException($"Not a visit: {cookie.Value}");

    private static DateOnly TodayIn(string timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(timeZone)).DateTime);
}
