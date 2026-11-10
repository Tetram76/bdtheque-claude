using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// The identity of an author as the domain derives it from its names (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes):
/// the main identifier displayed, the sort key and the entry of the navigation, which an entry form presents while the names
/// are typed.
/// </summary>
public sealed class AuthorIdentityTests
{
    // Examples of fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes.
    [Theory]
    [InlineData("Van Hamme", "Jean", null, "Jean Van Hamme", "Van Hamme Jean", "V")]
    [InlineData(null, null, "Moebius", "Moebius", "Moebius", "M")]
    [InlineData("Remi", "Georges", "Hergé", "Hergé", "Hergé", "H")]
    [InlineData("  Hugo  ", "  ", null, "Hugo", "Hugo", "H")]
    [InlineData(" Œuvre ", " Collectif ", " ", "Collectif Œuvre", "Œuvre Collectif", "O")]
    public void Of_ANameOrAPseudonym_IsItsIdentifierItsSortKeyAndItsEntry(
        string? lastName, string? firstName, string? pseudonym, string displayName, string sortKey, string entry)
    {
        Assert.Equal(new AuthorIdentity(displayName, sortKey, entry), AuthorIdentity.Of(lastName, firstName, pseudonym));
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(" ", "Jean", " ")]
    public void Of_NeitherALastNameNorAPseudonym_IsNone(string? lastName, string? firstName, string? pseudonym)
    {
        Assert.Null(AuthorIdentity.Of(lastName, firstName, pseudonym));
    }
}
