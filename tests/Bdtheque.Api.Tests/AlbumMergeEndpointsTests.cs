using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Errors;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Bdtheque.Infrastructure;
using Bdtheque.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ContractEnums = Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The merge of two albums (<c>/admin/albums/merge</c>, fonctionnel/regles-metier/fusion-des-fiches.md): the album that remains
/// keeps each data chosen, the type and the volumes an omnibus covers together, and the genres, universes and contributions
/// retained of both; the editions of both are its editions, with their intents, an intent on the whole album being withdrawn once
/// it has one.
/// </summary>
public sealed class AlbumMergeEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AlbumMergeEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task AlbumMergePreview_PresentsBothAlbumsAndCountsTheirEditionsAndIntents()
    {
        var seeded = await SeedAsync();

        var preview = (await _client.GetFromJsonAsync<AlbumMergePreview>($"/admin/albums/merge?first={seeded.First}&second={seeded.Second}"))!;

        Assert.Equal((seeded.First, "La Marque jaune", null), (preview.First.Id, preview.First.Title, preview.First.ManualSortKey));
        Assert.Equal((seeded.Series, "Blake et Mortimer"), (preview.First.Series!.Id, preview.First.Series.Title));
        Assert.Equal("Blake et Mortimer", preview.First.SeriesSortKey);
        Assert.Equal(("La marque jaune", "Marque jaune (manuelle)", null), (preview.Second.Title, preview.Second.ManualSortKey, preview.Second.Series));
        Assert.Equal((ContractEnums.AlbumType.Omnibus, 1, 2), (preview.Second.Type, preview.Second.StartVolumeNumber, preview.Second.EndVolumeNumber));
        Assert.Equal((seeded.FirstVersion, seeded.SecondVersion), (preview.First.Version, preview.Second.Version));
        Assert.Equal([seeded.Aventure], preview.First.Genres.Select(g => g.Id));
        Assert.Equal([seeded.Londres], preview.Second.Universes.Select(u => u.Universe.Id));
        Assert.Equal(
            [(seeded.Jacobs, ContractEnums.ContributionRole.Scenarist), (seeded.Jacobs, ContractEnums.ContributionRole.Illustrator)],
            preview.First.Contributions.Select(c => (c.Author.Id, c.Role)));
        // The edition targeted counts as its intent.
        Assert.Equal(
            [new MergeLinkCount(ContractEnums.EntityKind.Edition, 2, 1, 1, 0), new MergeLinkCount(ContractEnums.EntityKind.PurchaseIntent, 1, 1, 0, 0)],
            preview.Links);
        Assert.Empty(preview.WithdrawnPurchaseIntents);
    }

    [Fact]
    public async Task AlbumMergePreview_OfAnIntentOnTheWholeAlbumAndAnAlbumWithAnEdition_SaysTheIntentWithdrawn()
    {
        var (targeted, owned) = await SeedIntentAndEditionAsync();

        var preview = (await _client.GetFromJsonAsync<AlbumMergePreview>($"/admin/albums/merge?first={targeted}&second={owned}"))!;

        Assert.Equal([MergeSide.First], preview.WithdrawnPurchaseIntents);
        // The intent withdrawn does not pass to the album.
        Assert.Equal(new MergeLinkCount(ContractEnums.EntityKind.PurchaseIntent, 0, 0, 0, 0), preview.Links[1]);
    }

    [Fact]
    public async Task AlbumMergePreview_OfAnIntentOnTheWholeAlbumBothHold_CountsItCommonToBoth()
    {
        var (first, second) = await SeedIntentsOnTheWholeAlbumsAsync();

        var preview = (await _client.GetFromJsonAsync<AlbumMergePreview>($"/admin/albums/merge?first={first}&second={second}"))!;

        Assert.Empty(preview.WithdrawnPurchaseIntents);
        Assert.Equal(new MergeLinkCount(ContractEnums.EntityKind.PurchaseIntent, 1, 1, 1, 1), preview.Links[1]);
    }

    [Fact]
    public async Task AlbumMergePreview_OfAnUnknownAlbum_IsAFunctionalError()
    {
        var seeded = await SeedAsync();

        var response = await _client.GetAsync($"/admin/albums/merge?first={seeded.First}&second={Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task AlbumMerge_KeepsEachDataChosen_AndTheEditionsAndContributionsRetainedOfBoth()
    {
        var seeded = await SeedAsync();

        // Of the second album: its type with its volumes, its first publication and its summary; the rest of the first one, more
        // data being kept of it: the first one remains.
        var response = await _client.PostAsJsonAsync("/admin/albums/merge", Request(seeded, MergeSide.First) with
        {
            Type = MergeSide.Second,
            FirstPublicationDate = MergeSide.Second,
            Summary = MergeSide.Second,
            Genres = [new MergeElement(seeded.Policier, MergeSide.Second)],
            Universes = [new MergeElement(seeded.Londres, MergeSide.First)],
            Contributions = [new MergeElement(seeded.JacobsIllustratorOfFirst, MergeSide.Second), new MergeElement(seeded.ColoristOfSecond, MergeSide.Second)],
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(seeded.First, (await response.Content.ReadFromJsonAsync<MergeResult>())!.Id);
        await ProblemAssert.IsProblemAsync(await _client.GetAsync($"/admin/albums/{seeded.Second}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        var kept = (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{seeded.First}"))!;
        Assert.Equal(("La Marque jaune", null, seeded.Series), (kept.Content.Title, kept.Content.ManualSortKey, kept.Content.SeriesId));
        Assert.Equal((ContractEnums.AlbumType.Omnibus, 1, 2, 6), (kept.Content.Type, kept.Content.StartVolumeNumber, kept.Content.EndVolumeNumber, kept.Content.VolumeNumber));
        Assert.Equal((1987, 3, "Le résumé du second."), (kept.Content.FirstPublicationYear, kept.Content.FirstPublicationMonth, kept.Content.Summary));
        Assert.Equal([seeded.Aventure, seeded.Policier], kept.References.Genres.Select(g => g.Id));
        Assert.Empty(kept.References.Universes);

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        // Each edition keeps its identity, the intent with the edition it targets.
        Assert.Equivalent(
            new[] { seeded.OwnedOfFirst, seeded.TargetedOfFirst, seeded.OwnedOfSecond },
            await context.Editions.Where(e => e.AlbumId == seeded.First).Select(e => e.Id).ToListAsync());
        Assert.Equal(seeded.TargetedOfFirst, (await context.PurchaseIntents.SingleAsync(p => p.AlbumId == seeded.First)).EditionId);
        // Jacobs at the script, a credit of both, once; Jacobs at the drawing discarded; the colourist retained of the other album,
        // the same contribution.
        Assert.Equivalent(
            new[] { seeded.JacobsScenaristOfFirst, seeded.ColoristOfSecond },
            await context.Contributions.Where(c => c.AlbumId == seeded.First).Select(c => c.Id).ToListAsync());
    }

    [Fact]
    public async Task AlbumMerge_WithdrawsTheIntentOnTheWholeAlbum_OnceTheAlbumHasAnEdition()
    {
        var (targeted, owned) = await SeedIntentAndEditionAsync();
        var preview = (await _client.GetFromJsonAsync<AlbumMergePreview>($"/admin/albums/merge?first={targeted}&second={owned}"))!;

        var response = await _client.PostAsJsonAsync("/admin/albums/merge", new AlbumMergeRequest(
            new MergedRecord(targeted, preview.First.Version), new MergedRecord(owned, preview.Second.Version),
            MergeSide.First, MergeSide.First, MergeSide.First, MergeSide.First, MergeSide.First, MergeSide.First, MergeSide.First,
            MergeSide.First, MergeSide.First, MergeSide.First, [], [], []));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.Equal(1, await context.Editions.CountAsync(e => e.AlbumId == targeted));
        Assert.False(await context.PurchaseIntents.AnyAsync(p => p.AlbumId == targeted || p.AlbumId == owned));
    }

    [Fact]
    public async Task AlbumMerge_RecordsTheAlbumThatRemainsOnce_TheOtherDeleted_AndEachEditionAndIntentMoved()
    {
        var seeded = await SeedAsync();

        var response = await _client.PostAsJsonAsync("/admin/albums/merge", Request(seeded, MergeSide.Second));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var entries = await context.ChangeLog.Where(e => e.CreatedAt > seeded.SeededAt).ToListAsync();
        var keptEntry = Assert.Single(entries, e => e.RecordId == seeded.Second);
        Assert.Equal(ChangeLogOperation.Modification, keptEntry.Operation);
        Assert.Contains(keptEntry.Changes!, c => c.Attribute == ChangeLogAttribute.Contribution && c.Before is null);
        Assert.Equal(ChangeLogOperation.Deletion, Assert.Single(entries, e => e.RecordId == seeded.First).Operation);
        foreach (var moved in new[] { seeded.OwnedOfFirst, seeded.TargetedOfFirst, seeded.IntentOfFirst })
        {
            var change = Assert.Single(Assert.Single(entries, e => e.RecordId == moved).Changes!);
            Assert.Equal(ChangeLogAttribute.Album, change.Attribute);
            Assert.Equal((seeded.First, seeded.Second), (((Domain.Entities.ReferenceValue)change.Before!).Id, ((Domain.Entities.ReferenceValue)change.After!).Id));
        }
    }

    [Fact]
    public async Task AlbumMerge_RedirectsTheSavedSearchesOfTheOtherAlbumAndOfItsDuplicates_AndKeepsThoseOfItsEditions()
    {
        var seeded = await SeedAsync();
        var ofAlbum = await CreateSearchAsync(ContractEnums.EntityKind.Edition, new SearchLink(
            "album", new SearchCriterion("record", SearchComparison.Is, [seeded.Second.ToString()])));
        var ofEdition = await CreateSearchAsync(ContractEnums.EntityKind.Edition, Designating(seeded.OwnedOfSecond));
        var ofDuplicate = await CreateSearchAsync(ContractEnums.EntityKind.Contribution, Designating(seeded.JacobsScenaristOfSecond));

        var response = await _client.PostAsJsonAsync("/admin/albums/merge", Request(seeded, MergeSide.First));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([seeded.First], await DesignatedAsync(ofAlbum.Id));
        Assert.Equal([seeded.OwnedOfSecond], await DesignatedAsync(ofEdition.Id));
        Assert.Equal([seeded.JacobsScenaristOfFirst], await DesignatedAsync(ofDuplicate.Id));
    }

    [Fact]
    public async Task AlbumMerge_DiscardingAContributionASavedSearchDesignates_IsABusinessErrorAndMergesNothing()
    {
        var seeded = await SeedAsync();
        await CreateSearchAsync(ContractEnums.EntityKind.Contribution, Designating(seeded.ColoristOfSecond));

        var response = await _client.PostAsJsonAsync("/admin/albums/merge", Request(seeded, MergeSide.First) with
        {
            Contributions = [new MergeElement(seeded.JacobsIllustratorOfFirst, MergeSide.First), new MergeElement(seeded.ColoristOfSecond, MergeSide.First)],
        });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SavedSearchDesignatedRecordRemoved);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/albums/{seeded.Second}")).StatusCode);
    }

    [Fact]
    public async Task AlbumMerge_KeepingNeitherTitleNorSeries_IsABusinessErrorAndMergesNothing()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var untitled = new Album(null, new Series("Blake et Mortimer"));
        var standalone = new Album("Le Secret de l'Espadon", null);
        context.AddRange(untitled, standalone);
        await context.SaveChangesAsync();
        var preview = (await _client.GetFromJsonAsync<AlbumMergePreview>($"/admin/albums/merge?first={untitled.Id}&second={standalone.Id}"))!;

        // The title of the first album, none, and the series of the second one, none: an album without series must have a title.
        var response = await _client.PostAsJsonAsync("/admin/albums/merge", new AlbumMergeRequest(
            new MergedRecord(untitled.Id, preview.First.Version), new MergedRecord(standalone.Id, preview.Second.Version),
            MergeSide.Second, MergeSide.First, MergeSide.First, MergeSide.First, MergeSide.First, MergeSide.First, MergeSide.First,
            MergeSide.First, MergeSide.First, MergeSide.First, [], [], []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AlbumTitleRequiredWithoutSeries);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/albums/{standalone.Id}")).StatusCode);
    }

    [Fact]
    public async Task AlbumMerge_KeepingNoTitle_FilesTheAlbumUnderItsSeries_WhateverSortKeyEnteredByHandIsChosen()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var series = new Series("Blake et Mortimer");
        var untitled = new Album(null, series);
        var titled = new Album("Le Secret de l'Espadon", series);
        titled.SetSortKey("Espadon (manuelle)");
        context.AddRange(untitled, titled);
        await context.SaveChangesAsync();
        var preview = (await _client.GetFromJsonAsync<AlbumMergePreview>($"/admin/albums/merge?first={untitled.Id}&second={titled.Id}"))!;

        // The title of the first album, none, and the sort key of the second one, entered by hand.
        var response = await _client.PostAsJsonAsync("/admin/albums/merge", new AlbumMergeRequest(
            new MergedRecord(untitled.Id, preview.First.Version), new MergedRecord(titled.Id, preview.Second.Version),
            MergeSide.First, MergeSide.First, MergeSide.Second, MergeSide.First, MergeSide.First, MergeSide.First, MergeSide.First,
            MergeSide.First, MergeSide.First, MergeSide.First, [], [], []));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var kept = (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{untitled.Id}"))!;
        Assert.Equal((null, null, null), (kept.Content.Title, kept.Content.ManualSortKey, kept.SortKey));
    }

    [Fact]
    public async Task AlbumMerge_FromAStaleVersion_IsAFunctionalErrorAndMergesNothing()
    {
        var seeded = await SeedAsync();
        var second = (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{seeded.Second}"))!;
        (await _client.PutAsJsonAsync($"/admin/albums/{seeded.Second}",
            new UpdateAlbumRequest(second.Content with { Summary = "Modifié entre-temps." }, second.Version))).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/admin/albums/merge", Request(seeded, MergeSide.First));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/albums/{seeded.Second}")).StatusCode);
    }

    // Every data of the same side, the elements held by one album only of their own side: all retained.
    private static AlbumMergeRequest Request(Seeded seeded, MergeSide side) => new(
        new MergedRecord(seeded.First, seeded.FirstVersion), new MergedRecord(seeded.Second, seeded.SecondVersion),
        side, side, side, side, side, side, side, side, side, side,
        [new MergeElement(seeded.Policier, MergeSide.Second)], [new MergeElement(seeded.Londres, MergeSide.Second)],
        [new MergeElement(seeded.JacobsIllustratorOfFirst, MergeSide.First), new MergeElement(seeded.ColoristOfSecond, MergeSide.Second)]);

    private static SearchCriterion Designating(Guid record) => new("record", SearchComparison.Is, [record.ToString()]);

    private sealed record Seeded(
        Guid First, uint FirstVersion, Guid Second, uint SecondVersion, Guid Series, Guid Jacobs, Guid JacobsScenaristOfFirst,
        Guid JacobsIllustratorOfFirst, Guid JacobsScenaristOfSecond, Guid ColoristOfSecond, Guid Aventure, Guid Policier, Guid Londres,
        Guid OwnedOfFirst, Guid TargetedOfFirst, Guid IntentOfFirst, Guid OwnedOfSecond, DateTimeOffset SeededAt);

    // « La Marque jaune », in « Blake et Mortimer », tome 6, 1956, Aventure, Jacobs at the script and the drawing, an owned edition
    // and an edition targeted by an intent; « La marque jaune », manual sort key, without series, omnibus of the volumes 1 to 2,
    // tome 6, March 1987, Aventure and Policier, in Londres, Jacobs at the script and a colourist, an owned edition.
    private async Task<Seeded> SeedAsync()
    {
        var marker = Guid.NewGuid().ToString("N");
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var jacobs = new Author($"Jacobs {marker}", "Edgar P.", null);
        var colorist = new Author($"De Moor {marker}", "Bob", null);
        var aventure = new Genre($"Aventure {marker}");
        var policier = new Genre($"Policier {marker}");
        var londres = new Universe($"Londres {marker}");
        var publisher = new Publisher($"Le Lombard {marker}");
        var series = new Series("Blake et Mortimer");

        var first = new Album("La Marque jaune", series, [(jacobs, ContributionRole.Scenarist), (jacobs, ContributionRole.Illustrator)]);
        first.SetVolumeNumber(6);
        first.SetFirstPublicationDate(1956, null);
        first.SetSummary("Le résumé du premier.");
        first.AddGenre(aventure);
        var ownedOfFirst = new Edition(first, publisher);
        first.RecordAcquisition(ownedOfFirst, AcquisitionMode.Purchase);
        var targetedOfFirst = new Edition(first, publisher);
        var intentOfFirst = first.AddPurchaseIntent(targetedOfFirst);

        var second = new Album("La marque jaune", null, [(jacobs, ContributionRole.Scenarist), (colorist, ContributionRole.Colorist)]);
        second.SetSortKey("Marque jaune (manuelle)");
        second.SetTypeAndVolumeRange(AlbumType.Omnibus, 1, 2);
        second.SetVolumeNumber(6);
        second.SetFirstPublicationDate(1987, 3);
        second.SetSummary("Le résumé du second.");
        second.AddGenre(aventure);
        second.AddGenre(policier);
        second.AddUniverse(londres);
        var ownedOfSecond = new Edition(second, publisher);
        second.RecordAcquisition(ownedOfSecond, AcquisitionMode.Gift);
        context.AddRange(jacobs, colorist, aventure, policier, londres, publisher, series, first, second);
        await context.SaveChangesAsync();

        var firstForm = (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{first.Id}"))!;
        var secondForm = (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{second.Id}"))!;
        var seededAt = await context.ChangeLog.MaxAsync(e => e.CreatedAt);
        return new Seeded(
            first.Id, firstForm.Version, second.Id, secondForm.Version, series.Id, jacobs.Id,
            first.Contributions.Single(c => c.Role == ContributionRole.Scenarist).Id, first.Contributions.Single(c => c.Role == ContributionRole.Illustrator).Id,
            second.Contributions.Single(c => c.Role == ContributionRole.Scenarist).Id, second.Contributions.Single(c => c.Role == ContributionRole.Colorist).Id,
            aventure.Id, policier.Id, londres.Id, ownedOfFirst.Id, targetedOfFirst.Id, intentOfFirst.Id, ownedOfSecond.Id, seededAt);
    }

    // An album targeted as a whole, and an album with an owned edition.
    private async Task<(Guid Targeted, Guid Owned)> SeedIntentAndEditionAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var targeted = new Album("Le Piège diabolique", null);
        targeted.AddPurchaseIntent();
        var owned = new Album("Le piège diabolique", null);
        owned.RecordAcquisition(new Edition(owned, new Publisher($"Dargaud {Guid.NewGuid():N}")), AcquisitionMode.Purchase);
        context.AddRange(targeted, owned);
        await context.SaveChangesAsync();
        return (targeted.Id, owned.Id);
    }

    // Two albums each targeted as a whole.
    private async Task<(Guid First, Guid Second)> SeedIntentsOnTheWholeAlbumsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var first = new Album("L'Énigme de l'Atlantide", null);
        first.AddPurchaseIntent();
        var second = new Album("L'énigme de l'Atlantide", null);
        second.AddPurchaseIntent();
        context.AddRange(first, second);
        await context.SaveChangesAsync();
        return (first.Id, second.Id);
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
