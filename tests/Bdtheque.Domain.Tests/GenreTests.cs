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
        DomainAssert.Violates<Genre>(DomainRules.GenreLabelRequired, nameof(Genre.Label), () => new Genre(label!));
    }

    [Fact]
    public void SetLabel_TrimsValue()
    {
        var genre = new Genre("  SF  ");

        Assert.Equal("SF", genre.Label);
    }

    // A genre is filed under the initial of its label, as it is sorted (visuel/listes/liste-par-liste.md, navigation par
    // initiale), the entries being those of every navigation (fonctionnel/presentation/tri-et-navigation-par-initiale.md).
    [Theory]
    [InlineData("Aventure", "A")]
    [InlineData("Épouvante", "E")]
    [InlineData("  western", "W")]
    [InlineData("1001 nuits", "#")]
    [InlineData("« Roman graphique »", "@")]
    public void Constructor_ComputesNavigationEntry(string label, string expectedEntry)
    {
        Assert.Equal(expectedEntry, new Genre(label).NavigationEntry);
    }

    [Fact]
    public void SetLabel_RecomputesNavigationEntry()
    {
        var genre = new Genre("Aventure");

        genre.SetLabel("Polar");

        Assert.Equal("P", genre.NavigationEntry);
    }

    [Fact]
    public void SetLabel_Rejected_KeepsNavigationEntry()
    {
        var genre = new Genre("Aventure");

        DomainAssert.Violates<Genre>(DomainRules.GenreLabelRequired, nameof(Genre.Label), () => genre.SetLabel(" "));

        Assert.Equal("A", genre.NavigationEntry);
    }
}
