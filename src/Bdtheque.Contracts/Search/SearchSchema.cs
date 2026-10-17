using System.Text.Json.Serialization;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Search;

/// <summary>
/// What the advanced search can query, for the clients to build its form in the terms of the business
/// (fonctionnel.md § Recherche avancée): its keys are localized by the client, never shown as they are.
/// </summary>
public sealed record SearchSchema(IReadOnlyList<SearchEntityDescription> Entities);

/// <summary>An entity the advanced search returns, with the data and the links its criteria can follow.</summary>
public sealed record SearchEntityDescription(
    EntityKind Entity, IReadOnlyList<SearchFieldDescription> Fields, IReadOnlyList<SearchLinkDescription> Links);

/// <summary>A data of an entity, which a criterion compares.</summary>
/// <param name="Comparisons">Those its type allows, "is set" and "is not set" for an optional data only.</param>
/// <param name="DataComparisons">
/// Those its type allows to another data of the same type (<see cref="SearchDataComparison"/>); none for a type
/// that cannot be compared so.
/// </param>
/// <param name="DatePrecision">For a date: the finest precision it is known to (a partial date, a year).</param>
/// <param name="Enumeration">For an enumeration: its name, whose members a client localizes.</param>
/// <param name="Members">For an enumeration: the names of its members, in their business order.</param>
/// <param name="Record">For a record: the entity it is one of.</param>
public sealed record SearchFieldDescription(
    string Key,
    SearchDataType Type,
    bool IsOptional,
    IReadOnlyList<SearchComparison> Comparisons,
    IReadOnlyList<SearchComparison> DataComparisons,
    SearchDatePrecision? DatePrecision = null,
    string? Enumeration = null,
    IReadOnlyList<string>? Members = null,
    EntityKind? Record = null);

/// <summary>A link of the model from an entity to related records (fonctionnel.md § Liens entre entités).</summary>
/// <param name="IsMultiple">Whether it leads to several records, its condition then being quantified.</param>
public sealed record SearchLinkDescription(string Key, EntityKind Target, bool IsMultiple);

/// <summary>Type of a data, which determines its comparisons and how its values are written.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SearchDataType>))]
public enum SearchDataType
{
    Text,
    Number,
    Date,
    Boolean,
    Enumeration,

    /// <summary>A record, compared by identity.</summary>
    Record,

    /// <summary>
    /// An amount with its currency: only whether it is set, the comparison of its value joining the search
    /// with its conversion into euros (suivi-implementation.md, Phase 4).
    /// </summary>
    Amount,
}

/// <summary>The precision a date is known to, or compared at.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SearchDatePrecision>))]
public enum SearchDatePrecision
{
    Year,
    Month,
    Day,
}
