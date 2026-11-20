namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The numbers of a row of a list of the administration, in a single column, each followed by what it counts
/// (visuel/listes/principes-communs.md, nombres): « 2 collections · 688 éditions · 3 intentions ».
/// </summary>
public static class ListCounts
{
    /// <summary>
    /// The numbers counted, in the order given, which is that of the entity types; a zero, given as null, says nothing and is
    /// left out beside the others (visuel/charte-graphique/convivialite-et-efficacite.md, nombre nul).
    /// </summary>
    /// <returns>The numbers joined, or null if every one is a zero: the column then takes the dash of a missing data.</returns>
    public static string? Join(params string?[] counted) =>
        counted.OfType<string>().ToList() is { Count: > 0 } shown ? string.Join(" · ", shown) : null;
}
