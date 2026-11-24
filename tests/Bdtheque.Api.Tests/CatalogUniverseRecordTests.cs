using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The record of a universe in the consultation (<c>/catalog/universes/{id}</c>, visuel/fiche-univers/): its description, its
/// ancestors and sub-universes, and its series and albums, those of its sub-universes included, presented as the bibliography
/// of an author, whole, without filter (fonctionnel/structure-de-l-application.md).
/// </summary>
/// <remarks>Each test attaches its records to universes of its own, which narrows the records to them.</remarks>
public sealed class CatalogUniverseRecordTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;

    public CatalogUniverseRecordTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Record_ListsItsSeriesAndAlbumsWithoutSeriesTogetherBySortKey_EachSeriesWithItsAlbumsAttached()
    {
        var universe = await CreateUniverseAsync("Gaule", null);
        var other = await CreateUniverseAsync("Ailleurs", null);
        // A series attached itself: all its albums (fonctionnel/regles-metier/genres-et-univers-d-un-album.md), in its order.
        var asterix = await CreateSeriesAsync("Astérix", universeIds: [universe.Id]);
        var asterix2 = await CreateAlbumAsync("La Serpe d'or", asterix.Id, volume: 2);
        var asterix1 = await CreateAlbumAsync("Astérix le Gaulois", asterix.Id, volume: 1, firstPublicationYear: 1961);
        // A series attached through some of its albums only: those only.
        var blake = await CreateSeriesAsync("Blake et Mortimer");
        var blake1 = await CreateAlbumAsync("Le Secret de l'Espadon", blake.Id, volume: 1, universeIds: [universe.Id]);
        await CreateAlbumAsync("La Marque jaune", blake.Id, volume: 2, universeIds: [other.Id]);
        // A series attached itself, without album.
        var corto = await CreateSeriesAsync("Corto Maltese", universeIds: [universe.Id]);
        // An album without series.
        var pharaon = await CreateAlbumAsync("Le Dernier Pharaon", null, universeIds: [universe.Id]);
        // Records attached elsewhere, or nowhere.
        var unrelated = await CreateSeriesAsync("Achille Talon", universeIds: [other.Id]);
        await CreateAlbumAsync("Achille Talon et le Grain de la folie", unrelated.Id, volume: 1);
        await CreateAlbumAsync("Album sans univers", null);

        var record = await GetAsync<UniverseDetail>($"/catalog/universes/{universe.Id}");

        // "Astérix" < "Blake et Mortimer" < "Corto Maltese" < "Dernier Pharaon [Le]": the whole list, without pagination.
        Assert.Equal(
            [(asterix.Id, (Guid?)null), (blake.Id, null), (corto.Id, null), (null, pharaon.Id)],
            record.Works.Select(e => (e.Series?.Series.Id, e.Album?.Album.Album.Id)));

        var asterixEntry = record.Works[0].Series!;
        Assert.Equal((new SeriesListItem(asterix.Id, "Astérix"), true), (asterixEntry.Series, asterixEntry.OnSeries));
        Assert.Equal([asterix1.Id, asterix2.Id], asterixEntry.Albums.Select(a => a.Album.Album.Id));
        // Nobody is credited on a universe: no roles; the date of first publication, as in a bibliography.
        Assert.Empty(asterixEntry.Albums[0].Roles);
        Assert.Equal(1961, asterixEntry.Albums[0].FirstPublicationYear);
        Assert.Empty(asterixEntry.Roles);
        var blakeEntry = record.Works[1].Series!;
        Assert.Equal([blake1.Id], blakeEntry.Albums.Select(a => a.Album.Album.Id));
        Assert.False(blakeEntry.OnSeries);
        Assert.Equal(2, blakeEntry.Overview.AlbumCount);
        Assert.Empty(record.Works[2].Series!.Albums);
    }

    [Fact]
    public async Task Record_ListsTheSeriesAndAlbumsOfItsSubUniversesToo_AndPresentsItsAncestorsAndItsSubUniversesAtEveryLevel()
    {
        var marvel = await CreateUniverseAsync("Marvel", null, "L'univers partagé de Marvel Comics.");
        var xMen = await CreateUniverseAsync("X-Men", marvel.Id);
        var asgard = await CreateUniverseAsync("Asgard", marvel.Id);
        var mutants = await CreateUniverseAsync("Mutants", xMen.Id);
        var image = await CreateUniverseAsync("Image", null);
        // Attached to the universe itself.
        var avengers = await CreateSeriesAsync("Avengers", universeIds: [marvel.Id]);
        // Attached to a sub-universe: the series, an album without series, an album of a series not attached.
        var wolverine = await CreateSeriesAsync("Wolverine", universeIds: [mutants.Id]);
        var hulk = await CreateAlbumAsync("Hulk", null, universeIds: [xMen.Id]);
        var daredevil = await CreateSeriesAsync("Daredevil");
        var daredevil1 = await CreateAlbumAsync("Born Again", daredevil.Id, volume: 1, universeIds: [xMen.Id]);
        await CreateAlbumAsync("Guardian Devil", daredevil.Id, volume: 2);
        // Attached elsewhere.
        await CreateSeriesAsync("Spawn", universeIds: [image.Id]);

        var top = await GetAsync<UniverseDetail>($"/catalog/universes/{marvel.Id}");
        var sub = await GetAsync<UniverseDetail>($"/catalog/universes/{xMen.Id}");

        Assert.Equal((marvel.Name, "L'univers partagé de Marvel Comics.", 0), (top.Name, top.Description, top.Ancestors.Count));
        // Its sub-universes at every level, in the order of the hierarchy: each followed by its own, siblings by name.
        Assert.Equal([(asgard.Id, (Guid?)marvel.Id), (xMen.Id, marvel.Id), (mutants.Id, xMen.Id)], top.SubUniverses.Select(u => (u.Id, u.ParentId)));
        Assert.Equal(
            [(avengers.Id, (Guid?)null), (daredevil.Id, null), (null, hulk.Id), (wolverine.Id, null)],
            top.Works.Select(e => (e.Series?.Series.Id, e.Album?.Album.Album.Id)));
        Assert.Equal([daredevil1.Id], top.Works[1].Series!.Albums.Select(a => a.Album.Album.Id));
        Assert.False(top.Works[1].Series!.OnSeries);
        // Attached to a sub-universe, a series is attached itself to its ancestors (fonctionnel/regles-metier/hierarchie-des-univers.md).
        Assert.True(top.Works[3].Series!.OnSeries);

        Assert.Null(sub.Description);
        Assert.Equal([new UniverseListItem(marvel.Id, marvel.Name, null, null)], sub.Ancestors);
        Assert.Equal([mutants.Id], sub.SubUniverses.Select(u => u.Id));
        // The series and albums of the parent are not those of its sub-universe.
        Assert.Equal(
            [(daredevil.Id, (Guid?)null), (null, hulk.Id), (wolverine.Id, null)],
            sub.Works.Select(e => (e.Series?.Series.Id, e.Album?.Album.Album.Id)));
    }

    [Fact]
    public async Task UnknownUniverse_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/catalog/universes/{Guid.NewGuid()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    // Its name made unique, under its parent, the top level being shared by every test of the class.
    private Task<UniverseForm> CreateUniverseAsync(string name, Guid? parentId, string? description = null) =>
        PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest(
            parentId is null ? $"{name} {Guid.NewGuid():N}" : name, description, parentId));

    private Task<SeriesForm> CreateSeriesAsync(string title, Guid[]? universeIds = null) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, null, null, false, false, false, null, null, NoTemplate, [], universeIds ?? [], []));

    private Task<AlbumForm> CreateAlbumAsync(
        string? title, Guid? seriesId, int? volume = null, Guid[]? universeIds = null, int? firstPublicationYear = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volume, null, null, firstPublicationYear, null, null, null, null,
            [], universeIds ?? [], []));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
