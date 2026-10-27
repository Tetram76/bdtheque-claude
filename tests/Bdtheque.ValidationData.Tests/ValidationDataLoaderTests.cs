using System.Net.Http.Json;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.ValidationData.Tests;

/// <summary>
/// The validation data, loaded through the API as the validation environment loads it: the load must
/// keep going through every rule of the API as they evolve, and the data must keep presenting the cases
/// the screens show (choix-implementation/deploiement.md § Environnement de validation fonctionnelle).
/// </summary>
public sealed class ValidationDataLoaderTests(ValidationDataLoaderTests.LoadedApi api) : IClassFixture<ValidationDataLoaderTests.LoadedApi>
{
    private readonly HttpClient _client = api.CreateApiClient();

    [Fact]
    public async Task LongSeries_HasRegularVolumesOmnibusesAndSpecialIssues_WithAndWithoutCover()
    {
        var series = await SeriesAsync(ValidationDataLoader.LongSeriesTitle);

        Assert.True(series.Albums.Count >= 20);
        Assert.Contains(series.Albums, card => card.Album is { Type: AlbumType.Omnibus, IsSpecialIssue: false });
        Assert.Contains(series.Albums, card => card.Album is { Type: AlbumType.Regular, IsSpecialIssue: true });
        Assert.Contains(series.Albums, card => card.Album is { Type: AlbumType.Omnibus, IsSpecialIssue: true });
        Assert.Contains(series.Albums, card => card.Album.Title is null);
        Assert.Contains(series.Albums, card => card.Cover is not null);
        Assert.Contains(series.Albums, card => card.Album.IsInCollection && card.Cover is null);
        Assert.Contains(series.Albums, card => !card.Album.IsInCollection);
    }

    [Fact]
    public async Task PurchaseIntents_TargetWholeAlbumsAndEditions()
    {
        var intents = await _client.GetFromJsonAsync<Page<PurchaseIntentListItem>>("/catalog/purchase-intents?pageSize=100");

        Assert.Contains(intents!.Items, intent => intent.Edition is null);
        Assert.Contains(intents.Items, intent => intent.Edition is not null && intent.Album.IsInCollection);
    }

    [Fact]
    public async Task Universes_NestOnSeveralLevels()
    {
        var universes = (await _client.GetFromJsonAsync<Page<UniverseListItem>>("/catalog/universes?pageSize=100"))!.Items;

        Assert.Contains(universes, universe =>
            universes.Any(parent => parent.Id == universe.ParentId && parent.ParentId is not null));
    }

    [Fact]
    public async Task OneShot_HasSeveralEditions_OneOfThemWithEveryTypeOfVisual()
    {
        var album = await AlbumAsync(ValidationDataLoader.MultiEditionOneShotTitle);

        Assert.Null(album.Album.SeriesId);
        Assert.True(album.Editions.Count >= 2);
        Assert.Contains(album.Editions, edition =>
            Enum.GetValues<VisualType>().All(type => edition.Visuals.Any(visual => visual.Type == type))
            && edition.Visuals.Count(visual => visual.Type == VisualType.Cover) >= 2);
        Assert.Contains(album.Editions, edition => edition.IsbnChecksumValid == false);
    }

    [Fact]
    public async Task Collective_CreditsManyAuthors()
    {
        var album = await AlbumAsync(ValidationDataLoader.CollectiveTitle);

        Assert.True(album.Contributions.Select(contribution => contribution.Author.Id).Distinct().Count() >= 6);
    }

    [Theory]
    [InlineData("series", "#")]
    [InlineData("series", "@")]
    [InlineData("albums", "@")]
    [InlineData("albums", "E")]
    public async Task NavigationEntries_OutsideTheLetters_AndUnderAnArticle_AreCovered(string list, string entry)
    {
        var page = await _client.GetFromJsonAsync<Page<object>>($"/catalog/{list}?entry={Uri.EscapeDataString(entry)}");

        Assert.NotEmpty(page!.Items);
    }

    private async Task<SeriesDetail> SeriesAsync(string title)
    {
        var found = await _client.GetFromJsonAsync<Page<SeriesListItem>>($"/catalog/series?q={Uri.EscapeDataString(title)}");
        return (await _client.GetFromJsonAsync<SeriesDetail>($"/catalog/series/{Assert.Single(found!.Items).Id}"))!;
    }

    private async Task<AlbumDetail> AlbumAsync(string title)
    {
        var found = await _client.GetFromJsonAsync<Page<AlbumSummary>>($"/catalog/albums?q={Uri.EscapeDataString(title)}");
        return (await _client.GetFromJsonAsync<AlbumDetail>($"/catalog/albums/{Assert.Single(found!.Items).Id}"))!;
    }

    /// <summary>An API on an empty database, into which the validation data is loaded once for the whole class.</summary>
    public sealed class LoadedApi : ApiHostFactory, IAsyncLifetime
    {
        async Task IAsyncLifetime.InitializeAsync()
        {
            await InitializeAsync();
            await new ValidationDataLoader(CreateApiClient()).LoadAsync(CancellationToken.None);
        }

        Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
    }
}
