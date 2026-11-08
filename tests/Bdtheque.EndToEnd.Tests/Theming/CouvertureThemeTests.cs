extern alias frontend;

using System.Globalization;
using System.Net.Http.Json;
using Bdtheque.Contracts.Catalog;
using Bdtheque.EndToEnd.Tests.Headers;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Theming;

/// <summary>
/// « La couverture du jour » (visuel/dashboard/la-couverture-du-jour.md), in a browser: the colours of the cover of the
/// edition in the spotlight on every page, the header of its deep colour and the help a disk of its accent; the albums
/// put forward as books in volume, whose spine is the deep colour of their own cover; their moves left out when the reader
/// asks for fewer animations.
/// </summary>
[Collection(CollectionApplication.Name)]
public sealed class CouvertureThemeTests(CollectionApplication application) : BrowserTest<CollectionApplication>(application)
{
    internal static readonly ViewportSize Desktop = new() { Width = 1440, Height = 900 };

    [Fact]
    public async Task Page_TakesTheColorsOfTheCoverOfTheDay_InItsHeader()
    {
        var palette = await ColorsOfTheDayAsync();
        var page = await OpenAsync(AppPaths.Home);

        Assert.Equal(palette.Deep, await CustomPropertyAsync(page, "--day-deep"));
        Assert.Equal(palette.Dominant, await CustomPropertyAsync(page, "--day-dominant"));
        Assert.Equal(Rgb(palette.Deep), await page.Locator(".app-header").EvaluateAsync<string>("header => getComputedStyle(header).backgroundColor"));
    }

    [Fact]
    public async Task AlbumsPutForward_AreBooksInVolume_WhoseSpineIsTheDeepColorOfTheirCover()
    {
        var palette = await ColorsOfTheDayAsync();
        var page = await OpenAsync(AppPaths.Home);

        var spotlight = page.Locator("[data-area='spotlight'] .cover-volume");
        Assert.Equal(palette.Deep, (await spotlight.EvaluateAsync<string>("book => book.style.getPropertyValue('--cover-spine')")).Trim());
        Assert.NotEqual("none", await spotlight.EvaluateAsync<string>("book => getComputedStyle(book).transform"));
        Assert.Equal(Rgb(palette.Deep), await spotlight.EvaluateAsync<string>(
            "book => getComputedStyle(book, '::before').backgroundImage.match(/rgb\\([^)]*\\)/)[0]"));
        Assert.Equal(["2px", "5px"], await page.Locator("[data-area='spotlight'] .cover").EvaluateAsync<string[]>(
            "cover => [getComputedStyle(cover).borderTopLeftRadius, getComputedStyle(cover).borderTopRightRadius]"));
        await Assertions.Expect(page.Locator("[data-area='latest'] .cover-volume")).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task Moves_AreLeftOut_WhenTheReaderAsksForFewerAnimations()
    {
        var reduced = await OpenAsync(AppPaths.Home);
        var moving = await NewPageAsync(new BrowserNewContextOptions
        {
            ViewportSize = Desktop,
            ReducedMotion = ReducedMotion.NoPreference,
            ExtraHTTPHeaders = new Dictionary<string, string> { [Captures.RequestThemeSelector.HeaderName] = ThemeCatalog.Couverture.Key },
        });
        await PageReadiness.GotoAsync(moving, AppPaths.Home);

        const string Moves = """
            () => [document.querySelector("[data-area='spotlight'] .cover-volume"), document.querySelector("[data-area='latest'] li")]
                .map(element => getComputedStyle(element).animationName)
            """;
        Assert.Equal(["couverture-float", "couverture-arrive"], await moving.EvaluateAsync<string[]>(Moves));
        Assert.Equal(["none", "none"], await reduced.EvaluateAsync<string[]>(Moves));
    }

    [Fact]
    public async Task HelpButton_IsADiskOfTheAccent()
    {
        var page = await OpenAsync(AppPaths.Home);

        var help = await page.Locator(".header-help").EvaluateAsync<string[]>(
            "help => [getComputedStyle(help).backgroundColor, getComputedStyle(help).borderTopLeftRadius]");

        Assert.Equal([Rgb((await CustomPropertyAsync(page, "--day-accent"))!), "50%"], help);
    }

    [Fact]
    public async Task FocusInTheHeader_IsOfItsWhiteText()
    {
        var page = await OpenAsync(AppPaths.Home);
        var help = page.Locator(".header-help");

        await help.FocusAsync();

        Assert.Equal("rgb(255, 255, 255)", await help.EvaluateAsync<string>("help => getComputedStyle(help).outlineColor"));
    }

    [Fact]
    public async Task Page_UsesInterTight_ServedByTheApplication()
    {
        var page = await OpenAsync(AppPaths.Home);

        Assert.True(await page.EvaluateAsync<bool>(
            "() => [...document.fonts].some(font => font.family.replaceAll('\"', '') === 'Inter Tight' && font.status === 'loaded')"),
            "No face of Inter Tight was loaded from the application.");
    }

    private async Task<CoverPalette> ColorsOfTheDayAsync()
    {
        using var api = Application.CreateApiClient();
        // The day of the browser of the tests (Europe/Paris), that of the spotlight; a single edition has a cover.
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris")).DateTime);
        var colors = await api.GetFromJsonAsync<SpotlightColors>($"/catalog/spotlight/colors?day={day:yyyy-MM-dd}");
        return colors!.Palette!;
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageInThemeAsync(ThemeCatalog.Couverture.Key, Desktop);
        await PageReadiness.GotoAsync(page, path);
        await page.EvaluateAsync("() => document.fonts.ready");
        return page;
    }

    internal static async Task<string?> CustomPropertyAsync(IPage page, string name) =>
        (await page.EvaluateAsync<string>("name => getComputedStyle(document.documentElement).getPropertyValue(name)", name))?.Trim();

    internal static string Rgb(string hex)
    {
        int Channel(int start) => int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return $"rgb({Channel(1)}, {Channel(3)}, {Channel(5)})";
    }
}

/// <summary>Without an edition in the spotlight, the page takes the colours of the theme, and awaits its first album.</summary>
[Collection(EmptyCollectionApplication.Name)]
public sealed class EmptyCollectionCouvertureThemeTests(EmptyCollectionApplication application) : BrowserTest<EmptyCollectionApplication>(application)
{
    [Fact]
    public async Task Page_TakesTheOwnColorsOfTheTheme_AndAwaitsItsFirstAlbum()
    {
        var page = await NewPageInThemeAsync(ThemeCatalog.Couverture.Key, CouvertureThemeTests.Desktop);
        await PageReadiness.GotoAsync(page, AppPaths.Home);

        Assert.Equal(DayPalette.Own.Deep, await CouvertureThemeTests.CustomPropertyAsync(page, "--day-deep"));
        Assert.Equal(DayPalette.Own.Accent, await CouvertureThemeTests.CustomPropertyAsync(page, "--day-accent"));
        await Assertions.Expect(page.Locator(".empty-collection-first-album")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".empty-collection-badge")).ToHaveTextAsync("Collection à démarrer");
        await Assertions.Expect(page.Locator(".empty-collection-shelf")).ToBeHiddenAsync();
    }
}

/// <summary>On the pages of the administration too, the header of the deep colour (visuel/administration/declinaison-par-theme.md).</summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdministrationCouvertureThemeTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    [Fact]
    public async Task Header_IsOfTheDeepColor_AndBlocksAreWhite_WithLargeRoundedCorners()
    {
        var page = await NewPageInThemeAsync(ThemeCatalog.Couverture.Key, CouvertureThemeTests.Desktop);
        await PageReadiness.GotoAsync(page, AdminFramePage.Path);

        Assert.Equal(CouvertureThemeTests.Rgb(DayPalette.Own.Deep),
            await page.Locator(".app-header").EvaluateAsync<string>("header => getComputedStyle(header).backgroundColor"));
        var block = await page.Locator(".block").First.EvaluateAsync<string[]>(
            "block => [getComputedStyle(block).borderTopWidth, getComputedStyle(block).borderTopLeftRadius, getComputedStyle(block).boxShadow]");
        Assert.Equal("0px", block[0]);
        Assert.Equal("26px", block[1]);
        Assert.NotEqual("none", block[2]);
    }
}

/// <summary>A thumbnail of a list is no book: its corners are regular, the profile of a book being for the albums in volume.</summary>
[Collection(AdministrationApplication.Name)]
public sealed class ListCouvertureThemeTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    [Fact]
    public async Task ThumbnailsOfAList_HaveRegularCorners()
    {
        var page = await NewPageInThemeAsync(ThemeCatalog.Couverture.Key, CouvertureThemeTests.Desktop);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminAlbums);
        await PageReadiness.WaitAsync(page);

        var corners = await page.Locator(".cover").First.EvaluateAsync<string[]>("""
            cover => ['TopLeft', 'TopRight', 'BottomRight', 'BottomLeft'].map(corner => getComputedStyle(cover)[`border${corner}Radius`])
            """);

        Assert.Equal(["6px", "6px", "6px", "6px"], corners);
    }
}
