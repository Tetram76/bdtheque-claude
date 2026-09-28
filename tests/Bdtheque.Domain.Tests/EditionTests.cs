using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class EditionTests
{
    private static Album CreateAlbum() => new("Le Lotus bleu", null);

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
    public void Constructor_NullPublisher_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Edition(CreateAlbum(), null!));
    }

    [Fact]
    public void SetPublisher_CollectionBelongingToPublisher_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        var publisher = new Publisher("Dargaud");
        var collection = new PublisherCollection("Lucky Luke", publisher);

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
        var foreignCollection = new PublisherCollection("Lucky Luke", otherPublisher);

        Assert.Throws<ArgumentException>(() => edition.SetPublisher(CreatePublisher(), foreignCollection));
    }

    [Fact]
    public void SetPublisher_NullPublisher_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<ArgumentNullException>(() => edition.SetPublisher(null!, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetPublicationYear_NonPositive_Throws(int year)
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetPublicationYear(year));
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

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetPageCount(count));
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
    public void SetAcquisitionMode_UndefinedValue_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetAcquisitionMode((AcquisitionMode)42));
    }

    [Fact]
    public void SetAcquisitionMode_ValidValue_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        edition.SetAcquisitionMode(AcquisitionMode.Purchase);

        Assert.Equal(AcquisitionMode.Purchase, edition.AcquisitionMode);
    }

    [Fact]
    public void SetAcquisitionMode_ToNullWithDateSet_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Gift);
        edition.SetAcquisitionDate(new DateOnly(2020, 1, 1));

        Assert.Throws<InvalidOperationException>(() => edition.SetAcquisitionMode(null));
    }

    [Fact]
    public void SetAcquisitionMode_ToNullWithPriceSet_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);
        edition.SetAcquisitionPrice(12.5m, "EUR");

        Assert.Throws<InvalidOperationException>(() => edition.SetAcquisitionMode(null));
    }

    [Fact]
    public void SetAcquisitionMode_ToNullAfterClearingDateAndPrice_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);
        edition.SetAcquisitionDate(new DateOnly(2020, 1, 1));
        edition.SetAcquisitionPrice(12.5m, "EUR");

        edition.SetAcquisitionDate(null);
        edition.SetAcquisitionPrice(null, null);
        edition.SetAcquisitionMode(null);

        Assert.Null(edition.AcquisitionMode);
    }

    [Fact]
    public void SetAcquisitionDate_WithoutAcquisitionMode_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<InvalidOperationException>(() => edition.SetAcquisitionDate(new DateOnly(2020, 1, 1)));
    }

    [Fact]
    public void SetAcquisitionDate_WithAcquisitionMode_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);

        edition.SetAcquisitionDate(new DateOnly(2020, 1, 1));

        Assert.Equal(new DateOnly(2020, 1, 1), edition.AcquisitionDate);
    }

    [Fact]
    public void SetAcquisitionPrice_WithoutAcquisitionMode_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());

        Assert.Throws<InvalidOperationException>(() => edition.SetAcquisitionPrice(10m, "EUR"));
    }

    [Fact]
    public void SetAcquisitionPrice_AmountWithoutCurrency_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);

        Assert.Throws<ArgumentException>(() => edition.SetAcquisitionPrice(10m, null));
    }

    [Fact]
    public void SetAcquisitionPrice_CurrencyWithoutAmount_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);

        Assert.Throws<ArgumentException>(() => edition.SetAcquisitionPrice(null, "EUR"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void SetAcquisitionPrice_NonPositiveAmount_Throws(decimal amount)
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetAcquisitionPrice(amount, "EUR"));
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("eur")]
    [InlineData("12€")]
    public void SetAcquisitionPrice_InvalidCurrencyShape_Throws(string currency)
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);

        Assert.Throws<ArgumentException>(() => edition.SetAcquisitionPrice(10m, currency));
    }

    [Fact]
    public void SetAcquisitionPrice_ValidAmountAndCurrency_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);

        edition.SetAcquisitionPrice(12.5m, "USD");

        Assert.Equal(12.5m, edition.AcquisitionAmount);
        Assert.Equal("USD", edition.AcquisitionCurrency);
    }

    [Fact]
    public void SetAcquisitionPrice_ClearBoth_Succeeds()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);
        edition.SetAcquisitionPrice(12.5m, "USD");

        edition.SetAcquisitionPrice(null, null);

        Assert.Null(edition.AcquisitionAmount);
        Assert.Null(edition.AcquisitionCurrency);
    }

    [Fact]
    public void SetAcquisitionPrice_WhileFree_Throws()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Gift);
        edition.SetFree(true);

        Assert.Throws<InvalidOperationException>(() => edition.SetAcquisitionPrice(10m, "EUR"));
    }

    [Fact]
    public void SetFree_True_ClearsExistingPrice()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Gift);
        edition.SetAcquisitionPrice(30m, "EUR");

        edition.SetFree(true);

        Assert.True(edition.IsFree);
        Assert.Null(edition.AcquisitionAmount);
        Assert.Null(edition.AcquisitionCurrency);
    }

    [Fact]
    public void SetFree_False_AllowsSettingPriceAgain()
    {
        var edition = new Edition(CreateAlbum(), CreatePublisher());
        edition.SetAcquisitionMode(AcquisitionMode.Gift);
        edition.SetFree(true);

        edition.SetFree(false);
        edition.SetAcquisitionPrice(30m, "EUR");

        Assert.Equal(30m, edition.AcquisitionAmount);
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
}
