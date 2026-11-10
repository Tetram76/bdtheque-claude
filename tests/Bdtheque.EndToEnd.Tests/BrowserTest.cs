namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// A test that drives pages of a browser on <typeparamref name="TApplication"/>. It owns the browser contexts it opens, and
/// closes them at its end with their pages (Playwright, <c>Browser.NewContextAsync</c>: a context created directly is
/// closed explicitly once done). Left open, they would pile up over a run with their connections to the application, and
/// on the server the circuits of their interactive components, a load that grows with the number of tests.
/// </summary>
public abstract class BrowserTest<TApplication>(TApplication application) : IAsyncLifetime
    where TApplication : ApplicationHost
{
    private readonly List<IBrowserContext> _contexts = [];

    protected TApplication Application { get; } = application;

    /// <summary>A context of its own, closed at the end of the test.</summary>
    protected async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null)
    {
        var context = await Application.NewContextAsync(options ?? new BrowserNewContextOptions());
        _contexts.Add(context);
        PageReadiness.Track(context);
        return context;
    }

    /// <summary>A page of a context of its own.</summary>
    protected async Task<IPage> NewPageAsync(BrowserNewContextOptions? options = null) =>
        await (await NewContextAsync(options)).NewPageAsync();

    /// <summary>A page of its own, at the size of the given screen (e.g. a smartphone).</summary>
    protected Task<IPage> NewPageAsync(ViewportSize viewport) => NewPageAsync(new BrowserNewContextOptions { ViewportSize = viewport });

    /// <summary>A page of its own presented in the given theme, at the size of the given screen.</summary>
    protected Task<IPage> NewPageInThemeAsync(string themeKey, ViewportSize viewport) => NewPageAsync(new BrowserNewContextOptions
    {
        ViewportSize = viewport,
        DeviceScaleFactor = 1,
        Locale = "fr-FR",
        TimezoneId = ApplicationHost.BrowserTimeZoneId,
        ReducedMotion = ReducedMotion.Reduce,
        ExtraHTTPHeaders = new Dictionary<string, string> { [Captures.RequestThemeSelector.HeaderName] = themeKey },
    });

    /// <summary>
    /// A page of its own presented in the given theme, at the size of the given screen, for its capture. What a theme
    /// draws at random (themes/planche.js) is drawn from the same sequence of numbers at each capture.
    /// </summary>
    protected async Task<IPage> NewCapturePageAsync(string themeKey, ViewportSize viewport)
    {
        var page = await NewPageInThemeAsync(themeKey, viewport);
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
