extern alias frontend;

using System.Text.Json;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;
using Bdtheque.EndToEnd.Tests.Headers;

namespace Bdtheque.EndToEnd.Tests.Theming;

/// <summary>
/// The touches of « La gazette » (visuel/dashboard/la-gazette.md ; visuel/administration/declinaison-par-theme.md),
/// in a browser: blocks opened by a rule of ink without a frame, a mention in capitals above their title, a double rule
/// under the header, the current entry of the menu underlined with the accent, and the floating panel of the menu of the
/// administration opaque, bordered and shadowed although the blocks are not.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class GazetteThemeTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 900 };

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private const string Ink = "rgb(43, 16, 48)";
    private const string Paper = "rgb(251, 243, 234)";
    private const string Amber = "rgb(255, 181, 71)";

    [Fact]
    public async Task Blocks_AreOpenedByARuleOfInk_WithoutAFrame()
    {
        var page = await OpenAsync(CasesPage.Path);

        var blocks = JsonSerializer.Deserialize<Edges[]>(await page.Locator(".block").EvaluateAllAsync<JsonElement>("""
            blocks => blocks.map(block => {
                const style = getComputedStyle(block);
                return {
                    top: style.borderTopWidth, topColor: style.borderTopColor, topStyle: style.borderTopStyle,
                    sides: [style.borderRightWidth, style.borderBottomWidth, style.borderLeftWidth],
                    background: style.backgroundColor, shadow: style.boxShadow,
                };
            })
            """), Json)!;

        Assert.Equal(CasesPage.Titles.Length, blocks.Length);
        Assert.All(blocks, block =>
        {
            Assert.Equal(("2px", Ink, "solid"), (block.Top, block.TopColor, block.TopStyle));
            Assert.All(block.Sides, width => Assert.Equal("0px", width));
            Assert.Equal("rgba(0, 0, 0, 0)", block.Background);
            Assert.Equal("none", block.Shadow);
        });
    }

    [Fact]
    public async Task BlockTitles_HaveTheirMentionInCapitals_AboveThem()
    {
        var page = await OpenAsync(CasesPage.Path);
        var kicker = page.Locator(".block-kicker").First;

        await Assertions.Expect(kicker).ToBeVisibleAsync();
        Assert.Equal("uppercase", await kicker.EvaluateAsync<string>("kicker => getComputedStyle(kicker).textTransform"));
        var (mention, title) = (await kicker.BoundingBoxAsync(), await page.Locator(".block-title").First.BoundingBoxAsync());
        Assert.True(mention!.Y + mention.Height <= title!.Y, "The mention is not above the title.");
    }

    [Fact]
    public async Task Header_IsSetApartByADoubleRule()
    {
        var page = await OpenAsync(AppPaths.Home);

        var rule = await page.Locator(".app-header").EvaluateAsync<string[]>(
            "header => [getComputedStyle(header).borderBottomStyle, getComputedStyle(header).borderBottomColor]");

        Assert.Equal(["double", Ink], rule);
    }

    [Fact]
    public async Task ConsultationMenu_UnderlinesItsCurrentEntryWithTheAccent()
    {
        var page = await OpenAsync(AppPaths.Home);
        var current = page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" }).Locator("a[aria-current='page']");

        var entry = await current.EvaluateAsync<string[]>(
            "entry => [getComputedStyle(entry).backgroundColor, getComputedStyle(entry).color, getComputedStyle(entry, '::after').backgroundColor]");

        Assert.Equal(["rgba(0, 0, 0, 0)", Ink, Amber], entry);
    }

    [Fact]
    public async Task AdministrationMenu_IsOpaqueBorderedAndShadowed_ThoughTheBlocksAreNot()
    {
        var page = await OpenAsync(AdminFramePage.Path);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true }).ClickAsync();
        var menu = page.Locator("#admin-menu");
        await Assertions.Expect(menu).ToBeVisibleAsync();

        var panel = await menu.EvaluateAsync<string[]>("""
            menu => [getComputedStyle(menu).backgroundColor, getComputedStyle(menu).borderTopWidth,
                     getComputedStyle(menu).borderTopColor, getComputedStyle(menu).boxShadow]
            """);
        Assert.Equal(Paper, panel[0]);
        Assert.Equal(("2px", Ink), (panel[1], panel[2]));
        Assert.NotEqual("none", panel[3]);
    }

    [Fact]
    public async Task Page_UsesTheFontsOfTheTheme_ServedByTheApplication()
    {
        var page = await OpenAsync(CasesPage.Path);

        foreach (var family in new[] { "DM Sans", "Fraunces" })
            Assert.True(await page.EvaluateAsync<bool>(
                "family => [...document.fonts].some(font => font.family.replaceAll('\"', '') === family && font.status === 'loaded')", family),
                $"No face of {family} was loaded from the Application.");
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageInThemeAsync(ThemeCatalog.Gazette.Key, Desktop);
        await PageReadiness.GotoAsync(page, path);
        await page.EvaluateAsync("() => document.fonts.ready");
        return page;
    }

    private sealed record Edges(string Top, string TopColor, string TopStyle, string[] Sides, string Background, string Shadow);
}
