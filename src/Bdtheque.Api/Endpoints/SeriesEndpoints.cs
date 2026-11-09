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
        series.MapGet("/{id:guid}", GetAsync);
        series.MapGet("/{id:guid}/album-contributions", GetAlbumContributionsAsync);
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

        var albums = await context.Albums.AsNoTracking()
            .Where(a => a.SeriesId == id)
            .OrderInSeries()
            .Select(CatalogExpressions.Expand((Album a) => a.ToCard()))
            .ToListAsync(cancellationToken);
        var overview = (await CatalogWorks.OverviewsAsync(context, [id], cancellationToken))[id];
        var universes = await UniverseTree.InHierarchyAsync(context, series.Universes, cancellationToken);

        return new SeriesDetail(
            id, series.Title, EnumMapping.Map<ContractEnums.SeriesStatus>(series.Status), series.TheoreticalVolumeCount, series.IsComplete,
            series.Summary, series.PersonalNotes, series.Publisher, series.PublisherCollection, series.Genres, universes,
            series.Contributions, overview, albums, series.CreatedAt, series.ModifiedAt);
    }

    /// <summary>
    /// The authors the series gives an album attached to it, computed by the domain: the entry form of the album asks for them as
    /// soon as the series is chosen (fonctionnel/regles-metier/initialisation-des-contributions-depuis-la-serie.md).
    /// </summary>
    /// <param name="creditedRoles">The roles where the album already has authors, which keep theirs.</param>
    private static async Task<IReadOnlyList<SeriesAlbumContribution>> GetAlbumContributionsAsync(
        Guid id, BdthequeDbContext context, CancellationToken cancellationToken, ContractEnums.ContributionRole[]? creditedRoles = null)
    {
        var series = await context.Series.AsNoTracking()
                         .Include(s => s.TemplateContributions).ThenInclude(c => c.Author)
                         .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Series), id);

        return
        [
            .. series.ContributionsForAlbum([.. (creditedRoles ?? []).Select(r => EnumMapping.Map<DomainEnums.ContributionRole>(r)!.Value)])
                .OrderBy(c => c.Role).ThenBy(c => c.Author.SortKey).ThenBy(c => c.AuthorId)
                .Select(c => new SeriesAlbumContribution(
                    new AuthorListItem(c.AuthorId, c.Author.LastName, c.Author.FirstName, c.Author.Pseudonym),
                    EnumMapping.Map<ContractEnums.ContributionRole>(c.Role)!.Value)),
        ];
    }

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

    private static async Task<SeriesForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // Tracked, for the version of the aggregate (BdthequeDbContext.VersionOf): the context lives
        // for this request only.
        var series = await WithChildren(context.Series).SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Series), id);
        return ToForm(context, series);
    }

    private static async Task<Created<SeriesForm>> CreateAsync(
        SeriesContent request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var series = new Series(request.Title);
        await ApplyAsync(series, request, context, cancellationToken);
        context.Series.Add(series);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/series/{series.Id}", ToForm(context, series));
    }

    private static async Task<SeriesForm> UpdateAsync(
        Guid id, UpdateSeriesRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var series = await context.LoadAggregateForWriteAsync<Series>(id, request.Version, WithChildren, cancellationToken);
        await ApplyAsync(series, request.Content, context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, series);
    }

    private static IQueryable<Series> WithChildren(IQueryable<Series> query) =>
        query.Include(s => s.Genres).Include(s => s.Universes).Include(s => s.TemplateContributions);

    /// <summary>
    /// Applies the whole form to the series: scalar fields, then the records it references, each of
    /// which must still exist (a genre, universe, author, publisher or collection deleted since the
    /// form was read is a functional error).
    /// </summary>
    private static async Task ApplyAsync(Series series, SeriesContent content, BdthequeDbContext context, CancellationToken cancellationToken)
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

        var genres = await FormReferences.LoadAsync(context.Genres, content.GenreIds, cancellationToken);
        FormReferences.ReplaceAssociations(series.Genres, genres, series.AddGenre, series.RemoveGenre);
        var universes = await FormReferences.LoadAsync(context.Universes, content.UniverseIds, cancellationToken);
        FormReferences.ReplaceAssociations(series.Universes, universes, series.AddUniverse, series.RemoveUniverse);

        series.SetTemplateContributions(await FormReferences.LoadContributionsAsync(context, content.Contributions, cancellationToken));
    }

    private static SeriesForm ToForm(BdthequeDbContext context, Series series) =>
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
            context.VersionOf(series));
}
