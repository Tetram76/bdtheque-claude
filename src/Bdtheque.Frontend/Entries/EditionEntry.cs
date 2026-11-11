using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Entries;

/// <summary>
/// An edition as its entry form holds it while it is entered (visuel/saisie-edition/contenu.md): an owned edition, or an edition
/// targeted by a purchase intent, entered with the minimum of fields
/// (fonctionnel/regles-metier/intention-d-achat.md#réalisation-dune-intention). Persisted from the prerendering into the
/// interactive circuit, which does not read it again. It applies no rule of the edition: the domain is their only source
/// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md), and reports what to correct when the
/// edition is saved.
/// </summary>
public sealed class EditionEntry
{
    /// <summary>The currency proposed to an amount entered without one, that of the display culture.</summary>
    public const string DefaultCurrency = "EUR";

    /// <summary>The edition; <c>null</c> for a new one, not saved yet.</summary>
    public Guid? Id { get; set; }

    public Guid AlbumId { get; set; }

    /// <summary>The version of the album the form was read at, sent back with the saving: the edition belongs to its aggregate.</summary>
    public uint AlbumVersion { get; set; }

    /// <summary>The album of the edition, which the form names and leads to.</summary>
    public AlbumSummary Album { get; set; } = null!;

    /// <summary>Whether the edition is targeted by a purchase intent, hence not owned: no copy and no value are entered.</summary>
    public bool IsPurchaseIntent { get; set; }

    /// <summary>The intent that targets the edition, once saved.</summary>
    public Guid? PurchaseIntentId { get; set; }

    /// <summary>When the intent that targets the edition was set.</summary>
    public DateTimeOffset? PurchaseIntentCreatedAt { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public DateTimeOffset? EnteredCollectionAt { get; set; }

    /// <summary>
    /// The template of the album's series a new edition was pre-filled from, whose fields with a value say what it pre-filled;
    /// <c>null</c> for an edition already saved, or for an album without series.
    /// </summary>
    public SeriesEditionTemplate? SeriesTemplate { get; set; }

    public RecordOption? Publisher { get; set; }

    /// <summary>One of the collections of <see cref="Publisher"/>.</summary>
    public RecordOption? PublisherCollection { get; set; }

    public int? PublicationYear { get; set; }

    public string? Isbn { get; set; }

    /// <summary>Whether the check digit of the ISBN, as read, is consistent; <c>null</c> without ISBN.</summary>
    public bool? IsbnChecksumValid { get; set; }

    public EditionCategory? Category { get; set; }

    public EditionFormat? Format { get; set; }

    public BindingType? Binding { get; set; }

    public BookOrientation? Orientation { get; set; }

    public ReadingDirection? ReadingDirection { get; set; }

    public int? PageCount { get; set; }

    public bool IsColor { get; set; }

    public AcquisitionMode? AcquisitionMode { get; set; }

    public DateOnly? AcquisitionDate { get; set; }

    public EditionCondition? Condition { get; set; }

    public bool IsSecondHand { get; set; }

    public bool IsDedicated { get; set; }

    public string? PersonalReference { get; set; }

    public string? PersonalNotes { get; set; }

    public decimal? AcquisitionAmount { get; set; }

    public string? AcquisitionCurrency { get; set; }

    public decimal? InitialValueAmount { get; set; }

    public string? InitialValueCurrency { get; set; }

    /// <summary>Set through <see cref="SetFree"/>, which empties the amounts; public for the state persisted.</summary>
    public bool IsFree { get; set; }

    /// <summary>A new owned edition of an album, pre-filled as the API proposes it.</summary>
    public static EditionEntry New(NewEditionForm form)
    {
        var entry = new EditionEntry
        {
            AlbumId = form.AlbumId,
            AlbumVersion = form.AlbumVersion,
            Album = form.Album,
            SeriesTemplate = form.SeriesTemplate,
        };
        entry.Apply(form.Content, form.References);
        return entry;
    }

    /// <summary>A new edition targeted by a purchase intent, pre-filled as any new edition, without the traits of a copy.</summary>
    public static EditionEntry NewPurchaseIntent(NewPurchaseIntentForm form)
    {
        var edition = form.Edition;
        var entry = new EditionEntry
        {
            AlbumId = form.AlbumId,
            AlbumVersion = form.AlbumVersion,
            Album = form.Album,
            SeriesTemplate = form.SeriesTemplate,
            IsPurchaseIntent = true,
        };
        entry.Apply(
            new EditionContent(
                edition.PublisherId, edition.PublisherCollectionId, edition.PublicationYear, edition.Isbn, edition.Binding,
                edition.Orientation, edition.ReadingDirection, edition.Format, edition.PageCount, edition.Category, false,
                edition.IsColor, null, null, false, null, null, null, false, null, null, null, null),
            form.References);
        return entry;
    }

    /// <summary>The edition as the API gives it, owned or targeted by a purchase intent.</summary>
    public static EditionEntry Of(EditionForm form)
    {
        var entry = new EditionEntry
        {
            Id = form.Id,
            AlbumId = form.AlbumId,
            AlbumVersion = form.AlbumVersion,
            Album = form.Album,
            IsPurchaseIntent = form.PurchaseIntent is not null,
            PurchaseIntentId = form.PurchaseIntent?.Id,
            PurchaseIntentCreatedAt = form.PurchaseIntent?.CreatedAt,
            CreatedAt = form.CreatedAt,
            ModifiedAt = form.ModifiedAt,
            EnteredCollectionAt = form.EnteredCollectionAt,
            IsbnChecksumValid = form.IsbnChecksumValid,
        };
        entry.Apply(form.Content, form.References);
        return entry;
    }

    private void Apply(EditionContent content, EditionFormReferences references)
    {
        Publisher = references.Publisher is { } publisher ? new RecordOption(publisher.Id, publisher.Name, null) : null;
        PublisherCollection = references.PublisherCollection is { } collection
            ? new RecordOption(collection.Id, collection.Name, collection.PublisherName)
            : null;
        PublicationYear = content.PublicationYear;
        Isbn = content.Isbn;
        Binding = content.Binding;
        Orientation = content.Orientation;
        ReadingDirection = content.ReadingDirection;
        Format = content.Format;
        PageCount = content.PageCount;
        Category = content.Category;
        IsDedicated = content.IsDedicated;
        IsColor = content.IsColor;
        Condition = content.Condition;
        AcquisitionMode = content.AcquisitionMode;
        IsSecondHand = content.IsSecondHand;
        AcquisitionDate = content.AcquisitionDate;
        AcquisitionAmount = content.AcquisitionAmount;
        AcquisitionCurrency = content.AcquisitionCurrency;
        IsFree = content.IsFree;
        InitialValueAmount = content.InitialValueAmount;
        InitialValueCurrency = content.InitialValueCurrency;
        PersonalReference = content.PersonalReference;
        PersonalNotes = content.PersonalNotes;
    }

    /// <summary>
    /// Chooses the publisher: the collection, one of those of the publisher, is removed when the publisher changes
    /// (visuel/saisie-edition/contenu.md, as for a series).
    /// </summary>
    public void ChoosePublisher(RecordOption? publisher)
    {
        if (publisher?.Id != Publisher?.Id)
            PublisherCollection = null;
        Publisher = publisher;
    }

    /// <summary>
    /// Chooses the mode of acquisition: a purchase is never free, which unchecks the box the mode « Achat » disables
    /// (visuel/saisie-edition/contenu.md).
    /// </summary>
    public void ChooseAcquisitionMode(AcquisitionMode? mode)
    {
        AcquisitionMode = mode;
        if (mode == Contracts.Enums.AcquisitionMode.Purchase)
            IsFree = false;
    }

    /// <summary>Marks the edition free or not: a free edition has no value, its amounts emptied (modele-metier/edition.md).</summary>
    public void SetFree(bool isFree)
    {
        IsFree = isFree;
        if (!isFree)
            return;
        AcquisitionAmount = null;
        AcquisitionCurrency = null;
        InitialValueAmount = null;
        InitialValueCurrency = null;
    }

    /// <summary>
    /// The content to save: the whole form. An amount entered without its currency takes the one the form shows by default;
    /// a currency without amount is not sent.
    /// </summary>
    public EditionContent ToContent() => new(
        Publisher?.Id,
        PublisherCollection?.Id,
        PublicationYear,
        Isbn,
        Binding,
        Orientation,
        ReadingDirection,
        Format,
        PageCount,
        Category,
        IsDedicated,
        IsColor,
        Condition,
        AcquisitionMode,
        IsSecondHand,
        AcquisitionDate,
        AcquisitionAmount,
        AcquisitionAmount is null ? null : AcquisitionCurrency ?? DefaultCurrency,
        IsFree,
        InitialValueAmount,
        InitialValueAmount is null ? null : InitialValueCurrency ?? DefaultCurrency,
        PersonalReference,
        PersonalNotes);

    /// <summary>The edition targeted by a new purchase intent, with the minimum of fields.</summary>
    public PurchaseIntentEditionContent ToPurchaseIntentContent() => new(
        Publisher?.Id, PublisherCollection?.Id, PublicationYear, Isbn, Binding, Orientation, ReadingDirection, Format, PageCount, Category,
        IsColor);

    /// <summary>What tells two states of the form apart: the content it would save.</summary>
    public string Fingerprint() => JsonSerializer.Serialize(ToContent());

    public EditionEntry Copy() => JsonSerializer.Deserialize<EditionEntry>(JsonSerializer.Serialize(this))!;
}
