using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the albums of the consultation, which its address carries (<see cref="ListQuery"/>): its search,
/// on the title of the album and on that of its series, its cross filters (choix-implementation/api/recherche.md) and the
/// possession, its own filter, by which it limits itself to the collection
/// (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
/// </summary>
public sealed record CatalogAlbumListQuery(
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
    bool FiltersOpen = false) : ListQuery(Text, Entry, Page, FiltersOpen)
{
    // The names of the filters in the address, those of the API, as in the list of the administration.
    internal const string SeriesName = AlbumListQuery.SeriesName;
    internal const string AuthorName = AlbumListQuery.AuthorName;
    internal const string PublisherName = AlbumListQuery.PublisherName;
    internal const string PublisherCollectionName = AlbumListQuery.PublisherCollectionName;
    internal const string GenreName = AlbumListQuery.GenreName;
    internal const string UniverseName = AlbumListQuery.UniverseName;
    internal const string StateName = AlbumListQuery.StateName;

    private static readonly ListCriteriaFunctions AlbumFunctions = new(
        AudienceFunctions.AlbumsSearch, AudienceFunctions.AlbumsFilter, AudienceFunctions.AlbumsInitial, AudienceFunctions.AlbumsPage);

    /// <summary>The whole list, as the menu of the consultation opens it.</summary>
    public static readonly CatalogAlbumListQuery All = new();

    protected override string Path => AppPaths.Albums;

    protected override string ApiRoute => "/catalog/albums";

    protected override ListCriteriaFunctions CriteriaFunctions => AlbumFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters =>
    [
        (SeriesName, SeriesId), (AuthorName, AuthorId), (PublisherName, PublisherId), (PublisherCollectionName, PublisherCollectionId),
        (GenreName, GenreId), (UniverseName, UniverseId), (StateName, State),
    ];

    /// <summary>The list without its filters, back on its first page: « Tout effacer ».</summary>
    public CatalogAlbumListQuery WithoutFilters() => new(Text, Entry);

    /// <inheritdoc cref="ListQuery.Address"/>
    public static CatalogAlbumListQuery Parse(string query)
    {
        var address = new Address(query);
        return new CatalogAlbumListQuery(
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
            address.FiltersOpen);
    }
}
