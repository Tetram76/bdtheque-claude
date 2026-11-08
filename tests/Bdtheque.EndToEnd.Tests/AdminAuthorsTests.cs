extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the authors of the administration (visuel/listes/liste-par-liste.md), on the application shared by the tests:
/// each test names its authors with a marker of its own, and searches it. What the list shares with that of the albums (the
/// modal dialog, Escape) is pinned by AdminAlbumsTests.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminAuthorsTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Menu_LeadsToTheList_WhoseFilDAraneLeadsBackToTheAdministration()
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Auteurs" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAuthors));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Auteurs");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Fact]
    public async Task Search_ThenTheNavigationByInitial_NarrowTheList_ByTheSortKey()
    {
        // Filed by last name, a pseudonym standing for the name: « Gibrat … », then « Gotlib … ».
        await CreateAuthorAsync($"Gibrat {_marker}", "Jean-Pierre", null);
        await CreateAuthorAsync($"Gotlieb {_marker}", "Marcel", $"Gotlib {_marker}");
        await CreateAuthorAsync($"Moebius {_marker}", null, null);
        var page = await OpenAsync(AppPaths.AdminAuthors);

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher un auteur" }).FillAsync(_marker);
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher un auteur" }).PressAsync("Enter");

        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Jean-Pierre Gibrat {_marker}", $"Gotlib {_marker}", $"Moebius {_marker}"]);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" }).GetByRole(AriaRole.Link, new() { Name = "G", Exact = true }).ClickAsync();
        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Jean-Pierre Gibrat {_marker}", $"Gotlib {_marker}"]);
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAuthors, $"?q={_marker}&entry=G"));
    }

    [Fact]
    public async Task Deletion_ConfirmedInAModalDialog_TakesTheAuthorOutOfTheList()
    {
        await CreateAuthorAsync($"Asupprimer {_marker}", null, null);
        var page = await OpenAsync($"{AppPaths.AdminAuthors}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Asupprimer {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer cet auteur ?" });
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAuthors, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Aucun auteur ne correspond à « {_marker} ».");
    }

    [Fact]
    public async Task Deletion_OfACreditedAuthor_IsRefused_AndSaysWhatToDo()
    {
        var author = await CreateAuthorAsync($"Credite {_marker}", null, null);
        await PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            $"Album {_marker}", null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [],
            [new(author.Id, ContributionRole.Illustrator)]));
        var page = await OpenAsync($"{AppPaths.AdminAuthors}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Credite {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Suppression impossible" });
        await Assertions.Expect(dialog).ToContainTextAsync("1 album");
        await Assertions.Expect(dialog).ToContainTextAsync("Retirez d'abord l'auteur des contributions de ces albums");
        await Assertions.Expect(dialog.GetByRole(AriaRole.Link, new() { Name = "Fermer" })).ToBeVisibleAsync();
        await Assertions.Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Merge_SearchesTheOtherAuthor_AndContinuesWithTheOneChosen()
    {
        var author = await CreateAuthorAsync($"Giraud {_marker}", "Jean", null);
        var other = await CreateAuthorAsync($"Giraud {_marker}", null, $"Moebius {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminAuthors}?q={_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Jean Giraud {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Fusionner avec un autre auteur…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux auteurs" });

        await dialog.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher l'autre auteur" }).FillAsync(_marker);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Rechercher" }).ClickAsync();
        await Assertions.Expect(dialog.GetByRole(AriaRole.Radio)).ToHaveCountAsync(1);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergeAuthors, $"?other={other.Id}&author={author.Id}"));
    }

    [Theory]
    [InlineData(414)]
    [InlineData(768)]
    [InlineData(1024)]
    [InlineData(1180)]
    [InlineData(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheNameItsRoom_WithoutTheListOverflowing(int width)
    {
        await CreateAuthorAsync($"Van den Bogaert-Delacroix {_marker}", "Jean-Christophe", null);
        foreach (var theme in new[] { "base", "planche", "gazette" })
        {
            var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
            await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminAuthors}?q={_marker}");
            await PageReadiness.WaitAsync(page);

            var name = await page.Locator(".author-name").EvaluateAsync<double>("name => name.getBoundingClientRect().width");
            var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

            Assert.True(name >= 120, $"{theme} at {width}px: the name is {name}px wide.");
            Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
            // At the width of the mockups' desktop, the table has all its columns, the secondary ones included.
            if (width == 1440)
            {
                await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Nationalité" })).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Modifié" })).ToBeVisibleAsync();
            }
        }
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Names(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Auteurs" }).Locator(".author-name");

    private Task<AuthorForm> CreateAuthorAsync(string lastName, string? firstName, string? pseudonym) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(lastName, firstName, pseudonym, null, null));

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
