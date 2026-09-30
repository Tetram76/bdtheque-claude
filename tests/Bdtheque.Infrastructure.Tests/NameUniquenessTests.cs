using Bdtheque.Domain.Entities;
using Npgsql;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Pins the uniqueness rules of referential names (modele-metier.md): a genre label is unique
/// regardless of case and accents, a publisher name is unique as typed.
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
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => _fixture.Context.SaveChangesAsync());

        var violation = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("IX_Genres_Label", violation.ConstraintName);
    }

    [Fact]
    public async Task GenreLabel_DifferingOnlyByAccent_IsRejected()
    {
        _fixture.Context.Genres.Add(new Genre("Épopée"));
        await _fixture.Context.SaveChangesAsync();

        _fixture.Context.Genres.Add(new Genre("Epopee"));
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => _fixture.Context.SaveChangesAsync());

        var violation = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("IX_Genres_Label", violation.ConstraintName);
    }

    [Fact]
    public async Task PublisherName_DifferingOnlyByCase_IsAccepted()
    {
        _fixture.Context.Publishers.AddRange(new Publisher("Dargaud"), new Publisher("dargaud"));

        await _fixture.Context.SaveChangesAsync();
    }

    [Fact]
    public async Task PublisherName_Identical_IsRejected()
    {
        _fixture.Context.Publishers.Add(new Publisher("Dargaud"));
        await _fixture.Context.SaveChangesAsync();

        _fixture.Context.Publishers.Add(new Publisher("Dargaud"));
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => _fixture.Context.SaveChangesAsync());

        var violation = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("IX_Publishers_Name", violation.ConstraintName);
    }
}
