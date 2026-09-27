using Bdtheque.Domain.Common;
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
        Assert.Contains("Albums", tableNames);
        Assert.Contains("Contributions", tableNames);
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
    public void SortKeyMaxLength_AccommodatesWorstCaseArticleSuffixGrowth()
    {
        // TitleSortKeyCalculator grows the title (article moved to a bracketed suffix); the
        // "L'" elided form grows it the most (+3 characters, see its Compute remarks). The
        // SortKey column must stay large enough to hold a max-length title in that worst case,
        // or SaveChanges would fail for a title that legitimately fits the Title column.
        var entityType = _fixture.Context.Model.FindEntityType(typeof(Series))!;
        var titleMaxLength = entityType.FindProperty(nameof(Series.Title))!.GetMaxLength()!.Value;
        var sortKeyMaxLength = entityType.FindProperty(nameof(Series.SortKey))!.GetMaxLength()!.Value;

        var worstCaseTitle = "L'" + new string('a', titleMaxLength - 2);
        var worstCaseSortKey = TitleSortKeyCalculator.Compute(worstCaseTitle);

        Assert.True(worstCaseSortKey.Length <= sortKeyMaxLength,
            $"Sort key length {worstCaseSortKey.Length} exceeds the configured column max length {sortKeyMaxLength}.");
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

    [Fact]
    public async Task AddAlbum_AttachedToSeriesWithoutTitle_Persists()
    {
        var series = new Series("Tintin");
        var album = new Album(null, series);
        album.SetVolumeNumber(1);

        _fixture.Context.Series.Add(series);
        _fixture.Context.Albums.Add(album);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Albums
            .Include(a => a.Series)
            .FirstAsync(a => a.Id == album.Id);

        Assert.Null(saved.Title);
        Assert.Null(saved.SortKey);
        Assert.Equal("Tintin", saved.Series!.Title);
        Assert.Equal(1, saved.VolumeNumber);
    }

    [Fact]
    public async Task AddAlbum_StandaloneOmnibusWithGenresAndUniverses_Persists()
    {
        var album = new Album("Le Lotus bleu", null);
        album.SetType(AlbumType.Omnibus);
        album.SetVolumeRange(1, 6);
        album.SetFirstPublicationDate(1978, 6);
        var genre = new Genre("Aventure BD");
        var universe = new Universe("Franco-Belge BD");
        album.Genres.Add(genre);
        album.Universes.Add(universe);

        _fixture.Context.Albums.Add(album);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Albums
            .Include(a => a.Genres)
            .Include(a => a.Universes)
            .FirstAsync(a => a.Id == album.Id);

        Assert.Equal("Le Lotus bleu", saved.Title);
        Assert.Equal("Lotus bleu [Le]", saved.SortKey);
        Assert.Equal(AlbumType.Omnibus, saved.Type);
        Assert.Equal(1, saved.StartVolumeNumber);
        Assert.Equal(6, saved.EndVolumeNumber);
        Assert.Equal(1978, saved.FirstPublicationYear);
        Assert.Equal(6, saved.FirstPublicationMonth);
        Assert.Contains(saved.Genres, g => g.Label == "Aventure BD");
        Assert.Contains(saved.Universes, u => u.Name == "Franco-Belge BD");
    }

    [Fact]
    public async Task AddAlbum_WithType_PersistsEnumAsReadableString()
    {
        // Confirms the project-wide enum-as-string convention also applies to Album.Type.
        var album = new Album("Tintin", null);
        album.SetType(AlbumType.Omnibus);

        _fixture.Context.Albums.Add(album);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var rawValue = await _fixture.Context.Database
            .SqlQuery<string>($"SELECT \"Type\" AS \"Value\" FROM \"Albums\" WHERE \"Id\" = {album.Id}")
            .SingleAsync();

        Assert.Equal(nameof(AlbumType.Omnibus), rawValue);
    }

    [Fact]
    public async Task AddContribution_ForAlbum_Persists()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé (ModelCreation, Album)");
        var contribution = Contribution.ForAlbum(album, author, ContributionRole.Scenarist);

        _fixture.Context.Albums.Add(album);
        _fixture.Context.Authors.Add(author);
        _fixture.Context.Contributions.Add(contribution);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Contributions
            .Include(c => c.Album)
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == contribution.Id);

        Assert.Equal("Le Lotus bleu", saved.Album!.Title);
        Assert.Null(saved.Series);
        Assert.Equal("Hergé (ModelCreation, Album)", saved.Author.Pseudonym);
        Assert.Equal(ContributionRole.Scenarist, saved.Role);
    }

    [Fact]
    public async Task AddContribution_ForSeriesTemplate_Persists()
    {
        var series = new Series("Tintin (ModelCreation)");
        var author = new Author(null, null, "Hergé (ModelCreation, Série)");
        var contribution = Contribution.ForSeriesTemplate(series, author, ContributionRole.Illustrator);

        _fixture.Context.Series.Add(series);
        _fixture.Context.Authors.Add(author);
        _fixture.Context.Contributions.Add(contribution);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Contributions
            .Include(c => c.Series)
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == contribution.Id);

        Assert.Equal("Tintin (ModelCreation)", saved.Series!.Title);
        Assert.Null(saved.Album);
        Assert.Equal(ContributionRole.Illustrator, saved.Role);
    }

    [Fact]
    public async Task AddContribution_WithRole_PersistsEnumAsReadableString()
    {
        // Confirms the project-wide enum-as-string convention also applies to Contribution.Role.
        var album = new Album("Astérix (ModelCreation)", null);
        var author = new Author(null, null, "Goscinny (ModelCreation)");
        var contribution = Contribution.ForAlbum(album, author, ContributionRole.Colorist);

        _fixture.Context.Albums.Add(album);
        _fixture.Context.Authors.Add(author);
        _fixture.Context.Contributions.Add(contribution);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var rawValue = await _fixture.Context.Database
            .SqlQuery<string>($"SELECT \"Role\" AS \"Value\" FROM \"Contributions\" WHERE \"Id\" = {contribution.Id}")
            .SingleAsync();

        Assert.Equal(nameof(ContributionRole.Colorist), rawValue);
    }
}
