using Bdtheque.Frontend.Audience;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of a list of a referential of the administration, the genres or the universes (visuel/listes/), which its
/// address carries (<see cref="AdminListQuery"/>): its search, without filters nor navigation by initial, and the entry of a
/// record opened over the list, its records being entered there rather than on a page of their own
/// (visuel/listes/saisie-selon-l-entite.md).
/// </summary>
/// <param name="Edit">The record being changed.</param>
/// <param name="Adding">Whether a new record is being entered.</param>
public abstract record ReferentialListQuery(
    string? Text,
    int Page,
    Guid? Delete,
    Guid? Merge,
    string? MergeText,
    Guid? Edit,
    bool Adding) : AdminListQuery(Text, Entry: null, Page, FiltersOpen: false, Delete, Merge, MergeText)
{
    internal const string EditName = "edit";
    internal const string NewName = "new";
    internal const string NewOpenValue = "open";

    /// <summary>The functions the audience measurement follows for the entry of a record of the list.</summary>
    protected abstract EntryFunctions EntryFunctions { get; }

    protected override IEnumerable<(string Name, object? Value)> Filters => [];

    protected override IEnumerable<(string Name, string Value)> EntryParameters
    {
        get
        {
            if (Edit is { } edited)
                yield return (EditName, edited.ToString());
            else if (Adding)
                yield return (NewName, NewOpenValue);
        }
    }

    protected override (AudienceFunction Function, string? Parameters)? EntryAction => this switch
    {
        { Edit: { } edited } => (EntryFunctions.Edit, $"{Functions.Subject}={edited}"),
        { Adding: true } => (EntryFunctions.New, null),
        _ => null,
    };

    /// <summary>What a referential reads of its address, beyond the criteria of every list.</summary>
    protected static (string? Text, int Page, Guid? Delete, Guid? Merge, string? MergeText, Guid? Edit, bool Adding) Read(string query)
    {
        var address = new Address(query);
        return (address.Search, address.Page, address.Delete, address.Merge, address.MergeText, address.Id(EditName),
            address.Text(NewName) == NewOpenValue);
    }
}

/// <summary>
/// The functions the audience measurement follows for the entry of a record opened in its list; its saving, done by a form
/// rather than an address, is recorded by the page.
/// </summary>
/// <param name="Edit">The change of a record opened, in its row or in a panel.</param>
/// <param name="New">The entry of a new record opened.</param>
public sealed record EntryFunctions(AudienceFunction Edit, AudienceFunction New);
