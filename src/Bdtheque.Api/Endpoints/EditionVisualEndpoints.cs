using Bdtheque.Api.Deletion;
using Bdtheque.Api.Visuals;
using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
        visuals.MapPost("/", UploadAsync)
            // An internal API called by `frontend` only, with the shared key: no browser cookie to forge.
            .DisableAntiforgery()
            // The size of the file is checked by the API itself, as a business rule: no server or form
            // limit may refuse it first as a technical error.
            .WithMetadata(new DisableRequestSizeLimitAttribute())
            .WithFormOptions(multipartBodyLengthLimit: long.MaxValue);
        visuals.MapPut("/", ArrangeAsync);

        visuals.MapGet("/{id:guid}/deletion-impact", (Guid rootId, Guid editionId, Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
            AggregateDeletion.GetChildImpactAsync(context, rootId, id, WithVisuals, VisualsOf(editionId), DeletionLinks, cancellationToken));
        visuals.MapDelete("/{id:guid}", async (
            Guid rootId, Guid editionId, Guid id, uint version, string fingerprint, BdthequeDbContext context, VisualStorage storage,
            CancellationToken cancellationToken) =>
        {
            await AggregateDeletion.DeleteChildAsync(
                context, storage, rootId, id, version, fingerprint, WithVisuals, VisualsOf(editionId), DeletionLinks, cancellationToken);
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
    /// Uploads a visual, placed after the visuals of its type. The file is validated and converted in
    /// memory before anything is written; its files are written just before the row, and deleted
    /// right away if the row cannot be saved (choix-implementation.md § Visuels : stockage et traitement).
    /// </summary>
    private static async Task<Created<EditionVisualsForm>> UploadAsync(
        Guid rootId, Guid editionId,
        [FromForm(Name = UploadVisualFields.File)] IFormFile file,
        [FromForm(Name = UploadVisualFields.Type)] ContractEnums.VisualType type,
        [FromForm(Name = UploadVisualFields.AlbumVersion)] uint albumVersion,
        BdthequeDbContext context, VisualStorage storage, IOptions<VisualStorageOptions> options, CancellationToken cancellationToken)
    {
        VisualImage.EnsureSize(file.Length, options.Value.MaxFileSizeBytes);
        var content = new byte[file.Length];
        await using (var stream = file.OpenReadStream())
            await stream.ReadExactlyAsync(content, cancellationToken);
        var prepared = VisualImage.Prepare(content);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, albumVersion, WithVisuals, cancellationToken);
        var edition = EditionOf(album, editionId);
        var mediaReference = VisualStorage.NewMediaReference(prepared);
        var visual = edition.AppendVisual(EnumMapping.Map<DomainEnums.VisualType>(type)!.Value, mediaReference);
        context.EditionVisuals.Add(visual);

        await storage.WriteAsync(mediaReference, prepared, cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            storage.Delete(mediaReference);
            throw;
        }

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
