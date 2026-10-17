using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Search;

/// <summary>
/// A condition of the advanced search (fonctionnel.md § Recherche avancée), on the records of an entity: a
/// criterion on one of its data, a link to related records, or a combination of conditions.
/// </summary>
/// <remarks>
/// A condition holds only if the known data establish it: absent data satisfy no comparison but "is not
/// set", whereas <see cref="SearchNot"/> keeps whatever its condition does not establish
/// (fonctionnel.md § Comparaisons).
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(SearchGroup), "group")]
[JsonDerivedType(typeof(SearchNot), "not")]
[JsonDerivedType(typeof(SearchLink), "link")]
[JsonDerivedType(typeof(SearchCriterion), "criterion")]
[JsonDerivedType(typeof(SearchDataComparison), "dataComparison")]
public abstract record SearchCondition;

/// <summary>Conditions combined by AND or by OR; empty, AND holds for every record and OR for none.</summary>
public sealed record SearchGroup(SearchOperator Operator, IReadOnlyList<SearchCondition> Conditions) : SearchCondition;

/// <summary>Holds for every record the condition does not establish.</summary>
public sealed record SearchNot(SearchCondition Condition) : SearchCondition;

/// <summary>
/// A condition on the records related by a link of the model (fonctionnel.md § Liens entre entités), which
/// may itself follow further links: the path of the criteria it holds.
/// </summary>
/// <param name="Link">Key of the link, among those the schema describes for the entity (<see cref="SearchLinkDescription"/>).</param>
/// <param name="Condition">
/// What the same related record must satisfy; <c>null</c> for any related record (e.g. "has a series", "has
/// at least one edition").
/// </param>
/// <param name="Quantifier">
/// How many related records must satisfy the condition, for a link to several records only; <c>null</c>
/// for a link to one record, which must exist and satisfy it.
/// </param>
public sealed record SearchLink(string Link, SearchCondition? Condition = null, SearchQuantifier? Quantifier = null) : SearchCondition;

/// <summary>How many of the records related by a link to several records must satisfy its condition.</summary>
/// <param name="Comparison">
/// For <see cref="SearchQuantifierKind.Count"/> only: a comparison of numbers, to which the number of related
/// records satisfying the condition is compared.
/// </param>
/// <param name="Values">The values of <paramref name="Comparison"/>, written as those of a criterion on a number.</param>
public sealed record SearchQuantifier(
    SearchQuantifierKind Kind, SearchComparison? Comparison = null, IReadOnlyList<string>? Values = null);

/// <summary>A comparison of one data of the entity to one or several values.</summary>
/// <param name="Field">Key of the data, among those the schema describes for the entity (<see cref="SearchFieldDescription"/>).</param>
/// <param name="Values">
/// None for <see cref="SearchComparison.IsSet"/> and <see cref="SearchComparison.IsNotSet"/>, two for
/// <see cref="SearchComparison.Between"/>, at least one for <see cref="SearchComparison.IsOneOf"/> and
/// <see cref="SearchComparison.IsNoneOf"/>, one otherwise. Each is written according to the type of the
/// data: a text as is; a number in decimal digits; a date as <c>yyyy</c>, <c>yyyy-MM</c> or
/// <c>yyyy-MM-dd</c>, its precision being that of the comparison; a boolean as <c>true</c> or
/// <c>false</c>; a member of an enumeration by its name; a record by its identifier.
/// </param>
/// <param name="MatchCaseAndAccents">
/// For a text only: compares it respecting case and accents, ignored otherwise (fonctionnel.md §
/// Comparaisons).
/// </param>
public sealed record SearchCriterion(
    string Field, SearchComparison Comparison, IReadOnlyList<string>? Values = null, bool MatchCaseAndAccents = false)
    : SearchCondition;

/// <summary>
/// A comparison of one data of the entity to another data of the same type (fonctionnel.md § Comparaisons), of
/// the same record or of a record it leads to: it holds only if both data are set and the comparison
/// establishes it.
/// </summary>
/// <param name="Field">Key of the data of the entity, among those the schema describes (<see cref="SearchFieldDescription"/>).</param>
/// <param name="Comparison">One of the <see cref="SearchFieldDescription.DataComparisons"/> of the data.</param>
/// <param name="Other">
/// The data compared to: of the same type, of the same enumeration for an enumeration, of the same entity for
/// a record; any date for a date.
/// </param>
/// <param name="MatchCaseAndAccents">For texts only: compares them respecting case and accents.</param>
/// <param name="DatePrecision">
/// For dates only, and required: the precision both are compared at, each covering the whole period it is
/// known to at that precision (e.g. a date known to the year covers its twelve months).
/// </param>
public sealed record SearchDataComparison(
    string Field, SearchComparison Comparison, SearchOperand Other, bool MatchCaseAndAccents = false, SearchDatePrecision? DatePrecision = null)
    : SearchCondition;

/// <summary>The other data of a <see cref="SearchDataComparison"/>.</summary>
/// <param name="Field">Key of the data, among those of the entity <paramref name="Links"/> lead to.</param>
/// <param name="Links">
/// Keys of the links leading from the record to the one carrying the data, each to one record
/// (<see cref="SearchLinkDescription.IsMultiple"/> false); none for a data of the record itself. A
/// comparison to the data of several related records is written under a <see cref="SearchLink"/>, whose
/// quantifier counts them, and whose related record leads back to the record by a link to one record.
/// </param>
public sealed record SearchOperand(string Field, IReadOnlyList<string>? Links = null);

[JsonConverter(typeof(JsonStringEnumConverter<SearchOperator>))]
public enum SearchOperator
{
    And,
    Or,
}

/// <summary>How many related records must satisfy the condition of a link (fonctionnel.md § Liens entre entités).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SearchQuantifierKind>))]
public enum SearchQuantifierKind
{
    /// <summary>At least one.</summary>
    Any,

    /// <summary>None, which holds without any related record.</summary>
    None,

    /// <summary>All of them, and at least one: never without any related record.</summary>
    All,

    /// <summary>A number of them, compared as any number.</summary>
    Count,
}

/// <summary>The comparisons of a criterion, each available according to the type of its data (fonctionnel.md § Comparaisons).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SearchComparison>))]
public enum SearchComparison
{
    // Text.
    Contains,
    DoesNotContain,
    StartsWith,

    // Text, number, date.
    EqualTo,
    NotEqualTo,

    // Number, date.
    LessThan,
    LessThanOrEqualTo,
    GreaterThan,
    GreaterThanOrEqualTo,
    Between,

    // Enumeration, boolean, record.
    Is,
    IsNot,
    IsOneOf,
    IsNoneOf,

    // Any optional data.
    IsSet,
    IsNotSet,
}

/// <summary>An advanced search of one entity, whose results are paginated by the query of the request.</summary>
/// <param name="Criteria">What the records must satisfy; <c>null</c> for every record.</param>
public sealed record AdvancedSearchRequest(SearchCondition? Criteria);
