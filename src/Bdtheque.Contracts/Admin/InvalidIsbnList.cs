using System.Text.Json.Serialization;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The list of the editions whose ISBN has an inconsistent check digit (visuel/listes/liste-par-liste.md): one page of them, what
/// the list presents of the records it is filtered on, and how many of the editions it finds are errors accepted.
/// </summary>
/// <param name="AcceptedCount">How many of the editions of the whole list are errors accepted, which its count tells.</param>
public sealed record InvalidIsbnList(Page<InvalidIsbnListItem> Editions, EditionListFilters Filters, int AcceptedCount);

/// <summary>
/// An edition whose ISBN has an inconsistent check digit, owned or targeted by a purchase intent, as a row of its list presents it
/// (fonctionnel/regles-metier/validation-de-l-isbn.md).
/// </summary>
/// <param name="Cover">Its first cover; <c>null</c> for the generic visual.</param>
/// <param name="CorrectedIsbn">The ISBN the application supposes; <c>null</c> when no check character makes the ISBN consistent.</param>
/// <param name="IsOwned">Whether the edition is owned; otherwise a purchase intent targets it.</param>
/// <param name="AcquisitionMode">The mode of its acquisition, for an owned edition.</param>
/// <param name="AcquisitionYear">The year of its acquisition date, if known.</param>
/// <param name="IsAccepted">Whether its error is accepted.</param>
/// <param name="AlbumVersion">Version of its album, sent back by each of its actions: the edition belongs to the aggregate of its album.</param>
public sealed record InvalidIsbnListItem(
    AlbumSummary Album,
    EditionSummary Edition,
    AlbumCover? Cover,
    string? CorrectedIsbn,
    bool IsOwned,
    AcquisitionMode? AcquisitionMode,
    int? AcquisitionYear,
    bool IsAccepted,
    DateTimeOffset ModifiedAt,
    uint AlbumVersion);

/// <summary>Which errors the list of the ISBN to check presents (filtre « État »).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<InvalidIsbnState>))]
public enum InvalidIsbnState
{
    /// <summary>The errors to correct, the default.</summary>
    ToCorrect = 1,

    /// <summary>The errors accepted.</summary>
    Accepted = 2,

    /// <summary>Both.</summary>
    All = 3,
}

/// <summary>
/// An action on the ISBN of an edition from its list: its correction, or the acceptance of its error, under the version of its
/// album.
/// </summary>
public sealed record IsbnActionRequest(uint AlbumVersion);
