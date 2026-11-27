namespace Bdtheque.Frontend.Entries;

/// <summary>What a text field of an entry form holds.</summary>
public static class EntryText
{
    /// <summary>
    /// A field emptied is no value, as one never typed in: the same datum for its mark (EntryTracker), the same state for the
    /// save bar, and the same content sent.
    /// </summary>
    public static string? Typed(string? value) => string.IsNullOrEmpty(value) ? null : value;
}