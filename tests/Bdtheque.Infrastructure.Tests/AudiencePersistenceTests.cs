using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Persistence of the audience measurement (choix-implementation/mesure-audience.md § Mesure d'audience : mise en
/// œuvre): visits and usages, which are not records, and the monthly counters, unique per function, month and
/// compartment.
/// </summary>
public sealed class AudiencePersistenceTests : IAsyncLifetime
{
    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task AVisit_IsSavedWithItsUsages_BeginningWithItsFirstOne_WithoutAnyChangeLogEntry()
    {
        var album = new PresentedRecord(RecordType.Album, Guid.CreateVersion7());
        var first = AudienceVisit.Open(false, DeviceType.Smartphone, RenderingEngine.WebKit, "605.1", new FunctionUse("catalog.album", album, null));
        _fixture.Context.AudienceUsages.Add(first);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.AudienceUsages.Add(first.Follow(new FunctionUse("catalog.album", album, null)));
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var visit = await _fixture.Context.AudienceVisits.SingleAsync();
        var usages = await _fixture.Context.AudienceUsages.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id).ToListAsync();
        Assert.Equal(visit.CreatedAt, usages[0].CreatedAt);
        Assert.Equal([false, true], usages.Select(u => u.IsRepeat));
        Assert.All(usages, u => Assert.Equal(album, u.PresentedRecord));
        Assert.Empty(await _fixture.Context.ChangeLog.ToListAsync());
    }

    [Fact]
    public async Task DeletingAVisit_RemovesItsUsages()
    {
        var first = AudienceVisit.Open(false, DeviceType.Computer, RenderingEngine.Blink, null, new FunctionUse("dashboard", null, null));
        _fixture.Context.AudienceUsages.Add(first);
        await _fixture.Context.SaveChangesAsync();

        await _fixture.Context.AudienceVisits.ExecuteDeleteAsync();

        Assert.Empty(await _fixture.Context.AudienceUsages.ToListAsync());
    }

    [Fact]
    public async Task ACounter_IsSavedWithItsObjects()
    {
        var album = new PresentedRecord(RecordType.Album, Guid.CreateVersion7());
        _fixture.Context.AudienceCounters.Add(Counter("catalog.album", "130", [new AudienceCounterObject(album, null, 2)]));
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var counter = await _fixture.Context.AudienceCounters.SingleAsync();

        var saved = Assert.Single(counter.Objects);
        Assert.Equal((album, (string?)null, 2), (saved.Record, saved.Parameters, saved.UsageCount));
    }

    [Fact]
    public async Task TwoCounters_OfTheSameFunctionMonthAndCompartment_AreRefused_EvenWithoutEngineVersion()
    {
        _fixture.Context.AudienceCounters.Add(Counter("dashboard", null, []));
        await _fixture.Context.SaveChangesAsync();

        _fixture.Context.AudienceCounters.Add(Counter("dashboard", null, []));
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => _fixture.Context.SaveChangesAsync());

        var violation = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, violation.SqlState);
    }

    private static AudienceCounter Counter(string function, string? engineVersion, AudienceCounterObject[] objects) =>
        new(function, new DateOnly(2026, 3, 1), false, DeviceType.Computer, RenderingEngine.Blink, engineVersion, 2, 1, 1,
            DateTimeOffset.UtcNow, objects);
}
