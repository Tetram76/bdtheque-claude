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

    public Contribution(Album? album, Series? series, Author author, ContributionRole role)
    {
        EnsureExactlyOneOwner(album, series);
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

    public void SetRole(ContributionRole role)
    {
        EnumGuard.EnsureDefined(role, nameof(role));
        Role = role;
    }

    private static void EnsureExactlyOneOwner(Album? album, Series? series)
    {
        if ((album is null) == (series is null))
            throw new ArgumentException("A contribution must belong to exactly one of an album or a series.");
    }
}
