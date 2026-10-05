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

namespace Bdtheque.Api.Tests;

/// <summary>Administration of the series (<c>/admin/series</c>), including their deletion.</summary>
public sealed class SeriesEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SeriesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Create_WithTheTitleOnly_AppliesTheDefaultsAndAComputedSortKey()
    {
        var response = await _client.PostAsJsonAsync("/admin/series", Content(" Le Lotus bleu "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SeriesForm>();
        Assert.Equal("Lotus bleu [Le]", created!.SortKey);
        Assert.Equal(Content("Le Lotus bleu"), created.Content, SeriesContentComparer.Instance);
        Assert.Equal($"/admin/series/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(created, await GetAsync(created.Id), SeriesFormComparer.Instance);
    }

    [Fact]
    public async Task Create_WithEveryField_ReturnsThemAsStored()
    {
        var genre = await CreateGenreAsync();
        var universe = await CreateUniverseAsync();
        var publisher = await CreatePublisherAsync();
        var collection = await CreateCollectionAsync(publisher);
        var author = await CreateAuthorAsync();
        var content = Content(UniqueTitle()) with
        {
            Status = SeriesStatus.InProgress,
            TheoreticalVolumeCount = 12,
            IsComplete = true,
            ExcludeFromMissingVolumes = true,
            ExcludeFromReleaseEstimates = true,
            Summary = " Résumé ",
            PersonalNotes = " Notes ",
            EditionTemplate = new SeriesEditionTemplate(
                publisher.Id, collection.Id, EditionCategory.SpecialEdition, EditionCondition.Good, BindingType.Hardcover,
                BookOrientation.Landscape, ReadingDirection.RightToLeft, EditionFormat.Special, false),
            GenreIds = [genre.Id],
            UniverseIds = [universe.Id],
            Contributions = [new ContributionContent(author.Id, ContributionRole.Scenarist), new ContributionContent(author.Id, ContributionRole.Illustrator)],
        };

        var response = await _client.PostAsJsonAsync("/admin/series", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SeriesForm>();
        Assert.Equal(content with { Summary = "Résumé", PersonalNotes = "Notes" }, created!.Content, SeriesContentComparer.Instance);
        Assert.Equal(created, await GetAsync(created.Id), SeriesFormComparer.Instance);
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
    public async Task Create_WithBlankTitle_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/series", Content(" "));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SeriesTitleRequired);
    }

    [Fact]
    public async Task Create_WithABlankManualSortKey_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/series", Content(UniqueTitle()) with { ManualSortKey = " " });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SeriesSortKeyRequired);
    }

    [Fact]
    public async Task Create_WithANonPositiveTheoreticalVolumeCount_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/series", Content(UniqueTitle()) with { TheoreticalVolumeCount = 0 });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SeriesTheoreticalVolumeCountPositive);
    }

    [Fact]
    public async Task Create_WithATitleLongerThanItsColumn_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/series", Content(new string('x', 501)));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.TextTooLong);
    }

    [Fact]
    public async Task Create_WithATemplateCollectionButNoPublisher_IsABusinessError()
    {
        var collection = await CreateCollectionAsync(await CreatePublisherAsync());

        var response = await _client.PostAsJsonAsync("/admin/series", WithTemplate(Content(UniqueTitle()), null, collection.Id));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherCollectionNotOfPublisher);
    }

    [Fact]
    public async Task Create_WithATemplateCollectionOfAnotherPublisher_IsABusinessError()
    {
        var collection = await CreateCollectionAsync(await CreatePublisherAsync());
        var otherPublisher = await CreatePublisherAsync();

        var response = await _client.PostAsJsonAsync("/admin/series", WithTemplate(Content(UniqueTitle()), otherPublisher.Id, collection.Id));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherCollectionNotOfPublisher);
    }

    [Fact]
    public async Task Create_WithTheSameContributionTwice_IsABusinessError()
    {
        var author = await CreateAuthorAsync();
        var credit = new ContributionContent(author.Id, ContributionRole.Colorist);

        var response = await _client.PostAsJsonAsync("/admin/series", Content(UniqueTitle()) with { Contributions = [credit, credit] });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.ContributionAlreadyCredited);
    }

    [Theory]
    [InlineData("genre")]
    [InlineData("universe")]
    [InlineData("author")]
    [InlineData("publisher")]
    [InlineData("collection")]
    public async Task Create_ReferencingAnUnknownRecord_IsAFunctionalError(string reference)
    {
        var unknown = Guid.CreateVersion7();
        var content = Content(UniqueTitle());
        content = reference switch
        {
            "genre" => content with { GenreIds = [unknown] },
            "universe" => content with { UniverseIds = [unknown] },
            "author" => content with { Contributions = [new ContributionContent(unknown, ContributionRole.Scenarist)] },
            "publisher" => WithTemplate(content, unknown, null),
            _ => WithTemplate(content, (await CreatePublisherAsync()).Id, unknown),
        };

        var response = await _client.PostAsJsonAsync("/admin/series", content);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Get_UnknownSeries_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/series/{Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
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
        var publisher = await CreatePublisherAsync();
        var edited = created.Content with
        {
            Title = UniqueTitle(),
            Status = SeriesStatus.Completed,
            TheoreticalVolumeCount = 7,
            IsComplete = true,
            ExcludeFromMissingVolumes = true,
            ExcludeFromReleaseEstimates = true,
            GenreIds = [keptGenre.Id, addedGenre.Id],
            UniverseIds = [addedUniverse.Id],
            // The kept author keeps a role and gains another, the added one is credited, the dropped one is not.
            Contributions =
            [
                new ContributionContent(keptAuthor.Id, ContributionRole.Scenarist),
                new ContributionContent(keptAuthor.Id, ContributionRole.Illustrator),
                new ContributionContent(addedAuthor.Id, ContributionRole.Colorist),
            ],
            EditionTemplate = created.Content.EditionTemplate with { PublisherId = publisher.Id, Binding = BindingType.Paperback },
        };

        var response = await _client.PutAsJsonAsync($"/admin/series/{created.Id}", new UpdateSeriesRequest(edited, created.Version));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SeriesForm>();
        Assert.Equal(edited with { GenreIds = Sorted(edited.GenreIds), Contributions = Sorted(edited.Contributions) }, updated!.Content, SeriesContentComparer.Instance);
        Assert.NotEqual(created.Version, updated.Version);
        Assert.Equal(updated, await GetAsync(created.Id), SeriesFormComparer.Instance);
    }

    [Fact]
    public async Task Update_ClearingEveryOptionalField_EmptiesThem()
    {
        var genre = await CreateGenreAsync();
        var publisher = await CreatePublisherAsync();
        var created = await CreateAsync(WithTemplate(Content(UniqueTitle()), publisher.Id, null) with
        {
            Status = SeriesStatus.Abandoned,
            TheoreticalVolumeCount = 3,
            IsComplete = true,
            ExcludeFromMissingVolumes = true,
            ExcludeFromReleaseEstimates = true,
            Summary = "Résumé",
            GenreIds = [genre.Id],
        });

        var updated = await UpdateAsync(created, Content(created.Content.Title));

        Assert.Equal(Content(created.Content.Title), updated.Content, SeriesContentComparer.Instance);
    }

    [Fact]
    public async Task Update_WithABusinessErrorOnAReference_KeepsTheSeriesUntouched()
    {
        var author = await CreateAuthorAsync();
        var created = await CreateAsync(Content(UniqueTitle()) with { Contributions = [new ContributionContent(author.Id, ContributionRole.Scenarist)] });
        var otherAuthor = await CreateAuthorAsync();
        var invalid = created.Content with
        {
            Title = UniqueTitle(),
            Contributions = [new ContributionContent(otherAuthor.Id, ContributionRole.Scenarist)],
            TheoreticalVolumeCount = -1,
        };

        var response = await _client.PutAsJsonAsync($"/admin/series/{created.Id}", new UpdateSeriesRequest(invalid, created.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SeriesTheoreticalVolumeCountPositive);
        Assert.Equal(created, await GetAsync(created.Id), SeriesFormComparer.Instance);
    }

    [Fact]
    public async Task Update_FromAStaleVersion_IsAFunctionalErrorAndKeepsTheOtherModification()
    {
        var created = await CreateAsync(Content(UniqueTitle()));
        var otherTab = UniqueTitle();
        await UpdateAsync(created, created.Content with { Title = otherTab });

        var response = await _client.PutAsJsonAsync(
            $"/admin/series/{created.Id}", new UpdateSeriesRequest(created.Content with { Title = UniqueTitle() }, created.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(otherTab, (await GetAsync(created.Id)).Content.Title);
    }

    [Fact]
    public async Task Update_UnknownSeries_IsAFunctionalError()
    {
        var response = await _client.PutAsJsonAsync(
            $"/admin/series/{Guid.CreateVersion7()}", new UpdateSeriesRequest(Content(UniqueTitle()), 1));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task DeletionImpact_AnnouncesTheAlbumsBlockingAndTheContributionsDeletedWithTheSeries()
    {
        var author = await CreateAuthorAsync();
        var created = await CreateAsync(Content(UniqueTitle()) with
        {
            Contributions = [new ContributionContent(author.Id, ContributionRole.Scenarist), new ContributionContent(author.Id, ContributionRole.Colorist)],
        });
        await SeedAlbumsAsync(created.Id, 3);

        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/series/{created.Id}/deletion-impact");

        Assert.Equal([new ImpactCount(EntityKind.Album, 3)], impact!.BlockedBy);
        Assert.Empty(impact.AssociationsRemoved);
        Assert.Equal([new ImpactCount(EntityKind.Contribution, 2)], impact.DeletedWith);
        Assert.NotEmpty(impact.Fingerprint);
    }

    [Fact]
    public async Task DeletionImpact_OfAnUnknownSeries_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/series/{Guid.CreateVersion7()}/deletion-impact");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_OfASeriesWithoutAlbums_DeletesItsContributionsAndKeepsTheRecordsItReferenced()
    {
        var genre = await CreateGenreAsync();
        var author = await CreateAuthorAsync();
        var created = await CreateAsync(Content(UniqueTitle()) with
        {
            GenreIds = [genre.Id],
            Contributions = [new ContributionContent(author.Id, ContributionRole.Scenarist)],
        });
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/series/{created.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(created, impact!.Fingerprint));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await ProblemAssert.IsProblemAsync(
            await _client.GetAsync($"/admin/series/{created.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.Contributions.AnyAsync(c => c.SeriesId == created.Id));
        Assert.True(await context.Genres.AnyAsync(g => g.Id == genre.Id));
        Assert.True(await context.Authors.AnyAsync(a => a.Id == author.Id));
    }

    [Fact]
    public async Task Delete_OfASeriesContainingAlbums_IsRefusedWithTheBlockingImpact()
    {
        var created = await CreateAsync(Content(UniqueTitle()));
        await SeedAlbumsAsync(created.Id, 2);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/series/{created.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(created, impact!.Fingerprint));

        var problem = await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.DeletionBlockedByReferences);
        ProblemAssert.SameImpact(impact, ProblemAssert.ImpactOf(problem));
        Assert.Equal(created, await GetAsync(created.Id), SeriesFormComparer.Instance);
    }

    [Fact]
    public async Task Delete_FromAStaleVersion_IsAFunctionalError()
    {
        var created = await CreateAsync(Content(UniqueTitle()));
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/series/{created.Id}/deletion-impact");
        await UpdateAsync(created, created.Content with { Title = UniqueTitle() });

        var response = await _client.DeleteAsync(DeleteUri(created, impact!.Fingerprint));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    private static string UniqueTitle() => $"Série {Guid.NewGuid():N}";

    private static string DeleteUri(SeriesForm series, string fingerprint) =>
        $"/admin/series/{series.Id}?version={series.Version}&fingerprint={Uri.EscapeDataString(fingerprint)}";

    // The associations come back in a stable order, whichever order the form listed them in.
    private static List<Guid> Sorted(IReadOnlyList<Guid> ids) => ids.Order().ToList();

    private static List<ContributionContent> Sorted(IReadOnlyList<ContributionContent> contributions) =>
        contributions.OrderBy(c => c.Role).ThenBy(c => c.AuthorId).ToList();

    private static SeriesContent Content(string title) =>
        new(title, null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], []);

    private static SeriesContent WithTemplate(SeriesContent content, Guid? publisherId, Guid? collectionId) =>
        content with { EditionTemplate = content.EditionTemplate with { PublisherId = publisherId, PublisherCollectionId = collectionId } };

    private async Task<SeriesForm> GetAsync(Guid id) => (await _client.GetFromJsonAsync<SeriesForm>($"/admin/series/{id}"))!;

    private async Task<SeriesForm> CreateAsync(SeriesContent content)
    {
        var response = await _client.PostAsJsonAsync("/admin/series", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SeriesForm>())!;
    }

    private async Task<SeriesForm> UpdateAsync(SeriesForm series, SeriesContent content)
    {
        var response = await _client.PutAsJsonAsync($"/admin/series/{series.Id}", new UpdateSeriesRequest(content, series.Version));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SeriesForm>())!;
    }

    private Task<GenreForm> CreateGenreAsync() =>
        PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {Guid.NewGuid():N}"));

    private Task<UniverseForm> CreateUniverseAsync() =>
        PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest($"Univers {Guid.NewGuid():N}", null, null));

    private Task<PublisherForm> CreatePublisherAsync() =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest($"Éditeur {Guid.NewGuid():N}", null));

    private Task<PublisherCollectionForm> CreateCollectionAsync(PublisherForm publisher) =>
        PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest($"Collection {Guid.NewGuid():N}", publisher.Version));

    private Task<AuthorForm> CreateAuthorAsync() =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Auteur {Guid.NewGuid():N}", null, null, null, null));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }

    private async Task SeedAlbumsAsync(Guid seriesId, int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var series = await context.Series.SingleAsync(s => s.Id == seriesId);
        for (var i = 0; i < count; i++)
            context.Albums.Add(new Album($"Album {Guid.NewGuid():N}", series));
        await context.SaveChangesAsync();
    }

    // Records compare their lists by reference: these compare the content by value.
    private sealed class SeriesContentComparer : IEqualityComparer<SeriesContent>
    {
        public static readonly SeriesContentComparer Instance = new();

        public bool Equals(SeriesContent? x, SeriesContent? y) =>
            x is not null && y is not null
            && x with { GenreIds = [], UniverseIds = [], Contributions = [] } == y with { GenreIds = [], UniverseIds = [], Contributions = [] }
            && x.GenreIds.SequenceEqual(y.GenreIds)
            && x.UniverseIds.SequenceEqual(y.UniverseIds)
            && x.Contributions.SequenceEqual(y.Contributions);

        public int GetHashCode(SeriesContent obj) => obj.Title.GetHashCode();
    }

    private sealed class SeriesFormComparer : IEqualityComparer<SeriesForm>
    {
        public static readonly SeriesFormComparer Instance = new();

        public bool Equals(SeriesForm? x, SeriesForm? y) =>
            x is not null && y is not null
            && (x.Id, x.SortKey, x.Version) == (y.Id, y.SortKey, y.Version)
            && SeriesContentComparer.Instance.Equals(x.Content, y.Content);

        public int GetHashCode(SeriesForm obj) => obj.Id.GetHashCode();
    }
}
