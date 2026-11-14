namespace Bdtheque.EndToEnd.Tests.Errors;

/// <summary>
/// The failures of an interactive page, which the script of Blazor presents in the elements the application gives it: the
/// banner of an unhandled error, and the dialog of the reconnection, each a technical error presented by the single
/// component of the errors when it is one (choix-implementation/socle/frontend.md, Présentation), with its actions.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class InteractiveFailureTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    [Theory, InEachTheme]
    public async Task UnhandledError_IsPresentedAsATechnicalError_ThatCanBeDismissed(string theme)
    {
        var page = await FailedPageAsync(theme);

        var banner = page.Locator("#blazor-error-ui");
        await Assertions.Expect(banner.Locator("[role=alert][data-error-category=technical]")).ToBeVisibleAsync();

        await banner.GetByRole(AriaRole.Button, new() { Name = "Fermer" }).ClickAsync();
        await Assertions.Expect(banner).ToBeHiddenAsync();
    }

    [Theory, InEachTheme]
    public async Task UnhandledError_ReloadsThePage(string theme)
    {
        var page = await FailedPageAsync(theme);

        await page.Locator("#blazor-error-ui").GetByRole(AriaRole.Button, new() { Name = "Recharger la page" }).ClickAsync();

        // A new document, the only way for the banner to be hidden without being dismissed, whose interactive component
        // answers again.
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator("button[data-interactive=true]")).ToBeVisibleAsync();
    }

    // Paused by the server, the session is no error: the dialog says so, and resumes it.
    [Theory, InEachTheme]
    public async Task PausedSession_IsResumedFromTheDialog(string theme)
    {
        var page = await InteractivePageAsync(theme);

        await page.EvaluateAsync("() => Blazor.pauseCircuit()");

        var dialog = page.Locator("#components-reconnect-modal");
        await Assertions.Expect(dialog.GetByText("La session a été suspendue par le serveur.")).ToBeVisibleAsync();
        await Assertions.Expect(dialog.Locator("[role=alert]:visible")).ToHaveCountAsync(0);

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Reprendre" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }

    private async Task<IPage> InteractivePageAsync(string theme)
    {
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, FailurePage.Path);
        await Assertions.Expect(page.Locator("button[data-interactive=true]")).ToBeVisibleAsync();
        return page;
    }

    private async Task<IPage> FailedPageAsync(string theme)
    {
        var page = await InteractivePageAsync(theme);
        await page.GetByRole(AriaRole.Button, new() { Name = "Échouer" }).ClickAsync();
        return page;
    }
}
