using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Entries;

/// <summary>
/// An author as its side panel holds it (visuel/saisie-auteur/contenu.md), changed or new: what is typed, with the version it
/// was read at. Persisted from the prerendering into the interactive circuit, which reads it once.
/// </summary>
public sealed class AuthorEntry
{
    /// <summary>None for a new author.</summary>
    public Guid? Id { get; set; }

    /// <summary>The version the author was read at, which its saving is written under.</summary>
    public uint Version { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    // A field emptied is no value, as one never typed in: the same datum for the mark, and the same content sent.
    public string? FirstName { get; set => field = EntryText.Typed(value); }

    public string? LastName { get; set => field = EntryText.Typed(value); }

    public string? Pseudonym { get; set => field = EntryText.Typed(value); }

    public string? Nationality { get; set => field = EntryText.Typed(value); }

    public string? Biography { get; set => field = EntryText.Typed(value); }

    public static AuthorEntry Of(AuthorForm author) => new()
    {
        Id = author.Id,
        Version = author.Version,
        CreatedAt = author.CreatedAt,
        ModifiedAt = author.ModifiedAt,
        FirstName = author.FirstName,
        LastName = author.LastName,
        Pseudonym = author.Pseudonym,
        Nationality = author.Nationality,
        Biography = author.Biography,
    };

    /// <summary>
    /// A new author; opened on a search that finds none, the author searched, its name split as when created on the spot
    /// (visuel/saisie-auteur/contenu.md).
    /// </summary>
    public static AuthorEntry New(string? searched)
    {
        var entry = new AuthorEntry();
        if (searched is not null)
            (entry.FirstName, entry.LastName) = AuthorNames.Split(searched);
        return entry;
    }

    public AuthorEntry Copy() => (AuthorEntry)MemberwiseClone();

    public CreateAuthorRequest ToCreation() => new(LastName, FirstName, Pseudonym, Biography, Nationality);

    public UpdateAuthorRequest ToUpdate() => new(LastName, FirstName, Pseudonym, Biography, Nationality, Version);
}
