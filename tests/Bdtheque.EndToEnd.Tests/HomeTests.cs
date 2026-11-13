extern alias frontend;

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using SkiaSharp;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The home page (fonctionnel/page-accueil.md), on applications of their own: its rendering depends on the whole
/// collection, which the other tests change with their own data. Its captures are in
/// <see cref="Captures.EmptyCollectionHomeCaptureTests"/> and <see cref="Captures.DashboardHomeCaptureTests"/>.
/// </summary>
[Collection(EmptyCollectionApplication.Name)]
public sealed class EmptyCollectionHomeTests(EmptyCollectionApplication application) : BrowserTest<EmptyCollectionApplication>(application)
{
    [Theory, InEachTheme]
    public async Task Visitor_IsWelcomed_AndBrowsesTheDocumentaryBase(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Home);

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Les étagères attendent leur premier album !");
        await Assertions.Expect(page.Locator("header form[role='search']")).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Link, new() { Name = "Albums →" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.OfPath(AppPaths.NotYetImplemented));
    }

    // The link to the administration of the foot stays with an empty collection (visuel/dashboard/collection-vide.md).
    [Theory, InEachTheme]
    public async Task Foot_LeadsToTheAdministration(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Home);

        await Assertions.Expect(page.Locator("footer").GetByRole(AriaRole.Link, new() { Name = "Administration" }))
            .ToHaveAttributeAsync("href", AppPaths.AdminHome);
    }

    [Theory, InEachTheme]
    public async Task Visitor_IsLedToTheAdministration(string theme)
    {
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = 1440, Height = 900 });
        await page.GotoAsync(AppPaths.Home);

        await page.GetByRole(AriaRole.Link, new() { Name = "Ouvrir l'administration →" }).ClickAsync();

        // The administration asks for the login first: the address is polled rather than a load event awaited, which an
        // enhanced navigation of Blazor, patching the page instead of loading it, need not fire; on failure, it tells the
        // address reached.
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.OfPath(AppPaths.AdminLogin));
    }
}

[Collection(CollectionApplication.Name)]
public sealed class DashboardHomeTests(CollectionApplication application) : BrowserTest<CollectionApplication>(application)
{
    [Theory, InEachTheme]
    public async Task Visitor_SeesTheDashboard_AndGoesToTheAlbumInTheSpotlight(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Home);

        // The first greeting, the draws of the tests always picking the first choice.
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Entrez donc, il y a de quoi lire !");
        await Assertions.Expect(page.Locator("header form[role='search']")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("[data-counter]")).ToHaveTextAsync(["2 albums", "1 série", "2 éditions"]);

        var spotlight = page.Locator("section.block", new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = "À la une aujourd'hui !" }) });
        await spotlight.GetByRole(AriaRole.Link).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($@"{Regex.Escape(AppPaths.AlbumsSection)}/[0-9a-f-]+$"));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Le Port des brumes");
    }

    // The day of the spotlight and the dates of the entries are those of the browser (fonctionnel/page-accueil.md): the
    // welcome screen gives its time zone before the dashboard of a first visit.
    [Theory, InEachTheme]
    public async Task FirstVisit_GetsTheDashboard_InTheTimeZoneOfTheBrowser(string theme)
    {
        var context = await NewContextAsync(theme, new BrowserNewContextOptions
        {
            TimezoneId = "Pacific/Kiritimati",
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(AppPaths.Home);

        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-time-zone", "Pacific/Kiritimati");
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Entrez donc, il y a de quoi lire !");
    }

    // At the time of the application of the tests, the date of the browser is already the next day while it is still the day
    // before in universal time (ApplicationHost.Origin). The edition in the spotlight is drawn, and recorded, for the date of
    // the browser only (#340): had a day been drawn in universal time, the only edition with a cover would have been recent
    // the next day, and the dashboard of the date of the browser would have presented the other one.
    [Theory, InEachTheme]
    public async Task Spotlight_IsDrawnForTheDateOfTheBrowserOnly_WhenItIsNotTheDateInUniversalTime(string theme)
    {
        var universalDay = DateOnly.FromDateTime(Application.Clock.GetUtcNow().UtcDateTime);
        Assert.NotEqual(universalDay, Application.TodayIn(ApplicationHost.BrowserTimeZoneId));
        var page = await NewPageAsync(theme);

        await page.GotoAsync(AppPaths.Home);

        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-time-zone", ApplicationHost.BrowserTimeZoneId);
        var spotlight = page.Locator("section.block", new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = "À la une aujourd'hui !" }) });
        await Assertions.Expect(spotlight.GetByRole(AriaRole.Link)).ToContainTextAsync("Le Port des brumes");
        Assert.DoesNotContain(universalDay, await Application.SpotlightDaysAsync());
    }

    [Theory, InEachTheme]
    public async Task Foot_LeadsToTheAdministration(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Home);

        await page.Locator("footer").GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($@"^[a-z]+://[^/]+{Regex.Escape(AppPaths.AdminLogin)}\?"));
    }

    // The link belongs to the home page: the other pages of the consultation keep their foot as it is.
    [Theory, InEachTheme]
    public async Task OtherPagesOfTheConsultation_HaveNoLinkToTheAdministrationInTheirFoot(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.AudienceInformation);

        await Assertions.Expect(page.Locator("footer").GetByRole(AriaRole.Link, new() { Name = "Administration" })).ToHaveCountAsync(0);
    }

    [Theory, InEachTheme]
    public async Task LatestEntries_FromTheMostRecentAcquisition(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Home);

        var latest = page.Locator("section.block", new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = "Tout juste arrivés !" }) });
        await Assertions.Expect(latest.Locator("li")).ToHaveCountAsync(2);
        await Assertions.Expect(latest.Locator("li").First).ToContainTextAsync("Le Port des brumes");
        await Assertions.Expect(latest.Locator("li").First).ToContainTextAsync("12/10/2026");
    }

    [Theory, InEachTheme]
    public async Task LatestEntries_ThePublisherLeadsToItsWebsite_BesideTheApplication(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Home);

        // Its site out of the application, opened in a new tab, the dashboard staying as it was
        // (choix-implementation/socle/frontend.md, site extérieur).
        var latest = page.Locator("section.block", new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = "Tout juste arrivés !" }) });
        var publisher = latest.Locator("li").First.GetByRole(AriaRole.Link, new() { NameRegex = new Regex(@"^Casterman\s*, site externe$") });
        await Assertions.Expect(publisher).ToHaveAttributeAsync("href", "https://www.casterman.com");
        await Assertions.Expect(publisher).ToHaveAttributeAsync("target", "_blank");
        await Assertions.Expect(publisher).ToHaveAttributeAsync("rel", "noopener noreferrer");
    }
}

/// <summary>An application without any data: the welcome screen of an empty collection.</summary>
public sealed class EmptyCollectionApplication : ApplicationHost
{
    public const string Name = "Empty collection";

    protected override bool PaintedByItsSpotlight => true;
}

[CollectionDefinition(EmptyCollectionApplication.Name)]
public sealed class EmptyCollectionCollection : ICollectionFixture<EmptyCollectionApplication>;

/// <summary>
/// An application whose collection holds two owned editions, one of an album of a series with its summary, and an album
/// catalogued without any owned edition, which the dashboard does not count.
/// </summary>
public sealed class CollectionApplication : ApplicationHost
{
    public const string Name = "Collection";

    protected override bool PaintedByItsSpotlight => true;

    protected override async Task SeedAsync()
    {
        using var api = CreateApiClient();
        var publisher = await PostAsync<PublisherContent, PublisherForm>(api, "/admin/publishers", new("Casterman", "https://www.casterman.com", []));
        var series = await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", new SeriesContent(
            "Nestor Burma", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], []));

        // The only edition with a cover: the draw of the spotlight keeps only those (fonctionnel/page-accueil.md), so the
        // spotlight is the same at each capture.
        var spotlight = await OwnedAsync(api, publisher.Id, new("Le Port des brumes", null, series.Id, AlbumType.Regular, false, 3, null,
            null, 1990, null, "Un hiver brumeux sur le port.", null, null, [], [], []), new DateOnly(2026, 10, 12));
        await AddCoverAsync(api, spotlight);
        await OwnedAsync(api, publisher.Id, new("Brouillard au pont de Tolbiac", null, series.Id, AlbumType.Regular, false, 1, null, null,
            1982, null, null, null, null, [], [], []), new DateOnly(2026, 9, 3));
        await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", new("Album catalogué", null, null, AlbumType.Regular, false, null,
            null, null, 2001, null, null, null, null, [], [], []));
    }

    private static async Task<EditionForm> OwnedAsync(HttpClient api, Guid publisher, AlbumContent album, DateOnly acquired)
    {
        var created = await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", album);
        return await PostAsync<CreateEditionRequest, EditionForm>(api, $"/admin/albums/{created.Id}/editions", new(
            new(publisher, null, album.FirstPublicationYear, null, null, null, null, null, null, null, false, true, null,
                AcquisitionMode.Purchase, false, acquired, null, null, false, null, null, null, null),
            created.Version));
    }

    private static async Task AddCoverAsync(HttpClient api, EditionForm edition)
    {
        using var bitmap = new SKBitmap(300, 400);
        bitmap.Erase(SKColors.SteelBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);

        var file = new ByteArrayContent(png.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var form = new MultipartFormDataContent
        {
            { file, UploadVisualFields.File, "cover.png" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent(edition.AlbumVersion.ToString(CultureInfo.InvariantCulture)), UploadVisualFields.AlbumVersion },
        };
        using var response = await api.PostAsync($"/admin/albums/{edition.AlbumId}/editions/{edition.Id}/visuals", form);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<TResponse> PostAsync<TRequest, TResponse>(HttpClient api, string path, TRequest request)
    {
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }
}

[CollectionDefinition(CollectionApplication.Name)]
public sealed class CollectionCollection : ICollectionFixture<CollectionApplication>;
