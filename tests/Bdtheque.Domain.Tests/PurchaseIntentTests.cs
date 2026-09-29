using Bdtheque.Domain.Entities;

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
