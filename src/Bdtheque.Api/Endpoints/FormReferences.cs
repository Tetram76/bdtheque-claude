using Bdtheque.Contracts.Admin;
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
    /// exist when given. Whether the collection belongs to the publisher is left to the domain
    /// operation that receives both: a mismatch is a business error.
    /// </summary>
    /// <exception cref="EntityNotFoundException">The publisher or the collection no longer exists.</exception>
    public static async Task<(Publisher? Publisher, PublisherCollection? Collection)> LoadPublisherAsync(
        BdthequeDbContext context, Guid? publisherId, Guid? collectionId, CancellationToken cancellationToken)
    {
        var publisher = publisherId is { } pid
            ? await context.Publishers.SingleOrDefaultAsync(p => p.Id == pid, cancellationToken)
              ?? throw new EntityNotFoundException(typeof(Publisher), pid)
            : null;
        var collection = collectionId is { } cid
            ? await context.PublisherCollections.SingleOrDefaultAsync(c => c.Id == cid, cancellationToken)
              ?? throw new EntityNotFoundException(typeof(PublisherCollection), cid)
            : null;
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
        var authors = (await LoadAsync(context.Authors, contributions.Select(c => c.AuthorId).ToList(), cancellationToken, added))
            .ToDictionary(a => a.Id);
        return contributions
            .Select(c => (added is not null && added.TryGetValue(c.AuthorId, out var author) ? author : authors[c.AuthorId],
                EnumMapping.Map<DomainEnums.ContributionRole>(c.Role)!.Value))
            .ToList();
    }

    /// <summary>
    /// Creates, through the domain, the records the form added on the spot, in the transaction of the record it saves: they are
    /// saved with it, or not at all (choix-implementation/api/organisation-de-l-api.md). A new universe whose parent is given
    /// takes the hierarchy lock, as any change of parent.
    /// </summary>
    public static async Task<AddedRecords> CreateAddedAsync(BdthequeDbContext context, NewRecords? records, CancellationToken cancellationToken)
    {
        records ??= NewRecords.None;
        var authors = records.Authors.ToDictionary(a => a.Key, a => new Author(a.LastName, a.FirstName, a.Pseudonym));
        var genres = records.Genres.ToDictionary(g => g.Key, g => new Genre(g.Label));
        var universes = new Dictionary<Guid, Universe>();
        if (records.Universes.Any(u => u.ParentId is not null))
            await UniverseHierarchy.LockAsync(context, cancellationToken);
        foreach (var added in records.Universes)
        {
            var universe = new Universe(added.Name);
            if (added.ParentId is { } parentId)
                universe.SetParent(await UniverseEndpoints.LoadWithAncestorsAsync(context, parentId, cancellationToken));
            universes[added.Key] = universe;
        }

        context.Authors.AddRange(authors.Values);
        context.Genres.AddRange(genres.Values);
        context.Universes.AddRange(universes.Values);
        return new AddedRecords(authors, genres, universes);
    }

    /// <summary>The contributions as a form lists them, in a stable order.</summary>
    public static List<ContributionContent> ToContents(IEnumerable<Contribution> contributions) =>
        contributions
            .OrderBy(c => c.Role).ThenBy(c => c.AuthorId)
            .Select(c => new ContributionContent(c.AuthorId, EnumMapping.Map<ContractEnums.ContributionRole>(c.Role)!.Value))
            .ToList();
}

/// <summary>The records a form added on the spot, created with the record it saves, by the key that stands for their identifier.</summary>
internal sealed record AddedRecords(
    IReadOnlyDictionary<Guid, Author> Authors, IReadOnlyDictionary<Guid, Genre> Genres, IReadOnlyDictionary<Guid, Universe> Universes);
