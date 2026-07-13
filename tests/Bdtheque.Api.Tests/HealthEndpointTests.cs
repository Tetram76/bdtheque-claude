using System.Net;

namespace Bdtheque.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public HealthEndpointTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetHealth_RetourneOk_SansClePartagee()
    {
        // /health doit rester accessible aux orchestrateurs (Docker, Synology) sans
        // exiger le secret interne (cf. InternalApiKeyMiddleware).
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Statut {response.StatusCode} : {body}");
    }
}
