namespace Bdtheque.Api.Merge;

/// <summary>
/// The records the choice of the other record of a merge proposes from its opening (fonctionnel/regles-metier/fusion-des-fiches.md,
/// fiches pressenties), read by the list of their type of the administration, restricted to them and in their order: the dialog
/// presents them as it presents the records its search finds.
/// </summary>
/// <remarks>
/// The resemblance is computed in memory, by the domain, on every record of the type: a few thousand short texts, compared in
/// a few milliseconds, where SQL knows neither the common form of a text nor the distance between two.
/// </remarks>
internal static class MergeSuggestions
{
    /// <summary>
    /// The identifiers of at most <paramref name="count"/> records that resemble the <paramref name="subject"/>, the closest
    /// first, records alike in the order given; none for a subject that is not among them, as a list filtered on a record
    /// that does not exist (choix-implementation/api/recherche.md). The subject itself is never proposed.
    /// </summary>
    /// <param name="resemblance">The resemblance of the second record to the first, or <c>null</c>: <see cref="Domain.Common.Resemblance"/>.</param>
    public static IReadOnlyList<Guid> Rank<T>(
        IReadOnlyList<T> records, Guid subject, Func<T, Guid> id, Func<T, T, int?> resemblance, int count)
    {
        var record = records.FirstOrDefault(r => id(r) == subject);
        if (record is null)
            return [];

        return
        [
            .. records
                .Where(r => id(r) != subject)
                .Select(r => (Id: id(r), Score: resemblance(record, r)))
                .Where(candidate => candidate.Score is not null)
                .OrderBy(candidate => candidate.Score) // stable: records alike keep the order given
                .Take(count)
                .Select(candidate => candidate.Id),
        ];
    }

    /// <summary>The rows of a page of the list in the order of <paramref name="ranked"/>, or as they are without it.</summary>
    public static List<TItem> InRankOrder<TItem>(IEnumerable<TItem> items, IReadOnlyList<Guid>? ranked, Func<TItem, Guid> id) =>
        ranked is null ? [.. items] : [.. items.OrderBy(item => IndexOf(ranked, id(item)))];

    private static int IndexOf(IReadOnlyList<Guid> ranked, Guid id)
    {
        for (var index = 0; index < ranked.Count; index++)
        {
            if (ranked[index] == id)
                return index;
        }

        return ranked.Count;
    }
}
