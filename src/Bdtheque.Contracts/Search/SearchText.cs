namespace Bdtheque.Contracts.Search;

/// <summary>
/// The typographic variants of a character that a search by text takes for that character, on both sides — the text searched
/// and the text entered (choix-implementation/api/recherche.md): a text pasted from an office application writes its
/// apostrophes, spaces and hyphens in variants that its reader takes for the same character. Shared by the API, which compares
/// in the database, and by its clients, which compare what they hold in memory, so that both find the same records.
/// </summary>
public static class SearchText
{
    /// <summary>
    /// The variants of a space: no-break space, the spaces from en quad to hair space, narrow no-break space, medium
    /// mathematical space, ideographic space.
    /// </summary>
    public const string Spaces = "\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000";

    /// <summary>
    /// The variants of a hyphen: hyphen, non-breaking hyphen, figure dash, en dash, em dash, horizontal bar, minus sign.
    /// </summary>
    public const string Dashes = "\u2010\u2011\u2012\u2013\u2014\u2015\u2212";

    // Left and right single quotation marks, single high-reversed-9 quotation mark, modifier letter apostrophe.
    private const string Apostrophes = "\u2018\u2019\u201B\u02BC";

    /// <summary>Every variant, each at the index of the character it stands for in <see cref="Characters"/>.</summary>
    public const string Variants = Apostrophes + Spaces + Dashes;

    /// <summary>The character each of <see cref="Variants"/> stands for, at its index.</summary>
    public static readonly string Characters =
        new string('\'', Apostrophes.Length) + new string(' ', Spaces.Length) + new string('-', Dashes.Length);

    /// <summary><paramref name="text"/>, each variant replaced by the character it stands for.</summary>
    public static string Normalize(string text) =>
        string.Create(text.Length, text, (normalized, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var variant = Variants.IndexOf(source[i], StringComparison.Ordinal);
                normalized[i] = variant < 0 ? source[i] : Characters[variant];
            }
        });
}
