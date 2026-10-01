using System.Security.Cryptography;
using System.Text;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bdtheque.Api.Deletion;

/// <summary>
/// Common deletion mechanism of the API (choix-implementation.md § Suppression des entités : mise en
/// œuvre): the impact of a deletion, computed from the links each resource declares, and the
/// deletion itself, which never does more than the impact the user confirmed.
/// </summary>
internal static class AggregateDeletion
{
    /// <summary>Impact of deleting an existing aggregate, for the confirmation message.</summary>
    /// <exception cref="EntityNotFoundException">No aggregate has this identifier.</exception>
    public static async Task<DeletionImpact> GetImpactAsync<TRoot>(
        BdthequeDbContext context, Guid id, IReadOnlyList<DeletionLink> links, CancellationToken cancellationToken)
        where TRoot : EntityBase, IAggregateRoot
    {
        if (!await context.Set<TRoot>().AnyAsync(r => r.Id == id, cancellationToken))
            throw new EntityNotFoundException(typeof(TRoot), id);

        return await ComputeImpactAsync(context, id, links, cancellationToken);
    }

    /// <summary>
    /// Impact of deleting a child of an existing aggregate (e.g. a collection of a publisher), the
    /// aggregate being identified by its root.
    /// </summary>
    /// <exception cref="EntityNotFoundException">The root does not exist, or has no such child.</exception>
    public static async Task<DeletionImpact> GetChildImpactAsync<TRoot, TChild>(
        BdthequeDbContext context, Guid rootId, Guid childId, Func<IQueryable<TRoot>, IQueryable<TRoot>> shape,
        Func<TRoot, IEnumerable<TChild>> children, IReadOnlyList<DeletionLink> links, CancellationToken cancellationToken)
        where TRoot : EntityBase, IAggregateRoot
        where TChild : EntityBase
    {
        var root = await shape(context.Set<TRoot>().AsNoTracking()).SingleOrDefaultAsync(r => r.Id == rootId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(TRoot), rootId);
        if (!children(root).Any(c => c.Id == childId))
            throw new EntityNotFoundException(typeof(TChild), childId);

        return await ComputeImpactAsync(context, childId, links, cancellationToken);
    }

    /// <summary>
    /// Counts the records each link selects, by nature of link and entity type, and fingerprints
    /// their identities: counts alone would stay equal if one association disappeared while another
    /// appeared.
    /// </summary>
    public static async Task<DeletionImpact> ComputeImpactAsync(
        BdthequeDbContext context, Guid id, IReadOnlyList<DeletionLink> links, CancellationToken cancellationToken)
    {
        // Sorted, so that both the counts and the fingerprint come out in a stable order.
        var records = new SortedDictionary<(LinkNature Nature, EntityKind Kind), SortedSet<Guid>>();
        foreach (var link in links)
        {
            var ids = await link.Records(context, id).ToListAsync(cancellationToken);
            if (ids.Count == 0)
                continue;

            if (!records.TryGetValue((link.Nature, link.Kind), out var linked))
                records[(link.Nature, link.Kind)] = linked = [];
            linked.UnionWith(ids);
        }

        return new DeletionImpact(
            CountsOf(LinkNature.Reference),
            CountsOf(LinkNature.Association),
            CountsOf(LinkNature.Composition),
            Fingerprint(records));

        List<ImpactCount> CountsOf(LinkNature nature) =>
            records.Where(r => r.Key.Nature == nature).Select(r => new ImpactCount(r.Key.Kind, r.Value.Count)).ToList();
    }

    /// <summary>
    /// Deletes an aggregate, provided it is still at <paramref name="version"/>, nothing references
    /// it, and its impact is still the one confirmed by the user (<paramref name="fingerprint"/>).
    /// </summary>
    /// <remarks>
    /// The impact is recomputed after the root is locked: from then on, no write can add a record to
    /// it — creating a link to the root must lock its row too (<c>FOR KEY SHARE</c>, to check the
    /// foreign key) and waits, then fails once the root is gone. The deletion can therefore not
    /// remove anything that was not confirmed.
    /// </remarks>
    /// <exception cref="EntityNotFoundException">No aggregate has this identifier.</exception>
    /// <exception cref="DbUpdateConcurrencyException">The aggregate was modified since <paramref name="version"/>.</exception>
    /// <exception cref="DeletionRefusedException">Records reference the aggregate.</exception>
    /// <exception cref="DeletionImpactChangedException">The impact is no longer the one confirmed.</exception>
    public static Task DeleteAsync<TRoot>(
        BdthequeDbContext context, Guid id, uint version, string fingerprint, IReadOnlyList<DeletionLink> links,
        CancellationToken cancellationToken)
        where TRoot : EntityBase, IAggregateRoot =>
        DeleteCoreAsync<TRoot, TRoot>(context, id, version, fingerprint, links, shape: null, root => root, cancellationToken);

    /// <summary>
    /// Deletes a child of an aggregate (e.g. a collection of a publisher) under the same guarantees
    /// as <see cref="DeleteAsync{TRoot}"/>: the root is locked first and its version checked — the
    /// version guarding every write on the aggregate — then the child is deleted, and the root
    /// marked modified.
    /// </summary>
    /// <exception cref="EntityNotFoundException">The root does not exist, or has no such child.</exception>
    public static Task DeleteChildAsync<TRoot, TChild>(
        BdthequeDbContext context, Guid rootId, Guid childId, uint version, string fingerprint,
        Func<IQueryable<TRoot>, IQueryable<TRoot>> shape, Func<TRoot, IEnumerable<TChild>> children,
        IReadOnlyList<DeletionLink> links, CancellationToken cancellationToken)
        where TRoot : EntityBase, IAggregateRoot
        where TChild : EntityBase =>
        DeleteCoreAsync<TRoot, TChild>(
            context, rootId, version, fingerprint, links, shape,
            root => children(root).SingleOrDefault(c => c.Id == childId) ?? throw new EntityNotFoundException(typeof(TChild), childId),
            cancellationToken);

    private static async Task DeleteCoreAsync<TRoot, TTarget>(
        BdthequeDbContext context, Guid rootId, uint version, string fingerprint, IReadOnlyList<DeletionLink> links,
        Func<IQueryable<TRoot>, IQueryable<TRoot>>? shape, Func<TRoot, TTarget> target, CancellationToken cancellationToken)
        where TRoot : EntityBase, IAggregateRoot
        where TTarget : EntityBase
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var root = await context.LoadAggregateForWriteAsync(rootId, version, shape, cancellationToken);
        var deleted = target(root);

        var impact = await ComputeImpactAsync(context, deleted.Id, links, cancellationToken);
        if (impact.BlockedBy.Count > 0)
            throw new DeletionRefusedException(impact);
        if (!string.Equals(impact.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new DeletionImpactChangedException(impact);

        context.Remove(deleted);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.RestrictViolation })
        {
            // The ON DELETE RESTRICT safety net caught a reference the impact missed: still a refusal
            // the user can act upon, reported with the impact as it now stands. The failed statement
            // aborted the transaction, hence the impact recomputed outside of it.
            await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            throw new DeletionRefusedException(await ComputeImpactAsync(context, deleted.Id, links, cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static string Fingerprint(SortedDictionary<(LinkNature Nature, EntityKind Kind), SortedSet<Guid>> records)
    {
        var identities = new StringBuilder();
        foreach (var ((nature, kind), ids) in records)
            identities.Append(nature).Append('/').Append(kind).Append(':').AppendJoin(',', ids).Append(';');

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identities.ToString())));
    }
}
