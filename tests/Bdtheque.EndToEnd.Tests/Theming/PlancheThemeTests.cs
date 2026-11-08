extern alias frontend;

using System.Text.Json;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Theming;

/// <summary>
/// What « La planche encrée » draws at random on its blocks (themes/planche.js, visuel/dashboard/la-planche-encree.md),
/// in a browser: the tints of the cases, their edges drawn by hand, the balloons of their titles, drawn at each display
/// and kept while the page is displayed. The coverage of the .NET tests does not measure the scripts of the frontend.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class PlancheThemeTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 900 };

    private static readonly string[] Shapes = ["smooth", "cloud", "star"];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private const string Cases = """
        () => [...document.querySelectorAll('.block')].map(block => {
            const title = block.querySelector('.block-title');
            return {
                tint: Number(block.dataset.tint ?? 0),
                fill: getComputedStyle(block).getPropertyValue('--block-fill').trim(),
                art: block.style.getPropertyValue('--block-art'),
                drawn: block.dataset.drawn !== undefined,
                seed: block.dataset.seed,
                title: title.textContent.trim(),
                shape: title.dataset.shape,
                bubble: title.style.getPropertyValue('--bubble-art'),
            };
        })
        """;

    [Fact]
    public async Task Blocks_AreCasesOfTheTintsOfTheTheme_AndBlocksThatFollowEachOtherNeverShareAHue()
    {
        var page = await OpenAsync(ThemeCatalog.Planche.Key);

        var cases = await CasesAsync(page);

        Assert.Equal(CasesPage.Titles.Length, cases.Length);
        var tints = await page.EvaluateAsync<string[]>(
            "() => [...Array(14).keys()].map(i => getComputedStyle(document.documentElement).getPropertyValue(`--color-block-tint-${i + 1}`).trim())");
        Assert.All(cases, @case =>
        {
            Assert.InRange(@case.Tint, 1, 14);
            Assert.Equal(tints[@case.Tint - 1], @case.Fill, StringComparer.OrdinalIgnoreCase);
        });
        // Hues 1 to 7, in their main version (tints 1 to 7) or their softer one (8 to 14).
        var hues = cases.Select(@case => (@case.Tint - 1) % 7).ToArray();
        Assert.All(hues.Zip(hues.Skip(1)), pair => Assert.NotEqual(pair.First, pair.Second));
        Assert.Contains(cases, @case => @case.Tint <= 7);
    }

    [Fact]
    public async Task Blocks_AreDrawnByHand_AndTheirTitlesAreBalloons()
    {
        var page = await OpenAsync(ThemeCatalog.Planche.Key);

        var cases = await CasesAsync(page);

        Assert.All(cases, @case =>
        {
            Assert.True(@case.Drawn, @case.Title);
            Assert.StartsWith("url(\"data:image/svg+xml,", @case.Art);
            Assert.StartsWith("url(\"data:image/svg+xml,", @case.Bubble);
        });
        // Drawn, the case takes its edges and its fill from its drawing.
        Assert.Equal("rgba(0, 0, 0, 0)", await page.Locator(".block").First.EvaluateAsync<string>("block => getComputedStyle(block).backgroundColor"));
    }

    [Fact]
    public async Task CaseInRelief_CastsAShadowDrawnByHand_NotTheStraightShadowOfABox()
    {
        var page = await OpenAsync(ThemeCatalog.Planche.Key);

        // The paths of each drawing: the fill, four pencil lines and four inked ones; and, under a case in relief, its shadow.
        var drawings = JsonSerializer.Deserialize<Drawing[]>(await page.EvaluateAsync<JsonElement>("""
            () => [...document.querySelectorAll('.block')].map(block => ({
                raised: block.classList.contains('block-raised'),
                shadow: getComputedStyle(block).boxShadow,
                paths: decodeURIComponent(block.style.getPropertyValue('--block-art')).split('<path').length - 1,
            }))
            """), Json)!;

        Assert.Contains(drawings, drawing => drawing.Raised);
        Assert.All(drawings, drawing =>
        {
            Assert.Equal("none", drawing.Shadow);
            Assert.Equal(drawing.Raised ? 10 : 9, drawing.Paths);
        });
    }

    private sealed record Drawing(bool Raised, string Shadow, int Paths);

    [Fact]
    public async Task Balloons_AreCloudsOnQuestionsOnly_AndStarsOnExclamationsOnly_OnePerPageAtMost()
    {
        var page = await OpenAsync(ThemeCatalog.Planche.Key);
        var shapes = new List<string>();

        // Drawn anew at each display: over many displays, every rule holds on each of them.
        for (var display = 0; display < 15; display++)
        {
            if (display > 0)
            {
                await page.ReloadAsync();
                await PageReadiness.WaitAsync(page);
            }
            var cases = await CasesAsync(page);

            Assert.All(cases, @case =>
            {
                Assert.Contains(@case.Shape, Shapes);
                if (@case.Shape == "cloud")
                    Assert.EndsWith("?", @case.Title);
                if (@case.Shape == "star")
                    Assert.EndsWith("!", @case.Title);
            });
            Assert.True(cases.Count(@case => @case.Shape == "star") <= 1, "More than one noise on a page.");
            Assert.True(cases.Count(@case => @case.Shape == "cloud") <= Math.Max(1, cases.Length / 5), "Too many thoughts on a page.");
            shapes.AddRange(cases.Select(@case => @case.Shape!));
        }

        Assert.True(shapes.Count(shape => shape == "smooth") >= 0.75 * shapes.Count, "Speech is not the great majority of the balloons.");
    }

    [Fact]
    public async Task Drawing_IsKept_WhileThePageIsDisplayed_EvenAtAnotherSize()
    {
        var page = await OpenAsync(ThemeCatalog.Planche.Key);
        var before = await CasesAsync(page);

        await page.SetViewportSizeAsync(414, 896);
        await page.WaitForFunctionAsync("art => document.querySelector('.block').style.getPropertyValue('--block-art') !== art", before[0].Art);

        var after = await CasesAsync(page);
        Assert.Equal(before.Select(@case => (@case.Tint, @case.Seed, @case.Shape)), after.Select(@case => (@case.Tint, @case.Seed, @case.Shape)));
    }

    [Fact]
    public async Task EnhancedNavigation_ToANewPage_DrawsItsBlocks()
    {
        var page = await OpenAsync(ThemeCatalog.Planche.Key);

        await page.GetByRole(AriaRole.Link, new() { Name = "Afficher de nouveau" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(CasesPage.Path, "?again=1"));
        await page.WaitForFunctionAsync("() => [...document.querySelectorAll('.block')].every(block => block.dataset.drawn !== undefined)");

        Assert.All(await CasesAsync(page), @case => Assert.NotNull(@case.Shape));
    }

    // A visit whose theme is drawn again (a new date, the browser left open) changes theme on an enhanced navigation of
    // Blazor, which inserts the nodes of the new page without running their scripts.
    [Fact]
    public async Task EnhancedNavigation_IntoThisTheme_DrawsTheBlocksOfTheNewPage()
    {
        var page = await OpenAsync(ThemeCatalog.Base.Key);

        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { [Captures.RequestThemeSelector.HeaderName] = ThemeCatalog.Planche.Key });
        await page.GetByRole(AriaRole.Link, new() { Name = "Afficher de nouveau" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(CasesPage.Path, "?again=1"));
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", ThemeCatalog.Planche.Key);

        await page.WaitForFunctionAsync("() => [...document.querySelectorAll('.block')].every(block => block.dataset.drawn !== undefined)",
            null, new() { Timeout = 5000 });
        Assert.All(await CasesAsync(page), @case => Assert.NotNull(@case.Shape));
    }

    [Fact]
    public async Task EnhancedNavigation_OutOfThisTheme_DrawsNothingOnTheNewPage()
    {
        var page = await OpenAsync(ThemeCatalog.Planche.Key);

        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { [Captures.RequestThemeSelector.HeaderName] = ThemeCatalog.Base.Key });
        await page.GetByRole(AriaRole.Link, new() { Name = "Afficher de nouveau" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(CasesPage.Path, "?again=1"));
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", ThemeCatalog.Base.Key);
        // Past the frame in which the script of the theme would deal and draw the new page.
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

        Assert.All(await CasesAsync(page), @case =>
        {
            Assert.False(@case.Drawn, @case.Title);
            Assert.Null(@case.Shape);
            Assert.Equal(0, @case.Tint);
        });
    }

    [Fact]
    public async Task BaseTheme_DrawsNothing()
    {
        var page = await OpenAsync(ThemeCatalog.Base.Key);

        Assert.All(await CasesAsync(page), @case =>
        {
            Assert.False(@case.Drawn);
            Assert.Null(@case.Shape);
        });
    }

    [Fact]
    public async Task Page_UsesTheFontsOfTheTheme_ServedByTheApplication()
    {
        var page = await OpenAsync(ThemeCatalog.Planche.Key);

        foreach (var family in new[] { "Delius", "Fraunces", "Patrick Hand" })
            Assert.True(await page.EvaluateAsync<bool>(
                "family => [...document.fonts].some(font => font.family.replaceAll('\"', '') === family && font.status === 'loaded')", family),
                $"No face of {family} was loaded from the Application.");
    }

    private async Task<IPage> OpenAsync(string themeKey)
    {
        var page = await NewPageInThemeAsync(themeKey, Desktop);
        await PageReadiness.GotoAsync(page, CasesPage.Path);
        await page.EvaluateAsync("() => document.fonts.ready");
        return page;
    }

    private static async Task<Case[]> CasesAsync(IPage page) =>
        JsonSerializer.Deserialize<Case[]>(await page.EvaluateAsync<JsonElement>(Cases), Json)!;

    private sealed record Case(int Tint, string Fill, string Art, bool Drawn, string? Seed, string Title, string? Shape, string Bubble);
}
