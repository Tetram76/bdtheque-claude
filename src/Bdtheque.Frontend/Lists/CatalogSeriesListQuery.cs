using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the series of the consultation, which its address carries (<see cref="ListQuery"/>): its search,
/// on the title of the series, its cross filters, those of the list of the administration (choix-implementation/api/recherche.md),
/// and the possession of its albums, its own filter, by which it limits itself to the collection
/// (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
/// </summary>
public sealed record CatalogSeriesListQuery(
    string? Text = null,
    string? Entry = null,
    Guid? AuthorId = null,
    Guid? PublisherId = null,
    Guid? PublisherCollectionId = null,
    Guid? GenreId = null,
    Guid? UniverseId = null,
    AlbumCollectionState? State = null,
    int Page = 1,
    bool FiltersOpen = false) : ListQuery(Text, Entry, Page, FiltersOpen)
{
    // The names of the filters in the address, those of the API, as in the list of the administration; the possession, as in
    // the lists of the albums.
    internal const string AuthorName = SeriesListQuery.AuthorName;
    internal const string PublisherName = SeriesListQuery.PublisherName;
    internal const string PublisherCollectionName = SeriesListQuery.PublisherCollectionName;
    internal const string GenreName = SeriesListQuery.GenreName;
    internal const string UniverseName = SeriesListQuery.UniverseName;
    internal const string StateName = AlbumListQuery.StateName;

    private static readonly ListCriteriaFunctions SeriesFunctions = new(
        AudienceFunctions.SeriesListSearch, AudienceFunctions.SeriesListFilter, AudienceFunctions.SeriesListInitial, AudienceFunctions.SeriesListPage);

    /// <summary>The whole list, as the menu of the consultation opens it.</summary>
    public static readonly CatalogSeriesListQuery All = new();

    protected override string Path => AppPaths.Series;

    protected override string ApiRoute => "/catalog/series";

    protected override ListCriteriaFunctions CriteriaFunctions => SeriesFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters =>
    [
        (AuthorName, AuthorId), (PublisherName, PublisherId), (PublisherCollectionName, PublisherCollectionId), (GenreName, GenreId),
        (UniverseName, UniverseId), (StateName, State),
    ];

    /// <summary>The list without its filters, back on its first page: « Tout effacer ».</summary>
    public CatalogSeriesListQuery WithoutFilters() => new(Text, Entry);

    /// <inheritdoc cref="ListQuery.Address"/>
    public static CatalogSeriesListQuery Parse(string query)
    {
        var address = new Address(query);
        return new CatalogSeriesListQuery(
            address.Search,
            address.Entry,
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
