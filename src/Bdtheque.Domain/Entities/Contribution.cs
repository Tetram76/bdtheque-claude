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

    // Both factories are internal: a contribution belongs to its owner, which alone creates it
    // (Album.SetTitleSeriesAndContributions, Series.SetTemplateContributions) and so sees every
    // credit it holds.

    /// <summary>Creates a real contribution credited on a specific album.</summary>
    internal static Contribution ForAlbum(Album album, Author author, ContributionRole role)
    {
        ArgumentNullException.ThrowIfNull(album);
        return new Contribution(album, null, author, role);
    }

    /// <summary>
    /// Creates a template contribution on a series, copied onto an album attached to the series
    /// when it has no contribution of its own yet.
    /// </summary>
    internal static Contribution ForSeriesTemplate(Series series, Author author, ContributionRole role)
    {
        ArgumentNullException.ThrowIfNull(series);
        return new Contribution(null, series, author, role);
    }

    /// <summary>
    /// Replaces the contributions of an owner by <paramref name="credits"/>, as its form sends them
    /// whole: a contribution already credited is kept as it is, so that saving unchanged credits writes
    /// none of them. Nothing is changed if any credit is refused.
    /// </summary>
    /// <param name="create">Creates a contribution of the owner, for a credit it does not hold yet.</param>
    internal static void Replace(
        List<Contribution> contributions, IReadOnlyCollection<(Author Author, ContributionRole Role)> credits,
        Func<Author, ContributionRole, Contribution> create)
    {
        ArgumentNullException.ThrowIfNull(credits);
        foreach (var (author, role) in credits)
        {
            ArgumentNullException.ThrowIfNull(author, nameof(credits));
            EnumGuard.EnsureDefined(role, nameof(credits));
        }

        // Same comparison as the partial unique indexes of the contributions: reported here so that
        // the mistake is caught before the database.
        var wanted = credits.Select(c => (AuthorId: c.Author.Id, c.Role)).ToList();
        if (wanted.Count != wanted.Distinct().Count())
            throw new DomainRuleViolationException(
                DomainRules.ContributionAlreadyCredited, "An author cannot be credited twice with the same role.");

        contributions.RemoveAll(c => !wanted.Contains((c.AuthorId, c.Role)));
        foreach (var (author, role) in credits.Where(w => !contributions.Exists(c => c.AuthorId == w.Author.Id && c.Role == w.Role)))
            contributions.Add(create(author, role));
    }
}
