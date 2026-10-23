using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Works of a genre and of a universe (<c>/catalog/genres/{id}/works</c>,
/// <c>/catalog/universes/{id}/works</c>), which have no record: their series and albums, presented as
/// the bibliography of an author (fonctionnel/index.md § Structure de l'application).
/// </summary>
/// <remarks>Each test attaches its records to a genre or universe of its own, which narrows the works to them.</remarks>
public sealed class CatalogWorksTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;

    public CatalogWorksTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Genre_ListsItsSeriesAndAlbumsWithoutSeriesTogetherBySortKey_EachSeriesWithItsAlbumsOfTheGenre()
    {
        var genre = await CreateGenreAsync("Aventure");
        var other = await CreateGenreAsync("Humour");
        // A series of the genre: all its albums are of the genre (fonctionnel/regles-metier.md § Genres et univers d'un album), in its order.
        var asterix = await CreateSeriesAsync("Astérix", genreIds: [genre.Id]);
        var asterix2 = await CreateAlbumAsync("La Serpe d'or", asterix.Id, volume: 2);
        var asterix1 = await CreateAlbumAsync("Astérix le Gaulois", asterix.Id, volume: 1);
        // A series whose albums only are partly of the genre: those only.
        var blake = await CreateSeriesAsync("Blake et Mortimer");
        var blake1 = await CreateAlbumAsync("Le Secret de l'Espadon", blake.Id, volume: 1, genreIds: [genre.Id]);
        await CreateAlbumAsync("La Marque jaune", blake.Id, volume: 2, genreIds: [other.Id]);
        // A series of the genre without album.
        var corto = await CreateSeriesAsync("Corto Maltese", genreIds: [genre.Id]);
        // An album without series, of the genre.
        var pharaon = await CreateAlbumAsync("Le Dernier Pharaon", null, genreIds: [genre.Id]);
        // Records not of the genre.
        var unrelated = await CreateSeriesAsync("Achille Talon", genreIds: [other.Id]);
        await CreateAlbumAsync("Achille Talon et le Grain de la folie", unrelated.Id, volume: 1);
        await CreateAlbumAsync("Album sans genre", null);

        var works = await GetAsync<GenreWorks>($"/catalog/genres/{genre.Id}/works");

        Assert.Equal(new GenreListItem(genre.Id, genre.Label), works.Genre);
        // "Astérix" < "Blake et Mortimer" < "Corto Maltese" < "Dernier Pharaon [Le]".
        Assert.Equal(
            [(asterix.Id, (Guid?)null), (blake.Id, null), (corto.Id, null), (null, pharaon.Id)],
            works.Works.Items.Select(e => (e.Series?.Series.Id, e.Album?.Album.Id)));
        Assert.Equal((1, 50, 4), (works.Works.Number, works.Works.Size, works.Works.TotalCount));

        var asterixEntry = works.Works.Items[0].Series!;
        Assert.Equal(new SeriesListItem(asterix.Id, "Astérix"), asterixEntry.Series);
        Assert.Equal([asterix1.Id, asterix2.Id], asterixEntry.Albums.Select(a => a.Album.Id));
        Assert.Equal(new AlbumSummary(asterix1.Id, "Astérix le Gaulois", asterix.Id, "Astérix", AlbumType.Regular, false, 1, null, null, false),
            asterixEntry.Albums[0].Album);
        Assert.Equal([blake1.Id], works.Works.Items[1].Series!.Albums.Select(a => a.Album.Id));
        Assert.Empty(works.Works.Items[2].Series!.Albums);
    }

    [Fact]
    public async Task Works_ArePaginatedOnTheirFirstLevel()
    {
        var genre = await CreateGenreAsync("Polar");
        var series = await CreateSeriesAsync("Blacksad", genreIds: [genre.Id]);
        var blacksad1 = await CreateAlbumAsync("Quelque part entre les ombres", series.Id, volume: 1);
        var blacksad2 = await CreateAlbumAsync("Arctic-Nation", series.Id, volume: 2);
        await CreateAlbumAsync("Alack Sinner", null, genreIds: [genre.Id]);
        var tirLa = await CreateAlbumAsync("Le Tir de la mort", null, genreIds: [genre.Id]);

        var works = await GetAsync<GenreWorks>($"/catalog/genres/{genre.Id}/works?page=2&pageSize=2");

        // "Alack Sinner" < "Blacksad" < "Tir de la mort [Le]": a series counts once, with all its albums.
        Assert.Equal((2, 2, 3), (works.Works.Number, works.Works.Size, works.Works.TotalCount));
        Assert.Equal([tirLa.Id], works.Works.Items.Select(e => e.Album!.Album.Id));
        var firstPage = await GetAsync<GenreWorks>($"/catalog/genres/{genre.Id}/works?page=1&pageSize=2");
        Assert.Equal([blacksad1.Id, blacksad2.Id], firstPage.Works.Items[1].Series!.Albums.Select(a => a.Album.Id));
    }

    [Fact]
    public async Task Universe_ListsTheWorksOfItsSubUniversesToo_AndPresentsItsAncestors()
    {
        var marvel = await CreateUniverseAsync("Marvel", null);
        var xMen = await CreateUniverseAsync("X-Men", marvel.Id);
        var image = await CreateUniverseAsync("Image", null);
        // Attached to the universe itself.
        var avengers = await CreateSeriesAsync("Avengers", universeIds: [marvel.Id]);
        // Attached to a sub-universe: the series, an album without series, an album of a series not attached.
        var wolverine = await CreateSeriesAsync("Wolverine", universeIds: [xMen.Id]);
        var hulk = await CreateAlbumAsync("Hulk", null, universeIds: [xMen.Id]);
        var daredevil = await CreateSeriesAsync("Daredevil");
        var daredevil1 = await CreateAlbumAsync("Born Again", daredevil.Id, volume: 1, universeIds: [xMen.Id]);
        await CreateAlbumAsync("Guardian Devil", daredevil.Id, volume: 2);
        // Attached elsewhere.
        await CreateSeriesAsync("Spawn", universeIds: [image.Id]);

        var top = await GetAsync<UniverseWorks>($"/catalog/universes/{marvel.Id}/works");
        var sub = await GetAsync<UniverseWorks>($"/catalog/universes/{xMen.Id}/works");

        Assert.Equal((new UniverseListItem(marvel.Id, "Marvel", null, null), 0), (top.Universe.Universe, top.Universe.Ancestors.Count));
        Assert.Equal(
            [(avengers.Id, (Guid?)null), (daredevil.Id, null), (null, hulk.Id), (wolverine.Id, null)],
            top.Works.Items.Select(e => (e.Series?.Series.Id, e.Album?.Album.Id)));
        Assert.Equal([daredevil1.Id], top.Works.Items[1].Series!.Albums.Select(a => a.Album.Id));

        Assert.Equal([new UniverseListItem(marvel.Id, "Marvel", null, null)], sub.Universe.Ancestors);
        // The works of the parent are not those of its sub-universe.
        Assert.Equal(
            [(daredevil.Id, (Guid?)null), (null, hulk.Id), (wolverine.Id, null)],
            sub.Works.Items.Select(e => (e.Series?.Series.Id, e.Album?.Album.Id)));
    }

    [Theory]
    [InlineData("genres")]
    [InlineData("universes")]
    public async Task WorksOfAnUnknownRecord_AreAFunctionalError(string resource)
    {
        var response = await _client.GetAsync($"/catalog/{resource}/{Guid.NewGuid()}/works");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<GenreForm> CreateGenreAsync(string label) =>
        PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"{label} {Guid.NewGuid():N}"));

    private Task<UniverseForm> CreateUniverseAsync(string name, Guid? parentId) =>
        PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest(name, null, parentId));

    private Task<SeriesForm> CreateSeriesAsync(string title, Guid[]? genreIds = null, Guid[]? universeIds = null) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, null, null, false, false, false, null, null, NoTemplate, genreIds ?? [], universeIds ?? [], []));

    private Task<AlbumForm> CreateAlbumAsync(
        string? title, Guid? seriesId, int? volume = null, Guid[]? genreIds = null, Guid[]? universeIds = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volume, null, null, null, null, null, null, null,
            genreIds ?? [], universeIds ?? [], []));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
