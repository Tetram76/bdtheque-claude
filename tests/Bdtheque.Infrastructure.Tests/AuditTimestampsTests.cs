using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The creation and last modification dates common to every entity (modele-metier.md § Attributs
/// communs à toutes les entités) are set on save, never entered.
/// </summary>
public sealed class AuditTimestampsTests : IAsyncLifetime
{
    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    private static readonly DateTimeOffset Created = new(2026, 3, 14, 9, 26, 53, TimeSpan.Zero);

    [Fact]
    public async Task Add_SetsBothDatesToTheTimeOfTheApplicationClock()
    {
        _fixture.Clock.UtcNow = Created;
        var publisher = new Publisher("Dargaud");
        var collection = publisher.AddCollection("Lucky Luke");
        _fixture.Context.AddRange(publisher, collection);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        // Children too: every entity carries the dates, not only the aggregate roots.
        foreach (EntityBase saved in new EntityBase[]
                 {
                     await _fixture.Context.Publishers.SingleAsync(),
                     await _fixture.Context.PublisherCollections.SingleAsync(),
                 })
        {
            Assert.Equal(Created, saved.CreatedAt);
            Assert.Equal(Created, saved.ModifiedAt);
        }
    }

    [Fact]
    public async Task Save_TruncatesTheTimeToTheMicrosecond_SoThatTheTrackedValueIsTheStoredOne()
    {
        // PostgreSQL keeps microseconds: a tenth of a microsecond (one tick) would be lost on the way.
        _fixture.Clock.UtcNow = Created.AddTicks(1_234_567);
        var genre = new Genre("Aventure");
        _fixture.Context.Genres.Add(genre);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var expected = Created.AddTicks(1_234_560);
        Assert.Equal(expected, genre.CreatedAt);
        Assert.Equal(expected, (await _fixture.Context.Genres.SingleAsync()).CreatedAt);
    }

    [Fact]
    public async Task Modify_UpdatesTheModificationDateOnly()
    {
        _fixture.Clock.UtcNow = Created;
        var genre = new Genre("Aventure");
        _fixture.Context.Genres.Add(genre);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var modified = Created.AddDays(2);
        _fixture.Clock.UtcNow = modified;
        var reloaded = await _fixture.Context.Genres.SingleAsync();
        reloaded.SetLabel("Aventures");
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Genres.SingleAsync();
        Assert.Equal(Created, saved.CreatedAt);
        Assert.Equal(modified, saved.ModifiedAt);
    }

    [Fact]
    public async Task Modify_NeverRewritesTheCreationDate()
    {
        var genre = new Genre("Aventure");
        _fixture.Context.Genres.Add(genre);
        await _fixture.Context.SaveChangesAsync();
        var created = genre.CreatedAt;

        _fixture.Context.Entry(genre).Property(g => g.CreatedAt).CurrentValue = created.AddYears(-10);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        Assert.Equal(created, (await _fixture.Context.Genres.SingleAsync()).CreatedAt);
    }

    [Fact]
    public async Task WriteOnAChildThroughItsAggregate_UpdatesTheRootModificationDate()
    {
        // A child written through its aggregate modifies the root's record (choix-implementation/persistance.md
        // § Concurrence d'accès), whose modification date follows.
        _fixture.Clock.UtcNow = Created;
        var publisher = new Publisher("Dupuis");
        _fixture.Context.Publishers.Add(publisher);
        await _fixture.Context.SaveChangesAsync();
        var version = _fixture.Context.VersionOf(publisher);
        _fixture.Context.ChangeTracker.Clear();

        var modified = Created.AddHours(1);
        _fixture.Clock.UtcNow = modified;

        await using (var transaction = await _fixture.Context.Database.BeginTransactionAsync())
        {
            var root = await _fixture.Context.LoadAggregateForWriteAsync<Publisher>(
                publisher.Id, version, q => q.Include(p => p.Collections));
            root.AddCollection("Spirou");
            await _fixture.Context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        _fixture.Context.ChangeTracker.Clear();

        Assert.Equal(modified, (await _fixture.Context.Publishers.SingleAsync()).ModifiedAt);
    }
}
