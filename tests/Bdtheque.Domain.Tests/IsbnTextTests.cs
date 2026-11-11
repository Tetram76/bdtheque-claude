using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Tests;

public sealed class IsbnTextTests
{
    [Theory]
    [InlineData("2-905123-01-x", "2-905123-01-X")]
    [InlineData("  097522980x ", "097522980X")]
    [InlineData("978-2-205-00217-2", "978-2-205-00217-2")]
    public void Normalize_TrimsAndWritesTheCheckCharacterInCapitals(string isbn, string expected) =>
        Assert.Equal(expected, IsbnText.Normalize(isbn));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Blank_IsNull(string? isbn) => Assert.Null(IsbnText.Normalize(isbn));
}
