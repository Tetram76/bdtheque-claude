using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The sort key of a title as the domain computes it (<c>/admin/sort-keys/title</c>), which an entry form shows while the
/// title is typed (fonctionnel/presentation/tri-et-navigation-par-initiale.md#calcul-et-stockage-de-la-clé-de-tri).
/// </summary>
public sealed class TitleSortKeyEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateApiClient();

    [Theory]
    [InlineData(" Le Lotus bleu ", "Lotus bleu [Le]")]
    [InlineData("L'Épervier", "Épervier [L']")]
    [InlineData("Tintin", "Tintin")]
    public async Task OfATitle_IsTheKeyTheDomainStores(string title, string sortKey)
    {
        var key = await _client.GetFromJsonAsync<TitleSortKey>($"/admin/sort-keys/title?title={Uri.EscapeDataString(title)}");

        Assert.Equal(sortKey, key!.SortKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OfNoTitle_IsNone(string title)
    {
        var key = await _client.GetFromJsonAsync<TitleSortKey>($"/admin/sort-keys/title?title={Uri.EscapeDataString(title)}");

        Assert.Null(key!.SortKey);
    }
}
