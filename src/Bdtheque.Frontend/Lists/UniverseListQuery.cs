using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the universes of the administration (visuel/listes/liste-par-liste.md), which its address
/// carries (<see cref="ReferentialListQuery"/>): its search, on the name, and the panel of a universe, changed or new.
/// </summary>
public sealed record UniverseListQuery(
    string? Text = null,
    int Page = 1,
    Guid? Delete = null,
    Guid? Merge = null,
    string? MergeText = null,
    Guid? Edit = null,
    bool Adding = false) : ReferentialListQuery(Text, Page, Delete, Merge, MergeText, Edit, Adding)
{
    private static readonly ListFunctions UniverseFunctions = new(
        "universe", AudienceFunctions.AdminUniversesSearch, Filter: null, Initial: null, AudienceFunctions.AdminUniversesPage,
        AudienceFunctions.AdminUniversesDeletion, AudienceFunctions.AdminUniversesMerge);

    private static readonly EntryFunctions UniverseEntryFunctions = new(
        AudienceFunctions.AdminUniversesEdit, AudienceFunctions.AdminUniversesNew);

    protected override string Path => AppPaths.AdminUniverses;

    protected override string ApiRoute => "/admin/universes";

    protected override ListFunctions Functions => UniverseFunctions;

    protected override EntryFunctions EntryFunctions => UniverseEntryFunctions;

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static UniverseListQuery Parse(string query)
    {
        var (text, page, delete, merge, mergeText, edit, adding) = Read(query);
        return new UniverseListQuery(text, page, delete, merge, mergeText, edit, adding);
    }
}
