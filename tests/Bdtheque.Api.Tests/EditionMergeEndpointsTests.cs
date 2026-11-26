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
/// The merge of two editions (<c>/admin/editions/merge</c>, fonctionnel/regles-metier/fusion-des-fiches.md): the edition that
/// remains belongs to the album chosen and keeps each data chosen, the publisher and its collection together, the acquisition
/// whole, and the visuals retained of both, with their identity; its intent is withdrawn once it is owned, one kept otherwise.
/// </summary>
public sealed class EditionMergeEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditionMergeEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task EditionMergePreview_PresentsBothEditionsWithTheirAlbumAndVisuals()
    {
        var seeded = await SeedAsync();

        var preview = await PreviewAsync(seeded.First, seeded.Second);

        Assert.Equal((seeded.First, seeded.FirstAlbum, "Spirou à New York"), (preview.First.Id, preview.First.Album.Id, preview.First.Album.Title));
        Assert.Equal((seeded.Publisher, seeded.Collection), (preview.First.Publisher.Id, preview.First.PublisherCollection!.Id));
        Assert.Equal((ContractEnums.AcquisitionMode.Purchase, 12.5m, "EUR"), (preview.First.AcquisitionMode, preview.First.AcquisitionAmount, preview.First.AcquisitionCurrency));
        Assert.Equal((seeded.SecondAlbum, ContractEnums.AcquisitionMode.Gift, null), (preview.Second.Album.Id, preview.Second.AcquisitionMode, preview.Second.PublisherCollection));
        Assert.Equal([seeded.FirstCover], preview.First.Visuals.Select(v => v.Id));
        Assert.Equal([seeded.SecondCover, seeded.SecondPlate], preview.Second.Visuals.Select(v => v.Id));
        Assert.Equal((seeded.FirstVersion, seeded.SecondVersion), (preview.First.Version, preview.Second.Version));
        // Two owned editions: nothing passes to the edition that remains, nothing is withdrawn, whichever acquisition is kept.
        Assert.All(preview.Outcomes, o =>
        {
            Assert.Equal([new MergeLinkCount(ContractEnums.EntityKind.PurchaseIntent, 0, 0, 0, 0)], o.Links);
            Assert.Empty(o.WithdrawnPurchaseIntents);
        });
    }

    [Fact]
    public async Task EditionMergePreview_OfAnEditionTargeted_SaysItsIntentWithdrawn_OnlyForTheAcquisitionOfTheOwnedOne()
    {
        var (owned, targeted) = await SeedOwnedAndTargetedAsync();

        var preview = await PreviewAsync(owned, targeted);

        Assert.True(preview.Second.IsTargeted);
        var ofOwned = preview.Outcomes.Single(o => o.Acquisition == MergeSide.First);
        Assert.Equal([MergeSide.Second], ofOwned.WithdrawnPurchaseIntents);
        Assert.Equal(0, ofOwned.Links.Single().Total);
        var ofTargeted = preview.Outcomes.Single(o => o.Acquisition == MergeSide.Second);
        Assert.Empty(ofTargeted.WithdrawnPurchaseIntents);
        Assert.Equal(new MergeLinkCount(ContractEnums.EntityKind.PurchaseIntent, 1, 0, 1, 0), ofTargeted.Links.Single());
    }

    [Fact]
    public async Task EditionMergePreview_OfAnUnknownEdition_IsAFunctionalError()
    {
        var seeded = await SeedAsync();

        var response = await _client.GetAsync($"/admin/editions/merge?first={seeded.First}&second={Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task EditionMerge_KeepsEachDataChosen_InTheAlbumChosen_AndTheVisualsRetainedOfBoth()
    {
        var seeded = await SeedAsync();

        // Of the second edition: its album, its acquisition, its condition and its plate; the rest of the first one, more data being
        // kept of it: the first one remains.
        var response = await _client.PostAsJsonAsync("/admin/editions/merge", Request(seeded, MergeSide.First) with
        {
            Album = MergeSide.Second,
            Acquisition = MergeSide.Second,
            Condition = MergeSide.Second,
            Visuals = [new MergeElement(seeded.FirstCover, MergeSide.First), new MergeElement(seeded.SecondCover, MergeSide.First), new MergeElement(seeded.SecondPlate, MergeSide.Second)],
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(seeded.First, (await response.Content.ReadFromJsonAsync<MergeResult>())!.Id);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.Editions.AnyAsync(e => e.Id == seeded.Second));
        var kept = await context.Editions.AsNoTracking().Include(e => e.Visuals).SingleAsync(e => e.Id == seeded.First);
        Assert.Equal(seeded.SecondAlbum, kept.AlbumId);
        Assert.Equal((seeded.Publisher, seeded.Collection, 1987, "2-8001-1234-0"), (kept.PublisherId, kept.PublisherCollectionId, kept.PublicationYear, kept.Isbn));
        Assert.Equal((AcquisitionMode.Gift, null, true), (kept.AcquisitionMode, kept.AcquisitionAmount, kept.IsFree));
        Assert.Equal(seeded.SecondEnteredCollectionAt, kept.EnteredCollectionAt);
        Assert.Equal(EditionCondition.Good, kept.Condition);
        Assert.Equal([seeded.FirstCover, seeded.SecondPlate], kept.GetOrderedVisuals().Select(v => v.Id));
        Assert.False(await context.EditionVisuals.AnyAsync(v => v.Id == seeded.SecondCover));
    }

    [Fact]
    public async Task EditionMerge_PassesTheSpotlightOfTheOtherEdition_AndRedirectsItsSavedSearches()
    {
        var seeded = await SeedAsync();
        var day = new DateOnly(2000, 1, 1).AddDays(Random.Shared.Next(100_000));
        await using (var seeding = _factory.Services.CreateAsyncScope())
            await seeding.ServiceProvider.GetRequiredService<BdthequeDbContext>().InsertSpotlightAsync(day, seeded.Second);
        var search = await CreateSearchAsync(ContractEnums.EntityKind.Edition, new SearchCriterion("record", SearchComparison.Is, [seeded.Second.ToString()]));

        var response = await _client.PostAsJsonAsync("/admin/editions/merge", Request(seeded, MergeSide.First));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.Equal(seeded.First, (await context.Set<EditionSpotlight>().SingleAsync(s => s.Day == day)).EditionId);
        Assert.Equal([seeded.First], (await context.SavedSearches.SingleAsync(s => s.Id == search)).Designations.Select(d => d.RecordId));
    }

    [Fact]
    public async Task EditionMerge_RecordsTheEditionThatRemainsOnce_ItsAlbumChanged_AndTheOtherDeleted()
    {
        var seeded = await SeedAsync();

        var response = await _client.PostAsJsonAsync("/admin/editions/merge", Request(seeded, MergeSide.First) with { Album = MergeSide.Second });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var entries = await context.ChangeLog.Where(e => e.CreatedAt > seeded.SeededAt).ToListAsync();
        var keptEntry = Assert.Single(entries, e => e.RecordId == seeded.First);
        Assert.Contains(keptEntry.Changes!, c => c.Attribute == ChangeLogAttribute.Album);
        Assert.Contains(keptEntry.Changes!, c => c.Attribute == ChangeLogAttribute.Visual && c.Before is null);
        Assert.Equal(ChangeLogOperation.Deletion, Assert.Single(entries, e => e.RecordId == seeded.Second).Operation);
    }

    [Fact]
    public async Task EditionMerge_KeepingTheAcquisitionOfTheOwnedEdition_WithdrawsTheIntentOfTheOther()
    {
        var (owned, targeted) = await SeedOwnedAndTargetedAsync();
        var preview = await PreviewAsync(owned, targeted);

        var response = await _client.PostAsJsonAsync("/admin/editions/merge", Request(preview, MergeSide.Second) with { Acquisition = MergeSide.First });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var kept = await context.Editions.AsNoTracking().SingleAsync(e => e.Id == targeted);
        Assert.Equal(AcquisitionMode.Purchase, kept.AcquisitionMode);
        Assert.NotNull(kept.EnteredCollectionAt);
        Assert.False(await context.PurchaseIntents.AnyAsync(p => p.AlbumId == kept.AlbumId));
    }

    [Fact]
    public async Task EditionMerge_KeepingTheAcquisitionOfTheEditionTargeted_KeepsItsIntentOnTheEditionThatRemains()
    {
        var (owned, targeted) = await SeedOwnedAndTargetedAsync();
        var preview = await PreviewAsync(owned, targeted);

        var response = await _client.PostAsJsonAsync("/admin/editions/merge", Request(preview, MergeSide.First) with { Acquisition = MergeSide.Second });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var kept = await context.Editions.AsNoTracking().SingleAsync(e => e.Id == owned);
        Assert.Equal((null, null), (kept.AcquisitionMode, kept.EnteredCollectionAt));
        Assert.Equal(owned, (await context.PurchaseIntents.SingleAsync(p => p.AlbumId == kept.AlbumId)).EditionId);
    }

    [Fact]
    public async Task EditionMerge_FromAStaleVersion_IsAFunctionalErrorAndMergesNothing()
    {
        var seeded = await SeedAsync();
        var album = (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{seeded.SecondAlbum}"))!;
        (await _client.PutAsJsonAsync($"/admin/albums/{seeded.SecondAlbum}",
            new UpdateAlbumRequest(album.Content with { Summary = "Modifié entre-temps." }, album.Version))).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/admin/editions/merge", Request(seeded, MergeSide.First));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        await using var scope = _factory.Services.CreateAsyncScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().Editions.AnyAsync(e => e.Id == seeded.Second));
    }

    [Fact]
    public async Task EditionMerge_LeavingAPriceWithoutReferenceDate_IsABusinessErrorAndMergesNothing()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var publisher = new Publisher($"Dupuis {Guid.NewGuid():N}");
        var dated = new Album("Spirou à Moscou", null);
        dated.SetFirstPublicationDate(1990, null);
        var priced = new Edition(dated, publisher);
        dated.RecordAcquisition(priced, new EditionAcquisition(AcquisitionMode.Purchase, null, 10m, "EUR", false, null, null));
        var undated = new Album("Spirou a Moscou", null);
        var other = new Edition(undated, publisher);
        undated.RecordAcquisition(other, AcquisitionMode.Gift);
        context.AddRange(dated, undated);
        await context.SaveChangesAsync();
        var preview = await PreviewAsync(priced.Id, other.Id);

        // The price of the first edition, dated by its album only, in the album of the second one, without date.
        var response = await _client.PostAsJsonAsync("/admin/editions/merge", Request(preview, MergeSide.First) with { Album = MergeSide.Second });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionAcquisitionPriceReferenceDateRequired);
        Assert.True(await context.Editions.AsNoTracking().AnyAsync(e => e.Id == other.Id));
    }

    // Every data of the same side, the visuals of their own side: all retained.
    private static EditionMergeRequest Request(Seeded seeded, MergeSide side) => Request(
        new MergedRecord(seeded.First, seeded.FirstVersion), new MergedRecord(seeded.Second, seeded.SecondVersion), side,
        [new MergeElement(seeded.FirstCover, MergeSide.First), new MergeElement(seeded.SecondCover, MergeSide.Second), new MergeElement(seeded.SecondPlate, MergeSide.Second)]);

    private static EditionMergeRequest Request(EditionMergePreview preview, MergeSide side) => Request(
        new MergedRecord(preview.First.Id, preview.First.Version), new MergedRecord(preview.Second.Id, preview.Second.Version), side, []);

    private static EditionMergeRequest Request(MergedRecord first, MergedRecord second, MergeSide side, IReadOnlyList<MergeElement> visuals) => new(
        first, second, side, side, side, side, side, side, side, side, side, side, side, side, side, side, side, side, side, visuals);

    private async Task<EditionMergePreview> PreviewAsync(Guid first, Guid second) =>
        (await _client.GetFromJsonAsync<EditionMergePreview>($"/admin/editions/merge?first={first}&second={second}"))!;

    private sealed record Seeded(
        Guid First, Guid FirstAlbum, uint FirstVersion, Guid Second, Guid SecondAlbum, uint SecondVersion, Guid Publisher, Guid Collection,
        Guid FirstCover, Guid SecondCover, Guid SecondPlate, DateTimeOffset? SecondEnteredCollectionAt, DateTimeOffset SeededAt);

    // « Spirou à New York », 1987, an edition bought at 12,50 €, in the collection « Spirou », ISBN 2-8001-1234-0, its cover;
    // « Spirou a New-York », an edition offered, free, in good condition, its cover and a plate.
    private async Task<Seeded> SeedAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var publisher = new Publisher($"Dupuis {Guid.NewGuid():N}");
        var collection = publisher.AddCollection("Spirou");
        var firstAlbum = new Album("Spirou à New York", null);
        firstAlbum.SetFirstPublicationDate(1987, null);
        var first = new Edition(firstAlbum, publisher);
        first.SetPublisher(publisher, collection);
        first.SetIsbn("2-8001-1234-0");
        first.SetPublicationYear(1987);
        firstAlbum.RecordAcquisition(first, new EditionAcquisition(AcquisitionMode.Purchase, new DateOnly(2004, 3, 12), 12.5m, "EUR", false, null, null));
        var firstCover = first.AppendVisual(VisualType.Cover, $"{Guid.NewGuid():N}.jpg", TestPalette.Any);
        var secondAlbum = new Album("Spirou a New-York", null);
        var second = new Edition(secondAlbum, publisher);
        second.SetCondition(EditionCondition.Good);
        secondAlbum.RecordAcquisition(second, new EditionAcquisition(AcquisitionMode.Gift, null, null, null, true, null, null));
        var secondCover = second.AppendVisual(VisualType.Cover, $"{Guid.NewGuid():N}.jpg", TestPalette.Any);
        var secondPlate = second.AppendVisual(VisualType.Plate, $"{Guid.NewGuid():N}.jpg", TestPalette.Any);
        context.AddRange(publisher, firstAlbum, secondAlbum);
        await context.SaveChangesAsync();

        var firstForm = (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{firstAlbum.Id}"))!;
        var secondForm = (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{secondAlbum.Id}"))!;
        return new Seeded(
            first.Id, firstAlbum.Id, firstForm.Version, second.Id, secondAlbum.Id, secondForm.Version, publisher.Id, collection.Id,
            firstCover.Id, secondCover.Id, secondPlate.Id, second.EnteredCollectionAt, await context.ChangeLog.MaxAsync(e => e.CreatedAt));
    }

    // An owned edition, and an edition of the same album targeted by an intent.
    private async Task<(Guid Owned, Guid Targeted)> SeedOwnedAndTargetedAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var publisher = new Publisher($"Dupuis {Guid.NewGuid():N}");
        var album = new Album("Spirou et les héritiers", null);
        var owned = new Edition(album, publisher);
        album.RecordAcquisition(owned, AcquisitionMode.Purchase);
        var targeted = new Edition(album, publisher);
        album.AddPurchaseIntent(targeted);
        context.Add(album);
        await context.SaveChangesAsync();
        return (owned.Id, targeted.Id);
    }

    private async Task<Guid> CreateSearchAsync(ContractEnums.EntityKind entity, SearchCondition criteria)
    {
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent($"Recherche {Guid.NewGuid():N}", entity, criteria, []));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SavedSearchForm>())!.Id;
    }
}
