using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// An album as a card, illustrated by its cover: in the record of a series, the bibliography of an author
/// and the works of a genre or of a universe.
/// </summary>
/// <param name="Cover">
/// That of the album (fonctionnel/regles-metier.md § Couverture d'un album), among all its editions, owned or not;
/// <c>null</c> without any, the generic visual standing in for it (fonctionnel/regles-metier.md § Visuel par défaut).
/// </param>
/// <param name="HasPurchaseIntent">Whether a purchase intent targets the album, or one of its editions.</param>
public sealed record AlbumCard(AlbumSummary Album, AlbumRating? Rating, AlbumCover? Cover, bool HasPurchaseIntent);

/// <summary>The cover of an album: a visual of one of its editions.</summary>
/// <param name="VisualId">The visual, whose media <c>api</c> serves (<see cref="VisualMediaRoutes"/>).</param>
public sealed record AlbumCover(Guid VisualId);

/// <summary>
/// What represents a series: the number of its albums, and the covers of its first volumes, whether the
/// list presents them or not (e.g. a series an author is credited on itself only).
/// </summary>
/// <param name="AlbumCount">Every album of the series, omnibuses and special issues included.</param>
/// <param name="FirstVolumeCovers">
/// Those of its first <see cref="SeriesOverview.FirstVolumeCount"/> regular volumes at most — neither
/// omnibus nor special issue —, in the order of the series (fonctionnel/regles-metier.md § Ordre des albums dans une
/// série), <c>null</c> for a volume without cover. A place beyond them is a generic card (visuel/fiche-serie.md).
/// </param>
public sealed record SeriesOverview(int AlbumCount, IReadOnlyList<AlbumCover?> FirstVolumeCovers)
{
    /// <summary>How many first volumes represent a series: the pile of three covers of the mockups (visuel/fiche-serie.md).</summary>
    public const int FirstVolumeCount = 3;
}
