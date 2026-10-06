using System.Linq.Expressions;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Expressions shared by the lists and records of the consultation — what the labels of an album and of
/// an edition are built from, the membership of the collection, the purchase intents, the order of the albums —, written once and
/// inserted into each query (choix-implementation.md § Recherche).
/// </summary>
/// <remarks>
/// EF Core translates an expression tree, never the call of a C# method: a query calls the shared
/// expressions through the marker methods below, which <see cref="Expand{TSource, TResult}"/> replaces
/// by the expression they stand for.
/// </remarks>
internal static class CatalogExpressions
{
    /// <summary>Marker of <see cref="CollectionMembership.IsOwned"/>, for <see cref="Expand{TSource, TResult}"/> only.</summary>
    public static bool IsOwned(this Edition edition) => throw NotExpanded();

    /// <summary>Marker of a purchase intent as presented, for <see cref="Expand{TSource, TResult}"/> only.</summary>
    public static PurchaseIntentItem ToItem(this PurchaseIntent intent) => throw NotExpanded();

    /// <summary>Marker of the purchase intent targeting the edition, if any, for <see cref="Expand{TSource, TResult}"/> only.</summary>
    public static PurchaseIntentItem? PurchaseIntentOf(this Edition edition) => throw NotExpanded();

    /// <summary>Marker of the entry of a universe, for <see cref="Expand{TSource, TResult}"/> only.</summary>
    public static UniverseListItem ToListItem(this Universe universe) => throw NotExpanded();

    /// <summary>Marker of contributions as presented, by role then by author, for <see cref="Expand{TSource, TResult}"/> only.</summary>
    public static IEnumerable<ContributionItem> ToItems(this IEnumerable<Contribution> contributions) => throw NotExpanded();

    /// <summary>Marker of the summary of an album, for <see cref="Expand{TSource, TResult}"/> only.</summary>
    public static AlbumSummary ToSummary(this Album album) => throw NotExpanded();

    /// <summary>Marker of the summary of an edition, for <see cref="Expand{TSource, TResult}"/> only.</summary>
    public static EditionSummary ToSummary(this Edition edition) => throw NotExpanded();

    private static readonly Expression<Func<Album, AlbumSummary>> AlbumSummaryOf = a => new AlbumSummary(
        a.Id,
        a.Title,
        a.SeriesId,
        a.Series != null ? a.Series.Title : null,
        EnumMapping.Map<ContractEnums.AlbumType>(a.Type)!.Value,
        a.IsSpecialIssue,
        a.VolumeNumber,
        a.StartVolumeNumber,
        a.EndVolumeNumber,
        a.Editions.Any(e => e.IsOwned()));

    private static readonly Expression<Func<Edition, EditionSummary>> EditionSummaryOf = e => new EditionSummary(
        e.Id,
        e.PublisherId,
        e.Publisher.Name,
        e.PublisherCollectionId,
        e.PublisherCollection != null ? e.PublisherCollection.Name : null,
        e.PublicationYear,
        e.Isbn);

    private static readonly Expression<Func<Universe, UniverseListItem>> UniverseListItemOf =
        u => new UniverseListItem(u.Id, u.Name, u.ParentId, u.Parent != null ? u.Parent.Name : null);

    private static readonly Expression<Func<IEnumerable<Contribution>, IEnumerable<ContributionItem>>> ContributionItemsOf =
        contributions => contributions
            .OrderBy(c => c.Role)
            .ThenBy(c => c.Author.SortKey)
            .ThenBy(c => c.Id)
            .Select(c => new ContributionItem(
                new AuthorListItem(c.AuthorId, c.Author.LastName, c.Author.FirstName, c.Author.Pseudonym),
                EnumMapping.Map<ContractEnums.ContributionRole>(c.Role)!.Value,
                c.CreatedAt,
                c.ModifiedAt));

    private static readonly Expression<Func<PurchaseIntent, PurchaseIntentItem>> PurchaseIntentItemOf =
        p => new PurchaseIntentItem(p.Id, p.CreatedAt, p.ModifiedAt);

    // The intents of an album carry its identifier, those on its editions included.
    private static readonly Expression<Func<Edition, PurchaseIntentItem?>> PurchaseIntentOfEdition =
        e => e.Album.PurchaseIntents.Where(p => p.EditionId == e.Id).Select(p => p.ToItem()).FirstOrDefault();

    /// <summary>Replaces every marker method of <paramref name="expression"/> by the expression it stands for.</summary>
    public static Expression<Func<TSource, TResult>> Expand<TSource, TResult>(Expression<Func<TSource, TResult>> expression) =>
        (Expression<Func<TSource, TResult>>)new MarkerExpander().Visit(expression);

    /// <summary>
    /// Sorts in the order of the albums: by sort key, that of the series standing in for an album without
    /// a title of its own (modele-metier.md § Album), then by volume, then by album for a total order.
    /// </summary>
    public static IOrderedQueryable<T> OrderByAlbum<T>(this IQueryable<T> source, Expression<Func<T, Album>> album) =>
        source
            .OrderBy(Of(album, a => a.SortKey ?? a.Series!.SortKey))
            .ThenBy(Of(album, a => a.VolumeNumber))
            .ThenBy(Of(album, a => a.Id));

    /// <summary>
    /// Sorts the albums of a series in its order (fonctionnel.md § Ordre des albums dans une série): the
    /// albums not special issues first, then the special issues; in each, by volume — its first one for
    /// an omnibus —, failing which by first publication, the albums without volume coming after those
    /// with one (PostgreSQL sorts nulls last); then by sort key and album, for a total order.
    /// </summary>
    public static IOrderedQueryable<Album> OrderInSeries(this IQueryable<Album> albums) =>
        albums
            .OrderBy(a => a.IsSpecialIssue)
            .ThenBy(a => a.Type == DomainEnums.AlbumType.Omnibus ? a.StartVolumeNumber : a.VolumeNumber)
            .ThenBy(a => a.FirstPublicationYear)
            .ThenBy(a => a.FirstPublicationMonth)
            .ThenBy(a => a.SortKey)
            .ThenBy(a => a.Id);

    private static Expression<Func<T, TKey>> Of<T, TKey>(Expression<Func<T, Album>> album, Expression<Func<Album, TKey>> key) =>
        Expression.Lambda<Func<T, TKey>>(ReplacingExpressionVisitor.Replace(key.Parameters[0], album.Body, key.Body), album.Parameters);

    private static InvalidOperationException NotExpanded() =>
        new($"A marker of {nameof(CatalogExpressions)}, only meaningful in an expression passed to {nameof(Expand)}.");

    private sealed class MarkerExpander : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            LambdaExpression? expression = node.Method.DeclaringType != typeof(CatalogExpressions) ? null : node.Method.Name switch
            {
                nameof(IsOwned) => CollectionMembership.IsOwned,
                nameof(ToItem) => PurchaseIntentItemOf,
                nameof(PurchaseIntentOf) => PurchaseIntentOfEdition,
                nameof(ToListItem) => UniverseListItemOf,
                nameof(ToItems) => ContributionItemsOf,
                nameof(ToSummary) when node.Method.ReturnType == typeof(AlbumSummary) => AlbumSummaryOf,
                nameof(ToSummary) => EditionSummaryOf,
                _ => null,
            };
            if (expression is null)
                return base.VisitMethodCall(node);

            // Visited again: a shared expression may itself call markers (the summary of an album, the
            // membership of its editions).
            return Visit(ReplacingExpressionVisitor.Replace(expression.Parameters[0], Visit(node.Arguments[0]), expression.Body));
        }
    }
}
