namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The records an entry form adds without creating them (authors, genres, universes, series, publishers and publisher collections
/// added on the spot): they are created with the record the form saves, in the same transaction, or not at all. The form references
/// each by its <c>Key</c>, which it chose, in place of an identifier, among the records it cites.
/// </summary>
public sealed record NewRecords(
    IReadOnlyList<NewAuthor> Authors, IReadOnlyList<NewGenre> Genres, IReadOnlyList<NewUniverse> Universes, IReadOnlyList<NewSeries> Series,
    IReadOnlyList<NewPublisher> Publishers, IReadOnlyList<NewPublisherCollection> PublisherCollections)
{
    public static readonly NewRecords None = new([], [], [], [], [], []);

    /// <summary>Whether no record was added: a form then sends none. A method, so that it is no part of the content sent.</summary>
    public bool IsEmpty() =>
        Authors.Count + Genres.Count + Universes.Count + Series.Count + Publishers.Count + PublisherCollections.Count == 0;
}

public sealed record NewAuthor(Guid Key, string? LastName, string? FirstName, string? Pseudonym);

public sealed record NewGenre(Guid Key, string Label);

/// <param name="ParentId">An existing universe, if the new one belongs to it.</param>
public sealed record NewUniverse(Guid Key, string Name, Guid? ParentId);

/// <summary>A series created by its title alone, which proposes to its new editions what any new series proposes.</summary>
public sealed record NewSeries(Guid Key, string Title);

/// <summary>A publisher created by its name alone, without website nor collection, which its own form completes.</summary>
public sealed record NewPublisher(Guid Key, string Name);

/// <summary>A collection created by its name alone, in one of the publishers of the form.</summary>
/// <param name="PublisherId">Its publisher: an existing one, or the key of a publisher added on the spot by the same form.</param>
public sealed record NewPublisherCollection(Guid Key, Guid PublisherId, string Name);
