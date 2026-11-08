using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the authors of the administration (visuel/listes/liste-par-liste.md), which its address carries
/// (<see cref="AdminListQuery"/>): its search, on the name, the first name or the pseudonym, and its navigation by initial,
/// those of the list of the consultation; no filters.
/// </summary>
public sealed record AuthorListQuery(
    string? Text = null,
    string? Entry = null,
    int Page = 1,
    Guid? Delete = null,
    Guid? Merge = null,
    string? MergeText = null) : AdminListQuery(Text, Entry, Page, FiltersOpen: false, Delete, Merge, MergeText)
{
    private static readonly ListFunctions AuthorFunctions = new(
        "author", AudienceFunctions.AdminAuthorsSearch, Filter: null, AudienceFunctions.AdminAuthorsInitial, AudienceFunctions.AdminAuthorsPage,
        AudienceFunctions.AdminAuthorsDeletion, AudienceFunctions.AdminAuthorsMerge);

    protected override string Path => AppPaths.AdminAuthors;

    protected override string ApiRoute => "/admin/authors";

    protected override ListFunctions Functions => AuthorFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters => [];

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static AuthorListQuery Parse(string query)
    {
        var address = new Address(query);
        return new AuthorListQuery(address.Search, address.Entry, address.Page, address.Delete, address.Merge, address.MergeText);
    }
}
