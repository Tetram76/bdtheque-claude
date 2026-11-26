using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Errors;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The merge of two publishers (<c>/admin/publishers/merge</c>, fonctionnel/regles-metier/fusion-des-fiches.md): the publisher
/// that remains keeps the data chosen and takes the collections of the other one, two homonymous collections becoming one, and
/// every edition, series and saved search designating either designates it, with the collection that remains.
/// </summary>
public sealed class PublisherMergeEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateApiClient();

    [Fact]
    public async Task Preview_PresentsBothPublishers_TheirHomonymousCollections_AndCountsWhatPassesToTheRecordOnce()
    {
        var first = await CreatePublisherAsync("https://dupuis.com", "Aire Libre", "Repérages");
        var second = await CreatePublisherAsync(null, "aire libré", "Expresso");
        await SeedAsync(context => PublishedBy(context, first.Id, CollectionOf(first, "Aire Libre"), owned: 2, wished: 1, series: 1));
        await SeedAsync(context => PublishedBy(context, second.Id, null, owned: 1, wished: 0, series: 2));

        var preview = await _client.GetFromJsonAsync<PublisherMergePreview>($"/admin/publishers/merge?first={first.Id}&second={second.Id}");

        Assert.Equal((first.Id, first.Name, first.Website, first.Version), (preview!.First.Id, preview.First.Name, preview.First.Website, preview.First.Version));
        Assert.Equal((second.Id, second.Name, null), (preview.Second.Id, preview.Second.Name, preview.Second.Website));
        // The names compare without case nor accents, as they do among the collections of a publisher.
        Assert.Equal(
            [new PublisherCollectionHomonyms(CollectionOf(first, "Aire Libre"), "Aire Libre", CollectionOf(second, "aire libré"), "aire libré")],
            preview.Homonyms);
        Assert.Equal(
            [
                new MergeLinkCount(EntityKind.Series, 3, 1, 2, 0),
                new MergeLinkCount(EntityKind.PublisherCollection, 3, 2, 2, 1),
                new MergeLinkCount(EntityKind.Edition, 3, 2, 1, 0),
                new MergeLinkCount(EntityKind.PurchaseIntent, 1, 1, 0, 0),
            ],
            preview.Links);
    }

    [Fact]
    public async Task Preview_OfAnUnknownPublisher_IsAFunctionalError()
    {
        var publisher = await CreatePublisherAsync(null);

        var response = await _client.GetAsync($"/admin/publishers/merge?first={publisher.Id}&second={Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Merge_KeepsEachDataChosen_TakesTheCollections_AndPublishesTheEditionsAndSeriesOfBoth()
    {
        var first = await CreatePublisherAsync(null, "Aire Libre", "Repérages");
        var second = await CreatePublisherAsync("https://dupuis.com", "aire libre", "Expresso");
        var (aireLibreOfFirst, aireLibreOfSecond) = (CollectionOf(first, "Aire Libre"), CollectionOf(second, "aire libre"));
        var inFirst = await SeedAsync(context => PublishedBy(context, first.Id, aireLibreOfFirst, owned: 1, wished: 1, series: 1));
        var inSecond = await SeedAsync(context => PublishedBy(context, second.Id, aireLibreOfSecond, owned: 1, wished: 0, series: 1));
        var inExpresso = await SeedAsync(context => PublishedBy(context, second.Id, CollectionOf(second, "Expresso"), owned: 1, wished: 0, series: 0));

        // The name of the first publisher, the website of the second: as many of each, the first one remains. The name of the
        // collection of the second: that collection remains.
        var response = await _client.PostAsJsonAsync("/admin/publishers/merge", new PublisherMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.First, MergeSide.Second,
            [new PublisherCollectionMerge(aireLibreOfFirst, aireLibreOfSecond, MergeSide.Second)]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(first.Id, (await response.Content.ReadFromJsonAsync<MergeResult>())!.Id);
        var kept = (await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{first.Id}"))!;
        Assert.Equal((first.Name, "https://dupuis.com"), (kept.Name, kept.Website));
        // Each collection keeps its identity; of the homonyms, the one whose name is kept remains.
        Assert.Equal(
            [(aireLibreOfSecond, "aire libre"), (CollectionOf(second, "Expresso"), "Expresso"), (CollectionOf(first, "Repérages"), "Repérages")],
            kept.Collections.Select(c => (c.Id, c.Name)));
        await ProblemAssert.IsProblemAsync(await _client.GetAsync($"/admin/publishers/{second.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var editions = await context.Editions.Where(e => inFirst.Albums.Concat(inSecond.Albums).Concat(inExpresso.Albums).Contains(e.AlbumId))
            .Select(e => new { e.AlbumId, e.PublisherId, e.PublisherCollectionId }).ToListAsync();
        Assert.Equal(4, editions.Count);
        Assert.All(editions, e => Assert.Equal(first.Id, e.PublisherId));
        Assert.All(
            editions.Where(e => !inExpresso.Albums.Contains(e.AlbumId)), e => Assert.Equal(aireLibreOfSecond, e.PublisherCollectionId));
        Assert.Equal(CollectionOf(second, "Expresso"), editions.Single(e => inExpresso.Albums.Contains(e.AlbumId)).PublisherCollectionId);
        var series = await context.Series.Where(s => inFirst.Series.Concat(inSecond.Series).Contains(s.Id))
            .Select(s => new { s.TemplatePublisherId, s.TemplatePublisherCollectionId }).ToListAsync();
        Assert.All(series, s => Assert.Equal((first.Id, aireLibreOfSecond), (s.TemplatePublisherId, s.TemplatePublisherCollectionId)));
    }

    [Fact]
    public async Task Merge_ToTheNameOfTheMergedPublisher_IsNotRefusedAsAlreadyUsed()
    {
        // The name leaves with the publisher deleted, in the same write.
        var first = await CreatePublisherAsync(null);
        var second = await CreatePublisherAsync("https://dupuis.com");

        var response = await _client.PostAsJsonAsync("/admin/publishers/merge", new PublisherMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.Second, MergeSide.First, []));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(first.Id, (await response.Content.ReadFromJsonAsync<MergeResult>())!.Id);
        Assert.Equal(second.Name, (await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{first.Id}"))!.Name);
    }

    [Fact]
    public async Task Merge_FromAStaleVersion_IsAFunctionalErrorAndMergesNothing()
    {
        var first = await CreatePublisherAsync(null);
        var second = await CreatePublisherAsync(null);
        // A collection added on the spot modifies the publisher, whose homonyms may change.
        (await _client.PostAsJsonAsync($"/admin/publishers/{second.Id}/collections", new CreatePublisherCollectionRequest("Aire Libre", second.Version)))
            .EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/admin/publishers/merge", new PublisherMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.First, MergeSide.First, []));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/publishers/{second.Id}")).StatusCode);
    }

    [Fact]
    public async Task Merge_WithoutTheNameOfTwoHomonymousCollections_IsATechnicalErrorAndMergesNothing()
    {
        // The client sends back the homonyms it read, which the versions of the publishers keep as they were.
        var first = await CreatePublisherAsync(null, "Aire Libre");
        var second = await CreatePublisherAsync(null, "aire libre");

        var response = await _client.PostAsJsonAsync("/admin/publishers/merge", new PublisherMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.First, MergeSide.First, []));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/publishers/{second.Id}")).StatusCode);
    }

    [Fact]
    public async Task Merge_RedirectsTheSavedSearchesToThePublisherAndTheCollectionThatRemain()
    {
        var first = await CreatePublisherAsync(null, "Aire Libre");
        var second = await CreatePublisherAsync(null, "aire libre");
        var (ofFirst, ofSecond) = (CollectionOf(first, "Aire Libre"), CollectionOf(second, "aire libre"));
        var ofPublisher = await CreateSearchAsync(EntityKind.Series, new SearchLink("publisher", Designating(second.Id)));
        var ofCollection = await CreateSearchAsync(EntityKind.Edition, new SearchLink("publisherCollection", Designating(ofSecond)));

        var response = await _client.PostAsJsonAsync("/admin/publishers/merge", new PublisherMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.First, MergeSide.First,
            [new PublisherCollectionMerge(ofFirst, ofSecond, MergeSide.First)]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([first.Id], await DesignatedAsync(ofPublisher.Id));
        Assert.Equal([ofFirst], await DesignatedAsync(ofCollection.Id));
        var detail = await _client.GetFromJsonAsync<SavedSearchDetail>($"/catalog/saved-searches/{ofCollection.Id}");
        Assert.Equal([ofFirst.ToString()], Assert.IsType<SearchCriterion>(Assert.IsType<SearchLink>(detail!.Criteria).Condition).Values);
    }

    [Fact]
    public async Task Merge_RecordsTheCollectionPassedToTheOtherPublisher_AndTheHomonymRemoved()
    {
        var first = await CreatePublisherAsync(null, "Aire Libre");
        var second = await CreatePublisherAsync(null, "aire libre", "Expresso");
        var (removed, moved) = (CollectionOf(second, "aire libre"), CollectionOf(second, "Expresso"));

        var response = await _client.PostAsJsonAsync("/admin/publishers/merge", new PublisherMergeRequest(
            new MergedRecord(first.Id, first.Version), new MergedRecord(second.Id, second.Version), MergeSide.First, MergeSide.First,
            [new PublisherCollectionMerge(CollectionOf(first, "Aire Libre"), removed, MergeSide.First)]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var change = Assert.Single(
            (await context.ChangeLog.SingleAsync(e => e.RecordId == moved && e.Operation == ChangeLogOperation.Modification)).Changes!);
        Assert.Equal(ChangeLogAttribute.Publisher, change.Attribute);
        Assert.Equal(
            (new Domain.Entities.NameLabel(second.Name), new Domain.Entities.NameLabel(first.Name)),
            (Assert.IsType<Domain.Entities.ReferenceValue>(change.Before).Label, Assert.IsType<Domain.Entities.ReferenceValue>(change.After).Label));
        Assert.True(await context.ChangeLog.AnyAsync(e => e.RecordId == removed && e.Operation == ChangeLogOperation.Deletion));
        Assert.True(await context.ChangeLog.AnyAsync(e => e.RecordId == second.Id && e.Operation == ChangeLogOperation.Deletion));
    }

    private static SearchCriterion Designating(Guid id) => new("record", SearchComparison.Is, [id.ToString()]);

    private static Guid CollectionOf(PublisherForm publisher, string name) => publisher.Collections.Single(c => c.Name == name).Id;

    private async Task<PublisherForm> CreatePublisherAsync(string? website, params string[] collections)
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new PublisherContent(
            $"Éditeur {Guid.NewGuid():N}", website, [.. collections.Select(c => new PublisherCollectionContent(null, c))]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
    }

    private async Task<SavedSearchForm> CreateSearchAsync(EntityKind entity, SearchCondition criteria)
    {
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent($"Recherche {Guid.NewGuid():N}", entity, criteria, []));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SavedSearchForm>())!;
    }

    private async Task<List<Guid>> DesignatedAsync(Guid search)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        return [.. (await context.SavedSearches.SingleAsync(s => s.Id == search)).Designations.Select(d => d.RecordId)];
    }

    private async Task<(List<Guid> Albums, List<Guid> Series)> SeedAsync(Func<BdthequeDbContext, Task<(List<Guid>, List<Guid>)>> seed)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var seeded = await seed(context);
        await context.SaveChangesAsync();
        return seeded;
    }

    // Albums with an owned edition of the publisher, in the collection, others with an edition a purchase intent targets, and
    // series of which it is the publisher, in the collection.
    private static async Task<(List<Guid>, List<Guid>)> PublishedBy(
        BdthequeDbContext context, Guid publisherId, Guid? collectionId, int owned, int wished, int series)
    {
        var publisher = await context.Publishers.Include(p => p.Collections).SingleAsync(p => p.Id == publisherId);
        var collection = publisher.Collections.SingleOrDefault(c => c.Id == collectionId);
        var albumIds = Enumerable.Range(0, owned + wished).Select(index =>
        {
            var album = new Album($"Album {Guid.NewGuid():N}", null);
            var edition = new Edition(album, publisher);
            edition.SetPublisher(publisher, collection);
            if (index < owned)
                album.RecordAcquisition(edition, AcquisitionMode.Purchase);
            else
                album.AddPurchaseIntent(edition);
            context.Albums.Add(album);
            return album.Id;
        }).ToList();
        var seriesIds = Enumerable.Range(0, series).Select(_ =>
        {
            var oneSeries = new Series($"Série {Guid.NewGuid():N}");
            oneSeries.SetTemplate(publisher, collection);
            context.Series.Add(oneSeries);
            return oneSeries.Id;
        }).ToList();
        return (albumIds, seriesIds);
    }
}
