using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Search;

namespace Bdtheque.Contracts.Admin;

/// <summary>A saved search as its administration form edits it (fonctionnel/recherche-avancee.md § Recherches enregistrées).</summary>
/// <param name="Version">Version of the search, sent back with any modification or deletion.</param>
public sealed record SavedSearchForm(Guid Id, SavedSearchContent Content, uint Version);

/// <summary>
/// Everything the administrator edits on a saved search, sent to create it, and sent whole to modify it:
/// renaming it, changing its criteria, or the default values of its variables.
/// </summary>
/// <param name="Name">Under which the search is offered in the consultation, unique ignoring case and accents.</param>
/// <param name="Entity">The entity the search returns, whose schema the criteria follow.</param>
/// <param name="Criteria">
/// What the records must satisfy, as for an advanced search; its constants are the default values of the
/// variables that replace them.
/// </param>
/// <param name="Variables">The constants of the criteria replaced by a variable, whose value is asked at replay.</param>
public sealed record SavedSearchContent(
    string Name, EntityKind Entity, SearchCondition Criteria, IReadOnlyList<SearchVariableContent> Variables);

/// <summary>A constant of the criteria replaced by a variable.</summary>
/// <param name="Name">What the value asked at replay is named after, unique in the search ignoring case and accents.</param>
/// <param name="Location">
/// JSON pointer (RFC 6901) to the constant in the criteria, as they are written in JSON: one of the
/// <c>values</c> of a criterion or of a quantifier, e.g. <c>/conditions/1/values/0</c>.
/// </param>
public sealed record SearchVariableContent(string Name, string Location);

/// <param name="Version">Version of the search the form was read at.</param>
public sealed record UpdateSavedSearchRequest(SavedSearchContent Content, uint Version);
