using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// Waits for a page to be complete and settled, judged on its own state, as Playwright prescribes, rather than on the
/// quiet of the network (<c>NetworkIdle</c>, which its documentation discourages for tests): a network that falls quiet
/// says nothing of what the scripts of the page draw once it is laid out, nor of a stylesheet that failed to load.
/// </summary>
public static class PageReadiness
{
    // Every stylesheet of the page applied, or the page is not the one a user sees, whatever is checked on it. Chromium
    // keeps a sheet on the link of a stylesheet that failed to load, but refuses to read its rules (SecurityError), as for
    // a sheet of another origin, which the application never loads; a sheet answered by an error status is not applied
    // either. The fonts laid out, which change the size of the text. Then two frames: what the scripts of the page do once
    // it is laid out, observers of sizes and of mutations and drawings deferred to the next frame (themes/planche.js,
    // scroll-row.js), is done by the second one.
    private const string Settled = """
        async () => {
            // Why a stylesheet is not applied, or null when it is.
            const failure = link => {
                const status = performance.getEntriesByName(link.href)[0]?.responseStatus;
                if (status !== undefined && (status < 200 || status >= 300))
                    return `status ${status}`;
                if (!link.sheet)
                    return "no sheet";
                try {
                    link.sheet.cssRules;
                    return null;
                } catch (error) {
                    return `rules unreadable (${error.name})`;
                }
            };
            const missing = [...document.querySelectorAll('link[rel="stylesheet"]')]
                .map(link => [link.href, failure(link)]).filter(([, why]) => why).map(([href, why]) => `${href} (${why})`);
            if (missing.length)
                throw new Error(`Stylesheets not applied: ${missing.join(", ")}`);
            await document.fonts.ready;
            await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
        }
        """;

    // The page loaded, its document complete as its load event fires, and drawn: « La planche encrée » draws each block of
    // the page laid out (themes/planche.js), title balloons included. Judged in the page, by a single call that the driver
    // polls: the client of Playwright waits for a load state in two steps, checking those it received then subscribing to
    // those to come, and loses one that arrives in between (microsoft/playwright-dotnet#3364, #298).
    private const string LoadedAndDrawn = """
        () => document.readyState === "complete"
            && (document.documentElement.dataset.theme !== "planche"
                || [...document.querySelectorAll(".block")].every(block => block.offsetWidth <= 1 || block.dataset.drawn !== undefined))
        """;

    // The requests of each context that failed or were answered by an error, and those still running, which tell why a
    // page is not complete: a request that never ends holds back its load.
    private static readonly ConditionalWeakTable<IBrowserContext, Requests> Network = [];

    private sealed class Requests
    {
        public ConcurrentQueue<string> Failed { get; } = new();

        public ConcurrentDictionary<IRequest, byte> Running { get; } = new();
    }

    /// <summary>Notes the requests of the context that fail or are still running, for the message of a page that is not ready.</summary>
    public static void Track(IBrowserContext context)
    {
        var requests = Network.GetOrCreateValue(context);
        context.Request += (_, request) => requests.Running.TryAdd(request, 0);
        context.RequestFinished += (_, request) => requests.Running.TryRemove(request, out byte _);
        context.RequestFailed += (_, request) =>
        {
            requests.Running.TryRemove(request, out byte _);
            requests.Failed.Enqueue($"{request.Url} ({request.Failure})");
        };
        // A document may answer an error legitimately (e.g. the page of an unknown address): only what it loads is noted.
        context.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Request.ResourceType != "document")
                requests.Failed.Enqueue($"{response.Url} (status {response.Status})");
        };
    }

    /// <summary>Waits for the page loaded, its stylesheets applied, its fonts laid out, and what its scripts draw done.</summary>
    public static async Task WaitAsync(IPage page)
    {
        try
        {
            // The handle of the value the wait returns, of no use.
            await (await page.WaitForFunctionAsync(LoadedAndDrawn)).DisposeAsync();
            await page.EvaluateAsync(Settled);
        }
        catch (Exception exception) when (exception is PlaywrightException or TimeoutException
            && Network.TryGetValue(page.Context, out var requests) && (!requests.Failed.IsEmpty || !requests.Running.IsEmpty))
        {
            throw new PlaywrightException(
                $"{exception.Message}; requests failed: [{string.Join(", ", requests.Failed)}]; requests running: [{string.Join(", ", requests.Running.Keys.Select(request => request.Url))}]",
                exception);
        }
    }

    /// <summary>
    /// Waits for the entry form of the page to be rendered by its circuit (<c>EntryForm</c>, <c>data-interactive</c>): until
    /// then, the prerendered form answers neither typing nor clicks. A form that cites no record by its name has no other sign
    /// of it (see <c>ControlsPage.WaitUntilInteractiveAsync</c>).
    /// </summary>
    public static async Task WaitUntilEntryFormInteractiveAsync(IPage page) =>
        // The handle of the value the wait returns, of no use.
        await (await page.WaitForFunctionAsync("() => document.querySelector('.entry-form[data-interactive]') !== null")).DisposeAsync();

    /// <summary>Goes to the page and waits for it to be ready (<see cref="WaitAsync"/>).</summary>
    public static async Task<IResponse?> GotoAsync(IPage page, string path)
    {
        var response = await page.GotoAsync(path);
        await WaitAsync(page);
        return response;
    }
}
