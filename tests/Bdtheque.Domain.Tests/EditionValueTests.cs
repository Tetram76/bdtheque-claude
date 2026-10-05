using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// Rules on the value of an edition (modele-metier.md § Édition, contraintes d'intégrité): who may
/// carry an amount, and the reference date every amount needs (fonctionnel.md § Gestion des devises).
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

    [Fact]
    public void Constructor_RegistersTheEditionOnItsAlbum()
    {
        var edition = CreateEdition();

        Assert.Contains(edition, edition.Album.Editions);
    }

    [Fact]
    public void SetInitialValue_OnOwnedEditionWithReferenceDate_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetPublicationYear(1978);

        edition.SetInitialValue(35m, "FRF");

        Assert.Equal(35m, edition.InitialValueAmount);
        Assert.Equal("FRF", edition.InitialValueCurrency);
    }

    [Fact]
    public void SetInitialValue_ClearBoth_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);
        edition.SetInitialValue(35m, "FRF");

        edition.SetInitialValue(null, null);

        Assert.Null(edition.InitialValueAmount);
        Assert.Null(edition.InitialValueCurrency);
    }

    [Fact]
    public void SetInitialValue_OnEditionNotOwned_Throws()
    {
        var edition = CreateEdition(albumYear: 1978);

        DomainAssert.Violates(DomainRules.EditionAcquisitionModeRequired, () => edition.SetInitialValue(35m, "FRF"));
    }

    [Fact]
    public void SetInitialValue_WhileFree_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Gift, albumYear: 1978);
        edition.SetFree(true);

        DomainAssert.Violates(DomainRules.EditionFreeExcludesInitialValue, () => edition.SetInitialValue(35m, "FRF"));
    }

    [Theory]
    [InlineData(35, null)]
    [InlineData(null, "FRF")]
    public void SetInitialValue_AmountAndCurrencyNotTogether_Throws(int? amount, string? currency)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);

        DomainAssert.Violates(
            DomainRules.EditionInitialValueAmountCurrencyTogether, () => edition.SetInitialValue(amount, currency));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetInitialValue_NonPositiveAmount_Throws(decimal amount)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);

        DomainAssert.Violates(DomainRules.EditionInitialValueAmountPositive, () => edition.SetInitialValue(amount, "FRF"));
    }

    [Theory]
    [InlineData("frf")]
    [InlineData("FR")]
    public void SetInitialValue_InvalidCurrencyShape_Throws(string currency)
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1978);

        DomainAssert.Violates(DomainRules.EditionCurrencyCodeInvalid, () => edition.SetInitialValue(35m, currency));
    }

    [Fact]
    public void SetInitialValue_WithoutPublicationYearNorAlbumDate_Throws()
    {
        // The acquisition date is no reference date for the initial value, a price at publication.
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetAcquisitionDate(new DateOnly(2020, 1, 1));

        DomainAssert.Violates(
            DomainRules.EditionInitialValueReferenceDateRequired, () => edition.SetInitialValue(35m, "FRF"));
    }

    [Fact]
    public void SetInitialValue_WithAlbumDateOnly_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1958);

        edition.SetInitialValue(350m, "QZF");

        Assert.Equal("QZF", edition.InitialValueCurrency);
    }

    [Fact]
    public void SetAcquisitionPrice_OldFrenchFranc_Succeeds()
    {
        // The old franc has no ISO 4217 code: it is stored under the reserved code QZF
        // (choix-implementation.md § Représentation de la devise).
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 1955);

        edition.SetAcquisitionPrice(500m, "QZF");

        Assert.Equal("QZF", edition.AcquisitionCurrency);
    }

    [Fact]
    public void SetAcquisitionPrice_WithoutAnyReferenceDate_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);

        DomainAssert.Violates(
            DomainRules.EditionAcquisitionPriceReferenceDateRequired, () => edition.SetAcquisitionPrice(10m, "EUR"));
    }

    [Fact]
    public void SetAcquisitionPrice_WithAcquisitionDateOnly_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetAcquisitionDate(new DateOnly(2020, 1, 1));

        edition.SetAcquisitionPrice(10m, "EUR");

        Assert.Equal(10m, edition.AcquisitionAmount);
    }

    [Fact]
    public void SetAcquisitionPrice_WithPublicationYearOnly_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetPublicationYear(2001);

        edition.SetAcquisitionPrice(10m, "EUR");

        Assert.Equal(10m, edition.AcquisitionAmount);
    }

    [Fact]
    public void SetAcquisitionPrice_WithAlbumDateOnly_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2001);

        edition.SetAcquisitionPrice(10m, "EUR");

        Assert.Equal(10m, edition.AcquisitionAmount);
    }

    [Fact]
    public void SetAcquisitionDate_ClearedWhilePriceDependsOnIt_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetAcquisitionDate(new DateOnly(2020, 1, 1));
        edition.SetAcquisitionPrice(10m, "EUR");

        DomainAssert.Violates(DomainRules.EditionAcquisitionPriceReferenceDateRequired, () => edition.SetAcquisitionDate(null));
        Assert.NotNull(edition.AcquisitionDate);
    }

    [Fact]
    public void SetAcquisitionDate_ClearedWithAnotherReferenceDate_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetPublicationYear(2019);
        edition.SetAcquisitionDate(new DateOnly(2020, 1, 1));
        edition.SetAcquisitionPrice(10m, "EUR");

        edition.SetAcquisitionDate(null);

        Assert.Null(edition.AcquisitionDate);
    }

    [Fact]
    public void SetPublicationYear_ClearedWhilePriceDependsOnIt_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetPublicationYear(2019);
        edition.SetAcquisitionPrice(10m, "EUR");

        DomainAssert.Violates(DomainRules.EditionAcquisitionPriceReferenceDateRequired, () => edition.SetPublicationYear(null));
        Assert.Equal(2019, edition.PublicationYear);
    }

    [Fact]
    public void SetPublicationYear_ClearedWhileInitialValueDependsOnIt_Throws()
    {
        // The acquisition date keeps the price dated, but not the initial value.
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);
        edition.SetPublicationYear(2019);
        edition.SetAcquisitionDate(new DateOnly(2020, 1, 1));
        edition.SetInitialValue(15m, "EUR");

        DomainAssert.Violates(DomainRules.EditionInitialValueReferenceDateRequired, () => edition.SetPublicationYear(null));
    }

    [Fact]
    public void SetPublicationYear_ClearedWithAlbumDate_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2018);
        edition.SetPublicationYear(2019);
        edition.SetAcquisitionPrice(10m, "EUR");
        edition.SetInitialValue(15m, "EUR");

        edition.SetPublicationYear(null);

        Assert.Null(edition.PublicationYear);
    }

    [Fact]
    public void AlbumSetFirstPublicationDate_ClearedWhilePriceDependsOnIt_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2001);
        edition.SetAcquisitionPrice(10m, "EUR");

        DomainAssert.Violates(
            DomainRules.EditionAcquisitionPriceReferenceDateRequired, () => edition.Album.SetFirstPublicationDate(null, null));
        Assert.Equal(2001, edition.Album.FirstPublicationYear);
    }

    [Fact]
    public void AlbumSetFirstPublicationDate_ClearedWhileInitialValueDependsOnIt_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2001);
        edition.SetAcquisitionDate(new DateOnly(2020, 1, 1));
        edition.SetInitialValue(15m, "EUR");

        DomainAssert.Violates(
            DomainRules.EditionInitialValueReferenceDateRequired, () => edition.Album.SetFirstPublicationDate(null, null));
    }

    [Fact]
    public void AlbumSetFirstPublicationDate_ClearedWhileEditionsHaveTheirOwnDates_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2001);
        edition.SetPublicationYear(2003);
        edition.SetAcquisitionPrice(10m, "EUR");
        edition.SetInitialValue(15m, "EUR");

        edition.Album.SetFirstPublicationDate(null, null);

        Assert.Null(edition.Album.FirstPublicationYear);
    }

    [Fact]
    public void AlbumSetFirstPublicationDate_ChangedWhileAmountsDependOnIt_Succeeds()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase, albumYear: 2001);
        edition.SetAcquisitionPrice(10m, "EUR");

        edition.Album.SetFirstPublicationDate(2002, 5);

        Assert.Equal(2002, edition.Album.FirstPublicationYear);
    }

    [Fact]
    public void SetFree_True_ClearsInitialValue()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Gift, albumYear: 1978);
        edition.SetInitialValue(35m, "FRF");

        edition.SetFree(true);

        Assert.Null(edition.InitialValueAmount);
        Assert.Null(edition.InitialValueCurrency);
    }

    [Fact]
    public void SetFree_True_OnPurchasedEdition_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Purchase);

        DomainAssert.Violates(DomainRules.EditionPurchaseCannotBeFree, () => edition.SetFree(true));
        Assert.False(edition.IsFree);
    }

    [Fact]
    public void SetAcquisitionMode_PurchaseOnFreeEdition_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Gift);
        edition.SetFree(true);

        DomainAssert.Violates(DomainRules.EditionPurchaseCannotBeFree, () => edition.SetAcquisitionMode(AcquisitionMode.Purchase));
        Assert.Equal(AcquisitionMode.Gift, edition.AcquisitionMode);
    }

    [Fact]
    public void RecordAcquisition_PurchaseOfFreeEdition_ThrowsWithoutRealizingTheIntent()
    {
        // Freeness is a trait of the copy, left free of rules while the edition is not owned
        // (fonctionnel.md § Intention d'achat): it only conflicts with the purchase that acquires it.
        var edition = CreateEdition();
        var album = edition.Album;
        album.AddPurchaseIntent(edition);
        edition.SetFree(true);

        DomainAssert.Violates(DomainRules.EditionPurchaseCannotBeFree, () => album.RecordAcquisition(edition, AcquisitionMode.Purchase));
        Assert.Null(edition.AcquisitionMode);
        Assert.Single(album.PurchaseIntents);
    }

    [Fact]
    public void RecordAcquisition_GiftOfFreeEdition_Succeeds()
    {
        var edition = CreateEdition();
        edition.SetFree(true);

        edition.Album.RecordAcquisition(edition, AcquisitionMode.Gift);

        Assert.Equal(AcquisitionMode.Gift, edition.AcquisitionMode);
    }

    [Fact]
    public void SetAcquisitionMode_ToNullWithInitialValueSet_Throws()
    {
        var edition = CreateOwnedEdition(AcquisitionMode.Gift, albumYear: 1978);
        edition.SetInitialValue(35m, "FRF");

        DomainAssert.Violates(DomainRules.EditionAcquisitionModeRequired, () => edition.SetAcquisitionMode(null));
    }
}
