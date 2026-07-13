using System.Net;
using Bdtheque.Api.Security;

namespace Bdtheque.Api.Tests;

public sealed class InternalApiKeyMiddlewareTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public InternalApiKeyMiddlewareTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Requete_SansCleInterne_EstRejetee()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/n-importe-quoi");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Requete_AvecCleInterneCorrecte_NestPasRejeteeParLeMiddleware()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiKeyOptions.HeaderName, ApiWebApplicationFactory.InternalApiKey);

        var response = await client.GetAsync("/n-importe-quoi");

        // Aucune route n'existe à ce stade : le 404 prouve que le middleware a laissé
        // passer la requête jusqu'au routage, contrairement au 401 sans la clé.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
