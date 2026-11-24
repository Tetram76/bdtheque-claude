using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the public list of the purchase intents, which its address carries (<see cref="ListQuery"/>): its search, on
/// the title of the album or of its series, the navigation by initial of its albums, and the cross filters of the list of the
/// albums, applied to the album of each intent, without the possession, of no interest there
/// (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
/// </summary>
public sealed record CatalogPurchaseIntentListQuery(
    string? Text = null,
    string? Entry = null,
    Guid? SeriesId = null,
    Guid? AuthorId = null,
    Guid? PublisherId = null,
    Guid? PublisherCollectionId = null,
    Guid? GenreId = null,
    Guid? UniverseId = null,
    int Page = 1,
    bool FiltersOpen = false) : ListQuery(Text, Entry, Page, FiltersOpen)
{
    private static readonly ListCriteriaFunctions PurchaseIntentFunctions = new(
        AudienceFunctions.PurchaseIntentsSearch, AudienceFunctions.PurchaseIntentsFilter, AudienceFunctions.PurchaseIntentsInitial,
        AudienceFunctions.PurchaseIntentsPage);

    /// <summary>The whole list, as the menu of the consultation opens it.</summary>
    public static readonly CatalogPurchaseIntentListQuery All = new();

    protected override string Path => AppPaths.PurchaseIntents;

    protected override string ApiRoute => "/catalog/purchase-intents";

    protected override ListCriteriaFunctions CriteriaFunctions => PurchaseIntentFunctions;

    // The names of the filters in the address, those of the list of the albums, whose filters they are.
    protected override IEnumerable<(string Name, object? Value)> Filters =>
    [
        (AlbumListQuery.SeriesName, SeriesId), (AlbumListQuery.AuthorName, AuthorId), (AlbumListQuery.PublisherName, PublisherId),
        (AlbumListQuery.PublisherCollectionName, PublisherCollectionId), (AlbumListQuery.GenreName, GenreId), (AlbumListQuery.UniverseName, UniverseId),
    ];

    /// <summary>The list without its filters, back on its first page: « Tout effacer ».</summary>
    public CatalogPurchaseIntentListQuery WithoutFilters() => new(Text, Entry);

    /// <inheritdoc cref="ListQuery.Address"/>
    public static CatalogPurchaseIntentListQuery Parse(string query)
    {
        var address = new Address(query);
        return new CatalogPurchaseIntentListQuery(
            address.Search,
            address.Entry,
            address.Id(AlbumListQuery.SeriesName),
            address.Id(AlbumListQuery.AuthorName),
            address.Id(AlbumListQuery.PublisherName),
            address.Id(AlbumListQuery.PublisherCollectionName),
            address.Id(AlbumListQuery.GenreName),
            address.Id(AlbumListQuery.UniverseName),
            address.Page,
            address.FiltersOpen);
    }
}
