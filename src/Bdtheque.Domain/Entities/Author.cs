using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A person or collective credited on albums (Auteur / Artiste).
/// At least one of <see cref="LastName"/> or <see cref="Pseudonym"/> must be non-empty.
/// </summary>
public sealed class Author : EntityBase, IAggregateRoot
{
    public string? LastName { get; private set; }
    public string? FirstName { get; private set; }
    public string? Pseudonym { get; private set; }
    public string? Biography { get; private set; }
    public string? Nationality { get; private set; }

    // Stored rather than computed at read time, so that the database can sort and paginate
    // authors (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes). Derived from the
    // identity only: never set manually.
    public string SortKey { get; private set; } = string.Empty;
    public string NavigationEntry { get; private set; } = string.Empty;

    // EF Core parameterless constructor
    private Author() { }

    public Author(string? lastName, string? firstName, string? pseudonym)
    {
        UpdateIdentity(lastName, firstName, pseudonym);
    }

    public void UpdateIdentity(string? lastName, string? firstName, string? pseudonym)
    {
        var identity = AuthorIdentity.Of(lastName, firstName, pseudonym)
                       ?? throw new DomainRuleViolationException(
                           DomainRules.AuthorLastNameOrPseudonymRequired, "An author must have at least a last name or a pseudonym.");

        LastName = DomainText.NullIfBlank(lastName);
        FirstName = DomainText.NullIfBlank(firstName);
        Pseudonym = DomainText.NullIfBlank(pseudonym);

        SortKey = identity.SortKey;
        NavigationEntry = identity.NavigationEntry;
    }

    public void UpdateBiography(string? biography) => Biography = DomainText.NullIfBlank(biography);

    public void UpdateNationality(string? nationality) => Nationality = DomainText.NullIfBlank(nationality);
}
