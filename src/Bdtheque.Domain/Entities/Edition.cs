using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A published manifestation of an album (Édition): format, publisher, and — when the
/// acquisition mode is set — the data that makes it part of the user's collection (see
/// fonctionnel.md § Appartenance à la collection).
/// </summary>
public sealed class Edition : EntityBase
{
    public Guid AlbumId { get; private set; }
    public Album Album { get; private set; } = null!;

    public Guid PublisherId { get; private set; }
    public Publisher Publisher { get; private set; } = null!;
    public Guid? PublisherCollectionId { get; private set; }
    public PublisherCollection? PublisherCollection { get; private set; }

    public int? PublicationYear { get; private set; }
    public string? Isbn { get; private set; }

    public BindingType? Binding { get; private set; }
    public BookOrientation? Orientation { get; private set; }
    public ReadingDirection? ReadingDirection { get; private set; }
    public EditionFormat? Format { get; private set; }
    public int? PageCount { get; private set; }
    public EditionCategory? Category { get; private set; }
    public bool IsDedicated { get; private set; }
    public bool IsColor { get; private set; } = true;
    public EditionCondition? Condition { get; private set; }

    public AcquisitionMode? AcquisitionMode { get; private set; }
    public bool IsSecondHand { get; private set; }
    public DateOnly? AcquisitionDate { get; private set; }
    public decimal? AcquisitionAmount { get; private set; }
    public string? AcquisitionCurrency { get; private set; }
    public bool IsFree { get; private set; }
    public decimal? InitialValueAmount { get; private set; }
    public string? InitialValueCurrency { get; private set; }

    public string? PersonalReference { get; private set; }
    public string? PersonalNotes { get; private set; }

    // Read-only from outside: a visual is only ever created through AddVisual, so it can never
    // be moved to another edition. Enumerate it through GetOrderedVisuals for display.
    private readonly List<EditionVisual> _visuals = [];
    public IReadOnlyCollection<EditionVisual> Visuals => _visuals;

    // EF Core parameterless constructor
    private Edition() { }

    public Edition(Album album, Publisher publisher)
    {
        ArgumentNullException.ThrowIfNull(album);
        ArgumentNullException.ThrowIfNull(publisher);

        Album = album;
        AlbumId = album.Id;
        Publisher = publisher;
        PublisherId = publisher.Id;
        album.Register(this);
    }

    /// <summary>
    /// Sets the publisher and, optionally, a publisher collection, as a single atomic
    /// operation: a collection can only be set together with the publisher it belongs to
    /// (modele-metier.md § Édition).
    /// </summary>
    public void SetPublisher(Publisher publisher, PublisherCollection? collection)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        if (collection is not null && collection.PublisherId != publisher.Id)
            throw new DomainRuleViolationException(
                DomainRules.PublisherCollectionNotOfPublisher, "The publisher collection must belong to the given publisher.");

        Publisher = publisher;
        PublisherId = publisher.Id;
        PublisherCollection = collection;
        PublisherCollectionId = collection?.Id;
    }

    public void SetPublicationYear(int? year)
    {
        if (year is <= 0)
            throw new DomainRuleViolationException(DomainRules.EditionPublicationYearPositive, "Publication year must be positive when specified.");
        EnsureAmountsDated(AcquisitionAmount, InitialValueAmount, AcquisitionDate, year, IsAlbumDated);
        PublicationYear = year;
    }

    /// <summary>
    /// Stores the ISBN as entered, without rejecting an incorrect check digit: validation is
    /// advisory only (fonctionnel.md § Validation de l'ISBN) — see <see cref="IsbnChecksumValidator"/>
    /// for the non-blocking check a caller should run to warn the user.
    /// </summary>
    public void SetIsbn(string? isbn) => Isbn = DomainText.NullIfBlank(isbn);

    public void SetPageCount(int? count)
    {
        if (count is <= 0)
            throw new DomainRuleViolationException(DomainRules.EditionPageCountPositive, "Page count must be positive when specified.");
        PageCount = count;
    }

    public void SetBinding(BindingType? binding)
    {
        if (binding is not null)
            EnumGuard.EnsureDefined(binding.Value, nameof(binding));
        Binding = binding;
    }

    public void SetOrientation(BookOrientation? orientation)
    {
        if (orientation is not null)
            EnumGuard.EnsureDefined(orientation.Value, nameof(orientation));
        Orientation = orientation;
    }

    public void SetReadingDirection(ReadingDirection? readingDirection)
    {
        if (readingDirection is not null)
            EnumGuard.EnsureDefined(readingDirection.Value, nameof(readingDirection));
        ReadingDirection = readingDirection;
    }

    public void SetFormat(EditionFormat? format)
    {
        if (format is not null)
            EnumGuard.EnsureDefined(format.Value, nameof(format));
        Format = format;
    }

    public void SetCategory(EditionCategory? category)
    {
        if (category is not null)
            EnumGuard.EnsureDefined(category.Value, nameof(category));
        Category = category;
    }

    public void SetDedicated(bool isDedicated) => IsDedicated = isDedicated;

    public void SetColor(bool isColor) => IsColor = isColor;

    public void SetCondition(EditionCondition? condition)
    {
        if (condition is not null)
            EnumGuard.EnsureDefined(condition.Value, nameof(condition));
        Condition = condition;
    }

    /// <summary>
    /// Sets the acquisition mode. An edition is part of the collection only once this is set
    /// (fonctionnel.md § Appartenance à la collection); clearing it back to <see langword="null"/>
    /// requires the acquisition date, price and initial value to be cleared first, since an edition
    /// not owned has no value (modele-metier.md § Édition).
    /// </summary>
    public void SetAcquisitionMode(AcquisitionMode? mode)
    {
        if (mode is not null)
            EnumGuard.EnsureDefined(mode.Value, nameof(mode));
        // Acquiring an edition (no mode yet -> a mode) may realize an intent on it or on its album,
        // which only the album aggregate sees: it must go through Album.RecordAcquisition. Changing
        // the mode of an owned edition, or clearing it, stays here.
        if (mode is not null && AcquisitionMode is null)
            throw new InvalidOperationException(
                "An edition is acquired through Album.RecordAcquisition, which realizes the intents it satisfies.");
        if (mode is null && (AcquisitionDate is not null || AcquisitionAmount is not null || InitialValueAmount is not null))
            throw new DomainRuleViolationException(
                DomainRules.EditionAcquisitionModeRequired,
                "Cannot clear the acquisition mode while an acquisition date, price or initial value is set. " +
                "Clear them first with SetAcquisitionDate(null), SetAcquisitionPrice(null, null) and SetInitialValue(null, null).");
        if (mode is not null)
            EnsureNotFreePurchase(mode.Value);

        AcquisitionMode = mode;
    }

    public void SetSecondHand(bool isSecondHand) => IsSecondHand = isSecondHand;

    public void SetAcquisitionDate(DateOnly? date)
    {
        if (date is not null && AcquisitionMode is null)
            throw new DomainRuleViolationException(
                DomainRules.EditionAcquisitionModeRequired, "An acquisition mode must be set before an acquisition date.");
        EnsureAmountsDated(AcquisitionAmount, InitialValueAmount, date, PublicationYear, IsAlbumDated);
        AcquisitionDate = date;
    }

    /// <summary>
    /// Sets the acquisition price as a single amount + currency pair (modele-metier.md §
    /// Édition): both are provided together or cleared together. Requires an acquisition mode and
    /// a reference date (fonctionnel.md § Gestion des devises), and is mutually exclusive with
    /// <see cref="IsFree"/> (see <see cref="SetFree"/>).
    /// </summary>
    public void SetAcquisitionPrice(decimal? amount, string? currencyCode)
    {
        if ((amount is null) != (currencyCode is null))
            throw new DomainRuleViolationException(
                DomainRules.EditionAcquisitionAmountCurrencyTogether, "An acquisition amount and its currency must be provided together, or not at all.");

        if (amount is not null)
        {
            if (IsFree)
                throw new DomainRuleViolationException(
                    DomainRules.EditionFreeExcludesPrice,
                    "Cannot set an acquisition price while the edition is marked free. Clear it first with SetFree(false).");
            if (AcquisitionMode is null)
                throw new DomainRuleViolationException(
                    DomainRules.EditionAcquisitionModeRequired, "An acquisition mode must be set before an acquisition price.");
            if (amount <= 0)
                throw new DomainRuleViolationException(
                    DomainRules.EditionAcquisitionAmountPositive, "Acquisition amount must be positive when specified.");
            EnsureValidCurrencyCode(currencyCode!);
            EnsureAmountsDated(amount, null, AcquisitionDate, PublicationYear, IsAlbumDated);
        }

        AcquisitionAmount = amount;
        AcquisitionCurrency = currencyCode;
    }

    /// <summary>
    /// Sets the initial value — the selling price of the edition when it was published — as a
    /// single amount + currency pair, under the same rules as the acquisition price, except for its
    /// reference date: the edition year, or else the album's first publication (fonctionnel.md §
    /// Gestion des devises).
    /// </summary>
    public void SetInitialValue(decimal? amount, string? currencyCode)
    {
        if ((amount is null) != (currencyCode is null))
            throw new DomainRuleViolationException(
                DomainRules.EditionInitialValueAmountCurrencyTogether, "An initial value amount and its currency must be provided together, or not at all.");

        if (amount is not null)
        {
            if (IsFree)
                throw new DomainRuleViolationException(
                    DomainRules.EditionFreeExcludesInitialValue,
                    "Cannot set an initial value while the edition is marked free. Clear it first with SetFree(false).");
            if (AcquisitionMode is null)
                throw new DomainRuleViolationException(
                    DomainRules.EditionAcquisitionModeRequired, "An acquisition mode must be set before an initial value.");
            if (amount <= 0)
                throw new DomainRuleViolationException(
                    DomainRules.EditionInitialValueAmountPositive, "Initial value amount must be positive when specified.");
            EnsureValidCurrencyCode(currencyCode!);
            EnsureAmountsDated(null, amount, AcquisitionDate, PublicationYear, IsAlbumDated);
        }

        InitialValueAmount = amount;
        InitialValueCurrency = currencyCode;
    }

    /// <summary>
    /// Marks the edition as free, i.e. it has no value: no amount is recorded at all — neither a
    /// price, nor a known market value, nor an initial value. Per fonctionnel.md § Libellés
    /// contextuels sur l'édition, these fields are then disabled and emptied in the UI; the domain
    /// mirrors that by clearing them here. A purchased edition cannot be free.
    /// </summary>
    public void SetFree(bool isFree)
    {
        if (isFree && AcquisitionMode is Enums.AcquisitionMode.Purchase)
            throw new DomainRuleViolationException(DomainRules.EditionPurchaseCannotBeFree, "A purchased edition cannot be free.");

        IsFree = isFree;
        if (isFree)
        {
            AcquisitionAmount = null;
            AcquisitionCurrency = null;
            InitialValueAmount = null;
            InitialValueCurrency = null;
        }
    }

    public void SetPersonalReference(string? reference) => PersonalReference = DomainText.NullIfBlank(reference);

    public void SetPersonalNotes(string? notes) => PersonalNotes = DomainText.NullIfBlank(notes);

    /// <summary>
    /// Returns <see cref="Visuals"/> in the fixed presentation order required by
    /// fonctionnel.md § Ordre des visuels d'une édition: by <see cref="EditionVisual.Type"/> —
    /// whose explicit int values are assigned in that same fixed order, see
    /// <see cref="Enums.VisualType"/> — then by <see cref="EditionVisual.DisplayOrder"/> within
    /// the same type. This is the single place that applies the rule: EF Core does not order a
    /// loaded collection navigation on its own, so callers must go through this method rather
    /// than enumerate <see cref="Visuals"/> directly. Visuals tied on both keys are ordered by
    /// <see cref="Entities.Common.EntityBase.Id"/>, so that the database's arbitrary row order
    /// never shuffles them from one load to the next.
    /// </summary>
    public IEnumerable<EditionVisual> GetOrderedVisuals() =>
        _visuals.OrderBy(v => v.Type).ThenBy(v => v.DisplayOrder).ThenBy(v => v.Id);

    // Only reachable through Album.RecordAcquisition (see SetAcquisitionMode).
    internal void Acquire(AcquisitionMode mode)
    {
        EnumGuard.EnsureDefined(mode, nameof(mode));
        AcquisitionMode = mode;
    }

    public EditionVisual AddVisual(VisualType type, string mediaReference, int displayOrder)
    {
        var visual = new EditionVisual(this, type, mediaReference, displayOrder);
        _visuals.Add(visual);
        return visual;
    }

    // ISO 4217 gives every currency a 3-letter uppercase alphabetic code; validating the shape
    // (rather than a hand-maintained list of codes) matches "any currency" from fonctionnel.md
    // § Gestion des devises without artificially restricting which ones are accepted — including
    // the old franc's reserved code QZF, which has the same shape (choix-implementation.md §
    // Représentation de la devise).
    private static void EnsureValidCurrencyCode(string currencyCode)
    {
        if (currencyCode.Length != 3 || !currencyCode.All(c => c is >= 'A' and <= 'Z'))
            throw new DomainRuleViolationException(
                DomainRules.EditionCurrencyCodeInvalid, "Currency code must be a 3-letter uppercase ISO 4217 code.");
    }

    // Also run by Album.RecordAcquisition before it touches anything: freeness is left free of
    // rules while the edition is not owned, and only conflicts with the purchase that acquires it.
    internal void EnsureNotFreePurchase(AcquisitionMode mode)
    {
        if (mode == Enums.AcquisitionMode.Purchase && IsFree)
            throw new DomainRuleViolationException(DomainRules.EditionPurchaseCannotBeFree, "A free edition cannot be purchased.");
    }

    // Run by Album.SetFirstPublicationDate before it clears its date: the album's date is the last
    // reference date an amount of the edition may fall back on.
    internal void EnsureAmountsDatedWithoutAlbumDate() =>
        EnsureAmountsDated(AcquisitionAmount, InitialValueAmount, AcquisitionDate, PublicationYear, albumDated: () => false);

    /// <summary>
    /// Checks that every amount keeps a reference date (fonctionnel.md § Gestion des devises), with
    /// the values the edition would have after the write under way — the price: acquisition date,
    /// edition year, or album's first publication; the initial value: edition year, or album's
    /// first publication. The album is only consulted when the edition's own dates do not suffice.
    /// </summary>
    private static void EnsureAmountsDated(
        decimal? price, decimal? initialValue, DateOnly? acquisitionDate, int? publicationYear, Func<bool> albumDated)
    {
        if (price is not null && acquisitionDate is null && publicationYear is null && !albumDated())
            throw new DomainRuleViolationException(
                DomainRules.EditionAcquisitionPriceReferenceDateRequired,
                "An acquisition price requires an acquisition date, an edition year or the album's first publication date.");
        if (initialValue is not null && publicationYear is null && !albumDated())
            throw new DomainRuleViolationException(
                DomainRules.EditionInitialValueReferenceDateRequired,
                "An initial value requires an edition year or the album's first publication date.");
    }

    // A programming error, not a business one, when the album is missing: an edition read without
    // its album cannot tell whether its amounts are still dated.
    private bool IsAlbumDated() =>
        (Album ?? throw new InvalidOperationException("The album of the edition must be loaded to check the reference date of its amounts."))
        .FirstPublicationYear is not null;
}
