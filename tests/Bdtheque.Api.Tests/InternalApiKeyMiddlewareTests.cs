using System.Net;
using Bdtheque.Api.Security;

namespace Bdtheque.Api.Tests;

public sealed class InternalApiKeyMiddlewareTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public InternalApiKeyMiddlewareTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Request_WithoutInternalKey_IsRejected()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithCorrectInternalKey_IsNotRejectedByMiddleware()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiKeyOptions.HeaderName, ApiWebApplicationFactory.InternalApiKey);

        var response = await client.GetAsync("/anything");

        // No route exists at this stage: the 404 proves the middleware let the request
        // through to routing, unlike the 401 returned without the key.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
