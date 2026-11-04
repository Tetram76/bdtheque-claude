using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// What the label of a record is built from, as it was when the change log recorded it
/// (modele-metier/entree-du-journal-des-modifications.md): data, never a text, so that the label
/// is built when it is presented, in the display culture in force then
/// (fonctionnel/presentation/langue-et-culture-d-affichage.md).
/// </summary>
public abstract record RecordLabel;

/// <summary>What the label of an album is built from (fonctionnel/presentation/libelle-d-un-album.md).</summary>
public sealed record AlbumLabel(
    string? Title,
    string? SeriesTitle,
    AlbumType Type,
    bool IsSpecialIssue,
    int? VolumeNumber,
    int? StartVolumeNumber,
    int? EndVolumeNumber) : RecordLabel;

/// <summary>
/// What the label of an edition is built from (fonctionnel/presentation/libelle-d-une-edition.md), with the
/// label of its album, without which an edition is hardly identifiable.
/// </summary>
public sealed record EditionLabel(
    AlbumLabel Album,
    string PublisherName,
    string? PublisherCollectionName,
    int? PublicationYear,
    string? Isbn) : RecordLabel;

/// <summary>The label of the target of a purchase intent: its album as a whole, or one of its editions.</summary>
public sealed record PurchaseIntentLabel(AlbumLabel? Album, EditionLabel? Edition) : RecordLabel;

/// <summary>What the main identifier of an author is built from
/// (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes).</summary>
public sealed record AuthorLabel(string? LastName, string? FirstName, string? Pseudonym) : RecordLabel;

/// <summary>A publisher collection, with its publisher.</summary>
public sealed record PublisherCollectionLabel(string Name, string PublisherName) : RecordLabel;

/// <summary>A record labelled by its name alone: series, publisher, genre, universe.</summary>
public sealed record NameLabel(string Name) : RecordLabel;

/// <summary>The single setting of the application, which its type of record suffices to label.</summary>
public sealed record ApplicationSettingsLabel : RecordLabel;
