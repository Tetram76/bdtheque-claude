using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
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
        await ApplyTemplatePublisherAsync(series, template, context, cancellationToken);

        var genres = await LoadAsync(context.Genres, content.GenreIds, cancellationToken);
        foreach (var removed in series.Genres.Where(g => genres.TrueForAll(x => x.Id != g.Id)).ToList())
            series.RemoveGenre(removed);
        genres.ForEach(series.AddGenre);

        var universes = await LoadAsync(context.Universes, content.UniverseIds, cancellationToken);
        foreach (var removed in series.Universes.Where(u => universes.TrueForAll(x => x.Id != u.Id)).ToList())
            series.RemoveUniverse(removed);
        universes.ForEach(series.AddUniverse);

        await ApplyContributionsAsync(series, content.Contributions, context, cancellationToken);
    }

    // The publisher and its collection go through the single domain operation that checks that the
    // collection belongs to the publisher: a mismatch is the business error of the domain.
    private static async Task ApplyTemplatePublisherAsync(
        Series series, SeriesEditionTemplate template, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var publisher = template.PublisherId is { } publisherId
            ? await context.Publishers.SingleOrDefaultAsync(p => p.Id == publisherId, cancellationToken)
              ?? throw new EntityNotFoundException(typeof(Publisher), publisherId)
            : null;
        var collection = template.PublisherCollectionId is { } collectionId
            ? await context.PublisherCollections.SingleOrDefaultAsync(c => c.Id == collectionId, cancellationToken)
              ?? throw new EntityNotFoundException(typeof(PublisherCollection), collectionId)
            : null;
        series.SetTemplate(publisher, collection);
    }

    // The contributions already credited are kept as they are, so that saving a series whose
    // contributions did not change writes none of them.
    private static async Task ApplyContributionsAsync(
        Series series, IReadOnlyList<SeriesContribution> requested, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var wanted = requested.Select(c => (c.AuthorId, Role: EnumMapping.Map<DomainEnums.ContributionRole>(c.Role)!.Value)).ToList();
        if (wanted.Count != wanted.Distinct().Count())
            throw new DomainRuleViolationException(
                DomainRules.ContributionAlreadyCredited, "An author cannot be credited twice with the same role on the series template.");

        var authors = await LoadAsync(context.Authors, wanted.Select(w => w.AuthorId).ToList(), cancellationToken);
        foreach (var stale in series.TemplateContributions.Where(c => !wanted.Contains((c.AuthorId, c.Role))).ToList())
        {
            series.RemoveTemplateContribution(stale);
            context.Contributions.Remove(stale);
        }

        foreach (var (authorId, role) in wanted.Where(w => !series.TemplateContributions.Any(c => (c.AuthorId, c.Role) == w)))
            series.AddTemplateContribution(authors.Single(a => a.Id == authorId), role);
    }

    private static async Task<List<TEntity>> LoadAsync<TEntity>(
        DbSet<TEntity> set, IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
        where TEntity : EntityBase
    {
        var distinctIds = ids.Distinct().ToList();
        var entities = await set.Where(e => distinctIds.Contains(e.Id)).ToListAsync(cancellationToken);
        var missing = distinctIds.Except(entities.Select(e => e.Id)).ToList();
        return missing.Count == 0 ? entities : throw new EntityNotFoundException(typeof(TEntity), missing[0]);
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
                series.TemplateContributions
                    .OrderBy(c => c.Role).ThenBy(c => c.AuthorId)
                    .Select(c => new SeriesContribution(c.AuthorId, EnumMapping.Map<ContractEnums.ContributionRole>(c.Role)!.Value))
                    .ToList()),
            context.VersionOf(series));
}
