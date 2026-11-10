namespace Bdtheque.Domain.Common;

/// <summary>
/// The identity of an author derived from its names (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes): its
/// main identifier, the pseudonym if any, otherwise "First Last"; its sort key, the identifier, except that "First Last" sorts as
/// "Last First" to file authors by family name; and the entry of the navigation, from the sort key. Shared by
/// <see cref="Entities.Author"/>, which stores the key and the entry, and by the entry form, which presents them while the names
/// are typed.
/// </summary>
public sealed record AuthorIdentity(string DisplayName, string SortKey, string NavigationEntry)
{
    /// <summary>The identity of these names, trimmed; none without a last name or a pseudonym, which an author must have.</summary>
    public static AuthorIdentity? Of(string? lastName, string? firstName, string? pseudonym)
    {
        var last = DomainText.NullIfBlank(lastName);
        var first = DomainText.NullIfBlank(firstName);
        var (displayName, sortKey) = (DomainText.NullIfBlank(pseudonym), last, first) switch
        {
            ({ } alias, _, _) => (alias, alias),
            (null, null, _) => (null, null),
            (null, { } family, null) => (family, family),
            (null, { } family, { } given) => ($"{given} {family}", $"{family} {given}"),
        };
        return sortKey is null ? null : new AuthorIdentity(displayName!, sortKey, NavigationEntryCalculator.Compute(sortKey));
    }
}
