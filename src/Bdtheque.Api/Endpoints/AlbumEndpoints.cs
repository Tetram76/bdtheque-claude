using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>Administration of the albums (<c>/admin/albums</c>).</summary>
internal static class AlbumEndpoints
{
    // fonctionnel.md § Suppression des entités: an album takes along its editions (with their
    // visuals and purchase intents), its contributions and its own purchase intent. Its genres and
    // universes are associations that merely disappear, without any record being concerned.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Composition, EntityKind.Edition,
            (context, id) => context.Editions.Where(e => e.AlbumId == id).Select(e => e.Id)),
        new(LinkNature.Composition, EntityKind.EditionVisual,
            (context, id) => context.EditionVisuals.Where(v => v.Edition.AlbumId == id).Select(v => v.Id)),
        new(LinkNature.Composition, EntityKind.Contribution,
            (context, id) => context.Contributions.Where(c => c.AlbumId == id).Select(c => c.Id)),
        // Every intent carries the identifier of its album, that of an intent on an edition included.
        new(LinkNature.Composition, EntityKind.PurchaseIntent,
            (context, id) => context.PurchaseIntents.Where(p => p.AlbumId == id).Select(p => p.Id)),
    ];

    public static void MapAlbums(this RouteGroupBuilder admin)
    {
        var albums = admin.MapGroup("/albums");
        albums.MapGet("/{id:guid}", GetAsync);
        albums.MapPost("/", CreateAsync);
        albums.MapPut("/{id:guid}", UpdateAsync);
        albums.MapDeletion<Album>(DeletionLinks);
    }

    private static async Task<AlbumForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // Tracked, for the version of the aggregate (BdthequeDbContext.VersionOf): the context lives
        // for this request only.
        var album = await WithAssociations(context.Albums).SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Album), id);
        return ToForm(context, album);
    }

    private static async Task<Created<AlbumForm>> CreateAsync(
        AlbumContent request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var series = await LoadSeriesAsync(request.SeriesId, context, cancellationToken);
        var contributions = await FormReferences.LoadContributionsAsync(context, request.Contributions, cancellationToken);
        var album = new Album(request.Title, series, contributions);
        await ApplyAsync(album, request, context, cancellationToken);
        context.Albums.Add(album);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/albums/{album.Id}", ToForm(context, album));
    }

    private static async Task<AlbumForm> UpdateAsync(
        Guid id, UpdateAlbumRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(id, request.Version, WithAssociations, cancellationToken);
        var content = request.Content;
        var series = await LoadSeriesAsync(content.SeriesId, context, cancellationToken);
        var contributions = await FormReferences.LoadContributionsAsync(context, content.Contributions, cancellationToken);

        var previousContributions = album.Contributions.ToList();
        album.SetTitleSeriesAndContributions(content.Title, series, contributions);
        // Removed from the album, a contribution would otherwise only lose its owner: it is part of
        // the album, and deleted with its removal.
        context.Contributions.RemoveRange(previousContributions.Where(c => !album.Contributions.Contains(c)));

        await ApplyAsync(album, content, context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, album);
    }

    // The editions, intents and contributions are always loaded with the album (AlbumConfiguration);
    // split, so that the collections are not joined into a single cartesian result.
    private static IQueryable<Album> WithAssociations(IQueryable<Album> query) =>
        query.Include(a => a.Genres).Include(a => a.Universes).AsSplitQuery();

    // With its template contributions and their authors: attaching an album with no contribution to
    // the series credits them on the album.
    private static async Task<Series?> LoadSeriesAsync(Guid? seriesId, BdthequeDbContext context, CancellationToken cancellationToken) =>
        seriesId is { } id
            ? await context.Series.Include(s => s.TemplateContributions).ThenInclude(c => c.Author)
                  .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
              ?? throw new EntityNotFoundException(typeof(Series), id)
            : null;

    /// <summary>
    /// Applies the rest of the form to the album, its title, series and contributions being set
    /// together beforehand: the genres and universes it references must still exist (a record
    /// deleted since the form was read is a functional error).
    /// </summary>
    private static async Task ApplyAsync(Album album, AlbumContent content, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        if (content.ManualSortKey is { } manualSortKey)
            album.SetSortKey(manualSortKey);
        else
            album.ResetSortKey();
        album.SetTypeAndVolumeRange(
            EnumMapping.Map<DomainEnums.AlbumType>(content.Type)!.Value, content.StartVolumeNumber, content.EndVolumeNumber);
        album.SetSpecialIssue(content.IsSpecialIssue);
        album.SetVolumeNumber(content.VolumeNumber);
        album.SetFirstPublicationDate(content.FirstPublicationYear, content.FirstPublicationMonth);
        album.SetSummary(content.Summary);
        album.SetPersonalNotes(content.PersonalNotes);
        album.SetRating(EnumMapping.Map<DomainEnums.AlbumRating>(content.Rating));

        var genres = await FormReferences.LoadAsync(context.Genres, content.GenreIds, cancellationToken);
        FormReferences.ReplaceAssociations(album.Genres, genres, album.AddGenre, album.RemoveGenre);
        var universes = await FormReferences.LoadAsync(context.Universes, content.UniverseIds, cancellationToken);
        FormReferences.ReplaceAssociations(album.Universes, universes, album.AddUniverse, album.RemoveUniverse);
    }

    private static AlbumForm ToForm(BdthequeDbContext context, Album album) =>
        new(album.Id, album.SortKey,
            new AlbumContent(
                album.Title,
                album.IsManualSortKey ? album.SortKey : null,
                album.SeriesId,
                EnumMapping.Map<ContractEnums.AlbumType>(album.Type)!.Value,
                album.IsSpecialIssue,
                album.VolumeNumber,
                album.StartVolumeNumber,
                album.EndVolumeNumber,
                album.FirstPublicationYear,
                album.FirstPublicationMonth,
                album.Summary,
                album.PersonalNotes,
                EnumMapping.Map<ContractEnums.AlbumRating>(album.Rating),
                album.Genres.Select(g => g.Id).Order().ToList(),
                album.Universes.Select(u => u.Id).Order().ToList(),
                FormReferences.ToContents(album.Contributions)),
            context.VersionOf(album));
}
