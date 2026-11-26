using Bdtheque.Frontend.Audience;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of a list of the administration whose records are entered over the list rather than on a page of their own
/// (visuel/listes/saisie-selon-l-entite.md: a genre in its row, a universe or an author in a side panel), which its address
/// carries (<see cref="AdminListQuery"/>), with the entry the address opens.
/// </summary>
/// <param name="Edit">The record being changed.</param>
/// <param name="Adding">Whether a new record is being entered.</param>
public abstract record InListEntryQuery(
    string? Text,
    string? Entry,
    int Page,
    Guid? Delete,
    Guid? Merge,
    string? MergeText,
    Guid? Edit,
    bool Adding) : AdminListQuery(Text, Entry, Page, FiltersOpen: false, Delete, Merge, MergeText)
{
    internal const string EditName = "edit";
    internal const string NewName = "new";
    internal const string NewOpenValue = "open";

    /// <summary>The functions the audience measurement follows for the entry of a record of the list.</summary>
    protected abstract EntryFunctions EntryFunctions { get; }

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

    /// <summary>The entry the address opens: the record changed, or a new one.</summary>
    protected static (Guid? Edit, bool Adding) ReadEntry(Address address) =>
        (address.Id(EditName), address.Text(NewName) == NewOpenValue);
}

/// <summary>
/// The functions the audience measurement follows for the entry of a record opened in its list; its saving, done by a form
/// rather than an address, is recorded by the page.
/// </summary>
/// <param name="Edit">The change of a record opened, in its row or in a panel.</param>
/// <param name="New">The entry of a new record opened.</param>
public sealed record EntryFunctions(AudienceFunction Edit, AudienceFunction New);
