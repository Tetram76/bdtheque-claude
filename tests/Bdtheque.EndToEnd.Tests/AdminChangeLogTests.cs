extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The whole change log (visuel/journal/journal-des-modifications.md), on the application shared by the tests: each test names
/// its records with a marker of its own, and searches the change log for it. Its access from the home page and the menu, the
/// history of a record from the menu of one of its entries, its filters, and the record an entry leads to.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminChangeLogTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task HomePage_LeadsToTheWholeChangeLog_AndSoDoesTheMenu(string theme)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Link, new() { Name = "Tout le journal" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminChangeLog));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Journal des modifications");
        await page.GotoAsync(AppPaths.AdminHome);
        await PageReadiness.WaitAsync(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Journal des modifications" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminChangeLog));
    }

    [Theory, InEachTheme]
    // The search finds the record under its label, a modification with its change; the menu of an entry leads to the history of
    // its record, whose filter the page presents and removes.
    public async Task History_OfARecord_FromTheMenuOfOneOfItsEntries(string theme)
    {
        var genre = await CreateGenreAsync($"Polar {_marker}");
        await RenameGenreAsync(genre, $"Polar noir {_marker}");
        await CreateGenreAsync($"Western {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminChangeLog}?q={_marker}");
        await Assertions.Expect(Rows(page).Locator(".row-entry")).ToHaveTextAsync([$"Western {_marker}", $"Polar noir {_marker}", $"Polar {_marker}"]);
        await Assertions.Expect(Rows(page).Nth(1).Locator(".log-change")).ToHaveTextAsync([$"Libellé Polar {_marker} → Polar noir {_marker}"]);

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Polar noir {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Historique de cette fiche" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminChangeLog, $"?record={genre.Id}"));
        await Assertions.Expect(Rows(page).Locator(".log-operation")).ToHaveTextAsync(["Modification", "Création"]);
        await page.GetByRole(AriaRole.Link, new() { Name = $"Retirer « Fiche : Genre Polar noir {_marker} »" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminChangeLog));
    }

    [Theory, InEachTheme]
    // The operation chosen in the panel of the filters, with the search kept; the record of an entry, from its menu.
    public async Task Filters_TheOperation_ThenModifyTheRecord(string theme)
    {
        var genre = await CreateGenreAsync($"Aventure {_marker}");
        await RenameGenreAsync(genre, $"Aventures {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminChangeLog}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filtres" }).ClickAsync();
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Opération" }).SelectOptionAsync("Creation");
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Retirer « Opération : Création »" })).ToBeVisibleAsync();
        await Assertions.Expect(Rows(page).Locator(".row-entry")).ToHaveTextAsync([$"Aventure {_marker}"]);
        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Aventure {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Modifier la fiche" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditGenre(genre.Id)));
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(1440)]
    // An entry and its changes hold in the width of the page, a long label and a long value wrapping rather than overflowing.
    public async Task Rows_AtEveryWidth_HoldInThePage(string theme, int width)
    {
        var genre = await CreateGenreAsync($"Les grandes aventures maritimes des capitaines au long cours {_marker}");
        await RenameGenreAsync(genre, $"Les très grandes aventures maritimes et fluviales des capitaines au long cours {_marker}");
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminChangeLog}?q={_marker}");
        await PageReadiness.WaitAsync(page);

        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Rows(IPage page) =>
        page.GetByRole(AriaRole.Table, new() { Name = "Journal des modifications" }).Locator(".log-row:not(.log-row-head)");

    private Task<GenreForm> CreateGenreAsync(string label) => PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest(label));

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }

    private async Task RenameGenreAsync(GenreForm genre, string label)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PutAsJsonAsync($"/admin/genres/{genre.Id}", new UpdateGenreRequest(label, genre.Version));
        response.EnsureSuccessStatusCode();
    }
}
