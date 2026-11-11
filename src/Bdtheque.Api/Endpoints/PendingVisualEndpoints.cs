using Bdtheque.Api.Visuals;
using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The visuals waiting for the saving of their edition (<c>/admin/visuals</c>,
/// choix-implementation/domaine/visuels-stockage-et-traitement.md, Zone d'attente): uploaded while the edition is entered,
/// validated and converted as any visual, and kept until the edition is saved with them, which makes them its own.
/// </summary>
internal static class PendingVisualEndpoints
{
    public static void MapPendingVisuals(this RouteGroupBuilder admin)
    {
        var visuals = admin.MapGroup("/visuals");
        visuals.MapGet("/limits", (IOptions<VisualStorageOptions> options) =>
            new VisualLimits(options.Value.MaxFileSizeBytes, PendingVisual.Lifetime));
        // The form is read as a stream by the handler (VisualUploadReader), never bound by the framework.
        visuals.MapPost("/pending", UploadAsync).Accepts<IFormFile>("multipart/form-data");
        // The media of a pending visual, under the addresses of PendingVisualMediaRoutes.
        var pending = visuals.MapGroup("/pending/{id:guid}");
        pending.MapGet("/display", (Guid id, BdthequeDbContext context, VisualStorage storage, TimeProvider clock, HttpResponse response, CancellationToken cancellationToken) =>
            ServeAsync(id, storage.DisplayFile, context, clock, response, cancellationToken));
        pending.MapGet("/original", (Guid id, BdthequeDbContext context, VisualStorage storage, TimeProvider clock, HttpResponse response, CancellationToken cancellationToken) =>
            ServeAsync(id, storage.OriginalFile, context, clock, response, cancellationToken));
    }

    /// <summary>
    /// Uploads the file of a pending visual (form field <see cref="UploadVisualFields.File"/>), as a visual is: read, validated
    /// and converted in memory, one upload at a time, its files written just before its row, and deleted at once if the row
    /// cannot be saved.
    /// </summary>
    private static async Task<Created<PendingVisualForm>> UploadAsync(
        HttpRequest request, BdthequeDbContext context, VisualStorage storage, VisualUploadGate gate,
        IOptions<VisualStorageOptions> options, CancellationToken cancellationToken)
    {
        using var turn = await gate.EnterAsync(cancellationToken);
        var file = await VisualUploadReader.ReadFileAsync(request, options.Value.MaxFileSizeBytes, cancellationToken);
        var prepared = VisualImage.Prepare(file);

        var mediaReference = VisualStorage.NewMediaReference(prepared);
        var pending = new PendingVisual(mediaReference, prepared.Palette);
        context.PendingVisuals.Add(pending);
        await storage.WriteAsync(mediaReference, prepared, cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            storage.Delete(mediaReference);
            throw;
        }

        return TypedResults.Created($"/admin/visuals/pending/{pending.Id}", new PendingVisualForm(pending.Id, pending.ExpiresAt));
    }

    // A media of a pending visual, which the entry presents as the others (its display version, or its original opened from
    // its menu): for the administration only, while it is kept, and in a private cache, the visual being no one else's yet.
    private static async Task<PhysicalFileHttpResult> ServeAsync(
        Guid id, Func<string, VisualFile> fileOf, BdthequeDbContext context, TimeProvider clock, HttpResponse response,
        CancellationToken cancellationToken)
    {
        var pending = await context.PendingVisuals.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (pending is null || pending.IsExpiredAt(clock.GetUtcNow()))
            throw new EntityNotFoundException(typeof(PendingVisual), id);
        var file = fileOf(pending.MediaReference);
        response.Headers.CacheControl = "private, max-age=86400, immutable";
        return TypedResults.PhysicalFile(file.Path, file.ContentType);
    }
}
