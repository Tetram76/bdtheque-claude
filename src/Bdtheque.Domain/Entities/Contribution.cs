using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A single role credited to an author (Contribution), belonging either to a specific
/// <see cref="Entities.Album"/> (a real credit) or to a <see cref="Entities.Series"/> (a
/// template credit given to an album attached to the series, for each role where it has no author yet) —
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
    /// Creates a template contribution on a series, given to an album attached to the series
    /// for each role where it has no author yet.
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

    /// <summary>
    /// Passes the template contribution to <paramref name="series"/>, with its identity, as the merge of two series does when the
    /// series that remains retains it of the other one (<see cref="Series.MergeTemplateContributions"/>).
    /// </summary>
    internal void MoveTo(Series series)
    {
        Series = series;
        SeriesId = series.Id;
    }

    /// <summary>
    /// Passes the contribution to <paramref name="album"/>, with its identity, as the merge of two albums does when the album that
    /// remains retains it of the other one (<see cref="Album.MergeContributions"/>).
    /// </summary>
    internal void MoveTo(Album album)
    {
        Album = album;
        AlbumId = album.Id;
    }

    /// <summary>
    /// The credits two merged records both hold (fonctionnel/regles-metier/fusion-des-fiches.md, duplicates of a list): each
    /// contribution of <paramref name="other"/> whose author and role a contribution of <paramref name="held"/> credits, with
    /// that one, which replaces it.
    /// </summary>
    internal static IReadOnlyList<(Guid Removed, Guid Kept)> HeldOf(List<Contribution> held, IEnumerable<Contribution> other) =>
    [
        .. other
            .Select(c => (Removed: c.Id, Kept: held.Find(h => h.AuthorId == c.AuthorId && h.Role == c.Role)?.Id))
            .Where(c => c.Kept is not null)
            .Select(c => (c.Removed, c.Kept!.Value)),
    ];

    /// <summary>
    /// Keeps, of the contributions of the record that remains (<paramref name="kept"/>) and of the other one
    /// (<paramref name="merged"/>), those the merge retains: one of the other record retained passes to the one that remains with
    /// its identity, which a saved search may designate (<paramref name="moveTo"/>); one discarded stays with the other record,
    /// deleted with it; one of the record that remains discarded is removed. A credit both hold, the same value on both sides, is
    /// kept once, by the contribution of the record that remains, whatever <paramref name="retained"/> says of it.
    /// </summary>
    /// <param name="retained">The contributions retained, of either record.</param>
    /// <exception cref="ArgumentException">A contribution retained of neither record; nothing is changed.</exception>
    internal static void Merge(List<Contribution> kept, List<Contribution> merged, IReadOnlyCollection<Guid> retained, Action<Contribution> moveTo)
    {
        ArgumentNullException.ThrowIfNull(retained);
        var held = HeldOf(kept, merged);
        var foreign = retained.Where(id => !kept.Exists(c => c.Id == id) && !merged.Exists(c => c.Id == id)).ToList();
        if (foreign.Count > 0)
            throw new ArgumentException($"The contributions {string.Join(", ", foreign)} are of neither merged record.", nameof(retained));

        kept.RemoveAll(c => !retained.Contains(c.Id) && !held.Any(h => h.Kept == c.Id));
        foreach (var other in merged.Where(c => retained.Contains(c.Id) && !held.Any(h => h.Removed == c.Id)).ToList())
        {
            merged.Remove(other);
            moveTo(other);
            kept.Add(other);
        }
    }

    /// <summary>
    /// Credits <paramref name="kept"/> instead of <paramref name="merged"/> in every role the latter holds, as the merge of two
    /// authors does (fonctionnel/regles-metier/fusion-des-fiches.md): each contribution keeps its identity, which a saved search may
    /// designate, and a role both of them hold is credited once, by the contribution of <paramref name="kept"/>.
    /// </summary>
    /// <returns>Each contribution removed as a duplicate, with the one that replaces it.</returns>
    /// <exception cref="ArgumentException">The same author twice.</exception>
    internal static IReadOnlyList<(Guid Removed, Guid Kept)> Transfer(List<Contribution> contributions, Author merged, Author kept)
    {
        MergeGuard.EnsureDistinct(merged, kept);

        var replaced = new List<(Guid Removed, Guid Kept)>();
        foreach (var contribution in contributions.Where(c => c.AuthorId == merged.Id).ToList())
        {
            if (contributions.Find(c => c.AuthorId == kept.Id && c.Role == contribution.Role) is { } held)
            {
                contributions.Remove(contribution);
                replaced.Add((contribution.Id, held.Id));
            }
            else
            {
                contribution.Author = kept;
                contribution.AuthorId = kept.Id;
            }
        }

        return replaced;
    }
}
