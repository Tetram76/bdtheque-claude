using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The public list of the purchase intents (<c>GET /catalog/purchase-intents</c>, fonctionnel/structure-de-l-application.md):
/// what each row presents, its search on the title of the album or of its series, the navigation by initial of its albums and
/// its order, that of the albums. It has no filter of the possession (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test narrows it by searching a marker of its own,
/// which the titles of its albums or series carry.
/// </remarks>
public sealed class PurchaseIntentCatalogListTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public PurchaseIntentCatalogListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Row_OfAnIntentOnTheAlbum_CarriesTheAlbumAloneItsPossessionAndTheDateOfTheIntent()
    {
        var album = await CreateAlbumAsync($"Blacksad {_marker}");
        var intent = await CreateAlbumIntentAsync(album.Id);

        var row = Assert.Single((await ListAsync($"q={_marker}")).Items);

        // Without an owned edition, the album is wished for (fonctionnel/regles-metier/appartenance-a-la-collection.md#états-dun-album).
        Assert.Equal((intent, album.Id, AlbumCollectionState.PurchaseIntent), (row.Id, row.Album.Id, row.State));
        Assert.Null(row.Edition);
        Assert.Null(row.PublisherWebsite);
        // An album without an edition has no cover: the generic visual stands for it.
        Assert.Null(row.Cover);
        Assert.NotEqual(default, row.CreatedAt);
    }

    [Fact]
    public async Task Row_OfAnIntentOnAnEdition_CarriesTheEditionTheSiteOfItsPublisherAndTheCoverAmongAllTheEditions()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}", "https://www.example.org/dargaud");
        var album = await CreateAlbumAsync($"Fort Navajo {_marker}");
        var owned = await CreateOwnedEditionAsync(album.Id, publisher.Id, 2010);
        await UploadCoverAsync(album.Id, owned);
        // Older, but only targeted by the intent: the list, where everything can be consulted, presents every edition
        // (fonctionnel/regles-metier/couverture-d-un-album.md).
        var targeted = await CreateIntendedEditionAsync(album.Id, publisher.Id, 1985);
        var targetedCover = await UploadCoverAsync(album.Id, targeted);

        var row = Assert.Single((await ListAsync($"q={_marker}")).Items);

        Assert.Equal(AlbumCollectionState.InCollection, row.State);
        Assert.Equal(new EditionSummary(targeted, publisher.Id, $"Dargaud {_marker}", null, null, 1985, null), row.Edition);
        Assert.Equal("https://www.example.org/dargaud", row.PublisherWebsite);
        Assert.Equal(targetedCover, row.Cover);
    }

    [Fact]
    public async Task Search_FindsTheTitleOfTheAlbumOrOfItsSeries()
    {
        var series = await CreateSeriesAsync($"Le Château des étoiles {_marker}");
        var inSeries = await CreateAlbumIntentAsync((await CreateAlbumAsync(null, series.Id, 4)).Id);
        var titled = await CreateAlbumIntentAsync((await CreateAlbumAsync($"Les Vieux Fourneaux {_marker}")).Id);

        Assert.Equal([inSeries], await IdsAsync($"q={Uri.EscapeDataString($"Château des étoiles {_marker}")}"));
        Assert.Equal([titled], await IdsAsync($"q={Uri.EscapeDataString($"vieux fourneaux {_marker}")}"));
    }

    // An edition a purchase intent targets is found by its ISBN, as any element of the base, and presented as that intent
    // (fonctionnel/regles-metier/perimetre-de-la-consultation.md), its album still answering the other criteria.
    [Fact]
    public async Task Search_FindsTheIsbnOfTheTargetedEdition_WhateverTheSeparators()
    {
        var isbn = $"978{Random.Shared.NextInt64(1_000_000_000, 9_999_999_999)}";
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}", null);
        var album = await CreateAlbumAsync($"Spirou {_marker}");
        await CreateIntendedEditionAsync(album.Id, publisher.Id, 2001, isbn);
        var intent = Assert.Single(await IdsAsync($"q={_marker}"));

        Assert.Equal([intent], await IdsAsync($"q={Uri.EscapeDataString($"{isbn[..3]}-{isbn[3..]}")}"));
        Assert.Equal([intent], await IdsAsync($"q={isbn}&entry=S"));
        Assert.Empty(await IdsAsync($"q={isbn}&entry=A"));
    }

    [Fact]
    public async Task List_FollowsTheNavigationByInitialOfTheAlbums()
    {
        // An intent is filed under the entry of its album, an album without a title of its own under that of its series
        // (choix-implementation/api/navigation-par-initiale-mise-en-oeuvre.md).
        var untitled = await CreateAlbumIntentAsync((await CreateAlbumAsync(null, (await CreateSeriesAsync($"Blacksad {_marker}")).Id, 1)).Id);
        var lotus = await CreateAlbumIntentAsync((await CreateAlbumAsync($"Le Lotus bleu {_marker}")).Id);

        Assert.Equal([untitled], await IdsAsync($"q={_marker}&entry=B"));
        Assert.Equal([lotus], await IdsAsync($"q={_marker}&entry=L"));
        Assert.Empty(await IdsAsync($"q={_marker}&entry=%40"));
    }

    [Fact]
    public async Task List_PaginatesTheIntentsInTheOrderOfTheAlbums()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}", null);
        var series = await CreateSeriesAsync($"Les Aventures de Tintin {_marker}");
        // By the sort key of the album, or that of its series without a title of its own:
        // "Aventures de Tintin … [Les]" < "Lotus bleu … [Le]" < "Zorglub …"; the intents of an album in the order they were set.
        var zorglub = await CreateAlbumIntentAsync((await CreateAlbumAsync($"Zorglub {_marker}")).Id);
        var lotus = await CreateAlbumAsync($"Le Lotus bleu {_marker}", series.Id, 5);
        await CreateIntendedEditionAsync(lotus.Id, publisher.Id, 2024);
        await CreateIntendedEditionAsync(lotus.Id, publisher.Id, 1946);
        var untitled = await CreateAlbumIntentAsync((await CreateAlbumAsync(null, series.Id, 2)).Id);
        var lotusIntents = (await GetAsync<PurchaseIntentsForm>($"/admin/albums/{lotus.Id}/purchase-intents")).Intents.Select(i => i.Id);

        var first = await ListAsync($"q={_marker}&page=1&pageSize=2");
        var second = await ListAsync($"q={_marker}&page=2&pageSize=2");

        Assert.Equal((1, 2, 4), (first.Number, first.Size, first.TotalCount));
        Assert.Equal((2, 2, 4), (second.Number, second.Size, second.TotalCount));
        Assert.Equal([untitled, .. lotusIntents, zorglub], first.Items.Concat(second.Items).Select(r => r.Id));
    }

    [Fact]
    public async Task Filters_OfTheAlbums_KeepTheIntentsWhoseAlbumAnswersThem_AndAreNamedAsTheListsNameThem()
    {
        // The cross filters of the list of the albums, applied to the album of each intent, the possession excepted
        // (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
        var series = await CreateSeriesAsync($"Spirou {_marker}");
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Humour {_marker}"));
        var inSeries = await CreateAlbumIntentAsync((await CreateAlbumAsync(null, series.Id, 1)).Id);
        var withGenre = await CreateAlbumIntentAsync((await PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            $"Gaston {_marker}", null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [genre.Id], [], []))).Id);

        var bySeries = await CatalogListAsync($"q={_marker}&seriesId={series.Id}");
        var byGenre = await CatalogListAsync($"q={_marker}&genreId={genre.Id}");

        Assert.Equal([inSeries], bySeries.Intents.Items.Select(r => r.Id));
        Assert.Equal(new SeriesListItem(series.Id, $"Spirou {_marker}"), bySeries.Filters.Series);
        Assert.Equal([withGenre], byGenre.Intents.Items.Select(r => r.Id));
        Assert.Equal(genre.Id, byGenre.Filters.Genre!.Id);
    }

    [Fact]
    public async Task Possession_IsNoFilterOfTheList()
    {
        var owned = await CreateAlbumAsync($"Possédé {_marker}");
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}", null);
        await CreateOwnedEditionAsync(owned.Id, publisher.Id, 2000);
        await CreateIntendedEditionAsync(owned.Id, publisher.Id, 2020);
        await CreateAlbumIntentAsync((await CreateAlbumAsync($"Visé {_marker}")).Id);

        // An address written by hand with a possession keeps every intent.
        Assert.Equal(2, (await ListAsync($"q={_marker}&state=PurchaseIntent")).TotalCount);
    }

    [Theory]
    [InlineData("page=0&pageSize=10")]
    [InlineData("page=1&pageSize=0")]
    [InlineData("page=1&pageSize=101")]
    [InlineData("entry=AB")]
    public async Task List_WithAPageOutOfBoundsOrAnEntryThatIsNotOneOfTheNavigation_IsATechnicalError(string query)
    {
        // The frontend builds the paging and the navigation, never the user.
        var response = await _client.GetAsync($"/catalog/purchase-intents?{query}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private async Task<Page<PurchaseIntentListItem>> ListAsync(string query) => (await CatalogListAsync(query)).Intents;

    private Task<PurchaseIntentCatalogList> CatalogListAsync(string query) => GetAsync<PurchaseIntentCatalogList>($"/catalog/purchase-intents?{query}");

    private async Task<IReadOnlyList<Guid>> IdsAsync(string query) => [.. (await ListAsync(query)).Items.Select(r => r.Id)];

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<AlbumForm> CreateAlbumAsync(string? title, Guid? seriesId = null, int? volume = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volume, null, null, null, null, null, null, null, [], [], []));

    private Task<SeriesForm> CreateSeriesAsync(string title) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, null, null, false, false, false, null, null, new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null),
            [], [], []));

    private Task<PublisherForm> CreatePublisherAsync(string name, string? website) =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent(name, website, []));

    private async Task<Guid> CreateOwnedEditionAsync(Guid albumId, Guid publisherId, int? year) =>
        (await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(publisherId, null, year, null, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)))).Id;

    private async Task<Guid> CreateAlbumIntentAsync(Guid albumId)
    {
        var form = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents", new CreatePurchaseIntentRequest(null, await GetAlbumVersionAsync(albumId)));
        return Assert.Single(form.Intents).Id;
    }

    /// <returns>The edition targeted by the new intent, not owned.</returns>
    private async Task<Guid> CreateIntendedEditionAsync(Guid albumId, Guid publisherId, int? year, string? isbn = null)
    {
        var before = (await GetAsync<PurchaseIntentsForm>($"/admin/albums/{albumId}/purchase-intents")).Intents.Select(i => i.Id).ToHashSet();
        var form = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, year, isbn, null, null, null, null, null, null, true),
                await GetAlbumVersionAsync(albumId)));
        return form.Intents.Single(i => !before.Contains(i.Id)).EditionId!.Value;
    }

    /// <returns>The visual uploaded as a cover of the edition.</returns>
    private async Task<AlbumCover> UploadCoverAsync(Guid albumId, Guid editionId)
    {
        var uri = $"/admin/albums/{albumId}/editions/{editionId}/visuals";
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(TestImages.Encode(8, 8, SKEncodedImageFormat.Jpeg)), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent((await GetAlbumVersionAsync(albumId)).ToString()), UploadVisualFields.AlbumVersion },
        };
        var response = await _client.PostAsync(uri, form);
        response.EnsureSuccessStatusCode();
        return new AlbumCover(Assert.Single((await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.Visuals).Id, TestImages.UploadedPalette);
    }

    private async Task<uint> GetAlbumVersionAsync(Guid albumId) => (await GetAsync<AlbumForm>($"/admin/albums/{albumId}")).Version;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
