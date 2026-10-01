using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Api.Tests;

/// <summary>Administration of the genres (<c>/admin/genres</c>), including their deletion.</summary>
public sealed class GenreEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GenreEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Create_ReturnsTheGenreAsStoredWithItsVersion()
    {
        var label = UniqueLabel();

        var response = await _client.PostAsJsonAsync("/admin/genres", new CreateGenreRequest($"  {label}  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<GenreForm>();
        Assert.Equal(label, created!.Label);
        Assert.Equal($"/admin/genres/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(created, await _client.GetFromJsonAsync<GenreForm>($"/admin/genres/{created.Id}"));
    }

    [Fact]
    public async Task Create_WithBlankLabel_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/genres", new CreateGenreRequest(" "));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.GenreLabelRequired);
    }

    [Fact]
    public async Task Create_WithLabelDifferingOnlyByCaseAndAccents_IsABusinessError()
    {
        var label = UniqueLabel();
        await CreateAsync($"Épopée {label}");

        var response = await _client.PostAsJsonAsync("/admin/genres", new CreateGenreRequest($"EPOPEE {label}"));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.GenreLabelAlreadyUsed);
    }

    [Fact]
    public async Task Get_UnknownGenre_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/genres/{Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_AtTheCurrentVersion_SavesAndReturnsTheNewVersion()
    {
        var genre = await CreateAsync(UniqueLabel());
        var label = UniqueLabel();

        var response = await _client.PutAsJsonAsync($"/admin/genres/{genre.Id}", new UpdateGenreRequest(label, genre.Version));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GenreForm>();
        Assert.Equal(label, updated!.Label);
        Assert.NotEqual(genre.Version, updated.Version);
        Assert.Equal(updated, await _client.GetFromJsonAsync<GenreForm>($"/admin/genres/{genre.Id}"));
    }

    [Fact]
    public async Task Update_FromAStaleVersion_IsAFunctionalErrorAndKeepsTheOtherModification()
    {
        var genre = await CreateAsync(UniqueLabel());
        var otherTab = UniqueLabel();
        await _client.PutAsJsonAsync($"/admin/genres/{genre.Id}", new UpdateGenreRequest(otherTab, genre.Version));

        var response = await _client.PutAsJsonAsync($"/admin/genres/{genre.Id}", new UpdateGenreRequest(UniqueLabel(), genre.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(otherTab, (await _client.GetFromJsonAsync<GenreForm>($"/admin/genres/{genre.Id}"))!.Label);
    }

    [Fact]
    public async Task Update_UnknownGenre_IsAFunctionalError()
    {
        var response = await _client.PutAsJsonAsync($"/admin/genres/{Guid.CreateVersion7()}", new UpdateGenreRequest(UniqueLabel(), 1));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task DeletionImpact_CountsTheAlbumsAndSeriesLosingTheGenre()
    {
        var genre = await CreateAsync(UniqueLabel());
        await SeedAlbumsAndSeriesWithAsync(genre.Id, albums: 2, series: 1);

        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/genres/{genre.Id}/deletion-impact");

        Assert.Empty(impact!.BlockedBy);
        Assert.Equal([new ImpactCount(EntityKind.Album, 2), new ImpactCount(EntityKind.Series, 1)], impact.AssociationsRemoved);
        Assert.Empty(impact.DeletedWith);
        Assert.NotEmpty(impact.Fingerprint);
    }

    [Fact]
    public async Task DeletionImpact_OfAnUnknownGenre_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/genres/{Guid.CreateVersion7()}/deletion-impact");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_AsConfirmed_RemovesTheGenreFromItsAlbumsAndSeries()
    {
        var genre = await CreateAsync(UniqueLabel());
        var (albumIds, seriesIds) = await SeedAlbumsAndSeriesWithAsync(genre.Id, albums: 1, series: 1);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/genres/{genre.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(genre, impact!.Fingerprint));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await ProblemAssert.IsProblemAsync(
            await _client.GetAsync($"/admin/genres/{genre.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.Empty((await context.Albums.Include(a => a.Genres).SingleAsync(a => a.Id == albumIds[0])).Genres);
        Assert.Empty((await context.Series.Include(s => s.Genres).SingleAsync(s => s.Id == seriesIds[0])).Genres);
    }

    [Fact]
    public async Task Delete_AfterAnAssociationWasReplacedByAnother_IsRefusedWithTheNewImpact()
    {
        // The counts are unchanged (one album), but it is no longer the album the user confirmed.
        var genre = await CreateAsync(UniqueLabel());
        var (albumIds, _) = await SeedAlbumsAndSeriesWithAsync(genre.Id, albums: 1, series: 0);
        var confirmed = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/genres/{genre.Id}/deletion-impact");
        await SeedAlbumsAndSeriesWithAsync(genre.Id, albums: 1, series: 0);
        await RemoveGenreFromAlbumAsync(albumIds[0], genre.Id);

        var response = await _client.DeleteAsync(DeleteUri(genre, confirmed!.Fingerprint));

        var problem = await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        var current = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/genres/{genre.Id}/deletion-impact");
        Assert.Equal(confirmed.AssociationsRemoved, current!.AssociationsRemoved);
        Assert.NotEqual(confirmed.Fingerprint, current.Fingerprint);
        ProblemAssert.SameImpact(current, ProblemAssert.ImpactOf(problem));
    }

    [Fact]
    public async Task Delete_FromAStaleVersion_IsAFunctionalError()
    {
        var genre = await CreateAsync(UniqueLabel());
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/genres/{genre.Id}/deletion-impact");
        await _client.PutAsJsonAsync($"/admin/genres/{genre.Id}", new UpdateGenreRequest(UniqueLabel(), genre.Version));

        var response = await _client.DeleteAsync(DeleteUri(genre, impact!.Fingerprint));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    private static string UniqueLabel() => $"Genre {Guid.NewGuid():N}";

    private static string DeleteUri(GenreForm genre, string fingerprint) =>
        $"/admin/genres/{genre.Id}?version={genre.Version}&fingerprint={Uri.EscapeDataString(fingerprint)}";


    private async Task<GenreForm> CreateAsync(string label)
    {
        var response = await _client.PostAsJsonAsync("/admin/genres", new CreateGenreRequest(label));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GenreForm>())!;
    }

    private async Task<(List<Guid> AlbumIds, List<Guid> SeriesIds)> SeedAlbumsAndSeriesWithAsync(Guid genreId, int albums, int series)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var genre = await context.Genres.SingleAsync(g => g.Id == genreId);
        var albumIds = new List<Guid>();
        for (var i = 0; i < albums; i++)
        {
            var album = new Album(UniqueLabel(), null);
            album.AddGenre(genre);
            context.Albums.Add(album);
            albumIds.Add(album.Id);
        }

        var seriesIds = new List<Guid>();
        for (var i = 0; i < series; i++)
        {
            var oneSeries = new Series(UniqueLabel());
            oneSeries.AddGenre(genre);
            context.Series.Add(oneSeries);
            seriesIds.Add(oneSeries.Id);
        }

        await context.SaveChangesAsync();
        return (albumIds, seriesIds);
    }

    private async Task RemoveGenreFromAlbumAsync(Guid albumId, Guid genreId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var album = await context.Albums.Include(a => a.Genres).SingleAsync(a => a.Id == albumId);
        album.RemoveGenre(album.Genres.Single(g => g.Id == genreId));
        await context.SaveChangesAsync();
    }
}
