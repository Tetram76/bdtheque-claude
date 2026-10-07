using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Keeps the indexes of the text columns consistent with the ICU collations they sort and compare
/// along (choix-implementation.md § Collation des colonnes texte).
/// </summary>
/// <remarks>
/// A collation's definition comes with the version of ICU the database image ships, which an update
/// of the image can change. PostgreSQL then only warns: an index built under the former definition may
/// be corrupt — wrong order, a unique index letting a duplicate through or refusing a legitimate value —
/// and must be rebuilt before the version recorded for the collation is refreshed (PostgreSQL
/// documentation, ALTER COLLATION).
/// </remarks>
public static class CollationVersions
{
    private static readonly string[] Collations =
        [BdthequeDbContext.FrenchCollation, BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation];

    /// <summary>
    /// Rebuilds the indexes of the database and records the current version of the collations whose
    /// recorded version no longer matches the one the system provides.
    /// </summary>
    /// <returns>The collations whose version changed; none in the usual case.</returns>
    /// <exception cref="Npgsql.PostgresException">
    /// An index cannot be rebuilt — a unique index whose values the new definition makes equal: the
    /// data must be corrected by hand before the application can use the database again.
    /// </exception>
    public static async Task<IReadOnlyList<string>> RefreshAsync(BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var stale = await context.Database
            .SqlQuery<StaleCollation>($"""
                SELECT c.collname AS "Name", quote_ident(n.nspname) || '.' || quote_ident(c.collname) AS "QualifiedName"
                FROM pg_collation c JOIN pg_namespace n ON n.oid = c.collnamespace
                WHERE c.collname = ANY({Collations}) AND c.collversion IS DISTINCT FROM pg_collation_actual_version(c.oid)
                ORDER BY c.collname
                """)
            .ToListAsync(cancellationToken);
        if (stale.Count == 0)
            return [];

        // Every index of the database, rather than those depending on the stale collations: every text
        // column uses one of them, and the database is small. Outside any transaction, which REINDEX
        // DATABASE refuses.
        await context.Database.ExecuteSqlRawAsync("REINDEX DATABASE", cancellationToken);
        // An identifier cannot be passed as a parameter: the name comes from the catalog, quoted by the
        // database itself (quote_ident).
        foreach (var collation in stale)
        {
            var refresh = "ALTER COLLATION " + collation.QualifiedName + " REFRESH VERSION";
            await context.Database.ExecuteSqlRawAsync(refresh, cancellationToken);
        }

        return stale.Select(c => c.Name).ToList();
    }

    private sealed record StaleCollation(string Name, string QualifiedName);
}
