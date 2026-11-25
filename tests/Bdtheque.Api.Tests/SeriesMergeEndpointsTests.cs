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
using ContractEnums = Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The merge of two series (<c>/admin/series/merge</c>, fonctionnel/regles-metier/fusion-des-fiches.md): the series that remains
/// keeps each data chosen, the publisher and its collection together, and the genres, universes and contributions retained of
/// both; the albums of both are its albums.
/// </summary>
public sealed class SeriesMergeEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SeriesMergeEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task SeriesMergePreview_PresentsBothSeriesAndCountsTheirAlbums()
    {
        var seeded = await SeedAsync();

        var preview = await _client.GetFromJsonAsync<SeriesMergePreview>($"/admin/series/merge?first={seeded.First}&second={seeded.Second}");

        Assert.Equal((seeded.First, "Astérix", "Asterix (manuelle)"), (preview!.First.Id, preview.First.Title, preview.First.ManualSortKey));
        Assert.Equal((seeded.Second, "Asterix", null), (preview.Second.Id, preview.Second.Title, preview.Second.ManualSortKey));
        Assert.Equal((seeded.FirstVersion, seeded.SecondVersion), (preview.First.Version, preview.Second.Version));
        Assert.Equal((seeded.Publisher, "Pilote"), (preview.First.Publisher!.Id, preview.First.PublisherCollection!.Name));
        Assert.Null(preview.Second.Publisher);
        Assert.Equal([seeded.Aventure], preview.First.Genres.Select(g => g.Id));
        Assert.Equal([seeded.Aventure, seeded.Humour], preview.Second.Genres.Select(g => g.Id));
        Assert.Equal([seeded.Gaule], preview.Second.Universes.Select(u => u.Universe.Id));
        Assert.Equal(
            [(seeded.Goscinny, ContractEnums.ContributionRole.Scenarist), (seeded.Uderzo, ContractEnums.ContributionRole.Illustrator)],
            preview.First.Contributions.Select(c => (c.Author.Id, c.Role)));
        Assert.Equal([new MergeLinkCount(ContractEnums.EntityKind.Album, 3, 2, 1, 0)], preview.Links);
    }

    [Fact]
    public async Task SeriesMergePreview_OfAnUnknownSeries_IsAFunctionalError()
    {
        var seeded = await SeedAsync();

        var response = await _client.GetAsync($"/admin/series/merge?first={seeded.First}&second={Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task SeriesMerge_KeepsEachDataChosen_AndTheAlbumsAndContributionsRetainedOfBoth()
    {
        var seeded = await SeedAsync();

        // Of the second series: its title, its sort key (following the title), its status, its summary and its colour; the rest
        // of the first one, more data being kept of it: the first one remains.
        var response = await _client.PostAsJsonAsync("/admin/series/merge", Request(seeded, MergeSide.First) with
        {
            Title = MergeSide.Second,
            SortKey = MergeSide.Second,
            Status = MergeSide.Second,
            Summary = MergeSide.Second,
            IsColor = MergeSide.Second,
            Genres = [new MergeElement(seeded.Humour, MergeSide.Second)],
            Universes = [new MergeElement(seeded.Gaule, MergeSide.First)],
            Contributions = [new MergeElement(seeded.UderzoOfFirst, MergeSide.Second), new MergeElement(seeded.MorrisOfSecond, MergeSide.Second)],
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(seeded.First, (await response.Content.ReadFromJsonAsync<MergeResult>())!.Id);
        await ProblemAssert.IsProblemAsync(await _client.GetAsync($"/admin/series/{seeded.Second}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        var kept = (await _client.GetFromJsonAsync<SeriesForm>($"/admin/series/{seeded.First}"))!;
        Assert.Equal(("Asterix", null, ContractEnums.SeriesStatus.Completed, "Le résumé de la seconde."),
            (kept.Content.Title, kept.Content.ManualSortKey, kept.Content.Status, kept.Content.Summary));
        // The sort key follows the title kept.
        Assert.Equal("Asterix", kept.SortKey);
        Assert.Equal((12, true), (kept.Content.TheoreticalVolumeCount, kept.Content.IsComplete));
        Assert.Equal((false, ContractEnums.EditionFormat.Large), (kept.Content.EditionTemplate.IsColor, kept.Content.EditionTemplate.Format));
        Assert.Equal((seeded.Publisher, "Pilote"), (kept.References.Publisher!.Id, kept.References.PublisherCollection!.Name));
        Assert.Equal([seeded.Aventure, seeded.Humour], kept.References.Genres.Select(g => g.Id));
        Assert.Empty(kept.References.Universes);

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.Equal(3, await context.Albums.CountAsync(a => a.SeriesId == seeded.First));
        // Goscinny, a credit of both, once; Uderzo discarded; Morris retained of the other series, the same contribution.
        var contributions = await context.Contributions.Where(c => c.SeriesId == seeded.First).ToListAsync();
        Assert.Equivalent(
            new[] { (seeded.GoscinnyOfFirst, seeded.Goscinny), (seeded.MorrisOfSecond, seeded.Morris) },
            contributions.Select(c => (c.Id, c.AuthorId)));
    }

    [Fact]
    public async Task SeriesMerge_RecordsTheSeriesThatRemainsOnce_TheOtherDeleted_AndEachAlbumMoved()
    {
        var seeded = await SeedAsync();

        var response = await _client.PostAsJsonAsync("/admin/series/merge", Request(seeded, MergeSide.First) with
        {
            Title = MergeSide.Second,
            Contributions = [new MergeElement(seeded.UderzoOfFirst, MergeSide.First), new MergeElement(seeded.MorrisOfSecond, MergeSide.First)],
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var entries = await context.ChangeLog.Where(e => e.CreatedAt > seeded.SeededAt).ToListAsync();
        var keptEntry = Assert.Single(entries, e => e.RecordId == seeded.First);
        Assert.Equal(ChangeLogOperation.Modification, keptEntry.Operation);
        Assert.Contains(keptEntry.Changes!, c => c.Attribute == ChangeLogAttribute.Title);
        Assert.Contains(keptEntry.Changes!, c => c.Attribute == ChangeLogAttribute.Genre && c.Before is null);
        Assert.Equal(ChangeLogOperation.Deletion, Assert.Single(entries, e => e.RecordId == seeded.Second).Operation);
        Assert.Equal(1, entries.Count(e => e.RecordType == RecordType.Album && e.RecordId == seeded.AlbumOfSecond));
    }

    [Fact]
    public async Task SeriesMerge_RedirectsTheSavedSearchesOfTheOtherSeriesAndOfItsDuplicates()
    {
        var seeded = await SeedAsync();
        var ofSeries = await CreateSearchAsync(ContractEnums.EntityKind.Album, new SearchLink(
            "series", new SearchCriterion("record", SearchComparison.Is, [seeded.Second.ToString()])));
        var ofDuplicate = await CreateSearchAsync(ContractEnums.EntityKind.Contribution, Designating(seeded.GoscinnyOfSecond));
        var ofRetained = await CreateSearchAsync(ContractEnums.EntityKind.Contribution, Designating(seeded.MorrisOfSecond));

        var response = await _client.PostAsJsonAsync("/admin/series/merge", Request(seeded, MergeSide.First));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([seeded.First], await DesignatedAsync(ofSeries.Id));
        Assert.Equal([seeded.GoscinnyOfFirst], await DesignatedAsync(ofDuplicate.Id));
        Assert.Equal([seeded.MorrisOfSecond], await DesignatedAsync(ofRetained.Id));
    }

    [Fact]
    public async Task SeriesMerge_DiscardingAContributionASavedSearchDesignates_IsABusinessErrorAndMergesNothing()
    {
        var seeded = await SeedAsync();
        await CreateSearchAsync(ContractEnums.EntityKind.Contribution, Designating(seeded.MorrisOfSecond));

        var response = await _client.PostAsJsonAsync("/admin/series/merge", Request(seeded, MergeSide.First) with
        {
            Contributions = [new MergeElement(seeded.UderzoOfFirst, MergeSide.First), new MergeElement(seeded.MorrisOfSecond, MergeSide.First)],
        });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SavedSearchDesignatedRecordRemoved);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/series/{seeded.Second}")).StatusCode);
    }

    [Fact]
    public async Task SeriesMerge_FromAStaleVersion_IsAFunctionalErrorAndMergesNothing()
    {
        var seeded = await SeedAsync();
        var second = (await _client.GetFromJsonAsync<SeriesForm>($"/admin/series/{seeded.Second}"))!;
        (await _client.PutAsJsonAsync($"/admin/series/{seeded.Second}",
            new UpdateSeriesRequest(second.Content with { Summary = "Modifiée entre-temps." }, second.Version))).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/admin/series/merge", Request(seeded, MergeSide.First));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/series/{seeded.Second}")).StatusCode);
    }

    // Every data of the same side, the elements held by one series only of their own side: all retained.
    private static SeriesMergeRequest Request(Seeded seeded, MergeSide side) => new(
        new MergedRecord(seeded.First, seeded.FirstVersion), new MergedRecord(seeded.Second, seeded.SecondVersion),
        side, side, side, side, side, side, side, side, side, side, side, side, side, side, side, side, side,
        [new MergeElement(seeded.Humour, MergeSide.Second)], [new MergeElement(seeded.Gaule, MergeSide.Second)],
        [new MergeElement(seeded.UderzoOfFirst, MergeSide.First), new MergeElement(seeded.MorrisOfSecond, MergeSide.Second)]);

    private static SearchCriterion Designating(Guid contribution) => new("record", SearchComparison.Is, [contribution.ToString()]);

    private sealed record Seeded(
        Guid First, uint FirstVersion, Guid Second, uint SecondVersion, Guid Goscinny, Guid Uderzo, Guid Morris, Guid GoscinnyOfFirst,
        Guid UderzoOfFirst, Guid GoscinnyOfSecond, Guid MorrisOfSecond, Guid Aventure, Guid Humour, Guid Gaule, Guid Publisher, Guid AlbumOfSecond,
        DateTimeOffset SeededAt);

    // « Astérix », manual sort key, Dargaud and Pilote, Aventure, Goscinny and Uderzo, two albums; « Asterix », Aventure and
    // Humour, in Gaule, Goscinny and Morris, one album.
    private async Task<Seeded> SeedAsync()
    {
        var marker = Guid.NewGuid().ToString("N");
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var goscinny = new Author(null, null, $"Goscinny {marker}");
        var uderzo = new Author($"Uderzo {marker}", "Albert", null);
        var morris = new Author(null, null, $"Morris {marker}");
        var aventure = new Genre($"Aventure {marker}");
        var humour = new Genre($"Humour {marker}");
        var gaule = new Universe($"Gaule {marker}");
        var publisher = new Publisher($"Dargaud {marker}");
        var pilote = publisher.AddCollection("Pilote");

        var first = new Series("Astérix");
        first.SetSortKey("Asterix (manuelle)");
        first.SetTheoreticalVolumeCount(12);
        first.SetComplete(true);
        first.SetStatus(SeriesStatus.InProgress);
        first.SetSummary("Le résumé de la première.");
        first.SetTemplate(publisher, pilote);
        first.SetTemplateFormat(EditionFormat.Large);
        first.SetTemplateIsColor(true);
        first.AddGenre(aventure);
        first.SetTemplateContributions([(goscinny, ContributionRole.Scenarist), (uderzo, ContributionRole.Illustrator)]);
        var second = new Series("Asterix");
        second.SetStatus(SeriesStatus.Completed);
        second.SetSummary("Le résumé de la seconde.");
        second.SetTemplateIsColor(false);
        second.AddGenre(aventure);
        second.AddGenre(humour);
        second.AddUniverse(gaule);
        second.SetTemplateContributions([(goscinny, ContributionRole.Scenarist), (morris, ContributionRole.Colorist)]);
        var albumOfSecond = new Album(null, second);
        context.AddRange(goscinny, uderzo, morris, aventure, humour, gaule, publisher, first, second,
            new Album("Le Tour de Gaule", first), new Album(null, first), albumOfSecond);
        await context.SaveChangesAsync();

        var firstForm = (await _client.GetFromJsonAsync<SeriesForm>($"/admin/series/{first.Id}"))!;
        var secondForm = (await _client.GetFromJsonAsync<SeriesForm>($"/admin/series/{second.Id}"))!;
        var seededAt = await context.ChangeLog.MaxAsync(e => e.CreatedAt);
        return new Seeded(
            first.Id, firstForm.Version, second.Id, secondForm.Version, goscinny.Id, uderzo.Id, morris.Id,
            first.TemplateContributions.Single(c => c.AuthorId == goscinny.Id).Id, first.TemplateContributions.Single(c => c.AuthorId == uderzo.Id).Id,
            second.TemplateContributions.Single(c => c.AuthorId == goscinny.Id).Id, second.TemplateContributions.Single(c => c.AuthorId == morris.Id).Id,
            aventure.Id, humour.Id, gaule.Id, publisher.Id, albumOfSecond.Id, seededAt);
    }

    private async Task<SavedSearchForm> CreateSearchAsync(ContractEnums.EntityKind entity, SearchCondition criteria)
    {
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(
            $"Recherche {Guid.NewGuid():N}", entity, criteria, []));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SavedSearchForm>())!;
    }

    private async Task<List<Guid>> DesignatedAsync(Guid search)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        return (await context.SavedSearches.SingleAsync(s => s.Id == search)).Designations.Select(d => d.RecordId).ToList();
    }
}
