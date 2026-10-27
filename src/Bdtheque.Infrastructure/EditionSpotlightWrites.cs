using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bdtheque.Infrastructure;

/// <summary>
/// The only write of the history of the spotlights (choix-implementation/domaine.md § Édition « À la une » : mise en
/// œuvre).
/// </summary>
public static class EditionSpotlightWrites
{
    internal const string Table = "EditionSpotlights";

    /// <summary>
    /// Puts <paramref name="editionId"/> in the spotlight of <paramref name="day"/>, unless the day already has its
    /// edition: of two first displays at once, the key keeps one draw, which the other reads back — the same edition
    /// for both, without any lock. An edition deleted since it was drawn is not inserted either: the caller reads the
    /// day again, and draws again if it is still without edition.
    /// </summary>
    public static async Task InsertSpotlightAsync(
        this BdthequeDbContext context, DateOnly day, Guid editionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO "EditionSpotlights" ("Day", "EditionId") VALUES ({day}, {editionId})
                ON CONFLICT ("Day") DO NOTHING
                """,
                cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            // Not an error: the day is drawn again.
        }
    }
}
