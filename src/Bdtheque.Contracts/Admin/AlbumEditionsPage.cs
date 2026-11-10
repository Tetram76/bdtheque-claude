using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The page of the editions and purchase intents of an album (visuel/editions-album/contenu.md): the album in brief, as a row
/// of the list of the albums presents it, its editions of the collection and its purchase intents. An edition targeted by an
/// intent is one of the intents, never one of the editions (fonctionnel/regles-metier/intention-d-achat.md).
/// </summary>
/// <param name="Album">The album in brief, with the version that any deletion of its editions or intents sends back.</param>
/// <param name="CreatedAt">When the album was created; its last modification is the one of <paramref name="Album"/>.</param>
/// <param name="Editions">Its owned editions, by year of publication.</param>
/// <param name="PurchaseIntents">Its purchase intents, in the order they were set.</param>
public sealed record AlbumEditionsPage(
    AlbumAdminListItem Album,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AlbumEditionItem> Editions,
    IReadOnlyList<AlbumPurchaseIntentItem> PurchaseIntents);

/// <summary>An owned edition of the album, as its row presents it: its label, what it is, and its copy.</summary>
/// <param name="Cover">Its first cover (fonctionnel/regles-metier/ordre-des-visuels-d-une-edition.md); <c>null</c> for the generic visual.</param>
/// <param name="AcquisitionYear">The year of its acquisition date, if known.</param>
public sealed record AlbumEditionItem(
    EditionSummary Edition,
    AlbumCover? Cover,
    EditionCategory? Category,
    BindingType? Binding,
    int? PageCount,
    AcquisitionMode AcquisitionMode,
    int? AcquisitionYear,
    int VisualCount);

/// <summary>A purchase intent of the album, on the whole album or on an edition it targets.</summary>
/// <param name="Edition">The edition targeted; <c>null</c> for an intent on the whole album, any edition doing.</param>
/// <param name="Category">The category of the edition targeted.</param>
/// <param name="Binding">The binding of the edition targeted.</param>
/// <param name="IsConvertible">
/// Whether the intent turns into one on the other target (fonctionnel/regles-metier/intention-d-achat.md): always for an intent
/// on the album; for an intent on an edition, only when that edition is the only one of its album.
/// </param>
/// <param name="CreatedAt">When the intent was set.</param>
public sealed record AlbumPurchaseIntentItem(
    Guid Id,
    EditionSummary? Edition,
    EditionCategory? Category,
    BindingType? Binding,
    bool IsConvertible,
    DateTimeOffset CreatedAt);
