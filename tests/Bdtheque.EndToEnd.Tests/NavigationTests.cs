extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

[Collection(ApplicationCollection.Name)]
public sealed class NavigationTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    [Theory, InEachTheme]
    public async Task LinkToAPageNotYetImplemented_LeadsToTheNotYetImplementedPage_WhichLeadsBack(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Home);

        await page.GetByRole(AriaRole.Link, new() { Name = "Aide sur cette page" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.OfPath(AppPaths.NotYetImplemented));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Bientôt disponible");

        await page.GetByRole(AriaRole.Link, new() { Name = "Revenir à la page précédente" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.OfPath(AppPaths.Home));
    }

    [Theory, InEachTheme]
    public async Task Administration_WithoutAuthentication_LeadsToTheLoginPage(string theme)
    {
        var page = await NewPageAsync(theme);

        await page.GotoAsync(AppPaths.AdminHome);

        Assert.Equal(new Uri(Application.BaseAddress, AppPaths.AdminLogin).AbsolutePath, new Uri(page.Url).AbsolutePath);
    }

    [Theory, InEachTheme]
    public async Task UnknownPage_AnswersNotFound_WithThePageOfTheConsultation(string theme)
    {
        var page = await NewPageAsync(theme);

        var response = await page.GotoAsync("/no-such-page");

        Assert.Equal(404, response!.Status);
        await Assertions.Expect(page.Locator("main h1")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Aide sur cette page" })).ToBeVisibleAsync();
    }
}
