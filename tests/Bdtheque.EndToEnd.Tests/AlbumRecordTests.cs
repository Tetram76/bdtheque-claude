extern alias frontend;

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;
using SkiaSharp;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The record of an album in the consultation (visuel/fiche-album/contenu.md), as its visitor goes through it: under the albums
/// of the menu, the viewer of the visuals of an edition, the original image, the authors of a role unfolded and folded, on
/// the application of the records (<see cref="RecordsApplication"/>), and the rest of its series.
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

    // In each theme: a theme that draws at random (the balloons of « La planche encrée ») keeps its drawing on the same page.
    public static TheoryData<string> Themes => [.. ThemeCatalog.Offered.Select(theme => theme.Key)];

    [Theory]
    [MemberData(nameof(Themes))]
    public async Task Viewer_OpensOnAThumbnail_GoesToTheNext_AndCloses_ThePageStayingWhereItWas(string theme)
    {
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = 1280, Height = 720 });
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));
        // The editions some way down the screen, the page scrolled.
        await page.EvaluateAsync($"() => window.scrollBy(0, ({EditionsTop}) - 200)");
        var top = await page.EvaluateAsync<double>($"() => {EditionsTop}");
        var viewer = page.GetByRole(AriaRole.Dialog);

        await page.GetByRole(AriaRole.Link, new() { Name = "Planche 1" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(SecondVisualAddress());
        await Assertions.Expect(viewer.Locator(".visual-dialog-title")).ToHaveTextAsync("Planche 1");

        await viewer.GetByRole(AriaRole.Link, new() { Name = "Visuel suivant" }).ClickAsync();
        await Assertions.Expect(viewer.Locator(".visual-dialog-title")).ToHaveTextAsync("Planche 2");
        await Assertions.Expect(viewer.Locator(".visual-position")).ToHaveTextAsync("3 sur 4");

        // Escape follows the closing, back to the record alone.
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(viewer).ToHaveCountAsync(0);
        await Assertions.Expect(page).ToHaveURLAsync(RecordAddress());
        Assert.True(top > 0);
        Assert.Equal(top, await page.EvaluateAsync<double>($"() => {EditionsTop}"), tolerance: 1.0);
    }

    [Fact]
    public async Task OriginalImage_IsServed()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));
        await page.GetByRole(AriaRole.Link, new() { Name = "Couverture", Exact = true }).ClickAsync();

        var href = await page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Link, new() { Name = "Voir l'image originale" }).GetAttributeAsync("href");
        var response = await page.APIRequest.GetAsync(href!);

        Assert.Equal(200, response.Status);
        Assert.Equal("image/png", response.Headers["content-type"]);
    }

    // The top of the block of the editions in the window.
    private const string EditionsTop = "document.querySelector('[data-part=\"editions\"]').getBoundingClientRect().top";

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
    public async Task AuthorsOfARole_StayFolded_AcrossTheViewer()
    {
        var page = await NewPageAsync(new ViewportSize { Width = 414, Height = 896 });
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));
        var drawing = page.Locator(".contributions app-fold-row").Nth(1);
        var last = drawing.GetByRole(AriaRole.Link, new() { Name = "Inès Vasseur" });
        await Assertions.Expect(last).ToBeHiddenAsync();

        // The enhanced navigation of Blazor gives each element the attributes of the page it receives, which marks no name
        // as folded: the row is folded again.
        await page.GetByRole(AriaRole.Link, new() { Name = "Planche 1" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);

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

    [Fact]
    public async Task RestOfTheSeries_Folds_AndLeadsToAnotherAlbumOfTheSeries_PutForwardOnItsRecord()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));
        var series = page.Locator("[data-part='series']");
        var other = series.GetByRole(AriaRole.Link, new() { Name = "Les Celtiques" });
        await Assertions.Expect(other).ToBeVisibleAsync();

        await series.Locator("summary").ClickAsync();
        await Assertions.Expect(other).ToBeHiddenAsync();
        await series.Locator("summary").ClickAsync();
        await other.ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/albums/{Application.IntendedAlbumId}$"));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Les Celtiques");
        await Assertions.Expect(series.Locator("[aria-current='page']")).ToHaveTextAsync("Les Celtiques");
        await Assertions.Expect(series.GetByRole(AriaRole.Link, new() { Name = "Lointaines îles du vent" })).ToBeVisibleAsync();
    }

    [GeneratedRegex(@"^et \d+ autres?$")]
    private static partial Regex FoldedCount();

    [GeneratedRegex(@"[?&]visual=2$")]
    private static partial Regex SecondVisualAddress();

    [GeneratedRegex(@"/albums/[0-9a-f-]+$")]
    private static partial Regex RecordAddress();

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
/// An application whose albums present the cases of the records of an album, of a series and of an author, at fixed dates:
/// an album of a series with two editions in the collection, one with four visuals and one gift without any, wished for
/// again in a third, and a collective drawing, attached to a universe within two others; an album targeted by an intent on
/// itself, and one catalogued only; their series, and a series without any album; the whole bibliography of the author of
/// the series, and an author credited on nothing. Its visuals are the only ones, so that the edition in the spotlight, and
/// the colours of the day, are the same at each capture.
/// </summary>
public sealed class RecordsApplication : ApplicationHost
{
    public const string Name = "Records";

    private static readonly DateTimeOffset Start = new(2026, 2, 2, 9, 0, 0, TimeSpan.Zero);

    protected override bool PaintedByItsSpotlight => true;

    /// <summary>The album of the series with editions, visuals and a collective drawing.</summary>
    public Guid AlbumId { get; private set; }

    /// <summary>The first edition of <see cref="AlbumId"/>, with its four visuals.</summary>
    public Guid FirstEditionId { get; private set; }

    /// <summary>An album of the series targeted by a purchase intent on itself.</summary>
    public Guid IntendedAlbumId { get; private set; }

    /// <summary>An album of the series catalogued only, out of the collection.</summary>
    public Guid CataloguedAlbumId { get; private set; }

    /// <summary>The series of the albums above, with its publisher, collection, authors and summary.</summary>
    public Guid SeriesId { get; private set; }

    /// <summary>A series just created: no album, author, genre nor status.</summary>
    public Guid EmptySeriesId { get; private set; }

    /// <summary>Hugo Pratt, the author of the albums of the series, with a whole bibliography.</summary>
    public Guid AuthorId { get; private set; }

    /// <summary>An author known under a pseudonym, credited on nothing yet.</summary>
    public Guid UncreditedAuthorId { get; private set; }

    protected override async Task SeedAsync()
    {
        using var api = CreateApiClient();
        Clock.UtcNow = Start;
        var casterman = await PostAsync<PublisherForm>(api, "/admin/publishers", new PublisherContent("Casterman", "https://www.casterman.com/", []));
        var collection = await PostAsync<PublisherCollectionForm>(api,
            $"/admin/publishers/{casterman.Id}/collections", new CreatePublisherCollectionRequest("Univers d'auteurs", casterman.Version));
        var aventure = await PostAsync<GenreForm>(api, "/admin/genres", new CreateGenreRequest("Aventure"));
        var pratt = await PostAsync<UniverseForm>(api, "/admin/universes", new CreateUniverseRequest("Univers Hugo Pratt", null, null));
        var cortoUniverse = await PostAsync<UniverseForm>(api, "/admin/universes", new CreateUniverseRequest("Corto Maltese", null, pratt.Id));
        var antilles = await PostAsync<UniverseForm>(api, "/admin/universes", new CreateUniverseRequest("Les Antilles", null, cortoUniverse.Id));
        var hugo = await AuthorAsync(api, "Pratt", "Hugo");
        string[] drawers = ["Inès Vasseur", "Tom Arribas", "Clara Benmansour", "Yann Le Goff", "Nina Okafor"];
        List<ContributionContent> contributions = [new(hugo.Id, ContributionRole.Scenarist), new(hugo.Id, ContributionRole.Illustrator)];
        foreach (var drawer in drawers)
            contributions.Add(new((await AuthorAsync(api, drawer.Split(' ', 2)[1], drawer.Split(' ', 2)[0])).Id, ContributionRole.Illustrator));
        // Its publisher, collection, authors and summary, which only its own record presents; no universe, which the records
        // of its albums would present as theirs too (fonctionnel/regles-metier/genres-et-univers-d-un-album.md).
        var series = await PostAsync<SeriesForm>(api, "/admin/series", new SeriesContent(
            "Corto Maltese", null, SeriesStatus.Completed, 12, false, false, false,
            "Les errances d'un marin de fortune, des Caraïbes à la Sibérie, au début du XXe siècle.", null,
            new SeriesEditionTemplate(casterman.Id, collection.Id, null, null, null, null, null, null, null), [aventure.Id], [],
            [new(hugo.Id, ContributionRole.Scenarist), new(hugo.Id, ContributionRole.Illustrator)]));
        SeriesId = series.Id;

        var album = await AlbumAsync(api, series.Id, "Lointaines îles du vent", 5, contributions, [antilles.Id],
            "Venezuela, vers 1913. Amnésique, Corto Maltese est recueilli par son vieil ami Steiner.");
        AlbumId = album.Id;
        var first = await PostAsync<EditionForm>(api, $"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            Edition(casterman.Id, null, 2001, "2-203-34424-5", AcquisitionMode.Purchase, new DateOnly(2006, 9, 9), 16.95m), album.Version));
        FirstEditionId = first.Id;
        var version = first.AlbumVersion;
        foreach (var (type, color) in new[]
        {
            (VisualType.Cover, SKColors.SteelBlue), (VisualType.Plate, SKColors.Wheat), (VisualType.Plate, SKColors.Bisque), (VisualType.BackCover, SKColors.Tan),
        })
            version = await UploadAsync(api, album.Id, first.Id, type, color, version);
        var gift = await PostAsync<EditionForm>(api, $"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            Edition(casterman.Id, collection.Id, 2010, null, AcquisitionMode.Gift, new DateOnly(2011, 12, 25), null), version));

        var intended = await AlbumAsync(api, series.Id, "Les Celtiques", 6, [new(hugo.Id, ContributionRole.Scenarist)]);
        IntendedAlbumId = intended.Id;
        await PostAsync<PurchaseIntentsForm>(api, $"/admin/albums/{intended.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, intended.Version));
        CataloguedAlbumId = (await AlbumAsync(api, series.Id, "Les Éthiopiques", 7, [new(hugo.Id, ContributionRole.Scenarist)])).Id;

        // Owned, and wished for again in a special edition, presented under its editions.
        await PostAsync<PurchaseIntentsForm>(api, $"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(casterman.Id, null, 2023, null, BindingType.Hardcover, null, null, EditionFormat.Large, 112,
                EditionCategory.SpecialEdition, true),
            gift.AlbumVersion));

        // Last, so that no date of the albums above changes.
        EmptySeriesId = (await PostAsync<SeriesForm>(api, "/admin/series", new SeriesContent(
            "Les Récits du phare", null, null, 6, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], []))).Id;

        // The bibliography of Hugo Pratt (visuel/fiche-auteur/), written after the album, whose dates it leaves as they were:
        // an album without series, rated; « Corto Maltese », of three albums; a series credited on itself only, without album;
        // a series of seven albums credited, folded on arrival, one of them coloured by him. None has an edition, so that the
        // edition in the spotlight stays the only one with a cover.
        AuthorId = hugo.Id;
        await PostAsync<AlbumForm>(api, "/admin/albums", new AlbumContent(
            "Carnet de croquis", null, null, AlbumType.Regular, false, null, null, null, 1990, null, null, null, AlbumRating.Good, [], [],
            [new(hugo.Id, ContributionRole.Scenarist), new(hugo.Id, ContributionRole.Illustrator)]));
        await PostAsync<SeriesForm>(api, "/admin/series", new SeriesContent(
            "Les Scorpions du désert", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], [new(hugo.Id, ContributionRole.Scenarist)]));
        var kirk = await PostAsync<SeriesForm>(api, "/admin/series", new SeriesContent(
            "Sergent Kirk", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], []));
        for (var volume = 1; volume <= 7; volume++)
        {
            List<ContributionContent> credits = [new(hugo.Id, ContributionRole.Illustrator)];
            if (volume == 1)
                credits.Add(new(hugo.Id, ContributionRole.Colorist));
            await PostAsync<AlbumForm>(api, "/admin/albums", new AlbumContent(
                volume == 2 ? "Le Fort de l'Ouest" : null, null, kirk.Id, AlbumType.Regular, false, volume, null, null, 1953 + volume, null, null,
                null, volume == 2 ? AlbumRating.VeryGood : null, [], [], credits));
        }

        // Known under a pseudonym, with a nationality and a biography, credited on nothing yet.
        UncreditedAuthorId = (await PostAsync<AuthorForm>(api, "/admin/authors", new CreateAuthorRequest(
            "Giraud", "Jean", "Moebius", "Né à Nogent-sur-Marne en 1938, il signe Gir ses westerns et Moebius le reste de son œuvre.",
            "Française"))).Id;
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

    private async Task<uint> UploadAsync(HttpClient api, Guid album, Guid edition, VisualType type, SKColor color, uint albumVersion)
    {
        Tick();
        using var bitmap = new SKBitmap(300, 400);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        var file = new ByteArrayContent(png.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var form = new MultipartFormDataContent
        {
            { file, UploadVisualFields.File, "visual.png" },
            { new StringContent(type.ToString()), UploadVisualFields.Type },
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

    private void Tick() => Clock.UtcNow += TimeSpan.FromMinutes(15);
}

[CollectionDefinition(RecordsApplication.Name)]
public sealed class RecordsCollection : ICollectionFixture<RecordsApplication>;
