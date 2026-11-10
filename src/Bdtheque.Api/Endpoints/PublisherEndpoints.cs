using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Administration of the publishers and of their collections (<c>/admin/publishers</c>). A
/// collection belongs to the aggregate of its publisher: every write on it locks the publisher and
/// checks the version of the publisher (choix-implementation/persistance/concurrence-d-acces.md).
/// </summary>
internal static class PublisherEndpoints
{
    // fonctionnel/regles-metier/suppression-des-entites.md: a publisher is refused deletion while editions or
    // series templates reference it, and its collections are deleted with it.
    private static readonly DeletionLink[] PublisherDeletionLinks =
    [
        new(LinkNature.Reference, EntityKind.Edition,
            (context, id) => context.Editions.Where(e => e.PublisherId == id).Select(e => e.Id)),
        new(LinkNature.Reference, EntityKind.Series,
            (context, id) => context.Series.Where(s => s.TemplatePublisherId == id).Select(s => s.Id)),
        new(LinkNature.Composition, EntityKind.PublisherCollection,
            (context, id) => context.Set<PublisherCollection>().Where(c => c.PublisherId == id).Select(c => c.Id)),
    ];

    // A collection is refused deletion while editions or series templates reference it.
    private static readonly DeletionLink[] CollectionDeletionLinks =
    [
        new(LinkNature.Reference, EntityKind.Edition,
            (context, id) => context.Editions.Where(e => e.PublisherCollectionId == id).Select(e => e.Id)),
        new(LinkNature.Reference, EntityKind.Series,
            (context, id) => context.Series.Where(s => s.TemplatePublisherCollectionId == id).Select(s => s.Id)),
    ];

    public static void MapPublishers(this RouteGroupBuilder admin)
    {
        var publishers = admin.MapGroup("/publishers");
        publishers.MapGet("/", ListForAdministrationAsync);
        publishers.MapGet("/{id:guid}", GetAsync);
        publishers.MapPost("/", CreateAsync);
        publishers.MapPut("/{id:guid}", UpdateAsync);
        // Its collections are records of their own, whose deletion the change log records.
        publishers.MapDeletion<Publisher>(PublisherDeletionLinks, query => query.Include(p => p.Collections));

        // `rootId` is the name the common child deletion expects for the identifier of the root.
        var collections = publishers.MapGroup("/{rootId:guid}/collections");
        collections.MapGet("/", ListCollectionsAsync);
        collections.MapGet("/{id:guid}", GetCollectionAsync);
        collections.MapPost("/", CreateCollectionAsync);
        collections.MapPut("/{id:guid}", UpdateCollectionAsync);
        collections.MapChildDeletion<Publisher, PublisherCollection>(
            query => query.Include(p => p.Collections), publisher => publisher.Collections, CollectionDeletionLinks);
    }

    // A publisher and its collections have no record (fonctionnel/structure-de-l-application.md): a
    // publisher leads to its website, its editions are listed by the cross filters of the lists.
    public static void MapPublisherCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/publishers", ListAsync);
        catalog.MapGet("/publisher-collections", ListAllCollectionsAsync);
    }

    private static Task<Page<PublisherListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, int page = 1, int pageSize = Paging.DefaultSize) =>
        context.Publishers.AsNoTracking()
            .WhereContains(q, p => p.Name)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToPageAsync(page, pageSize, p => new PublisherListItem(p.Id, p.Name, p.Website), cancellationToken);

    /// <summary>
    /// The list of the publishers of the administration (visuel/listes/liste-par-liste.md): the search on the name and the
    /// order of the list of the consultation. Each row carries the website of the publisher, its collections, its editions
    /// in the collection, its modification and its version, which its deletion sends back.
    /// </summary>
    private static Task<Page<PublisherAdminListItem>> ListForAdministrationAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, int page = 1, int pageSize = Paging.DefaultSize) =>
        context.Publishers.AsNoTracking()
            .WhereContains(q, p => p.Name)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToPageAsync(page, pageSize, CatalogExpressions.Expand((Publisher p) => new PublisherAdminListItem(
                p.Id,
                p.Name,
                p.Website,
                p.Collections.OrderBy(c => c.Name).ThenBy(c => c.Id).Select(c => c.Name).ToList(),
                context.Editions.Count(e => e.PublisherId == p.Id && e.IsOwned()),
                p.ModifiedAt,
                EF.Property<uint>(p, BdthequeDbContext.VersionProperty))), cancellationToken);

    /// <summary>The collections of all publishers, or of one of them (choix-implementation/api/recherche.md).</summary>
    private static Task<Page<PublisherCollectionListItem>> ListAllCollectionsAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, Guid? publisherId = null, int page = 1,
        int pageSize = Paging.DefaultSize)
    {
        var collections = context.PublisherCollections.AsNoTracking().WhereContains(q, c => c.Name);
        if (publisherId is not null)
            collections = collections.Where(c => c.PublisherId == publisherId);

        return collections
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Publisher.Name)
            .ThenBy(c => c.Id)
            .ToPageAsync(page, pageSize, c => new PublisherCollectionListItem(c.Id, c.Name, c.PublisherId, c.Publisher.Name), cancellationToken);
    }

    private static async Task<PublisherForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // Tracked, for the version of the aggregate (BdthequeDbContext.VersionOf): the context lives for this request only.
        var publisher = await context.Publishers.Include(p => p.Collections).SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(Publisher), id);
        return await ToFormAsync(context, publisher, cancellationToken);
    }

    private static async Task<Created<PublisherForm>> CreateAsync(
        PublisherContent request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var publisher = new Publisher(request.Name);
        await ApplyAsync(context, publisher, request, cancellationToken);
        context.Publishers.Add(publisher);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/publishers/{publisher.Id}", await ToFormAsync(context, publisher, cancellationToken));
    }

    private static async Task<PublisherForm> UpdateAsync(
        Guid id, UpdatePublisherRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var publisher = await context.LoadAggregateForWriteAsync<Publisher>(
            id, request.Version, query => query.Include(p => p.Collections), cancellationToken);
        await ApplyAsync(context, publisher, request.Content, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToFormAsync(context, publisher, cancellationToken);
    }

    /// <summary>
    /// Writes the content of the form on the publisher, its collections replaced by those listed
    /// (choix-implementation/api/organisation-de-l-api.md): a collection listed is renamed, one added created, and one of
    /// the publisher's left out removed, provided nothing references it.
    /// </summary>
    /// <exception cref="EntityNotFoundException">A collection listed is not one of the publisher's.</exception>
    /// <exception cref="DomainRuleViolationException">
    /// A collection removed is referenced (<see cref="DomainRules.PublisherCollectionInUse"/>), or a value breaks a rule of the
    /// publisher or of its collections.
    /// </exception>
    private static async Task ApplyAsync(
        BdthequeDbContext context, Publisher publisher, PublisherContent content, CancellationToken cancellationToken)
    {
        publisher.SetName(content.Name);
        publisher.SetWebsite(content.Website);

        var listed = content.Collections.Where(c => c.Id is not null).ToDictionary(c => c.Id!.Value, c => c.Name);
        if (listed.Keys.Cast<Guid?>().FirstOrDefault(id => publisher.Collections.All(c => c.Id != id)) is { } unknown)
            throw new EntityNotFoundException(typeof(PublisherCollection), unknown);

        foreach (var collection in publisher.Collections.ToList())
        {
            if (listed.TryGetValue(collection.Id, out var name))
                collection.SetName(name);
            else
                await RemoveCollectionAsync(context, publisher, collection, cancellationToken);
        }

        foreach (var added in content.Collections.Where(c => c.Id is null))
            publisher.AddCollection(added.Name);
    }

    // A collection is removed as it would be deleted (fonctionnel/regles-metier/suppression-des-entites.md): refused while
    // editions, series templates or saved searches reference it, whose count is that of its deletion. Its row is locked
    // first, after the publisher's (choix-implementation/persistance/concurrence-d-acces.md): an edition or a series that
    // would reference it meanwhile waits for the end of the save, which no reference can then slip into.
    private static async Task RemoveCollectionAsync(
        BdthequeDbContext context, Publisher publisher, PublisherCollection collection, CancellationToken cancellationToken)
    {
        await context.LockAsync<PublisherCollection>(collection.Id, cancellationToken);
        var impact = await AggregateDeletion.ComputeImpactAsync<PublisherCollection>(
            context, collection.Id, CollectionDeletionLinks, cancellationToken);
        if (impact.BlockedBy.Count > 0)
            throw new DomainRuleViolationException(
                DomainRules.PublisherCollectionInUse, $"Collection {collection.Id} is referenced and cannot be removed.");

        publisher.RemoveCollection(collection);
    }

    // Its collections in the order of the base, by name, each with what references it: the editions and the series templates
    // its deletion counts (CollectionDeletionLinks), editions targeted by a purchase intent included.
    private static async Task<PublisherForm> ToFormAsync(BdthequeDbContext context, Publisher publisher, CancellationToken cancellationToken) =>
        new(
            publisher.Id,
            publisher.Name,
            publisher.Website,
            await context.PublisherCollections.AsNoTracking()
                .Where(c => c.PublisherId == publisher.Id)
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Id)
                .Select(c => new PublisherCollectionUse(
                    c.Id,
                    c.Name,
                    context.Editions.Count(e => e.PublisherCollectionId == c.Id),
                    context.Series.Count(s => s.TemplatePublisherCollectionId == c.Id)))
                .ToListAsync(cancellationToken),
            publisher.CreatedAt,
            publisher.ModifiedAt,
            context.VersionOf(publisher));

    private static async Task<List<PublisherCollectionForm>> ListCollectionsAsync(
        Guid rootId, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var publisher = await context.Publishers
            .Where(p => p.Id == rootId)
            .Select(p => new
            {
                Version = EF.Property<uint>(p, BdthequeDbContext.VersionProperty),
                Collections = p.Collections.OrderBy(c => c.Name).Select(c => new { c.Id, c.Name }).ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Publisher), rootId);
        return publisher.Collections.Select(c => new PublisherCollectionForm(c.Id, rootId, c.Name, publisher.Version)).ToList();
    }

    private static async Task<PublisherCollectionForm> GetCollectionAsync(
        Guid rootId, Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.Set<PublisherCollection>()
            .Where(c => c.Id == id && c.PublisherId == rootId)
            .Select(c => new PublisherCollectionForm(
                c.Id, c.PublisherId, c.Name, EF.Property<uint>(c.Publisher, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(PublisherCollection), id);

    private static async Task<Created<PublisherCollectionForm>> CreateCollectionAsync(
        Guid rootId, CreatePublisherCollectionRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var publisher = await context.LoadAggregateForWriteAsync<Publisher>(
            rootId, request.PublisherVersion, query => query.Include(p => p.Collections), cancellationToken);
        var collection = publisher.AddCollection(request.Name);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created($"/admin/publishers/{rootId}/collections/{collection.Id}", ToForm(context, publisher, collection));
    }

    private static async Task<PublisherCollectionForm> UpdateCollectionAsync(
        Guid rootId, Guid id, UpdatePublisherCollectionRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var publisher = await context.LoadAggregateForWriteAsync<Publisher>(
            rootId, request.PublisherVersion, query => query.Include(p => p.Collections), cancellationToken);
        var collection = publisher.Collections.SingleOrDefault(c => c.Id == id)
                         ?? throw new EntityNotFoundException(typeof(PublisherCollection), id);
        collection.SetName(request.Name);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, publisher, collection);
    }

    private static PublisherCollectionForm ToForm(BdthequeDbContext context, Publisher publisher, PublisherCollection collection) =>
        new(collection.Id, publisher.Id, collection.Name, context.VersionOf(publisher));
}
