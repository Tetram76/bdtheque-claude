using Bdtheque.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Entry point of every write on an existing aggregate (choix-implementation.md § Concurrence
/// d'accès).
/// </summary>
public static class AggregateWriteExtensions
{
    /// <summary>
    /// Locks the root row of an aggregate, checks that the aggregate is still at the version the
    /// client read, then loads the root — shaped by <paramref name="shape"/>, e.g. to include the
    /// children about to be written — and marks it modified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The root is locked (<c>SELECT … FOR UPDATE</c>) before anything else: every write on an
    /// aggregate, deletion included, takes its locks in the same root → child order, so two
    /// transactions on the same aggregate can never wait for each other.
    /// </para>
    /// <para>
    /// The lock opens the unit of work: the context must track nothing yet. A tracking query never
    /// refreshes an instance already tracked, so an aggregate read before its lock could carry
    /// values older than the locked row, which marking the root modified would then write back —
    /// silently overwriting a concurrent modification. Anything read beforehand (e.g. the album of
    /// an edition) is read without tracking.
    /// </para>
    /// <para>
    /// Marking the root modified makes the next <c>SaveChanges</c> rewrite its row, hence change its
    /// <c>xmin</c>, even when only a child row or an association is written (e.g. adding a genre to
    /// an album): otherwise a concurrent modification of the aggregate's children would go
    /// undetected.
    /// </para>
    /// </remarks>
    /// <exception cref="EntityNotFoundException">No aggregate has this identifier.</exception>
    /// <exception cref="DbUpdateConcurrencyException">The aggregate was modified since <paramref name="version"/>.</exception>
    public static async Task<TRoot> LoadAggregateForWriteAsync<TRoot>(
        this BdthequeDbContext context,
        Guid id,
        uint version,
        Func<IQueryable<TRoot>, IQueryable<TRoot>>? shape = null,
        CancellationToken cancellationToken = default)
        where TRoot : EntityBase, IAggregateRoot
    {
        // Outside a transaction, the lock would be released as soon as the SELECT returns.
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("An aggregate can only be loaded for write within a transaction.");
        if (context.ChangeTracker.Entries().Any())
            throw new InvalidOperationException(
                "An aggregate must be locked before anything is tracked: entities read earlier would not be refreshed.");

        var entityType = context.Model.FindEntityType(typeof(TRoot))
                         ?? throw new InvalidOperationException($"{typeof(TRoot).Name} is not mapped.");
        // EF Core has no locking clause: the statement is written by hand, with the identifiers taken
        // from the model and quoted by the provider, the id being passed as a parameter.
        var sql = context.GetService<ISqlGenerationHelper>();
        var table = sql.DelimitIdentifier(entityType.GetTableName()!, entityType.GetSchema());
        var idColumn = sql.DelimitIdentifier(entityType.FindProperty(nameof(EntityBase.Id))!.GetColumnName());
        var versionColumn = sql.DelimitIdentifier(entityType.FindProperty(BdthequeDbContext.VersionProperty)!.GetColumnName());
        // A raw query reads a uint as bigint, which Npgsql cannot read from an xid: hence the cast
        // (xid has no direct cast to bigint, only through its text form).
        var lockStatement =
            "SELECT " + versionColumn + "::text::bigint AS \"Value\" FROM " + table + " WHERE " + idColumn + " = {0} FOR UPDATE";

        // The version is read from the locked row itself, the only one the lock guarantees current.
        var lockedVersions = await context.Database
            .SqlQueryRaw<long>(lockStatement, id)
            .ToListAsync(cancellationToken);
        if (lockedVersions.Count == 0)
            throw new EntityNotFoundException(typeof(TRoot), id);

        // Checked now rather than left to SaveChanges: a stale form must be reported as such, not
        // as whatever business rule its outdated values happen to break first. The lock keeps the
        // version from changing until the transaction ends.
        if (lockedVersions[0] != version)
            throw new DbUpdateConcurrencyException(
                $"{typeof(TRoot).Name} {id} was modified since version {version} was read.");

        var query = context.Set<TRoot>().AsQueryable();
        var root = await (shape is null ? query : shape(query)).SingleAsync(r => r.Id == id, cancellationToken);

        context.Entry(root).State = EntityState.Modified;
        return root;
    }
}
