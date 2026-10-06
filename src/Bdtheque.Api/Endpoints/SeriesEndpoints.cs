using Bdtheque.Api.Deletion;
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

/// <summary>Administration of the series (<c>/admin/series</c>).</summary>
internal static class SeriesEndpoints
{
    // fonctionnel.md § Suppression des entités: a series is refused deletion while it contains
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
        series.MapGet("/{id:guid}", GetAsync);
        series.MapPost("/", CreateAsync);
        series.MapPut("/{id:guid}", UpdateAsync);
        series.MapDeletion<Series>(DeletionLinks);
    }

    public static void MapSeriesList(this RouteGroupBuilder catalog) => catalog.MapGet("/series", ListAsync);

    /// <summary>
    /// The series found by their title, narrowed by the cross filters (choix-implementation.md §
    /// Recherche): credited and published through their albums, the source of truth of contributions and
    /// editions — the template of a series is only the starting point of the data entry.
    /// </summary>
    private static async Task<Page<SeriesListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, string? entry = null, Guid? authorId = null,
        Guid? publisherId = null, Guid? genreId = null, Guid? universeId = null, int page = 1, int pageSize = Paging.DefaultSize)
    {
        CatalogFilters.EnsureNavigationEntry(entry);
        var series = context.Series.AsNoTracking().WhereContains(q, s => s.Title);
        if (entry is not null)
            series = series.Where(s => s.NavigationEntry == entry);
        if (authorId is not null)
            series = series.Where(s => context.Albums.Any(a => a.SeriesId == s.Id && a.Contributions.Any(c => c.AuthorId == authorId)));
        if (publisherId is not null)
            series = series.Where(s => context.Albums.Any(a => a.SeriesId == s.Id && a.Editions.Any(e => e.PublisherId == publisherId)));
        if (genreId is not null)
            series = series.Where(s => s.Genres.Any(g => g.Id == genreId));
        if (universeId is not null)
        {
            var universeIds = await CatalogFilters.UniverseAndDescendantsAsync(context, universeId.Value, cancellationToken);
            series = series.Where(s => s.Universes.Any(u => universeIds.Contains(u.Id)));
        }

        return await series
            .OrderBy(s => s.SortKey)
            .ThenBy(s => s.Id)
            .ToPageAsync(page, pageSize, s => new SeriesListItem(s.Id, s.Title), cancellationToken);
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

    // Split, so that the three collections are not joined into a single cartesian result.
    private static IQueryable<Series> WithChildren(IQueryable<Series> query) =>
        query.Include(s => s.Genres).Include(s => s.Universes).Include(s => s.TemplateContributions).AsSplitQuery();

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

        await ApplyContributionsAsync(series, content.Contributions, context, cancellationToken);
    }

    // The contributions already credited are kept as they are, so that saving a series whose
    // contributions did not change writes none of them.
    private static async Task ApplyContributionsAsync(
        Series series, IReadOnlyList<ContributionContent> requested, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var wanted = requested.Select(c => (c.AuthorId, Role: EnumMapping.Map<DomainEnums.ContributionRole>(c.Role)!.Value)).ToList();
        if (wanted.Count != wanted.Distinct().Count())
            throw new DomainRuleViolationException(
                DomainRules.ContributionAlreadyCredited, "An author cannot be credited twice with the same role on the series template.");

        var credits = await FormReferences.LoadContributionsAsync(context, requested, cancellationToken);
        foreach (var stale in series.TemplateContributions.Where(c => !wanted.Contains((c.AuthorId, c.Role))).ToList())
        {
            series.RemoveTemplateContribution(stale);
            context.Contributions.Remove(stale);
        }

        foreach (var (author, role) in credits.Where(w => !series.TemplateContributions.Any(c => (c.AuthorId, c.Role) == (w.Author.Id, w.Role))))
            series.AddTemplateContribution(author, role);
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
