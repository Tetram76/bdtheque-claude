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

    public string? PersonalReference { get; private set; }
    public string? PersonalNotes { get; private set; }

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
            throw new ArgumentException("The publisher collection must belong to the given publisher.", nameof(collection));

        Publisher = publisher;
        PublisherId = publisher.Id;
        PublisherCollection = collection;
        PublisherCollectionId = collection?.Id;
    }

    public void SetPublicationYear(int? year)
    {
        if (year is <= 0)
            throw new ArgumentOutOfRangeException(nameof(year), year, "Publication year must be positive when specified.");
        PublicationYear = year;
    }

    /// <summary>
    /// Stores the ISBN as entered, without rejecting an incorrect check digit: validation is
    /// advisory only (fonctionnel.md § Validation de l'ISBN) — see <see cref="IsbnChecksumValidator"/>
    /// for the non-blocking check a caller should run to warn the user.
    /// </summary>
    public void SetIsbn(string? isbn) => Isbn = NullIfEmpty(isbn);

    public void SetPageCount(int? count)
    {
        if (count is <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Page count must be positive when specified.");
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
    /// requires the acquisition date and price to be cleared first, since both depend on it.
    /// </summary>
    public void SetAcquisitionMode(AcquisitionMode? mode)
    {
        if (mode is not null)
            EnumGuard.EnsureDefined(mode.Value, nameof(mode));
        if (mode is null && (AcquisitionDate is not null || AcquisitionAmount is not null))
            throw new InvalidOperationException(
                "Cannot clear the acquisition mode while an acquisition date or price is set. " +
                "Clear them first with SetAcquisitionDate(null) and SetAcquisitionPrice(null, null).");

        AcquisitionMode = mode;
    }

    public void SetSecondHand(bool isSecondHand) => IsSecondHand = isSecondHand;

    public void SetAcquisitionDate(DateOnly? date)
    {
        if (date is not null && AcquisitionMode is null)
            throw new InvalidOperationException("An acquisition mode must be set before an acquisition date.");
        AcquisitionDate = date;
    }

    /// <summary>
    /// Sets the acquisition price as a single amount + currency pair (modele-metier.md §
    /// Édition): both are provided together or cleared together. Requires an acquisition mode
    /// and is mutually exclusive with <see cref="IsFree"/> (see <see cref="SetFree"/>).
    /// </summary>
    public void SetAcquisitionPrice(decimal? amount, string? currencyCode)
    {
        if ((amount is null) != (currencyCode is null))
            throw new ArgumentException("An acquisition amount and its currency must be provided together, or not at all.");

        if (amount is not null)
        {
            if (IsFree)
                throw new InvalidOperationException(
                    "Cannot set an acquisition price while the edition is marked free. Clear it first with SetFree(false).");
            if (AcquisitionMode is null)
                throw new InvalidOperationException("An acquisition mode must be set before an acquisition price.");
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Acquisition amount must be positive when specified.");
            EnsureValidCurrencyCode(currencyCode!);
        }

        AcquisitionAmount = amount;
        AcquisitionCurrency = currencyCode;
    }

    /// <summary>
    /// Marks the edition as free, i.e. no amount is recorded at all — not even a known market
    /// value. Per fonctionnel.md § Libellés contextuels sur l'édition, the price field is then
    /// disabled and emptied in the UI; the domain mirrors that by clearing it here.
    /// </summary>
    public void SetFree(bool isFree)
    {
        IsFree = isFree;
        if (isFree)
        {
            AcquisitionAmount = null;
            AcquisitionCurrency = null;
        }
    }

    public void SetPersonalReference(string? reference) => PersonalReference = NullIfEmpty(reference);

    public void SetPersonalNotes(string? notes) => PersonalNotes = NullIfEmpty(notes);

    // ISO 4217 gives every currency a 3-letter uppercase alphabetic code; validating the shape
    // (rather than a hand-maintained list of codes) matches "any currency" from fonctionnel.md
    // § Gestion des devises without artificially restricting which ones are accepted.
    private static void EnsureValidCurrencyCode(string currencyCode)
    {
        if (currencyCode.Length != 3 || !currencyCode.All(c => c is >= 'A' and <= 'Z'))
            throw new ArgumentException("Currency code must be a 3-letter uppercase ISO 4217 code.", nameof(currencyCode));
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
