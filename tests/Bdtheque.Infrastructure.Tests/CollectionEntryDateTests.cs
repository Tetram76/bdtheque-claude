using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The date an edition entered the collection (modele-metier.md § Édition) is set on save when the
/// edition becomes owned, never entered, and never rewritten afterwards.
/// </summary>
public sealed class CollectionEntryDateTests : IAsyncLifetime
{
    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    private static readonly DateTimeOffset Created = new(2026, 3, 14, 9, 26, 53, TimeSpan.Zero);

    [Fact]
    public async Task OwnedEditionCreated_EntersTheCollectionAtTheSave()
    {
        var album = new Album("Gaston", null);
        var publisher = new Publisher("Dupuis");
        var edition = new Edition(album, publisher);
        album.RecordAcquisition(edition, AcquisitionMode.Purchase);
        _fixture.Context.AddRange(album, publisher, edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Editions.SingleAsync();
        Assert.Equal(saved.CreatedAt, saved.EnteredCollectionAt);
    }

    [Fact]
    public async Task EditionTargetedByAnIntent_HasNotEnteredTheCollection()
    {
        var (_, edition, _) = await CreateIntendedEditionAsync();

        Assert.Null((await _fixture.Context.Editions.SingleAsync(e => e.Id == edition.Id)).EnteredCollectionAt);
    }

    [Fact]
    public async Task PurchaseConfirmed_EntersTheCollectionAtTheConfirmation()
    {
        // Created long before it is bought, an edition targeted by an intent enters the collection
        // when its purchase is confirmed, not when it was created.
        _fixture.Clock.UtcNow = Created;
        var (album, edition, version) = await CreateIntendedEditionAsync();

        var bought = Created.AddDays(30);
        _fixture.Clock.UtcNow = bought;
        await using (var transaction = await _fixture.Context.Database.BeginTransactionAsync())
        {
            var root = await _fixture.Context.LoadAggregateForWriteAsync<Album>(album.Id, version);
            root.RecordAcquisition(root.Editions.Single(e => e.Id == edition.Id), AcquisitionMode.Purchase);
            await _fixture.Context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Editions.SingleAsync(e => e.Id == edition.Id);
        Assert.Equal(Created, saved.CreatedAt);
        Assert.Equal(bought, saved.EnteredCollectionAt);
    }

    [Fact]
    public async Task AcquisitionOfAnOwnedEditionModified_KeepsItsEntryDate()
    {
        var album = new Album("Spirou", null);
        var publisher = new Publisher("Dupuis");
        var edition = new Edition(album, publisher);
        album.RecordAcquisition(edition, AcquisitionMode.Gift);
        _fixture.Clock.UtcNow = Created;
        _fixture.Context.AddRange(album, publisher, edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var modified = Created.AddDays(30);
        _fixture.Clock.UtcNow = modified;

        var reloaded = await _fixture.Context.Editions.Include(e => e.Album).SingleAsync();
        reloaded.SetPublicationYearAndAcquisition(
            1990, new EditionAcquisition(AcquisitionMode.Purchase, new DateOnly(1990, 5, 2), 8m, "FRF", false, null, null));
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Editions.SingleAsync();
        Assert.Equal(Created, saved.EnteredCollectionAt);
        Assert.Equal(modified, saved.ModifiedAt);
    }

    [Fact]
    public async Task Save_NeverRewritesTheEntryDate()
    {
        var album = new Album("Blake et Mortimer", null);
        var publisher = new Publisher("Éditions Blake et Mortimer");
        var edition = new Edition(album, publisher);
        album.RecordAcquisition(edition, AcquisitionMode.Purchase);
        _fixture.Context.AddRange(album, publisher, edition);
        await _fixture.Context.SaveChangesAsync();
        var entered = edition.EnteredCollectionAt;

        _fixture.Context.Entry(edition).Property(e => e.EnteredCollectionAt).CurrentValue = entered!.Value.AddYears(-10);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        Assert.Equal(entered, (await _fixture.Context.Editions.SingleAsync()).EnteredCollectionAt);
    }

    private async Task<(Album Album, Edition Edition, uint AlbumVersion)> CreateIntendedEditionAsync()
    {
        var album = new Album("Le Lotus bleu", null);
        var publisher = new Publisher("Casterman");
        var edition = new Edition(album, publisher);
        album.AddPurchaseIntent(edition);
        _fixture.Context.AddRange(album, publisher);
        await _fixture.Context.SaveChangesAsync();
        var version = _fixture.Context.VersionOf(album);
        _fixture.Context.ChangeTracker.Clear();
        return (album, edition, version);
    }
}
