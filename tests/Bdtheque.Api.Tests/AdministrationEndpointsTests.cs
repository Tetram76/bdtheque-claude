using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Api.Tests;

/// <summary>
/// What the home page of the administration presents of the whole base (visuel/administration/contenu.md): the number of
/// records of each type, with its detail, the checks the <c>.speckit/</c> defines, and the latest records written.
/// </summary>
public sealed class AdministrationEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateApiClient();

    [Fact]
    public async Task RecordCounts_CountEachTypeOfRecord_WithItsDetail()
    {
        var before = (await _client.GetFromJsonAsync<RecordCounts>("/admin/record-counts"))!;
        var publisher = await PostAsync<CreatePublisherRequest, PublisherForm>("/admin/publishers", new("Dupuis", null));
        await PostAsync<CreatePublisherCollectionRequest, PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new("Repérages", publisher.Version));
        await PostAsync<CreatePublisherRequest, PublisherForm>("/admin/publishers", new("Casterman", null));
        await PostAsync<SeriesContent, SeriesForm>("/admin/series", Series("Spirou", SeriesStatus.InProgress));
        await PostAsync<SeriesContent, SeriesForm>("/admin/series", Series("Gaston", null));
        var owned = await PostAsync<AlbumContent, AlbumForm>("/admin/albums", Album("Le Nid des Marsupilamis"));
        await PostAsync<CreateEditionRequest, EditionForm>($"/admin/albums/{owned.Id}/editions", new(Edition(publisher.Id, null), owned.Version));
        var wanted = await PostAsync<AlbumContent, AlbumForm>("/admin/albums", Album("Le Prisonnier du Bouddha"));
        await PostAsync<CreatePurchaseIntentRequest, PurchaseIntentsForm>($"/admin/albums/{wanted.Id}/purchase-intents", new(null, wanted.Version));
        // An edition targeted by an intent is one of the intents, never one of the editions
        // (fonctionnel/regles-metier/intention-d-achat.md).
        var targeted = await PostAsync<AlbumContent, AlbumForm>("/admin/albums", Album("Les Voleurs de Marsupilamis"));
        await PostAsync<CreatePurchaseIntentRequest, PurchaseIntentsForm>($"/admin/albums/{targeted.Id}/purchase-intents",
            new(new PurchaseIntentEditionContent(publisher.Id, null, null, null, null, null, null, null, null, null, true), targeted.Version));
        await PostAsync<AlbumContent, AlbumForm>("/admin/albums", Album("Z comme Zorglub"));
        await PostAsync<CreateAuthorRequest, AuthorForm>("/admin/authors", new("Franquin", "André", null, null, null));
        await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new("Humour"));
        var universe = await PostAsync<CreateUniverseRequest, UniverseForm>("/admin/universes", new("Spirouverse", null, null));
        await PostAsync<CreateUniverseRequest, UniverseForm>("/admin/universes", new("Champignac", null, universe.Id));

        var counts = await _client.GetFromJsonAsync<RecordCounts>("/admin/record-counts");

        // What this test added to what the other tests of the class have written.
        Assert.Equal(
            new RecordCounts(
                before.Albums + 4, before.AlbumsOutsideCollection + 3, before.Editions + 1, before.Series + 2,
                before.SeriesWithoutStatus + 1, before.PurchaseIntents + 2, before.Authors + 1, before.Publishers + 2,
                before.PublisherCollections + 1, before.Genres + 1, before.Universes + 2, before.SubUniverses + 1),
            counts);
    }

    [Fact]
    public async Task Checks_CountTheIsbnWhoseCheckDigitDisagrees()
    {
        var before = (await _client.GetFromJsonAsync<AdminChecks>("/admin/checks"))!.InvalidIsbnCount;
        var publisher = await PostAsync<CreatePublisherRequest, PublisherForm>("/admin/publishers", new("Glénat", null));
        foreach (var isbn in new[] { "978-2-7234-4934-2", "978-2-7234-4934-1", "2723449343", null })
        {
            var album = await PostAsync<AlbumContent, AlbumForm>("/admin/albums", Album($"Album {isbn}"));
            await PostAsync<CreateEditionRequest, EditionForm>($"/admin/albums/{album.Id}/editions", new(Edition(publisher.Id, isbn), album.Version));
        }

        var checks = await _client.GetFromJsonAsync<AdminChecks>("/admin/checks");

        // Only 978-2-7234-4934-1: 2723449343 is the valid ISBN-10 of the same book, and an edition without ISBN has nothing to check.
        Assert.Equal(before + 1, checks!.InvalidIsbnCount);
    }

    [Fact]
    public async Task LatestRecords_EachOnceAtItsLatestWrite_LeavingTheDeletedOnesOut()
    {
        var kept = await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new("Western"));
        var deleted = await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new("Polar"));
        var modified = await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new("Aventure"));
        (await _client.PutAsJsonAsync($"/admin/genres/{modified.Id}", new UpdateGenreRequest("Aventures", modified.Version))).EnsureSuccessStatusCode();
        await DeleteAsync($"/admin/genres/{deleted.Id}", deleted.Version);

        var latest = (await _client.GetFromJsonAsync<IReadOnlyList<ChangeLogEntryItem>>("/admin/change-log/latest-records?count=2"))!;

        Assert.Equal([(modified.Id, ChangeLogOperation.Modification), (kept.Id, ChangeLogOperation.Creation)],
            latest.Select(e => (e.RecordId, e.Operation)));
        Assert.Equal(new NameLabel("Aventures"), latest[0].Label);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task LatestRecords_CountOutOfBounds_IsATechnicalError(int count)
    {
        var response = await _client.GetAsync($"/admin/change-log/latest-records?count={count}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request)
    {
        var response = await _client.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    // A deletion confirms the impact it was shown, by its fingerprint.
    private async Task DeleteAsync(string path, uint version)
    {
        var impact = await _client.GetFromJsonAsync<Contracts.Deletion.DeletionImpact>($"{path}/deletion-impact");
        (await _client.DeleteAsync($"{path}?version={version}&fingerprint={Uri.EscapeDataString(impact!.Fingerprint)}")).EnsureSuccessStatusCode();
    }

    private static SeriesContent Series(string title, SeriesStatus? status) =>
        new(title, null, status, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], []);

    private static AlbumContent Album(string title) =>
        new(title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []);

    private static EditionContent Edition(Guid publisher, string? isbn) =>
        new(publisher, null, 2001, isbn, null, null, null, null, null, null, false, true, null,
            AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null);
}
