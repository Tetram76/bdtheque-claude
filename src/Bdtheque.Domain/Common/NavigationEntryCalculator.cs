using System.Globalization;
using System.Text;

namespace Bdtheque.Domain.Common;

/// <summary>
/// Computes the initial-based navigation entry (<c>A</c>–<c>Z</c>, <c>#</c> or <c>@</c>) of a sort
/// key, per fonctionnel.md § Entrées de la navigation par initiale. Shared by
/// <see cref="Entities.Series"/>, <see cref="Entities.Album"/> and <see cref="Entities.Author"/>.
/// </summary>
/// <remarks>
/// The base letter of an initial is the one the French alphabetical order itself assigns: the
/// last letter <c>A</c>–<c>Z</c> that precedes or equals it under the French ICU collation,
/// ignoring case and diacritics — the same collation family as the text columns. A letter is
/// thus filed exactly where the sorted list places it, including those that Unicode
/// decomposition leaves untouched (<c>Œ</c>, <c>Ø</c>, <c>Ł</c>…), without a hand-written
/// transliteration table that could contradict that order (choix-implementation.md
/// § Navigation par initiale : mise en œuvre).
/// </remarks>
public static class NavigationEntryCalculator
{
    public const string DigitEntry = "#";
    public const string OtherEntry = "@";

    /// <summary>Every entry <see cref="Compute"/> can return.</summary>
    public static IReadOnlyList<string> Entries { get; } =
        [.. Enumerable.Range('A', 26).Select(letter => ((char)letter).ToString()), DigitEntry, OtherEntry];

    private static readonly CompareInfo FrenchOrder = CultureInfo.GetCultureInfo("fr-FR").CompareInfo;
    private const CompareOptions BaseLetterOnly = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public static string Compute(string sortKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        // A text element, not a code unit: a decomposed "É" (E + combining accent) or a letter
        // outside the Basic Multilingual Plane is a single initial.
        var initial = StringInfo.GetNextTextElement(sortKey);

        // Classified on its first code point, never on a UTF-16 code unit, which would only be
        // the high surrogate of a character outside the Basic Multilingual Plane (e.g. "𝟙").
        var codePoint = Rune.GetRuneAt(initial, 0);
        if (Rune.IsDigit(codePoint))
            return DigitEntry;
        if (!Rune.IsLetter(codePoint))
            return OtherEntry;

        for (var letter = 'Z'; letter >= 'A'; letter--)
        {
            var baseLetter = letter.ToString();

            // An initial starting with the letter itself (accented form, or an expansion such as
            // "Æ" = "ae" or "ß" = "ss") belongs to it.
            if (FrenchOrder.IsPrefix(initial, baseLetter, BaseLetterOnly))
                return baseLetter;

            // A distinct letter sorting after this one belongs to it, except after "Z": the
            // order then places it beyond every word starting with "Z" (other alphabets, Latin
            // letters with no base letter such as "Þ").
            if (FrenchOrder.Compare(initial, baseLetter, BaseLetterOnly) > 0)
                return letter == 'Z' ? OtherEntry : baseLetter;
        }

        return OtherEntry;
    }
}
