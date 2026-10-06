using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Administration of the publishers and of their collections (<c>/admin/publishers</c>). A
/// collection belongs to the aggregate of its publisher: every write on it locks the publisher and
/// checks the version of the publisher (choix-implementation.md § Concurrence d'accès).
/// </summary>
internal static class PublisherEndpoints
{
    // fonctionnel.md § Suppression des entités: a publisher is refused deletion while editions or
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
        publishers.MapGet("/{id:guid}", GetAsync);
        publishers.MapPost("/", CreateAsync);
        publishers.MapPut("/{id:guid}", UpdateAsync);
        publishers.MapDeletion<Publisher>(PublisherDeletionLinks);

        // `rootId` is the name the common child deletion expects for the identifier of the root.
        var collections = publishers.MapGroup("/{rootId:guid}/collections");
        collections.MapGet("/", ListCollectionsAsync);
        collections.MapGet("/{id:guid}", GetCollectionAsync);
        collections.MapPost("/", CreateCollectionAsync);
        collections.MapPut("/{id:guid}", UpdateCollectionAsync);
        collections.MapChildDeletion<Publisher, PublisherCollection>(
            query => query.Include(p => p.Collections), publisher => publisher.Collections, CollectionDeletionLinks);
    }

    public static void MapPublisherCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/publishers", ListAsync);
        catalog.MapGet("/publishers/{id:guid}", GetDetailAsync);
        catalog.MapGet("/publisher-collections", ListAllCollectionsAsync);
        catalog.MapGet("/publisher-collections/{id:guid}", GetCollectionDetailAsync);
    }

    private static async Task<PublisherDetail> GetDetailAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.Publishers.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PublisherDetail(
                p.Id,
                p.Name,
                p.Website,
                p.Collections.OrderBy(c => c.Name).ThenBy(c => c.Id)
                    .Select(c => new PublisherCollectionListItem(c.Id, c.Name, p.Id, p.Name)).ToList(),
                p.CreatedAt,
                p.ModifiedAt))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Publisher), id);

    private static async Task<PublisherCollectionDetail> GetCollectionDetailAsync(
        Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.PublisherCollections.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new PublisherCollectionDetail(c.Id, c.Name, c.PublisherId, c.Publisher.Name, c.CreatedAt, c.ModifiedAt))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(PublisherCollection), id);

    private static Task<Page<PublisherListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, int page = 1, int pageSize = Paging.DefaultSize) =>
        context.Publishers.AsNoTracking()
            .WhereContains(q, p => p.Name)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToPageAsync(page, pageSize, p => new PublisherListItem(p.Id, p.Name), cancellationToken);

    /// <summary>The collections of all publishers, or of one of them (choix-implementation.md § Recherche).</summary>
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

    private static async Task<PublisherForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.Publishers
            .Where(p => p.Id == id)
            .Select(p => new PublisherForm(p.Id, p.Name, p.Website, EF.Property<uint>(p, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Publisher), id);

    private static async Task<Created<PublisherForm>> CreateAsync(
        CreatePublisherRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var publisher = new Publisher(request.Name);
        publisher.SetWebsite(request.Website);
        context.Publishers.Add(publisher);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/publishers/{publisher.Id}", ToForm(context, publisher));
    }

    private static async Task<PublisherForm> UpdateAsync(
        Guid id, UpdatePublisherRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var publisher = await context.LoadAggregateForWriteAsync<Publisher>(id, request.Version, cancellationToken: cancellationToken);
        publisher.SetName(request.Name);
        publisher.SetWebsite(request.Website);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, publisher);
    }

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

    private static PublisherForm ToForm(BdthequeDbContext context, Publisher publisher) =>
        new(publisher.Id, publisher.Name, publisher.Website, context.VersionOf(publisher));

    private static PublisherCollectionForm ToForm(BdthequeDbContext context, Publisher publisher, PublisherCollection collection) =>
        new(collection.Id, publisher.Id, collection.Name, context.VersionOf(publisher));
}
