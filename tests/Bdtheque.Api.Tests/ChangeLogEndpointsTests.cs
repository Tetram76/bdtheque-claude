using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Api.Tests;

/// <summary>Reading of the change log (<c>/admin/change-log</c>).</summary>
public sealed class ChangeLogEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChangeLogEndpointsTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Entries_ReadFromTheLatestToTheOldest_AsData()
    {
        var album = await CreateAlbumAsync("Le Lotus bleu", AlbumRating.Good);
        var response = await _client.PutAsJsonAsync(
            $"/admin/albums/{album.Id}", new UpdateAlbumRequest(Content("Les Cigares du pharaon", AlbumRating.VeryGood), album.Version));
        response.EnsureSuccessStatusCode();

        var entries = (await ReadAsync("pageSize=100")).Items.Where(e => e.RecordId == album.Id).ToList();

        Assert.Equal([ChangeLogOperation.Modification, ChangeLogOperation.Creation], entries.Select(e => e.Operation));
        Assert.All(entries, e => Assert.Equal(RecordType.Album, e.RecordType));
        var modification = entries[0];
        Assert.Equal(new AlbumLabel("Les Cigares du pharaon", null, AlbumType.Regular, false, null, null, null), modification.Label);
        Assert.Contains(
            new ChangeLogChange(ChangeLogAttribute.Title, new TextValue("Le Lotus bleu"), new TextValue("Les Cigares du pharaon")),
            modification.Changes);
        Assert.Contains(
            new ChangeLogChange(
                ChangeLogAttribute.Rating, new EnumValue(nameof(AlbumRating), nameof(AlbumRating.Good)),
                new EnumValue(nameof(AlbumRating), nameof(AlbumRating.VeryGood))),
            modification.Changes);
        Assert.Empty(entries[1].Changes);
    }

    [Fact]
    public async Task SameEntry_AlwaysReadsTheSame()
    {
        await CreateAlbumAsync("Tintin au Tibet", AlbumRating.Good);

        var first = await _client.GetStringAsync("/admin/change-log?pageSize=100");
        var second = await _client.GetStringAsync("/admin/change-log?pageSize=100");

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Entries_ArePaginated()
    {
        await CreateAlbumAsync("Coke en stock", null);
        await CreateAlbumAsync("Vol 714 pour Sydney", null);

        var page = await ReadAsync("pageSize=1");

        var latest = Assert.Single(page.Items);
        Assert.Equal(1, page.Size);
        Assert.True(page.TotalCount >= 2);
        Assert.Equal(latest.Id, (await ReadAsync("pageSize=100")).Items[0].Id);
    }

    [Fact]
    public async Task PageSizeOutOfBounds_IsATechnicalError()
    {
        var response = await _client.GetAsync("/admin/change-log?pageSize=0");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private async Task<Page<ChangeLogEntryItem>> ReadAsync(string query) =>
        (await _client.GetFromJsonAsync<Page<ChangeLogEntryItem>>($"/admin/change-log?{query}"))!;

    private async Task<AlbumForm> CreateAlbumAsync(string title, AlbumRating? rating)
    {
        var response = await _client.PostAsJsonAsync("/admin/albums", Content(title, rating));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AlbumForm>())!;
    }

    private static AlbumContent Content(string title, AlbumRating? rating) =>
        new(title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, rating, [], [], []);
}
