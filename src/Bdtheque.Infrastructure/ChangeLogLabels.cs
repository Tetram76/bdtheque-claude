using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Builds the labels the change log records (modele-metier.md § Entrée du journal des
/// modifications), from the records as the save under way leaves them.
/// </summary>
/// <remarks>
/// A record involved in the save is read from the change tracker, whatever its state; any other is
/// read from the database without being tracked, so that recording the change log never adds to
/// what the save writes.
/// </remarks>
internal sealed class ChangeLogLabels(BdthequeDbContext context)
{
    private readonly Dictionary<(Type, Guid), EntityBase> _read = [];

    public async ValueTask<RecordLabel> OfAsync<TRecord>(Guid id, CancellationToken cancellationToken) where TRecord : EntityBase =>
        await OfAsync(await FindAsync<TRecord>(id, cancellationToken), cancellationToken);

    public async ValueTask<RecordLabel> OfAsync(EntityBase record, CancellationToken cancellationToken) =>
        record switch
        {
            Album album => await AlbumAsync(album, cancellationToken),
            Edition edition => await EditionAsync(edition, cancellationToken),
            PurchaseIntent intent => intent.EditionId is { } editionId
                ? new PurchaseIntentLabel(null, await EditionAsync(await FindAsync<Edition>(editionId, cancellationToken), cancellationToken))
                : new PurchaseIntentLabel(await AlbumAsync(await FindAsync<Album>(intent.AlbumId, cancellationToken), cancellationToken), null),
            Series series => new NameLabel(series.Title),
            Author author => AuthorOf(author),
            Publisher publisher => new NameLabel(publisher.Name),
            PublisherCollection collection => new PublisherCollectionLabel(
                collection.Name, (await FindAsync<Publisher>(collection.PublisherId, cancellationToken)).Name),
            Genre genre => new NameLabel(genre.Label),
            Universe universe => new NameLabel(universe.Name),
            SavedSearch search => new NameLabel(search.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(record), record.GetType().Name, "Not a record of the change log."),
        };

    public async ValueTask<AuthorLabel> AuthorAsync(Guid id, CancellationToken cancellationToken) =>
        AuthorOf(await FindAsync<Author>(id, cancellationToken));

    private static AuthorLabel AuthorOf(Author author) => new(author.LastName, author.FirstName, author.Pseudonym);

    private async ValueTask<AlbumLabel> AlbumAsync(Album album, CancellationToken cancellationToken) =>
        new(
            album.Title,
            album.SeriesId is { } seriesId ? (await FindAsync<Series>(seriesId, cancellationToken)).Title : null,
            album.Type,
            album.IsSpecialIssue,
            album.VolumeNumber,
            album.StartVolumeNumber,
            album.EndVolumeNumber);

    private async ValueTask<EditionLabel> EditionAsync(Edition edition, CancellationToken cancellationToken) =>
        new(
            await AlbumAsync(await FindAsync<Album>(edition.AlbumId, cancellationToken), cancellationToken),
            (await FindAsync<Publisher>(edition.PublisherId, cancellationToken)).Name,
            edition.PublisherCollectionId is { } collectionId
                ? (await FindAsync<PublisherCollection>(collectionId, cancellationToken)).Name
                : null,
            edition.PublicationYear,
            edition.Isbn);

    private async ValueTask<TRecord> FindAsync<TRecord>(Guid id, CancellationToken cancellationToken) where TRecord : EntityBase
    {
        if (_read.TryGetValue((typeof(TRecord), id), out var read))
            return (TRecord)read;

        // Only its own columns are needed: an album would otherwise come with its whole aggregate.
        var record = context.ChangeTracker.Entries<TRecord>().FirstOrDefault(e => e.Entity.Id == id)?.Entity
                     ?? await context.Set<TRecord>().AsNoTracking().IgnoreAutoIncludes().SingleAsync(r => r.Id == id, cancellationToken);
        _read[(typeof(TRecord), id)] = record;
        return record;
    }
}
