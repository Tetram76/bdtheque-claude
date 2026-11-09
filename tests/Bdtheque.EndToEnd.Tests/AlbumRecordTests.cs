extern alias frontend;

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;
using SkiaSharp;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The record of an album in the consultation (visuel/fiche-album/contenu.md), as its visitor goes through it: under the albums
/// of the menu, the authors of a role unfolded and folded, on the application of the records (<see cref="RecordsApplication"/>).
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed partial class AlbumRecordTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    [Fact]
    public async Task Record_IsUnderTheAlbumsOfTheMenu_AndNamesTheAlbum()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Lointaines îles du vent");
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" })
            .GetByRole(AriaRole.Link, new() { Name = "Albums" })).ToHaveAttributeAsync("aria-current", "page");
    }

    [Fact]
    public async Task AuthorsOfARole_OnANarrowScreen_HoldOnOneLine_TheOthersUnfoldAndFold()
    {
        var page = await NewPageAsync(new ViewportSize { Width = 414, Height = 896 });
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));
        var drawing = page.Locator(".contributions app-fold-row").Nth(1);
        // The drawers by their sort key: the last one is folded first.
        var last = drawing.GetByRole(AriaRole.Link, new() { Name = "Inès Vasseur" });

        await Assertions.Expect(last).ToBeHiddenAsync();
        await Assertions.Expect(drawing.Locator(".contributions-more")).ToHaveTextAsync(FoldedCount());
        Assert.True(await OnOneLineAsync(drawing));

        await drawing.Locator(".contributions-more").ClickAsync();
        await Assertions.Expect(last).ToBeVisibleAsync();
        await drawing.GetByText("Réduire").ClickAsync();
        await Assertions.Expect(last).ToBeHiddenAsync();
        Assert.True(await OnOneLineAsync(drawing));
    }

    [Fact]
    public async Task AuthorsOfARole_OnAWideScreen_AllHold_UntilTheScreenNarrows()
    {
        var page = await NewPageAsync(new ViewportSize { Width = 1440, Height = 900 });
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));
        var drawing = page.Locator(".contributions app-fold-row").Nth(1);
        var last = drawing.GetByRole(AriaRole.Link, new() { Name = "Inès Vasseur" });

        await Assertions.Expect(last).ToBeVisibleAsync();
        await Assertions.Expect(drawing.Locator(".contributions-toggle")).ToBeHiddenAsync();

        await page.SetViewportSizeAsync(414, 896);
        await Assertions.Expect(last).ToBeHiddenAsync();
        Assert.True(await OnOneLineAsync(drawing));
    }

    [GeneratedRegex(@"^et \d+ autres?$")]
    private static partial Regex FoldedCount();

    // The names shown of a row, and the link that unfolds the others, all on the line of the first name.
    private static Task<bool> OnOneLineAsync(ILocator row) => row.EvaluateAsync<bool>("""
        row => {
            const shown = [...row.children].filter(child => child.checkVisibility());
            const bottom = shown[0].getBoundingClientRect().bottom;
            return shown.every(child => child.getBoundingClientRect().top < bottom);
        }
        """);

}

/// <summary>
/// An application whose albums present the cases of the record of an album, at fixed dates: an album of a series with two
/// editions in the collection and a collective drawing, attached to a universe within two others; an album targeted by an
/// intent on itself, and one catalogued only. Its cover is the only one,
/// so that the edition in the spotlight, and the colours of the day, are the same at each capture.
/// </summary>
public sealed class RecordsApplication : ApplicationHost
{
    public const string Name = "Records";

    private static readonly DateTimeOffset Start = new(2026, 2, 2, 9, 0, 0, TimeSpan.Zero);

    protected override bool PaintedByItsSpotlight => true;

    /// <summary>The album of the series with editions and a collective drawing.</summary>
    public Guid AlbumId { get; private set; }

    /// <summary>An album of the series targeted by a purchase intent on itself.</summary>
    public Guid IntendedAlbumId { get; private set; }

    /// <summary>An album of the series catalogued only, out of the collection.</summary>
    public Guid CataloguedAlbumId { get; private set; }

    protected override async Task SeedAsync()
    {
        using var api = CreateApiClient();
        ApiClock.UtcNow = Start;
        var casterman = await PostAsync<PublisherForm>(api, "/admin/publishers", new CreatePublisherRequest("Casterman", "https://www.casterman.com/"));
        var collection = await PostAsync<PublisherCollectionForm>(api,
            $"/admin/publishers/{casterman.Id}/collections", new CreatePublisherCollectionRequest("Univers d'auteurs", casterman.Version));
        var aventure = await PostAsync<GenreForm>(api, "/admin/genres", new CreateGenreRequest("Aventure"));
        var pratt = await PostAsync<UniverseForm>(api, "/admin/universes", new CreateUniverseRequest("Univers Hugo Pratt", null, null));
        var cortoUniverse = await PostAsync<UniverseForm>(api, "/admin/universes", new CreateUniverseRequest("Corto Maltese", null, pratt.Id));
        var antilles = await PostAsync<UniverseForm>(api, "/admin/universes", new CreateUniverseRequest("Les Antilles", null, cortoUniverse.Id));
        var hugo = await AuthorAsync(api, "Pratt", "Hugo");
        string[] drawers = ["Inès Vasseur", "Tom Arribas", "Clara Benmansour", "Yann Le Goff", "Sacha Morel", "Nina Okafor", "Paul Delorme", "Agathe Leroux"];
        List<ContributionContent> contributions = [new(hugo.Id, ContributionRole.Scenarist), new(hugo.Id, ContributionRole.Illustrator)];
        foreach (var drawer in drawers)
            contributions.Add(new((await AuthorAsync(api, drawer.Split(' ', 2)[1], drawer.Split(' ', 2)[0])).Id, ContributionRole.Illustrator));
        var series = await PostAsync<SeriesForm>(api, "/admin/series", new SeriesContent(
            "Corto Maltese", null, SeriesStatus.Completed, 12, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [aventure.Id], [], []));

        var album = await AlbumAsync(api, series.Id, "Lointaines îles du vent", 5, contributions, [antilles.Id],
            "Venezuela, vers 1913. Amnésique, Corto Maltese est recueilli par son vieil ami Steiner.");
        AlbumId = album.Id;
        var first = await PostAsync<EditionForm>(api, $"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            Edition(casterman.Id, null, 2001, "2-203-34424-5", AcquisitionMode.Purchase, new DateOnly(2006, 9, 9), 16.95m), album.Version));
        var version = await UploadCoverAsync(api, album.Id, first.Id, first.AlbumVersion);
        await PostAsync<EditionForm>(api, $"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            Edition(casterman.Id, collection.Id, 2010, null, AcquisitionMode.Gift, new DateOnly(2011, 12, 25), null), version));

        var intended = await AlbumAsync(api, series.Id, "Les Celtiques", 6, [new(hugo.Id, ContributionRole.Scenarist)]);
        IntendedAlbumId = intended.Id;
        await PostAsync<PurchaseIntentsForm>(api, $"/admin/albums/{intended.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, intended.Version));
        CataloguedAlbumId = (await AlbumAsync(api, series.Id, "Les Éthiopiques", 7, [new(hugo.Id, ContributionRole.Scenarist)])).Id;
    }

    private async Task<AuthorForm> AuthorAsync(HttpClient api, string lastName, string firstName) =>
        await PostAsync<AuthorForm>(api, "/admin/authors", new CreateAuthorRequest(lastName, firstName, null, null, null));

    private async Task<AlbumForm> AlbumAsync(
        HttpClient api, Guid series, string title, int volume, IReadOnlyList<ContributionContent> contributions, Guid[]? universes = null,
        string? summary = null) =>
        await PostAsync<AlbumForm>(api, "/admin/albums", new AlbumContent(
            title, null, series, AlbumType.Regular, false, volume, null, null, 1970, 11, summary, null, null, [], universes ?? [], contributions));

    private static EditionContent Edition(
        Guid publisher, Guid? collection, int year, string? isbn, AcquisitionMode mode, DateOnly acquired, decimal? amount) =>
        new(publisher, collection, year, isbn, BindingType.Hardcover, BookOrientation.Portrait, null, EditionFormat.Standard, null, null,
            false, true, EditionCondition.Excellent, mode, false, acquired, amount, amount is null ? null : "EUR", false, null, null, null, null);

    private async Task<uint> UploadCoverAsync(HttpClient api, Guid album, Guid edition, uint albumVersion)
    {
        Tick();
        using var bitmap = new SKBitmap(300, 400);
        bitmap.Erase(SKColors.SteelBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        var file = new ByteArrayContent(png.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var form = new MultipartFormDataContent
        {
            { file, UploadVisualFields.File, "visual.png" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent(albumVersion.ToString(CultureInfo.InvariantCulture)), UploadVisualFields.AlbumVersion },
        };
        using var response = await api.PostAsync($"/admin/albums/{album}/editions/{edition}/visuals", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.AlbumVersion;
    }

    // Each write a quarter of an hour after the one before: the dates of the records are known.
    private async Task<TResponse> PostAsync<TResponse>(HttpClient api, string path, object request)
    {
        Tick();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    private void Tick() => ApiClock.UtcNow += TimeSpan.FromMinutes(15);
}

[CollectionDefinition(RecordsApplication.Name)]
public sealed class RecordsCollection : ICollectionFixture<RecordsApplication>;
