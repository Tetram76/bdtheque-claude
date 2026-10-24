extern alias frontend;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FrontendProgram = frontend::Program;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The whole application, as a browser reaches it: <c>frontend</c> listens on a real port, which a
/// browser needs, and calls <c>api</c>, itself on its own PostgreSQL database
/// (<see cref="ApiHostFactory"/>), the way the Docker network carries its calls.
/// </summary>
public sealed class ApplicationHost : IAsyncLifetime
{
    private readonly ApiHostFactory _api = new();
    private FrontendFactory? _frontend;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;

    /// <summary>Address of <c>frontend</c>, the only container a browser talks to.</summary>
    public Uri BaseAddress { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _api.InitializeAsync();

        _frontend = new FrontendFactory(_api);
        _frontend.UseKestrel(0);
        _frontend.StartServer();
        BaseAddress = new Uri(_frontend.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single());

        // A no-op once the browser is installed: a local run needs no manual step before it.
        if (Microsoft.Playwright.Program.Main(["install", "chromium"]) != 0)
            throw new InvalidOperationException("The Chromium browser of Playwright could not be installed.");
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync();
    }

    /// <summary>A page of a browser context of its own: no cookie nor history shared between tests.</summary>
    public async Task<IPage> NewPageAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseAddress.ToString() });
        return await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.DisposeAsync();
        _playwright?.Dispose();
        if (_frontend is not null)
            await _frontend.DisposeAsync();
        await _api.DisposeAsync();
    }

    private sealed class FrontendFactory(ApiHostFactory api) : WebApplicationFactory<FrontendProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            // Outside Development, the static assets are read from the published output; the tests run on the build
            // output, whose assets (styles, blazor.web.js) are only reached through the static web assets manifest.
            builder.UseStaticWebAssets();

            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Api:BaseUrl"] = "http://api",
                    ["InternalApiKey:Key"] = ApiHostFactory.InternalApiKey,
                }));

            builder.ConfigureTestServices(services =>
                services.AddHttpClient("Api").ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler()));
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApplicationCollection : ICollectionFixture<ApplicationHost>
{
    public const string Name = "Application";
}
