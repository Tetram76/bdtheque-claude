using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A work of comics tracked in the catalogue (Album), either standalone or attached to a
/// <see cref="Entities.Series"/>.
/// </summary>
public sealed class Album : EntityBase, IAggregateRoot
{
    public string? Title { get; private set; }
    public string? SortKey { get; private set; }
    public bool IsManualSortKey { get; private set; }

    // Follows the album's own sort key only: for an album with no title, the series' entry is
    // substituted at read time, never copied here, where it would go stale whenever the series
    // changes (choix-implementation.md § Navigation par initiale : mise en œuvre).
    public string? NavigationEntry { get; private set; }

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

    // Read-only from outside: an edition registers itself here on construction, and can never be
    // moved to another album. The album's first publication date is a reference date of their
    // amounts, which SetFirstPublicationDate checks against this collection: the persistence layer
    // always loads it with the album, for the same reason as the intents.
    private readonly List<Edition> _editions = [];
    public IReadOnlyCollection<Edition> Editions => _editions;

    // Read-only from outside: only the album's own operations credit an author. The copy of the
    // series' contributions depends on the album having none, which the persistence layer
    // guarantees by always loading this collection with the album.
    private readonly List<Contribution> _contributions = [];
    public IReadOnlyCollection<Contribution> Contributions => _contributions;

    // EF Core parameterless constructor
    private Album() { }

    /// <summary>
    /// Creates an album, attached to <paramref name="series"/> if any: having no contribution yet,
    /// it receives those of the series template (fonctionnel.md § Initialisation des contributions
    /// depuis la série).
    /// </summary>
    public Album(string? title, Series? series) : this(title, series, []) { }

    /// <summary>
    /// Creates an album credited with <paramref name="contributions"/>; without any, an album
    /// attached to a series receives those of the series template.
    /// </summary>
    public Album(string? title, Series? series, IReadOnlyCollection<(Author Author, ContributionRole Role)> contributions)
    {
        SetTitleSeriesAndContributions(title, series, contributions);
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

    /// <summary>
    /// Sets the title, the series and the contributions together, as the form sends them: each
    /// depends on another — the title is required without a series, and attaching an album with no
    /// contribution copies those of the series — so that separate setters would succeed or fail
    /// depending on the order they are called in. Nothing is changed if any of them is refused.
    /// </summary>
    /// <param name="contributions">
    /// The album's contributions, replacing the current ones; a contribution already credited is
    /// kept as it is. Empty while the album is attached to another series than its current one,
    /// they are copied from the template of that series.
    /// </param>
    public void SetTitleSeriesAndContributions(
        string? title, Series? series, IReadOnlyCollection<(Author Author, ContributionRole Role)> contributions)
    {
        var normalizedTitle = DomainText.NullIfBlank(title);
        EnsureTitleOrSeries(normalizedTitle, series);

        Contribution.Replace(_contributions, contributions, (author, role) => Contribution.ForAlbum(this, author, role));
        AttachSeries(series);
        ApplyTitle(normalizedTitle);
    }

    private void ApplyTitle(string? title)
    {
        Title = title;

        if (Title is null)
        {
            // Absent title implies an absent sort key (modele-metier.md § Album): the caller
            // falls back to the attached series' own sort key for sorting/navigation instead.
            ApplySortKey(null);
            IsManualSortKey = false;
        }
        else if (!IsManualSortKey)
        {
            ApplySortKey(TitleSortKeyCalculator.Compute(Title));
        }
    }

    // Attaching to another series than the current one, while the album has no contribution, gives
    // it the contributions of the series template as a starting point; staying in the same series
    // copies nothing, so that contributions removed since never come back.
    private void AttachSeries(Series? series)
    {
        var attaching = series is not null && series.Id != SeriesId;
        Series = series;
        SeriesId = series?.Id;
        if (!attaching || _contributions.Count > 0)
            return;

        // The template's authors must be loaded: the copy credits the same authors.
        foreach (var template in series!.TemplateContributions)
            _contributions.Add(Contribution.ForAlbum(this, template.Author, template.Role));
    }

    public void SetSortKey(string sortKey)
    {
        if (Title is null)
            throw new DomainRuleViolationException(
                DomainRules.AlbumManualSortKeyRequiresTitle, "Cannot set a manual sort key on an album with no title.");
        ApplySortKey(DomainText.Required(sortKey, DomainRules.AlbumSortKeyRequired, "A manual sort key must not be blank."));
        IsManualSortKey = true;
    }

    /// <summary>Recomputes the sort key from the current title and returns to automatic mode.</summary>
    public void ResetSortKey()
    {
        IsManualSortKey = false;
        ApplySortKey(Title is null ? null : TitleSortKeyCalculator.Compute(Title));
    }

    // Single write path of the sort key: the stored navigation entry can never lag behind it.
    private void ApplySortKey(string? sortKey)
    {
        SortKey = sortKey;
        NavigationEntry = sortKey is null ? null : NavigationEntryCalculator.Compute(sortKey);
    }

    public void SetSpecialIssue(bool isSpecialIssue) => IsSpecialIssue = isSpecialIssue;

    public void SetVolumeNumber(int? number)
    {
        if (number is <= 0)
            throw new DomainRuleViolationException(DomainRules.AlbumVolumeNumberPositive, "Volume number must be positive when specified.");
        VolumeNumber = number;
    }

    /// <summary>
    /// Sets the type and the start/end volume range covered by an omnibus, together: the range is
    /// only meaningful for <see cref="AlbumType.Omnibus"/> albums, so that changing one without the
    /// other would be refused in one order or the other. Both bounds must be provided together or
    /// not at all (modele-metier.md § Album).
    /// </summary>
    public void SetTypeAndVolumeRange(AlbumType type, int? start, int? end)
    {
        EnumGuard.EnsureDefined(type, nameof(type));
        if ((start is null) != (end is null))
            throw new DomainRuleViolationException(
                DomainRules.AlbumVolumeRangeBothOrNeither, "Start and end volume numbers must be provided together, or not at all.");

        if (start is not null)
        {
            if (type != AlbumType.Omnibus)
                throw new DomainRuleViolationException(
                    DomainRules.AlbumVolumeRangeOmnibusOnly, "A volume range is only applicable to omnibus (Intégrale) albums.");
            if (start <= 0)
                throw new DomainRuleViolationException(DomainRules.AlbumVolumeRangeStartPositive, "Start volume number must be positive.");
            if (start > end)
                throw new DomainRuleViolationException(
                    DomainRules.AlbumVolumeRangeOrder, "Start volume number must not exceed the end volume number.");
        }

        Type = type;
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
        if (year is null)
        {
            foreach (var edition in _editions)
                edition.EnsureAmountsDatedWithoutAlbumDate();
        }

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
        EnsureTargetable(edition);
        EnsureNotTargetedAsWhole();

        return AddIntent(edition);
    }

    /// <summary>
    /// Removes an intent of this album. An intent on an edition takes the edition along: an edition
    /// not owned and its intent are inseparable (fonctionnel.md § Intention d'achat).
    /// </summary>
    public void RemovePurchaseIntent(PurchaseIntent intent)
    {
        EnsureOwnIntent(intent);
        _purchaseIntents.Remove(intent);
        if (intent.EditionId is { } editionId)
            _editions.RemoveAll(e => e.Id == editionId);
    }

    /// <summary>
    /// Converts the intent on the whole album into an intent on <paramref name="edition"/>, an edition
    /// of the album neither owned nor targeted yet (fonctionnel.md § Intention d'achat).
    /// </summary>
    /// <exception cref="ArgumentException">The intent does not target the whole album.</exception>
    public PurchaseIntent ConvertPurchaseIntentToEdition(PurchaseIntent intent, Edition edition)
    {
        EnsureOwnIntent(intent);
        // The client only offers this conversion for an intent on the whole album.
        if (intent.EditionId is not null)
            throw new ArgumentException("Only an intent on the whole album converts into an intent on an edition.", nameof(intent));
        EnsureTargetable(edition);

        _purchaseIntents.Remove(intent);
        return AddIntent(edition);
    }

    /// <summary>
    /// Converts an intent on an edition into an intent on the whole album, which removes the edition
    /// targeted, not owned (fonctionnel.md § Intention d'achat). Refused while other editions of the
    /// album are targeted: an intent on the whole album excludes them.
    /// </summary>
    /// <exception cref="ArgumentException">The intent already targets the whole album.</exception>
    public PurchaseIntent ConvertPurchaseIntentToAlbum(PurchaseIntent intent)
    {
        EnsureOwnIntent(intent);
        // The client only offers this conversion for an intent on an edition.
        if (intent.EditionId is null)
            throw new ArgumentException("Only an intent on an edition converts into an intent on the whole album.", nameof(intent));
        if (_purchaseIntents.Count > 1)
            throw new DomainRuleViolationException(
                DomainRules.PurchaseIntentEditionsAlreadyTargeted,
                "An intent on the whole album excludes the intents recorded on the other editions.");

        RemovePurchaseIntent(intent);
        return AddIntent(null);
    }

    /// <summary>
    /// Records the acquisition of one of this album's editions — the only way for an edition to
    /// become owned, since only this aggregate sees every intent it may realize (fonctionnel.md §
    /// Intention d'achat › Réalisation d'une intention): the intent on this edition, or else the
    /// one on the whole album, is removed; other editions' intents are kept.
    /// </summary>
    /// <param name="acquisition">
    /// The acquisition and value of the edition, whose mode is required: entering an edition
    /// requires it, only an intent creating an edition not owned (fonctionnel.md § Appartenance à la
    /// collection).
    /// </param>
    public void RecordAcquisition(Edition edition, EditionAcquisition acquisition)
    {
        // Every check runs before anything is mutated: a rejected acquisition must leave the
        // aggregate untouched, or a later save would delete an intent never actually realized.
        EnsureOwnEdition(edition);
        ArgumentNullException.ThrowIfNull(acquisition);
        if (acquisition.Mode is null)
            throw new DomainRuleViolationException(DomainRules.EditionAcquisitionModeRequired, "An acquisition requires its mode.");
        if (edition.AcquisitionMode is not null)
            throw new DomainRuleViolationException(DomainRules.EditionAlreadyOwned, "This edition is already owned.");
        edition.EnsureValid(acquisition, edition.PublicationYear);

        var realized = _purchaseIntents.Find(p => p.EditionId == edition.Id)
                       ?? _purchaseIntents.Find(p => p.EditionId is null);
        if (realized is not null)
            _purchaseIntents.Remove(realized);

        edition.Apply(acquisition);
    }

    // A programming error, not a business one: only this album's own editions are ever offered
    // for its intents and purchases, so no user input can reach this with a foreign edition.
    private void EnsureOwnEdition(Edition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);
        if (edition.AlbumId != Id)
            throw new ArgumentException("The edition does not belong to this album.", nameof(edition));
    }

    // A programming error, not a business one: the client only sends back the intents it read on
    // this album.
    private void EnsureOwnIntent(PurchaseIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (!_purchaseIntents.Contains(intent))
            throw new ArgumentException("The intent does not belong to this album.", nameof(intent));
    }

    private void EnsureTargetable(Edition edition)
    {
        EnsureOwnEdition(edition);
        // An edition already bought cannot become an intent again: a second copy is recorded as
        // a new edition of the album, which then carries the intent (fonctionnel.md § Intention
        // d'achat).
        if (edition.AcquisitionMode is not null)
            throw new DomainRuleViolationException(
                DomainRules.PurchaseIntentEditionAlreadyOwned, "An edition already owned cannot be targeted by a purchase intent.");
        if (_purchaseIntents.Any(p => p.EditionId == edition.Id))
            throw new DomainRuleViolationException(
                DomainRules.PurchaseIntentEditionAlreadyTargeted, "This edition is already targeted by a purchase intent.");
    }

    // Only reachable from the Edition constructor, the single way to create an edition of this album.
    internal void Register(Edition edition) => _editions.Add(edition);

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
        return intent;
    }
}
