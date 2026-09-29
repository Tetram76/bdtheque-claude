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

    // Read-only from outside: AddGenre/AddUniverse keep each association unique.
    private readonly List<Genre> _genres = [];
    public IReadOnlyCollection<Genre> Genres => _genres;
    private readonly List<Universe> _universes = [];
    public IReadOnlyCollection<Universe> Universes => _universes;

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
            throw new DomainRuleViolationException(
                DomainRules.AlbumTitleRequiredWithoutSeries, "An album must have a title when it is not attached to a series.");
    }

    public void SetTitle(string? title)
    {
        var normalized = DomainText.NullIfBlank(title);
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
            throw new DomainRuleViolationException(
                DomainRules.AlbumManualSortKeyRequiresTitle, "Cannot set a manual sort key on an album with no title.");
        SortKey = DomainText.Required(sortKey, DomainRules.AlbumSortKeyRequired, "A manual sort key must not be blank.");
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
            throw new DomainRuleViolationException(
                DomainRules.AlbumVolumeRangeOmnibusOnly,
                "Cannot change the type away from Omnibus while a start/end volume range is set. " +
                "Clear the range first with SetVolumeRange(null, null).");
        Type = type;
    }

    public void SetSpecialIssue(bool isSpecialIssue) => IsSpecialIssue = isSpecialIssue;

    public void SetVolumeNumber(int? number)
    {
        if (number is <= 0)
            throw new DomainRuleViolationException(DomainRules.AlbumVolumeNumberPositive, "Volume number must be positive when specified.");
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
            throw new DomainRuleViolationException(
                DomainRules.AlbumVolumeRangeBothOrNeither, "Start and end volume numbers must be provided together, or not at all.");

        if (start is not null)
        {
            if (Type != AlbumType.Omnibus)
                throw new DomainRuleViolationException(
                    DomainRules.AlbumVolumeRangeOmnibusOnly, "A volume range is only applicable to omnibus (Intégrale) albums.");
            if (start <= 0)
                throw new DomainRuleViolationException(DomainRules.AlbumVolumeRangeStartPositive, "Start volume number must be positive.");
            if (start > end)
                throw new DomainRuleViolationException(
                    DomainRules.AlbumVolumeRangeOrder, "Start volume number must not exceed the end volume number.");
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
            throw new DomainRuleViolationException(
                DomainRules.AlbumPublicationMonthRequiresYear, "A publication month requires a publication year.");
        if (month is < 1 or > 12)
            throw new DomainRuleViolationException(
                DomainRules.AlbumPublicationMonthRange, "Publication month must be between 1 and 12.");
        if (year is <= 0)
            throw new DomainRuleViolationException(DomainRules.AlbumPublicationYearPositive, "Publication year must be positive.");

        FirstPublicationYear = year;
        FirstPublicationMonth = month;
    }

    public void SetSummary(string? summary) => Summary = DomainText.NullIfBlank(summary);

    public void SetPersonalNotes(string? notes) => PersonalNotes = DomainText.NullIfBlank(notes);

    public void AddGenre(Genre genre) => _genres.AddOnce(genre);

    public void RemoveGenre(Genre genre) => _genres.RemoveById(genre);

    public void AddUniverse(Universe universe) => _universes.AddOnce(universe);

    public void RemoveUniverse(Universe universe) => _universes.RemoveById(universe);

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
        EnsureNotTargetedAsWhole();
        if (_purchaseIntents.Count > 0)
            throw new DomainRuleViolationException(
                DomainRules.PurchaseIntentEditionsAlreadyTargeted,
                "An intent on the whole album excludes the intents already recorded on its editions.");

        return AddIntent(null);
    }

    /// <summary>
    /// Records an intent to acquire one specific edition of this album. Several editions of the
    /// same album may each be targeted, but never while the album itself is
    /// (modele-metier.md § Intention d'achat).
    /// </summary>
    public PurchaseIntent AddPurchaseIntent(Edition edition)
    {
        EnsureOwnEdition(edition);
        // An edition already bought cannot become an intent again: a second copy is recorded as
        // a new edition of the album, which then carries the intent (fonctionnel.md § Intention
        // d'achat).
        if (edition.AcquisitionMode is not null)
            throw new DomainRuleViolationException(
                DomainRules.PurchaseIntentEditionAlreadyOwned, "An edition already owned cannot be targeted by a purchase intent.");
        EnsureNotTargetedAsWhole();
        if (_purchaseIntents.Any(p => p.EditionId == edition.Id))
            throw new DomainRuleViolationException(
                DomainRules.PurchaseIntentEditionAlreadyTargeted, "This edition is already targeted by a purchase intent.");

        return AddIntent(edition);
    }

    /// <summary>
    /// Confirms the purchase of one of this album's editions (fonctionnel.md § Intention d'achat
    /// › Réalisation d'une intention): the intent it realizes — the one on this edition, or else
    /// the one on the whole album — is removed, and the edition becomes owned. Other editions'
    /// intents are kept. This is the only way to acquire an edition targeted by an intent.
    /// </summary>
    public void ConfirmPurchase(Edition edition, AcquisitionMode mode)
    {
        // Every check runs before anything is mutated: a rejected confirmation must leave the
        // aggregate untouched, or a later save would delete an intent never actually realized.
        EnsureOwnEdition(edition);
        EnumGuard.EnsureDefined(mode, nameof(mode));
        if (edition.AcquisitionMode is not null)
            throw new DomainRuleViolationException(DomainRules.EditionAlreadyOwned, "This edition is already owned.");

        var realized = _purchaseIntents.Find(p => p.EditionId == edition.Id)
                       ?? _purchaseIntents.Find(p => p.EditionId is null);
        if (realized is not null)
        {
            _purchaseIntents.Remove(realized);
            realized.Edition?.SetPurchaseIntent(null);
        }

        edition.SetAcquisitionMode(mode);
    }

    // A programming error, not a business one: only this album's own editions are ever offered
    // for its intents and purchases, so no user input can reach this with a foreign edition.
    private void EnsureOwnEdition(Edition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);
        if (edition.AlbumId != Id)
            throw new ArgumentException("The edition does not belong to this album.", nameof(edition));
    }

    private void EnsureNotTargetedAsWhole()
    {
        if (_purchaseIntents.Any(p => p.EditionId is null))
            throw new DomainRuleViolationException(
                DomainRules.PurchaseIntentAlbumAlreadyTargeted, "This album is already targeted as a whole by a purchase intent.");
    }

    private PurchaseIntent AddIntent(Edition? edition)
    {
        var intent = new PurchaseIntent(this, edition);
        _purchaseIntents.Add(intent);
        edition?.SetPurchaseIntent(intent);
        return intent;
    }
}
