using System.Net;

namespace Bdtheque.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public HealthEndpointTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetHealth_ReturnsOk_WithoutSharedKey()
    {
        // /health must stay reachable by orchestrators (Docker, Synology) without
        // requiring the internal secret (see InternalApiKeyMiddleware).
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Status {response.StatusCode}: {body}");
    }
}
