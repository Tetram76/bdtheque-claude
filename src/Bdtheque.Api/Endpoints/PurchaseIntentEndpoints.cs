using System.Linq.Expressions;
using Bdtheque.Api.Deletion;
using Bdtheque.Api.Visuals;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Purchase intents (fonctionnel/regles-metier/intention-d-achat.md): their administration
/// (<c>/admin/albums/{albumId}/purchase-intents</c>) and their public list (<c>/catalog/purchase-intents</c>).
/// The intents belong to the aggregate of their album, which alone sees them all: every write on them
/// locks the album and checks its version (choix-implementation/persistance/concurrence-d-acces.md). Their
/// realization is the acquisition of an edition (<see cref="EditionEndpoints"/>).
/// </summary>
internal static class PurchaseIntentEndpoints
{
    // fonctionnel/regles-metier/suppression-des-entites.md: an intent on an edition takes along the edition
    // targeted, not owned, with its visuals; an intent on the whole album, nothing else.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Composition, EntityKind.Edition,
            (context, id) => context.PurchaseIntents.Where(p => p.Id == id && p.EditionId != null).Select(p => p.EditionId!.Value)),
        new(LinkNature.Composition, EntityKind.EditionVisual,
            (context, id) => context.EditionVisuals
                .Where(v => context.PurchaseIntents.Any(p => p.Id == id && p.EditionId == v.EditionId))
                .Select(v => v.Id)),
    ];

    public static void MapPurchaseIntents(this RouteGroupBuilder admin)
    {
        // `rootId` is the name the common child deletion expects for the identifier of the root.
        var intents = admin.MapGroup("/albums/{rootId:guid}/purchase-intents");
        intents.MapGet("/", GetAsync);
        intents.MapGet("/new", GetNewAsync);
        intents.MapPost("/", CreateAsync);
        intents.MapPost("/{id:guid}/to-edition", ConvertToEditionAsync);
        intents.MapPost("/{id:guid}/to-album", ConvertToAlbumAsync);
        // The intents and the editions are always loaded with their album (AlbumConfiguration).
        intents.MapChildDeletion<Album, PurchaseIntent>(
            query => query, album => album.PurchaseIntents, DeletionLinks, (album, intent) => album.RemovePurchaseIntent(intent));

        admin.MapGet("/purchase-intents", ListForAdministrationAsync);
    }

    public static void MapPurchaseIntentList(this RouteGroupBuilder catalog) => catalog.MapGet("/purchase-intents", ListAsync);

    private static async Task<PurchaseIntentsForm> GetAsync(Guid rootId, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var read = await context.Albums.AsNoTracking()
                       .Where(a => a.Id == rootId)
                       .Select(a => new
                       {
                           Intents = a.PurchaseIntents.OrderBy(p => p.Id).Select(p => new PurchaseIntentForm(p.Id, p.EditionId)).ToList(),
                           Version = EF.Property<uint>(a, BdthequeDbContext.VersionProperty),
                       })
                       .SingleOrDefaultAsync(cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Album), rootId);
        return new PurchaseIntentsForm(rootId, read.Intents, read.Version);
    }

    // An intent on an edition creates a new edition, pre-filled as any new edition of the album.
    private static async Task<NewPurchaseIntentForm> GetNewAsync(Guid rootId, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var form = await EditionEndpoints.GetNewAsync(rootId, context, cancellationToken);
        var content = form.Content;
        return new NewPurchaseIntentForm(
            form.AlbumId,
            new PurchaseIntentEditionContent(
                content.PublisherId, content.PublisherCollectionId, content.PublicationYear, content.Isbn, content.Binding,
                content.Orientation, content.ReadingDirection, content.Format, content.PageCount, content.Category, content.IsColor),
            form.AlbumVersion,
            form.Album,
            form.References,
            form.SeriesTemplate);
    }

    private static async Task<Created<PurchaseIntentsForm>> CreateAsync(
        Guid rootId, CreatePurchaseIntentRequest request, BdthequeDbContext context, TimeProvider clock, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, request.AlbumVersion, cancellationToken: cancellationToken);

        PurchaseIntent intent;
        if (request.Edition is { } content)
        {
            var edition = await AddEditionAsync(context, album, content, cancellationToken);
            intent = album.AddPurchaseIntent(edition);
            // A new edition has no visual to remove: its pending visuals become its own.
            await EditionVisualEntries.ApplyAsync(context, edition, request.Visuals, clock, cancellationToken);
        }
        else
        {
            intent = album.AddPurchaseIntent();
        }

        context.PurchaseIntents.Add(intent);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created($"/admin/albums/{rootId}/purchase-intents", ToForm(album, context.VersionOf(album)));
    }

    private static async Task<PurchaseIntentsForm> ConvertToEditionAsync(
        Guid rootId, Guid id, ConvertPurchaseIntentToEditionRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, request.AlbumVersion, cancellationToken: cancellationToken);
        var intent = album.PurchaseIntents.SingleOrDefault(p => p.Id == id) ?? throw new EntityNotFoundException(typeof(PurchaseIntent), id);

        var edition = await AddEditionAsync(context, album, request.Edition, cancellationToken);
        context.PurchaseIntents.Add(album.ConvertPurchaseIntentToEdition(intent, edition));

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(album, context.VersionOf(album));
    }

    /// <summary>
    /// Converts an intent on an edition into an intent on the whole album: the edition targeted is
    /// deleted with its visuals, as by the deletion of the intent, whose impact the user confirmed
    /// (fonctionnel/regles-metier/intention-d-achat.md).
    /// </summary>
    private static async Task<PurchaseIntentsForm> ConvertToAlbumAsync(
        Guid rootId, Guid id, ConvertPurchaseIntentToAlbumRequest request, BdthequeDbContext context, VisualStorage storage,
        CancellationToken cancellationToken)
    {
        var album = await AggregateDeletion.DeleteChildAsync<Album, PurchaseIntent>(
            context, storage, rootId, id, request.AlbumVersion, request.Fingerprint, query => query, a => a.PurchaseIntents, DeletionLinks,
            (a, intent) => context.PurchaseIntents.Add(a.ConvertPurchaseIntentToAlbum(intent)), cancellationToken);
        return ToForm(album, context.VersionOf(album));
    }

    internal static readonly Expression<Func<PurchaseIntent, PurchaseIntentListItem>> ListItem = CatalogExpressions.Expand(
        (PurchaseIntent p) => new PurchaseIntentListItem(p.Id, p.Album.ToSummary(), p.Edition == null ? null : p.Edition.ToSummary()));

    /// <summary>The public list of the intents, in the order of their albums.</summary>
    private static Task<Page<PurchaseIntentListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, int page = 1, int pageSize = Paging.DefaultSize) =>
        InListOrder(context.PurchaseIntents.AsNoTracking()).ToPageAsync(page, pageSize, ListItem, cancellationToken);

    /// <summary>
    /// The list of the intents of the administration (visuel/listes/liste-par-liste.md), found by the title of their album or
    /// of its series, part of its label (fonctionnel/presentation/libelle-d-un-album.md), in the order of the public list. Each
    /// row carries the album and what the intent targets, the cover of the album, whether the intent can turn into one on the
    /// other target, when it was set, and the version of its album, which its removal sends back.
    /// </summary>
    private static Task<Page<PurchaseIntentAdminListItem>> ListForAdministrationAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, int page = 1, int pageSize = Paging.DefaultSize) =>
        InListOrder(context.PurchaseIntents.AsNoTracking().WhereContains(q, p => p.Album.Title, p => p.Album.Series!.Title))
            .ToPageAsync(page, pageSize, CatalogExpressions.Expand((PurchaseIntent p) => new PurchaseIntentAdminListItem(
                p.Id,
                p.Album.ToSummary(),
                p.Edition == null ? null : p.Edition.ToSummary(),
                CatalogExpressions.ToCover(p.Album.CoverVisualOf()),
                // fonctionnel/regles-metier/intention-d-achat.md: an intent on an edition turns into one on the album only when
                // that edition is the only one of the album; one on the album, which has no edition, always into one on an edition.
                p.EditionId == null || p.Album.Editions.Count == 1,
                p.CreatedAt,
                EF.Property<uint>(p.Album, BdthequeDbContext.VersionProperty))), cancellationToken);

    private static IOrderedQueryable<PurchaseIntent> InListOrder(IQueryable<PurchaseIntent> intents) =>
        intents.OrderByAlbum(p => p.Album).ThenBy(p => p.Id);

    // Entered with the minimum of fields: the traits of the copy keep the values of an edition not
    // owned (fonctionnel/regles-metier/intention-d-achat.md#réalisation-dune-intention).
    private static Task<Edition> AddEditionAsync(
        BdthequeDbContext context, Album album, PurchaseIntentEditionContent edition, CancellationToken cancellationToken) =>
        EditionEndpoints.AddEditionAsync(
            context, album,
            new EditionContent(
                edition.PublisherId, edition.PublisherCollectionId, edition.PublicationYear, edition.Isbn, edition.Binding,
                edition.Orientation, edition.ReadingDirection, edition.Format, edition.PageCount, edition.Category,
                IsDedicated: false, edition.IsColor, Condition: null, AcquisitionMode: null, IsSecondHand: false, AcquisitionDate: null,
                AcquisitionAmount: null, AcquisitionCurrency: null, IsFree: false, InitialValueAmount: null, InitialValueCurrency: null,
                PersonalReference: null, PersonalNotes: null),
            cancellationToken);

    private static PurchaseIntentsForm ToForm(Album album, uint albumVersion) =>
        new(album.Id,
            album.PurchaseIntents.OrderBy(p => p.Id).Select(p => new PurchaseIntentForm(p.Id, p.EditionId)).ToList(),
            albumVersion);
}
