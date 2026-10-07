using System.Globalization;
using Bdtheque.Api.Security;
using Bdtheque.Infrastructure;
using Bdtheque.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Hosts the API exactly as the <c>api</c> container runs it, against an empty PostgreSQL
/// database of its own: startup therefore goes through the same migration step as a first
/// deployment (see <see cref="PostgreSqlTestServer"/>).
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string InternalApiKey = "test-internal-api-key";

    /// <summary>Heaviest visual accepted by the API under test, kept small so that a test can exceed it cheaply.</summary>
    public const long MaxVisualFileSizeBytes = 256 * 1024;

    private string _connectionString = null!;

    /// <summary>The visuals volume of this API instance, an empty temporary directory of its own.</summary>
    public string VisualsRoot { get; } = Path.Combine(Path.GetTempPath(), $"bdtheque-visuals-{Guid.NewGuid():N}");

    public async Task InitializeAsync() =>
        _connectionString = await PostgreSqlTestServer.CreateEmptyDatabaseAsync();

    /// <summary>A client calling the API as <c>frontend</c> does, with the internal shared key.</summary>
    public HttpClient CreateApiClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiKeyOptions.HeaderName, InternalApiKey);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production, not the factory's default Development: the tests must exercise what is
        // deployed (e.g. the API documentation endpoints are not mapped there).
        builder.UseEnvironment("Production");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Bdtheque"] = _connectionString,
                ["InternalApiKey:Key"] = InternalApiKey,
                ["Visuals:RootPath"] = VisualsRoot,
                ["Visuals:MaxFileSizeBytes"] = MaxVisualFileSizeBytes.ToString(CultureInfo.InvariantCulture),
            }));

        builder.ConfigureTestServices(services =>
            services.ConfigureDbContext<BdthequeDbContext>(options => StrictQueryWarnings.Apply(options)));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        PostgreSqlTestServer.ReleaseConnections(_connectionString);
        if (Directory.Exists(VisualsRoot))
            Directory.Delete(VisualsRoot, recursive: true);
    }
}
