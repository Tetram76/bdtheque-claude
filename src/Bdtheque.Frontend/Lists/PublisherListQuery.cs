using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the publishers of the administration (visuel/listes/liste-par-liste.md), which its address
/// carries (<see cref="AdminListQuery"/>): its search, on the name, without filters nor navigation by initial.
/// </summary>
public sealed record PublisherListQuery(
    string? Text = null,
    int Page = 1,
    Guid? Delete = null,
    Guid? Merge = null,
    string? MergeText = null) : AdminListQuery(Text, Entry: null, Page, FiltersOpen: false, Delete, Merge, MergeText)
{
    private static readonly ListFunctions PublisherFunctions = new(
        "publisher", AudienceFunctions.AdminPublishersSearch, Filter: null, Initial: null, AudienceFunctions.AdminPublishersPage,
        AudienceFunctions.AdminPublishersDeletion, AudienceFunctions.AdminPublishersMerge);

    protected override string Path => AppPaths.AdminPublishers;

    protected override string ApiRoute => "/admin/publishers";

    protected override ListFunctions Functions => PublisherFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters => [];

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static PublisherListQuery Parse(string query)
    {
        var address = new Address(query);
        return new PublisherListQuery(address.Search, address.Page, address.Delete, address.Merge, address.MergeText);
    }
}
