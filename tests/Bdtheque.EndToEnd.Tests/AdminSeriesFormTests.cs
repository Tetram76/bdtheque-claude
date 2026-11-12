extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The entry form of a series (visuel/saisie-serie/contenu.md), on the application shared by the tests: each test names its
/// records with a marker of its own. Each journey of the administrator, through the browser: creating a series with its
/// publisher and collection, modifying it, and the error of its saving. What it shares with the form of an album (authors,
/// genres and universes added on the spot, a record modified meanwhile) is exercised by the tests of that one.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminSeriesFormTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task List_LeadsToTheFormOfASeries_AndToANewOne(string theme)
    {
        var series = await PostAsync<SeriesForm>("/admin/series", Content($"Les Schtroumpfs {_marker}"));
        var page = await OpenAsync(theme, $"{AppPaths.AdminSeries}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier Les Schtroumpfs {_marker}" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditSeries(series.Id)));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Modifier la série");

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Séries" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Nouvelle série" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminNewSeries));
    }

    [Theory, InEachTheme]
    public async Task Create_WithItsPublisherChosenByTypingItsName_AndItsCollectionInTheList_LeadsBackToTheList(string theme)
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur{_marker}", null, []));
        var collection = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest($"Collection{_marker}", publisher.Version));
        var page = await OpenAsync(theme, AppPaths.AdminNewSeries);
        await ControlsPage.WaitUntilInteractiveAsync(page);

        await page.GetByLabel("Titre").FillAsync($"Le Scorpion {_marker}");
        await Assertions.Expect(page.GetByLabel("Clé de tri")).ToHaveValueAsync($"Scorpion {_marker} [Le]");
        // Its new editions proposing what any new edition is proposed.
        await Assertions.Expect(page.GetByLabel("Reliure")).ToHaveValueAsync(nameof(BindingType.Hardcover));
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Éditeur", Exact = true }).PressSequentiallyAsync($"Éditeur{_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Éditeur{_marker}" }).ClickAsync();
        var collectionField = page.GetByRole(AriaRole.Combobox, new() { Name = "Collection éditeur" });
        await Assertions.Expect(collectionField).ToBeEnabledAsync();
        await collectionField.SelectOptionAsync(new SelectOptionValue { Label = $"Collection{_marker}" });
        await page.GetByLabel("Couleur", new() { Exact = true }).SelectOptionAsync("Noir et blanc");
        await page.GetByRole(AriaRole.Button, new() { Name = "Créer la série" }).ClickAsync();

        // A record created leads back to the list (visuel/saisie-album/contenu.md).
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSeries));
        using var api = Application.CreateApiClient();
        var created = Assert.Single((await api.GetFromJsonAsync<SeriesAdminList>($"/admin/series?q={_marker}"))!.Series.Items);
        var template = (await GetAsync(created.Id)).Content.EditionTemplate;
        Assert.Equal((publisher.Id, collection.Id, EditionCondition.Excellent, false),
            (template.PublisherId, template.PublisherCollectionId, template.Condition, template.IsColor));
    }

    [Theory, InEachTheme]
    public async Task Collection_UnavailableWithoutPublisher_RemovedWhenThePublisherChanges(string theme)
    {
        var first = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Premier{_marker}", null, []));
        var created = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{first.Id}/collections", new CreatePublisherCollectionRequest($"Collection{_marker}", first.Version));
        await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Second{_marker}", null, []));
        var page = await OpenAsync(theme, AppPaths.AdminNewSeries);
        await ControlsPage.WaitUntilInteractiveAsync(page);
        var publisher = page.GetByRole(AriaRole.Combobox, new() { Name = "Éditeur", Exact = true });
        var collection = page.GetByRole(AriaRole.Combobox, new() { Name = "Collection éditeur" });
        await Assertions.Expect(collection).ToBeDisabledAsync();

        await publisher.PressSequentiallyAsync($"Premier{_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Premier{_marker}" }).ClickAsync();
        await Assertions.Expect(collection).ToBeEnabledAsync();
        await collection.SelectOptionAsync(new SelectOptionValue { Label = $"Collection{_marker}" });
        await Assertions.Expect(collection).ToHaveValueAsync(created.Id.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = $"Retirer « Premier{_marker} »" }).ClickAsync();
        await Assertions.Expect(publisher).ToHaveValueAsync("");
        await publisher.PressSequentiallyAsync($"Second{_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Second{_marker}" }).ClickAsync();

        await Assertions.Expect(collection).ToBeEnabledAsync();
        await Assertions.Expect(collection).ToHaveValueAsync("");
    }

    [Theory, InEachTheme]
    public async Task Modify_SavesTheWholeForm_AndStaysOnIt(string theme)
    {
        var series = await PostAsync<SeriesForm>("/admin/series", Content($"Série {_marker}"));
        var page = await OpenAsync(theme, AppPaths.AdminEditSeries(series.Id));
        await ControlsPage.WaitUntilInteractiveAsync(page);

        await page.GetByLabel("Statut").SelectOptionAsync("Terminée");
        await page.GetByLabel("Tomes numérotés prévus").FillAsync("6");
        await page.GetByLabel("Complète dans la collection").CheckAsync();
        await page.GetByLabel("Exclure des estimations de sortie").CheckAsync();
        await page.GetByLabel("Reliure").SelectOptionAsync("Brochée");
        await Assertions.Expect(page.GetByText("Modifications non enregistrées")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        // A modification saved leads back to the list (visuel/saisie-album/contenu.md).
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSeries));
        var saved = (await GetAsync(series.Id)).Content;
        Assert.Equal((SeriesStatus.Completed, 6, true, false, true, BindingType.Paperback),
            (saved.Status, saved.TheoreticalVolumeCount, saved.IsComplete, saved.ExcludeFromMissingVolumes, saved.ExcludeFromReleaseEstimates,
                saved.EditionTemplate.Binding));
    }

    [Theory, InEachTheme]
    public async Task Save_WithoutTitle_IsToCorrect_OnTheTitle_WhichGetsTheFocus(string theme)
    {
        var page = await OpenAsync(theme, AppPaths.AdminNewSeries);
        await ControlsPage.WaitUntilInteractiveAsync(page);

        // From the bottom of the form, where the title is out of sight.
        await page.Mouse.WheelAsync(0, 10_000);
        await page.GetByRole(AriaRole.Button, new() { Name = "Créer la série" }).ClickAsync();

        var error = page.GetByRole(AriaRole.Alert);
        await Assertions.Expect(error).ToContainTextAsync("À corriger");
        var title = page.GetByLabel("Titre");
        await Assertions.Expect(title).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(title).ToBeFocusedAsync();
        await Assertions.Expect(title).ToBeInViewportAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminNewSeries));
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static SeriesContent Content(string title) =>
        new(title, null, null, null, false, false, false, null, null, new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null),
            [], [], []);

    private async Task<SeriesForm> GetAsync(Guid id)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<SeriesForm>($"/admin/series/{id}"))!;
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
