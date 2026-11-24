extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the authors of the consultation, on the application shared by the tests, in each offered theme: each test names
/// its authors with a marker of its own, and searches it.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AuthorListTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList(string theme)
    {
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, AppPaths.Home);

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" }).GetByRole(AriaRole.Link, new() { Name = "Auteurs" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Authors));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Auteurs");
    }

    [Theory, InEachTheme]
    public async Task Possession_LimitsTheListToTheAuthorsOfTheCollection_WhichTheNavigationByInitialKeeps(string theme)
    {
        var owner = await CreateAuthorAsync($"Possesseur {_marker}");
        var album = await CreateAlbumAsync(owner);
        await CreateOwnedEditionAsync(album);
        await CreateAlbumAsync(await CreateAuthorAsync($"Pas possesseur {_marker}"));
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Authors}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filtres" }).ClickAsync();
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Possession" }).SelectOptionAsync("Possédé");
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Possesseur {_marker}"]);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" }).GetByRole(AriaRole.Link, new() { Name = "P", Exact = true }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Authors, $"?q={_marker}&entry=P&state=InCollection"));
        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Possesseur {_marker}"]);
    }

    [Theory, InEachTheme]
    public async Task ReturnFromARecord_ByItsFilDAriane_FindsTheListAsLeft_AndTheMenu_ItsFiltersOnly(string theme)
    {
        var author = await CreateAuthorAsync($"Retour {_marker}");
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Authors}?q={_marker}&entry=R&state=OutsideCollection");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Retour {_marker}" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AuthorRecord(author.Id)));
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Auteurs" }).ClickAsync();

        // Back where the user was (fonctionnel/structure-de-l-application.md, Retour à une liste).
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Authors, $"?q={_marker}&entry=R&state=OutsideCollection"));
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" }).GetByRole(AriaRole.Link, new() { Name = "Auteurs" }).ClickAsync();
        // To browse the list, from its start, within the filters chosen.
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Authors, "?state=OutsideCollection"));
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(768)]
    [InEachTheme(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheNameItsRoom_WithoutTheListOverflowing(string theme, int width)
    {
        var author = await PostAsync<AuthorForm>(
            "/admin/authors", new CreateAuthorRequest($"Van den Abeele-Quatremare {_marker}", "Marie-Clémentine", null, null, "Néo-Zélandaise"));
        var album = await CreateAlbumAsync(author);
        await CreateOwnedEditionAsync(album);
        await CreateAlbumAsync(author);
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await PageReadiness.GotoAsync(page, $"{AppPaths.Authors}?q={_marker}");

        var name = await page.Locator(".row-entry").EvaluateAsync<double>("entry => entry.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(name >= 120, $"{theme} at {width}px: the name is {name}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
    }

    private static ILocator Names(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Auteurs" }).Locator(".row-entry a");

    private Task<AuthorForm> CreateAuthorAsync(string pseudonym) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(null, null, pseudonym, null, null));

    private Task<AlbumForm> CreateAlbumAsync(AuthorForm author) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            $"Album {Guid.NewGuid():N}", null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [],
            [new ContributionContent(author.Id, ContributionRole.Scenarist)]));

    private async Task CreateOwnedEditionAsync(AlbumForm album)
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur {Guid.NewGuid():N}", null, []));
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
