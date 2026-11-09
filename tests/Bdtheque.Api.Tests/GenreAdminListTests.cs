using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the genres of the administration (<c>GET /admin/genres</c>, visuel/listes/liste-par-liste.md): what each row
/// presents, its search on the label and its order.
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its records with a marker of its
/// own, and narrows the list to it by searching it.
/// </remarks>
public sealed class GenreAdminListTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public GenreAdminListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Rows_CarryWhatTheListPresents()
    {
        var genre = await CreateGenreAsync($"Aventure {_marker}");

        var row = Assert.Single(await ListAsync($"q={_marker}"));

        Assert.Equal((genre.Id, genre.Label, genre.Version, 0, 0), (row.Id, row.Label, row.Version, row.AlbumCount, row.SeriesCount));
        Assert.NotEqual(default, row.ModifiedAt);
    }

    [Fact]
    public async Task Rows_CountTheAlbumsAndSeriesAsTheirListsFilteredOnTheGenreFindThem()
    {
        var genre = await CreateGenreAsync($"Polar {_marker}");
        var other = await CreateGenreAsync($"Western {_marker}");
        // An album of the genre, and one of a series of the genre: the genres of an album in a series are those of the album
        // and of the series (fonctionnel/regles-metier/genres-et-univers-d-un-album.md).
        await CreateAlbumAsync($"Album {_marker}", null, [genre.Id]);
        var series = await CreateSeriesAsync($"Série {_marker}", [genre.Id]);
        await CreateAlbumAsync($"Tome {_marker}", series.Id, []);
        // Of both genres, counted once in each.
        await CreateAlbumAsync($"Album double {_marker}", series.Id, [genre.Id, other.Id]);

        var rows = await ListAsync($"q={_marker}");

        Assert.Equal([(genre.Id, 3, 1), (other.Id, 1, 0)], rows.Select(r => (r.Id, r.AlbumCount, r.SeriesCount)));
        Assert.Equal(3, (await GetAsync<AlbumAdminList>($"/admin/albums?genreId={genre.Id}")).Albums.TotalCount);
        Assert.Equal(1, (await GetAsync<SeriesAdminList>($"/admin/series?genreId={genre.Id}")).Series.TotalCount);
    }

    [Fact]
    public async Task List_SearchesTheLabel_InAnyCaseAndAccents_AndIsOrderedByLabel()
    {
        var epic = await CreateGenreAsync($"Épopée {_marker}");
        var western = await CreateGenreAsync($"Western {_marker}");
        var adventure = await CreateGenreAsync($"Aventure {_marker}");

        Assert.Equal([adventure.Id, epic.Id, western.Id], (await ListAsync($"q={_marker}")).Select(r => r.Id));
        Assert.Equal([epic.Id], (await ListAsync($"q=EPOPEE {_marker}")).Select(r => r.Id));
    }

    private async Task<IReadOnlyList<GenreAdminListItem>> ListAsync(string query) =>
        (await GetAsync<Page<GenreAdminListItem>>($"/admin/genres?{query}")).Items;

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<GenreForm> CreateGenreAsync(string label) => PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest(label));

    private Task<SeriesForm> CreateSeriesAsync(string title, IReadOnlyList<Guid> genreIds) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(title, null, null, null, false, false, false, null, null, NoTemplate, genreIds, [], []));

    private Task<AlbumForm> CreateAlbumAsync(string title, Guid? seriesId, IReadOnlyList<Guid> genreIds) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, null, null, null, null, null, null, null, null, genreIds, [], []));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
