using System.Linq.Expressions;
using Bdtheque.Contracts.Search;
using Microsoft.EntityFrameworkCore.Query;

namespace Bdtheque.Api.Search;

/// <summary>
/// Translates the criteria of an advanced search into the condition of a query on its entity
/// (fonctionnel.md § Recherche avancée): EF Core runs it in the database, links included.
/// </summary>
internal static class SearchTranslator
{
    /// <summary>The condition the records of <paramref name="entity"/> must satisfy; every record without criteria.</summary>
    /// <exception cref="ArgumentException">
    /// Criteria the schema does not describe: a technical error, the client building them from it.
    /// </exception>
    public static Expression<Func<T, bool>> Translate<T>(SearchEntityDefinition entity, SearchCondition? criteria, SearchScope scope)
    {
        var record = Expression.Parameter(typeof(T), "record");
        return Expression.Lambda<Func<T, bool>>(criteria is null ? Expression.Constant(true) : Build(entity, record, criteria, scope), record);
    }

    /// <summary>
    /// Checks that <paramref name="criteria"/> are a search of <paramref name="entity"/>, as their translation
    /// would, without running anything (e.g. criteria saved to be replayed later).
    /// </summary>
    /// <exception cref="ArgumentException">Criteria the schema does not describe.</exception>
    public static void Check(SearchEntityDefinition entity, SearchCondition criteria, SearchScope scope) =>
        _ = Build(entity, Expression.Parameter(entity.ClrType, "record"), criteria, scope);

    /// <summary>Whether the criteria follow a link to or from the universes, which needs the depth of their hierarchy.</summary>
    public static bool FollowsUniverseHierarchy(SearchEntityDefinition entity, SearchCondition? criteria) => criteria switch
    {
        SearchGroup group => group.Conditions.Any(c => FollowsUniverseHierarchy(entity, c)),
        SearchNot not => FollowsUniverseHierarchy(entity, not.Condition),
        SearchLink link => entity.Link(link.Link) is var definition
                           && (definition.FollowsUniverseHierarchy
                               || FollowsUniverseHierarchy(AdvancedSearchSchema.Of(definition.Target), link.Condition)),
        _ => false,
    };

    private static Expression Build(SearchEntityDefinition entity, Expression record, SearchCondition condition, SearchScope scope) =>
        condition switch
        {
            SearchGroup group => Combine(group, group.Conditions.Select(c => Build(entity, record, Required(c), scope)).ToList()),
            SearchNot not => Expression.Not(Build(entity, record, Required(not.Condition), scope)),
            SearchCriterion criterion => entity.Field(criterion.Field).Build(record, criterion),
            SearchLink link => Follow(entity.Link(link.Link), record, link, scope),
            _ => throw new ArgumentException($"Unknown condition {condition.GetType().Name}.", nameof(condition)),
        };

    // Empty, AND holds for every record, and OR for none: the neutral element of each.
    private static Expression Combine(SearchGroup group, IReadOnlyList<Expression> conditions)
    {
        Func<Expression, Expression, Expression> combine = group.Operator switch
        {
            SearchOperator.And => Expression.AndAlso,
            SearchOperator.Or => Expression.OrElse,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group.Operator, "Not an operator."),
        };
        return conditions.Count == 0 ? Expression.Constant(group.Operator == SearchOperator.And) : conditions.Aggregate(combine);
    }

    private static Expression Follow(SearchLinkDefinition definition, Expression record, SearchLink link, SearchScope scope)
    {
        var target = AdvancedSearchSchema.Of(definition.Target);
        var lambda = definition.Related(scope);
        var related = Unwrapped(ReplacingExpressionVisitor.Replace(lambda.Parameters[0], record, lambda.Body));
        var relatedRecord = Expression.Parameter(target.ClrType, "related");
        var condition = link.Condition is null ? null : Expression.Lambda(Build(target, relatedRecord, link.Condition, scope), relatedRecord);

        if (!definition.IsMultiple)
        {
            if (link.Quantifier is not null)
                throw new ArgumentException($"{definition.Key} leads to one record, which no quantifier applies to.", nameof(link));
            // Through a navigation, the record itself; otherwise, a sequence of at most one.
            if (related.Type == target.ClrType)
                return Expression.AndAlso(
                    Expression.NotEqual(related, Expression.Constant(null, related.Type)),
                    condition is null ? Expression.Constant(true) : ReplacingExpressionVisitor.Replace(relatedRecord, related, condition.Body));
            return Call(nameof(Enumerable.Any), related, target.ClrType, condition);
        }

        var quantifier = link.Quantifier
                         ?? throw new ArgumentException($"{definition.Key} leads to several records, which a quantifier must count.", nameof(link));
        return quantifier.Kind switch
        {
            SearchQuantifierKind.Any => Call(nameof(Enumerable.Any), related, target.ClrType, condition),
            SearchQuantifierKind.None => Expression.Not(Call(nameof(Enumerable.Any), related, target.ClrType, condition)),
            // All of them, and at least one: never without any related record (fonctionnel.md § Liens entre entités).
            SearchQuantifierKind.All => Expression.AndAlso(
                Call(nameof(Enumerable.Any), related, target.ClrType, null),
                condition is null ? Expression.Constant(true) : Call(nameof(Enumerable.All), related, target.ClrType, condition)),
            SearchQuantifierKind.Count => CompareCount(Call(nameof(Enumerable.Count), related, target.ClrType, condition), quantifier),
            _ => throw new ArgumentOutOfRangeException(nameof(link), quantifier.Kind, "Not a quantifier."),
        };
    }

    private static Expression CompareCount(Expression count, SearchQuantifier quantifier)
    {
        var comparison = quantifier.Comparison is { } c && NumberField.NumberComparisons.Contains(c)
            ? c
            : throw new ArgumentException($"A number of related records is compared as a number, not by {quantifier.Comparison}.", nameof(quantifier));
        return NumberField.CompareNumber(count, comparison, SearchValues.Of(comparison, quantifier.Values));
    }

    // Any, All or Count of the related records: those of Queryable on a query (the related records reached
    // without navigation), of Enumerable on a navigation.
    private static Expression Call(string method, Expression related, Type type, LambdaExpression? condition)
    {
        var isQuery = typeof(IQueryable<>).MakeGenericType(type).IsAssignableFrom(related.Type);
        var declaringType = isQuery ? typeof(Queryable) : typeof(Enumerable);
        Expression[] arguments = condition is null
            ? [related]
            : [related, isQuery ? Expression.Quote(condition) : condition];
        return Expression.Call(declaringType, method, [type], arguments);
    }

    // A query declared as a sequence carries a conversion its methods do not need.
    private static Expression Unwrapped(Expression related) =>
        related is UnaryExpression { NodeType: ExpressionType.Convert } conversion && typeof(IQueryable).IsAssignableFrom(conversion.Operand.Type)
            ? conversion.Operand
            : related;

    private static SearchCondition Required(SearchCondition? condition) =>
        condition ?? throw new ArgumentException("A combination without its condition.", nameof(condition));
}
