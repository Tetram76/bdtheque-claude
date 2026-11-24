using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Errors;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The merge of two genres and of two authors (<c>/admin/genres/merge</c>, <c>/admin/authors/merge</c>,
/// fonctionnel/regles-metier/fusion-des-fiches.md): the record that remains keeps the data chosen, and every record designating
/// either of them designates it, each once.
/// </summary>
public sealed class MergeEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MergeEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task GenreMergePreview_PresentsBothGenresAndCountsWhatTheyClassOnce()
    {
        var first = await CreateGenreAsync();
        var second = await CreateGenreAsync();
        await SeedAsync(context => ClassedUnder(context, [first.Id], albums: 2, series: 1));
        await SeedAsync(context => ClassedUnder(context, [second.Id], albums: 1, series: 0));
        await SeedAsync(context => ClassedUnder(context, [first.Id, second.Id], albums: 1, series: 1));

        var preview = await _client.GetFromJsonAsync<GenreMergePreview>($"/admin/genres/merge?first={first.Id}&second={second.Id}");

        Assert.Equal((first.Id, first.Label, first.Version), (preview!.First.Id, preview.First.Label, preview.First.Version));
        Assert.Equal((second.Id, second.Label, second.Version), (preview.Second.Id, preview.Second.Label, preview.Second.Version));
        Assert.Equal(
            [new MergeLinkCount(EntityKind.Series, 2, 2, 1, 1), new MergeLinkCount(EntityKind.Album, 4, 3, 2, 1)],
            preview.Links);
    }

    [Fact]
    public async Task GenreMergePreview_OfAnUnknownGenre_IsAFunctionalError()
    {
        var genre = await CreateGenreAsync();

        var response = await _client.GetAsync($"/admin/genres/merge?first={genre.Id}&second={Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task GenreMerge_KeepsTheLabelChosenAndClassesWhatBothClassedOnce()
    {
        var first = await CreateGenreAsync();
        var second = await CreateGenreAsync();
        var onlyFirst = await SeedAsync(context => ClassedUnder(context, [first.Id], albums: 1, series: 1));
        var both = await SeedAsync(context => ClassedUnder(context, [first.Id, second.Id], albums: 1, series: 0));

        var response = await _client.PostAsJsonAsync("/admin/genres/merge", new GenreMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.Second));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var kept = (await response.Content.ReadFromJsonAsync<MergeResult>())!.Id;
        // The second genre gives its label: it is the one that remains.
        Assert.Equal(second.Id, kept);
        var merged = await _client.GetFromJsonAsync<GenreForm>($"/admin/genres/{kept}");
        Assert.Equal(second.Label, merged!.Label);
        await ProblemAssert.IsProblemAsync(await _client.GetAsync($"/admin/genres/{first.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        foreach (var albumId in onlyFirst.Albums.Concat(both.Albums))
            Assert.Equal([kept], (await context.Albums.Include(a => a.Genres).SingleAsync(a => a.Id == albumId)).Genres.Select(g => g.Id));
        Assert.Equal([kept], (await context.Series.Include(s => s.Genres).SingleAsync(s => s.Id == onlyFirst.Series[0])).Genres.Select(g => g.Id));
    }

    [Fact]
    public async Task GenreMerge_ToTheLabelOfTheMergedGenre_IsNotRefusedAsAlreadyUsed()
    {
        // The label leaves with the genre deleted, in the same write.
        var first = await CreateGenreAsync();
        var second = await CreateGenreAsync();

        var response = await _client.PostAsJsonAsync("/admin/genres/merge", new GenreMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.First));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(first.Id, (await response.Content.ReadFromJsonAsync<MergeResult>())!.Id);
    }

    [Fact]
    public async Task GenreMerge_FromAStaleVersion_IsAFunctionalErrorAndMergesNothing()
    {
        var first = await CreateGenreAsync();
        var second = await CreateGenreAsync();
        await _client.PutAsJsonAsync($"/admin/genres/{second.Id}", new UpdateGenreRequest($"Genre {Guid.NewGuid():N}", second.Version));

        var response = await _client.PostAsJsonAsync("/admin/genres/merge", new GenreMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.First));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/genres/{second.Id}")).StatusCode);
    }

    [Fact]
    public async Task GenreMerge_RedirectsTheSavedSearchesToTheGenreThatRemains()
    {
        var first = await CreateGenreAsync();
        var second = await CreateGenreAsync();
        var search = await CreateSearchAsync(new SearchLink(
            "genres", new SearchCriterion("record", SearchComparison.Is, [first.Id.ToString()]), new SearchQuantifier(SearchQuantifierKind.Any)));

        var response = await _client.PostAsJsonAsync("/admin/genres/merge", new GenreMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.Second));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await _client.GetFromJsonAsync<SavedSearchDetail>($"/catalog/saved-searches/{search.Id}");
        var link = Assert.IsType<SearchLink>(detail!.Criteria);
        Assert.Equal([second.Id.ToString()], Assert.IsType<SearchCriterion>(link.Condition).Values);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var designations = (await context.SavedSearches.SingleAsync(s => s.Id == search.Id)).Designations;
        Assert.Equal([second.Id], designations.Select(d => d.RecordId));
    }

    [Fact]
    public async Task AuthorIdentity_IsTheOneTheDomainDerives()
    {
        var identity = await _client.GetFromJsonAsync<AuthorIdentity>("/admin/authors/identity?lastName=Gotlieb&firstName=Marcel");

        Assert.Equal(new AuthorIdentity("Marcel Gotlieb", "Gotlieb Marcel", "G"), identity);
    }

    [Fact]
    public async Task AuthorIdentity_WithoutLastNameNorPseudonym_IsABusinessError()
    {
        var response = await _client.GetAsync("/admin/authors/identity?firstName=Marcel");

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AuthorLastNameOrPseudonymRequired);
    }

    [Fact]
    public async Task AuthorMergePreview_CountsTheSeriesAndAlbumsCreditedOnce()
    {
        var first = await CreateAuthorAsync(pseudonym: null);
        var second = await CreateAuthorAsync(pseudonym: null);
        await SeedAsync(context => CreditedOn(context, [first.Id], albums: 2, series: 1));
        await SeedAsync(context => CreditedOn(context, [first.Id, second.Id], albums: 1, series: 1));

        var preview = await _client.GetFromJsonAsync<AuthorMergePreview>($"/admin/authors/merge?first={first.Id}&second={second.Id}");

        Assert.Equal(first.LastName, preview!.First.LastName);
        Assert.Equal(second.Version, preview.Second.Version);
        Assert.Equal(
            [new MergeLinkCount(EntityKind.Series, 2, 2, 1, 1), new MergeLinkCount(EntityKind.Album, 3, 3, 1, 1)],
            preview.Links);
    }

    [Fact]
    public async Task AuthorMerge_KeepsEachDataChosenAndCreditsBothCreditsOnce()
    {
        var first = await CreateAuthorAsync(pseudonym: null, nationality: "Française", biography: "Longue biographie");
        var pseudonym = $"Pseudo {Guid.NewGuid():N}";
        var second = await CreateAuthorAsync(pseudonym, nationality: null, biography: "Courte");
        var onlySecond = await SeedAsync(context => CreditedOn(context, [second.Id], albums: 1, series: 1));
        var both = await SeedAsync(context => CreditedOn(context, [first.Id, second.Id], albums: 1, series: 0));

        // Three data of the first author, two of the second: the first one remains.
        var response = await _client.PostAsJsonAsync("/admin/authors/merge", new AuthorMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version),
            LastName: MergeSide.First, FirstName: MergeSide.First, Pseudonym: MergeSide.Second, Nationality: MergeSide.First,
            Biography: MergeSide.Second));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(first.Id, (await response.Content.ReadFromJsonAsync<MergeResult>())!.Id);
        var kept = await _client.GetFromJsonAsync<AuthorForm>($"/admin/authors/{first.Id}");
        Assert.Equal(
            (first.LastName, first.FirstName, pseudonym, "Française", "Courte"),
            (kept!.LastName, kept.FirstName, kept.Pseudonym, kept.Nationality, kept.Biography));
        await ProblemAssert.IsProblemAsync(await _client.GetAsync($"/admin/authors/{second.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        foreach (var albumId in onlySecond.Albums.Concat(both.Albums))
            Assert.Equal(
                [(first.Id, ContributionRole.Scenarist)],
                (await context.Albums.SingleAsync(a => a.Id == albumId)).Contributions.Select(c => (c.AuthorId, c.Role)));
        Assert.Equal(
            [(first.Id, ContributionRole.Scenarist)],
            (await context.Series.Include(s => s.TemplateContributions).SingleAsync(s => s.Id == onlySecond.Series[0]))
                .TemplateContributions.Select(c => (c.AuthorId, c.Role)));
    }

    [Fact]
    public async Task AuthorMerge_ToTheDisplayNameOfTheMergedAuthor_IsNotRefusedAsAlreadyUsed()
    {
        // The display name leaves with the author deleted, in the same write.
        var pseudonym = $"Pseudo {Guid.NewGuid():N}";
        var first = await CreateAuthorAsync(pseudonym: null);
        var second = await CreateAuthorAsync(pseudonym);

        var response = await _client.PostAsJsonAsync("/admin/authors/merge", new AuthorMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version),
            MergeSide.First, MergeSide.First, MergeSide.Second, MergeSide.First, MergeSide.First));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(pseudonym, (await _client.GetFromJsonAsync<AuthorForm>($"/admin/authors/{first.Id}"))!.Pseudonym);
    }

    [Fact]
    public async Task AuthorMerge_WithoutLastNameNorPseudonym_IsABusinessErrorAndMergesNothing()
    {
        var first = await CreateAuthorAsync(pseudonym: null);
        var second = await CreateAuthorAsync($"Pseudo {Guid.NewGuid():N}", lastName: null);

        var response = await _client.PostAsJsonAsync("/admin/authors/merge", new AuthorMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version),
            MergeSide.Second, MergeSide.First, MergeSide.First, MergeSide.First, MergeSide.First));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AuthorLastNameOrPseudonymRequired);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/authors/{second.Id}")).StatusCode);
    }

    private async Task<GenreForm> CreateGenreAsync()
    {
        var response = await _client.PostAsJsonAsync("/admin/genres", new CreateGenreRequest($"Genre {Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GenreForm>())!;
    }

    private async Task<AuthorForm> CreateAuthorAsync(
        string? pseudonym, string? nationality = null, string? biography = null, string? lastName = "Auteur")
    {
        var response = await _client.PostAsJsonAsync("/admin/authors", new CreateAuthorRequest(
            lastName is null ? null : $"{lastName} {Guid.NewGuid():N}", "Prénom", pseudonym, biography, nationality));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthorForm>())!;
    }

    private async Task<SavedSearchForm> CreateSearchAsync(SearchCondition criteria)
    {
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(
            $"Recherche {Guid.NewGuid():N}", EntityKind.Album, criteria, []));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SavedSearchForm>())!;
    }

    private async Task<(List<Guid> Albums, List<Guid> Series)> SeedAsync(Func<BdthequeDbContext, Task<(List<Guid>, List<Guid>)>> seed)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var seeded = await seed(context);
        await context.SaveChangesAsync();
        return seeded;
    }

    private static async Task<(List<Guid>, List<Guid>)> ClassedUnder(BdthequeDbContext context, Guid[] genreIds, int albums, int series)
    {
        var genres = await context.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync();
        var albumIds = Enumerable.Range(0, albums).Select(_ =>
        {
            var album = new Album($"Album {Guid.NewGuid():N}", null);
            genres.ForEach(album.AddGenre);
            context.Albums.Add(album);
            return album.Id;
        }).ToList();
        var seriesIds = Enumerable.Range(0, series).Select(_ =>
        {
            var oneSeries = new Series($"Série {Guid.NewGuid():N}");
            genres.ForEach(oneSeries.AddGenre);
            context.Series.Add(oneSeries);
            return oneSeries.Id;
        }).ToList();
        return (albumIds, seriesIds);
    }

    private static async Task<(List<Guid>, List<Guid>)> CreditedOn(BdthequeDbContext context, Guid[] authorIds, int albums, int series)
    {
        var authors = await context.Authors.Where(a => authorIds.Contains(a.Id)).ToListAsync();
        var credits = authors.Select(a => (a, ContributionRole.Scenarist)).ToList();
        var albumIds = Enumerable.Range(0, albums).Select(_ =>
        {
            var album = new Album($"Album {Guid.NewGuid():N}", null, credits);
            context.Albums.Add(album);
            return album.Id;
        }).ToList();
        var seriesIds = Enumerable.Range(0, series).Select(_ =>
        {
            var oneSeries = new Series($"Série {Guid.NewGuid():N}");
            oneSeries.SetTemplateContributions(credits);
            context.Series.Add(oneSeries);
            return oneSeries.Id;
        }).ToList();
        return (albumIds, seriesIds);
    }
}
