using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Serializes the changes to the hierarchy of the universes (choix-implementation.md § Cohérence
/// entre tables).
/// </summary>
/// <remarks>
/// Acyclicity spans the whole ancestor chain, which no row lock covers: two re-parentings of
/// different universes, each checked against a hierarchy that does not show the other yet, could
/// together persist a cycle (e.g. A under C while C goes under B, B being under A). Every change of
/// parent therefore takes this single lock before reading the hierarchy it checks, so that it sees
/// every change committed before it.
/// </remarks>
public static class UniverseHierarchy
{
    /// <summary>Key of the PostgreSQL advisory lock reserved to the universe hierarchy.</summary>
    public const long LockKey = 0x556E_6976_6572_7365; // "Universe"

    /// <summary>
    /// Waits for, then holds until the end of the current transaction, the lock of the hierarchy.
    /// Taken before reading the hierarchy to check a change of parent.
    /// </summary>
    public static async Task LockAsync(BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // Outside a transaction, the lock would be released as soon as the statement returns.
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("The universe hierarchy can only be locked within a transaction.");

        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({LockKey})", cancellationToken);
    }
}
