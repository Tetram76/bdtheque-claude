using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bdtheque.Domain.Common;

/// <summary>
/// How much two records look alike, for the records the choice of the other record of a merge proposes
/// (fonctionnel/regles-metier/fusion-des-fiches.md, fiches pressenties): their texts brought to a common form, then identical,
/// a few characters apart, or, where inclusion applies, one within the other in whole words. A resemblance is a score, the
/// lower the closer: <see cref="Identical"/>, then the distance between the texts, then <see cref="Included"/>.
/// </summary>
public static partial class Resemblance
{
    public const int Identical = 0;

    /// <summary>One text within the other, after any distance the texts can be apart.</summary>
    public const int Included = 4;

    // The articles of the sort key of a title, the elided « L' » being the word « l » once the apostrophe separates it.
    private static readonly string[] Articles = [.. TitleSortKeyCalculator.SpaceSeparatedArticles.Select(a => a.ToLowerInvariant()), "l"];

    private static readonly Dictionary<string, int> NumberWords = new(StringComparer.Ordinal)
    {
        ["zero"] = 0, ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4, ["cinq"] = 5, ["six"] = 6,
        ["sept"] = 7, ["huit"] = 8, ["neuf"] = 9, ["dix"] = 10, ["onze"] = 11, ["douze"] = 12, ["treize"] = 13,
        ["quatorze"] = 14, ["quinze"] = 15, ["seize"] = 16, ["vingt"] = 20, ["vingts"] = 20, ["trente"] = 30,
        ["quarante"] = 40, ["cinquante"] = 50, ["soixante"] = 60, ["cent"] = 100, ["cents"] = 100, ["mille"] = 1000,
    };

    /// <summary>
    /// The closest resemblance between one of the <paramref name="first"/> texts and one of the <paramref name="second"/>,
    /// or <c>null</c> when none resembles another.
    /// </summary>
    /// <param name="inclusion">Whether a text within the other in whole words resembles it, as for the types where a double
    /// entry often adds words to a same identifier.</param>
    public static int? Compare(IEnumerable<string?> first, IEnumerable<string?> second, bool inclusion)
    {
        var others = second.Select(Words).Where(words => words.Length > 0).ToList();
        int? closest = null;
        foreach (var words in first.Select(Words).Where(words => words.Length > 0))
        {
            foreach (var other in others)
            {
                if (Compare(words, other, inclusion) is { } score && (closest is null || score < closest))
                    closest = score;
            }
        }

        return closest;
    }

    /// <summary>
    /// Whether two ISBN are those of a same book, its ten-digit and thirteen-digit forms being the same: separators and check
    /// characters ignored, an ISBN-10 is the ISBN-13 of prefix 978.
    /// </summary>
    public static bool SameIsbn(string? first, string? second) =>
        IsbnKey(first) is { } key && key == IsbnKey(second);

    private static int? Compare(string[] first, string[] second, bool inclusion)
    {
        var firstText = string.Join(' ', first);
        var secondText = string.Join(' ', second);
        if (firstText == secondText)
            return Identical;

        // A distance as long as the shorter text leaves nothing in common: no resemblance, however short the texts.
        var shorter = Math.Min(firstText.Length, secondText.Length);
        var allowed = Math.Min(MaxDistance(shorter), shorter - 1);
        if (allowed > 0 && Distance(firstText, secondText, allowed) is { } distance)
            return distance;

        return inclusion && (Contains(first, second) || Contains(second, first)) ? Included : null;
    }

    // fonctionnel/regles-metier/fusion-des-fiches.md: at most 1 character up to 6, 2 up to 12, 3 beyond.
    private static int MaxDistance(int length) => length <= 6 ? 1 : length <= 12 ? 2 : 3;

    /// <summary>The Levenshtein distance between the two texts, or <c>null</c> beyond <paramref name="max"/>.</summary>
    private static int? Distance(string first, string second, int max)
    {
        if (Math.Abs(first.Length - second.Length) > max)
            return null;

        var previous = new int[second.Length + 1];
        var current = new int[second.Length + 1];
        for (var j = 0; j <= second.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= first.Length; i++)
        {
            current[0] = i;
            var rowMin = current[0];
            for (var j = 1; j <= second.Length; j++)
            {
                var substitution = previous[j - 1] + (first[i - 1] == second[j - 1] ? 0 : 1);
                current[j] = Math.Min(substitution, Math.Min(previous[j], current[j - 1]) + 1);
                rowMin = Math.Min(rowMin, current[j]);
            }

            // Every later distance is at least the smallest of this row: the texts are already too far apart.
            if (rowMin > max)
                return null;
            (previous, current) = (current, previous);
        }

        return previous[second.Length] <= max ? previous[second.Length] : null;
    }

    // Whether the words of the shorter text follow each other within the longer one.
    private static bool Contains(string[] longer, string[] shorter)
    {
        if (shorter.Length >= longer.Length)
            return false;
        for (var start = 0; start + shorter.Length <= longer.Length; start++)
        {
            if (longer.AsSpan(start, shorter.Length).SequenceEqual(shorter))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The words of a text in their common form: without capitals nor accents, punctuation, typographic variants of a
    /// character and extra spaces being mere separators; without the article that starts it, or that the sort key puts at its
    /// end; each number in figures, whether written in Arabic figures, in Roman numerals or in words.
    /// </summary>
    private static string[] Words(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var words = WordPattern().Matches(Folded(text)).Select(match => match.Value).ToList();
        if (words.Count > 1 && Articles.Contains(words[0]))
            words.RemoveAt(0);
        else if (words.Count > 1 && Articles.Contains(words[^1]) && text.TrimEnd() is [.., ')' or ']'])
            words.RemoveAt(words.Count - 1);

        return [.. Numbers(words)];
    }

    // Lower case without diacritics, the ligatures written in their letters: « Mœbius » is « moebius ».
    private static string Folded(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var folded = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            switch (character)
            {
                case 'œ': folded.Append("oe"); break;
                case 'æ': folded.Append("ae"); break;
                case 'ß': folded.Append("ss"); break;
                default:
                    if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                        folded.Append(character);
                    break;
            }
        }

        return folded.ToString();
    }

    private static IEnumerable<string> Numbers(List<string> words)
    {
        for (var index = 0; index < words.Count; index++)
        {
            var word = words[index];
            if (NumberWords.ContainsKey(word))
            {
                // A run of number words, « et » joining two of them (« vingt et un », « mille et une »).
                var end = index;
                while (end + 1 < words.Count && (NumberWords.ContainsKey(words[end + 1])
                       || (words[end + 1] == "et" && end + 2 < words.Count && NumberWords.ContainsKey(words[end + 2]))))
                    end++;
                yield return FromWords(words.Skip(index).Take(end - index + 1).Where(w => w != "et")).ToString(CultureInfo.InvariantCulture);
                index = end;
            }
            else if (word.All(char.IsAsciiDigit))
                yield return word.TrimStart('0') is { Length: > 0 } figures ? figures : "0";
            else if (FromRoman(word) is { } roman)
                yield return roman.ToString(CultureInfo.InvariantCulture);
            else
                yield return word;
        }
    }

    private static int FromWords(IEnumerable<string> words)
    {
        int total = 0, current = 0;
        foreach (var word in words)
        {
            var value = NumberWords[word];
            if (value == 1000)
            {
                total += (current == 0 ? 1 : current) * 1000;
                current = 0;
            }
            else if (value == 100)
                current = (current == 0 ? 1 : current) * 100;
            else if (value == 20 && current % 100 == 4)
                current += 80 - 4; // « quatre-vingts »
            else
                current += value;
        }

        return total + current;
    }

    // A single letter is a Roman numeral only as I, V or X: « l », « d », « c » or « m » alone are elisions (« l'ombre »).
    private static int? FromRoman(string word)
    {
        if (word.Length == 1 && word is not ("i" or "v" or "x") || !RomanPattern().IsMatch(word))
            return null;

        var total = 0;
        for (var index = 0; index < word.Length; index++)
        {
            var value = RomanValue(word[index]);
            total += index + 1 < word.Length && RomanValue(word[index + 1]) > value ? -value : value;
        }

        return total;
    }

    private static int RomanValue(char letter) => letter switch
    {
        'i' => 1, 'v' => 5, 'x' => 10, 'l' => 50, 'c' => 100, 'd' => 500, _ => 1000,
    };

    private static string? IsbnKey(string? isbn)
    {
        if (isbn is null)
            return null;
        var characters = isbn.Where(c => char.IsAsciiDigit(c) || c is 'x' or 'X').ToArray();
        return characters.Length switch
        {
            10 when characters[..9].All(char.IsAsciiDigit) => "978" + new string(characters[..9]),
            13 when characters.All(char.IsAsciiDigit) => new string(characters[..12]),
            _ => null,
        };
    }

    [GeneratedRegex(@"[\p{L}\p{Nd}]+")]
    private static partial Regex WordPattern();

    [GeneratedRegex("^m{0,3}(cm|cd|d?c{0,3})(xc|xl|l?x{0,3})(ix|iv|v?i{0,3})$")]
    private static partial Regex RomanPattern();
}
