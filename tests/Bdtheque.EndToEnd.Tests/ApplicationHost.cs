extern alias frontend;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bdtheque.EndToEnd.Tests.Captures;
using frontend::Bdtheque.Frontend.Api;
using frontend::Bdtheque.Frontend.Theming;
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

    /// <summary>Services of <c>frontend</c>, as the application configures them.</summary>
    public IServiceProvider FrontendServices => _frontend!.Services;

    /// <summary>A client of <c>api</c> itself, as <c>frontend</c> calls it: for a test to write its own data.</summary>
    public HttpClient CreateApiClient() => _api.CreateApiClient();

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
        // Glyphs drawn without hinting and colors in sRGB: the captures do not depend on settings of the machine.
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Args = ["--font-render-hinting=none", "--force-color-profile=srgb"],
        });
    }

    /// <summary>A page of a browser context of its own: no cookie nor history shared between tests.</summary>
    public Task<IPage> NewPageAsync() => NewPageAsync(new BrowserNewContextOptions());

    /// <summary>A page of its own presented in the given theme, at the size of the given screen, for its capture.</summary>
    public Task<IPage> NewCapturePageAsync(string themeKey, ViewportSize viewport) => NewPageAsync(new BrowserNewContextOptions
    {
        ViewportSize = viewport,
        DeviceScaleFactor = 1,
        Locale = "fr-FR",
        TimezoneId = "Europe/Paris",
        ReducedMotion = ReducedMotion.Reduce,
        ExtraHTTPHeaders = new Dictionary<string, string> { [RequestThemeSelector.HeaderName] = themeKey },
    });

    private async Task<IPage> NewPageAsync(BrowserNewContextOptions options)
    {
        options.BaseURL = BaseAddress.ToString();
        var context = await Browser.NewContextAsync(options);
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
            {
                services.AddHttpClient(ApiClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler());
                FixTheThemeByRequest(services);
            });
        }

        // The selector of the application stays the one used by a request that fixes no theme.
        private static void FixTheThemeByRequest(IServiceCollection services)
        {
            var application = services.Single(service => service.ServiceType == typeof(IThemeSelector));
            var implementation = application.ImplementationType
                ?? throw new InvalidOperationException("The theme selector of the application is expected to be registered by its type.");

            services.Remove(application);
            services.Add(ServiceDescriptor.Describe(implementation, implementation, application.Lifetime));
            services.Add(ServiceDescriptor.Describe(typeof(IThemeSelector),
                provider => new RequestThemeSelector(provider.GetRequiredService<IHttpContextAccessor>(), (IThemeSelector)provider.GetRequiredService(implementation)),
                application.Lifetime));
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApplicationCollection : ICollectionFixture<ApplicationHost>
{
    public const string Name = "Application";
}
