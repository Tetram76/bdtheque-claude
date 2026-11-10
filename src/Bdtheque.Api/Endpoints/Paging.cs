using System.Linq.Expressions;
using Bdtheque.Contracts.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>Offset pagination of the lists of the consultation (choix-implementation/api/organisation-de-l-api.md).</summary>
internal static class Paging
{
    public const int DefaultSize = 50;
    public const int MaxSize = 100;

    /// <summary>Reads one page of <paramref name="ordered"/>, with the number of items of the whole list.</summary>
    /// <param name="ordered">The list, in a total order: otherwise, an item could appear on two pages, or on none.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A page number below 1, or a size outside 1 to <see cref="MaxSize"/>: a technical error, the frontend
    /// building the paging, never the user.
    /// </exception>
    public static async Task<Page<TItem>> ToPageAsync<TSource, TItem>(
        this IOrderedQueryable<TSource> ordered, int number, int size, Expression<Func<TSource, TItem>> projection,
        CancellationToken cancellationToken)
    {
        EnsureBounds(number, size);
        var totalCount = await ordered.CountAsync(cancellationToken);
        var items = await ordered.Skip(checked((number - 1) * size)).Take(size).Select(projection).ToListAsync(cancellationToken);
        return new Page<TItem>(items, number, size, totalCount);
    }

    /// <summary>One page of a list ordered in memory, under the same bounds as <see cref="ToPageAsync"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">As <see cref="ToPageAsync"/>.</exception>
    public static IReadOnlyList<T> Slice<T>(IReadOnlyList<T> ordered, int number, int size)
    {
        EnsureBounds(number, size);
        return [.. ordered.Skip(checked((number - 1) * size)).Take(size)];
    }

    private static void EnsureBounds(int number, int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, MaxSize);
    }
}
