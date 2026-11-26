using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// An entity that publishes editions (Éditeur).
/// </summary>
public sealed class Publisher : EntityBase, IAggregateRoot
{
    public string Name { get; private set; } = null!;
    public string? Website { get; private set; }

    // Stored rather than computed at read time, so that the database can filter the list of the publishers on it and
    // index it (choix-implementation/api/navigation-par-initiale-mise-en-oeuvre.md). Derived from the name, as sorted.
    public string NavigationEntry { get; private set; } = null!;

    // Read-only from outside: a collection is only ever created through AddCollection, and only moves to another publisher
    // by the merge of its publisher into that one (Absorb), which every edition and series template relying on it belonging
    // to its publisher follows.
    private readonly List<PublisherCollection> _collections = [];
    public IReadOnlyCollection<PublisherCollection> Collections => _collections;

    // EF Core parameterless constructor
    private Publisher() { }

    public Publisher(string name)
    {
        SetName(name);
    }

    public void SetName(string name)
    {
        Name = DomainText.Required(name, DomainRules.PublisherNameRequired, "A publisher must have a name.", this, nameof(Name));
        NavigationEntry = NavigationEntryCalculator.Compute(Name);
    }

    public PublisherCollection AddCollection(string name)
    {
        var collection = new PublisherCollection(name, this);
        _collections.Add(collection);
        return collection;
    }

    /// <summary>
    /// Removes one of its collections, deleted with the publisher's next save. Whether anything still references it
    /// (editions, series templates) is checked by whoever saves, which alone sees the other records.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection is not one of this publisher's.</exception>
    public void RemoveCollection(PublisherCollection collection)
    {
        if (!_collections.Remove(collection))
            throw new InvalidOperationException($"Collection {collection.Id} does not belong to publisher {Id}.");
    }

    /// <summary>
    /// Takes the collections of <paramref name="merged"/>, as the merge of two publishers does
    /// (fonctionnel/regles-metier/fusion-des-fiches.md): each one passes to this publisher, keeping its identity, but two
    /// homonymous collections, one of each publisher, become one, the one that remains taking the place of the other.
    /// </summary>
    /// <remarks>
    /// Which collections are homonymous is for whoever reads them to say: their names compare as their column compares them,
    /// without case nor accents (choix-implementation/persistance/collation-des-colonnes-texte.md). The editions and series
    /// templates designating either publisher are then transferred by their own aggregates, from what this returns.
    /// </remarks>
    /// <param name="homonyms">Each pair of homonymous collections: the one that remains, and the one it replaces.</param>
    /// <returns>What the merge replaces, which the editions and series templates follow.</returns>
    /// <exception cref="ArgumentException">
    /// The same publisher twice, or a pair that is not one collection of each publisher, each collection once.
    /// </exception>
    public PublisherMerge Absorb(Publisher merged, IReadOnlyCollection<(PublisherCollection Remaining, PublisherCollection Removed)> homonyms)
    {
        MergeGuard.EnsureDistinct(merged, this);
        var paired = homonyms.SelectMany(h => new[] { h.Remaining, h.Removed }).ToList();
        if (homonyms.Any(h => !(Owns(h.Remaining) && merged.Owns(h.Removed)) && !(merged.Owns(h.Remaining) && Owns(h.Removed)))
            || paired.Distinct().Count() != paired.Count)
            throw new ArgumentException("Each pair must be one collection of each publisher, each collection once.", nameof(homonyms));

        var replaced = new Dictionary<Guid, PublisherCollection>();
        foreach (var (remaining, removed) in homonyms)
        {
            (Owns(removed) ? this : merged).RemoveCollection(removed);
            replaced.Add(removed.Id, remaining);
        }

        foreach (var collection in merged._collections)
        {
            collection.MoveTo(this);
            _collections.Add(collection);
        }

        merged._collections.Clear();
        return new PublisherMerge(merged, this, replaced);
    }

    private bool Owns(PublisherCollection collection) => _collections.Contains(collection);

    public void SetWebsite(string? website)
    {
        var trimmed = DomainText.NullIfBlank(website);
        if (trimmed is not null
            && (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            throw new DomainRuleViolationException(
                DomainRules.PublisherWebsiteInvalid, $"'{trimmed}' is not a valid absolute web URL (http or https required).", this,
                nameof(Website));

        Website = trimmed;
    }
}
