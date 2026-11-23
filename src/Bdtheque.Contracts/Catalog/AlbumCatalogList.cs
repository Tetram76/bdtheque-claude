using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// A page of the list of the albums of the consultation, and the records its filters designate, named as the lists name them,
/// for the screen to present them without another request.
/// </summary>
public sealed record AlbumCatalogList(Page<AlbumCatalogListItem> Albums, AlbumListFilters Filters);

/// <summary>An album as a row of the list of the consultation presents it.</summary>
/// <param name="Cover">
/// Its cover among the editions the list presents (fonctionnel/regles-metier/couverture-d-un-album.md): its owned editions only
/// in the list limited to them, all its editions otherwise; <c>null</c> for the generic visual.
/// </param>
/// <param name="Authors">The authors credited on the album, each once, by role then by sort key.</param>
/// <param name="FirstPublicationMonth">Set only with <paramref name="FirstPublicationYear"/>.</param>
public sealed record AlbumCatalogListItem(
    AlbumSummary Album,
    AlbumCover? Cover,
    IReadOnlyList<AuthorListItem> Authors,
    int? FirstPublicationYear,
    int? FirstPublicationMonth,
    AlbumRating? Rating,
    AlbumCollectionState State);
