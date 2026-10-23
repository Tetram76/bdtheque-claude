using Bdtheque.Api.Audience;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;
using Model = Bdtheque.Domain.Entities;
using ModelEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The audience measurement of the web interface (choix-implementation/mesure-audience.md § Mesure d'audience :
/// mise en œuvre): recorded by the frontend for any user (<c>/audience</c>), read in the administration only
/// (<c>/admin/audience</c>).
/// </summary>
internal static class AudienceEndpoints
{
    public static void MapAudienceRecording(this RouteGroupBuilder audience)
    {
        audience.MapPost("/visits", OpenAsync);
        audience.MapPost("/visits/{id:guid}/usages", RecordAsync);
    }

    public static void MapAudienceReading(this RouteGroupBuilder admin)
    {
        var audience = admin.MapGroup("/audience");
        audience.MapGet("/indicators", ReadIndicatorsAsync);
        audience.MapGet("/visits", ListVisitsAsync);
        audience.MapGet("/visits/{id:guid}", GetVisitAsync);
    }

    private static async Task<Created<OpenedVisit>> OpenAsync(
        OpenVisitRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var first = Model.AudienceVisit.Open(
            request.IsAdministrator,
            EnumMapping.Map<ModelEnums.DeviceType>(request.DeviceType)!.Value,
            EnumMapping.Map<ModelEnums.RenderingEngine>(request.RenderingEngine)!.Value,
            request.EngineVersion,
            ToUse(request.FirstUse));
        context.AudienceUsages.Add(first);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/audience/visits/{first.VisitId}", new OpenedVisit(first.VisitId));
    }

    // The visit is locked before its latest usage is read: two usages recorded at once (two tabs) would otherwise
    // both be compared with the same predecessor.
    private static async Task<NoContent> RecordAsync(Guid id, FunctionUse use, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.LockAsync<Model.AudienceVisit>(id, cancellationToken);
        var latest = await context.AudienceUsages.AsNoTracking()
            .Where(u => u.VisitId == id)
            .OrderByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .FirstAsync(cancellationToken);
        context.AudienceUsages.Add(latest.Follow(ToUse(use)));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<List<FunctionIndicators>> ReadIndicatorsAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, DateOnly? from = null, DateOnly? to = null,
        bool? administrator = null, ContractEnums.DeviceType? deviceType = null, ContractEnums.RenderingEngine? renderingEngine = null,
        string? engineVersion = null) =>
        AudienceCounting.ReadAsync(
            context,
            new AudienceSelection(
                from, to, administrator, EnumMapping.Map<ModelEnums.DeviceType>(deviceType),
                EnumMapping.Map<ModelEnums.RenderingEngine>(renderingEngine), engineVersion),
            cancellationToken);

    // From the latest visit to the oldest; their beginnings may coincide, their time-ordered identifiers not.
    private static Task<Page<AudienceVisitItem>> ListVisitsAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, int page = 1, int pageSize = Paging.DefaultSize) =>
        context.AudienceVisits.AsNoTracking()
            .OrderByDescending(v => v.CreatedAt)
            .ThenByDescending(v => v.Id)
            .ToPageAsync(page, pageSize, v => new AudienceVisitItem(
                v.Id, v.CreatedAt, v.IsAdministrator, (ContractEnums.DeviceType)v.DeviceType, (ContractEnums.RenderingEngine)v.RenderingEngine,
                v.EngineVersion, context.AudienceUsages.Count(u => u.VisitId == v.Id)), cancellationToken);

    private static async Task<AudienceVisitDetail> GetVisitAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var visit = await context.AudienceVisits.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Model.AudienceVisit), id);
        var usages = await context.AudienceUsages.AsNoTracking()
            .Where(u => u.VisitId == id)
            .OrderBy(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .ToListAsync(cancellationToken);

        return new AudienceVisitDetail(
            new AudienceVisitItem(
                visit.Id, visit.CreatedAt, visit.IsAdministrator, EnumMapping.Map<ContractEnums.DeviceType>(visit.DeviceType)!.Value,
                EnumMapping.Map<ContractEnums.RenderingEngine>(visit.RenderingEngine)!.Value, visit.EngineVersion, usages.Count),
            usages.Select(u => new AudienceUsageItem(
                    u.Id, u.CreatedAt, u.Function, ToRecord(u.PresentedRecord), u.Parameters, u.IsRepeat))
                .ToList());
    }

    private static Model.FunctionUse ToUse(FunctionUse use) =>
        new(
            use.Function,
            use.Record is null
                ? null
                : new Model.PresentedRecord(EnumMapping.Map<ModelEnums.RecordType>(use.Record.Type)!.Value, use.Record.Id),
            use.Parameters);

    private static PresentedRecord? ToRecord(Model.PresentedRecord? record) =>
        record is null ? null : new PresentedRecord(EnumMapping.Map<ContractEnums.RecordType>(record.Type)!.Value, record.Id);
}
