using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// Rules on the acquisition and the value of an edition (modele-metier.md § Édition, contraintes
/// d'intégrité): who may carry an amount, and the reference date every amount needs (fonctionnel.md §
/// Gestion des devises). The acquisition is written as a whole, the way the form sends it.
/// </summary>
public sealed class EditionValueTests
{
    private static Publisher CreatePublisher() => new("Casterman");

    // No date at all, unless a test gives one: each test states the reference dates it relies on.
    private static Edition CreateEdition(int? albumYear = null)
    {
        var album = new Album("Le Lotus bleu", null);
        album.SetFirstPublicationDate(albumYear, null);
        return new Edition(album, CreatePublisher());
    }

    private static Edition CreateOwnedEdition(AcquisitionMode mode, int? albumYear = null)
    {
        var edition = CreateEdition(albumYear);
        edition.Album.RecordAcquisition(edition, mode);
        return edition;
    }

    private static EditionAcquisition Acquisition(
        AcquisitionMode? mode,
        DateOnly? date = null,
        decimal? price = null,
        string? priceCurrency = null,
        bool isFree = false,
        decimal? initialValue = null,
        string? initialValueCurrency = null) =>
        new(mode, date, price, priceCurrency, isFree, initialValue, initialValueCurrency);

    [Fact]
    public void Constructor_RegistersTheEditionOnItsAlbum()
    {
        var edition = CreateEdition();

        Assert.Contains(edition, edition.Album.Editions);
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_OnOwnedEdition_WritesEverything()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);

        edition.SetPublicationYearAndAcquisition(
            1978, Acquisition(AcquisitionMode.Gift, new DateOnly(2020, 1, 1), 12.5m, "USD", initialValue: 35m, initialValueCurrency: "FRF"));

        Assert.Equal(1978, edition.PublicationYear);
        Assert.Equal(AcquisitionMode.Gift, edition.AcquisitionMode);
        Assert.Equal(new DateOnly(2020, 1, 1), edition.AcquisitionDate);
        Assert.Equal((12.5m, "USD"), (edition.AcquisitionAmount, edition.AcquisitionCurrency));
        Assert.Equal((35m, "FRF"), (edition.InitialValueAmount, edition.InitialValueCurrency));
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_ClearingTheAmounts_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);
        edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR", initialValue: 35m, initialValueCurrency: "FRF"));

        edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase));

        Assert.Null(edition.AcquisitionAmount);
        Assert.Null(edition.AcquisitionCurrency);
        Assert.Null(edition.InitialValueAmount);
        Assert.Null(edition.InitialValueCurrency);
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_UndefinedMode_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);

        Assert.Throws<ArgumentOutOfRangeException>(() => edition.SetPublicationYearAndAcquisition(null, Acquisition((AcquisitionMode)42)));
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_NonPositiveYear_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);

        DomainAssert.Violates(
            DomainRules.EditionPublicationYearPositive, () => edition.SetPublicationYearAndAcquisition(0, Acquisition(AcquisitionMode.Purchase)));
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_AcquiringAnEditionNotOwned_Throws()
    {
        // Acquiring an edition may realize an intent on it or on its album, which only the album
        // aggregate sees: it must go through Album.RecordAcquisition.
        var edition = CreateEdition();

        Assert.Throws<InvalidOperationException>(() => edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase)));
        Assert.Null(edition.AcquisitionMode);
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_ClearingTheModeOfAnOwnedEdition_ThrowsAndKeepsIt()
    {
        // An owned edition stays owned until deleted: a bought edition never becomes an intent again
        // (fonctionnel.md § Intention d'achat), and a form can leave the mode empty.
        var edition = CreateOwnedEdition(AcquisitionMode.Gift);

        DomainAssert.Violates(
            DomainRules.EditionAcquisitionModeRequired, () => edition.SetPublicationYearAndAcquisition(null, Acquisition(null)));
        Assert.Equal(AcquisitionMode.Gift, edition.AcquisitionMode);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void SetPublicationYearAndAcquisition_DateOrAmountOnAnEditionNotOwned_Throws(bool withDate, bool withPrice, bool withInitialValue)
    {
        var edition = CreateEdition(albumYear: 1978);
        var acquisition = Acquisition(
            null,
            withDate ? new DateOnly(2020, 1, 1) : null,
            withPrice ? 10m : null, withPrice ? "EUR" : null,
            initialValue: withInitialValue ? 35m : null, initialValueCurrency: withInitialValue ? "FRF" : null);

        DomainAssert.Violates(DomainRules.EditionAcquisitionModeRequired, () => edition.SetPublicationYearAndAcquisition(null, acquisition));
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_FreeOnAnEditionNotOwned_Succeeds()
    {
        // Freeness is a trait of the copy, free of rules while the edition is not owned.
        var edition = CreateEdition();

        edition.SetPublicationYearAndAcquisition(1978, Acquisition(null, isFree: true));

        Assert.True(edition.IsFree);
        Assert.Equal(1978, edition.PublicationYear);
    }

    [Theory]
    [InlineData(10, null)]
    [InlineData(null, "EUR")]
    public void SetPublicationYearAndAcquisition_PriceAmountAndCurrencyNotTogether_Throws(int? amount, string? currency)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);

        DomainAssert.Violates(
            DomainRules.EditionAcquisitionAmountCurrencyTogether,
            () => edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase, price: amount, priceCurrency: currency)));
    }

    [Theory]
    [InlineData(35, null)]
    [InlineData(null, "FRF")]
    public void SetPublicationYearAndAcquisition_InitialValueAmountAndCurrencyNotTogether_Throws(int? amount, string? currency)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);

        DomainAssert.Violates(
            DomainRules.EditionInitialValueAmountCurrencyTogether,
            () => edition.SetPublicationYearAndAcquisition(
                null, Acquisition(AcquisitionMode.Purchase, initialValue: amount, initialValueCurrency: currency)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetPublicationYearAndAcquisition_NonPositivePrice_Throws(decimal amount)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);

        DomainAssert.Violates(
            DomainRules.EditionAcquisitionAmountPositive,
            () => edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase, price: amount, priceCurrency: "EUR")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetPublicationYearAndAcquisition_NonPositiveInitialValue_Throws(decimal amount)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);

        DomainAssert.Violates(
            DomainRules.EditionInitialValueAmountPositive,
            () => edition.SetPublicationYearAndAcquisition(
                null, Acquisition(AcquisitionMode.Purchase, initialValue: amount, initialValueCurrency: "FRF")));
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void SetPublicationYearAndAcquisition_InvalidCurrencyShape_Throws(string currency)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);

        DomainAssert.Violates(
            DomainRules.EditionCurrencyCodeInvalid,
            () => edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: currency)));
        DomainAssert.Violates(
            DomainRules.EditionCurrencyCodeInvalid,
            () => edition.SetPublicationYearAndAcquisition(
                null, Acquisition(AcquisitionMode.Purchase, initialValue: 10m, initialValueCurrency: currency)));
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_OldFrenchFranc_Succeeds()
    {
        // The old franc has no ISO 4217 code: it is stored under the reserved code QZF
        // (choix-implementation.md § Représentation de la devise).
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1955);

        edition.SetPublicationYearAndAcquisition(
            null, Acquisition(AcquisitionMode.Purchase, price: 500m, priceCurrency: "QZF", initialValue: 450m, initialValueCurrency: "QZF"));

        Assert.Equal("QZF", edition.AcquisitionCurrency);
        Assert.Equal("QZF", edition.InitialValueCurrency);
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_FreeWithAPrice_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Gift, albumYear: 1978);

        DomainAssert.Violates(
            DomainRules.EditionFreeExcludesPrice,
            () => edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Gift, price: 10m, priceCurrency: "EUR", isFree: true)));
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_FreeWithAnInitialValue_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Gift, albumYear: 1978);

        DomainAssert.Violates(
            DomainRules.EditionFreeExcludesInitialValue,
            () => edition.SetPublicationYearAndAcquisition(
                null, Acquisition(AcquisitionMode.Gift, isFree: true, initialValue: 35m, initialValueCurrency: "FRF")));
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_FreePurchase_ThrowsAndKeepsTheEdition()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Gift);

        DomainAssert.Violates(
            DomainRules.EditionPurchaseCannotBeFree,
            () => edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase, isFree: true)));
        Assert.Equal(AcquisitionMode.Gift, edition.AcquisitionMode);
        Assert.False(edition.IsFree);
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_FreeGift_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);

        edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Gift, isFree: true));

        Assert.True(edition.IsFree);
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_PriceWithoutAnyReferenceDate_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);

        DomainAssert.Violates(
            DomainRules.EditionAcquisitionPriceReferenceDateRequired,
            () => edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR")));
    }

    [Theory]
    [InlineData(true, null, null)]
    [InlineData(false, 2001, null)]
    [InlineData(false, null, 2001)]
    public void SetPublicationYearAndAcquisition_PriceWithOneReferenceDate_Succeeds(bool withAcquisitionDate, int? editionYear, int? albumYear)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear);

        edition.SetPublicationYearAndAcquisition(
            editionYear,
            Acquisition(AcquisitionMode.Purchase, withAcquisitionDate ? new DateOnly(2020, 1, 1) : null, 10m, "EUR"));

        Assert.Equal(10m, edition.AcquisitionAmount);
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_InitialValueDatedByTheAcquisitionDateOnly_Throws()
    {
        // The acquisition date is no reference date for the initial value, a price at publication.
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);

        DomainAssert.Violates(
            DomainRules.EditionInitialValueReferenceDateRequired,
            () => edition.SetPublicationYearAndAcquisition(
                null, Acquisition(AcquisitionMode.Purchase, new DateOnly(2020, 1, 1), initialValue: 35m, initialValueCurrency: "FRF")));
    }

    [Theory]
    [InlineData(2001, null)]
    [InlineData(null, 2001)]
    public void SetPublicationYearAndAcquisition_InitialValueWithOneReferenceDate_Succeeds(int? editionYear, int? albumYear)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear);

        edition.SetPublicationYearAndAcquisition(editionYear, Acquisition(AcquisitionMode.Purchase, initialValue: 35m, initialValueCurrency: "FRF"));

        Assert.Equal(35m, edition.InitialValueAmount);
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_SwappingTheReferenceDateOfThePrice_SucceedsInASingleWrite()
    {
        // The edition year and the acquisition date replace each other as reference date of the price:
        // written one after the other, one of the two orders would leave the price undated in between.
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetPublicationYearAndAcquisition(2019, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR"));

        edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase, new DateOnly(2020, 1, 1), 10m, "EUR"));
        Assert.Equal((null, new DateOnly(2020, 1, 1)), (edition.PublicationYear, edition.AcquisitionDate));

        edition.SetPublicationYearAndAcquisition(2019, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR"));
        Assert.Equal((2019, (DateOnly?)null), (edition.PublicationYear, edition.AcquisitionDate));
    }

    [Fact]
    public void SetPublicationYearAndAcquisition_Refused_LeavesTheEditionUntouched()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetPublicationYearAndAcquisition(2019, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR"));

        DomainAssert.Violates(
            DomainRules.EditionAcquisitionPriceReferenceDateRequired,
            () => edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Gift, price: 20m, priceCurrency: "USD")));

        Assert.Equal(2019, edition.PublicationYear);
        Assert.Equal(AcquisitionMode.Purchase, edition.AcquisitionMode);
        Assert.Equal((10m, "EUR"), (edition.AcquisitionAmount, edition.AcquisitionCurrency));
    }

    [Fact]
    public void SetPublicationYear_ClearedWhilePriceDependsOnIt_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetPublicationYearAndAcquisition(2019, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR"));

        DomainAssert.Violates(DomainRules.EditionAcquisitionPriceReferenceDateRequired, () => edition.SetPublicationYear(null));
        Assert.Equal(2019, edition.PublicationYear);
    }

    [Fact]
    public void SetPublicationYear_ClearedWhileInitialValueDependsOnIt_Throws()
    {
        // The acquisition date keeps the price dated, but not the initial value.
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetPublicationYearAndAcquisition(
            2019, Acquisition(AcquisitionMode.Purchase, new DateOnly(2020, 1, 1), initialValue: 15m, initialValueCurrency: "EUR"));

        DomainAssert.Violates(DomainRules.EditionInitialValueReferenceDateRequired, () => edition.SetPublicationYear(null));
    }

    [Fact]
    public void SetPublicationYear_ClearedWithAlbumDate_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2018);
        edition.SetPublicationYearAndAcquisition(
            2019, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR", initialValue: 15m, initialValueCurrency: "EUR"));

        edition.SetPublicationYear(null);

        Assert.Null(edition.PublicationYear);
    }

    [Fact]
    public void AlbumSetFirstPublicationDate_ClearedWhilePriceDependsOnIt_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2001);
        edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR"));

        DomainAssert.Violates(
            DomainRules.EditionAcquisitionPriceReferenceDateRequired, () => edition.Album.SetFirstPublicationDate(null, null));
        Assert.Equal(2001, edition.Album.FirstPublicationYear);
    }

    [Fact]
    public void AlbumSetFirstPublicationDate_ClearedWhileInitialValueDependsOnIt_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2001);
        edition.SetPublicationYearAndAcquisition(
            null, Acquisition(AcquisitionMode.Purchase, new DateOnly(2020, 1, 1), initialValue: 15m, initialValueCurrency: "EUR"));

        DomainAssert.Violates(
            DomainRules.EditionInitialValueReferenceDateRequired, () => edition.Album.SetFirstPublicationDate(null, null));
    }

    [Fact]
    public void AlbumSetFirstPublicationDate_ClearedWhileEditionsHaveTheirOwnDates_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2001);
        edition.SetPublicationYearAndAcquisition(
            2003, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR", initialValue: 15m, initialValueCurrency: "EUR"));

        edition.Album.SetFirstPublicationDate(null, null);

        Assert.Null(edition.Album.FirstPublicationYear);
    }

    [Fact]
    public void AlbumSetFirstPublicationDate_ChangedWhileAmountsDependOnIt_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2001);
        edition.SetPublicationYearAndAcquisition(null, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR"));

        edition.Album.SetFirstPublicationDate(2002, 5);

        Assert.Equal(2002, edition.Album.FirstPublicationYear);
    }

    [Fact]
    public void RecordAcquisition_WithTheWholeAcquisition_WritesItAndRealizesTheIntent()
    {
        var edition = CreateEdition();
        var album = edition.Album;
        album.AddPurchaseIntent(edition);
        edition.SetPublicationYear(2019);

        album.RecordAcquisition(
            edition, Acquisition(AcquisitionMode.Purchase, new DateOnly(2020, 1, 1), 10m, "EUR", initialValue: 15m, initialValueCurrency: "EUR"));

        Assert.Equal(AcquisitionMode.Purchase, edition.AcquisitionMode);
        Assert.Equal(new DateOnly(2020, 1, 1), edition.AcquisitionDate);
        Assert.Equal((10m, "EUR"), (edition.AcquisitionAmount, edition.AcquisitionCurrency));
        Assert.Equal((15m, "EUR"), (edition.InitialValueAmount, edition.InitialValueCurrency));
        Assert.Empty(album.PurchaseIntents);
    }

    [Fact]
    public void RecordAcquisition_WithoutMode_IsABusinessErrorWithoutRealizingTheIntent()
    {
        // Entering an edition requires its acquisition mode: only an intent creates an edition not owned.
        var edition = CreateEdition();
        var album = edition.Album;
        album.AddPurchaseIntent();

        DomainAssert.Violates(DomainRules.EditionAcquisitionModeRequired, () => album.RecordAcquisition(edition, Acquisition(null)));
        Assert.Null(edition.AcquisitionMode);
        Assert.Single(album.PurchaseIntents);
    }

    [Fact]
    public void RecordAcquisition_WithAnInvalidAmount_ThrowsWithoutRealizingTheIntent()
    {
        // Every check runs before the aggregate changes: a later save would otherwise delete an intent
        // whose acquisition was never recorded.
        var edition = CreateEdition();
        var album = edition.Album;
        album.AddPurchaseIntent(edition);

        DomainAssert.Violates(
            DomainRules.EditionAcquisitionPriceReferenceDateRequired,
            () => album.RecordAcquisition(edition, Acquisition(AcquisitionMode.Purchase, price: 10m, priceCurrency: "EUR")));
        Assert.Null(edition.AcquisitionMode);
        Assert.Null(edition.AcquisitionAmount);
        Assert.Single(album.PurchaseIntents);
    }

    [Fact]
    public void RecordAcquisition_PurchaseOfFreeEdition_ThrowsWithoutRealizingTheIntent()
    {
        // Freeness is a trait of the copy, left free of rules while the edition is not owned
        // (fonctionnel.md § Intention d'achat): it only conflicts with the purchase that acquires it.
        var edition = CreateEdition();
        var album = edition.Album;
        album.AddPurchaseIntent(edition);
        edition.SetPublicationYearAndAcquisition(null, Acquisition(null, isFree: true));

        DomainAssert.Violates(DomainRules.EditionPurchaseCannotBeFree, () => album.RecordAcquisition(edition, AcquisitionMode.Purchase));
        Assert.Null(edition.AcquisitionMode);
        Assert.Single(album.PurchaseIntents);
    }

    [Fact]
    public void RecordAcquisition_GiftOfFreeEdition_KeepsItFree()
    {
        var edition = CreateEdition();
        edition.SetPublicationYearAndAcquisition(null, Acquisition(null, isFree: true));

        edition.Album.RecordAcquisition(edition, AcquisitionMode.Gift);

        Assert.Equal(AcquisitionMode.Gift, edition.AcquisitionMode);
        Assert.True(edition.IsFree);
    }
}
