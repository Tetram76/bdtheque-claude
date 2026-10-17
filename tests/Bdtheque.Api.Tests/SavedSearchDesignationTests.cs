using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Bdtheque.Contracts.Search.SearchComparison;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The records a saved search designates, by a criterion or by the default value of a variable: their
/// deletion is refused while a saved search designates them, or one of the records deleted with them
/// (fonctionnel.md § Suppression des entités, line « Toute fiche »).
/// </summary>
public sealed class SavedSearchDesignationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public SavedSearchDesignationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    // The albums having a given genre: the genre is designated deep in the criteria, through a link.
    private static SearchLink AlbumsOfGenre(Guid genreId) =>
        new("genres", new SearchCriterion("record", Is, [genreId.ToString()]), new SearchQuantifier(SearchQuantifierKind.Any));

    [Fact]
    public async Task DeleteGenre_DesignatedByASavedSearch_IsRefused()
    {
        var genre = await CreateGenreAsync();
        await CreateSearchAsync(EntityKind.Album, AlbumsOfGenre(genre.Id));

        var impact = await ImpactAsync($"/admin/genres/{genre.Id}");
        var response = await _client.DeleteAsync($"/admin/genres/{genre.Id}?version={genre.Version}&fingerprint={Uri.EscapeDataString(impact.Fingerprint)}");

        Assert.Equal([new ImpactCount(EntityKind.SavedSearch, 1)], impact.BlockedBy);
        var problem = await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.DeletionBlockedByReferences);
        ProblemAssert.SameImpact(impact, ProblemAssert.ImpactOf(problem));
    }

    [Fact]
    public async Task DeleteGenre_DesignatedByTheDefaultValueOfAVariable_IsRefused()
    {
        // The default value of a variable is the constant it replaces: the same designation.
        var genre = await CreateGenreAsync();
        await CreateSearchAsync(EntityKind.Album, AlbumsOfGenre(genre.Id), [new SearchVariableContent("Genre", "/condition/values/0")]);

        var impact = await ImpactAsync($"/admin/genres/{genre.Id}");

        Assert.Equal([new ImpactCount(EntityKind.SavedSearch, 1)], impact.BlockedBy);
    }

    [Fact]
    public async Task DeleteGenre_NoLongerDesignated_IsAllowed()
    {
        var genre = await CreateGenreAsync();
        var other = await CreateGenreAsync();
        var search = await CreateSearchAsync(EntityKind.Album, AlbumsOfGenre(genre.Id));
        var deleted = await CreateSearchAsync(EntityKind.Album, AlbumsOfGenre(other.Id));

        // The search now designates the other genre, and the one designating it is deleted.
        (await _client.PutAsJsonAsync(
            $"/admin/saved-searches/{search.Id}",
            new UpdateSavedSearchRequest(search.Content with { Criteria = AlbumsOfGenre(other.Id) }, search.Version))).EnsureSuccessStatusCode();
        await DeleteAsync($"/admin/saved-searches/{deleted.Id}", deleted.Version);

        Assert.Empty((await ImpactAsync($"/admin/genres/{genre.Id}")).BlockedBy);
        Assert.Equal([new ImpactCount(EntityKind.SavedSearch, 1)], (await ImpactAsync($"/admin/genres/{other.Id}")).BlockedBy);
        await DeleteAsync($"/admin/genres/{genre.Id}", genre.Version);
    }

    [Fact]
    public async Task DeleteAlbum_WhoseEditionIsDesignated_IsRefused()
    {
        // A record deleted with the album, the edition, is designated: the album cannot be deleted either.
        var album = await PostAsync<AlbumForm>("/admin/albums", AlbumContent([]));
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest($"Éditeur {_marker}", null));
        var edition = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(OwnedEdition(publisher.Id), album.Version));
        await CreateSearchAsync(EntityKind.Edition, new SearchCriterion("record", IsOneOf, [edition.Id.ToString()]));

        var impact = await ImpactAsync($"/admin/albums/{album.Id}");
        var response = await _client.DeleteAsync(
            $"/admin/albums/{album.Id}?version={edition.AlbumVersion}&fingerprint={Uri.EscapeDataString(impact.Fingerprint)}");

        Assert.Equal([new ImpactCount(EntityKind.SavedSearch, 1)], impact.BlockedBy);
        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.DeletionBlockedByReferences);
        Assert.Equal([new ImpactCount(EntityKind.SavedSearch, 1)], (await ImpactAsync($"/admin/albums/{album.Id}/editions/{edition.Id}")).BlockedBy);
    }

    [Fact]
    public async Task RemoveContribution_DesignatedByASavedSearch_IsABusinessError()
    {
        // Removed from the form of its album, the contribution is deleted without any confirmation: refused all the same.
        var author = await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Auteur {_marker}", null, null, null, null));
        var album = await PostAsync<AlbumForm>("/admin/albums", AlbumContent([new ContributionContent(author.Id, ContributionRole.Scenarist)]));
        await CreateSearchAsync(EntityKind.Contribution, new SearchCriterion("record", Is, [(await ContributionOfAsync(album.Id)).ToString()]));

        var response = await _client.PutAsJsonAsync($"/admin/albums/{album.Id}", new UpdateAlbumRequest(AlbumContent([]), album.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SavedSearchDesignatedRecordRemoved);
    }

    [Fact]
    public async Task Create_DesignatingAPurchaseIntent_IsABusinessError()
    {
        // An intent is only the wish to acquire another record (fonctionnel.md § Recherches enregistrées).
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(
            Name(), EntityKind.PurchaseIntent, new SearchCriterion("record", Is, [Guid.CreateVersion7().ToString()]), []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SavedSearchPurchaseIntentDesignated);
    }

    [Fact]
    public async Task Create_DesignatingARecordDeletedInTheMeantime_IsAFunctionalError()
    {
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(
            Name(), EntityKind.Album, AlbumsOfGenre(Guid.CreateVersion7()), []));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Create_SearchingTheSavedSearches_IsATechnicalError()
    {
        // The saved searches are counted by the impact of a deletion, never searched.
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(
            Name(), EntityKind.SavedSearch, new SearchGroup(SearchOperator.And, []), []));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private string Name() => $"Recherche {_marker} {Guid.NewGuid():N}";

    private Task<GenreForm> CreateGenreAsync() => PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker} {Guid.NewGuid():N}"));

    private Task<SavedSearchForm> CreateSearchAsync(EntityKind entity, SearchCondition criteria, IReadOnlyList<SearchVariableContent>? variables = null) =>
        PostAsync<SavedSearchForm>("/admin/saved-searches", new SavedSearchContent(Name(), entity, criteria, variables ?? []));

    private AlbumContent AlbumContent(IReadOnlyList<ContributionContent> contributions) =>
        new($"Album {_marker}", null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], contributions);

    private static EditionContent OwnedEdition(Guid publisherId) =>
        new(publisherId, null, null, null, null, null, null, null, null, null, false, true, null,
            AcquisitionMode.Gift, false, null, null, null, false, null, null, null, null);

    private async Task<Guid> ContributionOfAsync(Guid albumId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        return await context.Contributions.Where(c => c.AlbumId == albumId).Select(c => c.Id).SingleAsync();
    }

    private async Task<DeletionImpact> ImpactAsync(string uri) => (await _client.GetFromJsonAsync<DeletionImpact>($"{uri}/deletion-impact"))!;

    private async Task DeleteAsync(string uri, uint version)
    {
        var impact = await ImpactAsync(uri);
        (await _client.DeleteAsync($"{uri}?version={version}&fingerprint={Uri.EscapeDataString(impact.Fingerprint)}")).EnsureSuccessStatusCode();
    }

    private async Task<T> PostAsync<T>(string uri, object content)
    {
        var response = await _client.PostAsJsonAsync(uri, content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
