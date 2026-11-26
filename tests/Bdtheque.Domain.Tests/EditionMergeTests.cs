using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// The merge of two editions (fonctionnel/regles-metier/fusion-des-fiches.md): the edition that remains belongs to the album
/// chosen, takes the acquisition chosen, whole, keeps the visuals retained of both, with their identity, and an intent follows the
/// rules of the edition that remains: withdrawn once it is owned, one kept otherwise.
/// </summary>
public sealed class EditionMergeTests
{
    private static readonly Publisher Dupuis = new("Dupuis");

    private static readonly EditionAcquisition Bought =
        new(AcquisitionMode.Purchase, new DateOnly(2004, 3, 12), 12.5m, "EUR", false, 9.9m, "EUR");

    [Fact]
    public void MergeEditions_OfTheSameAlbum_RemovesTheOtherEdition_AndKeepsTheAcquisitionChosen()
    {
        var album = new Album("Spirou à New York", null);
        album.SetFirstPublicationDate(1987, null);
        var kept = new Edition(album, Dupuis);
        album.RecordAcquisition(kept, AcquisitionMode.Gift);
        var merged = new Edition(album, Dupuis);
        merged.SetPublicationYear(1987);
        album.RecordAcquisition(merged, Bought);

        Album.MergeEditions(kept, merged, album, 1987, merged, []);

        Assert.Same(kept, Assert.Single(album.Editions));
        Assert.Equal(
            (AcquisitionMode.Purchase, new DateOnly(2004, 3, 12), 12.5m, "EUR", false, 9.9m, "EUR", 1987),
            (kept.AcquisitionMode, kept.AcquisitionDate, kept.AcquisitionAmount, kept.AcquisitionCurrency, kept.IsFree,
                kept.InitialValueAmount, kept.InitialValueCurrency, kept.PublicationYear));
    }

    [Fact]
    public void MergeEditions_PassesTheEditionToTheAlbumChosen_WithItsIdentity()
    {
        var first = new Album("Spirou à New York", null);
        var second = new Album("Spirou a New-York", null);
        var kept = new Edition(first, Dupuis);
        first.RecordAcquisition(kept, AcquisitionMode.Purchase);
        var merged = new Edition(second, Dupuis);
        second.RecordAcquisition(merged, AcquisitionMode.Purchase);

        Album.MergeEditions(kept, merged, second, null, kept, []);

        Assert.Empty(first.Editions);
        Assert.Same(kept, Assert.Single(second.Editions));
        Assert.Equal((second, second.Id), (kept.Album, kept.AlbumId));
    }

    [Fact]
    public void MergeEditions_OfAnAlbumOfNeitherEdition_IsAProgrammingError()
    {
        var album = new Album("Spirou à New York", null);
        var (kept, merged) = (new Edition(album, Dupuis), new Edition(album, Dupuis));

        DomainAssert.Throws<ArgumentException>(() => Album.MergeEditions(kept, merged, new Album("Autre", null), null, kept, []));
        DomainAssert.Throws<ArgumentException>(() => Album.MergeEditions(kept, kept, album, null, kept, []));
    }

    [Fact]
    public void MergeEditions_KeepsTheEntryInTheCollectionOfTheAcquisitionChosen()
    {
        var album = new Album("Spirou à New York", null);
        var kept = new Edition(album, Dupuis);
        album.AddPurchaseIntent(kept);
        var merged = new Edition(album, Dupuis);
        album.RecordAcquisition(merged, AcquisitionMode.Purchase);

        Album.MergeEditions(kept, merged, album, null, merged, []);

        Assert.Equal(AcquisitionMode.Purchase, kept.AcquisitionMode);
        Assert.Equal(merged.EnteredCollectionAt, kept.EnteredCollectionAt);
    }

    [Fact]
    public void MergeEditions_OwnedOnceMerged_WithdrawsTheIntentsOfBothEditions()
    {
        var album = new Album("Spirou à New York", null);
        var kept = new Edition(album, Dupuis);
        album.AddPurchaseIntent(kept);
        var merged = new Edition(album, Dupuis);
        album.RecordAcquisition(merged, AcquisitionMode.Purchase);

        Assert.Equal([album.PurchaseIntents.Single()], Album.PurchaseIntentsWithdrawnByEditionMerge(kept, merged, merged));
        Album.MergeEditions(kept, merged, album, null, merged, []);

        Assert.Empty(album.PurchaseIntents);
    }

    [Fact]
    public void MergeEditions_NotOwnedOnceMerged_KeepsOneIntent_ItsOwn()
    {
        var album = new Album("Spirou à New York", null);
        var kept = new Edition(album, Dupuis);
        var intent = album.AddPurchaseIntent(kept);
        var merged = new Edition(album, Dupuis);
        album.AddPurchaseIntent(merged);

        Assert.Empty(Album.PurchaseIntentsWithdrawnByEditionMerge(kept, merged, merged));
        Album.MergeEditions(kept, merged, album, null, merged, []);

        Assert.Same(intent, Assert.Single(album.PurchaseIntents));
        Assert.Null(kept.AcquisitionMode);
    }

    [Fact]
    public void MergeEditions_NotOwnedOnceMerged_GivesTheIntentOfTheOtherEdition_ToAnEditionOwnedBefore()
    {
        var first = new Album("Spirou à New York", null);
        var kept = new Edition(first, Dupuis);
        first.RecordAcquisition(kept, Bought with { Date = null, PriceAmount = null, PriceCurrency = null, InitialValueAmount = null, InitialValueCurrency = null });
        var second = new Album("Spirou a New-York", null);
        var merged = new Edition(second, Dupuis);
        var intent = second.AddPurchaseIntent(merged);

        Album.MergeEditions(kept, merged, first, null, merged, []);

        Assert.Same(intent, Assert.Single(first.PurchaseIntents));
        Assert.Equal((first.Id, kept.Id), (intent.AlbumId, intent.EditionId));
        Assert.Empty(second.PurchaseIntents);
        Assert.Equal((null, null), (kept.AcquisitionMode, kept.EnteredCollectionAt));
    }

    [Fact]
    public void MergeEditions_PassesTheIntentWithTheEdition_ToTheAlbumChosen()
    {
        var first = new Album("Spirou à New York", null);
        var kept = new Edition(first, Dupuis);
        var intent = first.AddPurchaseIntent(kept);
        var second = new Album("Spirou a New-York", null);
        var merged = new Edition(second, Dupuis);
        second.AddPurchaseIntent(merged);

        Album.MergeEditions(kept, merged, second, null, kept, []);

        Assert.Empty(first.PurchaseIntents);
        Assert.Same(intent, Assert.Single(second.PurchaseIntents));
        Assert.Equal((second.Id, kept.Id), (intent.AlbumId, intent.EditionId));
    }

    [Fact]
    public void MergeEditions_KeepsTheVisualsRetained_ThoseOfTheOtherEditionPassingWithTheirIdentity()
    {
        var album = new Album("Spirou à New York", null);
        var kept = new Edition(album, Dupuis);
        var keptCover = kept.AppendVisual(VisualType.Cover, "kept-cover", TestPalette.Any);
        var keptPlate = kept.AppendVisual(VisualType.Plate, "kept-plate", TestPalette.Any);
        var merged = new Edition(album, Dupuis);
        var mergedCover = merged.AppendVisual(VisualType.Cover, "merged-cover", TestPalette.Any);
        var mergedBack = merged.AppendVisual(VisualType.BackCover, "merged-back", TestPalette.Any);

        var removed = Album.MergeEditions(kept, merged, album, null, kept, [keptCover.Id, mergedCover.Id, mergedBack.Id]);

        Assert.Equal([keptCover, mergedCover, mergedBack], kept.GetOrderedVisuals());
        Assert.Equal((0, 1, 0), (keptCover.DisplayOrder, mergedCover.DisplayOrder, mergedBack.DisplayOrder));
        Assert.All(kept.Visuals, v => Assert.Equal((kept, kept.Id), (v.Edition, v.EditionId)));
        Assert.Equal([keptPlate], removed);
    }

    [Fact]
    public void MergeEditions_SaysTheVisualsOfTheOtherEditionDiscarded_DeletedWithIt()
    {
        var album = new Album("Spirou à New York", null);
        var kept = new Edition(album, Dupuis);
        var merged = new Edition(album, Dupuis);
        var mergedCover = merged.AppendVisual(VisualType.Cover, "merged-cover", TestPalette.Any);

        var removed = Album.MergeEditions(kept, merged, album, null, kept, []);

        Assert.Empty(kept.Visuals);
        Assert.Equal([mergedCover], removed);
    }

    [Fact]
    public void MergeEditions_OfAVisualOfNeitherEdition_IsAProgrammingError()
    {
        var album = new Album("Spirou à New York", null);
        var (kept, merged) = (new Edition(album, Dupuis), new Edition(album, Dupuis));

        DomainAssert.Throws<ArgumentException>(() => Album.MergeEditions(kept, merged, album, null, kept, [Guid.CreateVersion7()]));
    }

    [Fact]
    public void MergeEditions_RefusesAPrice_LeftWithoutReferenceDate_AndChangesNothing()
    {
        var dated = new Album("Spirou à New York", null);
        dated.SetFirstPublicationDate(1987, null);
        var merged = new Edition(dated, Dupuis);
        dated.RecordAcquisition(merged, Bought with { Date = null, InitialValueAmount = null, InitialValueCurrency = null });
        var undated = new Album("Spirou a New-York", null);
        var kept = new Edition(undated, Dupuis);
        undated.RecordAcquisition(kept, AcquisitionMode.Gift);

        // The price of the other edition was dated by its album only, which the edition leaves.
        var exception = DomainAssert.Throws<DomainRuleViolationException>(() => Album.MergeEditions(kept, merged, undated, null, merged, []));

        Assert.Equal(DomainRules.EditionAcquisitionPriceReferenceDateRequired, exception.Rule);
        Assert.Equal(AcquisitionMode.Gift, kept.AcquisitionMode);
        Assert.Equal([merged], dated.Editions);
    }
}
