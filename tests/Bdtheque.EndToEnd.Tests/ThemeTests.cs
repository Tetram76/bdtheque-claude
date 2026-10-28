extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.EndToEnd.Tests;

[Collection(ApplicationCollection.Name)]
public sealed class ThemeTests(ApplicationHost application)
{
    [Fact]
    public async Task Page_IsPresentedInTheThemeOfTheSelector_WithTheContractsOfTheBaseTheme()
    {
        var theme = application.FrontendServices.GetRequiredService<IThemeSelector>().Current;
        var page = await application.NewPageAsync();

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", theme.Key);
        var background = await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--color-background').trim()");
        Assert.NotEmpty(background);
    }

    [Fact]
    public async Task Page_UsesTheFontsServedByTheApplication()
    {
        var page = await application.NewPageAsync();

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        Assert.True(await page.EvaluateAsync<bool>("async () => (await document.fonts.ready, document.fonts.check('16px Inter'))"),
            "The font of the base theme is not loaded.");
        Assert.True(await page.EvaluateAsync<bool>(
            "() => [...document.fonts].some(font => font.family.replaceAll('\"', '') === 'Inter' && font.status === 'loaded')"),
            "No face of the font of the base theme was loaded from the application.");
    }
}
