using Bdtheque.Domain.Entities;

namespace Bdtheque.Domain.Tests;

public sealed class PurchaseIntentTests
{
    [Fact]
    public void ForAlbum_Valid_Succeeds()
    {
        var album = new Album("Le Lotus bleu", null);

        var intent = PurchaseIntent.ForAlbum(album);

        Assert.Same(album, intent.Album);
        Assert.Equal(album.Id, intent.AlbumId);
        Assert.Null(intent.Edition);
        Assert.Null(intent.EditionId);
        Assert.NotEqual(Guid.Empty, intent.Id);
    }

    [Fact]
    public void ForEdition_Valid_Succeeds()
    {
        var edition = new Edition(new Album("Le Lotus bleu", null), new Publisher("Casterman"));

        var intent = PurchaseIntent.ForEdition(edition);

        Assert.Same(edition, intent.Edition);
        Assert.Equal(edition.Id, intent.EditionId);
        Assert.Null(intent.Album);
        Assert.Null(intent.AlbumId);
    }

    [Fact]
    public void ForAlbum_NullAlbum_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PurchaseIntent.ForAlbum(null!));
    }

    [Fact]
    public void ForEdition_NullEdition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PurchaseIntent.ForEdition(null!));
    }
}
