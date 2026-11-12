extern alias frontend;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Infrastructure;
using Bdtheque.EndToEnd.Tests.Captures;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Api;
using frontend::Bdtheque.Frontend.Formatting;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;
using frontend::Bdtheque.Frontend.Visits;
using FrontendProgram = frontend::Program;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The whole application, as a browser reaches it: <c>frontend</c> listens on a real port, which a
/// browser needs, and calls <c>api</c>, itself on its own PostgreSQL database
/// (<see cref="ApiHostFactory"/>), the way the Docker network carries its calls.
/// </summary>
public class ApplicationHost : IAsyncLifetime
{
    /// <summary>
    /// When the application starts, whenever its tests run (choix-implementation/outillage.md): a time of the night when the
    /// date of the browser of the tests (<see cref="BrowserTimeZoneId"/>) is already the next day while it is still the day
    /// before in universal time, so that every test runs where a date guessed in universal time would show (#340).
    /// </summary>
    public static readonly DateTimeOffset Origin = new(2026, 1, 15, 23, 20, 0, TimeSpan.Zero);

    /// <summary>The time zone of a browser of the tests, unless a test sets another one: not that of the machine.</summary>
    public const string BrowserTimeZoneId = "Europe/Paris";

    private readonly ApiHostFactory _api = new(TestClock.StartingAt(Origin));
    private FrontendFactory? _frontend;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;

    /// <summary>Address of <c>frontend</c>, the only container a browser talks to.</summary>
    public Uri BaseAddress { get; private set; } = null!;

    /// <summary>Services of <c>frontend</c>, as the application configures them.</summary>
    public IServiceProvider FrontendServices => _frontend!.Services;

    /// <summary>The administrator account of the tests.</summary>
    public const string AdminLogin = "admin";

    public const string AdminPassword = "test-admin-password";

    /// <summary>A client of <c>api</c> itself, as <c>frontend</c> calls it: for a test to write its own data.</summary>
    public HttpClient CreateApiClient() => _api.CreateApiClient();

    /// <summary>
    /// The clock of the whole application, <c>frontend</c> as <c>api</c>, from <see cref="Origin"/>: a test sets it when its
    /// captures show the dates it writes.
    /// </summary>
    public TestClock Clock => _api.Clock;

    /// <summary>The date of a browser in <paramref name="timeZone"/>, at the time of the application.</summary>
    public DateOnly TodayIn(string timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(timeZone)).DateTime);

    /// <summary>The days the history of the editions in the spotlight holds a line for.</summary>
    public async Task<IReadOnlyList<DateOnly>> SpotlightDaysAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().EditionSpotlights.Select(s => s.Day).ToListAsync();
    }

    /// <summary>Signs the administrator in, by the login page, as a user does: the page then goes to <paramref name="path"/>.</summary>
    public static async Task SignInAsync(IPage page, string path)
    {
        await page.GotoAsync(path);
        await page.GetByLabel("Identifiant").FillAsync(AdminLogin);
        await page.GetByLabel("Mot de passe").FillAsync(AdminPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(path));
    }

    public async Task InitializeAsync()
    {
        await _api.InitializeAsync();

        _frontend = new FrontendFactory(_api, PaintedByItsSpotlight);
        _frontend.UseKestrel(0);
        _frontend.StartServer();
        BaseAddress = new Uri(_frontend.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single());

        // A no-op once the browser is installed: a local run needs no manual step before it.
        if (Microsoft.Playwright.Program.Main(["install", "--only-shell", "chromium"]) != 0)
            throw new InvalidOperationException("The Chromium browser of Playwright could not be installed.");
        _playwright = await Playwright.CreateAsync();
        // Glyphs drawn without hinting and colors in sRGB: the captures do not depend on settings of the machine. Without
        // partial raster, a tile changed after the first paint (the interactive rendering of a component, a chevron that
        // a script shows) is redrawn whole, with the same antialiased edges as a tile painted at once: the captures do not
        // depend on when the page changed either.
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Args = ["--font-render-hinting=none", "--force-color-profile=srgb", "--disable-partial-raster"],
        });

        await SeedAsync();
    }

    /// <summary>
    /// The data of an application of its own, written through the API once it runs: a screen whose rendering depends on the
    /// whole collection (e.g. the dashboard) cannot share the database of the other tests, which write their own data.
    /// </summary>
    protected virtual Task SeedAsync() => Task.CompletedTask;

    /// <summary>
    /// Whether a theme painted by the cover of the day (« La couverture du jour ») takes the colours of the spotlight of the
    /// data of this application, which an application of its own fixes; otherwise the theme keeps its own colours: the
    /// spotlight of a database the tests share depends on the order in which they write.
    /// </summary>
    protected virtual bool PaintedByItsSpotlight => false;

    /// <summary>
    /// A browser context of its own, on <c>frontend</c>: no cookie nor history shared with another one. The caller owns
    /// it, and closes it once done: an open context keeps its pages, their connections to the application, and on the
    /// server the circuits of their interactive components. Its theme is the one its options fix, if any: the tests
    /// open their contexts through <see cref="BrowserTest{TApplication}"/>, which fixes it, the compiler refusing this
    /// method elsewhere (BannedSymbols.txt).
    /// </summary>
    public Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions options)
    {
        options.BaseURL = BaseAddress.ToString();
        options.TimezoneId ??= BrowserTimeZoneId;
        // Reduced motion, unless the test asks for motion: the welcome screen of the first request of each context then
        // gives way to the page at once, rather than when its animation ends (Components/WelcomeScreen.razor), which would
        // hold back every test the time of the animation.
        options.ReducedMotion ??= ReducedMotion.Reduce;
#pragma warning disable RS0030 // The browser of the tests, whose contexts are opened here only.
        return Browser.NewContextAsync(options);
#pragma warning restore RS0030
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

    private sealed class FrontendFactory(ApiHostFactory api, bool paintedByItsSpotlight) : WebApplicationFactory<FrontendProgram>
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
                    ["AdminAccount:Login"] = AdminLogin,
                    ["AdminAccount:Password"] = AdminPassword,
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient(ApiClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler());
                // One time for the whole application, as one machine gives it to its containers.
                services.Replace(ServiceDescriptor.Singleton<TimeProvider>(api.Clock));
                FixTheThemeByRequest(services);
                if (!paintedByItsSpotlight)
                    services.Replace(ServiceDescriptor.Scoped<IDayColorsSource, OwnDayColors>());
                services.Replace(ServiceDescriptor.Singleton(new PageAssemblies([typeof(ControlsPage).Assembly])));
                // What a page draws at each display (e.g. the greeting of the dashboard) is always the first choice: a page
                // looks the same at each capture.
                services.Replace(ServiceDescriptor.Singleton<Random>(new FirstChoice()));
                // No download of the ranges of the ISBN agency, whose site the tests do not reach: every ISBN as entered, the
                // same at each capture (their formatting is pinned by the tests of the frontend).
                services.PostConfigure<IsbnRangesOptions>(options => options.Source = null);
            });
        }

        // The selector of the application stays the one used by a request that fixes no theme. The application, in
        // production, draws the theme of each visit: no way to fix it is ever part of it (gestion-projet/gouvernance/
        // qualite-du-code.md), the selector of the tests being added here, by their host only.
        private static void FixTheThemeByRequest(IServiceCollection services)
        {
            var application = services.Single(service => service.ServiceType == typeof(IThemeSelector));
            if (application.ImplementationType != typeof(VisitThemeSelector))
                throw new InvalidOperationException(
                    $"In production, the theme is expected to be drawn for the visit, not chosen by {application.ImplementationType}.");
            var implementation = application.ImplementationType;

            services.Remove(application);
            services.Add(ServiceDescriptor.Describe(implementation, implementation, application.Lifetime));
            services.Add(ServiceDescriptor.Describe(typeof(IThemeSelector),
                provider => new RequestThemeSelector(provider.GetRequiredService<IHttpContextAccessor>(), provider.GetRequiredService<VisitCookie>(),
                    (IThemeSelector)provider.GetRequiredService(implementation)),
                application.Lifetime));
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApplicationCollection : ICollectionFixture<ApplicationHost>
{
    public const string Name = "Application";
}

/// <summary>A draw that always picks the first possibility.</summary>
internal sealed class FirstChoice : Random
{
    public override int Next(int maxValue) => 0;

    public override int Next(int minValue, int maxValue) => minValue;
}

/// <summary>No colours of the day: a theme painted by them keeps its own.</summary>
internal sealed class OwnDayColors : IDayColorsSource
{
    public Task<CoverPalette?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult<CoverPalette?>(null);
}
