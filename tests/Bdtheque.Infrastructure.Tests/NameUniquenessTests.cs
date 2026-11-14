using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Npgsql;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Pins the uniqueness rules of referential names (modele-metier/): a genre label, a publisher name, the name of a collection
/// within its publisher and that of a universe within its parent are unique regardless of case and accents.
/// </summary>
public sealed class NameUniquenessTests : IAsyncLifetime
{
    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task GenreLabel_DifferingOnlyByCase_IsRejected()
    {
        _fixture.Context.Genres.Add(new Genre("Aventure"));
        await _fixture.Context.SaveChangesAsync();

        _fixture.Context.Genres.Add(new Genre("aventure"));

        await AssertViolatesAsync("IX_Genres_Label");
    }

    [Fact]
    public async Task GenreLabel_DifferingOnlyByAccent_IsRejected()
    {
        _fixture.Context.Genres.Add(new Genre("Épopée"));
        await _fixture.Context.SaveChangesAsync();

        _fixture.Context.Genres.Add(new Genre("Epopee"));

        await AssertViolatesAsync("IX_Genres_Label");
    }

    [Fact]
    public async Task SavedSearchName_DifferingOnlyByCaseAndAccents_IsRejected()
    {
        const string criteria = """{"kind":"group","operator":"And","conditions":[]}""";
        _fixture.Context.SavedSearches.Add(new SavedSearch("Éditions dédicacées", EntityKind.Edition, criteria, [], []));
        await _fixture.Context.SaveChangesAsync();

        _fixture.Context.SavedSearches.Add(new SavedSearch("editions DEDICACEES", EntityKind.Edition, criteria, [], []));

        await AssertViolatesAsync("IX_SavedSearches_Name");
    }

    [Fact]
    public async Task PublisherName_DifferingOnlyByCaseAndAccents_IsRejected()
    {
        _fixture.Context.Publishers.Add(new Publisher("Éditions Dargaud"));
        await _fixture.Context.SaveChangesAsync();

        _fixture.Context.Publishers.Add(new Publisher("EDITIONS DARGAUD"));

        await AssertViolatesAsync("IX_Publishers_Name");
    }

    [Fact]
    public async Task CollectionName_DifferingOnlyByCaseAndAccents_IsRejectedWithinItsPublisher_AtCommit()
    {
        var publisher = new Publisher("Glénat");
        publisher.AddCollection("Vécu");
        _fixture.Context.Publishers.Add(publisher);
        await _fixture.Context.SaveChangesAsync();

        publisher.AddCollection("VECU");

        await AssertViolatesAsync("IX_PublisherCollections_PublisherId_Name");
    }

    [Fact]
    public async Task CollectionName_OfAnotherPublisher_IsAccepted()
    {
        var first = new Publisher("Dupuis");
        first.AddCollection("Aire Libre");
        var second = new Publisher("Le Lombard");
        second.AddCollection("aire libre");
        _fixture.Context.Publishers.AddRange(first, second);

        await _fixture.Context.SaveChangesAsync();
    }

    [Fact]
    public async Task UniverseName_DifferingOnlyByCaseAndAccents_IsRejectedAmongTheUniversesWithoutParent()
    {
        _fixture.Context.Universes.Add(new Universe("Les Mondes d'Aldébaran"));
        await _fixture.Context.SaveChangesAsync();

        _fixture.Context.Universes.Add(new Universe("les mondes d'aldebaran"));

        await AssertViolatesAsync("IX_Universes_ParentId_Name");
    }

    [Fact]
    public async Task UniverseName_IsUniqueWithinItsParent_NotAcrossParents()
    {
        var marvel = new Universe("Marvel");
        var dc = new Universe("DC");
        var marvelOrigins = new Universe("Origines");
        marvelOrigins.SetParent(marvel);
        var dcOrigins = new Universe("Origines");
        dcOrigins.SetParent(dc);
        _fixture.Context.Universes.AddRange(marvel, dc, marvelOrigins, dcOrigins);
        await _fixture.Context.SaveChangesAsync();

        var duplicate = new Universe("ORIGINES");
        duplicate.SetParent(marvel);
        _fixture.Context.Universes.Add(duplicate);

        await AssertViolatesAsync("IX_Universes_ParentId_Name");
    }

    private async Task AssertViolatesAsync(string constraint)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => _fixture.Context.SaveChangesAsync());

        var violation = Assert.IsType<PostgresException>(exception is PostgresException ? exception : exception.InnerException);
        Assert.Equal((PostgresErrorCodes.UniqueViolation, constraint), (violation.SqlState, violation.ConstraintName));
    }
}
