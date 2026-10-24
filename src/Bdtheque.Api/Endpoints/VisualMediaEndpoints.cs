using Bdtheque.Api.Visuals;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The media of the visuals (<see cref="VisualMediaRoutes"/>), which <c>api</c> alone serves: the volume
/// is private to it, as the database is, and its clients know neither its organization nor its
/// location (choix-implementation/domaine.md § Visuels : stockage et traitement).
/// </summary>
internal static class VisualMediaEndpoints
{
    // A visual never changes media, and a media is never rewritten (VisualStorage.NewMediaReference):
    // whatever a cache keeps under these addresses stays right, so that it never needs to ask again.
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    public static void MapVisualMedia(this RouteGroupBuilder catalog)
    {
        var visual = catalog.MapGroup("/visuals/{id:guid}");
        visual.MapGet("/display", (Guid id, BdthequeDbContext context, VisualStorage storage, HttpResponse response, CancellationToken cancellationToken) =>
            ServeAsync(id, storage.DisplayFile, context, response, cancellationToken));
        visual.MapGet("/original", (Guid id, BdthequeDbContext context, VisualStorage storage, HttpResponse response, CancellationToken cancellationToken) =>
            ServeAsync(id, storage.OriginalFile, context, response, cancellationToken));
    }

    // The file is streamed from the volume, never loaded whole in memory (originals of up to
    // Visuals:MaxFileSizeBytes). Its row, rather than the file name, designates it: only the media of
    // an existing visual is served, an orphan awaiting the reconciliation never.
    private static async Task<PhysicalFileHttpResult> ServeAsync(
        Guid id, Func<string, VisualFile> fileOf, BdthequeDbContext context, HttpResponse response, CancellationToken cancellationToken)
    {
        var mediaReference = await context.EditionVisuals.AsNoTracking()
                                 .Where(v => v.Id == id)
                                 .Select(v => v.MediaReference)
                                 .SingleOrDefaultAsync(cancellationToken)
                             ?? throw new EntityNotFoundException(typeof(EditionVisual), id);
        var file = fileOf(mediaReference);
        response.Headers.CacheControl = ImmutableCacheControl;
        return TypedResults.PhysicalFile(file.Path, file.ContentType);
    }
}
