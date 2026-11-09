using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the universes of the administration (<c>GET /admin/universes</c>, visuel/listes/liste-par-liste.md): its order,
/// that of the hierarchy, what each row presents, its search on the name and its pages.
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its records with a marker of its
/// own, and narrows the list to it by searching it.
/// </remarks>
public sealed class UniverseAdminListTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public UniverseAdminListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task List_FollowsTheHierarchy_EachUniverseFollowedByItsSubUniverses_SiblingsByName()
    {
        var tree = await CreateTreeAsync();

        var rows = await ListAsync($"q={_marker}");

        Assert.Equal(
            [tree.Cities, tree.Herge, tree.Tintin, tree.Moulinsart, tree.Pratt, tree.Antilles.Parent, tree.Antilles.Self, tree.Scorpions],
            rows.Select(r => r.Id));
    }

    [Fact]
    public async Task Rows_CarryTheirAncestors_FromTheTop_AndTheirSubUniversesAtEveryLevel()
    {
        var tree = await CreateTreeAsync();

        var rows = (await ListAsync($"q={_marker}")).ToDictionary(r => r.Id);

        Assert.Equal([tree.Pratt, tree.Antilles.Parent], rows[tree.Antilles.Self].Ancestors.Select(a => a.Id));
        Assert.Empty(rows[tree.Pratt].Ancestors);
        Assert.Equal(
            [(tree.Herge, 2), (tree.Tintin, 1), (tree.Moulinsart, 0), (tree.Pratt, 3), (tree.Antilles.Parent, 1)],
            new[] { tree.Herge, tree.Tintin, tree.Moulinsart, tree.Pratt, tree.Antilles.Parent }.Select(id => (id, rows[id].SubUniverseCount)));
        var corto = rows[tree.Antilles.Parent];
        var form = await GetAsync<UniverseForm>($"/admin/universes/{corto.Id}");
        Assert.Equal((form.Name, form.ParentId, form.Version, form.ModifiedAt), (corto.Name, corto.ParentId, corto.Version, corto.ModifiedAt));
    }

    [Fact]
    public async Task Rows_CountTheAlbumsAndSeries_SubUniversesIncluded_AsTheirListsFilteredOnTheUniverseFindThem()
    {
        var tree = await CreateTreeAsync();
        // Attached to a universe and to its sub-universe: counted once in each, and once in their ancestor.
        await CreateAlbumAsync($"Album {_marker}", null, [tree.Antilles.Parent, tree.Antilles.Self]);
        await CreateAlbumAsync($"Album des Antilles {_marker}", null, [tree.Antilles.Self]);
        // Through its series: the universes of an album in a series are those of the album and of the series
        // (fonctionnel/regles-metier/genres-et-univers-d-un-album.md).
        var series = await CreateSeriesAsync($"Série {_marker}", [tree.Scorpions]);
        await CreateAlbumAsync($"Tome {_marker}", series.Id, []);

        var rows = (await ListAsync($"q={_marker}")).ToDictionary(r => r.Id);

        Assert.Equal(
            [(2, 0), (2, 0), (1, 1), (3, 1), (0, 0)],
            new[] { tree.Antilles.Parent, tree.Antilles.Self, tree.Scorpions, tree.Pratt, tree.Herge }
                .Select(id => (rows[id].AlbumCount, rows[id].SeriesCount)));
        Assert.Equal(3, (await GetAsync<AlbumAdminList>($"/admin/albums?universeId={tree.Pratt}")).Albums.TotalCount);
        Assert.Equal(1, (await GetAsync<SeriesAdminList>($"/admin/series?universeId={tree.Pratt}")).Series.TotalCount);
    }

    [Fact]
    public async Task List_SearchesTheName_InAnyCaseAndAccents_KeepingTheOrderOfTheHierarchy()
    {
        var tree = await CreateTreeAsync();

        Assert.Equal([tree.Cities], (await ListAsync($"q=CITES OBSCURES {_marker}")).Select(r => r.Id));
        // Its ancestors are not found: the row still names them.
        var found = Assert.Single(await ListAsync($"q=Antilles {_marker}"));
        Assert.Equal([tree.Pratt, tree.Antilles.Parent], found.Ancestors.Select(a => a.Id));
    }

    [Fact]
    public async Task List_IsPaged_InTheOrderOfTheHierarchy()
    {
        var tree = await CreateTreeAsync();

        var page = await GetAsync<Page<UniverseAdminListItem>>($"/admin/universes?q={_marker}&page=2&pageSize=3");

        Assert.Equal((8, 2, 3), (page.TotalCount, page.Number, page.Size));
        Assert.Equal([tree.Moulinsart, tree.Pratt, tree.Antilles.Parent], page.Items.Select(r => r.Id));
    }

    // Les Cités obscures; Univers Hergé › Tintin › Moulinsart; Univers Hugo Pratt › Corto Maltese › Les Antilles, and
    // Univers Hugo Pratt › Les Scorpions du désert. Created in another order than that of the list.
    private async Task<Tree> CreateTreeAsync()
    {
        var pratt = await CreateUniverseAsync("Univers Hugo Pratt");
        var scorpions = await CreateUniverseAsync("Les Scorpions du désert", pratt);
        var corto = await CreateUniverseAsync("Corto Maltese", pratt);
        var antilles = await CreateUniverseAsync("Les Antilles", corto);
        var herge = await CreateUniverseAsync("Univers Hergé");
        var tintin = await CreateUniverseAsync("Tintin", herge);
        var moulinsart = await CreateUniverseAsync("Moulinsart", tintin);
        var cities = await CreateUniverseAsync("Les Cités obscures");
        return new Tree(cities, herge, tintin, moulinsart, pratt, (antilles, corto), scorpions);
    }

    private sealed record Tree(Guid Cities, Guid Herge, Guid Tintin, Guid Moulinsart, Guid Pratt, (Guid Self, Guid Parent) Antilles, Guid Scorpions);

    private async Task<IReadOnlyList<UniverseAdminListItem>> ListAsync(string query) =>
        (await GetAsync<Page<UniverseAdminListItem>>($"/admin/universes?{query}")).Items;

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private async Task<Guid> CreateUniverseAsync(string name, Guid? parentId = null) =>
        (await PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest($"{name} {_marker}", null, parentId))).Id;

    private Task<SeriesForm> CreateSeriesAsync(string title, IReadOnlyList<Guid> universeIds) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(title, null, null, null, false, false, false, null, null, NoTemplate, [], universeIds, []));

    private Task<AlbumForm> CreateAlbumAsync(string title, Guid? seriesId, IReadOnlyList<Guid> universeIds) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], universeIds, []));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
