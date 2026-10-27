using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Contracts.Search;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The data of the dashboard before Phase 4 (fonctionnel/page-accueil.md § Page d'accueil): the empty collection,
/// the counters, the edition in the spotlight of the day and the latest entries, the other blocks signalled as not
/// computable yet.
/// </summary>
/// <remarks>
/// Every statistic bears on the whole collection, and the spotlight of a day on every owned edition: each test has
/// an API and a database of its own, rather than one shared by the class.
/// </remarks>
public sealed class DashboardEndpointsTests : IAsyncLifetime
{
    private static readonly DateOnly Day = new(2026, 10, 27);
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly ApiWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateApiClient();
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [Fact]
    public async Task EmptyCollection_IsSignalled_WithoutAnyDraw()
    {
        // The documentary base is no collection: an album without edition, an edition targeted by an intent.
        var publisher = await CreatePublisherAsync("Dargaud");
        await CreateAlbumAsync("Blueberry", null);
        var intended = await CreateAlbumAsync("Mister Blueberry", null);
        await CreateIntendedEditionAsync(intended.Id, publisher.Id);

        var dashboard = await GetDashboardAsync(Day);

        Assert.Null(dashboard.Content);
        Assert.Equal(0, await SpotlightCountAsync());
    }

    [Fact]
    public async Task Counters_CountTheAlbumsTheirSeriesAndTheOwnedEditions_OfTheCollectionOnly()
    {
        var publisher = await CreatePublisherAsync("Dupuis");
        var gaston = await CreateSeriesAsync("Gaston");
        var first = await CreateAlbumAsync("Gala de gaffes", gaston.Id, volume: 1);
        await CreateOwnedEditionAsync(first.Id, publisher.Id);
        await CreateOwnedEditionAsync(first.Id, publisher.Id);
        var second = await CreateAlbumAsync("Gaffes en gros", gaston.Id, volume: 2);
        await CreateOwnedEditionAsync(second.Id, publisher.Id);
        var oneShot = await CreateAlbumAsync("Le Petit Spirou présente", null);
        await CreateOwnedEditionAsync(oneShot.Id, publisher.Id);
        // Neither album, series nor edition of the collection.
        var spirou = await CreateSeriesAsync("Spirou");
        var notOwned = await CreateAlbumAsync("Les Voleurs du Marsupilami", spirou.Id, volume: 5);
        await CreateIntendedEditionAsync(notOwned.Id, publisher.Id);
        await CreateAlbumAsync("Le Prisonnier du Bouddha", spirou.Id, volume: 14);

        var content = (await GetDashboardAsync(Day)).Content!;

        Assert.Equal(new DashboardCounters(3, 1, 4), content.Counters);
        Assert.False(content.AreValueIndicatorsComputable);
        Assert.False(content.AreRandomStatisticsComputable);
    }

    [Fact]
    public async Task Spotlight_IsAnOwnedEdition_WithItsFirstCoverTheLabelsAndTheSummaryOfItsAlbum()
    {
        var publisher = await CreatePublisherAsync("Casterman");
        var tintin = await CreateSeriesAsync("Tintin");
        var album = await CreateAlbumAsync("Le Lotus bleu", tintin.Id, volume: 5, summary: "Tintin en Chine.");
        var owned = await CreateOwnedEditionAsync(album.Id, publisher.Id, publicationYear: 1946);
        await UploadAsync(album.Id, owned, VisualType.Plate);
        var cover = await UploadAsync(album.Id, owned, VisualType.Cover);
        await UploadAsync(album.Id, owned, VisualType.Cover);
        // Never in the spotlight, cover or not: not owned.
        var intended = await CreateAlbumAsync("Tintin au Tibet", tintin.Id, volume: 20);
        await UploadAsync(intended.Id, await CreateIntendedEditionAsync(intended.Id, publisher.Id), VisualType.Cover);

        var spotlight = (await GetDashboardAsync(Day)).Content!.Spotlight;

        Assert.Equal(
            (album.Id, "Le Lotus bleu", "Tintin", 5, owned, publisher.Name, 1946, cover, "Tintin en Chine."),
            (spotlight.Album.Id, spotlight.Album.Title, spotlight.Album.SeriesTitle, spotlight.Album.VolumeNumber,
                spotlight.Edition.Id, spotlight.Edition.PublisherName, spotlight.Edition.PublicationYear, spotlight.Cover, spotlight.Summary));
    }

    [Fact]
    public async Task Spotlight_StaysTheSameAllDay_WhateverEditionEntersMeanwhile()
    {
        var publisher = await CreatePublisherAsync("Lombard");
        var album = await CreateAlbumAsync("Thorgal", null);
        await CreateOwnedEditionAsync(album.Id, publisher.Id);
        await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var first = (await GetDashboardAsync(Day)).Content!.Spotlight.Edition.Id;

        await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var displays = new List<Guid>();
        for (var i = 0; i < 5; i++)
            displays.Add((await GetDashboardAsync(Day)).Content!.Spotlight.Edition.Id);

        Assert.All(displays, edition => Assert.Equal(first, edition));
        Assert.Equal(1, await SpotlightCountAsync());
    }

    [Fact]
    public async Task Spotlight_ComesBackTenDaysLaterAtTheEarliest()
    {
        var publisher = await CreatePublisherAsync("Glénat");
        var album = await CreateAlbumAsync("Les Passagers du vent", null);
        await CreateOwnedEditionAsync(album.Id, publisher.Id);
        await CreateOwnedEditionAsync(album.Id, publisher.Id);

        var first = (await GetDashboardAsync(Day)).Content!.Spotlight.Edition.Id;
        var next = (await GetDashboardAsync(Day.AddDays(1))).Content!.Spotlight.Edition.Id;
        // The second, in the spotlight the day after, is still recent; the first no longer.
        var tenDaysLater = (await GetDashboardAsync(Day.AddDays(10))).Content!.Spotlight.Edition.Id;

        Assert.NotEqual(first, next);
        Assert.Equal(first, tenDaysLater);
    }

    [Fact]
    public async Task Spotlight_PrefersAnEditionWithACover()
    {
        var publisher = await CreatePublisherAsync("Soleil");
        var album = await CreateAlbumAsync("Lanfeust de Troy", null);
        await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var withCover = await CreateOwnedEditionAsync(album.Id, publisher.Id);
        await UploadAsync(album.Id, withCover, VisualType.Cover);

        // Days far enough apart for the repetitions not to leave it out.
        var spotlights = new List<Guid>();
        foreach (var day in new[] { Day, Day.AddDays(20), Day.AddDays(40) })
            spotlights.Add((await GetDashboardAsync(day)).Content!.Spotlight.Edition.Id);

        Assert.All(spotlights, edition => Assert.Equal(withCover, edition));
    }

    [Fact]
    public async Task Spotlight_OfAnEditionDeletedDuringTheDay_IsDrawnAgain()
    {
        var publisher = await CreatePublisherAsync("Delcourt");
        var album = await CreateAlbumAsync("Donjon", null);
        var first = await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var second = await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var deleted = (await GetDashboardAsync(Day)).Content!.Spotlight.Edition.Id;

        await using (var scope = _factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().Editions.Where(e => e.Id == deleted).ExecuteDeleteAsync();
        var spotlight = (await GetDashboardAsync(Day)).Content!.Spotlight.Edition.Id;

        Assert.Equal(deleted == first ? second : first, spotlight);
    }

    [Fact]
    public async Task LatestEntries_FromTheMostRecent_ByAcquisitionDateOrElseEntryDate_AsManyAsAsked()
    {
        var publisher = await CreatePublisherAsync("Futuropolis");
        var album = await CreateAlbumAsync("Le Combat ordinaire", null);
        var acquiredInJanuary = await CreateOwnedEditionAsync(album.Id, publisher.Id, enteredAt: new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero), acquisitionDate: new(2026, 1, 10));
        var enteredInMarch = await CreateOwnedEditionAsync(album.Id, publisher.Id, enteredAt: new(2026, 3, 5, 9, 0, 0, TimeSpan.Zero));
        var cover = await UploadAsync(album.Id, enteredInMarch, VisualType.Cover);
        await CreateOwnedEditionAsync(album.Id, publisher.Id, enteredAt: new(2026, 10, 2, 9, 0, 0, TimeSpan.Zero), acquisitionDate: new(2025, 12, 1));
        var enteredInJune = await CreateOwnedEditionAsync(album.Id, publisher.Id, enteredAt: new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero));
        // Not owned: never an entry.
        await CreateIntendedEditionAsync((await CreateAlbumAsync("Les Quantités négligeables", null)).Id, publisher.Id);

        var entries = (await GetDashboardAsync(Day, latestCount: 3)).Content!.LatestEntries;

        Assert.Equal([enteredInJune, enteredInMarch, acquiredInJanuary], entries.Select(e => e.Edition.Id));
        Assert.Equal(
            (album.Id, publisher.Name, cover, (DateOnly?)null, new DateTimeOffset(2026, 3, 5, 9, 0, 0, TimeSpan.Zero)),
            (entries[1].Album.Id, entries[1].Edition.PublisherName, entries[1].Cover, entries[1].AcquisitionDate, entries[1].EnteredCollectionAt));
        Assert.Equal((DateOnly?)new DateOnly(2026, 1, 10), entries[2].AcquisitionDate);
        Assert.Null(entries[0].Cover);
    }

    [Fact]
    public async Task Spotlights_AreCountedByTheAdvancedSearch()
    {
        var publisher = await CreatePublisherAsync("Vents d'Ouest");
        var album = await CreateAlbumAsync("Titeuf", null);
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id);
        await GetDashboardAsync(Day);
        await GetDashboardAsync(Day.AddDays(30));

        var response = await _client.PostAsJsonAsync(
            "/catalog/search/editions",
            new AdvancedSearchRequest(new SearchCriterion("spotlightCount", SearchComparison.EqualTo, ["2"])));
        response.EnsureSuccessStatusCode();
        var found = await response.Content.ReadFromJsonAsync<Page<EditionListItem>>();

        Assert.Equal([edition], found!.Items.Select(i => i.Edition.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task LatestCount_OutsideOneToTwenty_IsATechnicalError(int latestCount)
    {
        var publisher = await CreatePublisherAsync("Rue de Sèvres");
        await CreateOwnedEditionAsync((await CreateAlbumAsync("Le Grand Méchant Renard", null)).Id, publisher.Id);

        var response = await _client.GetAsync($"/catalog/dashboard?day=2026-10-27&latestCount={latestCount}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private async Task<Dashboard> GetDashboardAsync(DateOnly day, int? latestCount = null) =>
        (await _client.GetFromJsonAsync<Dashboard>(
            $"/catalog/dashboard?day={day:yyyy-MM-dd}{(latestCount is null ? "" : $"&latestCount={latestCount}")}"))!;

    private async Task<int> SpotlightCountAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().EditionSpotlights.CountAsync();
    }

    private static string VisualsUri(Guid albumId, Guid editionId) => $"/admin/albums/{albumId}/editions/{editionId}/visuals";

    private Task<PublisherForm> CreatePublisherAsync(string name) =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest(name, null));

    private Task<SeriesForm> CreateSeriesAsync(string title) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(title, null, null, null, false, false, false, null, null, NoTemplate, [], [], []));

    private Task<AlbumForm> CreateAlbumAsync(string title, Guid? seriesId, int? volume = null, string? summary = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volume, null, null, null, null, summary, null, null, [], [], []));

    private async Task<Guid> CreateOwnedEditionAsync(
        Guid albumId, Guid publisherId, int? publicationYear = null, DateTimeOffset? enteredAt = null, DateOnly? acquisitionDate = null)
    {
        _factory.Clock.UtcNow = enteredAt;
        try
        {
            return (await PostAsync<EditionForm>(
                $"/admin/albums/{albumId}/editions",
                new CreateEditionRequest(
                    new EditionContent(
                        publisherId, null, publicationYear, null, null, null, null, null, null, null, false, true, null,
                        AcquisitionMode.Purchase, false, acquisitionDate, null, null, false, null, null, null, null),
                    await GetAlbumVersionAsync(albumId)))).Id;
        }
        finally
        {
            _factory.Clock.UtcNow = null;
        }
    }

    /// <returns>The edition targeted by the intent, not owned.</returns>
    private async Task<Guid> CreateIntendedEditionAsync(Guid albumId, Guid publisherId)
    {
        var intents = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, null, null, null, null, null, null, null, null, true),
                await GetAlbumVersionAsync(albumId)));
        return intents.Intents.Single(i => i.EditionId is not null).EditionId!.Value;
    }

    /// <returns>The visual uploaded, as a cover.</returns>
    private async Task<AlbumCover> UploadAsync(Guid albumId, Guid editionId, VisualType type)
    {
        var before = (await _client.GetFromJsonAsync<EditionVisualsForm>(VisualsUri(albumId, editionId)))!.Visuals.Select(v => v.Id).ToHashSet();
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(TestImages.Encode(8, 8, SKEncodedImageFormat.Jpeg)), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(type.ToString()), UploadVisualFields.Type },
            { new StringContent((await GetAlbumVersionAsync(albumId)).ToString()), UploadVisualFields.AlbumVersion },
        };
        var response = await _client.PostAsync(VisualsUri(albumId, editionId), form);
        response.EnsureSuccessStatusCode();
        var visual = (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.Visuals.Single(v => !before.Contains(v.Id));
        return new AlbumCover(visual.Id);
    }

    private async Task<uint> GetAlbumVersionAsync(Guid albumId) =>
        (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{albumId}"))!.Version;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
