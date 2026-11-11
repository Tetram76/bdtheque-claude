using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// An edition as its administration form edits it. The edition belongs to the aggregate of its
/// album: the version sent back with any modification or deletion is the album's.
/// </summary>
/// <param name="IsbnChecksumValid">
/// Whether the check digit of the ISBN is consistent, <c>null</c> without ISBN: a warning only, an
/// inconsistent ISBN being stored as entered (fonctionnel/regles-metier/validation-de-l-isbn.md).
/// </param>
/// <param name="AlbumVersion">Version of the album, sent back with any modification or deletion of the edition.</param>
/// <param name="Album">The album of the edition, which the form names and leads to.</param>
/// <param name="References">The records the content references by their identifier, named for the form to present them.</param>
/// <param name="PurchaseIntent">The intent that targets the edition, not owned; <c>null</c> for an owned edition.</param>
/// <param name="EnteredCollectionAt">When the edition became owned; <c>null</c> for an edition targeted by an intent.</param>
public sealed record EditionForm(
    Guid Id,
    Guid AlbumId,
    EditionContent Content,
    bool? IsbnChecksumValid,
    uint AlbumVersion,
    AlbumSummary Album,
    EditionFormReferences References,
    PurchaseIntentItem? PurchaseIntent,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    DateTimeOffset? EnteredCollectionAt);

/// <summary>The records an <see cref="EditionContent"/> references, named as the lists name them.</summary>
/// <param name="PublisherCollection">With its publisher, the one it belongs to.</param>
public sealed record EditionFormReferences(PublisherListItem? Publisher, PublisherCollectionListItem? PublisherCollection);

/// <summary>
/// The form of a new edition of an album, pre-filled from the template of the album's series, or
/// with the default values for an album without series
/// (fonctionnel/regles-metier/initialisation-d-une-nouvelle-edition-depuis-la-serie.md).
/// </summary>
/// <param name="AlbumVersion">Version of the album, sent back with the creation of the edition.</param>
/// <param name="Album">The album of the edition, which the form names and leads to.</param>
/// <param name="References">The records the pre-filled content references, named for the form to present them.</param>
/// <param name="SeriesTemplate">
/// The template of the album's series, whose fields with a value are those the series pre-filled; <c>null</c> for an album
/// without series, whose form starts from the default values.
/// </param>
public sealed record NewEditionForm(
    Guid AlbumId, EditionContent Content, uint AlbumVersion, AlbumSummary Album, EditionFormReferences References,
    SeriesEditionTemplate? SeriesTemplate);

/// <summary>
/// Everything the user edits on an edition, sent to create it, and sent whole to modify it (the form
/// always sends the entire record).
/// </summary>
/// <param name="PublisherId">Required.</param>
/// <param name="PublisherCollectionId">Must belong to the publisher.</param>
/// <param name="AcquisitionMode">
/// Required to create an edition, which is then owned, and kept by an owned edition. Without it,
/// the edition — targeted by a purchase intent — carries neither acquisition date nor amount.
/// </param>
/// <param name="AcquisitionAmount">
/// Together with <paramref name="AcquisitionCurrency"/> (ISO 4217 code, or <c>QZF</c> for the old
/// franc). Requires a reference date: acquisition date, edition year or album's first publication.
/// </param>
/// <param name="IsFree">A free edition has neither acquisition amount nor initial value, and is never purchased.</param>
/// <param name="InitialValueAmount">
/// Together with <paramref name="InitialValueCurrency"/>. Requires the edition year or the album's
/// first publication.
/// </param>
public sealed record EditionContent(
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
    bool IsDedicated,
    bool IsColor,
    EditionCondition? Condition,
    AcquisitionMode? AcquisitionMode,
    bool IsSecondHand,
    DateOnly? AcquisitionDate,
    decimal? AcquisitionAmount,
    string? AcquisitionCurrency,
    bool IsFree,
    decimal? InitialValueAmount,
    string? InitialValueCurrency,
    string? PersonalReference,
    string? PersonalNotes);

/// <param name="AlbumVersion">Version of the album the form was read at.</param>
public sealed record CreateEditionRequest(EditionContent Content, uint AlbumVersion);

/// <param name="AlbumVersion">Version of the album the form was read at.</param>
public sealed record UpdateEditionRequest(EditionContent Content, uint AlbumVersion);

/// <summary>
/// Confirms the purchase of an edition targeted by an intent
/// (fonctionnel/regles-metier/intention-d-achat.md#réalisation-dune-intention): the edition, with its
/// acquisition, which realizes the intent.
/// </summary>
/// <param name="Content">The whole edition, whose acquisition mode is required.</param>
/// <param name="AlbumVersion">Version of the album the form was read at.</param>
public sealed record AcquireEditionRequest(EditionContent Content, uint AlbumVersion);

/// <summary>Result of the check of an ISBN during entry (fonctionnel/regles-metier/validation-de-l-isbn.md).</summary>
/// <param name="IsChecksumValid">Whether the check digit is consistent: a warning only, never a refusal.</param>
/// <param name="Isbn">The ISBN as it would be saved, its check character in capitals, which the entry takes up.</param>
public sealed record IsbnCheck(bool IsChecksumValid, string? Isbn);
