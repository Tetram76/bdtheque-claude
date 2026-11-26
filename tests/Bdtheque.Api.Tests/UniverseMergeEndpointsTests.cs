using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Errors;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The merge of two universes (<c>/admin/universes/merge</c>, fonctionnel/regles-metier/fusion-des-fiches.md): the universe that
/// remains keeps the data chosen and takes the series, albums and sub-universes of both, its homonymous sub-universes being
/// merged with it at every level; it can take for parent neither the other universe nor one of their sub-universes.
/// </summary>
public sealed class UniverseMergeEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateApiClient();

    [Fact]
    public async Task Preview_PairsTheHomonymousSubUniversesAtEveryLevelAndCountsWhatPassesOnce()
    {
        var (first, second) = (await CreateAsync("Star Wars"), await CreateAsync("Star Wars bis"));
        var cloneWars = await CreateAsync("Clone Wars", first.Id);
        var cloneWarsTwin = await CreateAsync("clone wars", second.Id);
        var jedi = await CreateAsync("Les Jedi", cloneWars.Id);
        var jediTwin = await CreateAsync("Les Jédi", cloneWarsTwin.Id);
        await CreateAsync("Rebels", second.Id);
        await SeedAlbumsAsync([first.Id], 2);
        await SeedAlbumsAsync([first.Id, second.Id], 1);
        await SeedAlbumsAsync([jediTwin.Id], 1);

        var preview = await PreviewAsync(first.Id, second.Id);

        Assert.Equal(
            [(["Clone Wars"], cloneWars.Id, cloneWarsTwin.Id), (["Clone Wars", "Les Jedi"], jedi.Id, jediTwin.Id)],
            [.. preview.Pairs.Select(p => (p.Path.ToArray(), p.First.Id, p.Second.Id))],
            new PairComparer());
        Assert.Equal(
            [new MergeLinkCount(EntityKind.Album, 4, 3, 2, 1), new MergeLinkCount(EntityKind.Universe, 3, 2, 3, 2)],
            preview.Links.Where(l => l.Total > 0));
        Assert.True(preview.First.ParentChoosable && preview.Second.ParentChoosable);
    }

    [Fact]
    public async Task Merge_MergesTheHomonymousSubUniversesAndMovesTheOthersToTheUniverseThatRemains()
    {
        var lucasfilm = await CreateAsync("Lucasfilm");
        var (first, second) = (await CreateAsync("Star Wars"), await CreateAsync("Star wars", lucasfilm.Id));
        var cloneWars = await CreateAsync("Clone Wars", first.Id);
        var cloneWarsTwin = await CreateAsync("Clone wars", second.Id);
        var rebels = await CreateAsync("Rebels", second.Id);
        var album = (await SeedAlbumsAsync([cloneWarsTwin.Id, cloneWars.Id], 1))[0];
        var preview = await PreviewAsync(first.Id, second.Id);
        var pair = Assert.Single(preview.Pairs);

        // The name of the first, the parent of the second; the name of the second sub-universe.
        var response = await MergeAsync(preview, MergeSide.First, MergeSide.First, MergeSide.Second, [Choice(pair, MergeSide.Second, MergeSide.First)]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(first.Id, (await response.Content.ReadFromJsonAsync<MergeResult>())!.Id);
        var kept = await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{first.Id}");
        Assert.Equal((first.Name, lucasfilm.Id), (kept!.Name, kept.ParentId));
        await ProblemAssert.IsProblemAsync(await _client.GetAsync($"/admin/universes/{second.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        // The pair keeps the sub-universe whose data it keeps the most of: the first, which takes the name of the second.
        var keptPair = await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{cloneWars.Id}");
        Assert.Equal(("Clone wars", first.Id), (keptPair!.Name, keptPair.ParentId));
        await ProblemAssert.IsProblemAsync(await _client.GetAsync($"/admin/universes/{cloneWarsTwin.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        Assert.Equal(first.Id, (await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{rebels.Id}"))!.ParentId);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.Equal([cloneWars.Id], (await context.Albums.Include(a => a.Universes).SingleAsync(a => a.Id == album)).Universes.Select(u => u.Id));
    }

    [Fact]
    public async Task Merge_OfAUniverseWithOneOfItsSubUniverses_RefusesItAsParentAndKeepsItsSubUniverses()
    {
        var pratt = await CreateAsync("Univers Hugo Pratt");
        var first = await CreateAsync("Corto Maltese", pratt.Id);
        var second = await CreateAsync("Corto", first.Id);
        var youth = await CreateAsync("Jeunesse", second.Id);
        var preview = await PreviewAsync(first.Id, second.Id);
        Assert.Equal((true, false), (preview.First.ParentChoosable, preview.Second.ParentChoosable));

        var refused = await MergeAsync(preview, MergeSide.First, MergeSide.First, MergeSide.Second, []);
        await ProblemAssert.IsProblemAsync(refused, HttpStatusCode.UnprocessableEntity, ProblemTypes.Business);

        var response = await MergeAsync(preview, MergeSide.First, MergeSide.First, MergeSide.First, []);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(pratt.Id, (await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{first.Id}"))!.ParentId);
        Assert.Equal(first.Id, (await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{youth.Id}"))!.ParentId);
    }

    // The universe that remains stands below a sub-universe of the other one, which passes under it.
    [Fact]
    public async Task Merge_OfAUniverseWithOneOfItsIndirectDescendants_TakesTheSubUniverseBetweenThemUnderTheOneThatRemains()
    {
        var pratt = await CreateAsync("Univers Hugo Pratt");
        var first = await CreateAsync("Corto", pratt.Id);
        var youth = await CreateAsync("Jeunesse", first.Id);
        var second = await CreateAsync("Corto Maltese", youth.Id);
        var preview = await PreviewAsync(first.Id, second.Id);

        var response = await MergeAsync(preview, MergeSide.Second, MergeSide.Second, MergeSide.First, []);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(pratt.Id, (await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{second.Id}"))!.ParentId);
        Assert.Equal(second.Id, (await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{youth.Id}"))!.ParentId);
    }

    [Fact]
    public async Task Merge_ChosenOnPairsThatChangedSince_IsAFunctionalErrorAndMergesNothing()
    {
        var (first, second) = (await CreateAsync("Star Wars"), await CreateAsync("Star wars"));
        var preview = await PreviewAsync(first.Id, second.Id);
        await CreateAsync("Clone Wars", first.Id);
        await CreateAsync("Clone wars", second.Id);

        var response = await MergeAsync(preview, MergeSide.First, MergeSide.First, MergeSide.First, []);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/universes/{second.Id}")).StatusCode);
    }

    [Fact]
    public async Task Merge_OfASubUniverseModifiedSince_IsAFunctionalError()
    {
        var (first, second) = (await CreateAsync("Star Wars"), await CreateAsync("Star wars"));
        await CreateAsync("Clone Wars", first.Id);
        var twin = await CreateAsync("Clone wars", second.Id);
        var preview = await PreviewAsync(first.Id, second.Id);
        (await _client.PutAsJsonAsync($"/admin/universes/{twin.Id}", new UpdateUniverseRequest(twin.Name, "Modifié", second.Id, twin.Version)))
            .EnsureSuccessStatusCode();

        var response = await MergeAsync(preview, MergeSide.First, MergeSide.First, MergeSide.First, [Choice(preview.Pairs[0], MergeSide.First, MergeSide.First)]);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Merge_RedirectsTheSavedSearchesOfAMergedSubUniverse()
    {
        var (first, second) = (await CreateAsync("Star Wars"), await CreateAsync("Star wars"));
        var cloneWars = await CreateAsync("Clone Wars", first.Id);
        var twin = await CreateAsync("Clone wars", second.Id);
        var search = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(
            $"Recherche {Guid.NewGuid():N}", EntityKind.Album,
            new SearchLink("universes", new SearchCriterion("record", SearchComparison.Is, [twin.Id.ToString()]), new SearchQuantifier(SearchQuantifierKind.Any)),
            []));
        search.EnsureSuccessStatusCode();
        var searchId = (await search.Content.ReadFromJsonAsync<SavedSearchForm>())!.Id;
        var preview = await PreviewAsync(first.Id, second.Id);

        var response = await MergeAsync(preview, MergeSide.First, MergeSide.First, MergeSide.First, [Choice(preview.Pairs[0], MergeSide.First, MergeSide.First)]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await _client.GetFromJsonAsync<SavedSearchDetail>($"/catalog/saved-searches/{searchId}");
        Assert.Equal([cloneWars.Id.ToString()], Assert.IsType<SearchCriterion>(Assert.IsType<SearchLink>(detail!.Criteria).Condition).Values);
    }

    // A unique name: every test of the class writes to the same database.
    private async Task<UniverseForm> CreateAsync(string name, Guid? parentId = null)
    {
        var response = await _client.PostAsJsonAsync("/admin/universes", new CreateUniverseRequest(parentId is null ? $"{name} {Guid.NewGuid():N}" : name, null, parentId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UniverseForm>())!;
    }

    private async Task<UniverseMergePreview> PreviewAsync(Guid first, Guid second) =>
        (await _client.GetFromJsonAsync<UniverseMergePreview>($"/admin/universes/merge?first={first}&second={second}"))!;

    private Task<HttpResponseMessage> MergeAsync(
        UniverseMergePreview preview, MergeSide name, MergeSide description, MergeSide parent, IReadOnlyList<UniverseMergePairChoice> pairs) =>
        _client.PostAsJsonAsync("/admin/universes/merge", new UniverseMergeRequest(
            new MergedRecord(preview.First.Id, preview.First.Version), new MergedRecord(preview.Second.Id, preview.Second.Version),
            name, description, parent, pairs));

    private static UniverseMergePairChoice Choice(UniverseMergePair pair, MergeSide name, MergeSide description) =>
        new(new MergedRecord(pair.First.Id, pair.First.Version), new MergedRecord(pair.Second.Id, pair.Second.Version), name, description);

    private async Task<List<Guid>> SeedAlbumsAsync(Guid[] universeIds, int count)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var universes = await context.Universes.Where(u => universeIds.Contains(u.Id)).ToListAsync();
        var ids = Enumerable.Range(0, count).Select(_ =>
        {
            var album = new Album($"Album {Guid.NewGuid():N}", null);
            universes.ForEach(album.AddUniverse);
            context.Albums.Add(album);
            return album.Id;
        }).ToList();
        await context.SaveChangesAsync();
        return ids;
    }

    private sealed class PairComparer : IEqualityComparer<(string[] Path, Guid First, Guid Second)>
    {
        public bool Equals((string[] Path, Guid First, Guid Second) x, (string[] Path, Guid First, Guid Second) y) =>
            x.Path.SequenceEqual(y.Path) && x.First == y.First && x.Second == y.Second;

        public int GetHashCode((string[] Path, Guid First, Guid Second) obj) => HashCode.Combine(obj.First, obj.Second);
    }
}
