using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Verifies that the migrations produce a schema matching the EF Core model, and that basic
/// persistence works against it, on the production database engine (see
/// <see cref="BdthequeDbContextFixture"/>).
/// </summary>
public sealed class ModelCreationTests : IAsyncLifetime
{
    // One database and one context per test (xunit creates a class instance per test): a
    // shared context would let a failed SaveChanges leave tracked entities behind and break
    // every later test, and shared data would make tests depend on one another.
    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Migrations_CreateATableForEveryMappedEntity()
    {
        var mappedTables = _fixture.Context.Model
            .GetEntityTypes()
            .Select(e => e.GetTableName()!)
            .Distinct()
            .Order()
            .ToList();

        var migratedTables = await _fixture.Context.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync();

        Assert.All(mappedTables, table => Assert.Contains(table, migratedTables));
    }

    [Fact]
    public void EntityKeys_AreNeverGeneratedByEfCore()
    {
        // EntityBase assigns Id in the domain. A key EF believes it generates makes any new child
        // discovered through a loaded parent's navigation look like an existing row (UPDATE of
        // 0 rows instead of INSERT) — see AddPurchaseIntent_OnAlbumLoadedFromDatabase_Persists.
        var generatedKeys = _fixture.Context.Model.GetEntityTypes()
            .Where(t => typeof(EntityBase).IsAssignableFrom(t.ClrType))
            .Select(t => t.FindProperty(nameof(EntityBase.Id))!)
            .Where(p => p.ValueGenerated != ValueGenerated.Never)
            .Select(p => p.DeclaringType.ShortName())
            .ToList();

        Assert.Empty(generatedKeys);
    }

    [Fact]
    public void MigrationsSnapshot_EntityKeys_AreNeverGenerated()
    {
        // has-pending-model-changes ignores this annotation (it produces no DDL), so a snapshot
        // generated before the key convention would go unnoticed until the next migration
        // silently carried the unrelated diff. Join tables have composite keys and no Id.
        var snapshotModel = _fixture.Context.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;

        var generatedKeys = snapshotModel.GetEntityTypes()
            .Select(t => t.FindProperty(nameof(EntityBase.Id)))
            .Where(p => p is not null && p.ValueGenerated != ValueGenerated.Never)
            .Select(p => p!.DeclaringType.Name)
            .ToList();

        Assert.Empty(generatedKeys);
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
        var collection = publisher.AddCollection("Tintin");

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
        series.AddGenre(genre);
        series.AddUniverse(universe);

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
        var collection = publisher.AddCollection("Collection Alpha");
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
    public async Task AddSeries_WithStatus_PersistsEnumAsExplicitInt()
    {
        // Confirms the project-wide enum-as-int convention (ConfigureConventions): the raw
        // column value is the member's explicit numeric value, not the CLR default ordinal —
        // every domain enum assigns its values explicitly precisely so this holds regardless
        // of declaration order (see choix-implementation.md).
        var series = new Series("Tintin");
        series.SetStatus(SeriesStatus.InProgress);

        _fixture.Context.Series.Add(series);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var rawValue = await _fixture.Context.Database
            .SqlQuery<int>($"SELECT \"Status\" AS \"Value\" FROM \"Series\" WHERE \"Id\" = {series.Id}")
            .SingleAsync();

        Assert.Equal((int)SeriesStatus.InProgress, rawValue);
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
        album.AddGenre(genre);
        album.AddUniverse(universe);

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
    public async Task AddAlbum_WithType_PersistsEnumAsExplicitInt()
    {
        // Confirms the project-wide enum-as-int convention also applies to Album.Type.
        var album = new Album("Tintin", null);
        album.SetType(AlbumType.Omnibus);

        _fixture.Context.Albums.Add(album);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var rawValue = await _fixture.Context.Database
            .SqlQuery<int>($"SELECT \"Type\" AS \"Value\" FROM \"Albums\" WHERE \"Id\" = {album.Id}")
            .SingleAsync();

        Assert.Equal((int)AlbumType.Omnibus, rawValue);
    }

    [Fact]
    public async Task AddContribution_ForAlbum_Persists()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");
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
        Assert.Equal("Hergé", saved.Author.Pseudonym);
        Assert.Equal(ContributionRole.Scenarist, saved.Role);
    }

    [Fact]
    public async Task AddContribution_ForSeriesTemplate_Persists()
    {
        var series = new Series("Tintin");
        var author = new Author(null, null, "Hergé");
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

        Assert.Equal("Tintin", saved.Series!.Title);
        Assert.Null(saved.Album);
        Assert.Equal(ContributionRole.Illustrator, saved.Role);
    }

    [Fact]
    public async Task AddContribution_WithRole_PersistsEnumAsExplicitInt()
    {
        // Confirms the project-wide enum-as-int convention also applies to Contribution.Role.
        var album = new Album("Astérix", null);
        var author = new Author(null, null, "Goscinny");
        var contribution = Contribution.ForAlbum(album, author, ContributionRole.Colorist);

        _fixture.Context.Albums.Add(album);
        _fixture.Context.Authors.Add(author);
        _fixture.Context.Contributions.Add(contribution);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var rawValue = await _fixture.Context.Database
            .SqlQuery<int>($"SELECT \"Role\" AS \"Value\" FROM \"Contributions\" WHERE \"Id\" = {contribution.Id}")
            .SingleAsync();

        Assert.Equal((int)ContributionRole.Colorist, rawValue);
    }

    [Fact]
    public async Task AddEdition_Minimal_Persists()
    {
        var album = new Album("Le Lotus bleu", null);
        var publisher = new Publisher("Casterman");
        var edition = new Edition(album, publisher);

        _fixture.Context.Albums.Add(album);
        _fixture.Context.Publishers.Add(publisher);
        _fixture.Context.Editions.Add(edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Editions
            .Include(e => e.Album)
            .Include(e => e.Publisher)
            .FirstAsync(e => e.Id == edition.Id);

        Assert.Equal("Le Lotus bleu", saved.Album.Title);
        Assert.Equal("Casterman", saved.Publisher.Name);
        Assert.True(saved.IsColor);
        Assert.False(saved.IsDedicated);
        Assert.Null(saved.AcquisitionMode);
    }

    [Fact]
    public async Task AddEdition_Owned_WithCollectionAndPrice_Persists()
    {
        var album = new Album("Astérix", null);
        var publisher = new Publisher("Dargaud");
        var collection = publisher.AddCollection("Astérix");
        var edition = new Edition(album, publisher);
        edition.SetPublisher(publisher, collection);
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);
        edition.SetAcquisitionDate(new DateOnly(2020, 3, 15));
        edition.SetAcquisitionPrice(9.9m, "EUR");
        edition.SetPublicationYear(1978);
        edition.SetIsbn("2-205-00217-0");

        _fixture.Context.Albums.Add(album);
        _fixture.Context.Publishers.Add(publisher);
        _fixture.Context.PublisherCollections.Add(collection);
        _fixture.Context.Editions.Add(edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Editions
            .Include(e => e.PublisherCollection)
            .FirstAsync(e => e.Id == edition.Id);

        Assert.Equal("Astérix", saved.PublisherCollection!.Name);
        Assert.Equal(AcquisitionMode.Purchase, saved.AcquisitionMode);
        Assert.Equal(new DateOnly(2020, 3, 15), saved.AcquisitionDate);
        Assert.Equal(9.9m, saved.AcquisitionAmount);
        Assert.Equal("EUR", saved.AcquisitionCurrency);
        Assert.Equal(1978, saved.PublicationYear);
        Assert.Equal("2-205-00217-0", saved.Isbn);
    }

    [Fact]
    public async Task AddEdition_WithAcquisitionMode_PersistsEnumAsExplicitInt()
    {
        // Confirms the project-wide enum-as-int convention also applies to Edition.AcquisitionMode.
        var album = new Album("Gaston", null);
        var publisher = new Publisher("Dupuis");
        var edition = new Edition(album, publisher);
        edition.SetAcquisitionMode(AcquisitionMode.Inherited);

        _fixture.Context.Albums.Add(album);
        _fixture.Context.Publishers.Add(publisher);
        _fixture.Context.Editions.Add(edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var rawValue = await _fixture.Context.Database
            .SqlQuery<int>($"SELECT \"AcquisitionMode\" AS \"Value\" FROM \"Editions\" WHERE \"Id\" = {edition.Id}")
            .SingleAsync();

        Assert.Equal((int)AcquisitionMode.Inherited, rawValue);
    }

    [Fact]
    public async Task AcquisitionAmount_ThreeDecimalCurrency_RoundTripsWithoutRounding()
    {
        // Some ISO 4217 currencies (KWD, BHD, OMR, JOD, TND) have 3 minor-unit digits;
        // fonctionnel.md § Gestion des devises requires supporting any currency, so a column
        // scale below 3 would let PostgreSQL silently round those amounts on save.
        var album = new Album("Tintin", null);
        var publisher = new Publisher("Casterman");
        var edition = new Edition(album, publisher);
        edition.SetAcquisitionMode(AcquisitionMode.Purchase);
        edition.SetAcquisitionPrice(12.345m, "KWD");
        _fixture.Context.AddRange(album, publisher, edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Editions.SingleAsync(e => e.Id == edition.Id);

        Assert.Equal(12.345m, saved.AcquisitionAmount);
    }

    [Fact]
    public async Task AddEditionVisual_Persists()
    {
        var album = new Album("Astérix", null);
        var publisher = new Publisher("Dargaud");
        var edition = new Edition(album, publisher);
        var visual = edition.AddVisual(VisualType.Cover, "covers/asterix-01.jpg", 1);

        _fixture.Context.Albums.Add(album);
        _fixture.Context.Publishers.Add(publisher);
        _fixture.Context.Editions.Add(edition);
        _fixture.Context.EditionVisuals.Add(visual);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.EditionVisuals
            .Include(v => v.Edition)
            .FirstAsync(v => v.Id == visual.Id);

        Assert.Equal(VisualType.Cover, saved.Type);
        Assert.Equal("covers/asterix-01.jpg", saved.MediaReference);
        Assert.Equal(1, saved.DisplayOrder);
        Assert.Equal(edition.Id, saved.EditionId);
    }

    [Fact]
    public async Task AddGenre_OnAlbumLoadedFromDatabase_Persists()
    {
        var album = new Album("Gaston", null);
        var genre = new Genre("Humour");
        _fixture.Context.AddRange(album, genre);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var reloaded = await _fixture.Context.Albums.Include(a => a.Genres).FirstAsync(a => a.Id == album.Id);
        reloaded.AddGenre(await _fixture.Context.Genres.FindAsync(genre.Id) ?? throw new InvalidOperationException());
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Albums.Include(a => a.Genres).FirstAsync(a => a.Id == album.Id);
        Assert.Equal(genre.Id, Assert.Single(saved.Genres).Id);
    }

    [Fact]
    public async Task AddVisual_OnEditionLoadedFromDatabase_Persists()
    {
        // The nominal flow: the visual is only reachable through the edition's read-only
        // collection, so EF must discover it there (backing field) at SaveChanges.
        var album = new Album("Gaston", null);
        var publisher = new Publisher("Dupuis");
        var edition = new Edition(album, publisher);
        _fixture.Context.AddRange(album, publisher, edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var reloaded = await _fixture.Context.Editions.Include(e => e.Visuals).FirstAsync(e => e.Id == edition.Id);
        reloaded.AddVisual(VisualType.BackCover, "back-covers/gaston-01.jpg", 0);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Editions
            .Include(e => e.Visuals)
            .FirstAsync(e => e.Id == edition.Id);

        Assert.Single(saved.Visuals);
        Assert.Equal(VisualType.BackCover, saved.Visuals.Single().Type);
    }

    [Fact]
    public async Task AddEditionVisual_WithType_PersistsEnumAsExplicitInt()
    {
        // Confirms the project-wide enum-as-int convention also applies to EditionVisual.Type.
        var album = new Album("Spirou", null);
        var publisher = new Publisher("Dupuis");
        var edition = new Edition(album, publisher);
        var visual = edition.AddVisual(VisualType.Endpaper, "endpapers/spirou-01.jpg", 0);

        _fixture.Context.Albums.Add(album);
        _fixture.Context.Publishers.Add(publisher);
        _fixture.Context.Editions.Add(edition);
        _fixture.Context.EditionVisuals.Add(visual);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var rawValue = await _fixture.Context.Database
            .SqlQuery<int>($"SELECT \"Type\" AS \"Value\" FROM \"EditionVisuals\" WHERE \"Id\" = {visual.Id}")
            .SingleAsync();

        Assert.Equal((int)VisualType.Endpaper, rawValue);
    }

    // Album.AddPurchaseIntent checks its rules against the loaded PurchaseIntents: these tests
    // pin that the aggregate is complete however the album is loaded, with no explicit Include.
    [Fact]
    public async Task AddPurchaseIntent_WholeAlbum_OnAlbumQueriedWithoutInclude_WhenEditionTargeted_Throws()
    {
        var album = await PersistAlbumWithEditionIntentAsync("Yakari");

        var reloaded = await _fixture.Context.Albums.FirstAsync(a => a.Id == album.Id);

        Assert.Equal(DomainRules.PurchaseIntentEditionsAlreadyTargeted,
            Assert.Throws<DomainRuleViolationException>(() => reloaded.AddPurchaseIntent()).Rule);
    }

    [Fact]
    public async Task AddPurchaseIntent_WholeAlbum_OnAlbumFound_WhenEditionTargeted_Throws()
    {
        var album = await PersistAlbumWithEditionIntentAsync("Yakari");

        var reloaded = await _fixture.Context.Albums.FindAsync(album.Id);

        Assert.Equal(DomainRules.PurchaseIntentEditionsAlreadyTargeted,
            Assert.Throws<DomainRuleViolationException>(() => reloaded!.AddPurchaseIntent()).Rule);
    }

    [Fact]
    public async Task AddPurchaseIntent_Edition_OnAlbumReachedThroughEdition_WhenWholeAlbumTargeted_Throws()
    {
        var album = new Album("Yakari", null);
        var publisher = new Publisher("Le Lombard");
        var edition = new Edition(album, publisher);
        album.AddPurchaseIntent();
        _fixture.Context.AddRange(album, publisher, edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var reloadedEdition = await _fixture.Context.Editions
            .Include(e => e.Album)
            .FirstAsync(e => e.Id == edition.Id);

        Assert.Equal(DomainRules.PurchaseIntentAlbumAlreadyTargeted,
            Assert.Throws<DomainRuleViolationException>(() => reloadedEdition.Album.AddPurchaseIntent(reloadedEdition)).Rule);
    }

    [Fact]
    public async Task AddPurchaseIntent_OnAlbumLoadedFromDatabase_Persists()
    {
        // The nominal flow: an existing album is loaded, then gains an intent. The intent is
        // only reachable through the album's collection, so EF discovers it at SaveChanges.
        var album = new Album("Yakari", null);
        _fixture.Context.Albums.Add(album);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var reloaded = await _fixture.Context.Albums.FirstAsync(a => a.Id == album.Id);
        var intent = reloaded.AddPurchaseIntent();
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        Assert.True(await _fixture.Context.PurchaseIntents.AnyAsync(p => p.Id == intent.Id));
    }

    private async Task<Album> PersistAlbumWithEditionIntentAsync(string title)
    {
        var album = new Album(title, null);
        var publisher = new Publisher("Le Lombard");
        var edition = new Edition(album, publisher);
        album.AddPurchaseIntent(edition);
        _fixture.Context.AddRange(album, publisher, edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();
        return album;
    }

    [Fact]
    public async Task AddPurchaseIntent_WholeAlbum_PersistsThroughAlbumAggregate()
    {
        var album = new Album("Blake et Mortimer", null);
        var intent = album.AddPurchaseIntent();

        _fixture.Context.Albums.Add(album);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Albums
            .Include(a => a.PurchaseIntents)
            .FirstAsync(a => a.Id == album.Id);

        var savedIntent = Assert.Single(saved.PurchaseIntents);
        Assert.Equal(intent.Id, savedIntent.Id);
        Assert.Null(savedIntent.EditionId);
    }

    [Fact]
    public async Task AddPurchaseIntent_Editions_PersistThroughAlbumAggregate()
    {
        var album = new Album("Thorgal", null);
        var publisher = new Publisher("Le Lombard");
        var firstEdition = new Edition(album, publisher);
        var secondEdition = new Edition(album, publisher);
        album.AddPurchaseIntent(firstEdition);
        album.AddPurchaseIntent(secondEdition);

        _fixture.Context.Albums.Add(album);
        _fixture.Context.Publishers.Add(publisher);
        _fixture.Context.Editions.AddRange(firstEdition, secondEdition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var saved = await _fixture.Context.Albums
            .Include(a => a.PurchaseIntents)
            .FirstAsync(a => a.Id == album.Id);

        Assert.Equal(
            new[] { firstEdition.Id, secondEdition.Id }.Order(),
            saved.PurchaseIntents.Select(p => p.EditionId!.Value).Order());
    }

    [Fact]
    public async Task SetAcquisitionMode_OnTargetedEditionQueriedWithoutInclude_Throws()
    {
        // The guard relies on Edition.PurchaseIntent: it must be loaded with the edition however
        // the edition is queried, or the guard would silently let the invariant be broken.
        var album = new Album("Yakari", null);
        var publisher = new Publisher("Le Lombard");
        var edition = new Edition(album, publisher);
        album.AddPurchaseIntent(edition);
        _fixture.Context.AddRange(album, publisher, edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var reloaded = await _fixture.Context.Editions.FirstAsync(e => e.Id == edition.Id);

        Assert.Equal(DomainRules.EditionTargetedByPurchaseIntent,
            Assert.Throws<DomainRuleViolationException>(() => reloaded.SetAcquisitionMode(AcquisitionMode.Purchase)).Rule);
    }

    [Fact]
    public async Task ConfirmPurchase_OnAlbumLoadedFromDatabase_DeletesRealizedIntentOnly()
    {
        var album = new Album("Yakari", null);
        var publisher = new Publisher("Le Lombard");
        var bought = new Edition(album, publisher);
        var stillWanted = new Edition(album, publisher);
        album.AddPurchaseIntent(bought);
        var remaining = album.AddPurchaseIntent(stillWanted);
        _fixture.Context.AddRange(album, publisher, bought, stillWanted);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        var reloadedAlbum = await _fixture.Context.Albums.FirstAsync(a => a.Id == album.Id);
        var reloadedEdition = await _fixture.Context.Editions.FirstAsync(e => e.Id == bought.Id);
        reloadedAlbum.ConfirmPurchase(reloadedEdition, AcquisitionMode.Purchase);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        Assert.Equal([remaining.Id], await _fixture.Context.PurchaseIntents.Select(p => p.Id).ToListAsync());
        var saved = await _fixture.Context.Editions.FirstAsync(e => e.Id == bought.Id);
        Assert.Equal(AcquisitionMode.Purchase, saved.AcquisitionMode);
        Assert.Null(saved.PurchaseIntent);
    }
}
