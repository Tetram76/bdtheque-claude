using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A work of comics tracked in the catalogue (Album), either standalone or attached to a
/// <see cref="Entities.Series"/>.
/// </summary>
public sealed class Album : EntityBase
{
    public string? Title { get; private set; }
    public string? SortKey { get; private set; }
    public bool IsManualSortKey { get; private set; }

    public Guid? SeriesId { get; private set; }
    public Series? Series { get; private set; }

    public AlbumType Type { get; private set; } = AlbumType.Regular;
    public bool IsSpecialIssue { get; private set; }

    public int? VolumeNumber { get; private set; }
    public int? StartVolumeNumber { get; private set; }
    public int? EndVolumeNumber { get; private set; }

    public int? FirstPublicationYear { get; private set; }
    public int? FirstPublicationMonth { get; private set; }

    public string? Summary { get; private set; }
    public string? PersonalNotes { get; private set; }
    public AlbumRating? Rating { get; private set; }

    public ICollection<Genre> Genres { get; private set; } = [];
    public ICollection<Universe> Universes { get; private set; } = [];

    // Read-only from outside: every intent concerning this album goes through AddPurchaseIntent,
    // the single place that can see them all and enforce the per-album rules. The persistence
    // layer always loads this collection with the album, so the rules never run against a
    // partially loaded aggregate.
    private readonly List<PurchaseIntent> _purchaseIntents = [];
    public IReadOnlyCollection<PurchaseIntent> PurchaseIntents => _purchaseIntents;

    // EF Core parameterless constructor
    private Album() { }

    public Album(string? title, Series? series)
    {
        EnsureTitleOrSeries(title, series);
        Series = series;
        SeriesId = series?.Id;
        SetTitle(title);
    }

    /// <summary>
    /// A title is required unless the album is attached to a series (the series and volume
    /// number can then suffice to identify it) — see modele-metier.md § Album.
    /// </summary>
    private static void EnsureTitleOrSeries(string? title, Series? series)
    {
        if (string.IsNullOrWhiteSpace(title) && series is null)
            throw new ArgumentException("An album must have a title when it is not attached to a series.");
    }

    public void SetTitle(string? title)
    {
        var normalized = NullIfEmpty(title);
        EnsureTitleOrSeries(normalized, Series);
        Title = normalized;

        if (Title is null)
        {
            // Absent title implies an absent sort key (modele-metier.md § Album): the caller
            // falls back to the attached series' own sort key for sorting/navigation instead.
            SortKey = null;
            IsManualSortKey = false;
        }
        else if (!IsManualSortKey)
        {
            SortKey = TitleSortKeyCalculator.Compute(Title);
        }
    }

    public void SetSeries(Series? series)
    {
        EnsureTitleOrSeries(Title, series);
        Series = series;
        SeriesId = series?.Id;
    }

    public void SetSortKey(string sortKey)
    {
        if (Title is null)
            throw new InvalidOperationException("Cannot set a manual sort key on an album with no title.");
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);
        SortKey = sortKey.Trim();
        IsManualSortKey = true;
    }

    /// <summary>Recomputes the sort key from the current title and returns to automatic mode.</summary>
    public void ResetSortKey()
    {
        IsManualSortKey = false;
        SortKey = Title is null ? null : TitleSortKeyCalculator.Compute(Title);
    }

    public void SetType(AlbumType type)
    {
        EnumGuard.EnsureDefined(type, nameof(type));
        if (type != AlbumType.Omnibus && (StartVolumeNumber is not null || EndVolumeNumber is not null))
            throw new InvalidOperationException(
                "Cannot change the type away from Omnibus while a start/end volume range is set. " +
                "Clear the range first with SetVolumeRange(null, null).");
        Type = type;
    }

    public void SetSpecialIssue(bool isSpecialIssue) => IsSpecialIssue = isSpecialIssue;

    public void SetVolumeNumber(int? number)
    {
        if (number is <= 0)
            throw new ArgumentOutOfRangeException(nameof(number), number, "Volume number must be positive when specified.");
        VolumeNumber = number;
    }

    /// <summary>
    /// Sets the start/end volume range covered by an omnibus edition. Both values must be
    /// provided together or not at all, and the range is only meaningful for
    /// <see cref="AlbumType.Omnibus"/> albums (modele-metier.md § Album).
    /// </summary>
    public void SetVolumeRange(int? start, int? end)
    {
        if ((start is null) != (end is null))
            throw new ArgumentException("Start and end volume numbers must be provided together, or not at all.");

        if (start is not null)
        {
            if (Type != AlbumType.Omnibus)
                throw new InvalidOperationException("A volume range is only applicable to omnibus (Intégrale) albums.");
            if (start <= 0)
                throw new ArgumentOutOfRangeException(nameof(start), start, "Start volume number must be positive.");
            if (start > end)
                throw new ArgumentException("Start volume number must not exceed the end volume number.", nameof(start));
        }

        StartVolumeNumber = start;
        EndVolumeNumber = end;
    }

    /// <summary>
    /// Sets the first publication date at year, or year + month, granularity — never a full
    /// date (modele-metier.md § Album).
    /// </summary>
    public void SetFirstPublicationDate(int? year, int? month)
    {
        if (month is not null && year is null)
            throw new ArgumentException("A publication month requires a publication year.", nameof(month));
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), month, "Publication month must be between 1 and 12.");
        if (year is <= 0)
            throw new ArgumentOutOfRangeException(nameof(year), year, "Publication year must be positive.");

        FirstPublicationYear = year;
        FirstPublicationMonth = month;
    }

    public void SetSummary(string? summary) => Summary = NullIfEmpty(summary);

    public void SetPersonalNotes(string? notes) => PersonalNotes = NullIfEmpty(notes);

    public void SetRating(AlbumRating? rating)
    {
        if (rating is not null)
            EnumGuard.EnsureDefined(rating.Value, nameof(rating));
        Rating = rating;
    }

    /// <summary>
    /// Records an intent to acquire this album in any edition. Refused if the album already has
    /// an intent of any kind: a whole-album intent excludes intents on its editions
    /// (modele-metier.md § Intention d'achat).
    /// </summary>
    public PurchaseIntent AddPurchaseIntent()
    {
        if (_purchaseIntents.Count > 0)
            throw new InvalidOperationException("This album is already targeted by a purchase intent, on itself or on one of its editions.");

        return AddIntent(null);
    }

    /// <summary>
    /// Records an intent to acquire one specific edition of this album. Several editions of the
    /// same album may each be targeted, but never while the album itself is
    /// (modele-metier.md § Intention d'achat).
    /// </summary>
    public PurchaseIntent AddPurchaseIntent(Edition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);
        if (edition.AlbumId != Id)
            throw new ArgumentException("The edition does not belong to this album.", nameof(edition));
        if (_purchaseIntents.Any(p => p.EditionId is null))
            throw new InvalidOperationException("This album is already targeted as a whole by a purchase intent.");
        if (_purchaseIntents.Any(p => p.EditionId == edition.Id))
            throw new InvalidOperationException("This edition is already targeted by a purchase intent.");

        return AddIntent(edition);
    }

    private PurchaseIntent AddIntent(Edition? edition)
    {
        var intent = new PurchaseIntent(this, edition);
        _purchaseIntents.Add(intent);
        return intent;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
