using System.Net;
using System.Text.Json;
using Bdtheque.Contracts.Errors;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Errors the framework or a middleware emits without an exception (choix-implementation.md §
/// Erreurs métier, fonctionnelles et techniques): no user input can produce them, the frontend
/// building every request, so they are technical — whatever their HTTP status.
/// </summary>
public sealed class ErrorResponseTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ErrorResponseTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task UnknownRoute_IsATechnicalProblem()
    {
        var response = await _factory.CreateApiClient().GetAsync("/admin/no-such-resource");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Technical);
    }

    [Fact]
    public async Task MissingInternalKey_IsATechnicalProblem()
    {
        var response = await _factory.CreateClient().GetAsync("/admin/no-such-resource");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Unauthorized, ProblemTypes.Technical);
    }

    [Fact]
    public async Task UnhealthyHealthCheck_KeepsItsOwnResponseFormat()
    {
        // /health is polled by the orchestrator, never by the frontend: its 503 is not a ProblemDetails.
        await using var unhealthy = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddHealthChecks().AddCheck("failing", () => HealthCheckResult.Unhealthy())));

        var response = await unhealthy.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var report = JsonSerializer.Deserialize<Dictionary<string, string>>(await response.Content.ReadAsStringAsync());
        Assert.Equal(nameof(HealthStatus.Unhealthy), report!["failing"]);
    }
}
