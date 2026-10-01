using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Pins the optimistic concurrency of aggregates (choix-implementation.md § Concurrence d'accès):
/// the version of an aggregate is its root's <c>xmin</c>, and any write on the aggregate locks
/// the root first, then marks it modified so that its version changes even when only a child
/// row is written.
/// </summary>
public sealed class AggregateConcurrencyTests : IAsyncLifetime
{
    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public void Version_IsMappedOnEveryAggregateRootAndOnlyOnThem()
    {
        var versioned = new List<string>();
        foreach (var entityType in _fixture.Context.Model.GetEntityTypes())
        {
            var version = entityType.FindProperty(BdthequeDbContext.VersionProperty);
            var isRoot = typeof(IAggregateRoot).IsAssignableFrom(entityType.ClrType);

            Assert.True(isRoot == (version is not null), $"{entityType.ShortName()}: aggregate root = {isRoot}, version = {version is not null}");
            if (version is null)
                continue;

            Assert.Equal(typeof(uint), version.ClrType);
            Assert.True(version.IsConcurrencyToken);
            Assert.Equal("xmin", version.GetColumnName());
            versioned.Add(entityType.ClrType.Name);
        }

        Assert.Equal(
            [nameof(Album), nameof(Author), nameof(Genre), nameof(Publisher), nameof(Series), nameof(Universe)],
            versioned.Order());
    }

    [Fact]
    public async Task LoadForWrite_WithCurrentVersion_ChangesVersionEvenWhenOnlyAChildRowIsWritten()
    {
        var (albumId, version) = await SeedAlbumAsync();
        var genre = new Genre("Aventure");
        _fixture.Context.Genres.Add(genre);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        await using (var transaction = await _fixture.Context.Database.BeginTransactionAsync())
        {
            var album = await _fixture.Context.LoadAggregateForWriteAsync<Album>(albumId, version, q => q.Include(a => a.Genres));
            album.AddGenre(await _fixture.Context.Genres.SingleAsync(g => g.Id == genre.Id));
            await _fixture.Context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        Assert.NotEqual(version, await ReadVersionAsync(albumId));
    }

    [Fact]
    public async Task LoadForWrite_WithStaleVersion_IsRejectedAsConcurrentModification()
    {
        var (albumId, staleVersion) = await SeedAlbumAsync();
        await using (var otherWrite = await _fixture.Context.Database.BeginTransactionAsync())
        {
            var album = await _fixture.Context.LoadAggregateForWriteAsync<Album>(albumId, staleVersion);
            album.SetSummary("Modifié depuis un autre onglet");
            await _fixture.Context.SaveChangesAsync();
            await otherWrite.CommitAsync();
        }
        _fixture.Context.ChangeTracker.Clear();

        await using var transaction = await _fixture.Context.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => _fixture.Context.LoadAggregateForWriteAsync<Album>(albumId, staleVersion));
    }

    [Fact]
    public async Task LoadForWrite_LocksTheRootRowUntilTheTransactionEnds()
    {
        var (albumId, version) = await SeedAlbumAsync();

        await using var transaction = await _fixture.Context.Database.BeginTransactionAsync();
        await _fixture.Context.LoadAggregateForWriteAsync<Album>(albumId, version);

        // Any other writer must wait for the root: NOWAIT turns that wait into an immediate error.
        await using var otherConnection = new NpgsqlConnection(_fixture.Context.Database.GetConnectionString());
        await otherConnection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT 1 FROM \"Albums\" WHERE \"Id\" = @id FOR UPDATE NOWAIT", otherConnection);
        command.Parameters.AddWithValue("id", albumId);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteScalarAsync());

        Assert.Equal(PostgresErrorCodes.LockNotAvailable, exception.SqlState);
    }

    [Fact]
    public async Task LoadForWrite_WithUnknownId_ReportsAMissingEntity()
    {
        await using var transaction = await _fixture.Context.Database.BeginTransactionAsync();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _fixture.Context.LoadAggregateForWriteAsync<Album>(Guid.CreateVersion7(), 1));
    }

    [Fact]
    public async Task LoadForWrite_AfterEntitiesWereAlreadyTracked_IsAProgrammingError()
    {
        // A tracking query never refreshes an instance already tracked: an aggregate read before its
        // lock could carry values (and a version) older than the locked row, and its rewrite would
        // silently overwrite a concurrent modification.
        var (albumId, version) = await SeedAlbumAsync();
        await _fixture.Context.Albums.SingleAsync(a => a.Id == albumId);

        await using var transaction = await _fixture.Context.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _fixture.Context.LoadAggregateForWriteAsync<Album>(albumId, version));
    }

    [Fact]
    public async Task LoadForWrite_OutsideATransaction_IsAProgrammingError()
    {
        // Without a transaction the row lock would be released as soon as the SELECT returns.
        var (albumId, version) = await SeedAlbumAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _fixture.Context.LoadAggregateForWriteAsync<Album>(albumId, version));
    }

    private async Task<(Guid Id, uint Version)> SeedAlbumAsync()
    {
        var album = new Album("Le Lotus bleu", null);
        _fixture.Context.Albums.Add(album);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();
        return (album.Id, await ReadVersionAsync(album.Id));
    }

    private Task<uint> ReadVersionAsync(Guid albumId) =>
        _fixture.Context.Albums.AsNoTracking()
            .Where(a => a.Id == albumId)
            .Select(a => EF.Property<uint>(a, BdthequeDbContext.VersionProperty))
            .SingleAsync();
}
