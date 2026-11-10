using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the genres of the administration (visuel/listes/liste-par-liste.md), which its address carries
/// (<see cref="ReferentialListQuery"/>): its search, on the label, and the genre changed in its row or the frame of a new one.
/// </summary>
public sealed record GenreListQuery(
    string? Text = null,
    int Page = 1,
    Guid? Delete = null,
    Guid? Merge = null,
    string? MergeText = null,
    Guid? Edit = null,
    bool Adding = false) : ReferentialListQuery(Text, Page, Delete, Merge, MergeText, Edit, Adding)
{
    private static readonly ListFunctions GenreFunctions = new(
        "genre", AudienceFunctions.AdminGenresSearch, Filter: null, Initial: null, AudienceFunctions.AdminGenresPage,
        AudienceFunctions.AdminGenresDeletion, AudienceFunctions.AdminGenresMerge);

    private static readonly EntryFunctions GenreEntryFunctions = new(
        AudienceFunctions.AdminGenresEdit, AudienceFunctions.AdminGenresNew);

    protected override string Path => AppPaths.AdminGenres;

    protected override string ApiRoute => "/admin/genres";

    protected override ListFunctions Functions => GenreFunctions;

    protected override EntryFunctions EntryFunctions => GenreEntryFunctions;

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static GenreListQuery Parse(string query)
    {
        var (text, page, delete, merge, mergeText, edit, adding) = Read(query);
        return new GenreListQuery(text, page, delete, merge, mergeText, edit, adding);
    }
}
