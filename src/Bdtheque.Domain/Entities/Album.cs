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
    // changes (choix-implementation/api/navigation-par-initiale-mise-en-oeuvre.md).
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

    // Read-only from outside: an edition registers itself here on construction, and only moves to
    // another album by the merge of its own into it (TakeEditionsOf). The album's first publication date is a reference date of their
    // amounts, which SetFirstPublicationDate checks against this collection: the persistence layer
    // always loads it with the album, for the same reason as the intents.
    private readonly List<Edition> _editions = [];
    public IReadOnlyCollection<Edition> Editions => _editions;

    // Read-only from outside: only the album's own operations credit an author.
    private readonly List<Contribution> _contributions = [];
    public IReadOnlyCollection<Contribution> Contributions => _contributions;

    // EF Core parameterless constructor
    private Album() { }

    /// <summary>Creates an album without contributions, attached to <paramref name="series"/> if any.</summary>
    public Album(string? title, Series? series) : this(title, series, []) { }

    /// <summary>
    /// Creates an album credited with <paramref name="contributions"/>, and with them only: the authors a series gives it are
    /// taken by the form as soon as the series is chosen (<see cref="Entities.Series.ContributionsForAlbum"/>).
    /// </summary>
    public Album(string? title, Series? series, IReadOnlyCollection<(Author Author, ContributionRole Role)> contributions)
    {
        SetTitleSeriesAndContributions(title, series, contributions);
    }

    /// <summary>
    /// A title is required unless the album is attached to a series (the series and volume
    /// number can then suffice to identify it) — see modele-metier/album.md.
    /// </summary>
    private void EnsureTitleOrSeries(string? title, Series? series)
    {
        if (string.IsNullOrWhiteSpace(title) && series is null)
            throw new DomainRuleViolationException(
                DomainRules.AlbumTitleRequiredWithoutSeries, "An album must have a title when it is not attached to a series.", this, nameof(Title));
    }

    /// <summary>
    /// Sets the title, the series and the contributions together, as the form sends them: the title
    /// is required without a series, so that separate setters would succeed or fail depending on the
    /// order they are called in. Nothing is changed if any of them is refused.
    /// </summary>
    /// <param name="contributions">
    /// The album's contributions, replacing the current ones; a contribution already credited is
    /// kept as it is. Attaching the album to a series copies nothing: authors removed from the form
    /// after they were taken from the series do not come back.
    /// </param>
    public void SetTitleSeriesAndContributions(
        string? title, Series? series, IReadOnlyCollection<(Author Author, ContributionRole Role)> contributions)
    {
        var normalizedTitle = DomainText.NullIfBlank(title);
        EnsureTitleOrSeries(normalizedTitle, series);

        Contribution.ReplaceOfAlbum(this, _contributions, contributions);
        Series = series;
        SeriesId = series?.Id;
        ApplyTitle(normalizedTitle);
    }

    private void ApplyTitle(string? title)
    {
        Title = title;

        if (Title is null)
        {
            // Absent title implies an absent sort key (modele-metier/album.md): the caller
            // falls back to the attached series' own sort key for sorting/navigation instead.
            ApplySortKey(null);
            IsManualSortKey = false;
        }
        else if (!IsManualSortKey)
        {
            ApplySortKey(TitleSortKeyCalculator.Compute(Title));
        }
    }

    public void SetSortKey(string sortKey)
    {
        if (Title is null)
            throw new DomainRuleViolationException(
                DomainRules.AlbumManualSortKeyRequiresTitle, "Cannot set a manual sort key on an album with no title.", this, nameof(SortKey));
        ApplySortKey(DomainText.Required(sortKey, DomainRules.AlbumSortKeyRequired, "A manual sort key must not be blank.", this, nameof(SortKey)));
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
            throw new DomainRuleViolationException(
                DomainRules.AlbumVolumeNumberPositive, "Volume number must be positive when specified.", this, nameof(VolumeNumber));
        VolumeNumber = number;
    }

    /// <summary>
    /// Sets the type and the start/end volume range covered by an omnibus, together: the range is
    /// only meaningful for <see cref="AlbumType.Omnibus"/> albums, so that changing one without the
    /// other would be refused in one order or the other. Both bounds must be provided together or
    /// not at all (modele-metier/album.md).
    /// </summary>
    public void SetTypeAndVolumeRange(AlbumType type, int? start, int? end)
    {
        EnumGuard.EnsureDefined(type, nameof(type));
        if ((start is null) != (end is null))
            throw new DomainRuleViolationException(
                DomainRules.AlbumVolumeRangeBothOrNeither, "Start and end volume numbers must be provided together, or not at all.", this,
                start is null ? nameof(StartVolumeNumber) : nameof(EndVolumeNumber));

        if (start is not null)
        {
            if (type != AlbumType.Omnibus)
                throw new DomainRuleViolationException(
                    DomainRules.AlbumVolumeRangeOmnibusOnly, "A volume range is only applicable to omnibus (Intégrale) albums.", this,
                    nameof(StartVolumeNumber));
            if (start <= 0)
                throw new DomainRuleViolationException(
                    DomainRules.AlbumVolumeRangeStartPositive, "Start volume number must be positive.", this, nameof(StartVolumeNumber));
            if (start > end)
                throw new DomainRuleViolationException(
                    DomainRules.AlbumVolumeRangeOrder, "Start volume number must not exceed the end volume number.", this, nameof(EndVolumeNumber));
        }

        Type = type;
        StartVolumeNumber = start;
        EndVolumeNumber = end;
    }

    /// <summary>
    /// Sets the first publication date at year, or year + month, granularity — never a full
    /// date (modele-metier/album.md).
    /// </summary>
    public void SetFirstPublicationDate(int? year, int? month)
    {
        if (month is not null && year is null)
            throw new DomainRuleViolationException(
                DomainRules.AlbumPublicationMonthRequiresYear, "A publication month requires a publication year.", this, nameof(FirstPublicationYear));
        if (month is < 1 or > 12)
            throw new DomainRuleViolationException(
                DomainRules.AlbumPublicationMonthRange, "Publication month must be between 1 and 12.", this, nameof(FirstPublicationMonth));
        if (year is <= 0)
            throw new DomainRuleViolationException(
                DomainRules.AlbumPublicationYearPositive, "Publication year must be positive.", this, nameof(FirstPublicationYear));
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

    /// <summary>
    /// Classes the album under <paramref name="kept"/> instead of <paramref name="merged"/>, as the merge of two genres does
    /// (fonctionnel/regles-metier/fusion-des-fiches.md); an album of both genres keeps <paramref name="kept"/> once.
    /// </summary>
    /// <exception cref="ArgumentException">The same genre twice.</exception>
    public void TransferGenre(Genre merged, Genre kept) => _genres.Transfer(merged, kept);

    /// <summary>
    /// Credits <paramref name="kept"/> instead of <paramref name="merged"/> on the album, in the same roles, as the merge of two
    /// authors does (fonctionnel/regles-metier/fusion-des-fiches.md); a role both of them hold is credited once.
    /// </summary>
    /// <returns>Each contribution removed as a duplicate, with the one that replaces it, which a saved search designating it designates instead.</returns>
    /// <exception cref="ArgumentException">The same author twice.</exception>
    public IReadOnlyList<(Guid Removed, Guid Kept)> TransferAuthor(Author merged, Author kept) =>
        Contribution.Transfer(_contributions, merged, kept);

    /// <summary>
    /// Publishes the editions of the album that designated what the merge of two publishers replaces by the publisher that
    /// remains, each in the collection that remains of its own (fonctionnel/regles-metier/fusion-des-fiches.md), editions
    /// targeted by a purchase intent included.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection of an edition concerned is not loaded.</exception>
    public void TransferPublisher(PublisherMerge merge)
    {
        foreach (var edition in _editions)
            edition.TransferPublisher(merge);
    }

    public void AddUniverse(Universe universe) => _universes.AddOnce(universe);

    public void RemoveUniverse(Universe universe) => _universes.RemoveById(universe);

    /// <summary>
    /// Attaches the album to <paramref name="kept"/> instead of <paramref name="merged"/>, as the merge of two series does
    /// (fonctionnel/regles-metier/fusion-des-fiches.md): its title, its contributions and everything else stay as they are.
    /// </summary>
    /// <exception cref="ArgumentException">The same series twice.</exception>
    public void TransferSeries(Series merged, Series kept)
    {
        MergeGuard.EnsureDistinct(merged, kept);
        if (SeriesId != merged.Id)
            return;
        Series = kept;
        SeriesId = kept.Id;
    }

    /// <summary>
    /// Attaches the album to <paramref name="kept"/> instead of <paramref name="merged"/>, as the merge of two universes does
    /// (fonctionnel/regles-metier/fusion-des-fiches.md); a album of both universes keeps <paramref name="kept"/> once.
    /// </summary>
    /// <exception cref="ArgumentException">The same universe twice.</exception>
    public void TransferUniverse(Universe merged, Universe kept) => _universes.Transfer(merged, kept);

    /// <summary>
    /// Each contribution of <paramref name="other"/> whose credit (author and role) this album holds too, with the contribution of
    /// this album that holds it: the merge of the two albums keeps such a credit once, by the contribution of the one that
    /// remains, which a saved search designating the other one designates instead (fonctionnel/regles-metier/fusion-des-fiches.md).
    /// </summary>
    /// <exception cref="ArgumentException">The same album twice.</exception>
    public IReadOnlyList<(Guid Removed, Guid Kept)> CreditsHeldOf(Album other)
    {
        MergeGuard.EnsureDistinct(other, this);
        return Contribution.HeldOf(_contributions, other._contributions);
    }

    /// <summary>
    /// Keeps, of the contributions of this album and of <paramref name="merged"/>, those the merge of the two albums retains
    /// (fonctionnel/regles-metier/fusion-des-fiches.md), this album being the one that remains: a contribution of the other album
    /// retained passes to this one with its identity, which a saved search may designate; one discarded stays with the other
    /// album, deleted with it; one of this album discarded is removed. A credit both albums hold is kept once, by the
    /// contribution of this album (<see cref="CreditsHeldOf"/>), whatever <paramref name="retained"/> says of it.
    /// </summary>
    /// <param name="retained">The contributions retained, of either album.</param>
    /// <exception cref="ArgumentException">The same album twice, or a contribution retained of neither album.</exception>
    public void MergeContributions(Album merged, IReadOnlyCollection<Guid> retained)
    {
        MergeGuard.EnsureDistinct(merged, this);
        Contribution.Merge(_contributions, merged._contributions, retained, c => c.MoveTo(this));
    }

    /// <summary>
    /// The intents on the whole album that the merge of <paramref name="first"/> and <paramref name="second"/> withdraws, whichever
    /// remains (fonctionnel/regles-metier/fusion-des-fiches.md): an album that has an edition, owned or targeted, can no longer be
    /// targeted as a whole, so that once either album has one, the intent on the whole other album goes. An intent both albums
    /// without edition hold is kept once, the same wish on both sides, and is not withdrawn.
    /// </summary>
    public static IReadOnlyList<PurchaseIntent> PurchaseIntentsWithdrawnByMerge(Album first, Album second)
    {
        return first._editions.Count + second._editions.Count == 0
            ? []
            : [.. first._purchaseIntents.Concat(second._purchaseIntents).Where(p => p.EditionId is null)];
    }

    /// <summary>
    /// Takes the editions of <paramref name="merged"/>, with their visuals and the intents targeting them, as the merge of two
    /// albums does, this album being the one that remains (fonctionnel/regles-metier/fusion-des-fiches.md): each keeps its identity,
    /// which a saved search, a visual or the history of the spotlight designates. The intents on the whole album then follow the
    /// rules of the album that remains (<see cref="PurchaseIntentsWithdrawnByMerge"/>): withdrawn once it has an edition, the one
    /// of the other album staying with it, deleted with it; otherwise one, its own, or else the one of the other album.
    /// </summary>
    /// <exception cref="ArgumentException">The same album twice.</exception>
    public void TakeEditionsOf(Album merged)
    {
        MergeGuard.EnsureDistinct(merged, this);
        var withdrawn = PurchaseIntentsWithdrawnByMerge(this, merged);

        foreach (var edition in merged._editions.ToList())
        {
            merged._editions.Remove(edition);
            edition.MoveTo(this);
            _editions.Add(edition);
        }

        var wholeAlbum = merged._purchaseIntents.Find(p => p.EditionId is null);
        var passing = merged._purchaseIntents.Where(p => p.EditionId is not null).ToList();
        if (wholeAlbum is not null && !withdrawn.Contains(wholeAlbum) && !_purchaseIntents.Exists(p => p.EditionId is null))
            passing.Add(wholeAlbum);
        foreach (var intent in passing)
        {
            merged._purchaseIntents.Remove(intent);
            intent.MoveTo(this);
            _purchaseIntents.Add(intent);
        }

        _purchaseIntents.RemoveAll(withdrawn.Contains);
    }

    /// <summary>
    /// The intents the merge of <paramref name="first"/> and <paramref name="second"/> withdraws, the acquisition of
    /// <paramref name="acquisitionOf"/> being kept (fonctionnel/regles-metier/fusion-des-fiches.md): an edition that remains owned
    /// cannot be targeted, so that the intents of both go; one that remains not owned keeps one, the same wish on both sides, and
    /// none is withdrawn.
    /// </summary>
    public static IReadOnlyList<PurchaseIntent> PurchaseIntentsWithdrawnByEditionMerge(Edition first, Edition second, Edition acquisitionOf)
    {
        return acquisitionOf.AcquisitionMode is null ? [] : [.. new[] { first, second }.Select(IntentOf).OfType<PurchaseIntent>()];
    }

    private static PurchaseIntent? IntentOf(Edition edition) => edition.Album._purchaseIntents.Find(p => p.EditionId == edition.Id);

    /// <summary>
    /// Merges <paramref name="merged"/> into <paramref name="kept"/>, the edition that remains
    /// (fonctionnel/regles-metier/fusion-des-fiches.md): it belongs to <paramref name="album"/>, the album of either edition, with
    /// its identity; it takes the edition year and the acquisition of <paramref name="acquisitionOf"/>, whole, its entry in the
    /// collection included (modele-metier/edition.md, contraintes d'intégrité); it keeps the visuals retained of both, those of the
    /// other edition passing to it with their identity, after its own of the same type. The other edition leaves its album, to be
    /// deleted, with the history of its spotlight passed to the edition that remains. Its intent follows the rules of the edition
    /// that remains (<see cref="PurchaseIntentsWithdrawnByEditionMerge"/>): withdrawn once it is owned, otherwise one, its own, or
    /// else that of the other edition. Every check runs before anything changes.
    /// </summary>
    /// <param name="retainedVisuals">The visuals retained, of either edition.</param>
    /// <returns>The visuals of both editions the merge deletes, whose files go once the merge is saved.</returns>
    /// <exception cref="ArgumentException">
    /// The same edition twice, an album or an acquisition of neither edition, or a visual retained of neither.
    /// </exception>
    public static IReadOnlyList<EditionVisual> MergeEditions(
        Edition kept, Edition merged, Album album, int? publicationYear, Edition acquisitionOf, IReadOnlyCollection<Guid> retainedVisuals)
    {
        MergeGuard.EnsureDistinct(merged, kept);
        if (album != kept.Album && album != merged.Album)
            throw new ArgumentException("The edition that remains belongs to the album of either edition.", nameof(album));
        if (acquisitionOf != kept && acquisitionOf != merged)
            throw new ArgumentException("The acquisition kept is that of either edition.", nameof(acquisitionOf));
        var visuals = kept.Visuals.Concat(merged.Visuals).ToList();
        if (!retainedVisuals.All(id => visuals.Exists(v => v.Id == id)))
            throw new ArgumentException("Each visual retained is one of either edition.", nameof(retainedVisuals));
        var acquisition = acquisitionOf.Acquisition;
        kept.EnsureValid(acquisition, publicationYear, album);

        var (keptIntent, mergedIntent) = (IntentOf(kept), IntentOf(merged));
        merged.Album._editions.Remove(merged);
        if (mergedIntent is not null)
            merged.Album._purchaseIntents.Remove(mergedIntent);
        if (keptIntent is not null)
            kept.Album._purchaseIntents.Remove(keptIntent);
        // The intent of the edition that remains: its own, or else that of the other edition, which then targets it.
        var intent = acquisition.Mode is null ? keptIntent ?? mergedIntent : null;
        intent?.Retarget(kept);

        kept.Album._editions.Remove(kept);
        kept.MoveTo(album);
        album._editions.Add(kept);
        if (intent is not null)
        {
            intent.MoveTo(album);
            album._purchaseIntents.Add(intent);
        }

        kept.TakeAcquisitionOf(acquisitionOf, publicationYear);
        merged.PassSpotlightsTo(kept);
        return kept.TakeVisualsOf(merged, retainedVisuals);
    }

    public void SetRating(AlbumRating? rating)
    {
        if (rating is not null)
            EnumGuard.EnsureDefined(rating.Value, nameof(rating));
        Rating = rating;
    }

    /// <summary>
    /// Records an intent to acquire this album in any edition. Refused if the album is already
    /// targeted as a whole, or has any edition, owned or targeted: an album targeted as a whole has
    /// no edition (modele-metier/intention-d-acquisition.md).
    /// </summary>
    public PurchaseIntent AddPurchaseIntent()
    {
        EnsureNotTargetedAsWhole();
        EnsureWithoutEditionBut(null);

        return AddIntent(null);
    }

    /// <summary>
    /// Records an intent to acquire one specific edition of this album. Several editions of the
    /// same album may each be targeted, but never while the album itself is
    /// (modele-metier/intention-d-acquisition.md).
    /// </summary>
    public PurchaseIntent AddPurchaseIntent(Edition edition)
    {
        EnsureTargetable(edition);
        EnsureNotTargetedAsWhole();

        return AddIntent(edition);
    }

    /// <summary>
    /// Removes an intent of this album. An intent on an edition takes the edition along: an edition
    /// not owned and its intent are inseparable (fonctionnel/regles-metier/intention-d-acquisition.md).
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
    /// of the album neither owned nor targeted yet (fonctionnel/regles-metier/intention-d-acquisition.md).
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
    /// targeted, not owned (fonctionnel/regles-metier/intention-d-acquisition.md). Refused unless this edition is the
    /// only one of the album: an album targeted as a whole has no edition.
    /// </summary>
    /// <exception cref="ArgumentException">The intent already targets the whole album.</exception>
    public PurchaseIntent ConvertPurchaseIntentToAlbum(PurchaseIntent intent)
    {
        EnsureOwnIntent(intent);
        // The client only offers this conversion for an intent on an edition.
        if (intent.EditionId is not { } editionId)
            throw new ArgumentException("Only an intent on an edition converts into an intent on the whole album.", nameof(intent));
        EnsureWithoutEditionBut(editionId);

        RemovePurchaseIntent(intent);
        return AddIntent(null);
    }

    /// <summary>
    /// Records the acquisition of one of this album's editions — the only way for an edition to
    /// become owned, since only this aggregate sees every intent it may realize
    /// (fonctionnel/regles-metier/intention-d-acquisition.md › Réalisation d'une intention): the intent on this
    /// edition, or else the
    /// one on the whole album, is removed; other editions' intents are kept.
    /// </summary>
    /// <param name="acquisition">
    /// The acquisition and value of the edition, whose mode is required: entering an edition
    /// requires it, only an intent creating an edition not owned
    /// (fonctionnel/regles-metier/appartenance-a-la-collection.md).
    /// </param>
    public void RecordAcquisition(Edition edition, EditionAcquisition acquisition)
    {
        // Every check runs before anything is mutated: a rejected acquisition must leave the
        // aggregate untouched, or a later save would delete an intent never actually realized.
        EnsureOwnEdition(edition);
        if (acquisition.Mode is null)
            throw new DomainRuleViolationException(
                DomainRules.EditionAcquisitionModeRequired, "An acquisition requires its mode.", edition, nameof(Edition.AcquisitionMode));
        if (edition.AcquisitionMode is not null)
            throw new DomainRuleViolationException(DomainRules.EditionAlreadyOwned, "This edition is already owned.");
        edition.EnsureValid(acquisition, edition.PublicationYear);

        // At most one of them exists: an album targeted as a whole has no other edition.
        var realized = _purchaseIntents.Find(p => p.EditionId == edition.Id || p.EditionId is null);
        if (realized is not null)
            _purchaseIntents.Remove(realized);

        edition.Apply(acquisition);
    }

    // A programming error, not a business one: only this album's own editions are ever offered
    // for its intents and purchases, so no user input can reach this with a foreign edition.
    private void EnsureOwnEdition(Edition edition)
    {
        if (edition.AlbumId != Id)
            throw new ArgumentException("The edition does not belong to this album.", nameof(edition));
    }

    // A programming error, not a business one: the client only sends back the intents it read on
    // this album.
    private void EnsureOwnIntent(PurchaseIntent intent)
    {
        if (!_purchaseIntents.Contains(intent))
            throw new ArgumentException("The intent does not belong to this album.", nameof(intent));
    }

    private void EnsureTargetable(Edition edition)
    {
        EnsureOwnEdition(edition);
        // An edition already bought cannot become an intent again: a second copy is recorded as
        // a new edition of the album, which then carries the intent
        // (fonctionnel/regles-metier/intention-d-acquisition.md).
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

    // An album targeted as a whole has no edition (modele-metier/intention-d-acquisition.md): once it has
    // one, owned or targeted, a specific edition is wished for, not any. The edition excepted is the one
    // a conversion to the whole album removes.
    private void EnsureWithoutEditionBut(Guid? removedEditionId)
    {
        if (_editions.Any(e => e.Id != removedEditionId))
            throw new DomainRuleViolationException(
                DomainRules.PurchaseIntentAlbumHasEditions, "An album with editions cannot be targeted as a whole by a purchase intent.");
    }

    private PurchaseIntent AddIntent(Edition? edition)
    {
        var intent = new PurchaseIntent(this, edition);
        _purchaseIntents.Add(intent);
        return intent;
    }
}
