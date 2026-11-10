namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of a list of a referential of the administration, the genres or the universes (visuel/listes/), which its
/// address carries (<see cref="InListEntryQuery"/>): its search, without filters nor navigation by initial, and the entry of a
/// record opened over the list.
/// </summary>
public abstract record ReferentialListQuery(
    string? Text,
    int Page,
    Guid? Delete,
    Guid? Merge,
    string? MergeText,
    Guid? Edit,
    bool Adding) : InListEntryQuery(Text, Entry: null, Page, Delete, Merge, MergeText, Edit, Adding)
{
    protected override IEnumerable<(string Name, object? Value)> Filters => [];

    /// <summary>What a referential reads of its address, beyond the criteria of every list.</summary>
    protected static (string? Text, int Page, Guid? Delete, Guid? Merge, string? MergeText, Guid? Edit, bool Adding) Read(string query)
    {
        var address = new Address(query);
        var (edit, adding) = ReadEntry(address);
        return (address.Search, address.Page, address.Delete, address.Merge, address.MergeText, edit, adding);
    }
}
