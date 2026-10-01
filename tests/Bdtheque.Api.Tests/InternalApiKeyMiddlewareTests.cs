using System.Net;

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
        var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/anything");

        // No route exists at this stage: the 404 proves the middleware let the request
        // through to routing, unlike the 401 returned without the key.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    public async Task Request_ToApiDocumentationPath_IsNotRejectedByMiddleware(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        // These routes are only mapped in Development (see Program.cs), so the 404 here
        // still proves the middleware let the request through without the shared key,
        // unlike the 401 returned for a regular path.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
