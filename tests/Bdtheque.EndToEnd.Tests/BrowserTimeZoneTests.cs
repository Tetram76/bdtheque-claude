extern alias frontend;

using frontend::Bdtheque.Frontend.Formatting;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The server knows the time zone of the browser, in which it presents the dates and times, even in the pages it
/// renders statically (fonctionnel/presentation.md § Langue et culture d'affichage).
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class BrowserTimeZoneTests(ApplicationHost application)
{
    [Fact]
    public async Task Server_LearnsTheTimeZoneOfTheBrowser_FromItsFirstPage()
    {
        var page = await PageInAsync("Asia/Tokyo");

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(AppPaths.Home);

        Assert.Equal("Asia/Tokyo", await page.Locator("html").GetAttributeAsync("data-time-zone"));
    }

    [Fact]
    public async Task Browser_ThatChangesTimeZone_IsFollowed()
    {
        var context = await application.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = application.BaseAddress.ToString(),
            TimezoneId = "Europe/Paris",
        });
        await context.AddCookiesAsync([new Cookie
        {
            Name = BrowserTimeZone.CookieName, Value = "Asia/Tokyo", Url = application.BaseAddress.ToString(),
        }]);
        var page = await context.NewPageAsync();

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Equal("Asia/Tokyo", await page.Locator("html").GetAttributeAsync("data-time-zone"));
        await page.GotoAsync(AppPaths.Home);

        Assert.Equal("Europe/Paris", await page.Locator("html").GetAttributeAsync("data-time-zone"));
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
}
