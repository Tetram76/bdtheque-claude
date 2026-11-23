extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using frontend::Bdtheque.Frontend.Components.Pages.Admin;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The search of a record to modify, from the header of the administration (visuel/administration/en-tete-et-menu.md), on the
/// application shared by the tests, each test naming its records with a marker of its own, and searching it: the records found
/// in the single order of their types, each leading to its page of modification, and the list of a type that finds more.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminSearchTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task HeaderSearch_FindsTheRecordsOfEveryType_InTheirOrder_AndLeadsToTheirModification(string theme)
    {
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Auteur {_marker}", null, null, null, null));
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur {_marker}", null, []));
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);
        await PageReadiness.WaitAsync(page);

        var field = page.GetByRole(AriaRole.Searchbox, new() { Name = "Retrouver une fiche" });
        await field.FillAsync(_marker);
        await field.PressAsync("Enter");

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSearch, $"?q={_marker}"));
        // From the container to the content, then the reference data (fonctionnel/presentation/ordre-des-types-de-fiche.md).
        await Assertions.Expect(page.Locator(".search-group h2")).ToHaveTextAsync(["Éditeurs", "Auteurs", "Genres"]);
        await Assertions.Expect(page.Locator(".search-hit .row-entry")).ToHaveTextAsync([$"Éditeur {_marker}", $"Auteur {_marker}", $"Genre {_marker}"]);
        await Assertions.Expect(page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une fiche à modifier" })).ToHaveValueAsync(_marker);

        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier Éditeur {_marker}" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditPublisher(publisher.Id)));

        await page.GoBackAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier Genre {_marker}" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminGenres, $"?edit={genre.Id}"));
    }

    [Theory, InEachTheme]
    public async Task MoreRecordsThanPresented_LeadToTheListSearchedOnTheSameText(string theme)
    {
        for (var i = 0; i <= AdminSearch.HitCount; i++)
            await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {i} {_marker}"));
        var page = await OpenAsync(theme, $"{AppPaths.AdminSearch}?q={_marker}");

        await Assertions.Expect(page.Locator(".search-hit")).ToHaveCountAsync(AdminSearch.HitCount);
        await page.GetByRole(AriaRole.Link, new() { Name = $"Voir les {AdminSearch.HitCount + 1} genres" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminGenres, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Table, new() { Name = "Genres" }).Locator(".row-entry")).ToHaveCountAsync(AdminSearch.HitCount + 1);
    }

    // The cross of the field of the page leads to the search without text, which invites to search; the field of the header,
    // without a list behind it, stays empty.
    [Theory, InEachTheme]
    public async Task ItsCross_LeadsToTheSearchWithoutText(string theme)
    {
        var page = await OpenAsync(theme, $"{AppPaths.AdminSearch}?q=zz{_marker}");
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Aucune fiche ne correspond à « zz{_marker} ».");

        await page.Locator("form.list-search").GetByRole(AriaRole.Link, new() { Name = "Effacer la recherche" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSearch));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("Saisissez le titre, le nom ou l'ISBN de la fiche à modifier.");
        await Assertions.Expect(page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une fiche à modifier" })).ToHaveValueAsync("");
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
