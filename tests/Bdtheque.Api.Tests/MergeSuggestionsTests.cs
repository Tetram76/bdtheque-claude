using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The records the choice of the other record of a merge proposes from its opening (fonctionnel/regles-metier/fusion-des-fiches.md,
/// fiches pressenties): the list of their type of the administration, restricted by <c>resembling</c> to the records that
/// resemble one of them, the closest first, the record itself never among them.
/// </summary>
/// <remarks>
/// The lists cover the whole base, shared by the tests of the class: each test names its records with a marker of its own,
/// eight characters that set them far apart from the records of the other tests.
/// </remarks>
public sealed class MergeSuggestionsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client = factory.CreateApiClient();
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Genres_ResemblingOne_AreTheClosestFirst_WithoutInclusion()
    {
        var subject = await CreateGenreAsync($"Science-fiction {_marker}");
        var typo = await CreateGenreAsync($"Science fction {_marker}");
        var same = await CreateGenreAsync($"SCIENCE FICTION {_marker}");
        await CreateGenreAsync($"Science-fiction {_marker} humoristique");
        await CreateGenreAsync($"Polar {_marker}");

        Assert.Equal([same.Id, typo.Id], (await GetAsync<Page<GenreAdminListItem>>(Resembling("genres", subject.Id))).Items.Select(g => g.Id));
    }

    [Fact]
    public async Task Authors_ResembleByEachOfTheirIdentifiers_InclusionComprised()
    {
        var subject = await CreateAuthorAsync(null, null, $"Gotlib{_marker}");
        var named = await CreateAuthorAsync($"Gotlib{_marker}", "Marcel", null);
        var typo = await CreateAuthorAsync(null, null, $"Gotlb{_marker}");
        await CreateAuthorAsync($"Franquin{_marker}", "André", null);

        var rows = (await GetAsync<Page<AuthorAdminListItem>>(Resembling("authors", subject.Id))).Items;

        // The pseudonym of one is the name of the other: identical, before the typing mistake.
        Assert.Equal([named.Id, typo.Id], rows.Select(a => a.Id));
    }

    [Fact]
    public async Task Publishers_Resemble_OneNameWithinTheOther()
    {
        var subject = await CreatePublisherAsync($"Dupuis {_marker}");
        var within = await CreatePublisherAsync($"Éditions Dupuis {_marker}");
        await CreatePublisherAsync($"Dargaud {_marker}");

        Assert.Equal([within.Id], (await GetAsync<Page<PublisherAdminListItem>>(Resembling("publishers", subject.Id))).Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Series_ResembleWhateverTheNotationOfTheirNumbers()
    {
        var subject = await CreateSeriesAsync($"Les 7 Boules {_marker}");
        var words = await CreateSeriesAsync($"Sept boules {_marker} (Les)");
        await CreateSeriesAsync($"Les 8 Boules {_marker} de cristal en plus");

        Assert.Equal([words.Id], (await GetAsync<SeriesAdminList>(Resembling("series", subject.Id))).Series.Items.Select(s => s.Id));
    }

    [Fact]
    public async Task Albums_Resemble_ByTheirLabel_OrBeingTheSameVolumeOfTheSameSeries()
    {
        var series = await CreateSeriesAsync($"Tintin {_marker}");
        var subject = await CreateAlbumAsync("Le Lotus bleu", series.Id, 5);
        var sameVolume = await CreateAlbumAsync(null, series.Id, 5);
        var labelled = await CreateAlbumAsync($"Tintin {_marker} – Le Lotus bleu", null, null);
        // Another volume of the series without a title of its own: one character away by its label, yet another album.
        await CreateAlbumAsync(null, series.Id, 6);

        var rows = (await GetAsync<AlbumAdminList>(Resembling("albums", subject.Id))).Albums.Items;

        Assert.Equal([sameVolume.Id, labelled.Id], rows.Select(r => r.Album.Id));
    }

    [Fact]
    public async Task Editions_Resemble_ByTheirIsbnInEitherForm_OrTheirAlbumAndPublisher()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}");
        var other = await CreatePublisherAsync($"Dargaud {_marker}");
        var album = await CreateAlbumAsync($"Album {_marker}", null, null);
        var otherAlbum = await CreateAlbumAsync($"Autre {_marker}", null, null);
        var core = string.Concat(Guid.NewGuid().ToByteArray().Take(9).Select(b => (char)('0' + b % 10)));
        var subject = await CreateEditionAsync(album.Id, publisher.Id, core + "0");
        var samePublisher = await CreateEditionAsync(album.Id, publisher.Id, null);
        var sameIsbn = await CreateEditionAsync(otherAlbum.Id, other.Id, $"978-{core}-1");
        await CreateEditionAsync(album.Id, other.Id, null);

        var rows = (await GetAsync<EditionAdminList>(Resembling("editions", subject.Id))).Editions.Items;

        // Alike, in the order of the list: by album.
        Assert.Equal([samePublisher.Id, sameIsbn.Id], rows.Select(r => r.Edition.Id));
    }

    [Fact]
    public async Task Universes_ResembleWhateverTheirParent_WithoutInclusion()
    {
        var parent = await CreateUniverseAsync($"Lucasfilm {_marker}", null);
        var subject = await CreateUniverseAsync($"Star Wars {_marker}", null);
        var child = await CreateUniverseAsync($"Starwars {_marker}", parent);
        await CreateUniverseAsync($"Star Wars {_marker} Clone Wars", subject);

        Assert.Equal([child], (await GetAsync<Page<UniverseAdminListItem>>(Resembling("universes", subject))).Items.Select(u => u.Id));
    }

    [Fact]
    public async Task Suggestions_AreBoundedByThePageSize()
    {
        var subject = await CreateGenreAsync($"Aventure {_marker}");
        foreach (var label in new[] { "Aventures", "Aventurre", "Aventur", "Avanture" })
            await CreateGenreAsync($"{label} {_marker}");

        var page = await GetAsync<Page<GenreAdminListItem>>($"{Resembling("genres", subject.Id)}&pageSize=2");

        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task Suggestions_ForARecordThatDoesNotExist_AreNone()
    {
        await CreateGenreAsync($"Western {_marker}");

        Assert.Empty((await GetAsync<Page<GenreAdminListItem>>(Resembling("genres", Guid.NewGuid()))).Items);
    }

    private static string Resembling(string list, Guid subject) => $"/admin/{list}?resembling={subject}";

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<GenreForm> CreateGenreAsync(string label) => PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest(label));

    private Task<AuthorForm> CreateAuthorAsync(string? lastName, string? firstName, string? pseudonym) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(lastName, firstName, pseudonym, null, null));

    private Task<PublisherForm> CreatePublisherAsync(string name) =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent(name, null, []));

    private Task<SeriesForm> CreateSeriesAsync(string title) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(title, null, null, null, false, false, false, null, null, NoTemplate, [], [], []));

    private Task<AlbumForm> CreateAlbumAsync(string? title, Guid? seriesId, int? volume) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volume, null, null, null, null, null, null, null, [], [], []));

    private async Task<EditionForm> CreateEditionAsync(Guid albumId, Guid publisherId, string? isbn) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(
                    publisherId, null, null, isbn, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                (await GetAsync<AlbumForm>($"/admin/albums/{albumId}")).Version));

    private async Task<Guid> CreateUniverseAsync(string name, Guid? parentId) =>
        (await PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest(name, null, parentId))).Id;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
