using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Lists and search of the consultation (<c>/catalog/…</c>): order, navigation by initial, search
/// insensitive to case and accents, and cross filters.
/// </summary>
/// <remarks>
/// The lists cover the whole base, shared by the tests of the class: each test names its records
/// with a marker of its own, and narrows every list to it — by searching it, or by a filter on a
/// record of its own. The few tests that cannot (authors searched by their real names, editions by
/// their ISBN) use names and ISBNs no other test uses.
/// </remarks>
public sealed class CatalogListTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public CatalogListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Series_AreSortedBySortKeyAndNarrowedByNavigationEntry()
    {
        var tintin = await CreateSeriesAsync($"Tintin {_marker}");
        var epervier = await CreateSeriesAsync($"L'Épervier {_marker}");
        var thirteen = await CreateSeriesAsync($"13 {_marker}");
        var schtroumpfs = await CreateSeriesAsync($"Les Schtroumpfs {_marker}");

        // Sort keys: "13 …" < "Épervier … [L']" < "Schtroumpfs … [Les]" < "Tintin …".
        Assert.Equal(
            [
                new SeriesListItem(thirteen.Id, $"13 {_marker}"),
                new SeriesListItem(epervier.Id, $"L'Épervier {_marker}"),
                new SeriesListItem(schtroumpfs.Id, $"Les Schtroumpfs {_marker}"),
                new SeriesListItem(tintin.Id, $"Tintin {_marker}"),
            ],
            await ListAsync<SeriesListItem>("series", $"q={_marker}"));
        Assert.Equal([epervier.Id], Ids(await ListAsync<SeriesListItem>("series", $"q={_marker}&entry=E")));
        Assert.Equal([thirteen.Id], Ids(await ListAsync<SeriesListItem>("series", $"q={_marker}&entry=%23")));
    }

    [Fact]
    public async Task Search_IgnoresCaseAndAccents()
    {
        var epervier = await CreateSeriesAsync($"L'Épervier {_marker}");

        var found = await ListAsync<SeriesListItem>("series", $"q={Uri.EscapeDataString($"EPERVIER {_marker.ToUpperInvariant()}")}");

        Assert.Equal([epervier.Id], Ids(found));
    }

    [Fact]
    public async Task Search_LooksForTheWildcardsOfLikeAsTheyAre()
    {
        var percent = await CreateSeriesAsync($"{_marker} 100%");
        await CreateSeriesAsync($"{_marker} 1000");
        var underscore = await CreateSeriesAsync($"{_marker} A_B");
        await CreateSeriesAsync($"{_marker} AxB");

        Assert.Equal([percent.Id], Ids(await ListAsync<SeriesListItem>("series", $"q={Uri.EscapeDataString($"{_marker} 100%")}")));
        Assert.Equal([underscore.Id], Ids(await ListAsync<SeriesListItem>("series", $"q={Uri.EscapeDataString($"{_marker} A_B")}")));
    }

    [Fact]
    public async Task Albums_AreSortedBySortKeyThatOfTheSeriesStandingInWithWhatTheirLabelsNeed()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}");
        var series = await CreateSeriesAsync($"Les Aventures de Tintin {_marker}");
        // Found by the title of their series, part of their label; sorted by their own sort key, or by
        // that of their series without a title of their own, then by volume:
        // "Aventures de Tintin … [Les]" (volumes 1, 2) < "Lotus bleu … [Le]" < "Zorglub …".
        var zorglub = await CreateAlbumAsync($"Zorglub {_marker}");
        var lotus = await CreateAlbumAsync($"Le Lotus bleu {_marker}", series.Id, 5);
        var second = await CreateAlbumAsync(null, series.Id, 2);
        var first = await CreateAlbumAsync(null, series.Id, 1);
        await CreateOwnedEditionAsync(lotus.Id, publisher.Id);
        await CreateIntendedEditionAsync(zorglub.Id, publisher.Id);

        Assert.Equal(
            [
                new AlbumSummary(first.Id, null, series.Id, $"Les Aventures de Tintin {_marker}", AlbumType.Regular, false, 1, null, null, false),
                new AlbumSummary(second.Id, null, series.Id, $"Les Aventures de Tintin {_marker}", AlbumType.Regular, false, 2, null, null, false),
                new AlbumSummary(lotus.Id, $"Le Lotus bleu {_marker}", series.Id, $"Les Aventures de Tintin {_marker}", AlbumType.Regular, false, 5, null, null, true),
                new AlbumSummary(zorglub.Id, $"Zorglub {_marker}", null, null, AlbumType.Regular, false, null, null, null, false),
            ],
            await ListAsync<AlbumSummary>("albums", $"q={_marker}"));
    }

    [Fact]
    public async Task Albums_WithoutTitle_FollowTheCurrentSortKeyAndEntryOfTheirSeries()
    {
        var series = await CreateSeriesAsync($"Zorglub {_marker}");
        var untitled = await CreateAlbumAsync(null, series.Id, 1);
        var asterix = await CreateAlbumAsync($"Astérix {_marker}");

        Assert.Equal([untitled.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&entry=Z")));

        await PutAsync($"/admin/series/{series.Id}", new UpdateSeriesRequest(SeriesContentOf($"Alix {_marker}"), series.Version));

        Assert.Empty(await ListAsync<AlbumSummary>("albums", $"q={_marker}&entry=Z"));
        // "Alix …" < "Astérix …".
        Assert.Equal([untitled.Id, asterix.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&entry=A")));
    }

    [Fact]
    public async Task Albums_AreFilteredByTheRecordsTheyAreLinkedTo()
    {
        var author = await CreateAuthorAsync(null, null, $"Auteur {_marker}");
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        var collection = await CreateCollectionAsync(publisher, $"Collection {_marker}");
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        var universe = await CreateUniverseAsync($"Univers {_marker}", null);
        var subUniverse = await CreateUniverseAsync($"Sous-univers {_marker}", universe.Id);
        var subSubUniverse = await CreateUniverseAsync($"Sous-sous-univers {_marker}", subUniverse.Id);
        var series = await CreateSeriesAsync($"Alpha {_marker}", genreIds: [genre.Id], universeIds: [subUniverse.Id]);

        // Genres and universes of an album in a series are those of the album and of the series.
        var inSeries = await CreateAlbumAsync(null, series.Id, 1);
        var credited = await CreateAlbumAsync($"Bravo {_marker}", contributions: [new ContributionContent(author.Id, ContributionRole.Scenarist)]);
        var published = await CreateAlbumAsync($"Charlie {_marker}");
        await CreateOwnedEditionAsync(published.Id, publisher.Id, collection.Id);
        var ofGenre = await CreateAlbumAsync($"Delta {_marker}", genreIds: [genre.Id]);
        var ofSubSubUniverse = await CreateAlbumAsync($"Echo {_marker}", universeIds: [subSubUniverse.Id]);
        await CreateAlbumAsync($"Foxtrot {_marker}");

        Assert.Equal([inSeries.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&seriesId={series.Id}")));
        Assert.Equal([credited.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&authorId={author.Id}")));
        Assert.Equal([published.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&publisherId={publisher.Id}")));
        Assert.Equal([published.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&publisherCollectionId={collection.Id}")));
        Assert.Equal([inSeries.Id, ofGenre.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&genreId={genre.Id}")));
        // A record attached to a universe is attached to its parents (fonctionnel.md § Hiérarchie des univers).
        Assert.Equal([inSeries.Id, ofSubSubUniverse.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&universeId={universe.Id}")));
        Assert.Equal([ofSubSubUniverse.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&universeId={subSubUniverse.Id}")));
        Assert.Equal([inSeries.Id], Ids(await ListAsync<AlbumSummary>("albums", $"q={_marker}&genreId={genre.Id}&universeId={universe.Id}&entry=A")));
        Assert.Empty(await ListAsync<AlbumSummary>("albums", $"q={_marker}&genreId={Guid.NewGuid()}"));
        Assert.Empty(await ListAsync<AlbumSummary>("albums", $"q={_marker}&universeId={Guid.NewGuid()}"));
    }

    [Fact]
    public async Task Series_AreFilteredByTheRecordsTheyAreLinkedTo()
    {
        var author = await CreateAuthorAsync(null, null, $"Auteur {_marker}");
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        var universe = await CreateUniverseAsync($"Univers {_marker}", null);
        var subUniverse = await CreateUniverseAsync($"Sous-univers {_marker}", universe.Id);

        // Credited and published through their albums, the source of truth of contributions and
        // editions: the template of a series is only the starting point of the data entry.
        var throughAlbums = await CreateSeriesAsync($"Alpha {_marker}");
        var album = await CreateAlbumAsync(null, throughAlbums.Id, 1, contributions: [new ContributionContent(author.Id, ContributionRole.Illustrator)]);
        await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var classified = await CreateSeriesAsync($"Bravo {_marker}", genreIds: [genre.Id], universeIds: [subUniverse.Id]);
        await CreateSeriesAsync(
            $"Charlie {_marker}",
            template: NoTemplate with { PublisherId = publisher.Id },
            contributions: [new ContributionContent(author.Id, ContributionRole.Scenarist)]);

        Assert.Equal([throughAlbums.Id], Ids(await ListAsync<SeriesListItem>("series", $"q={_marker}&authorId={author.Id}")));
        Assert.Equal([throughAlbums.Id], Ids(await ListAsync<SeriesListItem>("series", $"q={_marker}&publisherId={publisher.Id}")));
        Assert.Equal([classified.Id], Ids(await ListAsync<SeriesListItem>("series", $"q={_marker}&genreId={genre.Id}")));
        Assert.Equal([classified.Id], Ids(await ListAsync<SeriesListItem>("series", $"q={_marker}&universeId={universe.Id}")));
    }

    [Fact]
    public async Task Editions_AreSortedByAlbumThenYearAndFoundByIsbnWhateverTheSeparators()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var collection = await CreateCollectionAsync(publisher, $"Lucky Luke {_marker}");
        var alpha = await CreateAlbumAsync($"Alpha {_marker}");
        var bravo = await CreateAlbumAsync($"Bravo {_marker}");
        var bravoEdition = await CreateOwnedEditionAsync(bravo.Id, publisher.Id, collection.Id, 1990, "2-205-00217-1");
        var intended = await CreateIntendedEditionAsync(alpha.Id, publisher.Id, 2024, "2205 00999 1");
        var alphaEdition = await CreateOwnedEditionAsync(alpha.Id, publisher.Id, publicationYear: 1985);

        var listed = await ListAsync<EditionListItem>("editions", $"publisherId={publisher.Id}");

        Assert.Equal([alphaEdition.Id, intended, bravoEdition.Id], listed.Select(e => e.Edition.Id));
        Assert.Equal([true, false, true], listed.Select(e => e.IsInCollection));
        Assert.Equal(
            new EditionListItem(
                new AlbumSummary(bravo.Id, $"Bravo {_marker}", null, null, AlbumType.Regular, false, null, null, null, true),
                new EditionSummary(bravoEdition.Id, publisher.Id, $"Dargaud {_marker}", collection.Id, $"Lucky Luke {_marker}", 1990, "2-205-00217-1"),
                true),
            listed[2]);
        Assert.Equal([bravoEdition.Id], EditionIds(await ListAsync<EditionListItem>("editions", "q=220500217")));
        Assert.Equal([bravoEdition.Id], EditionIds(await ListAsync<EditionListItem>("editions", "q=2-205-00217")));
        Assert.Equal([intended, bravoEdition.Id], EditionIds(await ListAsync<EditionListItem>("editions", $"q={Uri.EscapeDataString("2 205")}")));
        Assert.Equal([alphaEdition.Id, intended], EditionIds(await ListAsync<EditionListItem>("editions", $"albumId={alpha.Id}")));
        Assert.Equal([bravoEdition.Id], EditionIds(await ListAsync<EditionListItem>("editions", $"publisherCollectionId={collection.Id}")));
    }

    [Fact]
    public async Task Authors_AreSortedByFamilyNameAndFoundByAnyPartOfTheirName()
    {
        // Names no other test of the class uses: the searches are not narrowed by the marker.
        var moebius = await CreateAuthorAsync("Giraud", "Jean", "Moebius");
        var vanHamme = await CreateAuthorAsync("Van Hamme", "Jean", null);
        var herge = await CreateAuthorAsync("Remi", "Georges", "Hergé");

        // Sort keys: "Moebius" < "Van Hamme Jean".
        Assert.Equal(
            [new AuthorListItem(moebius.Id, "Giraud", "Jean", "Moebius"), new AuthorListItem(vanHamme.Id, "Van Hamme", "Jean", null)],
            await ListAsync<AuthorListItem>("authors", "q=jean"));
        Assert.Equal([vanHamme.Id], Ids(await ListAsync<AuthorListItem>("authors", $"q={Uri.EscapeDataString("jean van")}")));
        Assert.Equal([vanHamme.Id], Ids(await ListAsync<AuthorListItem>("authors", $"q={Uri.EscapeDataString("hamme jean")}")));
        Assert.Equal([herge.Id], Ids(await ListAsync<AuthorListItem>("authors", "q=REMI")));
        Assert.Equal([herge.Id], Ids(await ListAsync<AuthorListItem>("authors", "q=herge")));
        Assert.Equal([vanHamme.Id], Ids(await ListAsync<AuthorListItem>("authors", "q=jean&entry=V")));
    }

    [Fact]
    public async Task Publishers_AndTheirCollections_AreSortedByNameAndSearched()
    {
        var lombard = await CreatePublisherAsync($"Le Lombard {_marker}");
        var dargaud = await CreatePublisherAsync($"Éditions Dargaud {_marker}");
        var signe = await CreateCollectionAsync(lombard, $"Signé {_marker}");
        var luckyLuke = await CreateCollectionAsync(dargaud, $"Lucky Luke {_marker}");

        Assert.Equal(
            [new PublisherListItem(dargaud.Id, $"Éditions Dargaud {_marker}"), new PublisherListItem(lombard.Id, $"Le Lombard {_marker}")],
            await ListAsync<PublisherListItem>("publishers", $"q={_marker}"));
        Assert.Equal([dargaud.Id], Ids(await ListAsync<PublisherListItem>("publishers", $"q={Uri.EscapeDataString($"editions dargaud {_marker}")}")));
        Assert.Equal(
            [
                new PublisherCollectionListItem(luckyLuke.Id, $"Lucky Luke {_marker}", dargaud.Id, $"Éditions Dargaud {_marker}"),
                new PublisherCollectionListItem(signe.Id, $"Signé {_marker}", lombard.Id, $"Le Lombard {_marker}"),
            ],
            await ListAsync<PublisherCollectionListItem>("publisher-collections", $"q={_marker}"));
        Assert.Equal([signe.Id], Ids(await ListAsync<PublisherCollectionListItem>("publisher-collections", $"q={_marker}&publisherId={lombard.Id}")));
    }

    [Fact]
    public async Task Genres_AndUniverses_AreSortedByNameAndSearched()
    {
        var epopee = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Épopée {_marker}"));
        var aventure = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Aventure {_marker}"));
        var marvel = await CreateUniverseAsync($"Marvel {_marker}", null);
        var xMen = await CreateUniverseAsync($"X-Men {_marker}", marvel.Id);

        Assert.Equal(
            [new GenreListItem(aventure.Id, $"Aventure {_marker}"), new GenreListItem(epopee.Id, $"Épopée {_marker}")],
            await ListAsync<GenreListItem>("genres", $"q={_marker}"));
        Assert.Equal([epopee.Id], Ids(await ListAsync<GenreListItem>("genres", $"q={Uri.EscapeDataString($"EPOPEE {_marker}")}")));
        Assert.Equal(
            [new UniverseListItem(marvel.Id, $"Marvel {_marker}", null, null), new UniverseListItem(xMen.Id, $"X-Men {_marker}", marvel.Id, $"Marvel {_marker}")],
            await ListAsync<UniverseListItem>("universes", $"q={_marker}"));
    }

    [Theory]
    [InlineData("series")]
    [InlineData("albums")]
    [InlineData("authors")]
    public async Task List_WithAnUnknownNavigationEntry_IsATechnicalError(string list)
    {
        // The frontend builds the navigation, never the user.
        var response = await _client.GetAsync($"/catalog/{list}?entry=a");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    [Theory]
    [InlineData("series")]
    [InlineData("albums")]
    [InlineData("editions")]
    [InlineData("authors")]
    [InlineData("publishers")]
    [InlineData("publisher-collections")]
    [InlineData("genres")]
    [InlineData("universes")]
    public async Task List_IsPaginatedWithinTheBoundsOfAPage(string list)
    {
        var page = await _client.GetFromJsonAsync<Page<object>>($"/catalog/{list}?page=2&pageSize=3");
        var outOfBounds = await _client.GetAsync($"/catalog/{list}?pageSize=101");

        Assert.Equal((2, 3), (page!.Number, page.Size));
        await ProblemAssert.IsProblemAsync(outOfBounds, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private async Task<IReadOnlyList<T>> ListAsync<T>(string list, string query) =>
        (await _client.GetFromJsonAsync<Page<T>>($"/catalog/{list}?{query}"))!.Items;

    private static IEnumerable<Guid> Ids<T>(IEnumerable<T> items) =>
        items.Select(item => (Guid)typeof(T).GetProperty("Id")!.GetValue(item)!);

    private static IEnumerable<Guid> EditionIds(IEnumerable<EditionListItem> items) => items.Select(e => e.Edition.Id);

    private static SeriesContent SeriesContentOf(
        string title, Guid[]? genreIds = null, Guid[]? universeIds = null, SeriesEditionTemplate? template = null,
        ContributionContent[]? contributions = null) =>
        new(title, null, null, null, false, false, false, null, null, template ?? NoTemplate, genreIds ?? [], universeIds ?? [], contributions ?? []);

    private Task<SeriesForm> CreateSeriesAsync(
        string title, Guid[]? genreIds = null, Guid[]? universeIds = null, SeriesEditionTemplate? template = null,
        ContributionContent[]? contributions = null) =>
        PostAsync<SeriesForm>("/admin/series", SeriesContentOf(title, genreIds, universeIds, template, contributions));

    private Task<AlbumForm> CreateAlbumAsync(
        string? title, Guid? seriesId = null, int? volumeNumber = null, Guid[]? genreIds = null, Guid[]? universeIds = null,
        ContributionContent[]? contributions = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volumeNumber, null, null, null, null, null, null, null,
            genreIds ?? [], universeIds ?? [], contributions ?? []));

    private Task<AuthorForm> CreateAuthorAsync(string? lastName, string? firstName, string? pseudonym) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(lastName, firstName, pseudonym, null, null));

    private Task<PublisherForm> CreatePublisherAsync(string name) =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest(name, null));

    private Task<PublisherCollectionForm> CreateCollectionAsync(PublisherForm publisher, string name) =>
        PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest(name, publisher.Version));

    private Task<UniverseForm> CreateUniverseAsync(string name, Guid? parentId) =>
        PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest(name, null, parentId));

    private async Task<EditionForm> CreateOwnedEditionAsync(
        Guid albumId, Guid publisherId, Guid? collectionId = null, int? publicationYear = null, string? isbn = null) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(
                    publisherId, collectionId, publicationYear, isbn, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)));

    /// <returns>The identifier of the edition targeted by the intent, not owned.</returns>
    private async Task<Guid> CreateIntendedEditionAsync(Guid albumId, Guid publisherId, int? publicationYear = null, string? isbn = null)
    {
        var intents = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, publicationYear, isbn, null, null, null, null, null, null, true),
                await GetAlbumVersionAsync(albumId)));
        return Assert.Single(intents.Intents).EditionId!.Value;
    }

    private async Task<uint> GetAlbumVersionAsync(Guid albumId) =>
        (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{albumId}"))!.Version;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }

    private async Task PutAsync(string uri, object request) => (await _client.PutAsJsonAsync(uri, request)).EnsureSuccessStatusCode();
}
