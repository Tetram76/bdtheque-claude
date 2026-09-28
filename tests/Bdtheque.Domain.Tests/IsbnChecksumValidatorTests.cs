using Bdtheque.Domain.Common;

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
    public void IsValid_ValidIsbn13_ReturnsTrue(string isbn)
    {
        Assert.True(IsbnChecksumValidator.IsValid(isbn));
    }

    [Fact]
    public void IsValid_Isbn13WrongCheckDigit_ReturnsFalse()
    {
        Assert.False(IsbnChecksumValidator.IsValid("9780306406158"));
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
}
