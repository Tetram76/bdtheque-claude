extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the series of the administration (visuel/listes/liste-par-liste.md), on the application shared by the tests:
/// each test names its series with a marker of its own, and searches it. What the list shares with that of the albums (the
/// modal dialog, Escape) is pinned by AdminAlbumsTests.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminSeriesTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList_WhoseFilDAraneLeadsBackToTheAdministration(string theme)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Séries" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSeries));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Séries");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Theory, InEachTheme]
    public async Task Search_ThenTheNavigationByInitial_NarrowTheList(string theme)
    {
        await CreateSeriesAsync($"Barracuda {_marker}");
        await CreateSeriesAsync($"Le Lotus bleu {_marker}");
        var page = await OpenAsync(theme, AppPaths.AdminSeries);

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une série" }).FillAsync(_marker);
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une série" }).PressAsync("Enter");

        await Assertions.Expect(Titles(page)).ToHaveTextAsync([$"Barracuda {_marker}", $"Le Lotus bleu {_marker}"]);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" }).GetByRole(AriaRole.Link, new() { Name = "L", Exact = true }).ClickAsync();
        await Assertions.Expect(Titles(page)).ToHaveTextAsync([$"Le Lotus bleu {_marker}"]);
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSeries, $"?q={_marker}&entry=L"));
    }

    [Theory, InEachTheme]
    public async Task Filter_ChosenByTypingItsName_NarrowsTheList_AndItsTagRemovesIt(string theme)
    {
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        await CreateSeriesAsync($"Avec le genre {_marker}", [genre.Id]);
        await CreateSeriesAsync($"Sans le genre {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminSeries}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filtres" }).ClickAsync();
        await ControlsPage.WaitUntilInteractiveAsync(page);
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Genre" }).PressSequentiallyAsync($"Genre {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Genre {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        await Assertions.Expect(Titles(page)).ToHaveTextAsync([$"Avec le genre {_marker}"]);
        await page.GetByRole(AriaRole.Link, new() { Name = $"Retirer « Genre : Genre {_marker} »" }).ClickAsync();
        await Assertions.Expect(Titles(page)).ToHaveTextAsync([$"Avec le genre {_marker}", $"Sans le genre {_marker}"]);
    }

    [Theory, InEachTheme]
    public async Task Deletion_ConfirmedInAModalDialog_TakesTheSeriesOutOfTheList(string theme)
    {
        await CreateSeriesAsync($"À supprimer {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminSeries}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur À supprimer {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer cette série ?" });
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSeries, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Aucune série ne correspond à « {_marker} ».");
    }

    [Theory, InEachTheme]
    public async Task Deletion_OfASeriesWithAlbums_IsRefused_AndSaysWhatToDo(string theme)
    {
        var series = await CreateSeriesAsync($"Avec un album {_marker}");
        await PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            null, null, series.Id, AlbumType.Regular, false, 1, null, null, null, null, null, null, null, [], [], []));
        var page = await OpenAsync(theme, $"{AppPaths.AdminSeries}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Avec un album {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Suppression impossible" });
        await Assertions.Expect(dialog).ToContainTextAsync("1 album");
        await Assertions.Expect(dialog).ToContainTextAsync("Rattachez d'abord ses albums à une autre série");
        await Assertions.Expect(dialog.GetByRole(AriaRole.Link, new() { Name = "Fermer" })).ToBeVisibleAsync();
        await Assertions.Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" })).ToHaveCountAsync(0);
    }

    [Theory, InEachTheme]
    public async Task Merge_SearchesTheOtherSeries_AndContinuesWithTheOneChosen(string theme)
    {
        var series = await CreateSeriesAsync($"Barracuda {_marker}");
        var other = await CreateSeriesAsync($"Barracuda bis {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminSeries}?q={_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Barracuda {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Fusionner avec une autre série…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux séries" });

        await dialog.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher l'autre série" }).FillAsync(_marker);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Rechercher" }).ClickAsync();
        await Assertions.Expect(dialog.GetByRole(AriaRole.Radio)).ToHaveCountAsync(1);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergeSeries, $"?other={other.Id}&series={series.Id}"));
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(768)]
    [InEachTheme(1024)]
    [InEachTheme(1180)]
    [InEachTheme(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheTitleItsRoom_WithoutTheListOverflowing(string theme, int width)
    {
        await CreateSeriesAsync($"Les Chroniques d'Ombrelune et des veilleurs du port {_marker}", isComplete: true);
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminSeries}?q={_marker}");
        await PageReadiness.WaitAsync(page);

        var title = await page.Locator(".row-entry").EvaluateAsync<double>("title => title.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(title >= 120, $"{theme} at {width}px: the title is {title}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
        // At the width of the mockups' desktop, the table has all its columns, the last modification included.
        if (width == 1440)
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Modifiée" })).ToBeVisibleAsync();
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Titles(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Séries" }).Locator(".row-entry");

    private Task<SeriesForm> CreateSeriesAsync(string title, Guid[]? genreIds = null, bool isComplete = false) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, null, null, isComplete, false, false, null, null, NoTemplate, genreIds ?? [], [], []));

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
