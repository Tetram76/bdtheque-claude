extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the series of the consultation, on the application shared by the tests, in each offered theme: each test names
/// its series with a marker of its own, and searches it.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class SeriesListTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList(string theme)
    {
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, AppPaths.Home);

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" }).GetByRole(AriaRole.Link, new() { Name = "Séries" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Series));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Séries");
    }

    [Theory, InEachTheme]
    public async Task Possession_LimitsTheListToTheSeriesOfTheCollection_WhichTheNavigationByInitialKeeps(string theme)
    {
        var owned = await CreateSeriesAsync($"Possédée {_marker}");
        await CreateOwnedEditionAsync(await CreateAlbumAsync(owned));
        await CreateSeriesAsync($"Cataloguée {_marker}");
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Series}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filtres" }).ClickAsync();
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Possession" }).SelectOptionAsync("Possédé");
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        await Assertions.Expect(Titles(page)).ToHaveTextAsync([$"Possédée {_marker}"]);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" }).GetByRole(AriaRole.Link, new() { Name = "P", Exact = true }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Series, $"?q={_marker}&entry=P&state=InCollection"));
        await Assertions.Expect(Titles(page)).ToHaveTextAsync([$"Possédée {_marker}"]);
    }

    [Theory, InEachTheme]
    public async Task Row_LeadsToTheRecordOfItsSeries(string theme)
    {
        var series = await CreateSeriesAsync($"Fiche {_marker}");
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Series}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Fiche {_marker}" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.SeriesRecord(series.Id)));
    }

    [Theory, InEachTheme]
    public async Task ReturnFromARecord_ByItsFilDAriane_FindsTheListAsLeft_AndTheMenu_ItsFiltersOnly(string theme)
    {
        var series = await CreateSeriesAsync($"Retour {_marker}");
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Series}?q={_marker}&entry=R&state=OutsideCollection");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Retour {_marker}" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.SeriesRecord(series.Id)));
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Séries" }).ClickAsync();

        // Back where the user was (fonctionnel/structure-de-l-application.md, Retour à une liste).
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Series, $"?q={_marker}&entry=R&state=OutsideCollection"));
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" }).GetByRole(AriaRole.Link, new() { Name = "Séries" }).ClickAsync();
        // To browse the list, from its start, within the filters chosen.
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Series, "?state=OutsideCollection"));
    }

    [Theory, InEachTheme]
    public async Task PileOfCovers_ShowsTheFirstVolumeInFront(string theme)
    {
        await CreateSeriesAsync($"Pile {_marker}");
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Series}?q={_marker}");

        // The card painted at the centre of the place of the first volume, the last card of the pile, is that card itself: the
        // cards behind it, moved up and to the right, never come over it (Components/Presentation/CoverPile.razor.css).
        var inFront = await page.Locator(".cover-pile").EvaluateAsync<bool>(
            """
            pile => {
                const first = pile.lastElementChild;
                const box = first.getBoundingClientRect();
                return first.contains(document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2));
            }
            """);

        Assert.True(inFront, $"{theme}: a card behind the first volume is painted over it.");
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(768)]
    [InEachTheme(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheTitleItsRoom_WithoutTheListOverflowing(string theme, int width)
    {
        await CreateSeriesAsync($"Les Veilleurs d'Ombrelune et les horlogers du port {_marker}");
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await PageReadiness.GotoAsync(page, $"{AppPaths.Series}?q={_marker}");

        var title = await page.Locator(".row-entry").EvaluateAsync<double>("title => title.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(title >= 120, $"{theme} at {width}px: the title is {title}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
    }

    private static ILocator Titles(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Séries" }).Locator(".row-entry a");

    private Task<SeriesForm> CreateSeriesAsync(string title) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(title, null, null, null, false, false, false, null, null, NoTemplate, [], [], []));

    private Task<AlbumForm> CreateAlbumAsync(SeriesForm series) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            null, null, series.Id, AlbumType.Regular, false, 1, null, null, null, null, null, null, null, [], [], []));

    private async Task CreateOwnedEditionAsync(AlbumForm album)
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur {_marker}", null, []));
        await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(
                publisher.Id, null, 2001, null, null, null, null, null, null, null, false, true, null,
                AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
            album.Version));
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
