namespace Bdtheque.Domain.Common;

/// <summary>
/// Codes of the business rules a <see cref="DomainRuleViolationException"/> can report. Each
/// value is a public contract — the frontend's localization keys off it — so a code is never
/// renamed or reused for a different rule once published.
/// </summary>
public static class DomainRules
{
    public const string AlbumTitleRequiredWithoutSeries = "Album.TitleRequiredWithoutSeries";
    public const string AlbumManualSortKeyRequiresTitle = "Album.ManualSortKeyRequiresTitle";
    public const string AlbumSortKeyRequired = "Album.SortKeyRequired";
    public const string AlbumVolumeNumberPositive = "Album.VolumeNumberPositive";
    public const string AlbumVolumeRangeBothOrNeither = "Album.VolumeRangeBothOrNeither";
    public const string AlbumVolumeRangeStartPositive = "Album.VolumeRangeStartPositive";
    public const string AlbumVolumeRangeOrder = "Album.VolumeRangeOrder";
    public const string AlbumVolumeRangeOmnibusOnly = "Album.VolumeRangeOmnibusOnly";
    public const string AlbumPublicationMonthRequiresYear = "Album.PublicationMonthRequiresYear";
    public const string AlbumPublicationMonthRange = "Album.PublicationMonthRange";
    public const string AlbumPublicationYearPositive = "Album.PublicationYearPositive";

    public const string PurchaseIntentAlbumAlreadyTargeted = "PurchaseIntent.AlbumAlreadyTargeted";
    public const string PurchaseIntentEditionsAlreadyTargeted = "PurchaseIntent.EditionsAlreadyTargeted";
    public const string PurchaseIntentEditionAlreadyTargeted = "PurchaseIntent.EditionAlreadyTargeted";
    public const string PurchaseIntentEditionAlreadyOwned = "PurchaseIntent.EditionAlreadyOwned";

    public const string EditionAlreadyOwned = "Edition.AlreadyOwned";

    public const string AuthorLastNameOrPseudonymRequired = "Author.LastNameOrPseudonymRequired";

    public const string EditionPublisherRequired = "Edition.PublisherRequired";
    public const string EditionPublicationYearPositive = "Edition.PublicationYearPositive";
    public const string EditionPageCountPositive = "Edition.PageCountPositive";
    public const string EditionAcquisitionModeRequired = "Edition.AcquisitionModeRequired";
    public const string EditionAcquisitionAmountCurrencyTogether = "Edition.AcquisitionAmountCurrencyTogether";
    public const string EditionAcquisitionAmountPositive = "Edition.AcquisitionAmountPositive";
    public const string EditionCurrencyCodeInvalid = "Edition.CurrencyCodeInvalid";
    public const string EditionFreeExcludesPrice = "Edition.FreeExcludesPrice";
    public const string EditionFreeExcludesInitialValue = "Edition.FreeExcludesInitialValue";
    public const string EditionPurchaseCannotBeFree = "Edition.PurchaseCannotBeFree";
    public const string EditionInitialValueAmountCurrencyTogether = "Edition.InitialValueAmountCurrencyTogether";
    public const string EditionInitialValueAmountPositive = "Edition.InitialValueAmountPositive";

    /// <summary>
    /// An acquisition price with none of its reference dates known (acquisition date, edition year,
    /// album's first publication), or a write that would clear the last one (fonctionnel.md § Gestion des devises).
    /// </summary>
    public const string EditionAcquisitionPriceReferenceDateRequired = "Edition.AcquisitionPriceReferenceDateRequired";

    /// <summary>
    /// An initial value with none of its reference dates known (edition year, album's first
    /// publication), or a write that would clear the last one (fonctionnel.md § Gestion des devises).
    /// </summary>
    public const string EditionInitialValueReferenceDateRequired = "Edition.InitialValueReferenceDateRequired";

    public const string EditionVisualMediaReferenceRequired = "EditionVisual.MediaReferenceRequired";
    public const string EditionVisualDisplayOrderNotNegative = "EditionVisual.DisplayOrderNotNegative";

    public const string GenreLabelRequired = "Genre.LabelRequired";

    public const string PublisherNameRequired = "Publisher.NameRequired";
    public const string PublisherWebsiteInvalid = "Publisher.WebsiteInvalid";

    /// <summary>A publisher collection used with a publisher it does not belong to (edition or series template).</summary>
    public const string PublisherCollectionNotOfPublisher = "PublisherCollection.NotOfPublisher";
    public const string PublisherCollectionNameRequired = "PublisherCollection.NameRequired";

    // Uniqueness rules: enforced by unique indexes, since the domain cannot see the other rows. The
    // API translates the violation of each index into its code (see Bdtheque.Api's UniqueIndexRules).
    public const string GenreLabelAlreadyUsed = "Genre.LabelAlreadyUsed";
    public const string PublisherNameAlreadyUsed = "Publisher.NameAlreadyUsed";
    public const string PublisherCollectionNameAlreadyUsed = "PublisherCollection.NameAlreadyUsed";

    /// <summary>The same author credited twice with the same role on an album, or on a series template.</summary>
    public const string ContributionAlreadyCredited = "Contribution.AlreadyCredited";

    public const string SeriesTitleRequired = "Series.TitleRequired";
    public const string SeriesSortKeyRequired = "Series.SortKeyRequired";
    public const string SeriesTheoreticalVolumeCountPositive = "Series.TheoreticalVolumeCountPositive";

    public const string UniverseNameRequired = "Universe.NameRequired";

    /// <summary>A universe set as its own ancestor, directly (own parent) or through its descendants.</summary>
    public const string UniverseHierarchyCycle = "Universe.HierarchyCycle";

    /// <summary>
    /// A text longer than its column (the persistence limit of every text field of the model): the
    /// database enforces it and the API translates the violation, like a uniqueness rule.
    /// </summary>
    public const string TextTooLong = "Text.TooLong";

    /// <summary>
    /// A deletion refused because other records still reference the deleted one (fonctionnel.md §
    /// Suppression des entités); the error carries the impact naming them.
    /// </summary>
    public const string DeletionBlockedByReferences = "Deletion.BlockedByReferences";
}
