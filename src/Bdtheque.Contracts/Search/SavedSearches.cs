using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Search;

/// <summary>A saved search, as the consultation offers it to be replayed (fonctionnel.md § Recherches enregistrées).</summary>
/// <param name="Entity">The entity the search returns.</param>
public sealed record SavedSearchListItem(Guid Id, string Name, EntityKind Entity);

/// <summary>
/// A saved search, with what its replay asks and what preloads the form of a new search: its criteria,
/// with no link kept to the saved search.
/// </summary>
/// <param name="Criteria">The criteria, each variable at its default value.</param>
public sealed record SavedSearchDetail(Guid Id, string Name, EntityKind Entity, SearchCondition Criteria, IReadOnlyList<SavedSearchVariable> Variables);

/// <summary>A variable of a saved search, whose value is asked at replay.</summary>
/// <param name="Location">JSON pointer (RFC 6901) to the constant of the criteria it replaces.</param>
/// <param name="DefaultValue">The constant it replaces, written as the values of the criteria.</param>
public sealed record SavedSearchVariable(string Name, string Location, string DefaultValue);

/// <summary>The replay of a saved search, whose results are paginated by the query of the request.</summary>
/// <param name="Values">
/// The value of each variable given one, by its name, written as the values of the criteria; the others
/// keep their default value.
/// </param>
public sealed record ReplaySavedSearchRequest(IReadOnlyDictionary<string, string>? Values);
