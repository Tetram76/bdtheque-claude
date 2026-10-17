using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// An album as a card, illustrated by its cover: in the record of a series, the bibliography of an author
/// and the works of a genre or of a universe.
/// </summary>
/// <param name="Cover">
/// That of the album (fonctionnel.md § Couverture d'un album), among all its editions, owned or not;
/// <c>null</c> without any, the generic visual standing in for it (fonctionnel.md § Visuel par défaut).
/// </param>
/// <param name="HasPurchaseIntent">Whether a purchase intent targets the album, or one of its editions.</param>
public sealed record AlbumCard(AlbumSummary Album, AlbumRating? Rating, AlbumCover? Cover, bool HasPurchaseIntent);

/// <summary>The cover of an album: a visual of one of its editions.</summary>
/// <param name="OriginalPath">Path of the original file, relative to the visuals volume.</param>
/// <param name="DisplayPath">Path of the reduced WebP version shown by default, relative to the visuals volume.</param>
public sealed record AlbumCover(string OriginalPath, string DisplayPath);

/// <summary>
/// What represents a series among albums: the number of its albums, and the covers of its first ones,
/// whether the list presents them or not (e.g. a series an author is credited on itself only).
/// </summary>
/// <param name="FirstAlbumCovers">
/// Those of its first <see cref="SeriesOverview.FirstAlbumCount"/> albums at most, in the order of the
/// series (fonctionnel.md § Ordre des albums dans une série), <c>null</c> for an album without cover.
/// </param>
public sealed record SeriesOverview(int AlbumCount, IReadOnlyList<AlbumCover?> FirstAlbumCovers)
{
    /// <summary>How many first albums represent a series: the pile of three covers of the mockups (visuel/fiche-serie.md).</summary>
    public const int FirstAlbumCount = 3;
}
