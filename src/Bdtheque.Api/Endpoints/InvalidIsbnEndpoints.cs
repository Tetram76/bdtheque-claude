using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The editions whose ISBN has an inconsistent check digit (fonctionnel/regles-metier/validation-de-l-isbn.md): their list,
/// which the block « À vérifier » of the home page of the administration leads to, and the correction and acceptance of the
/// error of one of them, each a write on the aggregate of its album (choix-implementation/persistance/concurrence-d-acces.md).
/// </summary>
internal static class InvalidIsbnEndpoints
{
    public static void MapInvalidIsbns(this RouteGroupBuilder admin)
    {
        admin.MapGet("/editions/invalid-isbns", ListAsync);

        // `rootId`, as for every write on an edition: the album, root of its aggregate.
        var edition = admin.MapGroup("/albums/{rootId:guid}/editions/{id:guid}");
        edition.MapPost("/isbn-correction", (Guid rootId, Guid id, IsbnActionRequest request, BdthequeDbContext context, CancellationToken cancellationToken) =>
            WriteAsync(rootId, id, request.AlbumVersion, e => e.CorrectIsbn(), context, cancellationToken));
        edition.MapPost("/isbn-acceptance", (Guid rootId, Guid id, IsbnActionRequest request, BdthequeDbContext context, CancellationToken cancellationToken) =>
            WriteAsync(rootId, id, request.AlbumVersion, e => e.AcceptIsbnError(), context, cancellationToken));
        edition.MapDelete("/isbn-acceptance", (Guid rootId, Guid id, uint version, BdthequeDbContext context, CancellationToken cancellationToken) =>
            WriteAsync(rootId, id, version, e => e.RevokeIsbnErrorAcceptance(), context, cancellationToken));
    }

    /// <summary>
    /// The number of errors to correct, which the home page of the administration presents: an error accepted is not counted.
    /// </summary>
    internal static async Task<int> CountToCorrectAsync(BdthequeDbContext context, CancellationToken cancellationToken) =>
        (await ReadIsbnsAsync(context.Editions, cancellationToken)).Count(e => !e.IsAccepted);

    /// <summary>
    /// The list of the ISBN to check (visuel/listes/liste-par-liste.md): the search and cross filters of the list of the editions,
    /// in its order, over every edition whose ISBN is inconsistent, owned or targeted by an intent; the errors to correct by
    /// default, the errors accepted, or both.
    /// </summary>
    private static async Task<InvalidIsbnList> ListAsync(
        BdthequeDbContext context, [AsParameters] EditionFilters filters, CancellationToken cancellationToken,
        InvalidIsbnState state = InvalidIsbnState.ToCorrect, int page = 1, int pageSize = Paging.DefaultSize)
    {
        var errors = (await ReadIsbnsAsync(filters.Apply(context.Editions), cancellationToken))
            .Where(e => state switch
            {
                InvalidIsbnState.ToCorrect => !e.IsAccepted,
                InvalidIsbnState.Accepted => e.IsAccepted,
                _ => true,
            })
            .ToList();
        var ids = errors.Select(e => e.Id).ToList();

        var rows = await EditionEndpoints.InListOrder(context.Editions.AsNoTracking().Where(e => ids.Contains(e.Id)))
            .ToPageAsync(page, pageSize, CatalogExpressions.Expand((Edition e) => new
            {
                Album = e.Album.ToSummary(),
                Edition = e.ToSummary(),
                Cover = CatalogExpressions.ToCover(e.CoverVisualOf()),
                IsOwned = e.IsOwned(),
                e.AcquisitionMode,
                e.AcquisitionDate,
                e.AcceptedIsbn,
                e.ModifiedAt,
                AlbumVersion = EF.Property<uint>(e.Album, BdthequeDbContext.VersionProperty),
            }), cancellationToken);

        var items = rows.Items
            .Select(r => new InvalidIsbnListItem(
                r.Album, r.Edition, r.Cover, IsbnChecksumValidator.Corrected(r.Edition.Isbn), r.IsOwned,
                EnumMapping.Map<ContractEnums.AcquisitionMode>(r.AcquisitionMode), r.AcquisitionDate?.Year,
                HoldsFor(r.AcceptedIsbn, r.Edition.Isbn), r.ModifiedAt, r.AlbumVersion))
            .ToList();
        return new InvalidIsbnList(
            new Page<InvalidIsbnListItem>(items, rows.Number, rows.Size, rows.TotalCount),
            await filters.NameAsync(context, cancellationToken),
            errors.Count(e => e.IsAccepted));
    }

    private sealed record IsbnError(Guid Id, bool IsAccepted);

    // The check digit is the domain's to verify (IsbnChecksumValidator), which no SQL translates: the ISBN are read, a few
    // thousand short texts at most, and checked in memory; the page is then read by the identifiers of the errors.
    private static async Task<List<IsbnError>> ReadIsbnsAsync(IQueryable<Edition> editions, CancellationToken cancellationToken) =>
        [.. (await editions.AsNoTracking()
            .Where(e => e.Isbn != null)
            .Select(e => new { e.Id, e.Isbn, e.AcceptedIsbn })
            .ToListAsync(cancellationToken))
        .Where(e => !IsbnChecksumValidator.IsValid(e.Isbn))
        .Select(e => new IsbnError(e.Id, HoldsFor(e.AcceptedIsbn, e.Isbn)))];

    // Whether an acceptance holds: it does for the ISBN accepted (modele-metier/edition.md), which the domain clears when the ISBN changes.
    private static bool HoldsFor(string? acceptedIsbn, string? isbn) => acceptedIsbn is not null && acceptedIsbn == isbn;

    private static async Task<NoContent> WriteAsync(
        Guid rootId, Guid id, uint albumVersion, Action<Edition> write, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(rootId, albumVersion, cancellationToken: cancellationToken);
        var edition = album.Editions.SingleOrDefault(e => e.Id == id) ?? throw new EntityNotFoundException(typeof(Edition), id);

        write(edition);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}
