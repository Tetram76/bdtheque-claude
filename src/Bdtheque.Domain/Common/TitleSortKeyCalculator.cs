namespace Bdtheque.Domain.Common;

/// <summary>
/// Computes the sort key of a French title by stripping its leading article, per the
/// predefined list agreed for this project (see fonctionnel.md § Tri et navigation par
/// initiale). Shared between <see cref="Entities.Series"/> and the future <c>Album</c> entity.
/// </summary>
public static class TitleSortKeyCalculator
{
    // Elided forms (no space before the following word) must be checked before the
    // space-separated forms below, and are matched case-insensitively on the "L" + apostrophe
    // prefix only (the apostrophe itself is not part of a word, so no trailing-space check applies).
    private static readonly char[] Apostrophes = ['\'', '’'];

    // Space-separated forms: only stripped when followed by a word boundary (a space),
    // so a title that merely starts with the same letters (e.g. "Larousse") is untouched.
    private static readonly string[] SpaceSeparatedArticles = ["Le", "La", "Les", "Un", "Une", "Des"];

    public static string Compute(string title)
    {
        var trimmed = title.Trim();

        if (trimmed.Length > 1 && (trimmed[0] == 'L' || trimmed[0] == 'l') && Apostrophes.Contains(trimmed[1]))
            return trimmed[2..];

        foreach (var article in SpaceSeparatedArticles)
        {
            var prefixWithSpace = article + " ";
            if (trimmed.Length > prefixWithSpace.Length
                && trimmed.StartsWith(prefixWithSpace, StringComparison.OrdinalIgnoreCase))
                return trimmed[prefixWithSpace.Length..];
        }

        return trimmed;
    }
}
