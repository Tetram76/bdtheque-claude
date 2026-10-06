using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The purchase intents of an album, which belong to the aggregate of the album: the version sent
/// back with any write on them is the album's.
/// </summary>
/// <param name="AlbumVersion">Version of the album, sent back with any write on its intents.</param>
public sealed record PurchaseIntentsForm(Guid AlbumId, IReadOnlyList<PurchaseIntentForm> Intents, uint AlbumVersion);

/// <summary>A purchase intent, on the whole album or on one of its editions.</summary>
/// <param name="EditionId">The edition targeted, not owned; <c>null</c> for an intent on the whole album.</param>
public sealed record PurchaseIntentForm(Guid Id, Guid? EditionId);

/// <summary>
/// The form of an intent on a new edition, pre-filled as the form of a new edition is (fonctionnel.md
/// § Initialisation d'une nouvelle édition depuis la série).
/// </summary>
/// <param name="AlbumVersion">Version of the album, sent back with the creation or the conversion of the intent.</param>
public sealed record NewPurchaseIntentForm(Guid AlbumId, PurchaseIntentEditionContent Edition, uint AlbumVersion);

/// <summary>
/// The edition an intent targets, entered with the minimum of fields: those of the published edition,
/// without the traits of the copy once owned (condition, second hand, dedication, freeness,
/// acquisition, personal reference and notes — fonctionnel.md § Réalisation d'une intention).
/// </summary>
/// <param name="PublisherId">Required.</param>
/// <param name="PublisherCollectionId">Must belong to the publisher.</param>
public sealed record PurchaseIntentEditionContent(
    Guid? PublisherId,
    Guid? PublisherCollectionId,
    int? PublicationYear,
    string? Isbn,
    BindingType? Binding,
    BookOrientation? Orientation,
    ReadingDirection? ReadingDirection,
    EditionFormat? Format,
    int? PageCount,
    EditionCategory? Category,
    bool IsColor);

/// <param name="Edition">The new edition targeted; <c>null</c> for an intent on the whole album.</param>
/// <param name="AlbumVersion">Version of the album the form was read at.</param>
public sealed record CreatePurchaseIntentRequest(PurchaseIntentEditionContent? Edition, uint AlbumVersion);

/// <summary>Converts the intent on the whole album into an intent on a new edition.</summary>
/// <param name="AlbumVersion">Version of the album the form was read at.</param>
public sealed record ConvertPurchaseIntentToEditionRequest(PurchaseIntentEditionContent Edition, uint AlbumVersion);

/// <summary>
/// Converts an intent on an edition into an intent on the whole album, which deletes the edition
/// targeted with its visuals: the same effect as deleting the intent, whose impact the user confirms.
/// </summary>
/// <param name="AlbumVersion">Version of the album the form was read at.</param>
/// <param name="Fingerprint">Fingerprint of the deletion impact of the intent, as confirmed by the user.</param>
public sealed record ConvertPurchaseIntentToAlbumRequest(uint AlbumVersion, string Fingerprint);
