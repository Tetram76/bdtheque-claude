using System.Text.Json.Serialization;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Search;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// An entry of the change log (modele-metier.md § Entrée du journal des modifications). It carries
/// data, never a text: the client builds the message in the display culture in force when it presents
/// it (fonctionnel/presentation.md § Langue et culture d'affichage), and always the same message for the same entry.
/// </summary>
/// <param name="Date">Date of the write.</param>
/// <param name="RecordId">Identifier of the record written, kept after its deletion.</param>
/// <param name="Label">What the label of the record was built from at the time of the write.</param>
/// <param name="Changes">For a modification only: each attribute changed.</param>
public sealed record ChangeLogEntryItem(
    Guid Id,
    DateTimeOffset Date,
    RecordType RecordType,
    Guid RecordId,
    RecordLabel Label,
    ChangeLogOperation Operation,
    IReadOnlyList<ChangeLogChange> Changes);

/// <summary>
/// One attribute changed by a modification: an element added (genre, contribution, visual…) has no
/// value before, an element removed none after.
/// </summary>
public sealed record ChangeLogChange(ChangeLogAttribute Attribute, ChangeLogValue? Before, ChangeLogValue? After);

/// <summary>A value of an attribute, as recorded in the record.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(TextValue), "text")]
[JsonDerivedType(typeof(NumberValue), "number")]
[JsonDerivedType(typeof(FlagValue), "flag")]
[JsonDerivedType(typeof(DateValue), "date")]
[JsonDerivedType(typeof(PartialDateValue), "partialDate")]
[JsonDerivedType(typeof(AmountValue), "amount")]
[JsonDerivedType(typeof(EnumValue), "enum")]
[JsonDerivedType(typeof(ReferenceValue), "reference")]
[JsonDerivedType(typeof(ContributionValue), "contribution")]
[JsonDerivedType(typeof(VisualValue), "visual")]
[JsonDerivedType(typeof(SearchCriteriaValue), "searchCriteria")]
[JsonDerivedType(typeof(SearchVariablesValue), "searchVariables")]
public abstract record ChangeLogValue;

public sealed record TextValue(string Value) : ChangeLogValue;

public sealed record NumberValue(int Value) : ChangeLogValue;

public sealed record FlagValue(bool Value) : ChangeLogValue;

public sealed record DateValue(DateOnly Value) : ChangeLogValue;

/// <summary>A date known at year, or year and month, granularity.</summary>
public sealed record PartialDateValue(int Year, int? Month) : ChangeLogValue;

/// <summary>An amount in its own currency (ISO 4217 code, or <c>QZF</c> for the old franc).</summary>
public sealed record AmountValue(decimal Amount, string Currency) : ChangeLogValue;

/// <summary>A member of one of the enumerations of the contracts.</summary>
/// <param name="Type">Name of the enumeration, e.g. <c>BindingType</c>.</param>
/// <param name="Member">Name of the member, e.g. <c>Hardcover</c>.</param>
public sealed record EnumValue(string Type, string Member) : ChangeLogValue;

/// <summary>Another record (e.g. the series of an album), with what its label was built from.</summary>
public sealed record ReferenceValue(Guid Id, RecordLabel Label) : ChangeLogValue;

/// <summary>A role credited to an author on an album or a series.</summary>
public sealed record ContributionValue(Guid AuthorId, AuthorLabel Author, ContributionRole Role) : ChangeLogValue;

/// <summary>A visual of an edition, with its rank among the visuals of its type.</summary>
public sealed record VisualValue(Guid Id, VisualType Type, int DisplayOrder, string MediaReference) : ChangeLogValue;

/// <summary>The criteria of a saved search.</summary>
public sealed record SearchCriteriaValue(SearchCondition Criteria) : ChangeLogValue;

/// <summary>The variables of a saved search, each with its default value.</summary>
public sealed record SearchVariablesValue(IReadOnlyList<SavedSearchVariable> Variables) : ChangeLogValue;

/// <summary>What the label of a record was built from at the time of the write.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(AlbumLabel), "album")]
[JsonDerivedType(typeof(EditionLabel), "edition")]
[JsonDerivedType(typeof(PurchaseIntentLabel), "purchaseIntent")]
[JsonDerivedType(typeof(AuthorLabel), "author")]
[JsonDerivedType(typeof(PublisherCollectionLabel), "publisherCollection")]
[JsonDerivedType(typeof(NameLabel), "name")]
public abstract record RecordLabel;

/// <summary>What the label of an album is built from (fonctionnel/presentation.md § Libellé d'un album).</summary>
public sealed record AlbumLabel(
    string? Title,
    string? SeriesTitle,
    AlbumType Type,
    bool IsSpecialIssue,
    int? VolumeNumber,
    int? StartVolumeNumber,
    int? EndVolumeNumber) : RecordLabel;

/// <summary>What the label of an edition is built from (fonctionnel/presentation.md § Libellé d'une édition), with the label of its album.</summary>
public sealed record EditionLabel(
    AlbumLabel Album,
    string PublisherName,
    string? PublisherCollectionName,
    int? PublicationYear,
    string? Isbn) : RecordLabel;

/// <summary>The target of a purchase intent: its album as a whole, or one of its editions.</summary>
public sealed record PurchaseIntentLabel(AlbumLabel? Album, EditionLabel? Edition) : RecordLabel;

/// <summary>What the main identifier of an author is built from (fonctionnel/presentation.md § Artistes).</summary>
public sealed record AuthorLabel(string? LastName, string? FirstName, string? Pseudonym) : RecordLabel;

/// <summary>A publisher collection, with its publisher.</summary>
public sealed record PublisherCollectionLabel(string Name, string PublisherName) : RecordLabel;

/// <summary>A record labelled by its name alone: series, publisher, genre, universe.</summary>
public sealed record NameLabel(string Name) : RecordLabel;
