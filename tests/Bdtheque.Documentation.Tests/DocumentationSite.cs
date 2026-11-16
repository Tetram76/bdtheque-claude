using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bdtheque.Documentation.Tests;

/// <summary>
/// The site of a documentation folder, built by scripts/Build-TechnicalDocumentation.ps1 as the CI and the validation
/// environment build it, served as static files on this machine, and opened in Chromium. Owns the site, its server and its
/// browser, all released on disposal.
/// </summary>
internal sealed partial class DocumentationSite : IAsyncDisposable
{
    /// <summary>
    /// Below this scale, the text of a diagram, which Mermaid draws at 16 pixels and shrinks to the width of the page, falls
    /// under 12 pixels: too small to read without zooming.
    /// </summary>
    public const double MinimumDiagramScale = 0.75;

    // The width of a desktop screen, on which the column of the page is that of a reader at a desk.
    private static readonly ViewportSize Viewport = new() { Width = 1280, Height = 900 };

    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(3);

    // DocFX writes a diagram as a code block, which its script turns into the element Mermaid draws in: every block turned
    // and processed by Mermaid, which marks the element once its drawing, the diagram or else its error, has its final size
    // (viewBox). Then, for each, its scale and whether it is an error.
    private const string RenderedScript = """
        () => document.querySelectorAll('code.lang-mermaid').length === 0
            && [...document.querySelectorAll('pre.mermaid')].every(p => p.hasAttribute('data-processed')
                && p.querySelector('svg')?.viewBox.baseVal.width > 0)
        """;

    private const string DiagramsScript = """
        () => [...document.querySelectorAll('pre.mermaid')].map(pre => {
            const svg = pre.querySelector('svg');
            const error = svg.getAttribute('aria-roledescription') === 'error' || !!svg.querySelector('.error-icon, .error-text');
            return { scale: svg.getBoundingClientRect().width / svg.viewBox.baseVal.width, error };
        })
        """;

    private readonly string _output;
    private readonly WebApplication _server;
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;

    private DocumentationSite(string output, WebApplication server, IPlaywright playwright, IBrowser browser)
    {
        _output = output;
        _server = server;
        _playwright = playwright;
        _browser = browser;
        Address = new Uri(server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
    }

    public Uri Address { get; }

    public string Output => _output;

    public static async Task<DocumentationSite> BuildAsync(string source, string commit)
    {
        var output = Path.Combine(Path.GetTempPath(), $"bdtheque-docs-site-{Guid.NewGuid():N}");
        var build = await Processes.PowerShellAsync(
            Repository.Script("Build-TechnicalDocumentation.ps1"), ["-Source", source, "-Output", output, "-Commit", commit],
            Repository.Root, BuildTimeout);
        if (!build.Succeeded)
            throw new InvalidOperationException($"The documentation of {source} could not be built:{Environment.NewLine}{build.Output}");

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var server = builder.Build();
        var files = new PhysicalFileProvider(output);
        server.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        server.UseStaticFiles(new StaticFileOptions { FileProvider = files, ServeUnknownFileTypes = true });
        await server.StartAsync();

        // A no-op once the browser is installed: a local run needs no manual step before it.
        if (Microsoft.Playwright.Program.Main(["install", "--only-shell", "chromium"]) != 0)
            throw new InvalidOperationException("The Chromium browser of Playwright could not be installed.");
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        return new DocumentationSite(output, server, playwright, browser);
    }

    /// <summary>
    /// What is wrong with the diagrams of a page of the site, and every request it sends off this machine (refused): the
    /// site must be read without network.
    /// </summary>
    public async Task<IReadOnlyList<string>> CheckPageAsync(string page)
    {
        var violations = new List<string>();
        await using var context = await _browser.NewContextAsync(new() { ViewportSize = Viewport });
        await context.RouteAsync("**/*", async route =>
        {
            if (new Uri(route.Request.Url).Host == Address.Host)
            {
                await route.ContinueAsync();
                return;
            }

            lock (violations)
                violations.Add($"{page}: request off the machine, refused: {route.Request.Url}");
            await route.AbortAsync();
        });

        var tab = await context.NewPageAsync();
        var response = await tab.GotoAsync(new Uri(Address, page).AbsoluteUri);
        if (response is not { Ok: true })
            return [$"{page}: not served ({response?.Status})"];

        await tab.WaitForFunctionAsync(RenderedScript, null, new() { Timeout = 15_000 });
        var diagrams = (await tab.EvaluateAsync<JsonElement>(DiagramsScript)).EnumerateArray().ToList();
        // Every diagram the page was built with, so that a check never passes on a page whose diagrams were not found.
        var expected = MermaidBlockPattern().Count(File.ReadAllText(Path.Combine(_output, page)));
        if (diagrams.Count != expected)
            violations.Add($"{page}: {diagrams.Count} diagrams drawn, {expected} written");
        for (var i = 0; i < diagrams.Count; i++)
        {
            var scale = diagrams[i].GetProperty("scale").GetDouble();
            if (diagrams[i].GetProperty("error").GetBoolean())
                violations.Add($"{page}: diagram {i + 1} is not rendered, Mermaid reports an error");
            else if (scale < MinimumDiagramScale)
                violations.Add($"{page}: diagram {i + 1} is shrunk to {scale:0.00} of its size, under {MinimumDiagramScale}: split it, or lay it out top to bottom");
        }

        return violations;
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
        await _server.DisposeAsync();
        Directory.Delete(_output, recursive: true);
    }

    [GeneratedRegex("<code class=\"lang-mermaid\">")]
    private static partial Regex MermaidBlockPattern();
}
