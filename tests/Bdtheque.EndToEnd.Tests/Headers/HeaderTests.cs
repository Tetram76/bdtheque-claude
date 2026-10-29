extern alias frontend;

using System.Text.RegularExpressions;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Headers;

/// <summary>
/// The headers, the menus and the rows that scroll sideways, in a browser: what only a browser does — the popover of the
/// menu of the administration, the sticky header, the layout on a smartphone, and the script of the rows (wheel, chevrons,
/// centering on the current entry), which the coverage of the .NET tests does not measure.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed partial class HeaderTests(ApplicationHost application)
{
    private static readonly ViewportSize Smartphone = new() { Width = 414, Height = 896 };

    [Fact]
    public async Task ConsultationMenu_OnASmartphone_ShowsAChevronOnTheSideWithEntriesOutOfView_AndItsChevronScrollsItByItsWidth()
    {
        var page = await OpenAsync(AppPaths.Home, Smartphone);
        var row = page.Locator("app-scroll-row.consultation-menu");
        var items = row.Locator(".scroll-row-items");

        await Assertions.Expect(row.Locator(".scroll-row-chevron[data-direction='end']")).ToBeVisibleAsync();
        await Assertions.Expect(row.Locator(".scroll-row-chevron[data-direction='start']")).ToBeHiddenAsync();
        var sizes = await items.EvaluateAsync<int[]>("items => [items.clientWidth, items.scrollWidth - items.clientWidth]");
        var (width, max) = (sizes[0], sizes[1]);
        Assert.True(max > 0, "The menu does not overflow on a smartphone.");

        await row.Locator(".scroll-row-chevron[data-direction='end']").ClickAsync();

        await page.WaitForFunctionAsync("([selector, expected]) => Math.abs(document.querySelector(selector).scrollLeft - expected) <= 1",
            new object[] { "app-scroll-row.consultation-menu .scroll-row-items", Math.Min(width, max) });
        await Assertions.Expect(row.Locator(".scroll-row-chevron[data-direction='start']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ConsultationMenu_MarksTheCurrentPage_AndLeadsToTheOtherEntries()
    {
        var page = await OpenAsync(AppPaths.Home);
        var menu = page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" });

        await Assertions.Expect(menu.Locator("a[aria-current='page']")).ToHaveTextAsync("Accueil");

        await menu.GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();

        await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == AppPaths.Albums);
    }

    [Fact]
    public async Task InitialNavigation_IsCenteredOnItsCurrentEntry_AndScrollsWithTheWheel()
    {
        var page = await OpenAsync($"{AdminFramePage.Path}?entry=M", Smartphone);
        var row = page.Locator("app-scroll-row:has(.initial-navigation)");
        var items = row.Locator(".scroll-row-items");

        // The current entry in the middle of the visible part of the row, to the pixel.
        var offset = await items.EvaluateAsync<double>("""
            items => {
                const current = items.querySelector('[aria-current="page"]').getBoundingClientRect();
                const box = items.getBoundingClientRect();
                return (current.left + current.width / 2) - (box.left + box.width / 2);
            }
            """);
        Assert.InRange(offset, -1, 1);
        await Assertions.Expect(row.Locator(".scroll-row-chevron[data-direction='start']")).ToBeVisibleAsync();

        var before = await items.EvaluateAsync<double>("items => items.scrollLeft");
        await items.HoverAsync();
        await page.Mouse.WheelAsync(0, 120);

        await page.WaitForFunctionAsync("([selector, before]) => document.querySelector(selector).scrollLeft > before",
            new object[] { "app-scroll-row:has(.initial-navigation) .scroll-row-items", before });
    }

    [Fact]
    public async Task InitialNavigation_IsCenteredAgainOnItsNewEntry_AfterAnEnhancedNavigation()
    {
        var page = await OpenAsync($"{AdminFramePage.Path}?entry=B", Smartphone);
        var navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" });

        await navigation.GetByRole(AriaRole.Link, new() { Name = "H", Exact = true }).ClickAsync();
        await Assertions.Expect(navigation.Locator("a[aria-current='page']")).ToHaveTextAsync("H");

        await page.WaitForFunctionAsync("""
            () => {
                const items = document.querySelector('app-scroll-row:has(.initial-navigation) .scroll-row-items');
                const current = items.querySelector('[aria-current="page"]').getBoundingClientRect();
                const box = items.getBoundingClientRect();
                return Math.abs((current.left + current.width / 2) - (box.left + box.width / 2)) <= 1;
            }
            """, null, new() { Timeout = 5000 });
    }

    [Fact]
    public async Task InitialNavigation_LeadsToTheListOnAnEntry()
    {
        var page = await OpenAsync(AdminFramePage.Path);
        var navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" });
        await Assertions.Expect(navigation.Locator("a[aria-current='page']")).ToHaveTextAsync("Toutes");

        await navigation.GetByRole(AriaRole.Link, new() { Name = "#", Exact = true }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(EntryNumberUrl());
        await Assertions.Expect(navigation.Locator("a[aria-current='page']")).ToHaveTextAsync("#");
    }

    [Fact]
    public async Task Pagination_LeadsToTheNextPage()
    {
        var page = await OpenAsync(AdminFramePage.Path);
        var pagination = page.GetByRole(AriaRole.Navigation, new() { Name = "Pages de la liste" });

        await pagination.GetByRole(AriaRole.Link, new() { Name = "Suivante" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(SecondPageUrl());
        await Assertions.Expect(pagination.Locator("[aria-current='page']")).ToHaveTextAsync("2");
    }

    [Fact]
    public async Task AdminMenu_OpensFromItsButton_OverThePage_WhichStaysStill_AndClosesOnAClickOutside()
    {
        var page = await OpenAsync(AdminFramePage.Path);
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true });
        var menu = page.Locator("#admin-menu");
        var main = await page.Locator("main").BoundingBoxAsync();

        await toggle.ClickAsync();

        await Assertions.Expect(menu).ToBeVisibleAsync();
        var (button, panel) = (await toggle.BoundingBoxAsync(), await menu.BoundingBoxAsync());
        Assert.True(panel!.Y >= button!.Y + button.Height, "The menu is not under its button.");
        Assert.Equal(button.X + button.Width, panel.X + panel.Width, 1);
        // Over the content, which it never pushes aside.
        Assert.Equal(main!.Y, (await page.Locator("main").BoundingBoxAsync())!.Y);
        Assert.Equal("hidden", await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).overflow"));

        await page.Locator("main h1").ClickAsync();

        await Assertions.Expect(menu).ToBeHiddenAsync();
        Assert.NotEqual("hidden", await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).overflow"));
    }

    [Fact]
    public async Task AdminMenu_ClosesByItsCross_AndByItsButton_AndLeadsToItsEntries()
    {
        var page = await OpenAsync(AdminFramePage.Path);
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true });
        var menu = page.Locator("#admin-menu");

        await toggle.ClickAsync();
        await menu.GetByRole(AriaRole.Button, new() { Name = "Fermer le menu" }).ClickAsync();
        await Assertions.Expect(menu).ToBeHiddenAsync();

        await toggle.ClickAsync();
        await toggle.ClickAsync();
        await Assertions.Expect(menu).ToBeHiddenAsync();

        await toggle.ClickAsync();
        await menu.GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();
        await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == AppPaths.AdminAlbums);
    }

    [Fact]
    public async Task Header_StaysAtTheTopOfTheScreen_WhenThePageScrolls()
    {
        var page = await OpenAsync(AdminFramePage.Path, new ViewportSize { Width = 1440, Height = 300 });

        await page.EvaluateAsync("() => window.scrollTo(0, document.documentElement.scrollHeight)");

        Assert.True(await page.EvaluateAsync<double>("() => window.scrollY") > 0, "The page does not scroll.");
        Assert.Equal(0, (await page.Locator("header.app-header").BoundingBoxAsync())!.Y);
    }

    [Fact]
    public async Task AdminHeader_OnASmartphone_TakesTwoLines_TheSearchUnderTheMenuButton()
    {
        var page = await OpenAsync(AdminFramePage.Path, Smartphone);

        var toggle = await page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true }).BoundingBoxAsync();
        var search = await page.GetByRole(AriaRole.Searchbox, new() { Name = "Retrouver une fiche" }).BoundingBoxAsync();
        var logo = await page.Locator("a.app-logo").BoundingBoxAsync();

        Assert.True(search!.Y >= toggle!.Y + toggle.Height, "The search is not under the line of the menu button.");
        Assert.True(toggle.Y < logo!.Y + logo.Height, "The menu button is not on the line of the logo.");
        Assert.Equal(toggle.Height, toggle.Width, 1);
    }

    [Fact]
    public async Task ConsultationHeader_OnASmartphone_PutsTheSearchUnderTheMenu()
    {
        var page = await OpenAsync(AppPaths.Home, Smartphone);

        var menu = await page.Locator("app-scroll-row.consultation-menu").BoundingBoxAsync();
        var search = await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher" }).BoundingBoxAsync();

        Assert.True(search!.Y >= menu!.Y + menu.Height, "The search is not under the menu.");
    }

    [GeneratedRegex(@"\?entry=%23$")]
    private static partial Regex EntryNumberUrl();

    [GeneratedRegex(@"\?page=2$")]
    private static partial Regex SecondPageUrl();

    private async Task<IPage> OpenAsync(string path, ViewportSize? viewport = null)
    {
        var page = await application.NewPageAsync(viewport ?? new ViewportSize { Width = 1440, Height = 900 });
        await page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        return page;
    }
}
