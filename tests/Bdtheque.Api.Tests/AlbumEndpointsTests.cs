using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>Administration of the albums (<c>/admin/albums</c>), including their deletion.</summary>
public sealed class AlbumEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AlbumEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Create_WithTheTitleOnly_AppliesTheDefaultsAndAComputedSortKey()
    {
        var response = await _client.PostAsJsonAsync("/admin/albums", Content(" Le Lotus bleu "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AlbumForm>();
        Assert.Equal("Lotus bleu [Le]", created!.SortKey);
        Assert.Equal(Content("Le Lotus bleu"), created.Content, AlbumContentComparer.Instance);
        Assert.Equal($"/admin/albums/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(created, await GetAsync(created.Id), AlbumFormComparer.Instance);
    }

    [Fact]
    public async Task Create_WithEveryField_ReturnsThemAsStored()
    {
        var series = await CreateSeriesAsync();
        var genre = await CreateGenreAsync();
        var universe = await CreateUniverseAsync();
        var author = await CreateAuthorAsync();
        var content = Content(UniqueTitle()) with
        {
            SeriesId = series.Id,
            Type = AlbumType.Omnibus,
            IsSpecialIssue = true,
            VolumeNumber = 2,
            StartVolumeNumber = 4,
            EndVolumeNumber = 6,
            FirstPublicationYear = 1978,
            FirstPublicationMonth = 6,
            Summary = " Résumé ",
            PersonalNotes = " Notes ",
            Rating = AlbumRating.Good,
            GenreIds = [genre.Id],
            UniverseIds = [universe.Id],
            Contributions = [new ContributionContent(author.Id, ContributionRole.Scenarist), new ContributionContent(author.Id, ContributionRole.Illustrator)],
        };

        var response = await _client.PostAsJsonAsync("/admin/albums", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AlbumForm>();
        Assert.Equal(content with { Summary = "Résumé", PersonalNotes = "Notes" }, created!.Content, AlbumContentComparer.Instance);
        Assert.Equal(created, await GetAsync(created.Id), AlbumFormComparer.Instance);
    }

    [Fact]
    public async Task Create_InASeriesWithoutTitle_HasNoSortKeyOfItsOwn()
    {
        var series = await CreateSeriesAsync();

        var created = await CreateAsync(Content(null) with { SeriesId = series.Id });

        Assert.Null(created.SortKey);
        Assert.Null(created.Content.Title);
    }

    [Fact]
    public async Task Create_InASeriesWithoutContributions_CopiesThoseOfTheSeries()
    {
        var scenarist = await CreateAuthorAsync();
        var illustrator = await CreateAuthorAsync();
        var series = await CreateSeriesAsync(
            new ContributionContent(scenarist.Id, ContributionRole.Scenarist), new ContributionContent(illustrator.Id, ContributionRole.Illustrator));

        var created = await CreateAsync(Content(null) with { SeriesId = series.Id });

        Assert.Equal(series.Content.Contributions, created.Content.Contributions);
        Assert.Equal(created, await GetAsync(created.Id), AlbumFormComparer.Instance);
    }

    [Fact]
    public async Task Create_InASeriesWithContributions_KeepsThoseOfTheForm()
    {
        var series = await CreateSeriesAsync(new ContributionContent((await CreateAuthorAsync()).Id, ContributionRole.Scenarist));
        var colorist = new ContributionContent((await CreateAuthorAsync()).Id, ContributionRole.Colorist);

        var created = await CreateAsync(Content(null) with { SeriesId = series.Id, Contributions = [colorist] });

        Assert.Equal([colorist], created.Content.Contributions);
    }

    [Fact]
    public async Task Update_AttachingToASeriesWithoutContributions_CopiesThoseOfTheSeries()
    {
        var series = await CreateSeriesAsync(new ContributionContent((await CreateAuthorAsync()).Id, ContributionRole.Scenarist));
        var created = await CreateAsync(Content(UniqueTitle()) with
        {
            Contributions = [new ContributionContent((await CreateAuthorAsync()).Id, ContributionRole.Colorist)],
        });

        var updated = await UpdateAsync(created, created.Content with { SeriesId = series.Id, Contributions = [] });

        Assert.Equal(series.Content.Contributions, updated.Content.Contributions);
        Assert.Equal(updated, await GetAsync(created.Id), AlbumFormComparer.Instance);
    }

    [Fact]
    public async Task Update_StayingInTheSameSeriesWithoutContributions_RemovesThemForGood()
    {
        // The album is the source of truth once attached: the series' contributions are not copied
        // again at each save.
        var series = await CreateSeriesAsync(new ContributionContent((await CreateAuthorAsync()).Id, ContributionRole.Scenarist));
        var created = await CreateAsync(Content(null) with { SeriesId = series.Id });

        var updated = await UpdateAsync(created, created.Content with { Contributions = [] });

        Assert.Empty(updated.Content.Contributions);
        Assert.Empty((await GetAsync(created.Id)).Content.Contributions);
    }

    [Fact]
    public async Task Create_WithAManualSortKey_KeepsItWhateverTheTitle()
    {
        var created = await CreateAsync(Content("Tintin") with { ManualSortKey = "Aventures de Tintin" });

        Assert.Equal("Aventures de Tintin", created.SortKey);
        Assert.Equal("Aventures de Tintin", created.Content.ManualSortKey);

        var renamed = await UpdateAsync(created, created.Content with { Title = "Les Aventures de Tintin" });

        Assert.Equal("Aventures de Tintin", renamed.SortKey);
    }

    [Fact]
    public async Task Update_WithoutManualSortKey_ReturnsToTheAutomaticMode()
    {
        var created = await CreateAsync(Content("Tintin") with { ManualSortKey = "Zorglub" });

        var updated = await UpdateAsync(created, created.Content with { Title = "Le Lotus bleu", ManualSortKey = null });

        Assert.Equal("Lotus bleu [Le]", updated.SortKey);
        Assert.Null(updated.Content.ManualSortKey);
    }

    [Fact]
    public async Task Update_FromOmnibusBackToRegular_ClearsTheRangeInASinglePut()
    {
        var created = await CreateAsync(Content(UniqueTitle()) with { Type = AlbumType.Omnibus, StartVolumeNumber = 1, EndVolumeNumber = 3 });

        var updated = await UpdateAsync(created, created.Content with { Type = AlbumType.Regular, StartVolumeNumber = null, EndVolumeNumber = null });

        Assert.Equal((AlbumType.Regular, (int?)null, (int?)null), (updated.Content.Type, updated.Content.StartVolumeNumber, updated.Content.EndVolumeNumber));
    }

    [Fact]
    public async Task Update_ClearingTheTitleWhileAttachingASeries_SucceedsInASinglePut()
    {
        var series = await CreateSeriesAsync();
        var created = await CreateAsync(Content(UniqueTitle()));

        var updated = await UpdateAsync(created, created.Content with { Title = null, SeriesId = series.Id });

        Assert.Equal(((string?)null, (Guid?)series.Id), (updated.Content.Title, updated.Content.SeriesId));
    }

    [Fact]
    public async Task Update_AtTheCurrentVersion_ReplacesTheFieldsAndTheAssociationsAndReturnsTheNewVersion()
    {
        var keptGenre = await CreateGenreAsync();
        var droppedGenre = await CreateGenreAsync();
        var addedGenre = await CreateGenreAsync();
        var droppedUniverse = await CreateUniverseAsync();
        var addedUniverse = await CreateUniverseAsync();
        var keptAuthor = await CreateAuthorAsync();
        var droppedAuthor = await CreateAuthorAsync();
        var addedAuthor = await CreateAuthorAsync();
        var created = await CreateAsync(Content(UniqueTitle()) with
        {
            GenreIds = [keptGenre.Id, droppedGenre.Id],
            UniverseIds = [droppedUniverse.Id],
            Contributions =
            [
                new ContributionContent(keptAuthor.Id, ContributionRole.Scenarist),
                new ContributionContent(droppedAuthor.Id, ContributionRole.Scenarist),
            ],
        });
        var edited = created.Content with
        {
            Title = UniqueTitle(),
            VolumeNumber = 3,
            IsSpecialIssue = true,
            FirstPublicationYear = 2001,
            Rating = AlbumRating.VeryPoor,
            GenreIds = [keptGenre.Id, addedGenre.Id],
            UniverseIds = [addedUniverse.Id],
            // The kept author keeps a role and gains another, the added one is credited, the dropped one is not.
            Contributions =
            [
                new ContributionContent(keptAuthor.Id, ContributionRole.Scenarist),
                new ContributionContent(keptAuthor.Id, ContributionRole.Illustrator),
                new ContributionContent(addedAuthor.Id, ContributionRole.Colorist),
            ],
        };

        var response = await _client.PutAsJsonAsync($"/admin/albums/{created.Id}", new UpdateAlbumRequest(edited, created.Version));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AlbumForm>();
        Assert.Equal(edited with { GenreIds = Sorted(edited.GenreIds), Contributions = Sorted(edited.Contributions) }, updated!.Content, AlbumContentComparer.Instance);
        Assert.NotEqual(created.Version, updated.Version);
        Assert.Equal(updated, await GetAsync(created.Id), AlbumFormComparer.Instance);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.Contributions.AnyAsync(c => c.AuthorId == droppedAuthor.Id));
    }

    [Fact]
    public async Task Update_ClearingEveryOptionalField_EmptiesThem()
    {
        var genre = await CreateGenreAsync();
        var created = await CreateAsync(Content(UniqueTitle()) with
        {
            VolumeNumber = 3,
            FirstPublicationYear = 1990,
            FirstPublicationMonth = 2,
            Summary = "Résumé",
            PersonalNotes = "Notes",
            Rating = AlbumRating.Average,
            GenreIds = [genre.Id],
        });

        var updated = await UpdateAsync(created, Content(created.Content.Title));

        Assert.Equal(Content(created.Content.Title), updated.Content, AlbumContentComparer.Instance);
    }

    [Fact]
    public async Task Create_WithoutTitleNorSeries_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/albums", Content(" "));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AlbumTitleRequiredWithoutSeries);
    }

    [Fact]
    public async Task Create_WithAManualSortKeyButNoTitle_IsABusinessError()
    {
        var series = await CreateSeriesAsync();

        var response = await _client.PostAsJsonAsync("/admin/albums", Content(null) with { SeriesId = series.Id, ManualSortKey = "Clé" });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AlbumManualSortKeyRequiresTitle);
    }

    [Fact]
    public async Task Create_WithAVolumeRangeOnARegularAlbum_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/albums", Content(UniqueTitle()) with { StartVolumeNumber = 1, EndVolumeNumber = 2 });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AlbumVolumeRangeOmnibusOnly);
    }

    [Fact]
    public async Task Create_WithAPublicationMonthButNoYear_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/albums", Content(UniqueTitle()) with { FirstPublicationMonth = 3 });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AlbumPublicationMonthRequiresYear);
    }

    [Fact]
    public async Task Create_WithTheSameContributionTwice_IsABusinessError()
    {
        var credit = new ContributionContent((await CreateAuthorAsync()).Id, ContributionRole.Colorist);

        var response = await _client.PostAsJsonAsync("/admin/albums", Content(UniqueTitle()) with { Contributions = [credit, credit] });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.ContributionAlreadyCredited);
    }

    [Fact]
    public async Task Create_WithATitleLongerThanItsColumn_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/albums", Content(new string('x', 501)));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.TextTooLong);
    }

    [Theory]
    [InlineData("series")]
    [InlineData("genre")]
    [InlineData("universe")]
    [InlineData("author")]
    public async Task Create_ReferencingAnUnknownRecord_IsAFunctionalError(string reference)
    {
        var unknown = Guid.CreateVersion7();
        var content = Content(UniqueTitle());
        content = reference switch
        {
            "series" => content with { SeriesId = unknown },
            "genre" => content with { GenreIds = [unknown] },
            "universe" => content with { UniverseIds = [unknown] },
            _ => content with { Contributions = [new ContributionContent(unknown, ContributionRole.Scenarist)] },
        };

        var response = await _client.PostAsJsonAsync("/admin/albums", content);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Get_UnknownAlbum_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/albums/{Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_WithABusinessError_KeepsTheAlbumUntouched()
    {
        var author = await CreateAuthorAsync();
        var created = await CreateAsync(Content(UniqueTitle()) with { Contributions = [new ContributionContent(author.Id, ContributionRole.Scenarist)] });
        var invalid = created.Content with
        {
            Title = UniqueTitle(),
            Contributions = [new ContributionContent((await CreateAuthorAsync()).Id, ContributionRole.Scenarist)],
            VolumeNumber = -1,
        };

        var response = await _client.PutAsJsonAsync($"/admin/albums/{created.Id}", new UpdateAlbumRequest(invalid, created.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AlbumVolumeNumberPositive);
        Assert.Equal(created, await GetAsync(created.Id), AlbumFormComparer.Instance);
    }

    [Fact]
    public async Task Update_FromAStaleVersion_IsAFunctionalErrorAndKeepsTheOtherModification()
    {
        var created = await CreateAsync(Content(UniqueTitle()));
        var otherTab = UniqueTitle();
        await UpdateAsync(created, created.Content with { Title = otherTab });

        var response = await _client.PutAsJsonAsync(
            $"/admin/albums/{created.Id}", new UpdateAlbumRequest(created.Content with { Title = UniqueTitle() }, created.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(otherTab, (await GetAsync(created.Id)).Content.Title);
    }

    [Fact]
    public async Task Update_UnknownAlbum_IsAFunctionalError()
    {
        var response = await _client.PutAsJsonAsync(
            $"/admin/albums/{Guid.CreateVersion7()}", new UpdateAlbumRequest(Content(UniqueTitle()), 1));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task DeletionImpact_AnnouncesEverythingDeletedWithTheAlbum()
    {
        var author = await CreateAuthorAsync();
        var created = await CreateAsync(Content(UniqueTitle()) with
        {
            GenreIds = [(await CreateGenreAsync()).Id],
            Contributions = [new ContributionContent(author.Id, ContributionRole.Scenarist), new ContributionContent(author.Id, ContributionRole.Colorist)],
        });
        await SeedEditionsAsync(created.Id);

        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/albums/{created.Id}/deletion-impact");

        Assert.Empty(impact!.BlockedBy);
        Assert.Empty(impact.AssociationsRemoved);
        Assert.Equal(
            [
                new ImpactCount(EntityKind.Edition, 2),
                new ImpactCount(EntityKind.EditionVisual, 3),
                new ImpactCount(EntityKind.Contribution, 2),
                new ImpactCount(EntityKind.PurchaseIntent, 1),
            ],
            impact.DeletedWith);
        // Deleted with all its editions, the album does not merely leave the collection.
        Assert.Empty(impact.LeavingCollection);
        Assert.NotEmpty(impact.Fingerprint);
    }

    [Fact]
    public async Task DeletionImpact_OfAnUnknownAlbum_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/albums/{Guid.CreateVersion7()}/deletion-impact");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_RemovesTheAlbumWithItsCompositionsAndKeepsTheRecordsItReferenced()
    {
        var series = await CreateSeriesAsync();
        var genre = await CreateGenreAsync();
        var universe = await CreateUniverseAsync();
        var author = await CreateAuthorAsync();
        var created = await CreateAsync(Content(UniqueTitle()) with
        {
            SeriesId = series.Id,
            GenreIds = [genre.Id],
            UniverseIds = [universe.Id],
            Contributions = [new ContributionContent(author.Id, ContributionRole.Scenarist)],
        });
        var publisherId = await SeedEditionsAsync(created.Id);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/albums/{created.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(await GetAsync(created.Id), impact!.Fingerprint));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await ProblemAssert.IsProblemAsync(
            await _client.GetAsync($"/admin/albums/{created.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.Editions.AnyAsync(e => e.AlbumId == created.Id));
        Assert.False(await context.EditionVisuals.AnyAsync(v => v.Edition.AlbumId == created.Id));
        Assert.False(await context.Contributions.AnyAsync(c => c.AlbumId == created.Id));
        Assert.False(await context.PurchaseIntents.AnyAsync(p => p.AlbumId == created.Id));
        Assert.True(await context.Series.AnyAsync(s => s.Id == series.Id));
        Assert.True(await context.Genres.AnyAsync(g => g.Id == genre.Id));
        Assert.True(await context.Universes.AnyAsync(u => u.Id == universe.Id));
        Assert.True(await context.Authors.AnyAsync(a => a.Id == author.Id));
        Assert.True(await context.Publishers.AnyAsync(p => p.Id == publisherId));
    }

    [Fact]
    public async Task Delete_FromAStaleVersion_IsAFunctionalError()
    {
        var created = await CreateAsync(Content(UniqueTitle()));
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/albums/{created.Id}/deletion-impact");
        await UpdateAsync(created, created.Content with { Title = UniqueTitle() });

        var response = await _client.DeleteAsync(DeleteUri(created, impact!.Fingerprint));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_AfterAnEditionWasAdded_IsAFunctionalErrorWithTheNewImpact()
    {
        // The edition is added without changing the album's version: only the fingerprint shows
        // that the impact grew since the user confirmed it.
        var created = await CreateAsync(Content(UniqueTitle()));
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/albums/{created.Id}/deletion-impact");
        await SeedEditionsAsync(created.Id);

        var response = await _client.DeleteAsync(DeleteUri(await GetAsync(created.Id), impact!.Fingerprint));

        var problem = await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Contains(ProblemAssert.ImpactOf(problem).DeletedWith, c => c.Kind == EntityKind.Edition);
    }

    private static string UniqueTitle() => $"Album {Guid.NewGuid():N}";

    private static string DeleteUri(AlbumForm album, string fingerprint) =>
        $"/admin/albums/{album.Id}?version={album.Version}&fingerprint={Uri.EscapeDataString(fingerprint)}";

    // The associations come back in a stable order, whichever order the form listed them in.
    private static List<Guid> Sorted(IReadOnlyList<Guid> ids) => ids.Order().ToList();

    private static List<ContributionContent> Sorted(IReadOnlyList<ContributionContent> contributions) =>
        contributions.OrderBy(c => c.Role).ThenBy(c => c.AuthorId).ToList();

    private static AlbumContent Content(string? title) =>
        new(title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []);

    private async Task<AlbumForm> GetAsync(Guid id) => (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{id}"))!;

    private async Task<AlbumForm> CreateAsync(AlbumContent content) => await PostAsync<AlbumForm>("/admin/albums", content);

    private async Task<AlbumForm> UpdateAsync(AlbumForm album, AlbumContent content)
    {
        var response = await _client.PutAsJsonAsync($"/admin/albums/{album.Id}", new UpdateAlbumRequest(content, album.Version));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AlbumForm>())!;
    }

    private Task<SeriesForm> CreateSeriesAsync(params ContributionContent[] contributions) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            $"Série {Guid.NewGuid():N}", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [],
            contributions.OrderBy(c => c.Role).ThenBy(c => c.AuthorId).ToList()));

    private Task<GenreForm> CreateGenreAsync() =>
        PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {Guid.NewGuid():N}"));

    private Task<UniverseForm> CreateUniverseAsync() =>
        PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest($"Univers {Guid.NewGuid():N}", null, null));

    private Task<AuthorForm> CreateAuthorAsync() =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Auteur {Guid.NewGuid():N}", null, null, null, null));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }

    // Visuals and intents have no endpoint yet: seeded through the domain, with their editions. One owned
    // edition with two visuals, one wished edition with one visual and its intent.
    private async Task<Guid> SeedEditionsAsync(Guid albumId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var album = await context.Albums.SingleAsync(a => a.Id == albumId);
        var publisher = new Publisher($"Éditeur {Guid.NewGuid():N}");
        var owned = new Edition(album, publisher);
        album.RecordAcquisition(owned, DomainEnums.AcquisitionMode.Purchase);
        owned.AppendVisual(DomainEnums.VisualType.Cover, $"{Guid.NewGuid():N}.jpg");
        owned.AppendVisual(DomainEnums.VisualType.BackCover, $"{Guid.NewGuid():N}.jpg");
        var wished = new Edition(album, publisher);
        wished.AppendVisual(DomainEnums.VisualType.Cover, $"{Guid.NewGuid():N}.jpg");
        album.AddPurchaseIntent(wished);
        context.AddRange(publisher, owned, wished);
        await context.SaveChangesAsync();
        return publisher.Id;
    }

    // Records compare their lists by reference: these compare the content by value.
    private sealed class AlbumContentComparer : IEqualityComparer<AlbumContent>
    {
        public static readonly AlbumContentComparer Instance = new();

        public bool Equals(AlbumContent? x, AlbumContent? y) =>
            x is not null && y is not null
            && x with { GenreIds = [], UniverseIds = [], Contributions = [] } == y with { GenreIds = [], UniverseIds = [], Contributions = [] }
            && x.GenreIds.SequenceEqual(y.GenreIds)
            && x.UniverseIds.SequenceEqual(y.UniverseIds)
            && x.Contributions.SequenceEqual(y.Contributions);

        public int GetHashCode(AlbumContent obj) => obj.SeriesId.GetHashCode();
    }

    private sealed class AlbumFormComparer : IEqualityComparer<AlbumForm>
    {
        public static readonly AlbumFormComparer Instance = new();

        public bool Equals(AlbumForm? x, AlbumForm? y) =>
            x is not null && y is not null
            && (x.Id, x.SortKey, x.Version) == (y.Id, y.SortKey, y.Version)
            && AlbumContentComparer.Instance.Equals(x.Content, y.Content);

        public int GetHashCode(AlbumForm obj) => obj.Id.GetHashCode();
    }
}
