using Bdtheque.Contracts.Admin;

namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// An intent as a row of the public list of the purchase intents presents it, which leads to the record of the album targeted
/// (fonctionnel/structure-de-l-application.md): always as an intent, with the album or the edition it targets
/// (fonctionnel/regles-metier/intention-d-acquisition.md).
/// </summary>
/// <param name="State">The possession of the album, never outside the collection for an album targeted.</param>
/// <param name="Edition">The edition targeted; <c>null</c> for an intent on the whole album.</param>
/// <param name="PublisherWebsite">
/// Where the publisher of the edition targeted leads (fonctionnel/presentation/libelle-d-une-edition.md); <c>null</c> when unknown,
/// or for an intent on the whole album.
/// </param>
/// <param name="Cover">
/// The cover of the album among all its editions, the list being a context of documentary base
/// (fonctionnel/regles-metier/couverture-d-un-album.md); <c>null</c> for the generic visual.
/// </param>
/// <param name="CreatedAt">When the intent was set.</param>
public sealed record PurchaseIntentListItem(
    Guid Id,
    AlbumSummary Album,
    AlbumCollectionState State,
    EditionSummary? Edition,
    string? PublisherWebsite,
    AlbumCover? Cover,
    DateTimeOffset CreatedAt);

/// <summary>
/// A page of the public list of the purchase intents, and the records its filters designate, named as the lists name them, for
/// the screen to present them without another request.
/// </summary>
public sealed record PurchaseIntentCatalogList(Page<PurchaseIntentListItem> Intents, AlbumListFilters Filters);
