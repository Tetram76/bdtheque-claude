using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Common;
using static Bdtheque.Contracts.Search.SearchComparison;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The saved searches (fonctionnel.md § Recherches enregistrées): their administration
/// (<c>/admin/saved-searches</c>), and their list, criteria and replay in the consultation
/// (<c>/catalog/saved-searches</c>).
/// </summary>
public sealed class SavedSearchEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public SavedSearchEndpointsTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    // The albums whose title contains the marker of the test, and a given text: the variable "Titre"
    // replaces that text.
    private SearchGroup AlbumsTitled(string text) =>
        new(SearchOperator.And, [new SearchCriterion("title", Contains, [_marker]), new SearchCriterion("title", Contains, [text])]);

    private const string TitleLocation = "/conditions/1/values/0";

    [Fact]
    public async Task Create_ReturnsTheSearchAsStoredWithItsVersion()
    {
        var content = new SavedSearchContent($"  {Name()}  ", EntityKind.Album, AlbumsTitled("Tintin"), [new SearchVariableContent(" Titre ", TitleLocation)]);

        var response = await _client.PostAsJsonAsync("/admin/saved-searches", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<SavedSearchForm>())!;
        Assert.Equal($"/admin/saved-searches/{created.Id}", response.Headers.Location?.OriginalString);
        AssertSameJson(
            content with { Name = content.Name.Trim(), Variables = [new SearchVariableContent("Titre", TitleLocation)] },
            created.Content);
        AssertSameJson(created, await _client.GetFromJsonAsync<SavedSearchForm>($"/admin/saved-searches/{created.Id}"));
    }

    [Fact]
    public async Task Create_WithBlankName_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(" ", EntityKind.Album, AlbumsTitled("Tintin"), []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SavedSearchNameRequired);
    }

    [Fact]
    public async Task Create_WithNameDifferingOnlyByCaseAndAccents_IsABusinessError()
    {
        await CreateAsync($"Éditions {_marker}", AlbumsTitled("Tintin"));

        var response = await _client.PostAsJsonAsync(
            "/admin/saved-searches", new SavedSearchContent($"EDITIONS {_marker}", EntityKind.Album, AlbumsTitled("Tintin"), []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SavedSearchNameAlreadyUsed);
    }

    [Fact]
    public async Task Create_WithTwoVariablesNamedAlike_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(
            Name(), EntityKind.Album, AlbumsTitled("Tintin"),
            [new SearchVariableContent("Marqueur", "/conditions/0/values/0"), new SearchVariableContent("marqueur", TitleLocation)]));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.SavedSearchVariableNameAlreadyUsed);
    }

    [Fact]
    public async Task Create_WithAValueLeftEmpty_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync(
            "/admin/saved-searches", new SavedSearchContent(Name(), EntityKind.Album, AlbumsTitled(""), []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AdvancedSearchValueMissing);
    }

    [Fact]
    public async Task Create_WithCriteriaTheSchemaDoesNotDescribe_IsATechnicalError()
    {
        // Valid for the albums, but the series have no volume number.
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(
            Name(), EntityKind.Series, new SearchCriterion("volumeNumber", EqualTo, ["1"]), []));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    [Fact]
    public async Task Create_WithAVariableNotOnAConstant_IsATechnicalError()
    {
        var response = await _client.PostAsJsonAsync("/admin/saved-searches", new SavedSearchContent(
            Name(), EntityKind.Album, AlbumsTitled("Tintin"), [new SearchVariableContent("Champ", "/conditions/1/field")]));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    [Fact]
    public async Task Update_AtTheCurrentVersion_RenamesAndReplacesItsSearch()
    {
        var search = await CreateAsync(Name(), AlbumsTitled("Tintin"), [new SearchVariableContent("Titre", TitleLocation)]);
        var content = new SavedSearchContent(Name(), EntityKind.Series, new SearchCriterion("title", StartsWith, ["A"]), []);

        var response = await _client.PutAsJsonAsync($"/admin/saved-searches/{search.Id}", new UpdateSavedSearchRequest(content, search.Version));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<SavedSearchForm>())!;
        AssertSameJson(content, updated.Content);
        Assert.NotEqual(search.Version, updated.Version);
        AssertSameJson(updated, await _client.GetFromJsonAsync<SavedSearchForm>($"/admin/saved-searches/{search.Id}"));
    }

    [Fact]
    public async Task Update_FromAStaleVersion_IsAFunctionalError()
    {
        var search = await CreateAsync(Name(), AlbumsTitled("Tintin"));
        await _client.PutAsJsonAsync($"/admin/saved-searches/{search.Id}", new UpdateSavedSearchRequest(search.Content with { Name = Name() }, search.Version));

        var response = await _client.PutAsJsonAsync(
            $"/admin/saved-searches/{search.Id}", new UpdateSavedSearchRequest(search.Content with { Name = Name() }, search.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Get_UnknownSearch_IsAFunctionalError()
    {
        await ProblemAssert.IsProblemAsync(
            await _client.GetAsync($"/admin/saved-searches/{Guid.CreateVersion7()}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        await ProblemAssert.IsProblemAsync(
            await _client.GetAsync($"/catalog/saved-searches/{Guid.CreateVersion7()}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
        await ProblemAssert.IsProblemAsync(
            await _client.PostAsJsonAsync($"/catalog/saved-searches/{Guid.CreateVersion7()}/results", new ReplaySavedSearchRequest(null)),
            HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_AsConfirmed_HasNoOtherEffect()
    {
        var search = await CreateAsync(Name(), AlbumsTitled("Tintin"));
        var impact = (await _client.GetFromJsonAsync<DeletionImpact>($"/admin/saved-searches/{search.Id}/deletion-impact"))!;

        var response = await _client.DeleteAsync(
            $"/admin/saved-searches/{search.Id}?version={search.Version}&fingerprint={Uri.EscapeDataString(impact.Fingerprint)}");

        Assert.Empty(impact.BlockedBy);
        Assert.Empty(impact.AssociationsRemoved);
        Assert.Empty(impact.DeletedWith);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await ProblemAssert.IsProblemAsync(
            await _client.GetAsync($"/admin/saved-searches/{search.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task List_PresentsTheSearchesByName()
    {
        var b = await CreateAsync($"B {_marker}", AlbumsTitled("Tintin"));
        var a = await CreateAsync($"a {_marker}", new SearchCriterion("title", StartsWith, ["A"]), entity: EntityKind.Series);

        var page = (await _client.GetFromJsonAsync<Page<SavedSearchListItem>>($"/catalog/saved-searches?q={_marker}"))!;

        Assert.Equal(
            [new SavedSearchListItem(a.Id, a.Content.Name, EntityKind.Series), new SavedSearchListItem(b.Id, b.Content.Name, EntityKind.Album)],
            page.Items);
    }

    [Fact]
    public async Task Detail_GivesTheCriteriaAndTheVariablesWithTheirDefaultValues()
    {
        // Its criteria preload the form of a new search, with no link kept to the saved one.
        var search = await CreateAsync(Name(), AlbumsTitled("Tintin"), [new SearchVariableContent("Titre", TitleLocation)]);

        var detail = (await _client.GetFromJsonAsync<SavedSearchDetail>($"/catalog/saved-searches/{search.Id}"))!;

        Assert.Equal((search.Id, search.Content.Name, EntityKind.Album), (detail.Id, detail.Name, detail.Entity));
        AssertSameJson(search.Content.Criteria, detail.Criteria);
        Assert.Equal([new SavedSearchVariable("Titre", TitleLocation, "Tintin")], detail.Variables);
    }

    [Fact]
    public async Task Replay_UsesTheValuesGiven_OrTheDefaultValues()
    {
        var tintin = await CreateAlbumAsync($"Tintin {_marker}");
        var asterix = await CreateAlbumAsync($"Astérix {_marker}");
        var search = await CreateAsync(Name(), AlbumsTitled("Tintin"), [new SearchVariableContent("Titre", TitleLocation)]);

        var byDefault = await ReplayAsync<AlbumSummary>(search.Id, null);
        var given = await ReplayAsync<AlbumSummary>(search.Id, new Dictionary<string, string> { ["Titre"] = "asterix" });

        Assert.Equal([tintin.Id], byDefault.Select(a => a.Id));
        Assert.Equal([asterix.Id], given.Select(a => a.Id));
    }

    [Fact]
    public async Task Replay_ReturnsTheEntitiesOfTheSearch()
    {
        var search = await CreateAsync(Name(), new SearchCriterion("title", StartsWith, [$"Série {_marker}"]), entity: EntityKind.Series);
        var series = await PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            $"Série {_marker}", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], []));

        var results = await ReplayAsync<SeriesListItem>(search.Id, null);

        Assert.Equal([new SeriesListItem(series.Id, $"Série {_marker}")], results);
    }

    [Fact]
    public async Task Replay_WithAValueLeftEmpty_IsABusinessError()
    {
        var search = await CreateAsync(Name(), AlbumsTitled("Tintin"), [new SearchVariableContent("Titre", TitleLocation)]);

        var response = await _client.PostAsJsonAsync(
            $"/catalog/saved-searches/{search.Id}/results", new ReplaySavedSearchRequest(new Dictionary<string, string> { ["Titre"] = "" }));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AdvancedSearchValueMissing);
    }

    [Fact]
    public async Task Replay_WithAVariableTheSearchDoesNotHave_IsATechnicalError()
    {
        var search = await CreateAsync(Name(), AlbumsTitled("Tintin"), [new SearchVariableContent("Titre", TitleLocation)]);

        var response = await _client.PostAsJsonAsync(
            $"/catalog/saved-searches/{search.Id}/results", new ReplaySavedSearchRequest(new Dictionary<string, string> { ["Auteur"] = "Hergé" }));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    [Fact]
    public async Task ChangeLog_RecordsTheCriteriaAndTheVariablesOfAModification()
    {
        var search = await CreateAsync(Name(), AlbumsTitled("Tintin"), [new SearchVariableContent("Titre", TitleLocation)]);
        var content = search.Content with { Criteria = AlbumsTitled("Astérix") };
        (await _client.PutAsJsonAsync($"/admin/saved-searches/{search.Id}", new UpdateSavedSearchRequest(content, search.Version)))
            .EnsureSuccessStatusCode();

        var log = (await _client.GetFromJsonAsync<Page<ChangeLogEntryItem>>("/admin/change-log?pageSize=100"))!;

        var entry = log.Items.First(e => e.RecordId == search.Id);
        Assert.Equal((RecordType.SavedSearch, ChangeLogOperation.Modification, (RecordLabel)new NameLabel(search.Content.Name)),
            (entry.RecordType, entry.Operation, entry.Label));
        Assert.Equal([ChangeLogAttribute.SearchCriteria, ChangeLogAttribute.SearchVariables], entry.Changes.Select(c => c.Attribute));
        AssertSameJson(new SearchCriteriaValue(content.Criteria), entry.Changes[0].After);
        Assert.Equal(
            [new SavedSearchVariable("Titre", TitleLocation, "Astérix")],
            Assert.IsType<SearchVariablesValue>(entry.Changes[1].After).Variables);
    }

    private string Name() => $"Recherche {_marker} {Guid.NewGuid():N}";

    private static void AssertSameJson<T>(T expected, T actual) =>
        Assert.Equal(JsonSerializer.Serialize(expected, JsonSerializerOptions.Web), JsonSerializer.Serialize(actual, JsonSerializerOptions.Web));

    private Task<SavedSearchForm> CreateAsync(
        string name, SearchCondition criteria, IReadOnlyList<SearchVariableContent>? variables = null, EntityKind entity = EntityKind.Album) =>
        PostAsync<SavedSearchForm>("/admin/saved-searches", new SavedSearchContent(name, entity, criteria, variables ?? []));

    private Task<AlbumForm> CreateAlbumAsync(string title) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

    private async Task<IReadOnlyList<T>> ReplayAsync<T>(Guid id, IReadOnlyDictionary<string, string>? values)
    {
        var response = await _client.PostAsJsonAsync($"/catalog/saved-searches/{id}/results?pageSize=100", new ReplaySavedSearchRequest(values));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Page<T>>())!.Items;
    }

    private async Task<T> PostAsync<T>(string uri, object content)
    {
        var response = await _client.PostAsJsonAsync(uri, content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
