using Bdtheque.Contracts.Search;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Search;

/// <summary>
/// The records criteria designate (fonctionnel/regles-metier/suppression-des-entites.md, line « Toute fiche »): the values of
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

    /// <summary>
    /// The same criteria, every value designating the record <paramref name="merged"/> of <paramref name="kind"/> designating
    /// <paramref name="kept"/> instead, at the same place, as the merge of two records does
    /// (fonctionnel/regles-metier/fusion-des-fiches.md): the variables of a saved search keep designating their constant.
    /// </summary>
    public static SearchCondition Replace(SearchEntityDefinition entity, SearchCondition criteria, EntityKind kind, Guid merged, Guid kept) =>
        Rewrite(entity, criteria, kind, merged, kept)!;

    private static SearchCondition? Rewrite(SearchEntityDefinition entity, SearchCondition? condition, EntityKind kind, Guid merged, Guid kept) =>
        condition switch
        {
            SearchGroup group => group with { Conditions = [.. group.Conditions.Select(c => Rewrite(entity, c, kind, merged, kept)!)] },
            SearchNot not => not with { Condition = Rewrite(entity, not.Condition, kind, merged, kept)! },
            SearchLink link => link with
            {
                Condition = Rewrite(AdvancedSearchSchema.Of(entity.Link(link.Link).Target), link.Condition, kind, merged, kept),
            },
            SearchCriterion criterion when entity.Field(criterion.Field) is RecordField field && field.Record == kind && criterion.Values is { } values =>
                criterion with { Values = [.. values.Select(v => Guid.Parse(v) == merged ? kept.ToString() : v)] },
            _ => condition,
        };

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
