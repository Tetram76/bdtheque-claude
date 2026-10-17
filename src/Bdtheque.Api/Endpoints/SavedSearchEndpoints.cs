using Bdtheque.Api.Search;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using DomainEnums = Bdtheque.Domain.Enums;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The saved searches (fonctionnel.md § Recherches enregistrées): their administration
/// (<c>/admin/saved-searches</c>), and their list, criteria and replay in the consultation
/// (<c>/catalog/saved-searches</c>).
/// </summary>
/// <remarks>
/// The criteria are those of the advanced search, kept in its JSON form (<see cref="SearchCriteriaJson"/>), into
/// which the variables point. A saved search is checked against the schema of the advanced search when
/// written, and replayed by the same search as its entity's route (<see cref="AdvancedSearchEndpoints"/>).
/// </remarks>
internal static class SavedSearchEndpoints
{
    public static void MapSavedSearches(this RouteGroupBuilder admin)
    {
        var searches = admin.MapGroup("/saved-searches");
        searches.MapGet("/{id:guid}", GetAsync);
        searches.MapPost("/", CreateAsync);
        searches.MapPut("/{id:guid}", UpdateAsync);
        // fonctionnel.md § Suppression des entités: deleted with no other effect.
        searches.MapDeletion<SavedSearch>([]);
    }

    public static void MapSavedSearchCatalog(this RouteGroupBuilder catalog)
    {
        var searches = catalog.MapGroup("/saved-searches");
        searches.MapGet("/", ListAsync);
        searches.MapGet("/{id:guid}", GetDetailAsync);
        searches.MapPost("/{id:guid}/results", ReplayAsync);
    }

    private static Task<Page<SavedSearchListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, int page = 1, int pageSize = Paging.DefaultSize) =>
        context.SavedSearches.AsNoTracking()
            .WhereContains(q, s => s.Name)
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .ToPageAsync(page, pageSize, s => new SavedSearchListItem(s.Id, s.Name, (EntityKind)s.Entity), cancellationToken);

    private static async Task<SavedSearchDetail> GetDetailAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var search = await FindAsync(context, id, cancellationToken);
        return new SavedSearchDetail(
            search.Id, search.Name, EnumMapping.Map<EntityKind>(search.Entity)!.Value, SearchCriteriaJson.Deserialize(search.Criteria),
            VariablesOf(search.Criteria, search.Variables));
    }

    /// <summary>
    /// Runs the search, each variable given a value replaced by it and the others keeping their default
    /// value; the results are those of the advanced search of its entity.
    /// </summary>
    private static async Task<Ok<object>> ReplayAsync(
        Guid id, ReplaySavedSearchRequest request, BdthequeDbContext context, CancellationToken cancellationToken, int page = 1,
        int pageSize = Paging.DefaultSize)
    {
        var search = await FindAsync(context, id, cancellationToken);
        var criteria = SearchCriteriaJson.Deserialize(search.CriteriaWith(request.Values ?? new Dictionary<string, string>()));
        return TypedResults.Ok(await AdvancedSearchEndpoints.SearchAsync(
            EnumMapping.Map<EntityKind>(search.Entity)!.Value, criteria, context, page, pageSize, cancellationToken));
    }

    /// <summary>The variables of a saved search, each with its default value: the constant of <paramref name="criteria"/> it replaces.</summary>
    private static List<SavedSearchVariable> VariablesOf(string criteria, IEnumerable<SearchVariable> variables) =>
        variables.Select(v => new SavedSearchVariable(v.Name, v.Location, SavedSearch.DefaultValueOf(criteria, v))).ToList();

    private static async Task<SavedSearchForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        ToForm(context, await context.SavedSearches.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(SavedSearch), id));

    private static async Task<Created<SavedSearchForm>> CreateAsync(
        SavedSearchContent content, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var (entity, criteria, variables, designated) = Checked(content, context);
        var search = new SavedSearch(content.Name, entity, criteria, variables, designated);
        context.SavedSearches.Add(search);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/saved-searches/{search.Id}", ToForm(context, search));
    }

    private static async Task<SavedSearchForm> UpdateAsync(
        Guid id, UpdateSavedSearchRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var search = await context.LoadAggregateForWriteAsync<SavedSearch>(id, request.Version, cancellationToken: cancellationToken);
        var (entity, criteria, variables, designated) = Checked(request.Content, context);
        search.SetName(request.Content.Name);
        search.SetSearch(entity, criteria, variables, designated);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, search);
    }

    /// <summary>
    /// The search as the domain keeps it, once its criteria are known to be a search of its entity: translated
    /// as they would be replayed, so that a criterion left without its value is the same business error
    /// (<c>AdvancedSearch.ValueMissing</c>), and criteria the schema does not describe the same technical error.
    /// The records the criteria designate, which only the schema tells, come with them.
    /// </summary>
    private static (DomainEnums.EntityKind Entity, string Criteria, IEnumerable<SearchVariable> Variables, IEnumerable<DesignatedRecord> Designated)
        Checked(SavedSearchContent content, BdthequeDbContext context)
    {
        var criteria = content.Criteria ?? throw new ArgumentException("A saved search has criteria.", nameof(content));
        var entity = AdvancedSearchSchema.Of(content.Entity);
        // Only built, never run: the depth of the hierarchy of the universes does not change what is checked.
        SearchTranslator.Check(entity, criteria, new SearchScope(context, 0));
        return (
            EnumMapping.Map<DomainEnums.EntityKind>(content.Entity)!.Value,
            SearchCriteriaJson.Serialize(criteria),
            (content.Variables ?? []).Select(v => new SearchVariable(v.Name, v.Location)),
            SearchDesignations.Of(entity, criteria).Select(r => new DesignatedRecord(EnumMapping.Map<DomainEnums.EntityKind>(r.Kind)!.Value, r.Id)));
    }

    private static async Task<SavedSearch> FindAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken) =>
        await context.SavedSearches.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(SavedSearch), id);

    private static SavedSearchForm ToForm(BdthequeDbContext context, SavedSearch search) =>
        new(
            search.Id,
            new SavedSearchContent(
                search.Name, EnumMapping.Map<EntityKind>(search.Entity)!.Value, SearchCriteriaJson.Deserialize(search.Criteria),
                search.Variables.Select(v => new SearchVariableContent(v.Name, v.Location)).ToList()),
            context.VersionOf(search));
}
