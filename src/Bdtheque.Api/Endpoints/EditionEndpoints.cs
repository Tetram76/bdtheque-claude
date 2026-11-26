using System.Data;
using System.Linq.Expressions;
using Bdtheque.Api.Deletion;
using Bdtheque.Api.Errors;
using Bdtheque.Api.Merge;
using Bdtheque.Api.Visuals;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Administration of the editions (<c>/admin/albums/{albumId}/editions</c>). An edition belongs to
/// the aggregate of its album, which alone sees the purchase intents an acquisition realizes: every
/// write on it locks the album and checks the version of the album
/// (choix-implementation/persistance/concurrence-d-acces.md).
/// </summary>
internal static class EditionEndpoints
{
    // fonctionnel/regles-metier/suppression-des-entites.md: an edition takes along its visuals and its purchase
    // intent; deleting the last owned edition of an album takes the album out of the collection.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Composition, EntityKind.EditionVisual,
            (context, id) => context.EditionVisuals.Where(v => v.EditionId == id).Select(v => v.Id)),
        new(LinkNature.Composition, EntityKind.PurchaseIntent,
            (context, id) => context.PurchaseIntents.Where(p => p.EditionId == id).Select(p => p.Id)),
        new(LinkNature.LeavingCollection, EntityKind.Album,
            (context, id) => context.Editions
                .Where(e => e.Id == id)
                .Where(CollectionMembership.IsOwned)
                .Where(e => !context.Editions.Where(CollectionMembership.IsOwned).Any(o => o.AlbumId == e.AlbumId && o.Id != id))
                .Select(e => e.AlbumId)),
    ];

    public static void MapEditions(this RouteGroupBuilder admin)
    {
        // `rootId` is the name the common child deletion expects for the identifier of the root.
        var editions = admin.MapGroup("/albums/{rootId:guid}/editions");
        editions.MapGet("/new", GetNewAsync);
        editions.MapGet("/{id:guid}", GetAsync);
        editions.MapPost("/", CreateAsync);
        editions.MapPut("/{id:guid}", UpdateAsync);
        editions.MapPost("/{id:guid}/acquisition", AcquireAsync);
        // The editions are always loaded with their album (AlbumConfiguration).
        editions.MapChildDeletion<Album, Edition>(query => query, album => album.Editions, DeletionLinks);

        admin.MapGet("/editions", ListForAdministrationAsync);
        admin.MapGet("/editions/merge", GetMergeAsync);
        admin.MapPost("/editions/merge", MergeAsync);
        admin.MapGet("/editions/isbn-check", (string isbn) => new IsbnCheck(IsbnChecksumValidator.IsValid(isbn), IsbnText.Normalize(isbn)));
    }

    /// <summary>
    /// Two editions to merge (fonctionnel/regles-metier/fusion-des-fiches.md), each with its album, and what the merge does for the
    /// acquisition of either: the edition that remains owned or not, its purchase intent withdrawn or kept.
    /// </summary>
    private static async Task<EditionMergePreview> GetMergeAsync(Guid first, Guid second, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot: the two editions as they are at the same instant.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        // Tracked, for the version of the album (BdthequeDbContext.VersionOf): the context lives for this request only.
        var editions = new List<Edition>(2);
        foreach (var id in new[] { first, second })
        {
            editions.Add(await context.Editions
                             .Include(e => e.Album).Include(e => e.Visuals).Include(e => e.Publisher).Include(e => e.PublisherCollection)
                             .SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Edition), id));
        }

        var (firstEdition, secondEdition) = (editions[0], editions[1]);
        return new EditionMergePreview(
            await MergeRecordAsync(context, firstEdition, cancellationToken),
            await MergeRecordAsync(context, secondEdition, cancellationToken),
            [MergeOutcome(firstEdition, secondEdition, MergeSide.First), MergeOutcome(firstEdition, secondEdition, MergeSide.Second)]);
    }

    // What the merge does for the acquisition of one edition: owned, the edition that remains loses the intents of both; not owned,
    // it keeps one, the same wish on both sides, common to both (visuel/fusion/fusion-de-deux-fiches.md).
    private static EditionMergeOutcome MergeOutcome(Edition first, Edition second, MergeSide acquisition)
    {
        var withdrawn = Album.PurchaseIntentsWithdrawnByEditionMerge(first, second, acquisition == MergeSide.First ? first : second);
        int Intents(Edition edition) => edition.Album.PurchaseIntents.Count(p => p.EditionId == edition.Id && !withdrawn.Contains(p));
        var common = Intents(first) + Intents(second) == 2 ? 1 : 0;
        return new EditionMergeOutcome(
            acquisition,
            [new MergeLinkCount(EntityKind.PurchaseIntent, Intents(first) + Intents(second) - common, Intents(first), Intents(second), common)],
            [
                .. withdrawn.Any(p => p.EditionId == first.Id) ? [MergeSide.First] : Array.Empty<MergeSide>(),
                .. withdrawn.Any(p => p.EditionId == second.Id) ? [MergeSide.Second] : Array.Empty<MergeSide>(),
            ]);
    }

    private static async Task<EditionMergeRecord> MergeRecordAsync(BdthequeDbContext context, Edition e, CancellationToken cancellationToken) =>
        new(e.Id, await SummaryAsync(context, e.AlbumId, cancellationToken), new PublisherListItem(e.Publisher.Id, e.Publisher.Name, e.Publisher.Website),
            e.PublisherCollection is { } collection
                ? new PublisherCollectionListItem(collection.Id, collection.Name, e.Publisher.Id, e.Publisher.Name)
                : null,
            e.PublicationYear, e.Isbn, EnumMapping.Map<ContractEnums.EditionCategory>(e.Category), EnumMapping.Map<ContractEnums.EditionFormat>(e.Format),
            EnumMapping.Map<ContractEnums.BindingType>(e.Binding), EnumMapping.Map<ContractEnums.BookOrientation>(e.Orientation),
            EnumMapping.Map<ContractEnums.ReadingDirection>(e.ReadingDirection), e.PageCount, e.IsColor,
            EnumMapping.Map<ContractEnums.AcquisitionMode>(e.AcquisitionMode), e.AcquisitionDate, e.AcquisitionAmount, e.AcquisitionCurrency,
            e.IsFree, e.InitialValueAmount, e.InitialValueCurrency, EnumMapping.Map<ContractEnums.EditionCondition>(e.Condition), e.IsSecondHand,
            e.IsDedicated, e.PersonalReference, e.PersonalNotes, e.Album.PurchaseIntents.Any(p => p.EditionId == e.Id),
            [
                .. e.GetOrderedVisuals().Select(v => new EditionVisualItem(
                    v.Id, EnumMapping.Map<ContractEnums.VisualType>(v.Type)!.Value, v.DisplayOrder, v.CreatedAt, v.ModifiedAt)),
            ],
            e.ModifiedAt, context.VersionOf(e.Album));

    /// <summary>
    /// Merges two editions: the edition that remains belongs to the album chosen and keeps each data chosen, the publisher and its
    /// collection together, the acquisition whole; it keeps the visuals retained of both, with their identity, and the history of
    /// the spotlight of both; its purchase intent is withdrawn once it is owned, one kept otherwise. The album of each edition is
    /// locked at the version its data were read, both in the order of their identifiers when they are two.
    /// </summary>
    private static async Task<MergeResult> MergeAsync(
        EditionMergeRequest request, BdthequeDbContext context, VisualStorage storage, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var (first, second) = await LoadForMergeAsync(context, request.First, request.Second, cancellationToken);
        var keepFirst = RecordMerge.KeepsFirst(
        [
            request.Album, request.Publisher, request.PublicationYear, request.Isbn, request.Category, request.Format, request.Binding,
            request.Orientation, request.ReadingDirection, request.PageCount, request.IsColor, request.Acquisition, request.Condition,
            request.IsSecondHand, request.IsDedicated, request.PersonalReference, request.PersonalNotes, .. request.Visuals.Select(v => v.Side),
        ]);
        var (kept, merged) = keepFirst ? (first, second) : (second, first);
        Edition Of(MergeSide side) => side == MergeSide.First ? first : second;

        // Two saves, one transaction, as any merge (RecordMerge.MergeAsync): the saved searches designating the edition merged
        // designate the one that remains first, before the deletion their designations would refuse.
        await RecordMerge.TransferSavedSearchesAsync(context, [new MergeReplacement(EntityKind.Edition, merged.Id, kept.Id)], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var published = Of(request.Publisher);
        await context.Entry(published).Reference(e => e.Publisher).LoadAsync(cancellationToken);
        await context.Entry(published).Reference(e => e.PublisherCollection).LoadAsync(cancellationToken);
        var (publisher, collection) = (published.Publisher, published.PublisherCollection);
        var isbn = Of(request.Isbn).Isbn;
        // The error of the ISBN kept stays accepted if either edition accepted it.
        var accepted = isbn is not null && (first.AcceptedIsbn == isbn || second.AcceptedIsbn == isbn);
        var (category, format, binding) = (Of(request.Category).Category, Of(request.Format).Format, Of(request.Binding).Binding);
        var (orientation, direction, pages) = (Of(request.Orientation).Orientation, Of(request.ReadingDirection).ReadingDirection, Of(request.PageCount).PageCount);
        var (color, condition, secondHand) = (Of(request.IsColor).IsColor, Of(request.Condition).Condition, Of(request.IsSecondHand).IsSecondHand);
        var (dedicated, reference, notes) = (Of(request.IsDedicated).IsDedicated, Of(request.PersonalReference).PersonalReference, Of(request.PersonalNotes).PersonalNotes);
        var retained = RecordMerge.Retained(new MergePair<Edition>(first, second, keepFirst), request.Visuals, e => e.Visuals, v => v.Id);

        var removed = Album.MergeEditions(
            kept, merged, Of(request.Album).Album, Of(request.PublicationYear).PublicationYear, Of(request.Acquisition), [.. retained.Select(v => v.Id)]);
        kept.SetPublisher(publisher, collection);
        kept.SetIsbn(isbn);
        if (accepted && kept.AcceptedIsbn is null)
            kept.AcceptIsbnError();
        kept.SetCategory(category);
        kept.SetFormat(format);
        kept.SetBinding(binding);
        kept.SetOrientation(orientation);
        kept.SetReadingDirection(direction);
        kept.SetPageCount(pages);
        kept.SetColor(color);
        kept.SetCondition(condition);
        kept.SetSecondHand(secondHand);
        kept.SetDedicated(dedicated);
        kept.SetPersonalReference(reference);
        kept.SetPersonalNotes(notes);
        context.EditionVisuals.RemoveRange(removed);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        // After the commit, as any deletion: a failed one leaves the files intact, a file not deleted is left to the reconciliation.
        storage.Delete(removed.Select(v => v.MediaReference));
        return new MergeResult(kept.Id);
    }

    // The two editions, their albums locked at the versions their data were read, loaded with the visuals and the history of the
    // spotlight of their editions: one album, or two, in the order of their identifiers (AggregateWriteExtensions).
    private static async Task<(Edition First, Edition Second)> LoadForMergeAsync(
        BdthequeDbContext context, MergedRecord first, MergedRecord second, CancellationToken cancellationToken)
    {
        if (first.Id == second.Id)
            throw new ArgumentException($"The edition {first.Id} is given twice.", nameof(second));
        // Read without tracking, before the locks (AggregateWriteExtensions); an edition passed to another album since was read in an
        // album that changed, which its version tells.
        var ids = new[] { first.Id, second.Id };
        var albumIds = await context.Editions.AsNoTracking().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.AlbumId, cancellationToken);
        Guid AlbumOf(MergedRecord record) => albumIds.TryGetValue(record.Id, out var id) ? id : throw new EntityNotFoundException(typeof(Edition), record.Id);
        var (firstAlbumId, secondAlbumId) = (AlbumOf(first), AlbumOf(second));

        static IQueryable<Album> Shape(IQueryable<Album> query) =>
            query.Include(a => a.Editions).ThenInclude(e => e.Visuals).Include(a => a.Editions).ThenInclude(e => e.Spotlights);
        if (firstAlbumId == secondAlbumId)
        {
            var album = await context.LoadAggregateForWriteAsync<Album>(firstAlbumId, first.Version, Shape, cancellationToken);
            if (second.Version != first.Version)
                throw new DbUpdateConcurrencyException($"Album {firstAlbumId} was modified since version {second.Version} was read.");
            return (album.Editions.Single(e => e.Id == first.Id), album.Editions.Single(e => e.Id == second.Id));
        }

        var (firstAlbum, secondAlbum) = await context.LoadAggregatesForWriteAsync<Album>(
            (firstAlbumId, first.Version), (secondAlbumId, second.Version), Shape, cancellationToken);
        return (firstAlbum.Editions.Single(e => e.Id == first.Id), secondAlbum.Editions.Single(e => e.Id == second.Id));
    }

    public static void MapEditionCatalog(this RouteGroupBuilder catalog) => catalog.MapGet("/editions", ListAsync);

    /// <summary>
    /// The editions of an album, by year, each whole, with its membership of the collection, its intent,
    /// and its visuals, in their presentation order: an edition has no record of its own, the record of its
    /// album presents it (fonctionnel/structure-de-l-application.md).
    /// </summary>
    internal static async Task<List<EditionDetail>> ReadDetailsAsync(Guid albumId, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // The entities, with their visuals, for the order of the visuals, which the domain alone applies.
        var editions = await context.Editions.AsNoTracking()
            .Include(e => e.Visuals)
            .Where(e => e.AlbumId == albumId)
            .OrderBy(e => e.PublicationYear)
            .ThenBy(e => e.Id)
            .Select(CatalogExpressions.Expand((Edition e) => new
            {
                Edition = e.ToSummary(),
                e.Publisher.Website,
                Entity = e,
                IsInCollection = e.IsOwned(),
                PurchaseIntent = e.PurchaseIntentOf(),
            }))
            .ToListAsync(cancellationToken);
        return editions.Select(e => ToDetail(e.Edition, e.Website, e.Entity, e.IsInCollection, e.PurchaseIntent)).ToList();
    }

    private static EditionDetail ToDetail(
        EditionSummary summary, string? publisherWebsite, Edition e, bool isInCollection, PurchaseIntentItem? purchaseIntent)
    {
        var visuals = e.GetOrderedVisuals()
            .Select(v => new EditionVisualItem(
                v.Id, EnumMapping.Map<ContractEnums.VisualType>(v.Type)!.Value, v.DisplayOrder, v.CreatedAt, v.ModifiedAt))
            .ToList();

        return new EditionDetail(
            summary,
            publisherWebsite,
            e.Isbn is null ? null : IsbnChecksumValidator.IsValid(e.Isbn),
            EnumMapping.Map<ContractEnums.BindingType>(e.Binding),
            EnumMapping.Map<ContractEnums.BookOrientation>(e.Orientation),
            EnumMapping.Map<ContractEnums.ReadingDirection>(e.ReadingDirection),
            EnumMapping.Map<ContractEnums.EditionFormat>(e.Format),
            e.PageCount,
            EnumMapping.Map<ContractEnums.EditionCategory>(e.Category),
            e.IsDedicated,
            e.IsColor,
            EnumMapping.Map<ContractEnums.EditionCondition>(e.Condition),
            EnumMapping.Map<ContractEnums.AcquisitionMode>(e.AcquisitionMode),
            e.IsSecondHand,
            e.AcquisitionDate,
            e.AcquisitionAmount,
            e.AcquisitionCurrency,
            e.IsFree,
            e.InitialValueAmount,
            e.InitialValueCurrency,
            e.PersonalReference,
            e.PersonalNotes,
            isInCollection,
            e.EnteredCollectionAt,
            purchaseIntent,
            visuals,
            e.CreatedAt,
            e.ModifiedAt);
    }

    internal static readonly Expression<Func<Edition, EditionListItem>> ListItem =
        CatalogExpressions.Expand((Edition e) => new EditionListItem(e.Album.ToSummary(), e.ToSummary(), e.IsOwned()));

    /// <summary>
    /// The editions found by their ISBN, narrowed by the cross filters (choix-implementation/api/recherche.md), in the order
    /// of their albums, then by year.
    /// </summary>
    private static Task<Page<EditionListItem>> ListAsync(
        BdthequeDbContext context, [AsParameters] EditionFilters filters, CancellationToken cancellationToken, int page = 1,
        int pageSize = Paging.DefaultSize) =>
        InListOrder(filters.Apply(context.Editions.AsNoTracking())).ToPageAsync(page, pageSize, ListItem, cancellationToken);

    /// <summary>
    /// The list of the editions of the administration (visuel/listes/liste-par-liste.md): the search and cross filters of the
    /// list of the consultation, in the same order, but the editions of the collection only, an edition targeted by a
    /// purchase intent being one of the intents (fonctionnel/regles-metier/intention-d-acquisition.md). Each row carries the
    /// edition and its album, its cover, its category, its acquisition, its modification and the version of its album,
    /// which its deletion sends back.
    /// </summary>
    private static async Task<EditionAdminList> ListForAdministrationAsync(
        BdthequeDbContext context, [AsParameters] EditionFilters filters, CancellationToken cancellationToken, int page = 1,
        int pageSize = Paging.DefaultSize)
    {
        var rows = await InListOrder(filters.Apply(context.Editions.AsNoTracking().Where(CollectionMembership.IsOwned)))
            .ToPageAsync(page, pageSize, CatalogExpressions.Expand((Edition e) => new
            {
                Album = e.Album.ToSummary(),
                Edition = e.ToSummary(),
                Cover = CatalogExpressions.ToCover(e.CoverVisualOf()),
                e.Category,
                e.AcquisitionMode,
                e.AcquisitionDate,
                e.ModifiedAt,
                AlbumVersion = EF.Property<uint>(e.Album, BdthequeDbContext.VersionProperty),
            }), cancellationToken);

        var items = rows.Items
            .Select(r => new EditionAdminListItem(
                r.Album, r.Edition, r.Cover, EnumMapping.Map<ContractEnums.EditionCategory>(r.Category),
                EnumMapping.Map<ContractEnums.AcquisitionMode>(r.AcquisitionMode)!.Value, r.AcquisitionDate?.Year, r.ModifiedAt,
                r.AlbumVersion))
            .ToList();
        return new EditionAdminList(
            new Page<EditionAdminListItem>(items, rows.Number, rows.Size, rows.TotalCount),
            await filters.NameAsync(context, cancellationToken));
    }

    internal static IOrderedQueryable<Edition> InListOrder(IQueryable<Edition> editions) =>
        editions.OrderByAlbum(e => e.Album).ThenBy(e => e.PublicationYear).ThenBy(e => e.Id);

    internal static async Task<NewEditionForm> GetNewAsync(Guid rootId, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // Tracked, for the version of the aggregate (BdthequeDbContext.VersionOf); read only, hence
        // without the children of the aggregate.
        var album = await context.Albums.IgnoreAutoIncludes().Include(a => a.Series)
                        .SingleOrDefaultAsync(a => a.Id == rootId, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Album), rootId);
        var preset = EditionPreset.For(album);
        return new NewEditionForm(
            album.Id,
            ToContent(preset),
            context.VersionOf(album),
            await SummaryAsync(context, rootId, cancellationToken),
            await NameAsync(context, preset.PublisherId, preset.PublisherCollectionId, cancellationToken),
            album.Series is { } series ? SeriesEndpoints.TemplateOf(series) : null);
    }

    /// <summary>
    /// The form of an edition, which names what it cites, its album and, for an edition not owned, the intent that targets
    /// it, with its dates (choix-implementation/api/organisation-de-l-api.md, Écritures).
    /// </summary>
    private static async Task<EditionForm> GetAsync(Guid rootId, Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var read = await context.Editions.AsNoTracking()
                       .Where(e => e.Id == id && e.AlbumId == rootId)
                       .Select(CatalogExpressions.Expand((Edition e) => new
                       {
                           Edition = e,
                           Album = e.Album.ToSummary(),
                           PurchaseIntent = e.PurchaseIntentOf(),
                           AlbumVersion = EF.Property<uint>(e.Album, BdthequeDbContext.VersionProperty),
                       }))
                       .SingleOrDefaultAsync(cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Edition), id);
        var edition = read.Edition;
        return ToForm(
            edition, read.AlbumVersion, read.Album,
            await NameAsync(context, edition.PublisherId, edition.PublisherCollectionId, cancellationToken), read.PurchaseIntent);
    }

    // The summary of an album, as the lists build it.
    private static async Task<AlbumSummary> SummaryAsync(BdthequeDbContext context, Guid albumId, CancellationToken cancellationToken) =>
        await context.Albums.AsNoTracking()
            .Where(a => a.Id == albumId)
            .Select(CatalogExpressions.Expand((Album a) => a.ToSummary()))
            .SingleAsync(cancellationToken);

    /// <summary>The publisher and the collection a form cites, named as the lists name them.</summary>
    internal static async Task<EditionFormReferences> NameAsync(
        BdthequeDbContext context, Guid? publisherId, Guid? collectionId, CancellationToken cancellationToken)
    {
        var publisher = publisherId is { } pid
            ? await context.Publishers.AsNoTracking()
                .Where(p => p.Id == pid)
                .Select(p => new PublisherListItem(p.Id, p.Name, p.Website))
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var collection = collectionId is { } cid
            ? await context.PublisherCollections.AsNoTracking()
                .Where(c => c.Id == cid)
                .Select(c => new PublisherCollectionListItem(c.Id, c.Name, c.PublisherId, c.Publisher.Name))
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        return new EditionFormReferences(publisher, collection);
    }

    private static async Task<Created<EditionForm>> CreateAsync(
        Guid rootId, CreateEditionRequest request, BdthequeDbContext context, ContentFields fields, TimeProvider clock,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, request.AlbumVersion, cancellationToken: cancellationToken);

        var edition = await AddEditionAsync(context, fields, album, request.Content, cancellationToken);
        // Entering an edition records its acquisition, which realizes the intent it satisfies.
        album.RecordAcquisition(edition, ToAcquisition(request.Content));
        // A new edition has no visual to remove: its pending visuals become its own.
        await EditionVisualEntries.ApplyAsync(context, edition, request.Visuals, clock, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created(
            $"/admin/albums/{rootId}/editions/{edition.Id}", await GetAsync(rootId, edition.Id, context, cancellationToken));
    }

    /// <summary>
    /// Confirms the purchase of an edition targeted by an intent
    /// (fonctionnel/regles-metier/intention-d-acquisition.md#réalisation-dune-intention): the whole form is applied, its
    /// visuals included, with the acquisition, which realizes the intent.
    /// </summary>
    private static async Task<EditionForm> AcquireAsync(
        Guid rootId, Guid id, AcquireEditionRequest request, BdthequeDbContext context, ContentFields fields, VisualStorage storage,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(
            rootId, request.AlbumVersion, request.Visuals is null ? null : EditionVisualEntries.WithVisuals, cancellationToken);
        var edition = album.Editions.SingleOrDefault(e => e.Id == id) ?? throw new EntityNotFoundException(typeof(Edition), id);
        fields.Place(edition);
        var content = request.Content;
        var (publisher, collection) = await LoadPublisherAsync(context, fields, content, cancellationToken);

        ApplyDetails(edition, content, publisher, collection);
        // Not owned until then, the edition has no amount its year could leave undated.
        edition.SetPublicationYear(content.PublicationYear);
        album.RecordAcquisition(edition, ToAcquisition(content));
        var removed = await EditionVisualEntries.ApplyAsync(context, edition, request.Visuals, clock, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        // After the commit, as any deletion: a failed one leaves the files intact, a file not deleted is left to the reconciliation.
        storage.Delete(removed);
        return await GetAsync(rootId, id, context, cancellationToken);
    }

    /// <summary>
    /// Adds to the album an edition entered with <paramref name="content"/>, not owned until its
    /// acquisition is recorded: the caller records it, or targets the edition by an intent. The edition is placed at the root
    /// of the content that enters it.
    /// </summary>
    internal static async Task<Edition> AddEditionAsync(
        BdthequeDbContext context, ContentFields fields, Album album, EditionContent content, CancellationToken cancellationToken)
    {
        var (publisher, collection) = await LoadPublisherAsync(context, fields, content, cancellationToken);

        var edition = fields.Create("", () => new Edition(album, publisher));
        ApplyDetails(edition, content, publisher, collection);
        // Not owned yet, the edition has no amount its year could leave undated.
        edition.SetPublicationYear(content.PublicationYear);
        context.Editions.Add(edition);
        return edition;
    }

    private static async Task<EditionForm> UpdateAsync(
        Guid rootId, Guid id, UpdateEditionRequest request, BdthequeDbContext context, ContentFields fields, VisualStorage storage,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(
            rootId, request.AlbumVersion, request.Visuals is null ? null : EditionVisualEntries.WithVisuals, cancellationToken);
        var edition = album.Editions.SingleOrDefault(e => e.Id == id) ?? throw new EntityNotFoundException(typeof(Edition), id);
        fields.Place(edition);
        var content = request.Content;
        var (publisher, collection) = await LoadPublisherAsync(context, fields, content, cancellationToken);

        ApplyDetails(edition, content, publisher, collection);
        edition.SetPublicationYearAndAcquisition(content.PublicationYear, ToAcquisition(content));
        var removed = await EditionVisualEntries.ApplyAsync(context, edition, request.Visuals, clock, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        // After the commit, as any deletion: a failed one leaves the files intact, a file not deleted is left to the reconciliation.
        storage.Delete(removed);
        return await GetAsync(rootId, id, context, cancellationToken);
    }

    /// <summary>
    /// The publisher and the collection of the edition, the publisher created first if the form added it on the spot: it is
    /// created with the edition, in the same transaction, or not at all
    /// (choix-implementation/api/organisation-de-l-api.md).
    /// </summary>
    private static async Task<(Publisher? Publisher, PublisherCollection? Collection)> LoadPublisherAsync(
        BdthequeDbContext context, ContentFields fields, EditionContent content, CancellationToken cancellationToken)
    {
        var added = await FormReferences.CreateAddedAsync(context, fields, content.NewRecords, cancellationToken);
        return await FormReferences.LoadPublisherAsync(context, content.PublisherId, content.PublisherCollectionId, added, cancellationToken);
    }

    /// <summary>
    /// Applies the fields of the form that depend on no other, and the publisher with its
    /// collection, which the domain receives together.
    /// </summary>
    private static void ApplyDetails(Edition edition, EditionContent content, Publisher? publisher, PublisherCollection? collection)
    {
        edition.SetPublisher(publisher, collection);
        edition.SetIsbn(content.Isbn);
        edition.SetBinding(EnumMapping.Map<DomainEnums.BindingType>(content.Binding));
        edition.SetOrientation(EnumMapping.Map<DomainEnums.BookOrientation>(content.Orientation));
        edition.SetReadingDirection(EnumMapping.Map<DomainEnums.ReadingDirection>(content.ReadingDirection));
        edition.SetFormat(EnumMapping.Map<DomainEnums.EditionFormat>(content.Format));
        edition.SetPageCount(content.PageCount);
        edition.SetCategory(EnumMapping.Map<DomainEnums.EditionCategory>(content.Category));
        edition.SetDedicated(content.IsDedicated);
        edition.SetColor(content.IsColor);
        edition.SetCondition(EnumMapping.Map<DomainEnums.EditionCondition>(content.Condition));
        edition.SetSecondHand(content.IsSecondHand);
        edition.SetPersonalReference(content.PersonalReference);
        edition.SetPersonalNotes(content.PersonalNotes);
    }

    private static EditionAcquisition ToAcquisition(EditionContent content) =>
        new(EnumMapping.Map<DomainEnums.AcquisitionMode>(content.AcquisitionMode),
            content.AcquisitionDate,
            content.AcquisitionAmount,
            content.AcquisitionCurrency,
            content.IsFree,
            content.InitialValueAmount,
            content.InitialValueCurrency);

    private static EditionContent ToContent(EditionPreset preset) =>
        new(preset.PublisherId,
            preset.PublisherCollectionId,
            null,
            null,
            EnumMapping.Map<ContractEnums.BindingType>(preset.Binding),
            EnumMapping.Map<ContractEnums.BookOrientation>(preset.Orientation),
            EnumMapping.Map<ContractEnums.ReadingDirection>(preset.ReadingDirection),
            EnumMapping.Map<ContractEnums.EditionFormat>(preset.Format),
            null,
            EnumMapping.Map<ContractEnums.EditionCategory>(preset.Category),
            false,
            preset.IsColor,
            EnumMapping.Map<ContractEnums.EditionCondition>(preset.Condition),
            null,
            false,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            null);

    private static EditionForm ToForm(
        Edition edition, uint albumVersion, AlbumSummary album, EditionFormReferences references, PurchaseIntentItem? purchaseIntent) =>
        new(edition.Id,
            edition.AlbumId,
            new EditionContent(
                edition.PublisherId,
                edition.PublisherCollectionId,
                edition.PublicationYear,
                edition.Isbn,
                EnumMapping.Map<ContractEnums.BindingType>(edition.Binding),
                EnumMapping.Map<ContractEnums.BookOrientation>(edition.Orientation),
                EnumMapping.Map<ContractEnums.ReadingDirection>(edition.ReadingDirection),
                EnumMapping.Map<ContractEnums.EditionFormat>(edition.Format),
                edition.PageCount,
                EnumMapping.Map<ContractEnums.EditionCategory>(edition.Category),
                edition.IsDedicated,
                edition.IsColor,
                EnumMapping.Map<ContractEnums.EditionCondition>(edition.Condition),
                EnumMapping.Map<ContractEnums.AcquisitionMode>(edition.AcquisitionMode),
                edition.IsSecondHand,
                edition.AcquisitionDate,
                edition.AcquisitionAmount,
                edition.AcquisitionCurrency,
                edition.IsFree,
                edition.InitialValueAmount,
                edition.InitialValueCurrency,
                edition.PersonalReference,
                edition.PersonalNotes),
            edition.Isbn is null ? null : IsbnChecksumValidator.IsValid(edition.Isbn),
            albumVersion,
            album,
            references,
            purchaseIntent,
            edition.CreatedAt,
            edition.ModifiedAt,
            edition.EnteredCollectionAt);
}
