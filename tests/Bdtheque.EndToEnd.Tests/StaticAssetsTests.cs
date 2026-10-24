extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The pages load their styles and the script of Blazor: without them, the tests would exercise pages that no
/// user sees, and an interactive component would never run.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class StaticAssetsTests(ApplicationHost application)
{
    [Fact]
    public async Task Page_LoadsItsStylesAndScripts()
    {
        var page = await application.NewPageAsync();
        var failures = new List<string>();
        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
                failures.Add($"{response.Status} {response.Url}");
        };

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        Assert.Empty(failures);
        Assert.True(await page.EvaluateAsync<bool>("() => typeof window.Blazor === 'object'"), "The script of Blazor did not run.");
    }
}
