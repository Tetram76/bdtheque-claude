using System.Data;
using System.Linq.Expressions;
using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>Administration of the albums (<c>/admin/albums</c>).</summary>
internal static class AlbumEndpoints
{
    // fonctionnel/regles-metier/suppression-des-entites.md: an album takes along its editions (with their
    // visuals and purchase intents), its contributions and its own purchase intent. Its genres and
    // universes are associations that merely disappear, without any record being concerned.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Composition, EntityKind.Edition,
            (context, id) => context.Editions.Where(e => e.AlbumId == id).Select(e => e.Id)),
        new(LinkNature.Composition, EntityKind.EditionVisual,
            (context, id) => context.EditionVisuals.Where(v => v.Edition.AlbumId == id).Select(v => v.Id)),
        new(LinkNature.Composition, EntityKind.Contribution,
            (context, id) => context.Contributions.Where(c => c.AlbumId == id).Select(c => c.Id)),
        // Every intent carries the identifier of its album, that of an intent on an edition included.
        new(LinkNature.Composition, EntityKind.PurchaseIntent,
            (context, id) => context.PurchaseIntents.Where(p => p.AlbumId == id).Select(p => p.Id)),
    ];

    public static void MapAlbums(this RouteGroupBuilder admin)
    {
        var albums = admin.MapGroup("/albums");
        albums.MapGet("/", ListForAdministrationAsync);
        albums.MapGet("/{id:guid}", GetAsync);
        albums.MapPost("/", CreateAsync);
        albums.MapPut("/{id:guid}", UpdateAsync);
        albums.MapDeletion<Album>(DeletionLinks);
    }

    public static void MapAlbumCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/albums", ListAsync);
        catalog.MapGet("/albums/{id:guid}", GetDetailAsync);
    }

    internal static readonly Expression<Func<Album, AlbumSummary>> ListItem = CatalogExpressions.Expand((Album a) => a.ToSummary());

    /// <summary>
    /// An album with what it is linked with: the genres and universes displayed — those of the album and
    /// of its series, without duplicates (fonctionnel/regles-metier/genres-et-univers-d-un-album.md) —, each universe
    /// with its ancestors, its contributions, and every edition, whole, which has no record of its own
    /// (fonctionnel/structure-de-l-application.md).
    /// </summary>
    private static async Task<AlbumDetail> GetDetailAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot for the reads below, which are assembled into one another.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var album = await context.Albums.AsNoTracking()
                        .Where(a => a.Id == id)
                        .Select(CatalogExpressions.Expand((Album a) => new
                        {
                            Album = a.ToSummary(),
                            a.FirstPublicationYear,
                            a.FirstPublicationMonth,
                            a.Summary,
                            a.PersonalNotes,
                            a.Rating,
                            Genres = context.Genres
                                .Where(g => a.Genres.Any(o => o.Id == g.Id) || a.Series!.Genres.Any(o => o.Id == g.Id))
                                .OrderBy(g => g.Label).ThenBy(g => g.Id)
                                .Select(g => new GenreListItem(g.Id, g.Label)).ToList(),
                            Universes = context.Universes
                                .Where(u => a.Universes.Any(o => o.Id == u.Id) || a.Series!.Universes.Any(o => o.Id == u.Id))
                                .OrderBy(u => u.Name).ThenBy(u => u.Id)
                                .Select(u => u.ToListItem()).ToList(),
                            Contributions = a.Contributions.ToItems().ToList(),
                            PurchaseIntent = a.PurchaseIntents.Where(p => p.EditionId == null).Select(p => p.ToItem()).FirstOrDefault(),
                            a.CreatedAt,
                            a.ModifiedAt,
                        }))
                        .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Album), id);

        var editions = await EditionEndpoints.ReadDetailsAsync(id, context, cancellationToken);
        var universes = await UniverseTree.InHierarchyAsync(context, album.Universes, cancellationToken);

        return new AlbumDetail(
            album.Album, album.FirstPublicationYear, album.FirstPublicationMonth, album.Summary, album.PersonalNotes,
            EnumMapping.Map<ContractEnums.AlbumRating>(album.Rating), album.Genres, universes, album.Contributions, editions,
            album.PurchaseIntent, album.CreatedAt, album.ModifiedAt);
    }

    /// <summary>
    /// The albums found by their title or that of their series, part of their label
    /// (fonctionnel/presentation/libelle-d-un-album.md), narrowed by the cross filters
    /// (choix-implementation/api/recherche.md). An album
    /// without a title of its own is filed under the current sort key and entry of its series.
    /// </summary>
    private static async Task<Page<AlbumSummary>> ListAsync(
        BdthequeDbContext context, [AsParameters] AlbumFilters filters, CancellationToken cancellationToken, int page = 1,
        int pageSize = Paging.DefaultSize)
    {
        var albums = await filters.ApplyAsync(context.Albums.AsNoTracking(), context, cancellationToken);
        return await albums.OrderByAlbum(a => a).ToPageAsync(page, pageSize, ListItem, cancellationToken);
    }

    /// <summary>
    /// The list of the albums of the administration (visuel/liste-albums/contenu.md): the search, navigation and cross
    /// filters of the consultation, and where each album stands with regard to the collection, in the same order.
    /// </summary>
    private static async Task<AlbumAdminList> ListForAdministrationAsync(
        BdthequeDbContext context, [AsParameters] AlbumFilters filters, CancellationToken cancellationToken,
        AlbumCollectionState? state = null, int page = 1, int pageSize = Paging.DefaultSize)
    {
        var albums = await filters.ApplyAsync(context.Albums.AsNoTracking(), context, cancellationToken);
        if (state is { } kept)
            albums = albums.Where(CatalogExpressions.Expand((Album a) => a.CollectionState() == kept));

        var rows = await albums.OrderByAlbum(a => a).ToPageAsync(page, pageSize, AdminListItem, cancellationToken);
        // An author credited with several roles is one of the authors once, at the place of their first role.
        var items = rows.Items.Select(item => item with { Authors = [.. item.Authors.DistinctBy(a => a.Id)] }).ToList();
        return new AlbumAdminList(
            new Page<AlbumAdminListItem>(items, rows.Number, rows.Size, rows.TotalCount),
            await filters.NameAsync(context, cancellationToken));
    }

    // Its authors in the order of its contributions, an author credited with several roles once per role: the list
    // tells them apart once read.
    private static readonly Expression<Func<Album, AlbumAdminListItem>> AdminListItem = CatalogExpressions.Expand((Album a) => new AlbumAdminListItem(
        a.ToSummary(),
        CatalogExpressions.ToCover(a.CoverVisualId()),
        a.Contributions
            .OrderBy(c => c.Role).ThenBy(c => c.Author.SortKey).ThenBy(c => c.AuthorId)
            .Select(c => new AuthorListItem(c.AuthorId, c.Author.LastName, c.Author.FirstName, c.Author.Pseudonym))
            .ToList(),
        a.FirstPublicationYear,
        a.FirstPublicationMonth,
        a.Editions.Count(e => e.IsOwned()),
        a.PurchaseIntents.Count,
        a.CollectionState(),
        a.ModifiedAt,
        EF.Property<uint>(a, BdthequeDbContext.VersionProperty)));

    private static async Task<AlbumForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // Tracked, for the version of the aggregate (BdthequeDbContext.VersionOf): the context lives
        // for this request only.
        var album = await WithAssociations(context.Albums).SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Album), id);
        return ToForm(context, album);
    }

    private static async Task<Created<AlbumForm>> CreateAsync(
        AlbumContent request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var series = await LoadSeriesAsync(request.SeriesId, context, cancellationToken);
        var contributions = await FormReferences.LoadContributionsAsync(context, request.Contributions, cancellationToken);
        var album = new Album(request.Title, series, contributions);
        await ApplyAsync(album, request, context, cancellationToken);
        context.Albums.Add(album);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/albums/{album.Id}", ToForm(context, album));
    }

    private static async Task<AlbumForm> UpdateAsync(
        Guid id, UpdateAlbumRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(id, request.Version, WithAssociations, cancellationToken);
        var content = request.Content;
        var series = await LoadSeriesAsync(content.SeriesId, context, cancellationToken);
        var contributions = await FormReferences.LoadContributionsAsync(context, content.Contributions, cancellationToken);

        album.SetTitleSeriesAndContributions(content.Title, series, contributions);
        await ApplyAsync(album, content, context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, album);
    }

    // The editions, intents and contributions are always loaded with the album (AlbumConfiguration).
    private static IQueryable<Album> WithAssociations(IQueryable<Album> query) =>
        query.Include(a => a.Genres).Include(a => a.Universes);

    // With its template contributions and their authors: attaching an album with no contribution to
    // the series credits them on the album.
    private static async Task<Series?> LoadSeriesAsync(Guid? seriesId, BdthequeDbContext context, CancellationToken cancellationToken) =>
        seriesId is { } id
            ? await context.Series.Include(s => s.TemplateContributions).ThenInclude(c => c.Author)
                  .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
              ?? throw new EntityNotFoundException(typeof(Series), id)
            : null;

    /// <summary>
    /// Applies the rest of the form to the album, its title, series and contributions being set
    /// together beforehand: the genres and universes it references must still exist (a record
    /// deleted since the form was read is a functional error).
    /// </summary>
    private static async Task ApplyAsync(Album album, AlbumContent content, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        if (content.ManualSortKey is { } manualSortKey)
            album.SetSortKey(manualSortKey);
        else
            album.ResetSortKey();
        album.SetTypeAndVolumeRange(
            EnumMapping.Map<DomainEnums.AlbumType>(content.Type)!.Value, content.StartVolumeNumber, content.EndVolumeNumber);
        album.SetSpecialIssue(content.IsSpecialIssue);
        album.SetVolumeNumber(content.VolumeNumber);
        album.SetFirstPublicationDate(content.FirstPublicationYear, content.FirstPublicationMonth);
        album.SetSummary(content.Summary);
        album.SetPersonalNotes(content.PersonalNotes);
        album.SetRating(EnumMapping.Map<DomainEnums.AlbumRating>(content.Rating));

        var genres = await FormReferences.LoadAsync(context.Genres, content.GenreIds, cancellationToken);
        FormReferences.ReplaceAssociations(album.Genres, genres, album.AddGenre, album.RemoveGenre);
        var universes = await FormReferences.LoadAsync(context.Universes, content.UniverseIds, cancellationToken);
        FormReferences.ReplaceAssociations(album.Universes, universes, album.AddUniverse, album.RemoveUniverse);
    }

    private static AlbumForm ToForm(BdthequeDbContext context, Album album) =>
        new(album.Id, album.SortKey,
            new AlbumContent(
                album.Title,
                album.IsManualSortKey ? album.SortKey : null,
                album.SeriesId,
                EnumMapping.Map<ContractEnums.AlbumType>(album.Type)!.Value,
                album.IsSpecialIssue,
                album.VolumeNumber,
                album.StartVolumeNumber,
                album.EndVolumeNumber,
                album.FirstPublicationYear,
                album.FirstPublicationMonth,
                album.Summary,
                album.PersonalNotes,
                EnumMapping.Map<ContractEnums.AlbumRating>(album.Rating),
                album.Genres.Select(g => g.Id).Order().ToList(),
                album.Universes.Select(u => u.Id).Order().ToList(),
                FormReferences.ToContents(album.Contributions)),
            context.VersionOf(album));
}
