using Bdtheque.Api.Deletion;
using Bdtheque.Api.Visuals;
using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Administration of the visuals of an edition (<c>/admin/albums/{albumId}/editions/{editionId}/visuals</c>):
/// upload, arrangement and deletion. The edition belongs to the aggregate of its album, whose version
/// guards every write on its visuals (choix-implementation.md § Concurrence d'accès).
/// </summary>
internal static class EditionVisualEndpoints
{
    // fonctionnel.md § Suppression des entités: a visual is deleted without any other record
    // concerned; its files follow it (AggregateDeletion).
    private static readonly DeletionLink[] DeletionLinks = [];

    public static void MapEditionVisuals(this RouteGroupBuilder admin)
    {
        var visuals = admin.MapGroup("/albums/{rootId:guid}/editions/{editionId:guid}/visuals");
        visuals.MapGet("/", GetAsync);
        // The form is read as a stream by the handler (VisualUploadReader), never bound by the framework.
        visuals.MapPost("/", UploadAsync).Accepts<IFormFile>("multipart/form-data");
        visuals.MapPut("/", ArrangeAsync);

        visuals.MapGet("/{id:guid}/deletion-impact", (Guid rootId, Guid editionId, Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
            AggregateDeletion.GetChildImpactAsync(context, rootId, id, WithVisuals, VisualsOf(editionId), DeletionLinks, cancellationToken));
        visuals.MapDelete("/{id:guid}", async (
            Guid rootId, Guid editionId, Guid id, uint version, string fingerprint, BdthequeDbContext context, VisualStorage storage,
            CancellationToken cancellationToken) =>
        {
            await AggregateDeletion.DeleteChildAsync(
                context, storage, rootId, id, version, fingerprint, WithVisuals, VisualsOf(editionId), DeletionLinks, delete: null,
                cancellationToken);
            return TypedResults.NoContent();
        });
    }

    private static async Task<EditionVisualsForm> GetAsync(Guid rootId, Guid editionId, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var read = await context.Editions.AsNoTracking().Include(e => e.Visuals)
                       .Where(e => e.Id == editionId && e.AlbumId == rootId)
                       .Select(e => new { Edition = e, AlbumVersion = EF.Property<uint>(e.Album, BdthequeDbContext.VersionProperty) })
                       .SingleOrDefaultAsync(cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Edition), editionId);
        return ToForm(read.Edition, read.AlbumVersion);
    }

    /// <summary>
    /// Uploads a visual (form fields: <see cref="UploadVisualFields"/>), placed after the visuals of its
    /// type. The file is read, validated and converted in memory before anything is written, one upload
    /// at a time (<see cref="VisualUploadGate"/>); its files are written just before the row, and deleted
    /// right away if the row cannot be saved (choix-implementation.md § Visuels : stockage et traitement).
    /// </summary>
    private static async Task<Created<EditionVisualsForm>> UploadAsync(
        Guid rootId, Guid editionId, HttpRequest request, BdthequeDbContext context, VisualStorage storage, VisualUploadGate gate,
        IOptions<VisualStorageOptions> options, CancellationToken cancellationToken)
    {
        using var turn = await gate.EnterAsync(cancellationToken);
        var upload = await VisualUploadReader.ReadAsync(request, options.Value.MaxFileSizeBytes, cancellationToken);
        var prepared = VisualImage.Prepare(upload.File);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, upload.AlbumVersion, WithVisuals, cancellationToken);
        var edition = EditionOf(album, editionId);
        var mediaReference = VisualStorage.NewMediaReference(prepared);
        var visual = edition.AppendVisual(EnumMapping.Map<DomainEnums.VisualType>(upload.Type)!.Value, mediaReference);
        context.EditionVisuals.Add(visual);

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

        // Not compensated: a commit that fails may have committed all the same (e.g. connection lost
        // while committing), and deleting the files would strip a saved visual of them. Left to the
        // reconciliation, which decides from the database.
        await transaction.CommitAsync(cancellationToken);

        return TypedResults.Created($"/admin/albums/{rootId}/editions/{editionId}/visuals", ToForm(edition, context.VersionOf(album)));
    }

    private static async Task<EditionVisualsForm> ArrangeAsync(
        Guid rootId, Guid editionId, ArrangeVisualsRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, request.AlbumVersion, WithVisuals, cancellationToken);
        var edition = EditionOf(album, editionId);

        edition.ArrangeVisuals(request.Visuals.Select(v => (v.Id, EnumMapping.Map<DomainEnums.VisualType>(v.Type)!.Value)).ToList());

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(edition, context.VersionOf(album));
    }

    // The editions are always loaded with their album (AlbumConfiguration), their visuals are not.
    private static IQueryable<Album> WithVisuals(IQueryable<Album> query) => query.Include(a => a.Editions).ThenInclude(e => e.Visuals);

    private static Func<Album, IEnumerable<EditionVisual>> VisualsOf(Guid editionId) =>
        album => album.Editions.Where(e => e.Id == editionId).SelectMany(e => e.Visuals);

    private static Edition EditionOf(Album album, Guid editionId) =>
        album.Editions.SingleOrDefault(e => e.Id == editionId) ?? throw new EntityNotFoundException(typeof(Edition), editionId);

    private static EditionVisualsForm ToForm(Edition edition, uint albumVersion) =>
        new(edition.Id,
            edition.GetOrderedVisuals()
                .Select(v => new EditionVisualForm(
                    v.Id,
                    EnumMapping.Map<ContractEnums.VisualType>(v.Type)!.Value,
                    v.DisplayOrder,
                    VisualStorage.OriginalPath(v.MediaReference),
                    VisualStorage.DisplayPath(v.MediaReference)))
                .ToList(),
            albumVersion);
}
