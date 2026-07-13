using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Verifies that the EF Core model builds and that basic persistence works against
/// an in-memory SQLite database. These tests catch mis-configuration in entity
/// configurations or the DbContext wiring without requiring a running PostgreSQL instance.
/// </summary>
public sealed class ModelCreationTests : IClassFixture<BdthequeDbContextFixture>
{
    private readonly BdthequeDbContextFixture _fixture;

    public ModelCreationTests(BdthequeDbContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EnsureCreated_BuildsModelWithoutError()
    {
        // The fixture already called EnsureCreated; verify the schema exists
        // by checking that all expected tables are present.
        var tableNames = _fixture.Context.Model
            .GetEntityTypes()
            .Select(e => e.GetTableName())
            .Where(t => t is not null)
            .OrderBy(t => t)
            .ToList();

        Assert.Contains("Authors", tableNames);
        Assert.Contains("Publishers", tableNames);
        Assert.Contains("PublisherCollections", tableNames);
        Assert.Contains("Genres", tableNames);
        Assert.Contains("Universes", tableNames);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddAuthor_WithLastNameOnly_Persists()
    {
        var author = new Author("Hugo", null, null);
        _fixture.Context.Authors.Add(author);
        await _fixture.Context.SaveChangesAsync();

        var saved = await _fixture.Context.Authors.FindAsync(author.Id);
        Assert.NotNull(saved);
        Assert.Equal("Hugo", saved.LastName);
    }

    [Fact]
    public async Task AddAuthor_WithPseudonymOnly_Persists()
    {
        var author = new Author(null, null, "Hergé");
        _fixture.Context.Authors.Add(author);
        await _fixture.Context.SaveChangesAsync();

        var saved = await _fixture.Context.Authors.FindAsync(author.Id);
        Assert.NotNull(saved);
        Assert.Equal("Hergé", saved.Pseudonym);
    }

    [Fact]
    public async Task AddPublisherWithCollection_Persists()
    {
        var publisher = new Publisher("Casterman");
        var collection = new PublisherCollection("Tintin", publisher);

        _fixture.Context.Publishers.Add(publisher);
        _fixture.Context.PublisherCollections.Add(collection);
        await _fixture.Context.SaveChangesAsync();

        var savedCollection = await _fixture.Context.PublisherCollections
            .Include(c => c.Publisher)
            .FirstOrDefaultAsync(c => c.Id == collection.Id);

        Assert.NotNull(savedCollection);
        Assert.Equal("Tintin", savedCollection.Name);
        Assert.Equal("Casterman", savedCollection.Publisher.Name);
    }

    [Fact]
    public async Task AddUniverse_WithParent_Persists()
    {
        var root = new Universe("Milky Way");
        var child = new Universe("Solar System");
        child.SetParent(root);

        _fixture.Context.Universes.Add(root);
        _fixture.Context.Universes.Add(child);
        await _fixture.Context.SaveChangesAsync();

        var savedChild = await _fixture.Context.Universes
            .Include(u => u.Parent)
            .FirstOrDefaultAsync(u => u.Id == child.Id);

        Assert.NotNull(savedChild);
        Assert.NotNull(savedChild.Parent);
        Assert.Equal("Milky Way", savedChild.Parent.Name);
    }

    [Fact]
    public async Task AddGenre_Persists()
    {
        var genre = new Genre("Aventure");
        _fixture.Context.Genres.Add(genre);
        await _fixture.Context.SaveChangesAsync();

        var saved = await _fixture.Context.Genres.FindAsync(genre.Id);
        Assert.NotNull(saved);
        Assert.Equal("Aventure", saved.Label);
    }
}
