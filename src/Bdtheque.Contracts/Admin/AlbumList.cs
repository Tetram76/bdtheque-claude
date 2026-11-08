using System.Text.Json.Serialization;
using Bdtheque.Contracts.Catalog;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The list of the albums of the administration (visuel/liste-albums/contenu.md): one page of its albums, and
/// what the list presents of the records it is filtered on.
/// </summary>
public sealed record AlbumAdminList(Page<AlbumAdminListItem> Albums, AlbumListFilters Filters);

/// <summary>An album as a row of the list of the administration presents it.</summary>
/// <param name="Cover">
/// Its cover among all its editions, owned or targeted by a purchase intent
/// (fonctionnel/regles-metier/couverture-d-un-album.md); <c>null</c> for the generic visual.
/// </param>
/// <param name="Authors">The authors credited on the album, each once, by role then by sort key.</param>
/// <param name="FirstPublicationMonth">Set only with <paramref name="FirstPublicationYear"/>.</param>
/// <param name="EditionCount">Its owned editions only: an edition targeted by an intent is one of its intents.</param>
/// <param name="PurchaseIntentCount">Its purchase intents, on the album or on its editions.</param>
/// <param name="Version">Version of the album, sent back with its deletion.</param>
public sealed record AlbumAdminListItem(
    AlbumSummary Album,
    AlbumCover? Cover,
    IReadOnlyList<AuthorListItem> Authors,
    int? FirstPublicationYear,
    int? FirstPublicationMonth,
    int EditionCount,
    int PurchaseIntentCount,
    AlbumCollectionState State,
    DateTimeOffset ModifiedAt,
    uint Version);

/// <summary>
/// Where an album stands with regard to the collection, as the list of the administration marks it and filters it
/// (fonctionnel/regles-metier/appartenance-a-la-collection.md): each album has exactly one.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AlbumCollectionState>))]
public enum AlbumCollectionState
{
    /// <summary>At least one owned edition, whatever its purchase intents.</summary>
    InCollection = 0,

    /// <summary>No owned edition, but a purchase intent on the album or on one of its editions.</summary>
    PurchaseIntent = 1,

    /// <summary>Neither an owned edition nor a purchase intent.</summary>
    OutsideCollection = 2,
}

/// <summary>
/// The records the list is filtered on, as the list names them; each <c>null</c> when the list is not filtered on
/// its type, or on a record that does not exist (a filtered list does not address its record: the list is then
/// empty, choix-implementation/api/recherche.md).
/// </summary>
public sealed record AlbumListFilters(
    SeriesListItem? Series,
    AuthorListItem? Author,
    PublisherListItem? Publisher,
    PublisherCollectionListItem? PublisherCollection,
    GenreListItem? Genre,
    UniverseListItem? Universe);
