using System.Net;
using System.Net.Http.Json;
using Bdtheque.Api.Visuals;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The visuals saved with their edition (choix-implementation/domaine/visuels-stockage-et-traitement.md, Zone d'attente): a file
/// uploaded as a pending visual, kept 24 hours, which the creation or the modification of an edition, or a purchase intent on a
/// new one, makes one of its visuals, in the order and with the types of its entry, the visuals it leaves out being removed with
/// their files; the pending visuals no longer kept and their files removed by the reconciliation.
/// </summary>
public sealed class PendingVisualEndpointsTests : IClassFixture<ApiWebApplicationFactory>, IDisposable
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PendingVisualEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    public void Dispose() => _factory.Clock.UtcNow = null;

    [Fact]
    public async Task Limits_GiveTheHeaviestFileAndHowLongAPendingVisualIsKept()
    {
        // The limits are api's: its clients announce them without knowing them themselves.
        var limits = await _client.GetFromJsonAsync<VisualLimits>("/admin/visuals/limits");

        Assert.Equal(new VisualLimits(ApiWebApplicationFactory.MaxVisualFileSizeBytes, TimeSpan.FromHours(24)), limits);
    }

    [Fact]
    public async Task Upload_WritesItsFilesAndKeepsIt24Hours_ItsDisplayServedToTheAdministration()
    {
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

        var pending = await UploadPendingAsync(SKColors.SteelBlue);

        Assert.Equal(new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero), pending.ExpiresAt);
        var mediaReference = await MediaReferenceOfPendingAsync(pending.Id);
        Assert.True(File.Exists(VolumePath(VisualStorage.OriginalPath(mediaReference))));
        using var display = await _client.GetAsync($"/admin/visuals/pending/{pending.Id}/display");
        display.EnsureSuccessStatusCode();
        Assert.Equal("image/webp", display.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Upload_NotAnImage_IsRefusedAtOnce_AndWritesNothing()
    {
        var files = VolumeFiles();

        using var response = await PostPendingAsync("pas une image"u8.ToArray());

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionVisualFileNotSupportedImage);
        Assert.Equal(files, VolumeFiles());
    }

    [Fact]
    public async Task Display_OfAPendingVisualNoLongerKept_IsNotFound()
    {
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
        var pending = await UploadPendingAsync(SKColors.SteelBlue);
        _factory.Clock.UtcNow = pending.ExpiresAt;

        using var response = await _client.GetAsync($"/admin/visuals/pending/{pending.Id}/display");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Original_OfAPendingVisual_IsServedAsUploaded_WhileItIsKept()
    {
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
        var pending = await UploadPendingAsync(SKColors.SteelBlue);

        using var original = await _client.GetAsync($"/admin/visuals/pending/{pending.Id}/original");
        original.EnsureSuccessStatusCode();
        Assert.Equal("image/jpeg", original.Content.Headers.ContentType!.MediaType);
        Assert.Equal((true, TimeSpan.FromDays(1)), (original.Headers.CacheControl!.Private, original.Headers.CacheControl.MaxAge));

        _factory.Clock.UtcNow = pending.ExpiresAt;
        using var expired = await _client.GetAsync($"/admin/visuals/pending/{pending.Id}/original");
        await ProblemAssert.IsProblemAsync(expired, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task CreateEdition_WithPendingVisuals_MakesThemItsVisuals_InTheOrderOfItsEntry()
    {
        var plate = await UploadPendingAsync(SKColors.Wheat);
        var cover = await UploadPendingAsync(SKColors.SteelBlue);
        var album = await CreateAlbumAsync();

        var edition = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            Content(await CreatePublisherAsync()), album.Version,
            [new(null, plate.Id, VisualType.Plate), new(null, cover.Id, VisualType.Cover)]));

        var visuals = await GetVisualsAsync(edition);
        Assert.Equal([VisualType.Cover, VisualType.Plate], visuals.Visuals.Select(v => v.Type));
        Assert.Equal(0, await PendingCountAsync(plate.Id, cover.Id));
    }

    [Fact]
    public async Task UpdateEdition_WithItsVisuals_KeepsAddsRemovesAndOrdersThem_TheRemovedFilesGone()
    {
        var album = await CreateAlbumAsync();
        var created = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            Content(await CreatePublisherAsync()), album.Version,
            [new(null, (await UploadPendingAsync(SKColors.SteelBlue)).Id, VisualType.Cover),
                new(null, (await UploadPendingAsync(SKColors.Wheat)).Id, VisualType.Plate)]));
        var (cover, plate) = ((await GetVisualsAsync(created)).Visuals[0], (await GetVisualsAsync(created)).Visuals[1]);
        var removedFile = VolumePath(VisualStorage.OriginalPath(await MediaReferenceOfVisualAsync(cover.Id)));
        var added = await UploadPendingAsync(SKColors.Bisque);

        await PutAsync($"/admin/albums/{album.Id}/editions/{created.Id}", new UpdateEditionRequest(
            created.Content, created.AlbumVersion, [new(null, added.Id, VisualType.Plate), new(plate.Id, null, VisualType.Plate)]));

        var visuals = (await GetVisualsAsync(created)).Visuals;
        Assert.Equal([(VisualType.Plate, 0), (VisualType.Plate, 1)], visuals.Select(v => (v.Type, v.DisplayOrder)));
        Assert.Equal(plate.Id, visuals[1].Id);
        Assert.False(File.Exists(removedFile));
        Assert.Equal(0, await PendingCountAsync(added.Id));
    }

    [Fact]
    public async Task UpdateEdition_WithoutItsVisuals_LeavesThemAsTheyAre()
    {
        var album = await CreateAlbumAsync();
        var created = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            Content(await CreatePublisherAsync()), album.Version, [new(null, (await UploadPendingAsync(SKColors.SteelBlue)).Id, VisualType.Cover)]));

        await PutAsync($"/admin/albums/{album.Id}/editions/{created.Id}", new UpdateEditionRequest(created.Content with { PageCount = 48 }, created.AlbumVersion));

        Assert.Single((await GetVisualsAsync(created)).Visuals);
    }

    [Fact]
    public async Task UpdateEdition_WithAPendingVisualNoLongerKept_IsABusinessError_AndChangesNothing()
    {
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
        var album = await CreateAlbumAsync();
        var created = await PostAsync<EditionForm>(
            $"/admin/albums/{album.Id}/editions", new CreateEditionRequest(Content(await CreatePublisherAsync()), album.Version));
        var pending = await UploadPendingAsync(SKColors.SteelBlue);
        _factory.Clock.UtcNow = pending.ExpiresAt;

        using var response = await _client.PutAsJsonAsync($"/admin/albums/{album.Id}/editions/{created.Id}", new UpdateEditionRequest(
            created.Content with { PageCount = 48 }, created.AlbumVersion, [new(null, pending.Id, VisualType.Cover)]));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionVisualPendingExpired);
        Assert.Empty((await GetVisualsAsync(created)).Visuals);
        Assert.Null((await _client.GetFromJsonAsync<EditionForm>($"/admin/albums/{album.Id}/editions/{created.Id}"))!.Content.PageCount);
    }

    [Fact]
    public async Task CreatePurchaseIntent_OnANewEditionWithPendingVisuals_MakesThemItsVisuals()
    {
        var album = await CreateAlbumAsync();
        var pending = await UploadPendingAsync(SKColors.SteelBlue);

        var intents = await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent((await CreatePublisherAsync()), null, 2018, null, null, null, null, null, null, null, true),
            album.Version, [new(null, pending.Id, VisualType.Cover)]));

        var editionId = Assert.Single(intents.Intents).EditionId!.Value;
        Assert.Single((await _client.GetFromJsonAsync<EditionVisualsForm>($"/admin/albums/{album.Id}/editions/{editionId}/visuals"))!.Visuals);
    }

    [Fact]
    public async Task Reconciliation_RemovesThePendingVisualsNoLongerKept_WithTheirFiles_AndKeepsTheOthers()
    {
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
        var abandoned = await UploadPendingAsync(SKColors.SteelBlue);
        var abandonedFile = VolumePath(VisualStorage.OriginalPath(await MediaReferenceOfPendingAsync(abandoned.Id)));
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);
        var kept = await UploadPendingAsync(SKColors.Wheat);
        var keptFile = VolumePath(VisualStorage.OriginalPath(await MediaReferenceOfPendingAsync(kept.Id)));
        // The files written long enough ago for the reconciliation to consider them.
        File.SetLastWriteTimeUtc(abandonedFile, new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(keptFile, new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc));
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

        await _factory.Services.GetRequiredService<VisualReconciliation>().ReconcileAsync(CancellationToken.None);

        Assert.Equal(0, await PendingCountAsync(abandoned.Id));
        Assert.False(File.Exists(abandonedFile));
        Assert.Equal(1, await PendingCountAsync(kept.Id));
        Assert.True(File.Exists(keptFile));
    }

    private Task<HttpResponseMessage> PostPendingAsync(byte[] content) =>
        _client.PostAsync("/admin/visuals/pending", new MultipartFormDataContent { { new ByteArrayContent(content), UploadVisualFields.File, "visual.jpg" } });

    private async Task<PendingVisualForm> UploadPendingAsync(SKColor color)
    {
        using var response = await PostPendingAsync(TestImages.EncodeHalves(40, 60, color, color));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PendingVisualForm>())!;
    }

    private async Task<int> PendingCountAsync(params Guid[] ids)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().PendingVisuals.CountAsync(v => ids.Contains(v.Id));
    }

    private async Task<string> MediaReferenceOfPendingAsync(Guid id)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().PendingVisuals
            .Where(v => v.Id == id).Select(v => v.MediaReference).SingleAsync();
    }

    private async Task<string> MediaReferenceOfVisualAsync(Guid id)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().EditionVisuals
            .Where(v => v.Id == id).Select(v => v.MediaReference).SingleAsync();
    }

    private string VolumePath(string relativePath) => Path.Combine(_factory.VisualsRoot, relativePath);

    private List<string> VolumeFiles() =>
        Directory.Exists(_factory.VisualsRoot)
            ? Directory.EnumerateFiles(_factory.VisualsRoot, "*", SearchOption.AllDirectories).Order().ToList()
            : [];

    private async Task<EditionVisualsForm> GetVisualsAsync(EditionForm edition) =>
        (await _client.GetFromJsonAsync<EditionVisualsForm>($"/admin/albums/{edition.AlbumId}/editions/{edition.Id}/visuals"))!;

    private static EditionContent Content(Guid publisher) =>
        new(publisher, null, null, null, null, null, null, null, null, null, false, true, null,
            AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null);

    private async Task<Guid> CreatePublisherAsync() =>
        (await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur {Guid.NewGuid():N}", null, []))).Id;

    private Task<AlbumForm> CreateAlbumAsync() =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            $"Album {Guid.NewGuid():N}", null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        using var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }

    private async Task PutAsync(string uri, object request)
    {
        using var response = await _client.PutAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
    }
}
