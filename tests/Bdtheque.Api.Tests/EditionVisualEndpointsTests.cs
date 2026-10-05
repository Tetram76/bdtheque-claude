using System.Net;
using System.Net.Http.Json;
using Bdtheque.Api.Visuals;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Administration of the visuals of an edition (<c>/admin/albums/{albumId}/editions/{editionId}/visuals</c>),
/// and the consistency between their rows and their files on the volume (choix-implementation.md §
/// Visuels : stockage et traitement), deletions of editions and albums included.
/// </summary>
public sealed class EditionVisualEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditionVisualEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Upload_StoresTheOriginalAndItsDisplayVersion()
    {
        var edition = await CreateEditionAsync();
        var jpeg = TestImages.Encode(1600, 2400, SKEncodedImageFormat.Jpeg);

        var response = await PostUploadAsync(edition, jpeg, VisualType.Cover, edition.AlbumVersion);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var form = (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!;
        Assert.Equal(edition.Id, form.EditionId);
        var visual = Assert.Single(form.Visuals);
        Assert.Equal((VisualType.Cover, 0), (visual.Type, visual.DisplayOrder));
        Assert.Matches("^originals/[0-9a-f]{32}\\.jpg$", visual.OriginalPath);
        Assert.Equal($"display/{Path.GetFileNameWithoutExtension(visual.OriginalPath)}.webp", visual.DisplayPath);
        Assert.Equal(jpeg, await File.ReadAllBytesAsync(VolumePath(visual.OriginalPath)));
        Assert.Equal(new SKSizeI(800, 1200), TestImages.SizeOf(await File.ReadAllBytesAsync(VolumePath(visual.DisplayPath))));
        Assert.NotEqual(edition.AlbumVersion, form.AlbumVersion);
        Assert.Equal(form.AlbumVersion, (await GetAlbumAsync(edition.AlbumId)).Version);
        Assert.Equal(form, await GetVisualsAsync(edition), VisualsFormComparer.Instance);
    }

    [Fact]
    public async Task Upload_PlacesTheVisualAfterThoseOfItsType()
    {
        var edition = await CreateEditionAsync();

        var plate = await UploadAsync(edition, VisualType.Plate);
        var cover = await UploadAsync(edition, VisualType.Cover);
        var form = await UploadAsync(edition, VisualType.Plate);

        Assert.Equal(
            [(VisualType.Cover, 0), (VisualType.Plate, 0), (VisualType.Plate, 1)],
            form.Visuals.Select(v => (v.Type, v.DisplayOrder)));
        Assert.Equal(cover.Visuals.Single(v => v.Type == VisualType.Cover).Id, form.Visuals[0].Id);
        Assert.Equal(plate.Visuals.Single().Id, form.Visuals[1].Id);
    }

    [Fact]
    public async Task Upload_NotAnImage_IsABusinessErrorAndWritesNothing()
    {
        var edition = await CreateEditionAsync();
        var files = VolumeFiles();

        var response = await PostUploadAsync(edition, "%PDF-1.7"u8.ToArray(), VisualType.Cover, edition.AlbumVersion);

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionVisualFileNotSupportedImage);
        Assert.Equal(files, VolumeFiles());
        Assert.Empty((await GetVisualsAsync(edition)).Visuals);
    }

    [Fact]
    public async Task Upload_HeavierThanTheMaximum_IsABusinessError()
    {
        var edition = await CreateEditionAsync();

        var response = await PostUploadAsync(
            edition, new byte[ApiWebApplicationFactory.MaxVisualFileSizeBytes + 1], VisualType.Cover, edition.AlbumVersion);

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionVisualFileTooLarge);
    }

    [Fact]
    public async Task Upload_FromAStaleAlbumVersion_IsAFunctionalErrorAndWritesNothing()
    {
        var edition = await CreateEditionAsync();
        await UploadAsync(edition, VisualType.Cover);
        var files = VolumeFiles();

        var response = await PostUploadAsync(edition, SmallJpeg(), VisualType.Plate, edition.AlbumVersion);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(files, VolumeFiles());
    }

    [Fact]
    public async Task Upload_ToTheEditionOfAnotherAlbum_IsAFunctionalError()
    {
        var edition = await CreateEditionAsync();
        var otherAlbum = await CreateAlbumAsync();

        var response = await PostUploadAsync(
            edition with { AlbumId = otherAlbum.Id }, SmallJpeg(), VisualType.Cover, otherAlbum.Version);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Upload_WhoseRowCannotBeSaved_DeletesItsFiles()
    {
        // The files are written just before the row: a failure of the row removes them at once. The
        // failure is produced by a trigger refusing any visual of this edition.
        var edition = await CreateEditionAsync();
        var files = VolumeFiles();
        await ExecuteSqlAsync(
            $"""
             CREATE FUNCTION refuse_visual_{edition.Id:N}() RETURNS trigger LANGUAGE plpgsql AS $$
             BEGIN RAISE EXCEPTION 'refused'; END $$;
             CREATE TRIGGER refuse_visual_{edition.Id:N} BEFORE INSERT ON "EditionVisuals" FOR EACH ROW
             WHEN (NEW."EditionId" = '{edition.Id}') EXECUTE FUNCTION refuse_visual_{edition.Id:N}();
             """);
        try
        {
            var response = await PostUploadAsync(edition, SmallJpeg(), VisualType.Cover, edition.AlbumVersion);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
            Assert.Equal(files, VolumeFiles());
        }
        finally
        {
            await ExecuteSqlAsync(
                $"""DROP TRIGGER refuse_visual_{edition.Id:N} ON "EditionVisuals"; DROP FUNCTION refuse_visual_{edition.Id:N}();""");
        }
    }

    [Fact]
    public async Task Upload_FailingAtCommit_LeavesItsFilesToTheReconciliation()
    {
        // Whether a failed commit committed is uncertain (e.g. connection lost while committing):
        // deleting the files could strip a saved visual of them. They are left to the reconciliation,
        // which decides from the database. The failure is produced by a deferred trigger, run at commit.
        var edition = await CreateEditionAsync();
        var files = VolumeFiles();
        await ExecuteSqlAsync(
            $"""
             CREATE FUNCTION refuse_at_commit_{edition.Id:N}() RETURNS trigger LANGUAGE plpgsql AS $$
             BEGIN RAISE EXCEPTION 'refused'; END $$;
             CREATE CONSTRAINT TRIGGER refuse_at_commit_{edition.Id:N} AFTER INSERT ON "EditionVisuals"
             DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
             WHEN (NEW."EditionId" = '{edition.Id}') EXECUTE FUNCTION refuse_at_commit_{edition.Id:N}();
             """);
        try
        {
            var response = await PostUploadAsync(edition, SmallJpeg(), VisualType.Cover, edition.AlbumVersion);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
            Assert.Equal(2, VolumeFiles().Except(files).Count());
            Assert.Empty((await GetVisualsAsync(edition)).Visuals);
        }
        finally
        {
            await ExecuteSqlAsync(
                $"""DROP TRIGGER refuse_at_commit_{edition.Id:N} ON "EditionVisuals"; DROP FUNCTION refuse_at_commit_{edition.Id:N}();""");
        }
    }

    [Fact]
    public async Task Arrange_SetsTheTypesAndTheOrderOfTheVisuals()
    {
        var edition = await CreateEditionAsync();
        await UploadAsync(edition, VisualType.Cover);
        await UploadAsync(edition, VisualType.Plate);
        var uploaded = await UploadAsync(edition, VisualType.Plate);
        var (cover, firstPlate, secondPlate) = (uploaded.Visuals[0], uploaded.Visuals[1], uploaded.Visuals[2]);

        // The second plate moves before the first, and the cover becomes a back cover.
        var arranged = await ArrangeAsync(
            edition,
            [new(secondPlate.Id, VisualType.Plate), new(cover.Id, VisualType.BackCover), new(firstPlate.Id, VisualType.Plate)],
            uploaded.AlbumVersion);

        Assert.Equal(
            [(secondPlate.Id, VisualType.Plate, 0), (firstPlate.Id, VisualType.Plate, 1), (cover.Id, VisualType.BackCover, 0)],
            arranged.Visuals.Select(v => (v.Id, v.Type, v.DisplayOrder)));
        Assert.NotEqual(uploaded.AlbumVersion, arranged.AlbumVersion);
        Assert.Equal(arranged, await GetVisualsAsync(edition), VisualsFormComparer.Instance);
    }

    [Fact]
    public async Task Arrange_FromAStaleAlbumVersion_IsAFunctionalError()
    {
        var edition = await CreateEditionAsync();
        var uploaded = await UploadAsync(edition, VisualType.Cover);

        var response = await _client.PutAsJsonAsync(
            VisualsUri(edition), new ArrangeVisualsRequest([new(uploaded.Visuals[0].Id, VisualType.Plate)], edition.AlbumVersion));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Get_TheVisualsOfTheEditionOfAnotherAlbum_IsAFunctionalError()
    {
        var edition = await CreateEditionAsync();
        var otherAlbum = await CreateAlbumAsync();

        var response = await _client.GetAsync(VisualsUri(edition with { AlbumId = otherAlbum.Id }));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_RemovesTheVisualAndItsFiles()
    {
        var edition = await CreateEditionAsync();
        var uploaded = await UploadAsync(edition, VisualType.Cover);
        var visual = uploaded.Visuals.Single();
        var visualUri = $"{VisualsUri(edition)}/{visual.Id}";
        var impact = (await _client.GetFromJsonAsync<DeletionImpact>($"{visualUri}/deletion-impact"))!;

        var response = await _client.DeleteAsync($"{visualUri}?version={uploaded.AlbumVersion}&fingerprint={impact.Fingerprint}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(impact.DeletedWith);
        Assert.Empty((await GetVisualsAsync(edition)).Visuals);
        AssertFilesDeleted(visual);
    }

    [Fact]
    public async Task DeletionImpact_OfTheVisualOfAnotherEdition_IsAFunctionalError()
    {
        var edition = await CreateEditionAsync();
        var visual = (await UploadAsync(edition, VisualType.Cover)).Visuals.Single();
        var otherEdition = await CreateEditionAsync(edition.AlbumId);

        var response = await _client.GetAsync($"{VisualsUri(otherEdition)}/{visual.Id}/deletion-impact");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task DeleteEdition_RemovesTheFilesOfItsVisuals()
    {
        var edition = await CreateEditionAsync();
        var uploaded = await UploadAsync(edition, VisualType.Cover);
        var editionUri = $"/admin/albums/{edition.AlbumId}/editions/{edition.Id}";
        var impact = (await _client.GetFromJsonAsync<DeletionImpact>($"{editionUri}/deletion-impact"))!;

        var response = await _client.DeleteAsync($"{editionUri}?version={uploaded.AlbumVersion}&fingerprint={impact.Fingerprint}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        AssertFilesDeleted(uploaded.Visuals.Single());
    }

    [Fact]
    public async Task DeleteAlbum_RemovesTheFilesOfTheVisualsOfItsEditions()
    {
        var edition = await CreateEditionAsync();
        await UploadAsync(edition, VisualType.Cover);
        var otherEdition = await CreateEditionAsync(edition.AlbumId);
        var uploaded = await UploadAsync(otherEdition, VisualType.Plate);
        var visuals = (await GetVisualsAsync(edition)).Visuals.Concat(uploaded.Visuals).ToList();
        var albumUri = $"/admin/albums/{edition.AlbumId}";
        var impact = (await _client.GetFromJsonAsync<DeletionImpact>($"{albumUri}/deletion-impact"))!;

        var response = await _client.DeleteAsync($"{albumUri}?version={uploaded.AlbumVersion}&fingerprint={impact.Fingerprint}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(2, visuals.Count);
        visuals.ForEach(AssertFilesDeleted);
    }

    [Fact]
    public async Task FailedDeletion_LeavesTheFilesIntact()
    {
        var edition = await CreateEditionAsync();
        var uploaded = await UploadAsync(edition, VisualType.Cover);
        var albumUri = $"/admin/albums/{edition.AlbumId}";
        var impact = (await _client.GetFromJsonAsync<DeletionImpact>($"{albumUri}/deletion-impact"))!;

        var response = await _client.DeleteAsync($"{albumUri}?version={edition.AlbumVersion}&fingerprint={impact.Fingerprint}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        var visual = uploaded.Visuals.Single();
        Assert.True(File.Exists(VolumePath(visual.OriginalPath)));
        Assert.True(File.Exists(VolumePath(visual.DisplayPath)));
    }

    [Fact]
    public async Task Reconciliation_DeletesTheOldOrphansOnly()
    {
        var edition = await CreateEditionAsync();
        var referenced = (await UploadAsync(edition, VisualType.Cover)).Visuals.Single();
        var oldOrphan = await WriteVolumeFileAsync($"originals/{Guid.CreateVersion7():N}.jpg", TimeSpan.FromHours(2));
        var recentOrphan = await WriteVolumeFileAsync($"display/{Guid.CreateVersion7():N}.webp", TimeSpan.FromMinutes(10));
        File.SetLastWriteTimeUtc(VolumePath(referenced.OriginalPath), DateTime.UtcNow.AddDays(-3));

        await _factory.Services.GetRequiredService<VisualReconciliation>().ReconcileAsync(CancellationToken.None);

        Assert.False(File.Exists(oldOrphan));
        Assert.True(File.Exists(recentOrphan));
        Assert.True(File.Exists(VolumePath(referenced.OriginalPath)));
        Assert.True(File.Exists(VolumePath(referenced.DisplayPath)));
    }

    private static byte[] SmallJpeg() => TestImages.Encode(40, 60, SKEncodedImageFormat.Jpeg);

    private static string VisualsUri(EditionForm edition) => $"/admin/albums/{edition.AlbumId}/editions/{edition.Id}/visuals";

    private string VolumePath(string relativePath) => Path.Combine(_factory.VisualsRoot, relativePath);

    private List<string> VolumeFiles() =>
        Directory.Exists(_factory.VisualsRoot)
            ? Directory.EnumerateFiles(_factory.VisualsRoot, "*", SearchOption.AllDirectories).Order().ToList()
            : [];

    private void AssertFilesDeleted(EditionVisualForm visual)
    {
        Assert.False(File.Exists(VolumePath(visual.OriginalPath)));
        Assert.False(File.Exists(VolumePath(visual.DisplayPath)));
    }

    private async Task<string> WriteVolumeFileAsync(string relativePath, TimeSpan age)
    {
        var path = VolumePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, [1, 2, 3]);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow - age);
        return path;
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().Database.ExecuteSqlRawAsync(sql);
    }

    private Task<HttpResponseMessage> PostUploadAsync(EditionForm edition, byte[] content, VisualType type, uint albumVersion)
    {
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(content), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(type.ToString()), UploadVisualFields.Type },
            { new StringContent(albumVersion.ToString()), UploadVisualFields.AlbumVersion },
        };
        return _client.PostAsync(VisualsUri(edition), form);
    }

    // Uploads at the current version of the album.
    private async Task<EditionVisualsForm> UploadAsync(EditionForm edition, VisualType type)
    {
        var response = await PostUploadAsync(edition, SmallJpeg(), type, (await GetAlbumAsync(edition.AlbumId)).Version);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!;
    }

    private async Task<EditionVisualsForm> ArrangeAsync(EditionForm edition, IReadOnlyList<VisualArrangement> visuals, uint albumVersion)
    {
        var response = await _client.PutAsJsonAsync(VisualsUri(edition), new ArrangeVisualsRequest(visuals, albumVersion));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!;
    }

    private async Task<EditionVisualsForm> GetVisualsAsync(EditionForm edition) =>
        (await _client.GetFromJsonAsync<EditionVisualsForm>(VisualsUri(edition)))!;

    private async Task<EditionForm> CreateEditionAsync(Guid? albumId = null)
    {
        var album = albumId is { } id ? await GetAlbumAsync(id) : await CreateAlbumAsync();
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest($"Éditeur {Guid.NewGuid():N}", null));
        var content = new EditionContent(
            publisher.Id, null, null, null, null, null, null, null, null, null, false, true, null,
            AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null);
        return await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(content, album.Version));
    }

    private Task<AlbumForm> CreateAlbumAsync() =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            $"Album {Guid.NewGuid():N}", null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

    private async Task<AlbumForm> GetAlbumAsync(Guid id) => (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{id}"))!;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }

    /// <summary>Compares two forms by value (a record compares its list of visuals by reference).</summary>
    private sealed class VisualsFormComparer : IEqualityComparer<EditionVisualsForm>
    {
        public static readonly VisualsFormComparer Instance = new();

        public bool Equals(EditionVisualsForm? x, EditionVisualsForm? y) =>
            x is not null && y is not null
            && (x.EditionId, x.AlbumVersion) == (y.EditionId, y.AlbumVersion)
            && x.Visuals.SequenceEqual(y.Visuals);

        public int GetHashCode(EditionVisualsForm obj) => obj.EditionId.GetHashCode();
    }
}
