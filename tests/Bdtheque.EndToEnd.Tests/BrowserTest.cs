extern alias frontend;

using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// A test that drives pages of a browser on <typeparamref name="TApplication"/>. It owns the browser contexts it opens, and
/// closes them at its end with their pages (Playwright, <c>Browser.NewContextAsync</c>: a context created directly is
/// closed explicitly once done). Left open, they would pile up over a run with their connections to the application, and
/// on the server the circuits of their interactive components, a load that grows with the number of tests.
/// <para>
/// Every context presents its pages in the theme the test fixes, one of those it runs in (<see cref="InEachThemeAttribute"/>):
/// a test left to the theme the application draws would pass or fail depending on the draw
/// (gestion-projet/gouvernance/qualite-du-code.md).
/// </para>
/// </summary>
public abstract class BrowserTest<TApplication>(TApplication application) : IAsyncLifetime
    where TApplication : ApplicationHost
{
    private readonly List<IBrowserContext> _contexts = [];

    protected TApplication Application { get; } = application;

    /// <summary>A context of its own in the given theme, closed at the end of the test.</summary>
    protected Task<IBrowserContext> NewContextAsync(string theme, BrowserNewContextOptions? options = null)
    {
        if (!ThemeCatalog.Offered.Any(offered => offered.Key == theme))
            throw new ArgumentException($"Not an offered theme: {theme}.", nameof(theme));

        options ??= new BrowserNewContextOptions();
        options.ExtraHTTPHeaders = new Dictionary<string, string>(options.ExtraHTTPHeaders ?? [])
        {
            [RequestThemeSelector.HeaderName] = theme,
        };
        return OpenAsync(options);
    }

    /// <summary>
    /// A context of its own whose theme the application draws, as for a visitor: only for the tests of the draw itself
    /// (<see cref="ThemeTests"/>), which the compiler refuses elsewhere (BannedSymbols.txt).
    /// </summary>
    protected Task<IBrowserContext> NewContextInTheDrawnThemeAsync(BrowserNewContextOptions? options = null) =>
        OpenAsync(options ?? new BrowserNewContextOptions());

    private async Task<IBrowserContext> OpenAsync(BrowserNewContextOptions options)
    {
#pragma warning disable RS0030 // The one opening of the contexts of the tests, each of which this class owns and closes.
        var context = await Application.NewContextAsync(options);
#pragma warning restore RS0030
        _contexts.Add(context);
        PageReadiness.Track(context);
        return context;
    }

    /// <summary>A page of a context of its own, in the given theme.</summary>
    protected async Task<IPage> NewPageAsync(string theme, BrowserNewContextOptions? options = null) =>
        await (await NewContextAsync(theme, options)).NewPageAsync();

    /// <summary>A page of its own in the given theme, at the size of the given screen (e.g. a smartphone).</summary>
    protected Task<IPage> NewPageAsync(string theme, ViewportSize viewport) =>
        NewPageAsync(theme, new BrowserNewContextOptions { ViewportSize = viewport });

    /// <summary>
    /// A page of its own in the given theme, at the size of the given screen, presented as the captures present it: French
    /// locale, a device pixel per CSS pixel, reduced motion.
    /// </summary>
    protected Task<IPage> NewPageInThemeAsync(string theme, ViewportSize viewport) => NewPageAsync(theme, new BrowserNewContextOptions
    {
        ViewportSize = viewport,
        DeviceScaleFactor = 1,
        Locale = "fr-FR",
        TimezoneId = ApplicationHost.BrowserTimeZoneId,
        ReducedMotion = ReducedMotion.Reduce,
    });

    /// <summary>
    /// A page of its own presented in the given theme, at the size of the given screen, for its capture. What a theme
    /// draws at random (themes/planche.js) is drawn from the same sequence of numbers at each capture.
    /// </summary>
    protected async Task<IPage> NewCapturePageAsync(string theme, ViewportSize viewport)
    {
        var page = await NewPageInThemeAsync(theme, viewport);
        await page.AddInitScriptAsync(SeededRandom);
        return page;
    }

    // Park-Miller, from a fixed seed, in place of the random numbers of the browser.
    private const string SeededRandom = "(() => { let s = 20261102; Math.random = () => (s = (s * 16807) % 2147483647) / 2147483647; })()";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.CloseAsync();
    }
}
