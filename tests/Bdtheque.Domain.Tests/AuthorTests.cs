using Bdtheque.Domain.Entities;

namespace Bdtheque.Domain.Tests;

public sealed class AuthorTests
{
    [Theory]
    [InlineData("Hugo", null, null)]
    [InlineData(null, null, "Hergé")]
    [InlineData("Remi", "Georges", "Hergé")]
    public void Constructor_ValidIdentity_Succeeds(string? lastName, string? firstName, string? pseudonym)
    {
        var author = new Author(lastName, firstName, pseudonym);

        Assert.NotEqual(Guid.Empty, author.Id);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", "", null)]
    [InlineData(null, "Georges", "")]
    [InlineData("  ", null, "  ")]
    public void Constructor_NeitherLastNameNorPseudonym_Throws(string? lastName, string? firstName, string? pseudonym)
    {
        Assert.Throws<ArgumentException>(() => new Author(lastName, firstName, pseudonym));
    }

    [Fact]
    public void UpdateIdentity_BothNullOrEmpty_Throws()
    {
        var author = new Author("Hugo", null, null);

        Assert.Throws<ArgumentException>(() => author.UpdateIdentity(null, null, null));
    }

    [Fact]
    public void UpdateIdentity_ValidIdentity_Updates()
    {
        var author = new Author("Hugo", null, null);

        author.UpdateIdentity("Remi", "Georges", "Hergé");

        Assert.Equal("Remi", author.LastName);
        Assert.Equal("Georges", author.FirstName);
        Assert.Equal("Hergé", author.Pseudonym);
    }

    [Fact]
    public void Constructor_WhitespaceLastName_NormalizesToNull()
    {
        var author = new Author("  ", null, "Moebius");

        Assert.Null(author.LastName);
        Assert.Equal("Moebius", author.Pseudonym);
    }

    [Fact]
    public void Constructor_TrimsValues()
    {
        var author = new Author("  Hugo  ", "  Victor  ", null);

        Assert.Equal("Hugo", author.LastName);
        Assert.Equal("Victor", author.FirstName);
    }

    [Fact]
    public void UniqueIds_TwoAuthors_HaveDifferentIds()
    {
        var a1 = new Author("Hugo", null, null);
        var a2 = new Author(null, null, "Hergé");

        Assert.NotEqual(a1.Id, a2.Id);
    }
}
