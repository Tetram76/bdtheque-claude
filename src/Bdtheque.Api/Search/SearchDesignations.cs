using Bdtheque.Contracts.Search;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Search;

/// <summary>
/// The records criteria designate (fonctionnel.md § Suppression des entités, line « Toute fiche »): the values of
/// the criteria comparing a record, at every level of the links they follow, whose entity only the schema tells.
/// </summary>
internal static class SearchDesignations
{
    /// <summary>The records <paramref name="criteria"/> of <paramref name="entity"/> designate, in their order, each once.</summary>
    /// <remarks>
    /// The criteria are read as <see cref="SearchTranslator.Check"/> has accepted them: the values of a record are
    /// identifiers.
    /// </remarks>
    public static IReadOnlyList<(EntityKind Kind, Guid Id)> Of(SearchEntityDefinition entity, SearchCondition criteria) =>
        Read(entity, criteria).Distinct().ToList();

    private static IEnumerable<(EntityKind Kind, Guid Id)> Read(SearchEntityDefinition entity, SearchCondition? condition) => condition switch
    {
        SearchGroup group => group.Conditions.SelectMany(c => Read(entity, c)),
        SearchNot not => Read(entity, not.Condition),
        SearchLink link => Read(AdvancedSearchSchema.Of(entity.Link(link.Link).Target), link.Condition),
        SearchCriterion criterion when entity.Field(criterion.Field) is RecordField field =>
            (criterion.Values ?? []).Select(value => (field.Record, Guid.Parse(value))),
        _ => [],
    };
}
