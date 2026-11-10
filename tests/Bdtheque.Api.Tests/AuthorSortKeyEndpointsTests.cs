using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The sort key of an author as the domain computes it from its names (<c>/admin/sort-keys/author</c>), with the identifier it
/// derives from and its entry, which the entry form of an author presents while the names are typed
/// (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes).
/// </summary>
public sealed class AuthorSortKeyEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateApiClient();

    [Theory]
    [InlineData(" Van Hamme ", "Jean", "", "Jean Van Hamme", "Van Hamme Jean", "V")]
    [InlineData("Remi", "Georges", "Hergé", "Hergé", "Hergé", "H")]
    [InlineData("", "", "Moebius", "Moebius", "Moebius", "M")]
    public async Task OfNames_IsTheKeyTheDomainStores_WithItsIdentifierAndItsEntry(
        string lastName, string firstName, string pseudonym, string displayName, string sortKey, string entry)
    {
        var key = await _client.GetFromJsonAsync<AuthorSortKey>(
            $"/admin/sort-keys/author?lastName={Uri.EscapeDataString(lastName)}&firstName={Uri.EscapeDataString(firstName)}" +
            $"&pseudonym={Uri.EscapeDataString(pseudonym)}");

        Assert.Equal(new AuthorSortKey(displayName, sortKey, entry), key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?firstName=Jean&lastName=%20")]
    public async Task OfNeitherALastNameNorAPseudonym_IsNone(string query)
    {
        var key = await _client.GetFromJsonAsync<AuthorSortKey>($"/admin/sort-keys/author{query}");

        Assert.Equal(new AuthorSortKey(null, null, null), key);
    }
}
