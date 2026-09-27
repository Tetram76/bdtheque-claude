using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
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
        Assert.Contains("Series", tableNames);
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

    [Fact]
    public async Task SetParent_WithPartiallyLoadedAncestorChain_ThrowsInvalidOperationException()
    {
        // Persist A ← B ← C (B's parent is A, C's parent is B).
        // Then reload only A and C (without B) using AsNoTracking.
        // Calling A.SetParent(C) must throw InvalidOperationException because C's ancestor
        // chain is incomplete in memory: C.ParentId = B.Id but C.Parent = null.
        // The method cannot verify acyclicity without B, so it refuses the assignment.
        var a = new Universe("GrandParentA");
        var b = new Universe("ParentB");
        var c = new Universe("ChildC");
        b.SetParent(a);
        c.SetParent(b);
        _fixture.Context.Universes.AddRange(a, b, c);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        // Load A and C independently, without B — C has ParentId = B.Id but Parent = null
        var reloadedA = await _fixture.Context.Universes.AsNoTracking()
            .FirstAsync(u => u.Id == a.Id);
        var reloadedC = await _fixture.Context.Universes.AsNoTracking()
            .FirstAsync(u => u.Id == c.Id);

        Assert.Null(reloadedC.Parent);
        Assert.Equal(b.Id, reloadedC.ParentId);

        // Attempting A.SetParent(C) should throw because C's full ancestor chain is not in memory
        Assert.Throws<InvalidOperationException>(() => reloadedA.SetParent(reloadedC));
    }

    [Fact]
    public async Task AddSeries_Persists()
    {
        var series = new Series("Le Lotus bleu");

        _fixture.Context.Series.Add(series);
        await _fixture.Context.SaveChangesAsync();

        var saved = await _fixture.Context.Series.FindAsync(series.Id);
        Assert.NotNull(saved);
        Assert.Equal("Le Lotus bleu", saved.Title);
        Assert.Equal("Lotus bleu [Le]", saved.SortKey);
    }

    [Fact]
    public async Task AddSeries_WithGenresAndUniverses_Persists()
    {
        // Distinct labels from every other test in this shared-fixture class (Genre.Label and
        // Publisher.Name are unique-indexed database-wide, and the fixture's database is shared
        // across all tests in this class).
        var series = new Series("Tintin");
        var genre = new Genre("Policier");
        var universe = new Universe("Franco-Belge");
        series.Genres.Add(genre);
        series.Universes.Add(universe);

        _fixture.Context.Series.Add(series);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Series
            .Include(s => s.Genres)
            .Include(s => s.Universes)
            .FirstAsync(s => s.Id == series.Id);

        Assert.Contains(saved.Genres, g => g.Label == "Policier");
        Assert.Contains(saved.Universes, u => u.Name == "Franco-Belge");
    }

    [Fact]
    public async Task AddSeries_WithTemplatePublisherAndCollection_Persists()
    {
        var publisher = new Publisher("Éditions Fictives");
        var collection = new PublisherCollection("Collection Alpha", publisher);
        var series = new Series("Tintin");
        series.SetTemplate(publisher, collection);

        _fixture.Context.Publishers.Add(publisher);
        _fixture.Context.PublisherCollections.Add(collection);
        _fixture.Context.Series.Add(series);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Series
            .Include(s => s.TemplatePublisher)
            .Include(s => s.TemplatePublisherCollection)
            .FirstAsync(s => s.Id == series.Id);

        Assert.Equal("Éditions Fictives", saved.TemplatePublisher!.Name);
        Assert.Equal("Collection Alpha", saved.TemplatePublisherCollection!.Name);
    }

    [Fact]
    public async Task AddSeries_WithStatus_PersistsEnumAsReadableString()
    {
        // Confirms the project-wide enum-as-string convention (ConfigureConventions):
        // the raw column value must be the enum member name, not its numeric ordinal,
        // so that reordering enum members later cannot silently corrupt existing rows.
        var series = new Series("Tintin");
        series.SetStatus(SeriesStatus.InProgress);

        _fixture.Context.Series.Add(series);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var rawValue = await _fixture.Context.Database
            .SqlQuery<string>($"SELECT \"Status\" AS \"Value\" FROM \"Series\" WHERE \"Id\" = {series.Id}")
            .SingleAsync();

        Assert.Equal(nameof(SeriesStatus.InProgress), rawValue);
    }
}
