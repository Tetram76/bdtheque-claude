using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class EditionTests
{
    // Dated, so that an amount always has a reference date to fall back on: the rules tied to the
    // reference date of an amount are covered by EditionValueTests.
    private static Album CreateAlbum()
    {
        var album = new Album("Le Lotus bleu", null);
        album.SetFirstPublicationDate(1936, null);
        return album;
    }

    private static Publisher CreatePublisher() => new("Casterman");

    [Fact]
    public void Constructor_Valid_SetsRequiredFieldsAndDefaults()
    {
        var album = CreateAlbum();
        var publisher = CreatePublisher();

        var edition = new Edition(album, publisher);

        Assert.Same(album, edition.Album);
        Assert.Equal(album.Id, edition.AlbumId);
        Assert.Same(publisher, edition.Publisher);
        Assert.Equal(publisher.Id, edition.PublisherId);
        Assert.Null(edition.PublisherCollection);
        Assert.Null(edition.PublisherCollectionId);
        Assert.False(edition.IsDedicated);
        Assert.True(edition.IsColor);
        Assert.False(edition.IsSecondHand);
        Assert.False(edition.IsFree);
        Assert.Null(edition.AcquisitionMode);
        Assert.NotEqual(Guid.Empty, edition.Id);
    }

    [Fact]
    public void Constructor_NullAlbum_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Edition(null!, CreatePublisher()));
    }

    [Fact]
    public void Constructor_WithoutPublisher_IsABusinessErrorAndRegistersNothing()
    {
        // The publisher is a required field of the form: leaving it empty is an input mistake.
        var album = CreateAlbum();

        DomainAssert.Violates(DomainRules.EditionPublisherRequired, () => new Edition(album, null));
        Assert.Empty(album.Editions);
    }

    [Fact]
    public void SetPublisher_CollectionBelongingToPublisher_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        var publisher = new Publisher("Dargaud");
        var collection = publisher.AddCollection("Lucky Luke");

        edition.SetPublisher(publisher, collection);

        Assert.Same(publisher, edition.Publisher);
        Assert.Same(collection, edition.PublisherCollection);
        Assert.Equal(collection.Id, edition.PublisherCollectionId);
    }

    [Fact]
    public void SetPublisher_CollectionFromAnotherPublisher_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        var otherPublisher = new Publisher("Dargaud");
        var foreignCollection = otherPublisher.AddCollection("Lucky Luke");

        DomainAssert.Violates(DomainRules.PublisherCollectionNotOfPublisher, () => edition.SetPublisher(CreatePublisher(), foreignCollection));
    }

    [Fact]
    public void SetPublisher_WithoutPublisher_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        DomainAssert.Violates(DomainRules.EditionPublisherRequired, () => edition.SetPublisher(null, null));
        Assert.NotNull(edition.Publisher);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetPublicationYear_NonPositive_Throws(int year)
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        DomainAssert.Violates(DomainRules.EditionPublicationYearPositive, () => edition.SetPublicationYear(year));
    }

    [Fact]
    public void SetPublicationYear_Positive_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetPublicationYear(1978);

        Assert.Equal(1978, edition.PublicationYear);
    }

    [Fact]
    public void SetIsbn_TrimsAndStoresWithoutValidation()
    {
        // ISBN checksum validation is advisory only (fonctionnel.md § Validation de l'ISBN):
        // the domain must accept and store an incorrect ISBN as-is.
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetIsbn("  978-2-205-00217-2  ");

        Assert.Equal("978-2-205-00217-2", edition.Isbn);
    }

    [Fact]
    public void SetIsbn_Blank_SetsNull()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetIsbn("   ");

        Assert.Null(edition.Isbn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetPageCount_NonPositive_Throws(int count)
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        DomainAssert.Violates(DomainRules.EditionPageCountPositive, () => edition.SetPageCount(count));
    }

    [Fact]
    public void SetPageCount_Positive_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetPageCount(48);

        Assert.Equal(48, edition.PageCount);
    }

    [Fact]
    public void SetBinding_UndefinedValue_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetBinding((BindingType)42));
    }

    [Fact]
    public void SetBinding_ValidValue_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetBinding(BindingType.Hardcover);

        Assert.Equal(BindingType.Hardcover, edition.Binding);
    }

    [Fact]
    public void SetOrientation_UndefinedValue_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetOrientation((BookOrientation)42));
    }

    [Fact]
    public void SetReadingDirection_UndefinedValue_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetReadingDirection((ReadingDirection)42));
    }

    [Fact]
    public void SetFormat_UndefinedValue_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetFormat((EditionFormat)42));
    }

    [Fact]
    public void SetCategory_UndefinedValue_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetCategory((EditionCategory)42));
    }

    [Fact]
    public void SetCondition_UndefinedValue_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetCondition((EditionCondition)42));
    }

    [Fact]
    public void SetDedicated_UpdatesValue()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetDedicated(true);

        Assert.True(edition.IsDedicated);
    }

    [Fact]
    public void SetColor_UpdatesValue()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetColor(false);

        Assert.False(edition.IsColor);
    }

    [Fact]
    public void SetSecondHand_UpdatesValue()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetSecondHand(true);

        Assert.True(edition.IsSecondHand);
    }

    [Fact]
    public void SetPersonalReference_TrimsAndStores()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetPersonalReference("  A-001  ");

        Assert.Equal("A-001", edition.PersonalReference);
    }

    [Fact]
    public void SetPersonalNotes_TrimsAndStores()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetPersonalNotes("  Achat au marché  ");

        Assert.Equal("Achat au marché", edition.PersonalNotes);
    }

    [Fact]
    public void GetOrderedVisuals_MultipleTypesAndOrders_ReturnsInFixedOrder()
    {
        // fonctionnel.md § Ordre des visuels d'une édition: by type in a fixed order
        // (Couverture → Dédicace → Page de garde → Planche → 4e de couverture), then by
        // display order within the same type. Added out of order and across types on
        // purpose so a naive Visuals enumeration (unordered) would fail this assertion.
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        var plate2 = edition.AddVisual(VisualType.Plate, "plate-2.jpg", 2);
        var backCover = edition.AddVisual(VisualType.BackCover, "back-cover.jpg", 0);
        var cover = edition.AddVisual(VisualType.Cover, "cover.jpg", 0);
        var plate1 = edition.AddVisual(VisualType.Plate, "plate-1.jpg", 1);

        var ordered = edition.GetOrderedVisuals().ToList();

        Assert.Equal([cover, plate1, plate2, backCover], ordered);
    }

    [Fact]
    public void GetOrderedVisuals_SameTypeAndDisplayOrder_DoesNotDependOnLoadOrder()
    {
        // EF Core materializes a collection in whatever order the database returns its rows (no
        // ORDER BY): two visuals tied on type and display order must still always come out in
        // the same order, or the page would shuffle them from one load to the next.
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.AddVisual(VisualType.Plate, "plate-a.jpg", 1);
        edition.AddVisual(VisualType.Plate, "plate-b.jpg", 1);

        var firstLoad = edition.GetOrderedVisuals().ToList();
        ReverseLoadedVisuals(edition);
        var secondLoad = edition.GetOrderedVisuals().ToList();

        Assert.Equal(firstLoad, secondLoad);
    }

    [Fact]
    public void AddVisual_AppearsInVisuals()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        var visual = edition.AddVisual(VisualType.Cover, "cover.jpg", 0);

        Assert.Contains(visual, edition.Visuals);
    }

    private static void ReverseLoadedVisuals(Edition edition)
    {
        var field = typeof(Edition).GetField("_visuals", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((List<EditionVisual>)field.GetValue(edition)!).Reverse();
    }
}
