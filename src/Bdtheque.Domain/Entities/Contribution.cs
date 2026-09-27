using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A single role credited to an author (Contribution), belonging either to a specific
/// <see cref="Entities.Album"/> (a real credit) or to a <see cref="Entities.Series"/> (a
/// template credit copied onto an album attached to the series when it has none yet) —
/// never both, never neither.
/// </summary>
public sealed class Contribution : EntityBase
{
    public Guid? AlbumId { get; private set; }
    public Album? Album { get; private set; }

    public Guid? SeriesId { get; private set; }
    public Series? Series { get; private set; }

    public Guid AuthorId { get; private set; }
    public Author Author { get; private set; } = null!;

    public ContributionRole Role { get; private set; }

    // EF Core parameterless constructor
    private Contribution() { }

    // Private: only reachable through ForAlbum/ForSeriesTemplate below, which each pass exactly
    // one non-null owner — so the album/series exclusivity has nothing left to validate here.
    private Contribution(Album? album, Series? series, Author author, ContributionRole role)
    {
        ArgumentNullException.ThrowIfNull(author);
        EnumGuard.EnsureDefined(role, nameof(role));

        Album = album;
        AlbumId = album?.Id;
        Series = series;
        SeriesId = series?.Id;
        Author = author;
        AuthorId = author.Id;
        Role = role;
    }

    /// <summary>Creates a real contribution credited on a specific album.</summary>
    public static Contribution ForAlbum(Album album, Author author, ContributionRole role)
    {
        ArgumentNullException.ThrowIfNull(album);
        return new Contribution(album, null, author, role);
    }

    /// <summary>
    /// Creates a template contribution on a series, copied onto an album attached to the series
    /// when it has no contribution of its own yet.
    /// </summary>
    public static Contribution ForSeriesTemplate(Series series, Author author, ContributionRole role)
    {
        ArgumentNullException.ThrowIfNull(series);
        return new Contribution(null, series, author, role);
    }

    public void SetRole(ContributionRole role)
    {
        EnumGuard.EnsureDefined(role, nameof(role));
        Role = role;
    }
}
