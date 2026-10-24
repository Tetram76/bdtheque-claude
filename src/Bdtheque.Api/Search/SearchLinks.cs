using System.Linq.Expressions;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Search;

/// <summary>
/// What a search is translated in: the context, which the links without navigation query, the depth of the
/// hierarchy of the universes, which the links to universes follow, and the time zone of the dates and times.
/// </summary>
/// <param name="UniverseAncestorLevels">The most ancestors a universe has.</param>
/// <param name="TimeZone">The time zone in which a date and time is compared by its day (<see cref="InstantField"/>).</param>
internal sealed record SearchScope(BdthequeDbContext Context, int UniverseAncestorLevels, TimeZoneInfo TimeZone);

/// <summary>A link of the model from an entity to related records (fonctionnel/recherche-avancee.md § Liens entre entités).</summary>
/// <param name="Related">
/// The related records of a record, built in the scope of the search: the record itself for a link to one
/// record through a navigation, or a sequence otherwise (of at most one record for a link to one record).
/// </param>
/// <param name="FollowsUniverseHierarchy">Whether <paramref name="Related"/> needs the depth of the hierarchy of the universes.</param>
internal sealed record SearchLinkDefinition(
    string Key, EntityKind Target, bool IsMultiple, Func<SearchScope, LambdaExpression> Related, bool FollowsUniverseHierarchy = false)
{
    public SearchLinkDescription Describe() => new(Key, Target, IsMultiple);
}

/// <summary>
/// Links through the associations of a record with genres or universes (modele-metier.md § Relations et
/// cardinalités), in both directions: a record is attached to a genre or a universe directly, or through
/// another record (an album through its series, fonctionnel/regles-metier.md § Genres et univers d'un album), and to a
/// universe, to its ancestors too (fonctionnel/regles-metier.md § Hiérarchie des univers).
/// </summary>
internal static class SearchAttachments
{
    /// <summary>The genres a record is attached to, through any of <paramref name="attached"/>.</summary>
    public static Func<SearchScope, LambdaExpression> GenresOf<TRecord>(params Expression<Func<TRecord, IEnumerable<Genre>>>[] attached) =>
        scope => Targets(scope, attached, Same<Genre>());

    /// <summary>The records attached to a genre, through any of <paramref name="attached"/>.</summary>
    public static Func<SearchScope, LambdaExpression> RecordsOfGenre<TRecord>(params Expression<Func<TRecord, IEnumerable<Genre>>>[] attached)
        where TRecord : class =>
        scope => Records(scope, attached, Same<Genre>());

    /// <summary>The universes a record is attached to, with their ancestors, through any of <paramref name="attached"/>.</summary>
    public static Func<SearchScope, LambdaExpression> UniversesOf<TRecord>(params Expression<Func<TRecord, IEnumerable<Universe>>>[] attached) =>
        scope => Targets(scope, attached, AncestorOrSelf(scope.UniverseAncestorLevels));

    /// <summary>The records attached to a universe or to one of its descendants, through any of <paramref name="attached"/>.</summary>
    public static Func<SearchScope, LambdaExpression> RecordsOfUniverse<TRecord>(params Expression<Func<TRecord, IEnumerable<Universe>>>[] attached)
        where TRecord : class =>
        scope => Records(scope, attached, AncestorOrSelf(scope.UniverseAncestorLevels));

    // record => targets.Where(target => record is attached to target)
    private static LambdaExpression Targets<TRecord, TTarget>(
        SearchScope scope, Expression<Func<TRecord, IEnumerable<TTarget>>>[] attached, Expression<Func<TTarget, TTarget, bool>> covers)
        where TTarget : class
    {
        var record = Expression.Parameter(typeof(TRecord), "record");
        var target = Expression.Parameter(typeof(TTarget), "target");
        var where = Expression.Lambda<Func<TTarget, bool>>(IsAttached(record, target, attached, covers), target);
        return Expression.Lambda<Func<TRecord, IEnumerable<TTarget>>>(
            Expression.Call(typeof(Queryable), nameof(Queryable.Where), [typeof(TTarget)], SetOf<TTarget>(scope), Expression.Quote(where)),
            record);
    }

    // target => records.Where(record => record is attached to target)
    private static LambdaExpression Records<TRecord, TTarget>(
        SearchScope scope, Expression<Func<TRecord, IEnumerable<TTarget>>>[] attached, Expression<Func<TTarget, TTarget, bool>> covers)
        where TRecord : class
    {
        var record = Expression.Parameter(typeof(TRecord), "record");
        var target = Expression.Parameter(typeof(TTarget), "target");
        var where = Expression.Lambda<Func<TRecord, bool>>(IsAttached(record, target, attached, covers), record);
        return Expression.Lambda<Func<TTarget, IEnumerable<TRecord>>>(
            Expression.Call(typeof(Queryable), nameof(Queryable.Where), [typeof(TRecord)], SetOf<TRecord>(scope), Expression.Quote(where)),
            target);
    }

    // attached₁(record).Any(a => covers(target, a)) || attached₂(record).Any(…) || …
    private static Expression IsAttached<TRecord, TTarget>(
        ParameterExpression record, ParameterExpression target, Expression<Func<TRecord, IEnumerable<TTarget>>>[] attached,
        Expression<Func<TTarget, TTarget, bool>> covers)
    {
        var associated = Expression.Parameter(typeof(TTarget), "associated");
        var covered = Expression.Lambda<Func<TTarget, bool>>(
            ReplacingExpressionVisitor.Replace(covers.Parameters[1], associated,
                ReplacingExpressionVisitor.Replace(covers.Parameters[0], target, covers.Body)),
            associated);
        return attached
            .Select(a => (Expression)Expression.Call(
                typeof(Enumerable), nameof(Enumerable.Any), [typeof(TTarget)],
                ReplacingExpressionVisitor.Replace(a.Parameters[0], record, a.Body), covered))
            .Aggregate(Expression.OrElse);
    }

    private static Expression<Func<T, T, bool>> Same<T>() where T : EntityBase => (target, associated) => associated.Id == target.Id;

    // The universe associated with the record is the target or one of its descendants, down to the depth of
    // the hierarchy: associated.Id == target.Id || associated.ParentId == target.Id
    // || associated.Parent.ParentId == target.Id || …, without a recursive query (the hierarchy of the
    // universes is read whole elsewhere too, choix-implementation/api.md § Recherche).
    private static Expression<Func<Universe, Universe, bool>> AncestorOrSelf(int ancestorLevels)
    {
        var target = Expression.Parameter(typeof(Universe), "target");
        var associated = Expression.Parameter(typeof(Universe), "associated");
        var targetId = Expression.Property(target, nameof(Universe.Id));
        Expression condition = Expression.Equal(Expression.Property(associated, nameof(Universe.Id)), targetId);
        Expression ancestor = associated;
        for (var level = 0; level < ancestorLevels; level++)
        {
            condition = Expression.OrElse(
                condition,
                Expression.Equal(Expression.Property(ancestor, nameof(Universe.ParentId)), Expression.Convert(targetId, typeof(Guid?))));
            ancestor = Expression.Property(ancestor, nameof(Universe.Parent));
        }
        return Expression.Lambda<Func<Universe, Universe, bool>>(condition, target, associated);
    }

    // context.Set<T>(), from the scope captured as a constant: EF Core inlines it as the root of a subquery.
    private static Expression SetOf<T>(SearchScope scope) where T : class =>
        Expression.Call(
            Expression.Property(Expression.Constant(scope), nameof(SearchScope.Context)),
            nameof(BdthequeDbContext.Set), [typeof(T)]);
}
