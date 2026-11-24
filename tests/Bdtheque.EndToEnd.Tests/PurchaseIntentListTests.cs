extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The public list of the purchase intents, on the application shared by the tests, in each offered theme: each test names
/// the albums of its intents with a marker of its own, and searches it.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class PurchaseIntentListTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList(string theme)
    {
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, AppPaths.Home);

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" })
            .GetByRole(AriaRole.Link, new() { Name = "Intentions d'acquisition" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.PurchaseIntents));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Intentions d'acquisition");
    }

    [Theory, InEachTheme]
    public async Task Search_ThenTheNavigationByInitial_NarrowTheList_AndARowLeadsToTheRecordOfItsAlbum(string theme)
    {
        var lotus = await CreateAlbumIntentAsync($"Le Lotus bleu {_marker}");
        await CreateAlbumIntentAsync($"Zorglub {_marker}");
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, AppPaths.PurchaseIntents);

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une intention d'acquisition" }).FillAsync(_marker);
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une intention d'acquisition" }).PressAsync("Enter");
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Le Lotus bleu {_marker}", $"Zorglub {_marker}"]);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" }).GetByRole(AriaRole.Link, new() { Name = "L", Exact = true }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.PurchaseIntents, $"?q={_marker}&entry=L"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Le Lotus bleu {_marker}"]);

        await Labels(page).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Album(lotus)));
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(768)]
    [InEachTheme(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheAlbumItsRoom_WithoutTheListOverflowing(string theme, int width)
    {
        await CreateAlbumIntentAsync($"L'Horloger d'Ombrelune et les veilleurs du port {_marker}");
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await PageReadiness.GotoAsync(page, $"{AppPaths.PurchaseIntents}?q={_marker}");

        var label = await page.Locator(".row-entry").EvaluateAsync<double>("label => label.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(label >= 120, $"{theme} at {width}px: the album is {label}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
    }

    private static ILocator Labels(IPage page) =>
        page.GetByRole(AriaRole.Table, new() { Name = "Intentions d'acquisition" }).Locator(".row-entry a");

    /// <returns>The album wished for as a whole.</returns>
    private async Task<Guid> CreateAlbumIntentAsync(string title)
    {
        var album = await PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, album.Version));
        return album.Id;
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
