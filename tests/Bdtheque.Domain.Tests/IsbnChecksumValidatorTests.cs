namespace Bdtheque.Domain.Tests;

public sealed class IsbnChecksumValidatorTests
{
    [Theory]
    [InlineData("0306406152")]
    [InlineData("0-306-40615-2")]
    [InlineData("097522980X")]
    [InlineData("0-9752298-0-X")]
    public void IsValid_ValidIsbn10_ReturnsTrue(string isbn)
    {
        Assert.True(IsbnChecksumValidator.IsValid(isbn));
    }

    [Theory]
    [InlineData("0306406151")]
    [InlineData("097522980Y")]
    public void IsValid_Isbn10WrongCheckDigit_ReturnsFalse(string isbn)
    {
        Assert.False(IsbnChecksumValidator.IsValid(isbn));
    }

    [Theory]
    [InlineData("9780306406157")]
    [InlineData("978-0-306-40615-7")]
    // Its digits of even rank do not sum to a multiple of 5, unlike the two above: only then does a weight wrong on
    // them change the check.
    [InlineData("9782203025127")]
    public void IsValid_ValidIsbn13_ReturnsTrue(string isbn)
    {
        Assert.True(IsbnChecksumValidator.IsValid(isbn));
    }

    [Fact]
    public void IsValid_Isbn13WrongCheckDigit_ReturnsFalse()
    {
        Assert.False(IsbnChecksumValidator.IsValid("9780306406158"));
    }

    [Fact]
    public void IsValid_ChecksumValidEan13WithNonIsbnPrefix_ReturnsFalse()
    {
        // "4006381333931" is a real, checksum-valid EAN-13 (a German retail barcode) but is not
        // an ISBN: only the Bookland prefixes 978/979 identify an EAN-13 as an ISBN-13. A
        // checksum-only check would wrongly accept it, masking a genuine data-entry mistake
        // (e.g. a non-book barcode pasted into the ISBN field).
        Assert.False(IsbnChecksumValidator.IsValid("4006381333931"));
    }

    [Theory]
    [InlineData("4080000000015")] // third digit of a Bookland prefix, without its "97"
    [InlineData("0780000000001")] // "78" of a Bookland prefix, without its "9"
    [InlineData("9980000000006")] // "9" and "8" of a Bookland prefix, without its "7"
    public void IsValid_ChecksumValidEan13WithAPartOfTheBooklandPrefix_ReturnsFalse(string ean)
    {
        Assert.False(IsbnChecksumValidator.IsValid(ean));
    }

    [Fact]
    public void IsValid_Isbn13WithBookland979Prefix_ReturnsTrue()
    {
        Assert.True(IsbnChecksumValidator.IsValid("9791234567896"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("12345678901234")]
    [InlineData("030640615A")]
    [InlineData("978030640615A")]
    public void IsValid_MalformedInput_ReturnsFalse(string? isbn)
    {
        Assert.False(IsbnChecksumValidator.IsValid(isbn));
    }

    [Fact]
    public void IsValid_Isbn10LowercaseXCheckDigit_ReturnsTrue()
    {
        Assert.True(IsbnChecksumValidator.IsValid("097522980x"));
    }

    // fonctionnel/regles-metier/validation-de-l-isbn.md, ISBN corrigé: the digits entered, the check character alone
    // computed again, in the same format and with the same separators.
    [Theory]
    [InlineData("978-2-505-00865-3", "978-2-505-00865-1")]
    [InlineData("9782505008653", "9782505008651")]
    [InlineData("979 1234567 89 0", "979 1234567 89 6")]
    [InlineData("2-8001-0123-4", "2-8001-0123-7")]
    [InlineData("0-9752298-0-1", "0-9752298-0-X")]
    [InlineData("0-306-40615-X", "0-306-40615-2")]
    [InlineData("0-306-40615-x", "0-306-40615-2")]
    public void Corrected_AnIsbnWithAWrongCheckCharacter_ComputesItAgain(string isbn, string expected)
    {
        Assert.Equal(expected, IsbnChecksumValidator.Corrected(isbn));
    }

    [Theory]
    [InlineData("978-0-306-40615-7")]
    [InlineData("0-9752298-0-X")]
    public void Corrected_AConsistentIsbn_IsTheIsbnItself(string isbn)
    {
        Assert.Equal(isbn, IsbnChecksumValidator.Corrected(isbn));
    }

    // An ISBN the check character alone cannot make consistent: no ISBN can be supposed.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2-7234-3451")]
    [InlineData("12345678901234")]
    [InlineData("4006381333932")]
    [InlineData("03064A6152")]
    [InlineData("030640615A")]
    [InlineData("978030640615X")]
    public void Corrected_AnIsbnNoCheckCharacterMakesConsistent_IsNone(string? isbn)
    {
        Assert.Null(IsbnChecksumValidator.Corrected(isbn));
    }
}
