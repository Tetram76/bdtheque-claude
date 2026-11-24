extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using frontend::Bdtheque.Frontend.Components.Pages;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The simple search of the consultation, from its header (fonctionnel/structure-de-l-application.md), on the application shared
/// by the tests, each test naming its records with a marker of its own, and searching it: the records found in the single order of
/// their types, each leading where the consultation leads it, and the list of a type that finds more.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class ConsultationSearchTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task HeaderSearch_FindsTheRecordsOfEveryType_InTheirOrder_AndLeadsWhereTheConsultationLeadsThem(string theme)
    {
        await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        var author = await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Auteur {_marker}", null, null, null, null));
        await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur {_marker}", "https://editeur.example/", []));
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, AppPaths.Authors);

        var field = page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher", Exact = true });
        await field.FillAsync(_marker);
        await field.PressAsync("Enter");

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Search, $"?q={_marker}"));
        // From the container to the content, then the reference data (fonctionnel/presentation/ordre-des-types-de-fiche.md).
        await Assertions.Expect(page.Locator(".search-group h2")).ToHaveTextAsync(["Éditeurs", "Auteurs", "Genres"]);
        await Assertions.Expect(page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher dans la base" })).ToHaveValueAsync(_marker);
        // A publisher leads to its website, out of the application.
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = $"Éditeur {_marker}" })).ToHaveAttributeAsync("href", "https://editeur.example/");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Auteur {_marker}", Exact = true }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AuthorRecord(author.Id)));
    }

    [Theory, InEachTheme]
    public async Task MoreRecordsThanPresented_LeadToTheListOfTheConsultationSearchedOnTheSameText(string theme)
    {
        for (var i = 0; i <= ConsultationSearch.HitCount; i++)
            await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Auteur {i} {_marker}", null, null, null, null));
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Search}?q={_marker}");

        await Assertions.Expect(page.Locator(".author-row:not(.author-row-head)")).ToHaveCountAsync(ConsultationSearch.HitCount);
        await page.GetByRole(AriaRole.Link, new() { Name = $"Voir les {ConsultationSearch.HitCount + 1} auteurs" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Authors, $"?q={_marker}"));
        await Assertions.Expect(page.Locator(".author-row:not(.author-row-head)")).ToHaveCountAsync(ConsultationSearch.HitCount + 1);
    }

    // The cross of the field of the page leads to the search without text, which invites to search.
    [Theory, InEachTheme]
    public async Task ItsCross_LeadsToTheSearchWithoutText(string theme)
    {
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, $"{AppPaths.Search}?q=zz{_marker}");
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Rien ne correspond à « zz{_marker} ».");

        await page.Locator("form.list-search").GetByRole(AriaRole.Link, new() { Name = "Effacer la recherche" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Search));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("Saisissez un titre, un nom ou un ISBN.");
        await Assertions.Expect(page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher dans la base" })).ToHaveValueAsync("");
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
