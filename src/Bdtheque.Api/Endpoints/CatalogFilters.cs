using System.Linq.Expressions;
using Bdtheque.Domain.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Search, navigation by initial and cross filters of the lists of the consultation
/// (choix-implementation.md § Recherche).
/// </summary>
internal static class CatalogFilters
{
    private const string LikeEscapeCharacter = "\\";

    /// <summary>
    /// Keeps the items one of whose <paramref name="texts"/> contains <paramref name="text"/>, ignoring
    /// case and accents; all of them if <paramref name="text"/> is blank.
    /// </summary>
    public static IQueryable<T> WhereContains<T>(this IQueryable<T> source, string? text, params Expression<Func<T, string?>>[] texts)
    {
        if (string.IsNullOrWhiteSpace(text))
            return source;

        // The wildcards entered are looked for as they are. Captured, the pattern is sent as a parameter:
        // a constant would be written into the SQL, and compile a new query for each search.
        var pattern = $"%{EscapeLike(text.Trim())}%";
        Expression<Func<string?, bool>> contains = value => EF.Functions.Like(
            EF.Functions.Collate(value, BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation), pattern, LikeEscapeCharacter);

        var item = Expression.Parameter(typeof(T), "item");
        var condition = texts
            .Select(t => ReplacingExpressionVisitor.Replace(
                contains.Parameters[0], ReplacingExpressionVisitor.Replace(t.Parameters[0], item, t.Body), contains.Body))
            .Aggregate(Expression.OrElse);
        return source.Where(Expression.Lambda<Func<T, bool>>(condition, item));
    }

    private static string EscapeLike(string text) =>
        text.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);

    /// <exception cref="ArgumentOutOfRangeException">
    /// Not one of the entries of the navigation: a technical error, the frontend building the navigation,
    /// never the user.
    /// </exception>
    public static void EnsureNavigationEntry(string? entry)
    {
        if (entry is not null && !NavigationEntryCalculator.Entries.Contains(entry))
            throw new ArgumentOutOfRangeException(nameof(entry), entry, "Not an entry of the navigation by initial.");
    }

    /// <summary>
    /// The universe and its descendants at every level, to whose records it is attached too
    /// (fonctionnel.md § Hiérarchie des univers). Computed from the whole hierarchy, a small reference
    /// table, rather than by a recursive query.
    /// </summary>
    public static async Task<IReadOnlyCollection<Guid>> UniverseAndDescendantsAsync(
        BdthequeDbContext context, Guid universeId, CancellationToken cancellationToken)
    {
        var children = (await context.Universes.AsNoTracking()
                .Where(u => u.ParentId != null)
                .Select(u => new { u.Id, ParentId = u.ParentId!.Value })
                .ToListAsync(cancellationToken))
            .ToLookup(u => u.ParentId, u => u.Id);

        var scope = new HashSet<Guid> { universeId };
        var pending = new Queue<Guid>(scope);
        while (pending.TryDequeue(out var parentId))
        {
            foreach (var childId in children[parentId])
            {
                if (scope.Add(childId))
                    pending.Enqueue(childId);
            }
        }
        return scope;
    }
}
