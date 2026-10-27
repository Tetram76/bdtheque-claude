using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The history of the spotlights of the dashboard (choix-implementation/domaine.md § Édition « À la une » : mise en
/// œuvre): one edition per day, the first drawn for a day staying that of every display of the day, and an edition
/// deleted between its draw and its insertion leaving the day without edition.
/// </summary>
public sealed class EditionSpotlightTests : IAsyncLifetime
{
    private static readonly DateOnly Day = new(2026, 10, 27);

    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task InsertSpotlight_KeepsTheEditionOfTheDayAlreadyDrawn()
    {
        var (first, second) = await CreateOwnedEditionsAsync();

        await _fixture.Context.InsertSpotlightAsync(Day, first.Id);
        await _fixture.Context.InsertSpotlightAsync(Day, second.Id);

        var spotlight = await _fixture.Context.EditionSpotlights.SingleAsync();
        Assert.Equal((Day, first.Id), (spotlight.Day, spotlight.EditionId));
    }

    [Fact]
    public async Task InsertSpotlight_OfAnEditionDeletedSinceItsDraw_InsertsNothing()
    {
        var (deleted, _) = await CreateOwnedEditionsAsync();
        await _fixture.Context.Editions.Where(e => e.Id == deleted.Id).ExecuteDeleteAsync();

        await _fixture.Context.InsertSpotlightAsync(Day, deleted.Id);

        Assert.False(await _fixture.Context.EditionSpotlights.AnyAsync());
    }

    [Fact]
    public async Task ASpotlight_IsNoWriteOnTheEdition()
    {
        var (edition, _) = await CreateOwnedEditionsAsync();
        var before = await _fixture.Context.Editions.AsNoTracking().SingleAsync(e => e.Id == edition.Id);
        var entries = await _fixture.Context.ChangeLog.CountAsync();

        await _fixture.Context.InsertSpotlightAsync(Day, edition.Id);

        var after = await _fixture.Context.Editions.AsNoTracking().SingleAsync(e => e.Id == edition.Id);
        Assert.Equal(before.ModifiedAt, after.ModifiedAt);
        Assert.Equal(entries, await _fixture.Context.ChangeLog.CountAsync());
    }

    private async Task<(Edition First, Edition Second)> CreateOwnedEditionsAsync()
    {
        var album = new Album("Le Lotus bleu", null);
        var publisher = new Publisher("Casterman");
        var first = new Edition(album, publisher);
        album.RecordAcquisition(first, AcquisitionMode.Purchase);
        var second = new Edition(album, publisher);
        album.RecordAcquisition(second, AcquisitionMode.Gift);
        _fixture.Context.AddRange(album, publisher, first, second);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();
        return (first, second);
    }
}
