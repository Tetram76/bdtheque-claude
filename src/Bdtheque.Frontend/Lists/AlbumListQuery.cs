using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the albums of the administration (visuel/liste-albums/contenu.md), which its address
/// carries (<see cref="AdminListQuery"/>): its search, on the title of the album and on that of its series, its cross filters
/// and the state of the album with regard to the collection.
/// </summary>
public sealed record AlbumListQuery(
    string? Text = null,
    string? Entry = null,
    Guid? SeriesId = null,
    Guid? AuthorId = null,
    Guid? PublisherId = null,
    Guid? PublisherCollectionId = null,
    Guid? GenreId = null,
    Guid? UniverseId = null,
    AlbumCollectionState? State = null,
    int Page = 1,
    bool FiltersOpen = false,
    Guid? Delete = null,
    Guid? Merge = null,
    string? MergeText = null) : AdminListQuery(Text, Entry, Page, FiltersOpen, Delete, Merge, MergeText)
{
    internal const string SeriesName = "seriesId";
    internal const string AuthorName = "authorId";
    internal const string PublisherName = "publisherId";
    internal const string PublisherCollectionName = "publisherCollectionId";
    internal const string GenreName = "genreId";
    internal const string UniverseName = "universeId";
    internal const string StateName = "state";

    private static readonly ListFunctions AlbumFunctions = new(
        "album", AudienceFunctions.AdminAlbumsSearch, AudienceFunctions.AdminAlbumsFilter, AudienceFunctions.AdminAlbumsInitial,
        AudienceFunctions.AdminAlbumsPage, AudienceFunctions.AdminAlbumsDeletion, AudienceFunctions.AdminAlbumsMerge);

    /// <summary>The whole list, as the menu of the administration opens it.</summary>
    public static readonly AlbumListQuery All = new();

    protected override string Path => AppPaths.AdminAlbums;

    protected override string ApiRoute => "/admin/albums";

    protected override ListFunctions Functions => AlbumFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters =>
    [
        (SeriesName, SeriesId), (AuthorName, AuthorId), (PublisherName, PublisherId), (PublisherCollectionName, PublisherCollectionId),
        (GenreName, GenreId), (UniverseName, UniverseId), (StateName, State),
    ];

    /// <summary>The list without its filters, back on its first page: « Tout effacer ».</summary>
    public AlbumListQuery WithoutFilters() => new(Text, Entry);

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static AlbumListQuery Parse(string query)
    {
        var address = new Address(query);
        return new AlbumListQuery(
            address.Search,
            address.Entry,
            address.Id(SeriesName),
            address.Id(AuthorName),
            address.Id(PublisherName),
            address.Id(PublisherCollectionName),
            address.Id(GenreName),
            address.Id(UniverseName),
            address.Enum<AlbumCollectionState>(StateName),
            address.Page,
            address.FiltersOpen,
            address.Delete,
            address.Merge,
            address.MergeText);
    }
}
