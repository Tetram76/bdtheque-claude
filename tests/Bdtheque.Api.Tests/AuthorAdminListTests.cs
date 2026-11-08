using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the authors of the administration (<c>GET /admin/authors</c>, visuel/listes/liste-par-liste.md): what each row
/// presents, and its search and navigation, those of the list of the consultation, in the same order.
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its records with a marker of its
/// own, and narrows the list to it by searching it.
/// </remarks>
public sealed class AuthorAdminListTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public AuthorAdminListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Rows_CarryWhatTheListPresents()
    {
        var author = await PostAsync<AuthorForm>(
            "/admin/authors", new CreateAuthorRequest($"Gotlieb {_marker}", "Marcel", "Gotlib", null, "Française"));

        var row = Assert.Single(await ListAsync($"q={_marker}"));

        Assert.Equal(
            (author.Id, $"Gotlieb {_marker}", "Marcel", "Gotlib", "Française", author.Version),
            (row.Id, row.LastName, row.FirstName, row.Pseudonym, row.Nationality, row.Version));
        Assert.NotEqual(default, row.ModifiedAt);
    }

    [Fact]
    public async Task Rows_CountTheAlbumsCredited_OnceEach_AndTheRolesHeld_OnAlbumsAndSeries()
    {
        var author = await CreateAuthorAsync($"Gibrat {_marker}");
        var other = await CreateAuthorAsync($"Autre {_marker}");
        // Two roles on one album make one album credited; the roles held on a series count as well.
        var series = await CreateSeriesAsync($"Le Vol du corbeau {_marker}", [new(author.Id, ContributionRole.Colorist)]);
        await CreateAlbumAsync(series.Id, [new(author.Id, ContributionRole.Illustrator), new(author.Id, ContributionRole.Scenarist)]);
        await CreateAlbumAsync(null, [new(author.Id, ContributionRole.Illustrator)]);
        await CreateAlbumAsync(null, [new(other.Id, ContributionRole.Scenarist)]);
        var uncredited = await CreateAuthorAsync($"Inconnu {_marker}");

        var rows = await ListAsync($"q={_marker}");

        Assert.Equal(
            [
                (other.Id, 1, new[] { ContributionRole.Scenarist }),
                (author.Id, 2, new[] { ContributionRole.Scenarist, ContributionRole.Illustrator, ContributionRole.Colorist }),
                (uncredited.Id, 0, Array.Empty<ContributionRole>()),
            ],
            rows.Select(r => (r.Id, r.CreditedAlbumCount, r.Roles.ToArray())));
    }

    [Fact]
    public async Task List_FollowsTheSearchAndTheNavigationOfTheConsultation_InTheOrderOfTheSortKey()
    {
        // "Van Hamme Jean …" files under V, by last name; a pseudonym stands for the name.
        var vanHamme = await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Van Hamme {_marker}", "Jean", null, null, null));
        var moebius = await CreateAuthorAsync($"Moebius {_marker}");
        var herge = await PostAsync<AuthorForm>(
            "/admin/authors", new CreateAuthorRequest($"Remi {_marker}", "Georges", $"Hergé {_marker}", null, null));

        Assert.Equal([herge.Id, moebius.Id, vanHamme.Id], await IdsAsync($"q={_marker}"));
        Assert.Equal([vanHamme.Id], await IdsAsync($"q={_marker}&entry=V"));
        // The full name, as displayed, finds an author under a pseudonym, as the list of the consultation does.
        Assert.Equal([herge.Id], await IdsAsync($"q=georges remi {_marker}"));
        Assert.Equal(await CatalogIdsAsync($"q={_marker}"), await IdsAsync($"q={_marker}"));
    }

    [Fact]
    public async Task Entry_ThatIsNotOneOfTheNavigation_IsATechnicalError()
    {
        var response = await _client.GetAsync("/admin/authors?entry=AB");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private async Task<IReadOnlyList<AuthorAdminListItem>> ListAsync(string query) =>
        (await GetAsync<Page<AuthorAdminListItem>>($"/admin/authors?{query}")).Items;

    private async Task<IReadOnlyList<Guid>> IdsAsync(string query) => [.. (await ListAsync(query)).Select(r => r.Id)];

    private async Task<IReadOnlyList<Guid>> CatalogIdsAsync(string query) =>
        [.. (await GetAsync<Page<AuthorListItem>>($"/catalog/authors?{query}")).Items.Select(a => a.Id)];

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<AuthorForm> CreateAuthorAsync(string pseudonym) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(null, null, pseudonym, null, null));

    private Task<SeriesForm> CreateSeriesAsync(string title, ContributionContent[] contributions) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, null, null, false, false, false, null, null, NoTemplate, [], [], contributions));

    private Task<AlbumForm> CreateAlbumAsync(Guid? seriesId, ContributionContent[] contributions) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            seriesId is null ? $"Album {_marker}" : null, null, seriesId, AlbumType.Regular, false, seriesId is null ? null : 1, null, null,
            null, null, null, null, null, [], [], contributions));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
