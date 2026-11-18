using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the genres of the administration (visuel/listes/liste-par-liste.md), which its address carries
/// (<see cref="InListEntryQuery"/>): its search, on the label, its navigation by initial, without filters, and the genre
/// changed in its row or the frame of a new one.
/// </summary>
public sealed record GenreListQuery(
    string? Text = null,
    string? Entry = null,
    int Page = 1,
    Guid? Delete = null,
    Guid? Merge = null,
    string? MergeText = null,
    Guid? Edit = null,
    bool Adding = false) : InListEntryQuery(Text, Entry, Page, Delete, Merge, MergeText, Edit, Adding)
{
    private static readonly ListFunctions GenreFunctions = new(
        "genre", AudienceFunctions.AdminGenresSearch, Filter: null, AudienceFunctions.AdminGenresInitial, AudienceFunctions.AdminGenresPage,
        AudienceFunctions.AdminGenresDeletion, AudienceFunctions.AdminGenresMerge);

    private static readonly EntryFunctions GenreEntryFunctions = new(
        AudienceFunctions.AdminGenresEdit, AudienceFunctions.AdminGenresNew);

    protected override string Path => AppPaths.AdminGenres;

    protected override string ApiRoute => "/admin/genres";

    protected override ListFunctions Functions => GenreFunctions;

    protected override EntryFunctions EntryFunctions => GenreEntryFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters => [];

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static GenreListQuery Parse(string query)
    {
        var address = new Address(query);
        var (edit, adding) = ReadEntry(address);
        return new GenreListQuery(address.Search, address.Entry, address.Page, address.Delete, address.Merge, address.MergeText, edit, adding);
    }
}
