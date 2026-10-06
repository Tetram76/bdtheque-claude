using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// An entry of the public list of the purchase intents, which leads to the record of the album, or of
/// the edition targeted (fonctionnel.md § Structure de l'application).
/// </summary>
/// <param name="Edition">The edition targeted; <c>null</c> for an intent on the whole album.</param>
public sealed record PurchaseIntentListItem(Guid Id, AlbumSummary Album, EditionSummary? Edition);

/// <summary>
/// What the label of an album is built from (fonctionnel.md § Libellé d'un album), with its membership
/// of the collection.
/// </summary>
/// <param name="Title">The title in natural form; <c>null</c> for an album of a series without its own title.</param>
/// <param name="IsInCollection">Whether the album has at least one owned edition (fonctionnel.md § Appartenance à la collection).</param>
public sealed record AlbumSummary(
    Guid Id,
    string? Title,
    Guid? SeriesId,
    string? SeriesTitle,
    AlbumType Type,
    bool IsSpecialIssue,
    int? VolumeNumber,
    int? StartVolumeNumber,
    int? EndVolumeNumber,
    bool IsInCollection);

/// <summary>What the label of an edition is built from (fonctionnel.md § Libellé d'une édition).</summary>
public sealed record EditionSummary(
    Guid Id,
    Guid PublisherId,
    string PublisherName,
    Guid? PublisherCollectionId,
    string? PublisherCollectionName,
    int? PublicationYear,
    string? Isbn);
