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

    /// <summary>Creates an edition of <paramref name="album"/>, not owned until its acquisition is recorded.</summary>
    public Edition(Album album, Publisher? publisher)
    {
        ArgumentNullException.ThrowIfNull(album);
        SetPublisher(publisher, null);

        Album = album;
        AlbumId = album.Id;
        album.Register(this);
    }

    /// <summary>
    /// Sets the publisher and, optionally, a publisher collection, as a single atomic
    /// operation: a collection can only be set together with the publisher it belongs to
    /// (modele-metier.md § Édition). The publisher is required, a form left without one being an
    /// input mistake.
    /// </summary>
    public void SetPublisher(Publisher? publisher, PublisherCollection? collection)
    {
        if (publisher is null)
            throw new DomainRuleViolationException(DomainRules.EditionPublisherRequired, "An edition must have a publisher.");
        if (collection is not null && collection.PublisherId != publisher.Id)
            throw new DomainRuleViolationException(
                DomainRules.PublisherCollectionNotOfPublisher, "The publisher collection must belong to the given publisher.");

        Publisher = publisher;
        PublisherId = publisher.Id;
        PublisherCollection = collection;
        PublisherCollectionId = collection?.Id;
    }

    /// <summary>
    /// Sets the edition year alone, which must leave every amount with a reference date. A form,
    /// which may at once replace the edition year by the acquisition date as reference date of the
    /// price, goes through <see cref="SetPublicationYearAndAcquisition"/>.
    /// </summary>
    public void SetPublicationYear(int? year)
    {
        EnsurePublicationYearPositive(year);
        EnsureAmountsDated(AcquisitionAmount, InitialValueAmount, AcquisitionDate, year, IsAlbumDated);
        PublicationYear = year;
    }

    /// <summary>
    /// Sets the edition year, the acquisition and the value together, as the form sends them: the
    /// edition year and the acquisition date are both reference dates of the price, so that writing
    /// them one after the other would fail in one order or the other. Nothing is changed if any of
    /// them is refused.
    /// </summary>
    /// <remarks>
    /// An edition is acquired through <see cref="Album.RecordAcquisition(Edition, EditionAcquisition)"/>
    /// only, and an owned edition stays owned until deleted: a bought edition never becomes an
    /// intent again (fonctionnel.md § Intention d'achat).
    /// </remarks>
    public void SetPublicationYearAndAcquisition(int? publicationYear, EditionAcquisition acquisition)
    {
        ArgumentNullException.ThrowIfNull(acquisition);
        // Acquiring may realize an intent on this edition or on its album, which only the album
        // aggregate sees: a programming error to reach it from here.
        if (AcquisitionMode is null && acquisition.Mode is not null)
            throw new InvalidOperationException(
                "An edition is acquired through Album.RecordAcquisition, which realizes the intents it satisfies.");
        if (AcquisitionMode is not null && acquisition.Mode is null)
            throw new DomainRuleViolationException(
                DomainRules.EditionAcquisitionModeRequired, "An owned edition keeps an acquisition mode until it is deleted.");
        EnsurePublicationYearPositive(publicationYear);
        EnsureValid(acquisition, publicationYear);

        PublicationYear = publicationYear;
        Apply(acquisition);
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

    public void SetSecondHand(bool isSecondHand) => IsSecondHand = isSecondHand;

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

    // Only run once EnsureValid has accepted the acquisition: by SetPublicationYearAndAcquisition, or
    // by Album.RecordAcquisition, the only way to acquire the edition.
    internal void Apply(EditionAcquisition acquisition)
    {
        AcquisitionMode = acquisition.Mode;
        AcquisitionDate = acquisition.Date;
        AcquisitionAmount = acquisition.PriceAmount;
        AcquisitionCurrency = acquisition.PriceCurrency;
        IsFree = acquisition.IsFree;
        InitialValueAmount = acquisition.InitialValueAmount;
        InitialValueCurrency = acquisition.InitialValueCurrency;
    }

    /// <summary>
    /// Checks the acquisition and value an edition would have after the write under way, with
    /// <paramref name="publicationYear"/> as its edition year (modele-metier.md § Édition, contraintes
    /// d'intégrité). Also run by <see cref="Album.RecordAcquisition(Edition, EditionAcquisition)"/>
    /// before it touches anything.
    /// </summary>
    internal void EnsureValid(EditionAcquisition acquisition, int? publicationYear)
    {
        if (acquisition.Mode is not null)
            EnumGuard.EnsureDefined(acquisition.Mode.Value, nameof(acquisition));
        if ((acquisition.PriceAmount is null) != (acquisition.PriceCurrency is null))
            throw new DomainRuleViolationException(
                DomainRules.EditionAcquisitionAmountCurrencyTogether, "An acquisition amount and its currency must be provided together, or not at all.");
        if ((acquisition.InitialValueAmount is null) != (acquisition.InitialValueCurrency is null))
            throw new DomainRuleViolationException(
                DomainRules.EditionInitialValueAmountCurrencyTogether, "An initial value amount and its currency must be provided together, or not at all.");

        // An edition not owned has no value; freeness, a trait of the copy, is left free of rules
        // until the edition is acquired (fonctionnel.md § Intention d'achat).
        if (acquisition.Mode is null
            && (acquisition.Date is not null || acquisition.PriceAmount is not null || acquisition.InitialValueAmount is not null))
            throw new DomainRuleViolationException(
                DomainRules.EditionAcquisitionModeRequired, "An acquisition date, price or initial value requires an acquisition mode.");

        // A free edition has no value at all: neither price paid, nor market value, nor initial value.
        if (acquisition.IsFree)
        {
            if (acquisition.PriceAmount is not null)
                throw new DomainRuleViolationException(DomainRules.EditionFreeExcludesPrice, "A free edition has no acquisition price.");
            if (acquisition.InitialValueAmount is not null)
                throw new DomainRuleViolationException(DomainRules.EditionFreeExcludesInitialValue, "A free edition has no initial value.");
            if (acquisition.Mode == Enums.AcquisitionMode.Purchase)
                throw new DomainRuleViolationException(DomainRules.EditionPurchaseCannotBeFree, "A purchased edition cannot be free.");
        }

        if (acquisition.PriceAmount is <= 0)
            throw new DomainRuleViolationException(
                DomainRules.EditionAcquisitionAmountPositive, "Acquisition amount must be positive when specified.");
        if (acquisition.PriceCurrency is not null)
            EnsureValidCurrencyCode(acquisition.PriceCurrency);
        if (acquisition.InitialValueAmount is <= 0)
            throw new DomainRuleViolationException(
                DomainRules.EditionInitialValueAmountPositive, "Initial value amount must be positive when specified.");
        if (acquisition.InitialValueCurrency is not null)
            EnsureValidCurrencyCode(acquisition.InitialValueCurrency);

        EnsureAmountsDated(acquisition.PriceAmount, acquisition.InitialValueAmount, acquisition.Date, publicationYear, IsAlbumDated);
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

    private static void EnsurePublicationYearPositive(int? year)
    {
        if (year is <= 0)
            throw new DomainRuleViolationException(DomainRules.EditionPublicationYearPositive, "Publication year must be positive when specified.");
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
