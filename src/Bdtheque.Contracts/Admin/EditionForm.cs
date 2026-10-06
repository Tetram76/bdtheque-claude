using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// An edition as its administration form edits it. The edition belongs to the aggregate of its
/// album: the version sent back with any modification or deletion is the album's.
/// </summary>
/// <param name="IsbnChecksumValid">
/// Whether the check digit of the ISBN is consistent, <c>null</c> without ISBN: a warning only, an
/// inconsistent ISBN being stored as entered (fonctionnel.md § Validation de l'ISBN).
/// </param>
/// <param name="AlbumVersion">Version of the album, sent back with any modification or deletion of the edition.</param>
public sealed record EditionForm(Guid Id, Guid AlbumId, EditionContent Content, bool? IsbnChecksumValid, uint AlbumVersion);

/// <summary>
/// The form of a new edition of an album, pre-filled from the template of the album's series, or
/// with the default values for an album without series (fonctionnel.md § Initialisation d'une
/// nouvelle édition depuis la série).
/// </summary>
/// <param name="AlbumVersion">Version of the album, sent back with the creation of the edition.</param>
public sealed record NewEditionForm(Guid AlbumId, EditionContent Content, uint AlbumVersion);

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
/// Confirms the purchase of an edition targeted by an intent (fonctionnel.md § Réalisation d'une
/// intention): the edition, with its acquisition, which realizes the intent.
/// </summary>
/// <param name="Content">The whole edition, whose acquisition mode is required.</param>
/// <param name="AlbumVersion">Version of the album the form was read at.</param>
public sealed record AcquireEditionRequest(EditionContent Content, uint AlbumVersion);

/// <summary>Result of the check of an ISBN during entry (fonctionnel.md § Validation de l'ISBN).</summary>
/// <param name="IsChecksumValid">Whether the check digit is consistent: a warning only, never a refusal.</param>
public sealed record IsbnCheck(bool IsChecksumValid);
