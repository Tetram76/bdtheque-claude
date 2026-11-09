using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the editions of the administration (visuel/listes/liste-par-liste.md), which its address
/// carries (<see cref="AdminListQuery"/>): its search, on the ISBN, and its cross filters, those of the list of the
/// consultation, without navigation by initial.
/// </summary>
public sealed record EditionListQuery(
    string? Text = null,
    Guid? AlbumId = null,
    Guid? PublisherId = null,
    Guid? PublisherCollectionId = null,
    int Page = 1,
    bool FiltersOpen = false,
    Guid? Delete = null,
    Guid? Merge = null,
    string? MergeText = null) : AdminListQuery(Text, Entry: null, Page, FiltersOpen, Delete, Merge, MergeText)
{
    internal const string AlbumName = "albumId";
    internal const string PublisherName = "publisherId";
    internal const string PublisherCollectionName = "publisherCollectionId";

    private static readonly ListFunctions EditionFunctions = new(
        "edition", AudienceFunctions.AdminEditionsSearch, AudienceFunctions.AdminEditionsFilter, Initial: null,
        AudienceFunctions.AdminEditionsPage, AudienceFunctions.AdminEditionsDeletion, AudienceFunctions.AdminEditionsMerge);

    protected override string Path => AppPaths.AdminEditions;

    protected override string ApiRoute => "/admin/editions";

    protected override ListFunctions Functions => EditionFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters =>
        [(AlbumName, AlbumId), (PublisherName, PublisherId), (PublisherCollectionName, PublisherCollectionId)];

    /// <summary>The list without its filters, back on its first page: « Tout effacer ».</summary>
    public EditionListQuery WithoutFilters() => new(Text);

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static EditionListQuery Parse(string query)
    {
        var address = new Address(query);
        return new EditionListQuery(
            address.Search,
            address.Id(AlbumName),
            address.Id(PublisherName),
            address.Id(PublisherCollectionName),
            address.Page,
            address.FiltersOpen,
            address.Delete,
            address.Merge,
            address.MergeText);
    }
}
