namespace Bdtheque.Domain.Common;

/// <summary>
/// Computes the sort key of a French title by moving its leading article to a bracketed
/// suffix, per the predefined list agreed for this project (see fonctionnel.md § Tri et
/// navigation par initiale). Shared between <see cref="Entities.Series"/> and the future
/// <c>Album</c> entity.
/// </summary>
/// <remarks>
/// The article is relocated rather than dropped: dropping it would make two titles that only
/// differ by their leading article (e.g. "Le Lotus bleu" / "Un Lotus bleu") produce the same
/// sort key, leaving their relative order arbitrary. Keeping the article as a suffix preserves
/// a deterministic order while still grouping on the significant word for initial-based
/// navigation (the suffix never changes the sort key's first character).
/// </remarks>
public static class TitleSortKeyCalculator
{
    // Elided forms (no space before the following word) must be checked before the
    // space-separated forms below, and are matched case-insensitively on the "L" + apostrophe
    // prefix only (the apostrophe itself is not part of a word, so no trailing-space check applies).
    private static readonly char[] Apostrophes = ['\'', '’'];

    // Space-separated forms: only moved when followed by a word boundary (a space),
    // so a title that merely starts with the same letters (e.g. "Larousse") is untouched.
    private static readonly string[] SpaceSeparatedArticles = ["Le", "La", "Les", "Un", "Une", "Des"];

    public static string Compute(string title)
    {
        var trimmed = title.Trim();

        // > 2 (not > 1): at least one character must remain after the 2-character "L'" prefix,
        // mirroring the same requirement enforced below for space-separated articles.
        if (trimmed.Length > 2 && (trimmed[0] == 'L' || trimmed[0] == 'l') && Apostrophes.Contains(trimmed[1]))
            return BuildSortKey(trimmed, articleLength: 2);

        foreach (var article in SpaceSeparatedArticles)
        {
            if (trimmed.Length > article.Length
                && trimmed.StartsWith(article, StringComparison.OrdinalIgnoreCase)
                && char.IsWhiteSpace(trimmed[article.Length]))
                return BuildSortKey(trimmed, article.Length);
        }

        return trimmed;
    }

    // Separator whitespace after the article is entirely discarded (not just its first
    // character): user-entered or migrated data can carry stray extra spaces, and a leftover
    // one would make the stored key start with whitespace instead of the significant word.
    private static string BuildSortKey(string trimmed, int articleLength) =>
        $"{trimmed[articleLength..].TrimStart()} [{trimmed[..articleLength]}]";
}
