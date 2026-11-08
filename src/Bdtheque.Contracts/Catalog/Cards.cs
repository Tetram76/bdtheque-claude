using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// An album as a card, illustrated by its cover: in the record of a series, the bibliography of an author
/// and the works of a genre or of a universe.
/// </summary>
/// <param name="Cover">
/// That of the album (fonctionnel/regles-metier/couverture-d-un-album.md), among all its editions, owned or not;
/// <c>null</c> without any, the generic visual standing in for it (fonctionnel/regles-metier/visuel-par-defaut.md).
/// </param>
/// <param name="HasPurchaseIntent">Whether a purchase intent targets the album, or one of its editions.</param>
public sealed record AlbumCard(AlbumSummary Album, AlbumRating? Rating, AlbumCover? Cover, bool HasPurchaseIntent);

/// <summary>The cover of an album: a visual of one of its editions.</summary>
/// <param name="VisualId">The visual, whose media <c>api</c> serves (<see cref="VisualMediaRoutes"/>).</param>
/// <param name="Palette">The colours drawn from it, e.g. for the spine of the album shown as a book.</param>
public sealed record AlbumCover(Guid VisualId, CoverPalette Palette);

/// <summary>
/// The three colours drawn from a cover (visuel/dashboard/la-couverture-du-jour.md), each in the hexadecimal notation
/// of CSS, <c>#RRGGBB</c>. Legibility of a text set on them is up to the theme that paints them.
/// </summary>
/// <param name="Dominant">The colour that most stands out on the cover.</param>
/// <param name="Deep">A deep version of <paramref name="Dominant"/>, on which white text is legible.</param>
/// <param name="Accent">A colour of the cover that sets itself apart from <paramref name="Dominant"/>.</param>
public sealed record CoverPalette(string Dominant, string Deep, string Accent);

/// <summary>
/// What represents a series: the number of its albums, and the covers of its first volumes, whether the
/// list presents them or not (e.g. a series an author is credited on itself only).
/// </summary>
/// <param name="AlbumCount">Every album of the series, omnibuses and special issues included.</param>
/// <param name="FirstVolumeCovers">
/// Those of its first <see cref="SeriesOverview.FirstVolumeCount"/> regular volumes at most — neither
/// omnibus nor special issue —, in the order of the series
/// (fonctionnel/regles-metier/ordre-des-albums-dans-une-serie.md), <c>null</c> for a volume without cover. A place
/// beyond them is a generic card (visuel/fiche-serie/).
/// </param>
public sealed record SeriesOverview(int AlbumCount, IReadOnlyList<AlbumCover?> FirstVolumeCovers)
{
    /// <summary>How many first volumes represent a series: the pile of three covers of the mockups (visuel/fiche-serie/).</summary>
    public const int FirstVolumeCount = 3;
}
