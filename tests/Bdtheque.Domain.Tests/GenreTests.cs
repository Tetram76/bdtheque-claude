using Bdtheque.Domain.Entities;

namespace Bdtheque.Domain.Tests;

public sealed class GenreTests
{
    [Fact]
    public void Constructor_ValidLabel_Succeeds()
    {
        var genre = new Genre("Science-fiction");

        Assert.Equal("Science-fiction", genre.Label);
        Assert.NotEqual(Guid.Empty, genre.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyLabel_Throws(string? label)
    {
        DomainAssert.Violates(DomainRules.GenreLabelRequired, () => new Genre(label!));
    }

    [Fact]
    public void SetLabel_TrimsValue()
    {
        var genre = new Genre("  SF  ");

        Assert.Equal("SF", genre.Label);
    }
}
