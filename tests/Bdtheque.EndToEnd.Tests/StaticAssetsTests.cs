extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The pages load their styles and the script of Blazor: without them, the tests would exercise pages that no
/// user sees, and an interactive component would never run.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class StaticAssetsTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    [Theory, InEachTheme]
    public async Task Page_LoadsItsStylesAndScripts(string theme)
    {
        var page = await NewPageAsync(theme);
        var failures = new List<string>();
        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
                failures.Add($"{response.Status} {response.Url}");
        };

        await PageReadiness.GotoAsync(page, AppPaths.Home);

        Assert.Empty(failures);
        Assert.True(await page.EvaluateAsync<bool>("() => typeof window.Blazor === 'object'"), "The script of Blazor did not run.");
    }
}
