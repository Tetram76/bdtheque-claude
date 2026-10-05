using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>An album as its administration form edits it.</summary>
/// <param name="SortKey">
/// The sort key in force, computed from the title or entered by hand (see
/// <see cref="AlbumContent.ManualSortKey"/>); <c>null</c> for an album with no title, sorted by its
/// series.
/// </param>
/// <param name="Version">Version of the album, sent back with any modification or deletion.</param>
public sealed record AlbumForm(Guid Id, string? SortKey, AlbumContent Content, uint Version);

/// <summary>
/// Everything the user edits on an album, sent to create it, and sent whole to modify it (the form
/// always sends the entire record).
/// </summary>
/// <param name="Title">Required unless the album is attached to a series.</param>
/// <param name="ManualSortKey">
/// The sort key entered by hand, which is then kept whatever the title becomes; <c>null</c> for the
/// automatic mode, where the sort key follows the title (fonctionnel.md § Calcul et stockage de la
/// clé de tri). Requires a title.
/// </param>
/// <param name="StartVolumeNumber">Omnibus only, together with <paramref name="EndVolumeNumber"/>.</param>
/// <param name="FirstPublicationMonth">Requires <paramref name="FirstPublicationYear"/>.</param>
/// <param name="GenreIds">Genres of the album itself, without those of its series.</param>
/// <param name="UniverseIds">Universes of the album itself, without those of its series.</param>
/// <param name="Contributions">
/// Contributions of the album. Left empty while attaching the album to a series, they are copied
/// from the template of that series (fonctionnel.md § Initialisation des contributions depuis la série).
/// </param>
public sealed record AlbumContent(
    string? Title,
    string? ManualSortKey,
    Guid? SeriesId,
    AlbumType Type,
    bool IsSpecialIssue,
    int? VolumeNumber,
    int? StartVolumeNumber,
    int? EndVolumeNumber,
    int? FirstPublicationYear,
    int? FirstPublicationMonth,
    string? Summary,
    string? PersonalNotes,
    AlbumRating? Rating,
    IReadOnlyList<Guid> GenreIds,
    IReadOnlyList<Guid> UniverseIds,
    IReadOnlyList<ContributionContent> Contributions);

/// <param name="Version">Version of the album the form was read at.</param>
public sealed record UpdateAlbumRequest(AlbumContent Content, uint Version);
