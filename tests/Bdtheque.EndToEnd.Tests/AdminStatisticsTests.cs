extern alias frontend;

using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Audience;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The indicators of the audience measurement in the administration (fonctionnel/mesure-audience.md, Indicateurs), on an
/// application of its own (<see cref="AudienceApplication"/>): they present every usage of the base. The browser of each test
/// opposes the measurement, so that its own pages change nothing of what the others present. Their captures are in
/// <see cref="Captures.AdminStatisticsCaptureTests"/>.
/// </summary>
[Collection(AudienceApplication.Name)]
public sealed class AdminStatisticsTests(AudienceApplication application) : BrowserTest<AudienceApplication>(application)
{
    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheStatistics_EveryFunctionFollowed_TheMostUsedFirst(string theme)
    {
        var page = await AudienceApplication.OpenAsync(await NewPageAsync(theme), Application, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Statistiques privées" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminStatistics));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Statistiques privées");
        var rows = page.GetByRole(AriaRole.Table, new() { Name = "Mesure d'audience" }).Locator(".audience-row:not(.audience-row-head)");
        await Assertions.Expect(rows).ToHaveCountAsync(AudienceIndicatorsQuery.PageSize);
        await Assertions.Expect(rows.First.Locator(".row-entry")).ToHaveTextAsync("Fiche d'un album");
        await Assertions.Expect(rows.First.Locator(".audience-usages")).ToContainTextAsync("5");
        await Assertions.Expect(rows.Filter(new() { Has = page.Locator(".row-entry", new() { HasTextRegex = new Regex("^Administration · déconnexion$") }) })).ToHaveAttributeAsync("data-unused", "");
    }

    [Theory, InEachTheme]
    public async Task Criteria_ReadTheIndicatorsOfACompartment_FromTheAddress(string theme)
    {
        var page = await AudienceApplication.OpenAsync(await NewPageAsync(theme), Application, AppPaths.AdminStatistics);

        await page.GetByLabel("Utilisateurs").SelectOptionAsync("Visitors");
        await page.GetByLabel("Appareil").SelectOptionAsync("Smartphone");
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminStatistics, "?period=TwelveMonths&users=Visitors&device=Smartphone&engine="));
        // The record of an album, on a smartphone: three times in March, once in January; the administrator never used a
        // smartphone.
        var album = page.Locator(".audience-row", new() { Has = page.Locator(".row-entry", new() { HasTextRegex = new Regex("^Fiche d'un album$") }) }).First;
        await Assertions.Expect(album.Locator(".audience-usages")).ToContainTextAsync("4");
        await Assertions.Expect(page.Locator(".audience-row", new() { Has = page.Locator(".row-entry", new() { HasTextRegex = new Regex("^Liste des albums$") }) }).First)
            .ToHaveAttributeAsync("data-unused", "");
    }
}

/// <summary>
/// An application whose base holds visits of every kind over a year, written through the endpoints the frontend records them
/// by, at fixed dates: visitors on a smartphone, a tablet and a computer, under three engines, and the administrator; records
/// presented again and a search repeated at once. The current month of the application is that of
/// <see cref="ApplicationHost.Origin"/>, January 2026.
/// </summary>
public sealed class AudienceApplication : ApplicationHost
{
    public const string Name = "Audience";

    private static readonly Guid AlbumA = Guid.Parse("01940000-0000-7000-8000-000000000001");
    private static readonly Guid AlbumB = Guid.Parse("01940000-0000-7000-8000-000000000002");
    private static readonly Guid SeriesS = Guid.Parse("01940000-0000-7000-8000-000000000003");
    private static readonly Guid AuthorX = Guid.Parse("01940000-0000-7000-8000-000000000004");

    protected override async Task SeedAsync()
    {
        using var api = CreateApiClient();
        await VisitAsync(api, new(2025, 3, 10, 9, 0, 0, TimeSpan.Zero), false, DeviceType.Smartphone, RenderingEngine.WebKit, "18.2",
            Screen(AudienceFunctions.Home), Screen(AudienceFunctions.AlbumRecord, AlbumA), Screen(AudienceFunctions.AlbumRecord, AlbumA),
            Screen(AudienceFunctions.AlbumRecord, AlbumB), Screen(AudienceFunctions.SeriesRecord, SeriesS));
        await VisitAsync(api, new(2025, 6, 12, 18, 0, 0, TimeSpan.Zero), false, DeviceType.Computer, RenderingEngine.Gecko, "133",
            Screen(AudienceFunctions.Home), Screen(AudienceFunctions.AlbumRecord, AlbumA));
        await VisitAsync(api, new(2025, 9, 20, 7, 0, 0, TimeSpan.Zero), false, DeviceType.Tablet, RenderingEngine.WebKit, "17.6",
            Screen(AudienceFunctions.Home), Screen(AudienceFunctions.AuthorRecord, AuthorX));
        await VisitAsync(api, new(2025, 12, 5, 20, 0, 0, TimeSpan.Zero), true, DeviceType.Computer, RenderingEngine.Blink, "131",
            Screen(AudienceFunctions.AdminHome), Screen(AudienceFunctions.AdminAlbums),
            Action(AudienceFunctions.AdminAlbumsSearch, "q=ast"), Action(AudienceFunctions.AdminAlbumsSearch, "q=ast"),
            Screen(AudienceFunctions.AdminAlbums), Screen(AudienceFunctions.AdminAlbum, AlbumA));
        await VisitAsync(api, new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero), false, DeviceType.Smartphone, RenderingEngine.WebKit, "18.2",
            Screen(AudienceFunctions.Home), Screen(AudienceFunctions.AlbumRecord, AlbumA),
            Action(AudienceFunctions.AlbumRecordVisual, "edition=01940000-0000-7000-8000-000000000005&visual=1"));
        await VisitAsync(api, new(2026, 1, 12, 8, 0, 0, TimeSpan.Zero), true, DeviceType.Computer, RenderingEngine.Blink, "131",
            Screen(AudienceFunctions.AdminHome), Screen(AudienceFunctions.AdminSearch),
            Action(AudienceFunctions.AdminSearchQuery, "q=ast"));
        // Back to the time of the application, which every test reads from.
        Clock.UtcNow = null;
    }

    /// <summary>
    /// The page at <paramref name="path"/>, the administrator signed in, in a browser that opposes the measurement: nothing it
    /// displays is recorded, and the indicators stay those of the base.
    /// </summary>
    public static async Task<IPage> OpenAsync(IPage page, ApplicationHost application, string path)
    {
        await page.Context.AddCookiesAsync([new Cookie { Name = AudienceOpposition.CookieName, Value = "1", Url = application.BaseAddress.ToString() }]);
        await SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    // A visit and its usages, a minute apart from its beginning.
    private async Task VisitAsync(
        HttpClient api, DateTimeOffset begin, bool administrator, DeviceType device, RenderingEngine engine, string version,
        params FunctionUse[] uses)
    {
        Clock.UtcNow = begin;
        var opened = await api.PostAsJsonAsync("/audience/visits", new OpenVisitRequest(administrator, device, engine, version, uses[0]));
        opened.EnsureSuccessStatusCode();
        var visit = (await opened.Content.ReadFromJsonAsync<OpenedVisit>())!.VisitId;
        foreach (var use in uses.Skip(1))
        {
            Clock.UtcNow += TimeSpan.FromMinutes(1);
            (await api.PostAsJsonAsync($"/audience/visits/{visit}/usages", use)).EnsureSuccessStatusCode();
        }
    }

    private static FunctionUse Screen(AudienceFunction screen, Guid? record = null) =>
        new(screen.Key, record is { } id ? new PresentedRecord(RecordTypeOf(screen), id) : null, null);

    private static FunctionUse Action(AudienceFunction action, string parameters) => new(action.Key, null, parameters);

    private static RecordType RecordTypeOf(AudienceFunction screen) => screen.Key switch
    {
        "series" => RecordType.Series,
        "author" => RecordType.Author,
        _ => RecordType.Album,
    };
}

[CollectionDefinition(AudienceApplication.Name)]
public sealed class AudienceCollection : ICollectionFixture<AudienceApplication>;
