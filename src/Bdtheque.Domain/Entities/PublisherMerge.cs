namespace Bdtheque.Domain.Entities;

/// <summary>
/// What the merge of two publishers replaces (fonctionnel/regles-metier/fusion-des-fiches.md), which every edition and series
/// designating either of them follows: the publisher deleted by the publisher that remains, and each collection removed as
/// homonymous to one that remains by that one.
/// </summary>
public sealed class PublisherMerge
{
    private readonly Dictionary<Guid, PublisherCollection> _replaced;

    internal PublisherMerge(Publisher merged, Publisher kept, Dictionary<Guid, PublisherCollection> replaced)
    {
        Merged = merged;
        Kept = kept;
        _replaced = replaced;
    }

    /// <summary>The publisher the merge deletes.</summary>
    public Publisher Merged { get; }

    /// <summary>The publisher that remains, which has taken the collections of the other one.</summary>
    public Publisher Kept { get; }

    /// <summary>Each collection the merge removes, by its identifier, with the collection that remains in its place.</summary>
    public IReadOnlyDictionary<Guid, PublisherCollection> Replaced => _replaced;

    /// <summary>Whether a record of <paramref name="publisherId"/>, in <paramref name="collectionId"/>, designates what the merge replaces.</summary>
    internal bool Concerns(Guid? publisherId, Guid? collectionId) =>
        publisherId == Merged.Id || (collectionId is { } id && _replaced.ContainsKey(id));

    /// <summary>The collection a record keeps in place of its own: the one that remains in place of a collection removed.</summary>
    /// <param name="collectionId">The collection of the record, by its identifier.</param>
    /// <param name="collection">The same collection, which the record must have loaded.</param>
    /// <exception cref="InvalidOperationException">
    /// The collection of the record is not loaded: transferring the record would lose it, a programming error.
    /// </exception>
    internal PublisherCollection? CollectionReplacing(Guid? collectionId, PublisherCollection? collection)
    {
        if (collectionId is null)
            return null;
        if (collection is null)
            throw new InvalidOperationException($"The collection {collectionId} must be loaded for its record to pass to another publisher.");
        return _replaced.GetValueOrDefault(collection.Id, collection);
    }
}
