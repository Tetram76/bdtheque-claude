using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class PurchaseIntentTests
{
    private static readonly Publisher Casterman = new("Casterman");

    [Fact]
    public void AddPurchaseIntent_WholeAlbum_Succeeds()
    {
        var album = new Album("Le Lotus bleu", null);

        var intent = album.AddPurchaseIntent();

        Assert.Same(album, intent.Album);
        Assert.Equal(album.Id, intent.AlbumId);
        Assert.Null(intent.Edition);
        Assert.Null(intent.EditionId);
        Assert.NotEqual(Guid.Empty, intent.Id);
        Assert.Equal([intent], album.PurchaseIntents);
    }

    [Fact]
    public void AddPurchaseIntent_Edition_Succeeds()
    {
        var album = new Album("Le Lotus bleu", null);
        var edition = new Edition(album, Casterman);

        var intent = album.AddPurchaseIntent(edition);

        Assert.Same(edition, intent.Edition);
        Assert.Equal(edition.Id, intent.EditionId);
        Assert.Equal(album.Id, intent.AlbumId);
        Assert.Equal([intent], album.PurchaseIntents);
    }

    [Fact]
    public void AddPurchaseIntent_DistinctEditionsOfSameAlbum_Succeeds()
    {
        var album = new Album("Le Lotus bleu", null);

        album.AddPurchaseIntent(new Edition(album, Casterman));
        album.AddPurchaseIntent(new Edition(album, Casterman));

        Assert.Equal(2, album.PurchaseIntents.Count);
    }

    [Fact]
    public void AddPurchaseIntent_EditionAlreadyOwned_Throws()
    {
        // fonctionnel.md § Intention d'achat: an edition already bought cannot become an intent
        // again — a second copy is a new edition of the album, on which the intent is placed.
        var album = new Album("Le Lotus bleu", null);
        var edition = new Edition(album, Casterman);
        edition.Album.RecordAcquisition(edition, AcquisitionMode.Purchase);

        DomainAssert.Violates(DomainRules.PurchaseIntentEditionAlreadyOwned, () => album.AddPurchaseIntent(edition));
        Assert.Empty(album.PurchaseIntents);
    }

    [Fact]
    public void AddPurchaseIntent_WholeAlbumTwice_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        album.AddPurchaseIntent();

        DomainAssert.Violates(DomainRules.PurchaseIntentAlbumAlreadyTargeted, () => album.AddPurchaseIntent());
    }

    [Fact]
    public void AddPurchaseIntent_SameEditionTwice_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var edition = new Edition(album, Casterman);
        album.AddPurchaseIntent(edition);

        DomainAssert.Violates(DomainRules.PurchaseIntentEditionAlreadyTargeted, () => album.AddPurchaseIntent(edition));
    }

    [Fact]
    public void AddPurchaseIntent_WholeAlbum_WhenAnEditionIsTargeted_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        album.AddPurchaseIntent(new Edition(album, Casterman));

        DomainAssert.Violates(DomainRules.PurchaseIntentEditionsAlreadyTargeted, () => album.AddPurchaseIntent());
    }

    [Fact]
    public void AddPurchaseIntent_Edition_WhenWholeAlbumIsTargeted_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        album.AddPurchaseIntent();

        DomainAssert.Violates(DomainRules.PurchaseIntentAlbumAlreadyTargeted, () => album.AddPurchaseIntent(new Edition(album, Casterman)));
    }

    [Fact]
    public void AddPurchaseIntent_EditionOfAnotherAlbum_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var otherEdition = new Edition(new Album("Tintin au Tibet", null), Casterman);

        Assert.Throws<ArgumentException>(() => album.AddPurchaseIntent(otherEdition));
    }

    [Fact]
    public void AddPurchaseIntent_NullEdition_Throws()
    {
        var album = new Album("Le Lotus bleu", null);

        Assert.Throws<ArgumentNullException>(() => album.AddPurchaseIntent(null!));
    }
}

public sealed class AcquisitionRecordingTests
{
    private static readonly Publisher Casterman = new("Casterman");

    [Fact]
    public void RecordAcquisition_TargetedEdition_RemovesOnlyItsIntentAndRecordsAcquisition()
    {
        var album = new Album("Le Lotus bleu", null);
        var bought = new Edition(album, Casterman);
        var stillWanted = new Edition(album, Casterman);
        album.AddPurchaseIntent(bought);
        var remaining = album.AddPurchaseIntent(stillWanted);

        album.RecordAcquisition(bought, AcquisitionMode.Purchase);

        Assert.Equal(AcquisitionMode.Purchase, bought.AcquisitionMode);
        Assert.Equal([remaining], album.PurchaseIntents);
    }

    [Fact]
    public void RecordAcquisition_AlbumTargetedAsWhole_RemovesTheAlbumIntent()
    {
        // Any edition satisfies an intent on the album (fonctionnel.md § Intention d'achat).
        var album = new Album("Le Lotus bleu", null);
        album.AddPurchaseIntent();
        var edition = new Edition(album, Casterman);

        album.RecordAcquisition(edition, AcquisitionMode.Gift);

        Assert.Equal(AcquisitionMode.Gift, edition.AcquisitionMode);
        Assert.Empty(album.PurchaseIntents);
    }

    [Fact]
    public void RecordAcquisition_WithoutIntent_RecordsAcquisition()
    {
        var album = new Album("Le Lotus bleu", null);
        var edition = new Edition(album, Casterman);

        album.RecordAcquisition(edition, AcquisitionMode.Purchase);

        Assert.Equal(AcquisitionMode.Purchase, edition.AcquisitionMode);
    }

    [Fact]
    public void RecordAcquisition_EditionAlreadyOwned_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var edition = new Edition(album, Casterman);
        album.RecordAcquisition(edition, AcquisitionMode.Purchase);

        DomainAssert.Violates(DomainRules.EditionAlreadyOwned, () => album.RecordAcquisition(edition, AcquisitionMode.Gift));
        Assert.Equal(AcquisitionMode.Purchase, edition.AcquisitionMode);
    }

    [Fact]
    public void RecordAcquisition_UndefinedMode_ThrowsWithoutRemovingTheIntent()
    {
        // A rejected acquisition must leave the aggregate untouched: a caller saving the context
        // afterwards would otherwise delete an intent whose acquisition was never recorded.
        var album = new Album("Le Lotus bleu", null);
        var edition = new Edition(album, Casterman);
        var intent = album.AddPurchaseIntent(edition);

        Assert.Throws<ArgumentOutOfRangeException>(() => album.RecordAcquisition(edition, (AcquisitionMode)42));
        Assert.Equal([intent], album.PurchaseIntents);
        Assert.Null(edition.AcquisitionMode);
    }

    [Fact]
    public void RecordAcquisition_EditionOfAnotherAlbum_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var otherEdition = new Edition(new Album("Tintin au Tibet", null), Casterman);

        Assert.Throws<ArgumentException>(() => album.RecordAcquisition(otherEdition, AcquisitionMode.Purchase));
    }
}
