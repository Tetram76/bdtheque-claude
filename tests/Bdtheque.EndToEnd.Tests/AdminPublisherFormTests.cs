extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The entry form of a publisher and of its collections (visuel/saisie-editeur/contenu.md), on the application shared by the
/// tests: each test names its records with a marker of its own. Each journey of the administrator, through the browser: reaching
/// the form from the list, creating a publisher with its collections, modifying them, and the error of its saving.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminPublisherFormTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task List_LeadsToTheFormOfAPublisher_AndToANewOne(string theme)
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Glénat {_marker}", null, []));
        var page = await OpenAsync(theme, $"{AppPaths.AdminPublishers}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier Glénat {_marker}" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditPublisher(publisher.Id)));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Modifier l'éditeur");

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Éditeurs" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Nouvel éditeur" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminNewPublisher));
    }

    [Theory, InEachTheme]
    public async Task Create_WithItsCollections_LeadsBackToTheList(string theme)
    {
        var page = await OpenAsync(theme, AppPaths.AdminNewPublisher);
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        await page.GetByLabel("Nom(obligatoire)").FillAsync($"Delcourt {_marker}");
        await page.GetByLabel("Site web").FillAsync("https://www.editions-delcourt.fr");
        await AddCollectionAsync(page, "Shampooing");
        await AddCollectionAsync(page, "Encrages");
        await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'éditeur" }).ClickAsync();

        // A record created leads back to the list (visuel/saisie-album/contenu.md).
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPublishers));
        using var api = Application.CreateApiClient();
        var created = Assert.Single((await api.GetFromJsonAsync<Page<PublisherAdminListItem>>($"/admin/publishers?q={_marker}"))!.Items);
        var saved = await GetAsync(created.Id);
        Assert.Equal("https://www.editions-delcourt.fr", saved.Website);
        // Its collections in the order of their names.
        Assert.Equal(["Encrages", "Shampooing"], saved.Collections.Select(c => c.Name));
    }

    [Theory, InEachTheme]
    public async Task Modify_RenamesRemovesRestoresAndAddsCollections_SavedWithThePublisher(string theme)
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent(
            $"Casterman {_marker}", null, [new(null, "Écritures"), new(null, "Fac-similé"), new(null, "Les Romans (À suivre)")]));
        var page = await OpenAsync(theme, AppPaths.AdminEditPublisher(publisher.Id));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        await page.GetByLabel("Nom de la collection").First.FillAsync("Écritures (poche)");
        await page.GetByRole(AriaRole.Button, new() { Name = "Retirer Fac-similé" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Retirer Les Romans (À suivre)" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Retirée à l'enregistrement")).ToHaveCountAsync(2);
        await page.GetByRole(AriaRole.Button, new() { Name = "Rétablir" }).Last.ClickAsync();
        await Assertions.Expect(page.GetByText("Retirée à l'enregistrement")).ToHaveCountAsync(1);
        await AddCollectionAsync(page, "Sakka");
        await Assertions.Expect(page.GetByText("Modifications non enregistrées")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        // A modification saved leads back to the list (visuel/saisie-album/contenu.md).
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPublishers));
        var saved = await GetAsync(publisher.Id);
        Assert.Equal(["Écritures (poche)", "Les Romans (À suivre)", "Sakka"], saved.Collections.Select(c => c.Name));
    }

    [Theory, InEachTheme]
    public async Task Save_WithoutName_IsToCorrect_OnTheName_WhichGetsTheFocus(string theme)
    {
        var page = await OpenAsync(theme, AppPaths.AdminNewPublisher);
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'éditeur" }).ClickAsync();

        var error = page.GetByRole(AriaRole.Alert);
        await Assertions.Expect(error).ToContainTextAsync("À corriger");
        var name = page.GetByLabel("Nom(obligatoire)");
        await Assertions.Expect(name).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(name).ToBeFocusedAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminNewPublisher));
    }

    /// <summary>
    /// Adds a collection and names it, once its row is rendered by the circuit: named before, the last row would still be
    /// the one before it.
    /// </summary>
    internal static async Task AddCollectionAsync(IPage page, string name)
    {
        var names = page.GetByLabel("Nom de la collection");
        var count = await names.CountAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Ajouter une collection" }).ClickAsync();
        await Assertions.Expect(names).ToHaveCountAsync(count + 1);
        await names.Last.FillAsync(name);
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private async Task<PublisherForm> GetAsync(Guid id)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{id}"))!;
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
