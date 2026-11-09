namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// An ISBN as it is presented, formatted with its hyphens by the last ranges of the International ISBN Agency the
/// application obtained (<see cref="IsbnRangesRefresher"/>); as entered without them, or when no range places it. Only its
/// presentation: the ISBN entered is kept, and found by the search with or without its hyphens.
/// </summary>
public sealed class IsbnFormatter
{
    private volatile IsbnRanges? _ranges;

    /// <summary>The last ranges obtained; <c>null</c> until the first download.</summary>
    public IsbnRanges? Ranges
    {
        get => _ranges;
        set => _ranges = value;
    }

    public string Format(string isbn) => _ranges?.Hyphenate(isbn) ?? isbn;
}
