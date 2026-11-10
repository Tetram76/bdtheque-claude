using System.Data;
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

/// <summary>Administration of the series (<c>/admin/series</c>).</summary>
internal static class SeriesEndpoints
{
    // fonctionnel/regles-metier/suppression-des-entites.md: a series is refused deletion while it contains
    // albums, and its template contributions are deleted with it. Its genres and universes are
    // associations that merely disappear, without any record being concerned.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Reference, EntityKind.Album,
            (context, id) => context.Albums.Where(a => a.SeriesId == id).Select(a => a.Id)),
        new(LinkNature.Composition, EntityKind.Contribution,
            (context, id) => context.Contributions.Where(c => c.SeriesId == id).Select(c => c.Id)),
    ];

    public static void MapSeries(this RouteGroupBuilder admin)
    {
        var series = admin.MapGroup("/series");
        series.MapGet("/", ListForAdministrationAsync);
        series.MapGet("/new", GetNew);
        series.MapGet("/{id:guid}", GetAsync);
        series.MapPost("/{id:guid}/album-contributions", GetAlbumContributionsAsync);
        series.MapPost("/", CreateAsync);
        series.MapPut("/{id:guid}", UpdateAsync);
        series.MapDeletion<Series>(DeletionLinks);
    }

    public static void MapSeriesCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/series", ListAsync);
        catalog.MapGet("/series/{id:guid}", GetDetailAsync);
    }

    /// <summary>
    /// A series with its albums, in its order, its universes, each with its ancestors, and what describes
    /// it of its template: its publisher, collection and authors
    /// (fonctionnel/structure-de-l-application.md).
    /// </summary>
    private static async Task<SeriesDetail> GetDetailAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot for the reads below, which are assembled into one another.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var series = await context.Series.AsNoTracking()
                         .Where(s => s.Id == id)
                         .Select(CatalogExpressions.Expand((Series s) => new
                         {
                             s.Title,
                             s.Status,
                             s.TheoreticalVolumeCount,
                             s.IsComplete,
                             s.Summary,
                             s.PersonalNotes,
                             Publisher = s.TemplatePublisher == null
                                 ? null
                                 : new PublisherListItem(s.TemplatePublisher.Id, s.TemplatePublisher.Name, s.TemplatePublisher.Website),
                             PublisherCollection = s.TemplatePublisherCollection == null
                                 ? null
                                 : new PublisherCollectionListItem(
                                     s.TemplatePublisherCollection.Id, s.TemplatePublisherCollection.Name,
                                     s.TemplatePublisherCollection.PublisherId, s.TemplatePublisherCollection.Publisher.Name),
                             Genres = s.Genres.OrderBy(g => g.Label).ThenBy(g => g.Id).Select(g => new GenreListItem(g.Id, g.Label)).ToList(),
                             Universes = s.Universes.OrderBy(u => u.Name).ThenBy(u => u.Id).Select(u => u.ToListItem()).ToList(),
                             Contributions = s.TemplateContributions.ToItems().ToList(),
                             s.CreatedAt,
                             s.ModifiedAt,
                         }))
                         .SingleOrDefaultAsync(cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Series), id);

        var albums = await ReadAlbumsAsync(id, context, cancellationToken);
        var overview = (await CatalogWorks.OverviewsAsync(context, [id], cancellationToken))[id];
        var universes = await UniverseTree.InHierarchyAsync(context, series.Universes, cancellationToken);

        return new SeriesDetail(
            id, series.Title, EnumMapping.Map<ContractEnums.SeriesStatus>(series.Status), series.TheoreticalVolumeCount, series.IsComplete,
            series.Summary, series.PersonalNotes, series.Publisher, series.PublisherCollection, series.Genres, universes,
            series.Contributions, overview, albums, series.CreatedAt, series.ModifiedAt);
    }

    /// <summary>
    /// Every album of a series, as a card, in its order (fonctionnel/regles-metier/ordre-des-albums-dans-une-serie.md): those
    /// of the record of the series, and of the rest of the series on the record of one of its albums.
    /// </summary>
    internal static Task<List<AlbumCard>> ReadAlbumsAsync(Guid seriesId, BdthequeDbContext context, CancellationToken cancellationToken) =>
        context.Albums.AsNoTracking()
            .Where(a => a.SeriesId == seriesId)
            .OrderInSeries()
            .Select(CatalogExpressions.Expand((Album a) => a.ToCard()))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// The contributions the series gives an album attached to it, computed by the domain: the entry form of the album asks for
    /// them as soon as the series is chosen (fonctionnel/regles-metier/initialisation-des-contributions-depuis-la-serie.md).
    /// Nothing is written.
    /// </summary>
    private static async Task<SeriesAlbumContributions> GetAlbumContributionsAsync(
        Guid id, SeriesAlbumContributionsRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var series = await WithTemplateContributionsAsync(context, id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Series), id);
        // A previous series deleted meanwhile is none: the authors it gave the album are the album's own.
        var previous = request.PreviousSeriesId is { } previousId ? await WithTemplateContributionsAsync(context, previousId, cancellationToken) : null;
        var credits = request.Contributions.Select(c => (c.AuthorId, EnumMapping.Map<DomainEnums.ContributionRole>(c.Role)!.Value)).ToList();
        var contributions = series.ContributionsForAlbum(credits, previous, request.NewAlbum);
        return contributions is null
            ? new SeriesAlbumContributions(false, [])
            : new SeriesAlbumContributions(true,
            [
                .. contributions
                    .OrderBy(c => c.Role).ThenBy(c => c.Author.SortKey).ThenBy(c => c.AuthorId)
                    .Select(c => new SeriesAlbumContribution(
                        new AuthorListItem(c.AuthorId, c.Author.LastName, c.Author.FirstName, c.Author.Pseudonym),
                        EnumMapping.Map<ContractEnums.ContributionRole>(c.Role)!.Value)),
            ]);
    }

    private static Task<Series?> WithTemplateContributionsAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken) =>
        context.Series.AsNoTracking()
            .Include(s => s.TemplateContributions).ThenInclude(c => c.Author)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <summary>The series found by their title, narrowed by the cross filters (choix-implementation/api/recherche.md).</summary>
    private static async Task<Page<SeriesListItem>> ListAsync(
        BdthequeDbContext context, [AsParameters] SeriesFilters filters, CancellationToken cancellationToken, int page = 1,
        int pageSize = Paging.DefaultSize)
    {
        var series = await filters.ApplyAsync(context.Series.AsNoTracking(), context, cancellationToken);
        return await series
            .OrderBy(s => s.SortKey)
            .ThenBy(s => s.Id)
            .ToPageAsync(page, pageSize, s => new SeriesListItem(s.Id, s.Title), cancellationToken);
    }

    /// <summary>
    /// The list of the series of the administration (visuel/listes/liste-par-liste.md): the search, navigation and cross
    /// filters of the list of the consultation, in the same order. Each row carries the pile of covers and the number of
    /// albums of the series, as the consultation presents them, its albums in the collection, its status, whether it is
    /// complete, its modification and its version, which its deletion sends back.
    /// </summary>
    private static async Task<SeriesAdminList> ListForAdministrationAsync(
        BdthequeDbContext context, [AsParameters] SeriesFilters filters, CancellationToken cancellationToken, int page = 1,
        int pageSize = Paging.DefaultSize)
    {
        var series = await filters.ApplyAsync(context.Series.AsNoTracking(), context, cancellationToken);
        var rows = await series
            .OrderBy(s => s.SortKey)
            .ThenBy(s => s.Id)
            .ToPageAsync(page, pageSize, CatalogExpressions.Expand((Series s) => new
            {
                s.Id,
                s.Title,
                s.Status,
                AlbumsInCollectionCount = context.Albums.Count(a => a.SeriesId == s.Id && a.Editions.Any(e => e.IsOwned())),
                s.IsComplete,
                s.ModifiedAt,
                Version = EF.Property<uint>(s, BdthequeDbContext.VersionProperty),
            }), cancellationToken);
        var overviews = await CatalogWorks.OverviewsAsync(context, [.. rows.Items.Select(r => r.Id)], cancellationToken);

        var items = rows.Items
            .Select(r => new SeriesAdminListItem(
                r.Id, r.Title, EnumMapping.Map<ContractEnums.SeriesStatus>(r.Status), overviews[r.Id], r.AlbumsInCollectionCount, r.IsComplete,
                r.ModifiedAt, r.Version))
            .ToList();
        return new SeriesAdminList(
            new Page<SeriesAdminListItem>(items, rows.Number, rows.Size, rows.TotalCount),
            await filters.NameAsync(context, cancellationToken));
    }

    /// <summary>
    /// What the form of a new series starts from: nothing, but the values proposed to any new edition, which pre-fill those
    /// the series proposes to the new editions of its albums (modele-metier/serie.md). Only a starting point: nothing is
    /// written, and the series saves what the form sends.
    /// </summary>
    private static SeriesContent GetNew()
    {
        var proposed = EditionPreset.Defaults;
        return new SeriesContent(
            "", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(
                proposed.PublisherId,
                proposed.PublisherCollectionId,
                EnumMapping.Map<ContractEnums.EditionCategory>(proposed.Category),
                EnumMapping.Map<ContractEnums.EditionCondition>(proposed.Condition),
                EnumMapping.Map<ContractEnums.BindingType>(proposed.Binding),
                EnumMapping.Map<ContractEnums.BookOrientation>(proposed.Orientation),
                EnumMapping.Map<ContractEnums.ReadingDirection>(proposed.ReadingDirection),
                EnumMapping.Map<ContractEnums.EditionFormat>(proposed.Format),
                proposed.IsColor),
            [], [], []);
    }

    private static async Task<SeriesForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // Tracked, for the version of the aggregate (BdthequeDbContext.VersionOf): the context lives
        // for this request only.
        var series = await WithChildren(context.Series).SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Series), id);
        return await ToFormAsync(context, series, cancellationToken);
    }

    private static async Task<Created<SeriesForm>> CreateAsync(
        SeriesContent request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var added = await FormReferences.CreateAddedAsync(context, request.NewRecords, cancellationToken);
        var series = new Series(request.Title);
        await ApplyAsync(series, request, added, context, cancellationToken);
        context.Series.Add(series);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created($"/admin/series/{series.Id}", await ToFormAsync(context, series, cancellationToken));
    }

    private static async Task<SeriesForm> UpdateAsync(
        Guid id, UpdateSeriesRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var series = await context.LoadAggregateForWriteAsync<Series>(id, request.Version, WithChildren, cancellationToken);
        var added = await FormReferences.CreateAddedAsync(context, request.Content.NewRecords, cancellationToken);
        await ApplyAsync(series, request.Content, added, context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToFormAsync(context, series, cancellationToken);
    }

    // Its template publisher and collection, and the authors of its contributions, for the form to name them.
    private static IQueryable<Series> WithChildren(IQueryable<Series> query) =>
        query.Include(s => s.Genres).Include(s => s.Universes).Include(s => s.TemplatePublisher).Include(s => s.TemplatePublisherCollection)
            .Include(s => s.TemplateContributions).ThenInclude(c => c.Author);

    /// <summary>
    /// Applies the whole form to the series: scalar fields, then the records it references, each of
    /// which must still exist (a genre, universe, author, publisher or collection deleted since the
    /// form was read is a functional error), unless the form added them on the spot.
    /// </summary>
    private static async Task ApplyAsync(
        Series series, SeriesContent content, AddedRecords added, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        series.SetTitle(content.Title);
        if (content.ManualSortKey is { } manualSortKey)
            series.SetSortKey(manualSortKey);
        else
            series.ResetSortKey();
        series.SetStatus(EnumMapping.Map<DomainEnums.SeriesStatus>(content.Status));
        series.SetTheoreticalVolumeCount(content.TheoreticalVolumeCount);
        series.SetComplete(content.IsComplete);
        series.SetExcludeFromMissingVolumes(content.ExcludeFromMissingVolumes);
        series.SetExcludeFromReleaseEstimates(content.ExcludeFromReleaseEstimates);
        series.SetSummary(content.Summary);
        series.SetPersonalNotes(content.PersonalNotes);

        var template = content.EditionTemplate;
        series.SetTemplateCondition(EnumMapping.Map<DomainEnums.EditionCondition>(template.Condition));
        series.SetTemplateEditionCategory(EnumMapping.Map<DomainEnums.EditionCategory>(template.Category));
        series.SetTemplateBinding(EnumMapping.Map<DomainEnums.BindingType>(template.Binding));
        series.SetTemplateOrientation(EnumMapping.Map<DomainEnums.BookOrientation>(template.Orientation));
        series.SetTemplateReadingDirection(EnumMapping.Map<DomainEnums.ReadingDirection>(template.ReadingDirection));
        series.SetTemplateFormat(EnumMapping.Map<DomainEnums.EditionFormat>(template.Format));
        series.SetTemplateIsColor(template.IsColor);
        var (publisher, collection) = await FormReferences.LoadPublisherAsync(
            context, template.PublisherId, template.PublisherCollectionId, cancellationToken);
        series.SetTemplate(publisher, collection);

        var genres = await FormReferences.LoadAsync(context.Genres, content.GenreIds, cancellationToken, added.Genres);
        FormReferences.ReplaceAssociations(series.Genres, genres, series.AddGenre, series.RemoveGenre);
        var universes = await FormReferences.LoadAsync(context.Universes, content.UniverseIds, cancellationToken, added.Universes);
        FormReferences.ReplaceAssociations(series.Universes, universes, series.AddUniverse, series.RemoveUniverse);

        series.SetTemplateContributions(await FormReferences.LoadContributionsAsync(context, content.Contributions, cancellationToken, added.Authors));
    }

    private static async Task<SeriesForm> ToFormAsync(BdthequeDbContext context, Series series, CancellationToken cancellationToken) =>
        new(series.Id, series.SortKey,
            new SeriesContent(
                series.Title,
                series.IsManualSortKey ? series.SortKey : null,
                EnumMapping.Map<ContractEnums.SeriesStatus>(series.Status),
                series.TheoreticalVolumeCount,
                series.IsComplete,
                series.ExcludeFromMissingVolumes,
                series.ExcludeFromReleaseEstimates,
                series.Summary,
                series.PersonalNotes,
                new SeriesEditionTemplate(
                    series.TemplatePublisherId,
                    series.TemplatePublisherCollectionId,
                    EnumMapping.Map<ContractEnums.EditionCategory>(series.TemplateEditionCategory),
                    EnumMapping.Map<ContractEnums.EditionCondition>(series.TemplateCondition),
                    EnumMapping.Map<ContractEnums.BindingType>(series.TemplateBinding),
                    EnumMapping.Map<ContractEnums.BookOrientation>(series.TemplateOrientation),
                    EnumMapping.Map<ContractEnums.ReadingDirection>(series.TemplateReadingDirection),
                    EnumMapping.Map<ContractEnums.EditionFormat>(series.TemplateFormat),
                    series.TemplateIsColor),
                series.Genres.Select(g => g.Id).Order().ToList(),
                series.Universes.Select(u => u.Id).Order().ToList(),
                FormReferences.ToContents(series.TemplateContributions)),
            context.VersionOf(series),
            new SeriesFormReferences(
                series.TemplatePublisher is { } publisher ? new PublisherListItem(publisher.Id, publisher.Name, publisher.Website) : null,
                // The collection of the template belongs to its publisher (Series.SetTemplate).
                series.TemplatePublisherCollection is { } collection
                    ? new PublisherCollectionListItem(collection.Id, collection.Name, collection.PublisherId, series.TemplatePublisher!.Name)
                    : null,
                FormReferences.Genres(series.Genres),
                await FormReferences.UniversesInHierarchyAsync(context, series.Universes, cancellationToken),
                FormReferences.Authors(series.TemplateContributions)),
            series.CreatedAt,
            series.ModifiedAt);
}
