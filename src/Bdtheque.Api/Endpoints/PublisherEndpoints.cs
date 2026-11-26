using System.Data;
using Bdtheque.Api.Deletion;
using Bdtheque.Api.Errors;
using Bdtheque.Api.Merge;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using DomainEnums = Bdtheque.Domain.Enums;
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

        publishers.MapGet("/merge", GetMergeAsync);
        publishers.MapPost("/merge", MergeAsync);
    }

    // The series, editions and purchase intents of a publisher, an edition a purchase intent targets being counted as that intent
    // (visuel/fusion/fusion-de-deux-fiches.md, fonctionnel/regles-metier/suppression-des-entites.md); its collections, whose
    // homonyms become one, are counted apart (CollectionLinkAsync).
    private static readonly (EntityKind Kind, Func<BdthequeDbContext, Guid, IQueryable<Guid>> Records)[] MergeLinks =
    [
        (EntityKind.Series, (context, id) => context.Series.Where(s => s.TemplatePublisherId == id).Select(s => s.Id)),
        (EntityKind.Edition, (context, id) =>
            context.Editions.Where(CatalogExpressions.Expand((Edition e) => e.PublisherId == id && e.IsOwned())).Select(e => e.Id)),
        (EntityKind.PurchaseIntent, (context, id) => context.PurchaseIntents.Where(i => i.Edition!.PublisherId == id).Select(i => i.Id)),
    ];

    /// <summary>
    /// Two publishers to merge, their homonymous collections, and what designates them
    /// (fonctionnel/regles-metier/fusion-des-fiches.md).
    /// </summary>
    private static async Task<PublisherMergePreview> GetMergeAsync(Guid first, Guid second, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot: the homonyms and the counts are those of the two publishers read.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var (firstRecord, secondRecord) = (await MergeRecordAsync(context, first, cancellationToken), await MergeRecordAsync(context, second, cancellationToken));
        var homonyms = await HomonymsAsync(context, first, second, cancellationToken);
        var links = (await RecordMerge.CountLinksAsync(context, first, second, MergeLinks, cancellationToken))
            .Append(await CollectionLinkAsync(context, first, second, homonyms.Count, cancellationToken))
            .OrderBy(l => EntityKindOrder.Of(l.Kind))
            .ToList();
        return new PublisherMergePreview(firstRecord, secondRecord, homonyms, links);
    }

    private static async Task<PublisherMergeRecord> MergeRecordAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken) =>
        await context.Publishers.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PublisherMergeRecord(p.Id, p.Name, p.Website, p.ModifiedAt, EF.Property<uint>(p, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Publisher), id);

    /// <summary>
    /// The collections of the first publisher homonymous to one of the second, by name: the names compare as their column
    /// compares them, without case nor accents (choix-implementation/persistance/collation-des-colonnes-texte.md), the only
    /// comparison that the uniqueness of a name among the collections of a publisher, which the merge must keep, follows.
    /// Each collection has at most one homonym, the names of a publisher's collections being unique.
    /// </summary>
    private static async Task<List<PublisherCollectionHomonyms>> HomonymsAsync(
        BdthequeDbContext context, Guid first, Guid second, CancellationToken cancellationToken) =>
        [.. (await context.PublisherCollections.AsNoTracking()
            .Where(c => c.PublisherId == first)
            .Join(context.PublisherCollections.Where(c => c.PublisherId == second), c => c.Name, c => c.Name,
                (ofFirst, ofSecond) => new { First = ofFirst.Id, FirstName = ofFirst.Name, Second = ofSecond.Id, SecondName = ofSecond.Name })
            .OrderBy(h => h.FirstName)
            .ThenBy(h => h.First)
            .ToListAsync(cancellationToken))
        .Select(h => new PublisherCollectionHomonyms(h.First, h.FirstName, h.Second, h.SecondName))];

    // The collections of both publishers, two homonyms counted once, as common to both: they become one.
    private static async Task<MergeLinkCount> CollectionLinkAsync(
        BdthequeDbContext context, Guid first, Guid second, int homonyms, CancellationToken cancellationToken)
    {
        var ofFirst = await context.PublisherCollections.CountAsync(c => c.PublisherId == first, cancellationToken);
        var ofSecond = await context.PublisherCollections.CountAsync(c => c.PublisherId == second, cancellationToken);
        return new MergeLinkCount(EntityKind.PublisherCollection, ofFirst + ofSecond - homonyms, ofFirst, ofSecond, homonyms);
    }

    /// <summary>
    /// Merges two publishers: the publisher that remains keeps each data chosen and takes the collections of the other one,
    /// two homonymous collections becoming the one whose name is kept; the editions and series of both are published by it,
    /// each in the collection that remains of its own.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The collections chosen are not the homonyms of the two publishers as read: the client sends back what it read, which
    /// the versions of the publishers guarantee unchanged.
    /// </exception>
    private static Task<MergeResult> MergeAsync(PublisherMergeRequest request, BdthequeDbContext context, CancellationToken cancellationToken) =>
        RecordMerge.MergeAsync<Publisher>(
            context, request.First, request.Second, RecordMerge.KeepsFirst([request.Name, request.Website]),
            pair =>
            {
                // Read from both before the kept publisher changes.
                var (name, website) = (pair.Of(request.Name).Name, pair.Of(request.Website).Website);
                pair.Kept.SetName(name);
                pair.Kept.SetWebsite(website);
            },
            async pair =>
            {
                // Loaded, and tracked before the editions and series that designate them: each collection is one of a
                // publisher's, and a collection deleted with its publisher is recorded by the change log.
                foreach (var publisher in new[] { pair.First, pair.Second })
                    await context.Entry(publisher).Collection(p => p.Collections).LoadAsync(cancellationToken);
                var collections = pair.First.Collections.Concat(pair.Second.Collections).ToDictionary(c => c.Id);
                var homonyms = KeptOfHomonyms(request, await HomonymsAsync(context, pair.First.Id, pair.Second.Id, cancellationToken))
                    .Select(h => h.Name == MergeSide.First
                        ? (Remaining: collections[h.Homonyms.First], Removed: collections[h.Homonyms.Second])
                        : (Remaining: collections[h.Homonyms.Second], Removed: collections[h.Homonyms.First]))
                    .ToList();

                var removed = homonyms.Select(h => h.Removed.Id).ToList();
                var series = await RecordMerge.LoadLinkedForWriteAsync<Series>(
                    context,
                    context.Series.Where(s => s.TemplatePublisherId == pair.Merged.Id
                                              || (s.TemplatePublisherCollectionId != null && removed.Contains(s.TemplatePublisherCollectionId.Value)))
                        .Select(s => s.Id),
                    cancellationToken);
                var albums = await RecordMerge.LoadLinkedForWriteAsync<Album>(
                    context,
                    context.Editions.Where(e => e.PublisherId == pair.Merged.Id
                                                || (e.PublisherCollectionId != null && removed.Contains(e.PublisherCollectionId.Value)))
                        .Select(e => e.AlbumId),
                    cancellationToken);

                var merge = pair.Kept.Absorb(pair.Merged, homonyms);
                foreach (var oneSeries in series)
                    oneSeries.TransferPublisher(merge);
                foreach (var album in albums)
                    album.TransferPublisher(merge);
                return [.. merge.Replaced.Select(r => new MergeReplacement(EntityKind.PublisherCollection, r.Key, r.Value.Id))];
            },
            cancellationToken);

    /// <summary>The name each pair of homonymous collections keeps, as the request chooses it.</summary>
    /// <exception cref="ArgumentException">
    /// The request does not choose the name of each pair, and of them only: the client sends back the homonyms it read, which the
    /// versions of the publishers guarantee unchanged.
    /// </exception>
    private static List<(PublisherCollectionHomonyms Homonyms, MergeSide Name)> KeptOfHomonyms(
        PublisherMergeRequest request, List<PublisherCollectionHomonyms> homonyms)
    {
        if (request.Collections.Count != homonyms.Count)
            throw new ArgumentException("A collection chosen is not one of the homonyms of the two publishers.", nameof(request));
        var kept = new List<(PublisherCollectionHomonyms Homonyms, MergeSide Name)>(homonyms.Count);
        foreach (var pair in homonyms)
        {
            var chosen = request.Collections.FirstOrDefault(c => c.First == pair.First && c.Second == pair.Second)
                         ?? throw new ArgumentException($"The name of the collections {pair.First} and {pair.Second} is not chosen.", nameof(request));
            kept.Add((pair, chosen.Name));
        }

        return kept;
    }

    // A publisher and its collections have no record (fonctionnel/structure-de-l-application.md): a
    // publisher leads to its website, its editions are listed by the cross filters of the lists.
    public static void MapPublisherCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/publishers", ListAsync);
        catalog.MapGet("/publisher-collections", ListAllCollectionsAsync);
    }

    private static Task<Page<PublisherListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, string? entry = null, int page = 1,
        int pageSize = Paging.DefaultSize) =>
        Search(context, q, entry).ToPageAsync(page, pageSize, p => new PublisherListItem(p.Id, p.Name, p.Website), cancellationToken);

    /// <summary>
    /// The list of the publishers of the administration (visuel/listes/liste-par-liste.md): the search, the navigation by
    /// initial and the order of the list of the consultation. Each row carries the website of the publisher, its collections,
    /// its editions in the collection and, apart, the purchase intents on its editions, its modification and its version,
    /// which its deletion sends back.
    /// </summary>
    private static Task<Page<PublisherAdminListItem>> ListForAdministrationAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, string? entry = null, int page = 1,
        int pageSize = Paging.DefaultSize) =>
        Search(context, q, entry)
            .ToPageAsync(page, pageSize, CatalogExpressions.Expand((Publisher p) => new PublisherAdminListItem(
                p.Id,
                p.Name,
                p.Website,
                p.Collections.OrderBy(c => c.Name).ThenBy(c => c.Id).Select(c => c.Name).ToList(),
                context.Editions.Count(e => e.PublisherId == p.Id && e.IsOwned()),
                context.PurchaseIntents.Count(i => i.Edition!.PublisherId == p.Id),
                p.ModifiedAt,
                EF.Property<uint>(p, BdthequeDbContext.VersionProperty))), cancellationToken);

    /// <summary>The publishers found by their name, under the entry of the navigation, by name.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An entry that is not one of the navigation: a technical error.</exception>
    private static IOrderedQueryable<Publisher> Search(BdthequeDbContext context, string? text, string? entry)
    {
        CatalogFilters.EnsureNavigationEntry(entry);
        var publishers = context.Publishers.AsNoTracking().WhereContains(text, p => p.Name);
        if (entry is not null)
            publishers = publishers.Where(p => p.NavigationEntry == entry);
        return publishers.OrderByEntry(p => p.NavigationEntry).ThenBy(p => p.Name).ThenBy(p => p.Id);
    }

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
        PublisherContent request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        var publisher = fields.Create("", () => new Publisher(request.Name));
        await ApplyAsync(context, fields, publisher, request, cancellationToken);
        context.Publishers.Add(publisher);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/publishers/{publisher.Id}", await ToFormAsync(context, publisher, cancellationToken));
    }

    private static async Task<PublisherForm> UpdateAsync(
        Guid id, UpdatePublisherRequest request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var publisher = await context.LoadAggregateForWriteAsync<Publisher>(
            id, request.Version, query => query.Include(p => p.Collections), cancellationToken);
        fields.Place(publisher);
        await ApplyAsync(context, fields, publisher, request.Content, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToFormAsync(context, publisher, cancellationToken);
    }

    /// <summary>
    /// Writes the content of the form on the publisher, its collections replaced by those listed
    /// (choix-implementation/api/organisation-de-l-api.md): a collection listed is renamed, one added created, and one of
    /// the publisher's left out removed, provided nothing references it. Each collection listed is placed where the content
    /// lists it, which an error on its name designates.
    /// </summary>
    /// <exception cref="EntityNotFoundException">A collection listed is not one of the publisher's.</exception>
    /// <exception cref="DomainRuleViolationException">
    /// A value breaks a rule of the publisher or of its collections.
    /// </exception>
    /// <exception cref="RemovalRefusedException">A collection removed is referenced (<see cref="DomainRules.PublisherCollectionInUse"/>).</exception>
    private static async Task ApplyAsync(
        BdthequeDbContext context, ContentFields fields, Publisher publisher, PublisherContent content,
        CancellationToken cancellationToken)
    {
        publisher.SetName(content.Name);
        publisher.SetWebsite(content.Website);

        var listed = content.Collections.Index().Where(c => c.Item.Id is not null).ToDictionary(c => c.Item.Id!.Value);
        if (listed.Keys.Cast<Guid?>().FirstOrDefault(id => publisher.Collections.All(c => c.Id != id)) is { } unknown)
            throw new EntityNotFoundException(typeof(PublisherCollection), unknown);

        foreach (var collection in publisher.Collections.ToList())
        {
            if (listed.TryGetValue(collection.Id, out var entered))
            {
                fields.Place(collection, CollectionPath(entered.Index));
                collection.SetName(entered.Item.Name);
            }
            else
            {
                await RemoveCollectionAsync(context, publisher, collection, cancellationToken);
            }
        }

        foreach (var (index, added) in content.Collections.Index().Where(c => c.Item.Id is null))
            fields.Create(CollectionPath(index), () => publisher.AddCollection(added.Name));
    }

    private static string CollectionPath(int index) => $"/collections/{index}";

    // A collection is removed as it would be deleted (fonctionnel/regles-metier/suppression-des-entites.md): refused while
    // editions, series templates or saved searches reference it, whose count is that of its deletion, which the refusal carries
    // to be presented line by line (visuel/saisie-editeur/contenu.md), with the collection the content no longer cites. Its row is locked
    // first, after the publisher's (choix-implementation/persistance/concurrence-d-acces.md): an edition or a series that
    // would reference it meanwhile waits for the end of the save, which no reference can then slip into.
    private static async Task RemoveCollectionAsync(
        BdthequeDbContext context, Publisher publisher, PublisherCollection collection, CancellationToken cancellationToken)
    {
        await context.LockAsync<PublisherCollection>(collection.Id, cancellationToken);
        var impact = await AggregateDeletion.ComputeImpactAsync<PublisherCollection>(
            context, collection.Id, CollectionDeletionLinks, cancellationToken);
        if (impact.BlockedBy.Count > 0)
            throw new RemovalRefusedException(DomainRules.PublisherCollectionInUse, collection.Id, impact);

        publisher.RemoveCollection(collection);
    }

    // Its collections in the order of the base, by name, each with what references it, counted as the impact of its deletion
    // (CollectionDeletionLinks, AggregateDeletion): an edition a purchase intent targets is counted as that intent, and the
    // saved searches designating the collection are counted (visuel/saisie-editeur/contenu.md).
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
                    context.Editions.Count(e => e.PublisherCollectionId == c.Id && !context.PurchaseIntents.Any(p => p.EditionId == e.Id)),
                    context.PurchaseIntents.Count(p => p.Edition!.PublisherCollectionId == c.Id),
                    context.Series.Count(s => s.TemplatePublisherCollectionId == c.Id),
                    context.SavedSearches.Count(s => s.Designations.Any(
                        d => d.Kind == DomainEnums.EntityKind.PublisherCollection && d.RecordId == c.Id))))
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
        return [.. publisher.Collections.Select(c => new PublisherCollectionForm(c.Id, rootId, c.Name, publisher.Version))];
    }

    private static async Task<PublisherCollectionForm> GetCollectionAsync(
        Guid rootId, Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.Set<PublisherCollection>()
            .Where(c => c.Id == id && c.PublisherId == rootId)
            .Select(c => new PublisherCollectionForm(
                c.Id, c.PublisherId, c.Name, EF.Property<uint>(c.Publisher, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(PublisherCollection), id);

    // The collection is the record the content describes, its name entered at /name.
    private static async Task<Created<PublisherCollectionForm>> CreateCollectionAsync(
        Guid rootId, CreatePublisherCollectionRequest request, BdthequeDbContext context, ContentFields fields,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var publisher = await context.LoadAggregateForWriteAsync<Publisher>(
            rootId, request.PublisherVersion, query => query.Include(p => p.Collections), cancellationToken);
        var collection = fields.Create("", () => publisher.AddCollection(request.Name));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created($"/admin/publishers/{rootId}/collections/{collection.Id}", ToForm(context, publisher, collection));
    }

    private static async Task<PublisherCollectionForm> UpdateCollectionAsync(
        Guid rootId, Guid id, UpdatePublisherCollectionRequest request, BdthequeDbContext context, ContentFields fields,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var publisher = await context.LoadAggregateForWriteAsync<Publisher>(
            rootId, request.PublisherVersion, query => query.Include(p => p.Collections), cancellationToken);
        var collection = publisher.Collections.SingleOrDefault(c => c.Id == id)
                         ?? throw new EntityNotFoundException(typeof(PublisherCollection), id);
        fields.Place(collection);
        collection.SetName(request.Name);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, publisher, collection);
    }

    private static PublisherCollectionForm ToForm(BdthequeDbContext context, Publisher publisher, PublisherCollection collection) =>
        new(collection.Id, publisher.Id, collection.Name, context.VersionOf(publisher));
}
