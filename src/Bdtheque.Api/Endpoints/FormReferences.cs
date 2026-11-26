using Bdtheque.Api.Errors;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>The records an administration form references, shared by the resources whose forms list them.</summary>
internal static class FormReferences
{
    // Where the content of a form enters the records it added on the spot (its NewRecords).
    private const string AddedPath = "/newRecords";

    /// <summary>
    /// Loads the records with the given identifiers, each of which must still exist: a record deleted
    /// since the form was read is a functional error.
    /// </summary>
    /// <param name="added">The records the form added on the spot, by the key that stands for their identifier.</param>
    /// <exception cref="EntityNotFoundException">One of the records no longer exists.</exception>
    public static async Task<List<TEntity>> LoadAsync<TEntity>(
        DbSet<TEntity> set, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken,
        IReadOnlyDictionary<Guid, TEntity>? added = null)
        where TEntity : EntityBase
    {
        added ??= new Dictionary<Guid, TEntity>();
        var distinctIds = ids.Distinct().Where(id => !added.ContainsKey(id)).ToList();
        var entities = await set.Where(e => distinctIds.Contains(e.Id)).ToListAsync(cancellationToken);
        var missing = distinctIds.Except(entities.Select(e => e.Id)).ToList();
        return missing.Count == 0
            ? [.. entities, .. ids.Distinct().Where(added.ContainsKey).Select(id => added[id])]
            : throw new EntityNotFoundException(typeof(TEntity), missing[0]);
    }

    /// <summary>
    /// Loads the publisher and the publisher collection a form references, each of which must still
    /// exist when given, unless the form added it on the spot. Whether the collection belongs to the publisher is
    /// left to the domain operation that receives both: a mismatch is a business error.
    /// </summary>
    /// <exception cref="EntityNotFoundException">The publisher or the collection no longer exists.</exception>
    public static async Task<(Publisher? Publisher, PublisherCollection? Collection)> LoadPublisherAsync(
        BdthequeDbContext context, Guid? publisherId, Guid? collectionId, AddedRecords added, CancellationToken cancellationToken)
    {
        Publisher? publisher = null;
        if (publisherId is { } pid && !added.Publishers.TryGetValue(pid, out publisher))
            publisher = await context.Publishers.SingleOrDefaultAsync(p => p.Id == pid, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(Publisher), pid);
        PublisherCollection? collection = null;
        if (collectionId is { } cid && !added.PublisherCollections.TryGetValue(cid, out collection))
            collection = await context.PublisherCollections.SingleOrDefaultAsync(c => c.Id == cid, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(PublisherCollection), cid);
        return (publisher, collection);
    }

    /// <summary>Replaces the current associations (genres, universes…) by the wanted ones.</summary>
    public static void ReplaceAssociations<TEntity>(
        IReadOnlyCollection<TEntity> current, IReadOnlyList<TEntity> wanted, Action<TEntity> add, Action<TEntity> remove)
        where TEntity : EntityBase
    {
        foreach (var removed in current.Where(c => wanted.All(w => w.Id != c.Id)).ToList())
            remove(removed);
        foreach (var entity in wanted)
            add(entity);
    }

    /// <summary>Loads the authors of the contributions of a form, each of which must still exist.</summary>
    /// <exception cref="EntityNotFoundException">One of the authors no longer exists.</exception>
    public static async Task<List<(Author Author, DomainEnums.ContributionRole Role)>> LoadContributionsAsync(
        BdthequeDbContext context, IReadOnlyList<ContributionContent> contributions, CancellationToken cancellationToken,
        IReadOnlyDictionary<Guid, Author>? added = null)
    {
        var authors = (await LoadAsync(context.Authors, [.. contributions.Select(c => c.AuthorId)], cancellationToken, added))
            .ToDictionary(a => a.Id);
        return [.. contributions
            .Select(c => (added is not null && added.TryGetValue(c.AuthorId, out var author) ? author : authors[c.AuthorId],
                EnumMapping.Map<DomainEnums.ContributionRole>(c.Role)!.Value))];
    }

    /// <summary>
    /// Creates, through the domain, the records the form added on the spot, in the transaction of the record it saves: they are
    /// saved with it, or not at all (choix-implementation/api/organisation-de-l-api.md). Each is placed where the content enters
    /// it, so that an error it raises designates it, as any record the request writes
    /// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md). A new universe is at the top of the
    /// hierarchy, which it changes in nothing that could form a cycle: it takes no hierarchy lock. A new collection of an
    /// existing publisher writes a child of the aggregate of that publisher, whose root it locks and marks
    /// (choix-implementation/persistance/concurrence-d-acces.md).
    /// </summary>
    public static async Task<AddedRecords> CreateAddedAsync(
        BdthequeDbContext context, ContentFields fields, NewRecords? records, CancellationToken cancellationToken)
    {
        records ??= NewRecords.None;
        var authors = records.Authors.Select((a, index) => (a.Key, Author: fields.Create(
                $"{AddedPath}/authors/{index}", () => new Author(a.LastName, a.FirstName, a.Pseudonym), AuthorEndpoints.EnteredOtherwise(a.Pseudonym))))
            .ToDictionary(a => a.Key, a => a.Author);
        var genres = records.Genres.Select((g, index) => (g.Key, Genre: fields.Create($"{AddedPath}/genres/{index}", () => new Genre(g.Label))))
            .ToDictionary(g => g.Key, g => g.Genre);
        var universes = records.Universes.Select((u, index) => (u.Key, Universe: fields.Create(
                $"{AddedPath}/universes/{index}", () => new Universe(u.Name))))
            .ToDictionary(u => u.Key, u => u.Universe);

        var series = records.Series.Select((s, index) => (s.Key, Series: fields.Create(
                $"{AddedPath}/series/{index}", () => Series.WithProposedEditionTemplate(s.Title))))
            .ToDictionary(s => s.Key, s => s.Series);
        var publishers = records.Publishers.Select((p, index) => (p.Key, Publisher: fields.Create(
                $"{AddedPath}/publishers/{index}", () => new Publisher(p.Name))))
            .ToDictionary(p => p.Key, p => p.Publisher);

        context.Authors.AddRange(authors.Values);
        context.Genres.AddRange(genres.Values);
        context.Universes.AddRange(universes.Values);
        context.Series.AddRange(series.Values);
        context.Publishers.AddRange(publishers.Values);

        var collections = new Dictionary<Guid, PublisherCollection>();
        foreach (var (added, index) in records.PublisherCollections.Select((c, index) => (c, index)))
        {
            var publisher = publishers.GetValueOrDefault(added.PublisherId)
                            ?? await context.LoadOtherAggregateForWriteAsync<Publisher>(added.PublisherId, cancellationToken);
            var collection = fields.Create($"{AddedPath}/publisherCollections/{index}", () => publisher.AddCollection(added.Name));
            context.PublisherCollections.Add(collection);
            collections[added.Key] = collection;
        }

        return new AddedRecords(authors, genres, universes, series, publishers, collections);
    }

    /// <summary>The authors of the contributions, each once, named as the lists name them.</summary>
    public static List<AuthorListItem> Authors(IEnumerable<Contribution> contributions) =>
        [.. contributions.Select(c => c.Author).DistinctBy(a => a.Id).Select(a => new AuthorListItem(a.Id, a.LastName, a.FirstName, a.Pseudonym))];

    /// <summary>The genres, named as the lists name them, in their order.</summary>
    public static List<GenreListItem> Genres(IEnumerable<Genre> genres) =>
        [.. genres.OrderBy(g => g.Label).ThenBy(g => g.Id).Select(g => new GenreListItem(g.Id, g.Label))];

    /// <summary>
    /// The universes, each with its ancestors, a universe being presented with them: the hierarchy is only read for a record
    /// attached to a universe.
    /// </summary>
    public static async Task<IReadOnlyList<UniverseInHierarchy>> UniversesInHierarchyAsync(
        BdthequeDbContext context, IReadOnlyCollection<Universe> universes, CancellationToken cancellationToken)
    {
        if (universes.Count == 0)
            return [];
        var tree = await UniverseTree.LoadAsync(context, cancellationToken);
        return [.. universes.OrderBy(u => u.Name).ThenBy(u => u.Id).Select(u => tree.InHierarchy(u.Id))];
    }

    /// <summary>The contributions as a form lists them, in a stable order.</summary>
    public static List<ContributionContent> ToContents(IEnumerable<Contribution> contributions) =>
        [.. contributions
            .OrderBy(c => c.Role).ThenBy(c => c.AuthorId)
            .Select(c => new ContributionContent(c.AuthorId, EnumMapping.Map<ContractEnums.ContributionRole>(c.Role)!.Value))];
}

/// <summary>The records a form added on the spot, created with the record it saves, by the key that stands for their identifier.</summary>
internal sealed record AddedRecords(
    IReadOnlyDictionary<Guid, Author> Authors, IReadOnlyDictionary<Guid, Genre> Genres, IReadOnlyDictionary<Guid, Universe> Universes,
    IReadOnlyDictionary<Guid, Series> Series, IReadOnlyDictionary<Guid, Publisher> Publishers,
    IReadOnlyDictionary<Guid, PublisherCollection> PublisherCollections);
