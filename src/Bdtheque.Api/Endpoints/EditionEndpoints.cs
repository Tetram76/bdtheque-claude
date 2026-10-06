using System.Linq.Expressions;
using Bdtheque.Api.Deletion;
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

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Administration of the editions (<c>/admin/albums/{albumId}/editions</c>). An edition belongs to
/// the aggregate of its album, which alone sees the purchase intents an acquisition realizes: every
/// write on it locks the album and checks the version of the album (choix-implementation.md §
/// Concurrence d'accès).
/// </summary>
internal static class EditionEndpoints
{
    // fonctionnel.md § Suppression des entités: an edition takes along its visuals and its purchase
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

        admin.MapGet("/editions/isbn-check", (string isbn) => new IsbnCheck(IsbnChecksumValidator.IsValid(isbn)));
    }

    public static void MapEditionCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/editions", ListAsync);
        catalog.MapGet("/editions/{id:guid}", GetDetailAsync);
    }

    /// <summary>
    /// An edition with its album, its membership of the collection, its intent, and its visuals, which
    /// have no record of their own, in their presentation order.
    /// </summary>
    private static async Task<EditionDetail> GetDetailAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // The entity, with its visuals, for the order of the visuals, which the domain alone applies.
        var edition = await context.Editions.AsNoTracking()
                          .Include(e => e.Visuals)
                          .Where(e => e.Id == id)
                          .Select(CatalogExpressions.Expand((Edition e) => new
                          {
                              Album = e.Album.ToSummary(),
                              Edition = e.ToSummary(),
                              Entity = e,
                              IsInCollection = e.IsOwned(),
                              PurchaseIntent = e.PurchaseIntentOf(),
                          }))
                          .AsSplitQuery()
                          .SingleOrDefaultAsync(cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Edition), id);
        var e = edition.Entity;
        var visuals = e.GetOrderedVisuals()
            .Select(v => new EditionVisualItem(
                v.Id, EnumMapping.Map<ContractEnums.VisualType>(v.Type)!.Value, v.DisplayOrder, VisualStorage.OriginalPath(v.MediaReference),
                VisualStorage.DisplayPath(v.MediaReference), v.CreatedAt, v.ModifiedAt))
            .ToList();

        return new EditionDetail(
            edition.Album,
            edition.Edition,
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
            edition.IsInCollection,
            edition.PurchaseIntent,
            visuals,
            e.CreatedAt,
            e.ModifiedAt);
    }

    private static readonly Expression<Func<Edition, EditionListItem>> ListItem =
        CatalogExpressions.Expand((Edition e) => new EditionListItem(e.Album.ToSummary(), e.ToSummary(), e.IsOwned()));

    /// <summary>
    /// The editions found by their ISBN, separators ignored on both sides — an ISBN is entered as
    /// printed, with or without them —, narrowed by the cross filters (choix-implementation.md §
    /// Recherche), in the order of their albums, then by year.
    /// </summary>
    private static Task<Page<EditionListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, Guid? albumId = null, Guid? publisherId = null,
        Guid? publisherCollectionId = null, int page = 1, int pageSize = Paging.DefaultSize)
    {
        var editions = context.Editions.AsNoTracking()
            .WhereContains(q is null ? null : WithoutIsbnSeparators(q), e => e.Isbn!.Replace("-", "").Replace(" ", ""));
        if (albumId is not null)
            editions = editions.Where(e => e.AlbumId == albumId);
        if (publisherId is not null)
            editions = editions.Where(e => e.PublisherId == publisherId);
        if (publisherCollectionId is not null)
            editions = editions.Where(e => e.PublisherCollectionId == publisherCollectionId);

        return editions
            .OrderByAlbum(e => e.Album)
            .ThenBy(e => e.PublicationYear)
            .ThenBy(e => e.Id)
            .ToPageAsync(page, pageSize, ListItem, cancellationToken);
    }

    private static string WithoutIsbnSeparators(string isbn) =>
        isbn.Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);

    internal static async Task<NewEditionForm> GetNewAsync(Guid rootId, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // Tracked, for the version of the aggregate (BdthequeDbContext.VersionOf); read only, hence
        // without the children of the aggregate.
        var album = await context.Albums.IgnoreAutoIncludes().Include(a => a.Series)
                        .SingleOrDefaultAsync(a => a.Id == rootId, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Album), rootId);
        return new NewEditionForm(album.Id, ToContent(EditionPreset.For(album)), context.VersionOf(album));
    }

    private static async Task<EditionForm> GetAsync(Guid rootId, Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var read = await context.Editions.AsNoTracking()
                       .Where(e => e.Id == id && e.AlbumId == rootId)
                       .Select(e => new { Edition = e, AlbumVersion = EF.Property<uint>(e.Album, BdthequeDbContext.VersionProperty) })
                       .SingleOrDefaultAsync(cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Edition), id);
        return ToForm(read.Edition, read.AlbumVersion);
    }

    private static async Task<Created<EditionForm>> CreateAsync(
        Guid rootId, CreateEditionRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, request.AlbumVersion, cancellationToken: cancellationToken);

        var edition = await AddEditionAsync(context, album, request.Content, cancellationToken);
        // Entering an edition records its acquisition, which realizes the intent it satisfies.
        album.RecordAcquisition(edition, ToAcquisition(request.Content));

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created($"/admin/albums/{rootId}/editions/{edition.Id}", ToForm(edition, context.VersionOf(album)));
    }

    /// <summary>
    /// Confirms the purchase of an edition targeted by an intent (fonctionnel.md § Réalisation d'une
    /// intention): the whole form is applied, with the acquisition, which realizes the intent.
    /// </summary>
    private static async Task<EditionForm> AcquireAsync(
        Guid rootId, Guid id, AcquireEditionRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, request.AlbumVersion, cancellationToken: cancellationToken);
        var edition = album.Editions.SingleOrDefault(e => e.Id == id) ?? throw new EntityNotFoundException(typeof(Edition), id);
        var content = request.Content;
        var (publisher, collection) = await FormReferences.LoadPublisherAsync(
            context, content.PublisherId, content.PublisherCollectionId, cancellationToken);

        ApplyDetails(edition, content, publisher, collection);
        // Not owned until then, the edition has no amount its year could leave undated.
        edition.SetPublicationYear(content.PublicationYear);
        album.RecordAcquisition(edition, ToAcquisition(content));

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(edition, context.VersionOf(album));
    }

    /// <summary>
    /// Adds to the album an edition entered with <paramref name="content"/>, not owned until its
    /// acquisition is recorded: the caller records it, or targets the edition by an intent.
    /// </summary>
    internal static async Task<Edition> AddEditionAsync(
        BdthequeDbContext context, Album album, EditionContent content, CancellationToken cancellationToken)
    {
        var (publisher, collection) = await FormReferences.LoadPublisherAsync(
            context, content.PublisherId, content.PublisherCollectionId, cancellationToken);

        var edition = new Edition(album, publisher);
        ApplyDetails(edition, content, publisher, collection);
        // Not owned yet, the edition has no amount its year could leave undated.
        edition.SetPublicationYear(content.PublicationYear);
        context.Editions.Add(edition);
        return edition;
    }

    private static async Task<EditionForm> UpdateAsync(
        Guid rootId, Guid id, UpdateEditionRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, request.AlbumVersion, cancellationToken: cancellationToken);
        var edition = album.Editions.SingleOrDefault(e => e.Id == id) ?? throw new EntityNotFoundException(typeof(Edition), id);
        var content = request.Content;
        var (publisher, collection) = await FormReferences.LoadPublisherAsync(
            context, content.PublisherId, content.PublisherCollectionId, cancellationToken);

        ApplyDetails(edition, content, publisher, collection);
        edition.SetPublicationYearAndAcquisition(content.PublicationYear, ToAcquisition(content));

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(edition, context.VersionOf(album));
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

    private static EditionForm ToForm(Edition edition, uint albumVersion) =>
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
            albumVersion);
}
