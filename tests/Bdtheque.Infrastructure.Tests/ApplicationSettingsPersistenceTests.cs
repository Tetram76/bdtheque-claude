using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The setting of the application is a single record, always present, which is modified but neither created nor
/// deleted (modele-metier.md § Paramétrage de l'application; choix-implementation/domaine.md § Paramétrage de
/// l'application : mise en œuvre).
/// </summary>
public sealed class ApplicationSettingsPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 30, 0, TimeSpan.Zero);

    private readonly BdthequeDbContextFixture _fixture = new();

    private BdthequeDbContext Context => _fixture.Context;

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Provision_OnAnEmptyDatabase_InsertsTheDefaultsDatedByTheClockWithoutChangeLogEntry()
    {
        _fixture.Clock.UtcNow = Now;

        await Context.ProvisionApplicationSettingsAsync();

        var settings = await Context.ApplicationSettings.AsNoTracking().SingleAsync();
        Assert.Equal(ApplicationSettings.SingletonId, settings.Id);
        Assert.Equal(AlbumLabelFormat.TitleSeriesVolume, settings.AlbumLabelFormat);
        Assert.Equal(DisplayCulture.FrenchFrance, settings.DisplayCulture);
        Assert.Equal((Now, Now), (settings.CreatedAt, settings.ModifiedAt));
        Assert.Empty(await Context.ChangeLog.ToListAsync());
    }

    [Fact]
    public async Task Provision_AgainAtTheNextStartup_KeepsTheSettingAsModified()
    {
        await Context.ProvisionApplicationSettingsAsync();
        var settings = await Context.ApplicationSettings.SingleAsync();
        settings.Set(AlbumLabelFormat.VolumeTitleSeries, DisplayCulture.FrenchFrance);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        await Context.ProvisionApplicationSettingsAsync();

        var stored = await Context.ApplicationSettings.AsNoTracking().SingleAsync();
        Assert.Equal(AlbumLabelFormat.VolumeTitleSeries, stored.AlbumLabelFormat);
    }

    [Fact]
    public async Task ASecondSetting_IsRefusedByTheDatabase()
    {
        await Context.ProvisionApplicationSettingsAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() => Context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "ApplicationSettings" ("Id", "AlbumLabelFormat", "DisplayCulture", "CreatedAt", "ModifiedAt")
            VALUES ({Guid.CreateVersion7()}, 1, 1, {Now}, {Now})
            """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    [Fact]
    public async Task Modification_IsRecordedInTheChangeLog()
    {
        await Context.ProvisionApplicationSettingsAsync();
        var settings = await Context.ApplicationSettings.SingleAsync();

        settings.Set(AlbumLabelFormat.VolumeTitleSeries, DisplayCulture.FrenchFrance);
        await Context.SaveChangesAsync();

        var entry = await Context.ChangeLog.AsNoTracking().SingleAsync();
        Assert.Equal(RecordType.ApplicationSettings, entry.RecordType);
        Assert.Equal(ApplicationSettings.SingletonId, entry.RecordId);
        Assert.Equal(ChangeLogOperation.Modification, entry.Operation);
        Assert.Equal(new ApplicationSettingsLabel(), entry.Label);
        Assert.Equal(
            [
                new ChangeLogChange(
                    ChangeLogAttribute.AlbumLabelFormat,
                    new EnumValue((int)AlbumLabelFormat.TitleSeriesVolume),
                    new EnumValue((int)AlbumLabelFormat.VolumeTitleSeries)),
            ],
            entry.Changes);
    }
}
