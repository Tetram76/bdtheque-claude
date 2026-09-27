using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A person or collective credited on albums (Auteur / Artiste).
/// At least one of <see cref="LastName"/> or <see cref="Pseudonym"/> must be non-empty.
/// </summary>
public sealed class Author : EntityBase
{
    public string? LastName { get; private set; }
    public string? FirstName { get; private set; }
    public string? Pseudonym { get; private set; }
    public string? Biography { get; private set; }
    public string? Nationality { get; private set; }

    // EF Core parameterless constructor
    private Author() { }

    public Author(string? lastName, string? firstName, string? pseudonym)
    {
        ValidateIdentity(lastName, pseudonym);
        LastName = NullIfEmpty(lastName);
        FirstName = NullIfEmpty(firstName);
        Pseudonym = NullIfEmpty(pseudonym);
    }

    public void UpdateIdentity(string? lastName, string? firstName, string? pseudonym)
    {
        ValidateIdentity(lastName, pseudonym);
        LastName = NullIfEmpty(lastName);
        FirstName = NullIfEmpty(firstName);
        Pseudonym = NullIfEmpty(pseudonym);
    }

    public void UpdateBiography(string? biography) => Biography = NullIfEmpty(biography);

    public void UpdateNationality(string? nationality) => Nationality = NullIfEmpty(nationality);

    private static void ValidateIdentity(string? lastName, string? pseudonym)
    {
        if (string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(pseudonym))
            throw new ArgumentException("An author must have at least a last name or a pseudonym.");
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
