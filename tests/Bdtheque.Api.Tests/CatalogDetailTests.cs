using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Records of the consultation (<c>/catalog/…/{id}</c>): everything known of an entity, the entries
/// leading to the records it is linked with, and the dates of the record.
/// </summary>
public sealed class CatalogDetailTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;

    public CatalogDetailTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Album_PresentsItsDisplayedGenresAndUniversesItsContributionsEditionsAndIntents()
    {
        var aventure = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Aventure {Guid.NewGuid():N}"));
        var humour = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Humour {Guid.NewGuid():N}"));
        var spirou = await CreateUniverseAsync("Spirou", null);
        var champignac = await CreateUniverseAsync("Champignac", spirou.Id);
        var franquin = await CreateAuthorAsync("Franquin", "André");
        var jije = await CreateAuthorAsync("Gillain", "Joseph");
        var dupuis = await CreatePublisherAsync("Dupuis");
        var series = await PostAsync<SeriesForm>("/admin/series", SeriesContentOf("Spirou et Fantasio", [aventure.Id], [spirou.Id]));
        var album = await PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            "Le Nid des Marsupilamis", null, series.Id, AlbumType.Regular, false, 12, null, null, 1960, 3, "Résumé", "Notes",
            AlbumRating.VeryGood, [aventure.Id, humour.Id], [champignac.Id],
            [new(jije.Id, ContributionRole.Scenarist), new(franquin.Id, ContributionRole.Illustrator), new(franquin.Id, ContributionRole.Scenarist)]));
        var owned = await CreateOwnedEditionAsync(album.Id, dupuis.Id, 1960);
        var (intended, intent) = await CreateIntendedEditionAsync(album.Id, dupuis.Id, 2010);

        var detail = await GetAsync<AlbumDetail>($"/catalog/albums/{album.Id}");

        Assert.Equal(
            new AlbumSummary(album.Id, "Le Nid des Marsupilamis", series.Id, "Spirou et Fantasio", AlbumType.Regular, false, 12, null, null, true),
            detail.Album);
        Assert.Equal((1960, 3, "Résumé", "Notes", AlbumRating.VeryGood),
            (detail.FirstPublicationYear, detail.FirstPublicationMonth, detail.Summary, detail.PersonalNotes, detail.Rating));
        // Union of those of the album and of its series, without duplicates.
        Assert.Equal([new GenreListItem(aventure.Id, aventure.Label), new GenreListItem(humour.Id, humour.Label)], detail.Genres);
        Assert.Equal(
            [new UniverseListItem(champignac.Id, "Champignac", spirou.Id, "Spirou"), new UniverseListItem(spirou.Id, "Spirou", null, null)],
            detail.Universes);
        // By role, then by author; created with the album, untouched since.
        Assert.Equal(
            [
                (new AuthorListItem(franquin.Id, "Franquin", "André", null), ContributionRole.Scenarist),
                (new AuthorListItem(jije.Id, "Gillain", "Joseph", null), ContributionRole.Scenarist),
                (new AuthorListItem(franquin.Id, "Franquin", "André", null), ContributionRole.Illustrator),
            ],
            detail.Contributions.Select(c => (c.Author, c.Role)));
        Assert.All(detail.Contributions, c => Assert.Equal((detail.CreatedAt, detail.CreatedAt), (c.CreatedAt, c.ModifiedAt)));
        Assert.Equal(
            [
                (new EditionSummary(owned.Id, dupuis.Id, dupuis.Name, null, null, 1960, null), true, (Guid?)null),
                (new EditionSummary(intended, dupuis.Id, dupuis.Name, null, null, 2010, null), false, intent),
            ],
            detail.Editions.Select(e => (e.Edition, e.IsInCollection, e.PurchaseIntent?.Id)));
        AssertCreatedAfter(detail.CreatedAt, detail.Editions[1].PurchaseIntent!);
        Assert.Null(detail.PurchaseIntent);
    }

    [Fact]
    public async Task Album_NotInTheCollection_SaysSoAndShowsItsIntentOnTheWholeAlbum()
    {
        var album = await CreateAlbumAsync("Album sans édition", null);
        var intents = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, album.Version));

        var detail = await GetAsync<AlbumDetail>($"/catalog/albums/{album.Id}");

        Assert.False(detail.Album.IsInCollection);
        Assert.Equal(Assert.Single(intents.Intents).Id, detail.PurchaseIntent!.Id);
        AssertCreatedAfter(detail.CreatedAt, detail.PurchaseIntent);
        Assert.Empty(detail.Editions);
        Assert.Empty(detail.Genres);
    }

    [Fact]
    public async Task Series_PresentsItsAlbumsInTheOrderOfTheSeriesAndItsAuthorsPublisherAndCollection()
    {
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Western {Guid.NewGuid():N}"));
        var universe = await CreateUniverseAsync("Far West", null);
        var morris = await CreateAuthorAsync("De Bevere", "Maurice");
        var goscinny = await CreateAuthorAsync("Goscinny", "René");
        var dargaud = await CreatePublisherAsync("Dargaud");
        var collection = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{dargaud.Id}/collections", new CreatePublisherCollectionRequest("Lucky Luke", dargaud.Version));
        var series = await PostAsync<SeriesForm>("/admin/series", SeriesContentOf(
            "Lucky Luke", [genre.Id], [universe.Id],
            new SeriesEditionTemplate(dargaud.Id, collection.Id, null, null, BindingType.Hardcover, null, null, null, true),
            [new(goscinny.Id, ContributionRole.Scenarist), new(morris.Id, ContributionRole.Illustrator)]));

        // fonctionnel.md § Ordre des albums dans une série: the albums not special issues, then the special
        // issues, each by volume (its first one for an omnibus), failing which by first publication.
        var specialUndated = await CreateAlbumAsync("HS sans tome", series.Id, specialIssue: true);
        var special1 = await CreateAlbumAsync("HS 1", series.Id, volume: 1, specialIssue: true);
        var undated2001 = await CreateAlbumAsync("Sans tome 2001", series.Id, year: 2001);
        var volume2 = await CreateAlbumAsync("Tome 2", series.Id, volume: 2, year: 1950);
        var omnibus = await CreateAlbumAsync("Intégrale 3 à 5", series.Id, type: AlbumType.Omnibus, start: 3, end: 5);
        var undated1999 = await CreateAlbumAsync("Sans tome 1999", series.Id, year: 1999);
        var volume1 = await CreateAlbumAsync("Tome 1", series.Id, volume: 1, year: 1949);

        var detail = await GetAsync<SeriesDetail>($"/catalog/series/{series.Id}");

        Assert.Equal(
            [volume1.Id, volume2.Id, omnibus.Id, undated1999.Id, undated2001.Id, special1.Id, specialUndated.Id],
            detail.Albums.Select(a => a.Id));
        Assert.Equal(new AlbumSummary(omnibus.Id, "Intégrale 3 à 5", series.Id, "Lucky Luke", AlbumType.Omnibus, false, null, 3, 5, false),
            detail.Albums[2]);
        Assert.Equal(new PublisherListItem(dargaud.Id, dargaud.Name), detail.Publisher);
        Assert.Equal(new PublisherCollectionListItem(collection.Id, "Lucky Luke", dargaud.Id, dargaud.Name), detail.PublisherCollection);
        Assert.Equal(
            [
                (new AuthorListItem(goscinny.Id, "Goscinny", "René", null), ContributionRole.Scenarist),
                (new AuthorListItem(morris.Id, "De Bevere", "Maurice", null), ContributionRole.Illustrator),
            ],
            detail.Contributions.Select(c => (c.Author, c.Role)));
        Assert.All(detail.Contributions, c => Assert.Equal((detail.CreatedAt, detail.CreatedAt), (c.CreatedAt, c.ModifiedAt)));
        Assert.Equal([new GenreListItem(genre.Id, genre.Label)], detail.Genres);
        Assert.Equal([new UniverseListItem(universe.Id, "Far West", null, null)], detail.Universes);
        Assert.Equal(("Lucky Luke", (SeriesStatus?)null, (int?)null, false), (detail.Title, detail.Status, detail.TheoreticalVolumeCount, detail.IsComplete));
    }

    [Fact]
    public async Task Series_WithoutTemplatePublisher_HasNone()
    {
        var series = await PostAsync<SeriesForm>("/admin/series", SeriesContentOf("Sans éditeur"));

        var detail = await GetAsync<SeriesDetail>($"/catalog/series/{series.Id}");

        Assert.Null(detail.Publisher);
        Assert.Null(detail.PublisherCollection);
        Assert.Empty(detail.Albums);
    }

    [Fact]
    public async Task Edition_PresentsItsAlbumItsVisualsInOrderAndItsMembership()
    {
        var publisher = await CreatePublisherAsync("Casterman");
        var album = await CreateAlbumAsync("Le Lotus bleu", null);
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, 1946, "978-2-203-00104-9");
        var plate = await UploadAsync(album.Id, edition.Id, VisualType.Plate);
        var backCover = await UploadAsync(album.Id, edition.Id, VisualType.BackCover);
        var cover = await UploadAsync(album.Id, edition.Id, VisualType.Cover);

        var detail = await GetAsync<EditionDetail>($"/catalog/editions/{edition.Id}");

        Assert.Equal(new AlbumSummary(album.Id, "Le Lotus bleu", null, null, AlbumType.Regular, false, null, null, null, true), detail.Album);
        Assert.Equal(new EditionSummary(edition.Id, publisher.Id, publisher.Name, null, null, 1946, "978-2-203-00104-9"), detail.Edition);
        // Stored as entered, the ISBN carries the warning of its inconsistent check digit (6 expected).
        Assert.Equal((AcquisitionMode.Purchase, true, false), (detail.AcquisitionMode, detail.IsInCollection, detail.IsbnChecksumValid));
        Assert.Null(detail.PurchaseIntent);
        Assert.Equal([cover, plate, backCover], detail.Visuals.Select(v => v with { CreatedAt = default, ModifiedAt = default }));
        Assert.All(detail.Visuals, v => AssertCreatedAfter(detail.CreatedAt, (v.CreatedAt, v.ModifiedAt)));
    }

    [Fact]
    public async Task Edition_TargetedByAnIntent_IsNotInTheCollection()
    {
        var publisher = await CreatePublisherAsync("Glénat");
        var album = await CreateAlbumAsync("Album visé", null);
        var (intended, intent) = await CreateIntendedEditionAsync(album.Id, publisher.Id, 2020);

        var detail = await GetAsync<EditionDetail>($"/catalog/editions/{intended}");

        Assert.Equal((null, false, intent, null), ((AcquisitionMode?)detail.AcquisitionMode, detail.IsInCollection, detail.PurchaseIntent?.Id, detail.IsbnChecksumValid));
        Assert.Empty(detail.Visuals);
    }

    [Fact]
    public async Task Author_PresentsTheAlbumsCreditedOnInTheirOrderWithTheRoles()
    {
        var author = await CreateAuthorAsync("Hergé", null);
        var other = await CreateAuthorAsync("Jacobs", "Edgar P.");
        var temple = await CreateAlbumAsync("Le Temple du Soleil", null, contributions:
            [new(author.Id, ContributionRole.Illustrator), new(author.Id, ContributionRole.Scenarist), new(other.Id, ContributionRole.Colorist)]);
        var lotus = await CreateAlbumAsync("Le Lotus bleu", null, contributions: [new(author.Id, ContributionRole.Scenarist)]);
        await CreateAlbumAsync("Le Secret de l'Espadon", null, contributions: [new(other.Id, ContributionRole.Scenarist)]);

        var detail = await GetAsync<AuthorDetail>($"/catalog/authors/{author.Id}");

        Assert.Equal(("Hergé", (string?)null), (detail.LastName, detail.FirstName));
        Assert.Equal([lotus.Id, temple.Id], detail.Bibliography.Select(b => b.Album.Id));
        Assert.Equal([ContributionRole.Scenarist], detail.Bibliography[0].Roles);
        Assert.Equal([ContributionRole.Scenarist, ContributionRole.Illustrator], detail.Bibliography[1].Roles);
    }

    [Fact]
    public async Task Publisher_PresentsItsCollectionsByName_EachWithItsOwnRecord()
    {
        var publisher = await CreatePublisherAsync("Le Lombard");
        var signe = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest("Signé", publisher.Version));
        var aventure = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest("Aventure", signe.PublisherVersion));

        var detail = await GetAsync<PublisherDetail>($"/catalog/publishers/{publisher.Id}");
        var collection = await GetAsync<PublisherCollectionDetail>($"/catalog/publisher-collections/{signe.Id}");

        Assert.Equal(
            [
                new PublisherCollectionListItem(aventure.Id, "Aventure", publisher.Id, publisher.Name),
                new PublisherCollectionListItem(signe.Id, "Signé", publisher.Id, publisher.Name),
            ],
            detail.Collections);
        Assert.Equal((signe.Id, "Signé", publisher.Id, publisher.Name),
            (collection.Id, collection.Name, collection.PublisherId, collection.PublisherName));
    }

    [Fact]
    public async Task Universe_PresentsItsAncestorsFromTheTopAndItsChildrenByName()
    {
        var top = await CreateUniverseAsync("Marvel", null);
        var middle = await CreateUniverseAsync("X-Men", top.Id);
        var universe = await CreateUniverseAsync("Wolverine", middle.Id);
        var weaponX = await CreateUniverseAsync("Weapon X", universe.Id);
        var origins = await CreateUniverseAsync("Origins", universe.Id);

        var detail = await GetAsync<UniverseDetail>($"/catalog/universes/{universe.Id}");

        Assert.Equal(
            [new UniverseListItem(top.Id, "Marvel", null, null), new UniverseListItem(middle.Id, "X-Men", top.Id, "Marvel")],
            detail.Ancestors);
        Assert.Equal(
            [
                new UniverseListItem(origins.Id, "Origins", universe.Id, "Wolverine"),
                new UniverseListItem(weaponX.Id, "Weapon X", universe.Id, "Wolverine"),
            ],
            detail.Children);
    }

    [Fact]
    public async Task Record_CarriesTheDatesOfItsCreationAndOfItsLastModification()
    {
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Polar {Guid.NewGuid():N}"));
        var created = await GetAsync<GenreDetail>($"/catalog/genres/{genre.Id}");
        (await _client.PutAsJsonAsync($"/admin/genres/{genre.Id}", new UpdateGenreRequest($"Policier {Guid.NewGuid():N}", genre.Version)))
            .EnsureSuccessStatusCode();

        var modified = await GetAsync<GenreDetail>($"/catalog/genres/{genre.Id}");

        Assert.Equal(created.CreatedAt, created.ModifiedAt);
        Assert.Equal(created.CreatedAt, modified.CreatedAt);
        Assert.True(modified.ModifiedAt > created.ModifiedAt);
    }

    [Theory]
    [InlineData("albums")]
    [InlineData("series")]
    [InlineData("editions")]
    [InlineData("authors")]
    [InlineData("publishers")]
    [InlineData("publisher-collections")]
    [InlineData("genres")]
    [InlineData("universes")]
    public async Task UnknownRecord_IsAFunctionalError(string resource)
    {
        var response = await _client.GetAsync($"/catalog/{resource}/{Guid.NewGuid()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private static SeriesContent SeriesContentOf(
        string title, Guid[]? genreIds = null, Guid[]? universeIds = null, SeriesEditionTemplate? template = null,
        ContributionContent[]? contributions = null) =>
        new(title, null, null, null, false, false, false, null, null, template ?? NoTemplate, genreIds ?? [], universeIds ?? [], contributions ?? []);

    private Task<AlbumForm> CreateAlbumAsync(
        string? title, Guid? seriesId, AlbumType type = AlbumType.Regular, bool specialIssue = false, int? volume = null, int? start = null,
        int? end = null, int? year = null, ContributionContent[]? contributions = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, type, specialIssue, volume, start, end, year, null, null, null, null, [], [], contributions ?? []));

    private Task<AuthorForm> CreateAuthorAsync(string lastName, string? firstName) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(lastName, firstName, null, null, null));

    private Task<PublisherForm> CreatePublisherAsync(string name) =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest($"{name} {Guid.NewGuid():N}", null));

    private Task<UniverseForm> CreateUniverseAsync(string name, Guid? parentId) =>
        PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest(name, null, parentId));

    private async Task<EditionForm> CreateOwnedEditionAsync(Guid albumId, Guid publisherId, int? publicationYear, string? isbn = null) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(
                    publisherId, null, publicationYear, isbn, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)));

    /// <returns>The edition targeted by the intent, not owned, and the intent.</returns>
    private async Task<(Guid Edition, Guid Intent)> CreateIntendedEditionAsync(Guid albumId, Guid publisherId, int? publicationYear)
    {
        var intents = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, publicationYear, null, null, null, null, null, null, null, true),
                await GetAlbumVersionAsync(albumId)));
        var intent = intents.Intents.Single(i => i.EditionId is not null);
        return (intent.EditionId!.Value, intent.Id);
    }

    /// <summary>A record carried by another, created after it and untouched since.</summary>
    private static void AssertCreatedAfter(DateTimeOffset carrierCreatedAt, PurchaseIntentItem intent) =>
        AssertCreatedAfter(carrierCreatedAt, (intent.CreatedAt, intent.ModifiedAt));

    private static void AssertCreatedAfter(DateTimeOffset carrierCreatedAt, (DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt) dates)
    {
        Assert.True(dates.CreatedAt > carrierCreatedAt);
        Assert.Equal(dates.CreatedAt, dates.ModifiedAt);
    }

    /// <returns>The visual uploaded, as the consultation presents it, without its dates.</returns>
    private async Task<EditionVisualItem> UploadAsync(Guid albumId, Guid editionId, VisualType type)
    {
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(TestImages.Encode(8, 8, SKEncodedImageFormat.Jpeg)), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(type.ToString()), UploadVisualFields.Type },
            { new StringContent((await GetAlbumVersionAsync(albumId)).ToString()), UploadVisualFields.AlbumVersion },
        };
        var response = await _client.PostAsync($"/admin/albums/{albumId}/editions/{editionId}/visuals", form);
        response.EnsureSuccessStatusCode();
        var visual = (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.Visuals.Single(v => v.Type == type);
        return new EditionVisualItem(visual.Id, visual.Type, visual.DisplayOrder, visual.OriginalPath, visual.DisplayPath, default, default);
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
