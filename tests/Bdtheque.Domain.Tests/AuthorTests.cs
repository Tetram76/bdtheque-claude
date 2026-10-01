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
        DomainAssert.Violates(DomainRules.AuthorLastNameOrPseudonymRequired, () => new Author(lastName, firstName, pseudonym));
    }

    [Fact]
    public void UpdateIdentity_BothNullOrEmpty_Throws()
    {
        var author = new Author("Hugo", null, null);

        DomainAssert.Violates(DomainRules.AuthorLastNameOrPseudonymRequired, () => author.UpdateIdentity(null, null, null));
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

    // Examples of fonctionnel.md § Artistes.
    [Theory]
    [InlineData("Van Hamme", "Jean", null, "Van Hamme Jean", "V")]
    [InlineData(null, null, "Moebius", "Moebius", "M")]
    [InlineData("Remi", "Georges", "Hergé", "Hergé", "H")]
    [InlineData("Hugo", null, null, "Hugo", "H")]
    [InlineData("Œuvre", "Collectif", null, "Œuvre Collectif", "O")]
    public void Constructor_ComputesSortKeyAndNavigationEntry(
        string? lastName, string? firstName, string? pseudonym, string expectedSortKey, string expectedEntry)
    {
        var author = new Author(lastName, firstName, pseudonym);

        Assert.Equal(expectedSortKey, author.SortKey);
        Assert.Equal(expectedEntry, author.NavigationEntry);
    }

    [Fact]
    public void UpdateIdentity_RecomputesSortKeyAndNavigationEntry()
    {
        var author = new Author(null, null, "Moebius");

        author.UpdateIdentity("Giraud", "Jean", null);

        Assert.Equal("Giraud Jean", author.SortKey);
        Assert.Equal("G", author.NavigationEntry);
    }

    [Fact]
    public void UpdateIdentity_Rejected_KeepsSortKeyAndNavigationEntry()
    {
        var author = new Author(null, null, "Moebius");

        DomainAssert.Violates(DomainRules.AuthorLastNameOrPseudonymRequired, () => author.UpdateIdentity(null, "Jean", null));

        Assert.Equal("Moebius", author.SortKey);
        Assert.Equal("M", author.NavigationEntry);
    }
}
