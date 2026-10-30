extern alias frontend;

using frontend::Bdtheque.Frontend.Formatting;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Visits;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

[Collection(ApplicationCollection.Name)]
public sealed class ThemeTests(ApplicationHost application)
{
    [Fact]
    public async Task Page_IsPresentedInAnOfferedTheme_WithTheContractsOfTheBaseTheme()
    {
        var page = await application.NewPageAsync();

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var theme = await page.Locator("html").GetAttributeAsync("data-theme");
        Assert.Contains(ThemeCatalog.Offered, offered => offered.Key == theme);
        var background = await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--color-background').trim()");
        Assert.NotEmpty(background);
    }

    [Fact]
    public async Task FirstPage_StartsTheVisit_InASessionCookie_WithTheThemeOfThePage()
    {
        var page = await application.NewPageAsync();

        await page.GotoAsync(AppPaths.Home);

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
        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(AppPaths.Home);
        var visit = (await VisitCookieAsync(page)).Value;

        var tab = await page.Context.NewPageAsync();
        await tab.GotoAsync(AppPaths.Home);

        Assert.Equal(visit, (await VisitCookieAsync(tab)).Value);
        Assert.Equal(VisitOf(await VisitCookieAsync(tab)).ThemeKey, await tab.Locator("html").GetAttributeAsync("data-theme"));
    }

    [Fact]
    public async Task DateOfTheTheme_IsTheDateOfTheBrowser_OnceItGaveItsTimeZone()
    {
        // Already the next day in Kiribati while it is still the day before in universal time, for a part of each day.
        var page = await PageInAsync("Pacific/Kiritimati");

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Null(VisitOf(await VisitCookieAsync(page)).ThemeDate);
        await page.GotoAsync(AppPaths.Home);

        Assert.Equal(TodayIn("Pacific/Kiritimati"), VisitOf(await VisitCookieAsync(page)).ThemeDate);
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

        await page.GotoAsync(AppPaths.Home);

        Assert.Equal(new BrowserVisit(start, ThemeCatalog.Base.Key, today), VisitOf(await VisitCookieAsync(page)));
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

        await page.GotoAsync(AppPaths.Home);

        var theme = await page.Locator("html").GetAttributeAsync("data-theme");
        Assert.Contains(ThemeCatalog.Offered, offered => offered.Key == theme);
        Assert.Equal(new BrowserVisit(today, theme!, today), VisitOf(await VisitCookieAsync(page)));
    }

    [Fact]
    public async Task Page_UsesTheFontsServedByTheApplication()
    {
        var page = await application.NewPageAsync();

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        Assert.True(await page.EvaluateAsync<bool>("async () => (await document.fonts.ready, document.fonts.check('16px Inter'))"),
            "The font of the base theme is not loaded.");
        Assert.True(await page.EvaluateAsync<bool>(
            "() => [...document.fonts].some(font => font.family.replaceAll('\"', '') === 'Inter' && font.status === 'loaded')"),
            "No face of the font of the base theme was loaded from the application.");
    }

    private async Task<IPage> PageInAsync(string timeZone)
    {
        var context = await application.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = application.BaseAddress.ToString(),
            TimezoneId = timeZone,
        });
        return await context.NewPageAsync();
    }

    private Cookie Cookie(string name, string value) => new() { Name = name, Value = value, Url = application.BaseAddress.ToString() };

    private static async Task<BrowserContextCookiesResult> VisitCookieAsync(IPage page) =>
        (await page.Context.CookiesAsync()).Single(cookie => cookie.Name == BrowserVisit.CookieName);

    private static BrowserVisit VisitOf(BrowserContextCookiesResult cookie) =>
        BrowserVisit.Parse(Uri.UnescapeDataString(cookie.Value)) ?? throw new InvalidOperationException($"Not a visit: {cookie.Value}");

    private static DateOnly TodayIn(string timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(timeZone)).DateTime);
}
