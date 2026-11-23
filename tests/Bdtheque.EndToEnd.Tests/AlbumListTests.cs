extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the albums of the consultation, on the application shared by the tests, in each offered theme: each test names
/// its albums with a marker of its own, and searches it.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AlbumListTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList(string theme)
    {
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, AppPaths.Home);

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" }).GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Albums));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Albums");
    }

    [Theory, InEachTheme]
    public async Task Possession_LimitsTheListToTheCollection_WhichTheNavigationByInitialKeeps(string theme)
    {
        var owned = await CreateAlbumAsync($"Possédé {_marker}");
        await CreateOwnedEditionAsync(owned);
        await CreateAlbumAsync($"Catalogué {_marker}");
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Albums}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filtres" }).ClickAsync();
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Possession" }).SelectOptionAsync("Possédé");
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Possédé {_marker}"]);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" }).GetByRole(AriaRole.Link, new() { Name = "P", Exact = true }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Albums, $"?q={_marker}&entry=P&state=InCollection"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Possédé {_marker}"]);
    }

    [Theory, InEachTheme]
    public async Task Row_LeadsToTheRecordOfItsAlbum(string theme)
    {
        var album = await CreateAlbumAsync($"Fiche {_marker}");
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Albums}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Fiche {_marker}" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Album(album.Id)));
    }

    [Theory, InEachTheme]
    public async Task ReturnFromARecord_ByItsFilDAriane_FindsTheListAsLeft_AndTheMenu_ItsFiltersOnly(string theme)
    {
        var album = await CreateAlbumAsync($"Retour {_marker}");
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Albums}?q={_marker}&entry=R&state=OutsideCollection");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Retour {_marker}" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Album(album.Id)));
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();

        // Back where the user was (fonctionnel/structure-de-l-application.md, Retour à une liste).
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Albums, $"?q={_marker}&entry=R&state=OutsideCollection"));
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" }).GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();
        // To browse the list, from its start, within the filters chosen.
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Albums, "?state=OutsideCollection"));
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(768)]
    [InEachTheme(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheLabelItsRoom_WithoutTheListOverflowing(string theme, int width)
    {
        await CreateAlbumAsync($"L'Horloger d'Ombrelune et les veilleurs du port {_marker}");
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await PageReadiness.GotoAsync(page, $"{AppPaths.Albums}?q={_marker}");

        var label = await page.Locator(".row-entry").EvaluateAsync<double>("label => label.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(label >= 120, $"{theme} at {width}px: the label is {label}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
    }

    private static ILocator Labels(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Albums" }).Locator(".row-entry a");

    private Task<AlbumForm> CreateAlbumAsync(string title) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

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
