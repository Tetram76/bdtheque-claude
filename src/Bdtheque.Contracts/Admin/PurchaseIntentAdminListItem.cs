using Bdtheque.Contracts.Catalog;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// A purchase intent as a row of the list of the administration presents it (visuel/listes/liste-par-liste.md): the album it
/// targets, with its membership of the collection, and what it targets, the whole album or one of its editions.
/// </summary>
/// <param name="Edition">The edition targeted; <c>null</c> for an intent on the whole album, any edition doing.</param>
/// <param name="Cover">The cover of the album (fonctionnel/regles-metier/couverture-d-un-album.md); <c>null</c> for the generic visual.</param>
/// <param name="IsConvertible">
/// Whether the intent turns into one on the other target (fonctionnel/regles-metier/intention-d-achat.md): always for an intent
/// on the album; for an intent on an edition, only when that edition is the only one of its album.
/// </param>
/// <param name="CreatedAt">When the intent was set.</param>
/// <param name="AlbumVersion">Version of its album, sent back with its removal: the intent belongs to the aggregate of its album.</param>
public sealed record PurchaseIntentAdminListItem(
    Guid Id,
    AlbumSummary Album,
    EditionSummary? Edition,
    AlbumCover? Cover,
    bool IsConvertible,
    DateTimeOffset CreatedAt,
    uint AlbumVersion);
