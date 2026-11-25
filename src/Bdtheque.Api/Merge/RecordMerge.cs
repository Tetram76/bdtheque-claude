using Bdtheque.Api.Endpoints;
using Bdtheque.Api.Search;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Merge;

/// <summary>
/// Common merge mechanism of the API (fonctionnel/regles-metier/fusion-des-fiches.md,
/// choix-implementation/ecritures/fusion-des-fiches-mise-en-oeuvre.md): two records of the same type become one, which keeps,
/// data by data, the value of the one or the other, and which every record designating either of them designates.
/// </summary>
internal static class RecordMerge
{
    /// <summary>
    /// The records a type of linked records counts for each of the two merged records, and for the record they become, in the
    /// order of the entity types the screen presents.
    /// </summary>
    /// <param name="links">For each type, the records linked to a record, by its identifier.</param>
    public static async Task<IReadOnlyList<MergeLinkCount>> CountLinksAsync(
        BdthequeDbContext context, Guid first, Guid second,
        IReadOnlyList<(EntityKind Kind, Func<BdthequeDbContext, Guid, IQueryable<Guid>> Records)> links, CancellationToken cancellationToken)
    {
        var counts = new List<MergeLinkCount>();
        foreach (var (kind, records) in links.OrderBy(l => EntityKindOrder.Of(l.Kind)))
        {
            var ofFirst = await records(context, first).Distinct().ToListAsync(cancellationToken);
            var ofSecond = await records(context, second).Distinct().ToListAsync(cancellationToken);
            var common = ofFirst.Intersect(ofSecond).Count();
            counts.Add(new MergeLinkCount(kind, ofFirst.Count + ofSecond.Count - common, ofFirst.Count, ofSecond.Count, common));
        }

        return counts;
    }

    /// <summary>
    /// The record that remains: the one whose data the merge keeps the most of, the first one when as many are kept of both
    /// (fonctionnel/regles-metier/fusion-des-fiches.md, no record favoured).
    /// </summary>
    public static bool KeepsFirst(IEnumerable<MergeSide> choices) =>
        choices.Count(c => c == MergeSide.First) >= choices.Count(c => c == MergeSide.Second);

    /// <summary>
    /// Merges two records: both are locked and checked at the versions their data were read, what designated
    /// the other one is transferred to the record that remains, the saved searches designating the other one included, then the
    /// record that remains takes its data and the other one is deleted, all in one transaction: the caller's when it opened one,
    /// which the caller then commits.
    /// </summary>
    /// <param name="keepFirst">Whether the first record remains (<see cref="KeepsFirst"/>).</param>
    /// <param name="assign">
    /// Gives the record that remains its data, of the one or the other, both read and locked, the elements of the other one it
    /// retains included (e.g. a contribution of a series, which passes to the series that remains with its identity).
    /// </param>
    /// <param name="transfer">
    /// Transfers to the record that remains what designated the other one, and tells the records the merge removes as duplicates
    /// (e.g. a contribution both authors held), each with the one that replaces it.
    /// </param>
    /// <returns>The record that remains.</returns>
    /// <exception cref="ArgumentException">The same record twice.</exception>
    /// <exception cref="EntityNotFoundException">One of the records no longer exists.</exception>
    /// <exception cref="DbUpdateConcurrencyException">One of the records was modified since its data were read.</exception>
    public static async Task<MergeResult> MergeAsync<TRoot>(
        BdthequeDbContext context, MergedRecord first, MergedRecord second, bool keepFirst, Action<MergePair<TRoot>> assign, Func<MergePair<TRoot>, Task<IReadOnlyList<MergeReplacement>>> transfer,
        CancellationToken cancellationToken)
        where TRoot : EntityBase, IAggregateRoot
    {
        // The transaction of the caller, when it opened one to take a lock before any row lock (the hierarchy of the universes,
        // choix-implementation/persistance/coherence-entre-tables.md), which the caller commits; otherwise one of its own.
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var (firstRoot, secondRoot) = await context.LoadAggregatesForWriteAsync<TRoot>(
            (first.Id, first.Version), (second.Id, second.Version), cancellationToken: cancellationToken);
        var pair = new MergePair<TRoot>(firstRoot, secondRoot, keepFirst);

        // Two saves, one transaction. What designated the merged record is transferred first: what the transfer takes from
        // its owners (e.g. the contributions of the merged author on an album) is only deleted by the save, before which it
        // still designates the merged record, whose deletion would refuse it. The kept record then takes its data, and the
        // merged record is deleted, in the same save: the elements of the merged record the kept one retains leave it before its
        // deletion takes them along, and the change log records the kept record once. A unique value the kept record takes
        // from the other one (e.g. the display name of an author) is free, EF Core ordering the deletion that releases a value
        // of a unique index before the update that takes it (source of EF Core, CommandBatchPreparer.AddUniqueValueEdges),
        // as a sub-universe moved to the kept universe before the deletion of its former parent.
        var replaced = (await transfer(pair)).ToList();
        var kind = AdvancedSearchSchema.KindOf(typeof(TRoot));
        if (kind is not null)
            replaced.Add(new MergeReplacement(kind.Value, pair.Merged.Id, pair.Kept.Id));
        await TransferSavedSearchesAsync(context, replaced, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        assign(pair);
        // The merged record, and every other record of its type the transfer replaced by another one (the homonymous
        // sub-universes merged with two universes), loaded by the transfer.
        var removed = replaced.Where(r => r.Kind == kind).Select(r => r.Merged).Append(pair.Merged.Id).ToHashSet();
        foreach (var record in context.Set<TRoot>().Local.Where(r => removed.Contains(r.Id)).ToList())
            context.Remove(record);
        await context.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return new MergeResult(pair.Kept.Id);
    }

    /// <summary>
    /// Locks and loads the aggregates <paramref name="records"/> designates, in the order of their identifiers, each marked
    /// modified (choix-implementation/persistance/concurrence-d-acces.md), for the merge to transfer to the kept record what
    /// designated the other one.
    /// </summary>
    public static async Task<IReadOnlyList<TLinked>> LoadLinkedForWriteAsync<TLinked>(
        BdthequeDbContext context, IQueryable<Guid> records, CancellationToken cancellationToken)
        where TLinked : EntityBase, IAggregateRoot
    {
        var ids = await records.Distinct().OrderBy(id => id).ToListAsync(cancellationToken);
        var linked = new List<TLinked>(ids.Count);
        foreach (var id in ids)
            linked.Add(await context.LoadOtherAggregateForWriteAsync<TLinked>(id, cancellationToken));
        return linked;
    }

    /// <summary>
    /// The saved searches designating a record the merge removes designate the one that replaces it instead, at the same places
    /// of their criteria: their variables keep their constants, and their designations follow.
    /// </summary>
    private static async Task TransferSavedSearchesAsync(
        BdthequeDbContext context, IReadOnlyList<MergeReplacement> replaced, CancellationToken cancellationToken)
    {
        var removed = replaced.Select(r => r.Merged).ToList();
        var searches = await LoadLinkedForWriteAsync<SavedSearch>(
            context,
            context.SavedSearches.IgnoreAutoIncludes().Where(s => s.Designations.Any(d => removed.Contains(d.RecordId))).Select(s => s.Id),
            cancellationToken);
        foreach (var search in searches)
        {
            var entity = AdvancedSearchSchema.Of(EnumMapping.Map<EntityKind>(search.Entity)!.Value);
            var criteria = replaced.Aggregate(
                SearchCriteriaJson.Deserialize(search.Criteria), (current, r) => SearchDesignations.Replace(entity, current, r.Kind, r.Merged, r.Kept));
            search.SetSearch(
                search.Entity, SearchCriteriaJson.Serialize(criteria), search.Variables,
                SearchDesignations.Of(entity, criteria).Select(r => new DesignatedRecord(EnumMapping.Map<DomainEnums.EntityKind>(r.Kind)!.Value, r.Id)));
        }
    }
}

/// <summary>A record a merge removes, of <paramref name="Kind"/>, and the one that replaces it, which every designation follows.</summary>
internal sealed record MergeReplacement(EntityKind Kind, Guid Merged, Guid Kept);
/// <summary>The two records of a merge, read and locked: the one that remains, and the one the merge deletes.</summary>
internal sealed class MergePair<TRoot>(TRoot first, TRoot second, bool keepFirst)
{
    public TRoot First { get; } = first;
    public TRoot Second { get; } = second;
    public TRoot Kept => keepFirst ? First : Second;
    public TRoot Merged => keepFirst ? Second : First;

    /// <summary>The record a data is kept from.</summary>
    public TRoot Of(MergeSide side) => side == MergeSide.First ? First : Second;
}
