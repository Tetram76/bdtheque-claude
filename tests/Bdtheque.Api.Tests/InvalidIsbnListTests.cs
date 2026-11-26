using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The editions whose ISBN has an inconsistent check digit (<c>GET /admin/editions/invalid-isbns</c>,
/// fonctionnel/regles-metier/validation-de-l-isbn.md, visuel/listes/liste-par-liste.md): owned or targeted by an intent, the
/// ISBN the application supposes for each, the errors accepted shown on demand, and the correction and acceptance of an error.
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test narrows it to a publisher of its own.
/// </remarks>
public sealed class InvalidIsbnListTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateApiClient();
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task List_HoldsTheEditionsWhoseIsbnIsInconsistent_OwnedOrTargetedByAnIntent()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var album = await CreateAlbumAsync($"Barracuda {_marker}");
        var owned = await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-505-00865-3");
        await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-505-00865-1");
        await CreateOwnedEditionAsync(album.Id, publisher.Id, null);
        var intended = await CreateIntendedEditionAsync((await CreateAlbumAsync($"Blacksad {_marker}")).Id, publisher.Id, "2-8001-0123-4");

        var rows = (await ListAsync($"publisherId={publisher.Id}")).Editions.Items;

        Assert.Equal([owned.Id, intended], rows.Select(r => r.Edition.Id));
        Assert.Equal([true, false], rows.Select(r => r.IsOwned));
    }

    [Fact]
    public async Task Rows_CarryWhatTheListPresents()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}");
        var album = await CreateAlbumAsync($"Les Bijoux de la Castafiore {_marker}");
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-203-00120-7", AcquisitionMode.Gift, new DateOnly(2008, 5, 1));
        var version = await GetAlbumVersionAsync(album.Id);

        var row = Assert.Single((await ListAsync($"publisherId={publisher.Id}")).Editions.Items);

        Assert.Equal((album.Id, edition.Id, "978-2-203-00120-7"), (row.Album.Id, row.Edition.Id, row.Edition.Isbn));
        Assert.Equal("978-2-203-00120-6", row.CorrectedIsbn);
        Assert.Equal((true, (AcquisitionMode?)AcquisitionMode.Gift, 2008, false), (row.IsOwned, row.AcquisitionMode, row.AcquisitionYear, row.IsAccepted));
        Assert.Null(row.Cover);
        Assert.Equal(version, row.AlbumVersion);
        Assert.NotEqual(default, row.ModifiedAt);
    }

    [Fact]
    public async Task Rows_AnIsbnNoCheckCharacterMakesConsistent_HasNoCorrectedIsbn()
    {
        var publisher = await CreatePublisherAsync($"Glénat {_marker}");
        await CreateOwnedEditionAsync((await CreateAlbumAsync($"Les Passagers du vent {_marker}")).Id, publisher.Id, "2-7234-3451");

        var row = Assert.Single((await ListAsync($"publisherId={publisher.Id}")).Editions.Items);

        Assert.Null(row.CorrectedIsbn);
    }

    [Fact]
    public async Task State_TheErrorsAcceptedAreShownOnDemand_EachTellingItsState()
    {
        var publisher = await CreatePublisherAsync($"Delcourt {_marker}");
        var album = await CreateAlbumAsync($"De cape et de crocs {_marker}");
        var toCorrect = await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-7560-1234-0");
        var accepted = await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-7560-1234-1");
        (await AcceptAsync(album.Id, accepted.Id)).EnsureSuccessStatusCode();

        var byDefault = await ListAsync($"publisherId={publisher.Id}");
        var all = await ListAsync($"publisherId={publisher.Id}&state=All");

        Assert.Equal([toCorrect.Id], byDefault.Editions.Items.Select(r => r.Edition.Id));
        Assert.Equal([accepted.Id], (await ListAsync($"publisherId={publisher.Id}&state=Accepted")).Editions.Items.Select(r => r.Edition.Id));
        Assert.Equal([(toCorrect.Id, false), (accepted.Id, true)], all.Editions.Items.Select(r => (r.Edition.Id, r.IsAccepted)));
        Assert.Equal((0, 1), (byDefault.AcceptedCount, all.AcceptedCount));
    }

    [Fact]
    public async Task List_FollowsTheSearchAndTheFiltersOfTheListOfTheEditions()
    {
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}");
        var series = await CreateSeriesAsync($"Spirou {_marker}");
        var album = await CreateAlbumAsync(null, series.Id, 1);
        var digits = Random.Shared.NextInt64(100_000_000, 1_000_000_000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        // A check digit that disagrees with the ISBN, whatever its digits.
        var isbn = $"{digits}{(IsbnChecksumValidator.Corrected($"{digits}0")![^1] == '0' ? '1' : '0')}";
        var found = await CreateOwnedEditionAsync(album.Id, publisher.Id, isbn);
        await CreateOwnedEditionAsync((await CreateAlbumAsync($"Sans série {_marker}")).Id, publisher.Id, "978-2-505-00865-3");

        Assert.Equal([found.Id], await IdsAsync($"q={digits}"));
        Assert.Equal([found.Id], await IdsAsync($"seriesId={series.Id}"));
        Assert.Equal([found.Id], await IdsAsync($"publisherId={publisher.Id}&albumId={album.Id}"));
        Assert.Equal(new SeriesListItem(series.Id, $"Spirou {_marker}"), (await ListAsync($"seriesId={series.Id}")).Filters.Series);
    }

    [Fact]
    public async Task List_IsInTheOrderOfTheAlbums_AndPaged()
    {
        var publisher = await CreatePublisherAsync($"Lombard {_marker}");
        var second = await CreateOwnedEditionAsync((await CreateAlbumAsync($"Zorglub {_marker}")).Id, publisher.Id, "978-2-505-00865-3");
        var first = await CreateOwnedEditionAsync((await CreateAlbumAsync($"Le Lotus bleu {_marker}")).Id, publisher.Id, "978-2-203-00120-7");

        var page = (await ListAsync($"publisherId={publisher.Id}&pageSize=1&page=2")).Editions;

        Assert.Equal([first.Id, second.Id], await IdsAsync($"publisherId={publisher.Id}"));
        Assert.Equal([second.Id], page.Items.Select(r => r.Edition.Id));
        Assert.Equal((2, 1, 2), (page.Number, page.Size, page.TotalCount));
    }

    [Fact]
    public async Task Correct_PutsTheCorrectedIsbnInPlaceOfTheIsbnEntered()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var album = await CreateAlbumAsync($"Barracuda {_marker}");
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-505-00865-3");

        (await CorrectAsync(album.Id, edition.Id)).EnsureSuccessStatusCode();

        Assert.Equal("978-2-505-00865-1", (await GetEditionAsync(album.Id, edition.Id)).Content.Isbn);
        Assert.Empty((await ListAsync($"publisherId={publisher.Id}&state=All")).Editions.Items);
    }

    [Fact]
    public async Task Correct_AnIsbnNoCheckCharacterMakesConsistent_IsABusinessError()
    {
        var publisher = await CreatePublisherAsync($"Glénat {_marker}");
        var album = await CreateAlbumAsync($"Les Passagers du vent {_marker}");
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, "2-7234-3451");

        await ProblemAssert.IsBusinessProblemAsync(await CorrectAsync(album.Id, edition.Id), DomainRules.EditionIsbnNotCorrectable);
    }

    [Fact]
    public async Task CorrectOrAccept_AnIsbnCorrectedSince_IsABusinessError()
    {
        // The version of the album is that of the list, read again: only the ISBN tells nothing is left to correct.
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var album = await CreateAlbumAsync($"Barracuda {_marker}");
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-505-00865-1");

        await ProblemAssert.IsBusinessProblemAsync(await CorrectAsync(album.Id, edition.Id), DomainRules.EditionIsbnConsistent);
        await ProblemAssert.IsBusinessProblemAsync(await AcceptAsync(album.Id, edition.Id), DomainRules.EditionIsbnConsistent);
    }

    [Fact]
    public async Task Actions_UnderAnOutdatedVersionOfTheAlbum_AreAFunctionalError()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var album = await CreateAlbumAsync($"Barracuda {_marker}");
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-505-00865-3");
        var outdated = await GetAlbumVersionAsync(album.Id);
        (await AcceptAsync(album.Id, edition.Id)).EnsureSuccessStatusCode();

        await ProblemAssert.IsProblemAsync(await CorrectAsync(album.Id, edition.Id, outdated), HttpStatusCode.Conflict, ProblemTypes.Functional);
        await ProblemAssert.IsProblemAsync(await RevokeAsync(album.Id, edition.Id, outdated), HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Revoke_MakesTheErrorOneToCorrectAgain()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var album = await CreateAlbumAsync($"Barracuda {_marker}");
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-505-00865-3");
        (await AcceptAsync(album.Id, edition.Id)).EnsureSuccessStatusCode();

        (await RevokeAsync(album.Id, edition.Id)).EnsureSuccessStatusCode();

        Assert.False(Assert.Single((await ListAsync($"publisherId={publisher.Id}")).Editions.Items).IsAccepted);
    }

    [Fact]
    public async Task Checks_DoNotCountTheErrorsAccepted()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var album = await CreateAlbumAsync($"Barracuda {_marker}");
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, "978-2-505-00865-3");
        var before = (await GetAsync<AdminChecks>("/admin/checks")).InvalidIsbnCount;

        (await AcceptAsync(album.Id, edition.Id)).EnsureSuccessStatusCode();

        Assert.Equal(before - 1, (await GetAsync<AdminChecks>("/admin/checks")).InvalidIsbnCount);
    }

    private async Task<InvalidIsbnList> ListAsync(string query) => await GetAsync<InvalidIsbnList>($"/admin/editions/invalid-isbns?{query}");

    private async Task<IReadOnlyList<Guid>> IdsAsync(string query) => [.. (await ListAsync(query)).Editions.Items.Select(r => r.Edition.Id)];

    private async Task<HttpResponseMessage> CorrectAsync(Guid albumId, Guid editionId, uint? version = null) =>
        await _client.PostAsJsonAsync(
            $"/admin/albums/{albumId}/editions/{editionId}/isbn-correction", new IsbnActionRequest(version ?? await GetAlbumVersionAsync(albumId)));

    private async Task<HttpResponseMessage> AcceptAsync(Guid albumId, Guid editionId) =>
        await _client.PostAsJsonAsync(
            $"/admin/albums/{albumId}/editions/{editionId}/isbn-acceptance", new IsbnActionRequest(await GetAlbumVersionAsync(albumId)));

    private async Task<HttpResponseMessage> RevokeAsync(Guid albumId, Guid editionId, uint? version = null) =>
        await _client.DeleteAsync(
            $"/admin/albums/{albumId}/editions/{editionId}/isbn-acceptance?version={version ?? await GetAlbumVersionAsync(albumId)}");

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<EditionForm> GetEditionAsync(Guid albumId, Guid editionId) => GetAsync<EditionForm>($"/admin/albums/{albumId}/editions/{editionId}");

    private Task<AlbumForm> CreateAlbumAsync(string? title, Guid? seriesId = null, int? volume = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volume, null, null, null, null, null, null, null, [], [], []));

    private Task<SeriesForm> CreateSeriesAsync(string title) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, null, null, false, false, false, null, null, new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null),
            [], [], []));

    private Task<PublisherForm> CreatePublisherAsync(string name) => PostAsync<PublisherForm>("/admin/publishers", new PublisherContent(name, null, []));

    private async Task<EditionForm> CreateOwnedEditionAsync(
        Guid albumId, Guid publisherId, string? isbn, AcquisitionMode mode = AcquisitionMode.Purchase, DateOnly? acquisitionDate = null) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(publisherId, null, null, isbn, null, null, null, null, null, null, false, true, null,
                    mode, false, acquisitionDate, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)));

    // The identifier of the edition the new intent targets.
    private async Task<Guid> CreateIntendedEditionAsync(Guid albumId, Guid publisherId, string isbn)
    {
        var form = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, null, isbn, null, null, null, null, null, null, true),
                await GetAlbumVersionAsync(albumId)));
        return Assert.Single(form.Intents).EditionId!.Value;
    }

    private async Task<uint> GetAlbumVersionAsync(Guid albumId) => (await GetAsync<AlbumForm>($"/admin/albums/{albumId}")).Version;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
