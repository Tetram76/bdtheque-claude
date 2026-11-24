extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the ISBN to check (visuel/listes/liste-par-liste.md, fonctionnel/regles-metier/validation-de-l-isbn.md), on the
/// application shared by the tests: each test names its publisher with a marker of its own, and narrows the list to it. Its
/// actions, forms of their own sent by the browser: the correction, the acceptance of an error and its revocation.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminInvalidIsbnsTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task HomePage_LeadsToTheList_WhoseFilDAraneLeadsBack(string theme)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Link, new() { Name = "ISBN au chiffre de contrôle incohérent" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminInvalidIsbns));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("ISBN au chiffre de contrôle incohérent");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Theory, InEachTheme]
    public async Task Correct_PutsTheCorrectedIsbnInPlace_AndTakesTheEditionOutOfTheList(string theme)
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var album = await CreateAlbumAsync($"Barracuda {_marker}");
        var edition = await CreateEditionAsync(album, publisher, "978-2-505-00865-3");
        var page = await OpenAsync(theme, $"{AppPaths.AdminInvalidIsbns}?publisherId={publisher.Id}");
        await Assertions.Expect(Rows(page).Locator(".isbn-corrected .isbn-value")).ToHaveTextAsync(["978-2-505-00865-1"]);

        await page.GetByRole(AriaRole.Button, new() { Name = $"Corriger l'ISBN de Dargaud {_marker}" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminInvalidIsbns, $"?publisherId={publisher.Id}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("Aucune édition ne correspond à ces critères.");
        Assert.Equal("978-2-505-00865-1", (await ReadEditionAsync(album, edition)).Content.Isbn);
    }

    [Theory, InEachTheme]
    public async Task Accept_TakesTheErrorOutOfTheList_TheStateShowsIt_AndItIsAcceptedNoMore(string theme)
    {
        var publisher = await CreatePublisherAsync($"Delcourt {_marker}");
        await CreateEditionAsync(await CreateAlbumAsync($"De cape et de crocs {_marker}"), publisher, "978-2-7560-1234-0");
        var page = await OpenAsync(theme, $"{AppPaths.AdminInvalidIsbns}?publisherId={publisher.Id}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Delcourt {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Accepter l'erreur" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("Aucune édition ne correspond à ces critères.");
        await page.GotoAsync($"{AppPaths.AdminInvalidIsbns}?state=All&publisherId={publisher.Id}");
        await PageReadiness.WaitAsync(page);
        await Assertions.Expect(Rows(page).Locator(".isbn-state")).ToHaveTextAsync(["Acceptée"]);
        await Assertions.Expect(page.Locator(".list-summary")).ToContainTextAsync("1 édition dont 1 acceptée");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Delcourt {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Ne plus accepter l'erreur" }).ClickAsync();

        await Assertions.Expect(Rows(page).Locator(".isbn-state")).ToHaveTextAsync(["À corriger"]);
    }

    [Theory, InEachTheme]
    public async Task Filter_OnTheState_ShowsTheErrorsAccepted_AndItsTagLeadsBackToThoseToCorrect(string theme)
    {
        var publisher = await CreatePublisherAsync($"Glénat {_marker}");
        await CreateEditionAsync(await CreateAlbumAsync($"Les Passagers du vent {_marker}"), publisher, "2-7234-3451");
        var page = await OpenAsync(theme, $"{AppPaths.AdminInvalidIsbns}?publisherId={publisher.Id}&filters=open");

        await page.GetByRole(AriaRole.Combobox, new() { Name = "État" }).SelectOptionAsync("All");
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        // The form of the filters sends its fields as they are, empty ones included, as in every list.
        await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "État" })).ToBeVisibleAsync();
        await Assertions.Expect(Rows(page).Locator(".isbn-state")).ToHaveTextAsync(["À corriger"]);
        await page.GetByRole(AriaRole.Link, new() { Name = "Retirer « État : Toutes »" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminInvalidIsbns, $"?publisherId={publisher.Id}"));
    }

    [Theory, InEachTheme]
    // An ISBN no check character corrects: its main action modifies the edition, here the one an intent targets.
    public async Task Modify_TheIntentWhoseIsbnNoCheckCharacterCorrects_OpensItsEntryForm(string theme)
    {
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}");
        var album = await CreateAlbumAsync($"Buddy Longway {_marker}");
        var edition = await CreateIntentAsync(album, publisher, "2-8001-012");
        var page = await OpenAsync(theme, $"{AppPaths.AdminInvalidIsbns}?publisherId={publisher.Id}");

        await Assertions.Expect(Rows(page).Locator(".isbn-acquisition")).ToHaveTextAsync(["Intention"]);
        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier l'intention : Dupuis {_marker}" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditEdition(album.Id, edition)));
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(1024)]
    [InEachTheme(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheLabelItsRoom_WithoutTheListOverflowing(string theme, int width)
    {
        var publisher = await CreatePublisherAsync($"Les Éditions du Grand Large et des Archipels réunis {_marker}");
        var album = await CreateAlbumAsync($"Les Voyages extraordinaires du capitaine Archibald au-delà des mers {_marker}");
        await CreateEditionAsync(album, publisher, "978-2-7234-4567-1");
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminInvalidIsbns}?state=All&publisherId={publisher.Id}");
        await PageReadiness.WaitAsync(page);

        var label = await page.Locator(".row-entry").EvaluateAsync<double>("label => label.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(label >= 120, $"{theme} at {width}px: the label is {label}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
        // The ISBN, which a line never breaks, ends before the actions: none of it is hidden under them.
        var isbnEnd = await page.Locator(".row-entry .label-isbn").EvaluateAsync<double>("isbn => isbn.getBoundingClientRect().right");
        var actionsStart = await Rows(page).Locator(".isbn-actions").EvaluateAsync<double>(
            "actions => Math.min(...[...actions.querySelectorAll('.button, .actions-menu-toggle')].filter(child => child.getClientRects().length > 0).map(child => child.getBoundingClientRect().left))");
        Assert.True(isbnEnd <= actionsStart, $"{theme} at {width}px: the ISBN ends at {isbnEnd}px, under the actions from {actionsStart}px.");
        if (width == 1440)
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "État" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Modifiée" })).ToBeVisibleAsync();
        }
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Rows(IPage page) =>
        page.GetByRole(AriaRole.Table, new() { Name = "ISBN au chiffre de contrôle incohérent" }).Locator(".isbn-row:not(.isbn-row-head)");

    private Task<PublisherForm> CreatePublisherAsync(string name) =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent(name, null, []));

    private Task<AlbumForm> CreateAlbumAsync(string title) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

    private async Task<Guid> CreateEditionAsync(AlbumForm album, PublisherForm publisher, string isbn) =>
        (await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, null, isbn, null, null, null, null, null, null, false, true, null,
                AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
            await AlbumVersionAsync(album)))).Id;

    // The identifier of the edition the new intent targets.
    private async Task<Guid> CreateIntentAsync(AlbumForm album, PublisherForm publisher, string isbn) =>
        (await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(publisher.Id, null, null, isbn, null, null, null, null, null, null, true),
            await AlbumVersionAsync(album)))).Intents.Single().EditionId!.Value;

    private async Task<uint> AlbumVersionAsync(AlbumForm album)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<AlbumForm>($"/admin/albums/{album.Id}"))!.Version;
    }

    private async Task<EditionForm> ReadEditionAsync(AlbumForm album, Guid edition)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<EditionForm>($"/admin/albums/{album.Id}/editions/{edition}"))!;
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
