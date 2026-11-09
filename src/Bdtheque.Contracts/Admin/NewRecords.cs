namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The records an entry form adds without creating them (authors, genres, universes added on the spot): they are created with
/// the record the form saves, in the same transaction, or not at all. The form references each by its <c>Key</c>, which it
/// chose, in place of an identifier, among the records it cites.
/// </summary>
public sealed record NewRecords(IReadOnlyList<NewAuthor> Authors, IReadOnlyList<NewGenre> Genres, IReadOnlyList<NewUniverse> Universes)
{
    public static readonly NewRecords None = new([], [], []);
}

public sealed record NewAuthor(Guid Key, string? LastName, string? FirstName, string? Pseudonym);

public sealed record NewGenre(Guid Key, string Label);

/// <param name="ParentId">An existing universe, if the new one belongs to it.</param>
public sealed record NewUniverse(Guid Key, string Name, Guid? ParentId);
