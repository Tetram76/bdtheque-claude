extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

[Collection(ApplicationCollection.Name)]
public sealed class NavigationTests(ApplicationHost application)
{
    [Fact]
    public async Task LinkToAPageNotYetImplemented_LeadsToTheNotYetImplementedPage_WhichLeadsBack()
    {
        var page = await application.NewPageAsync();
        await page.GotoAsync(AppPaths.Home);

        await page.GetByRole(AriaRole.Link, new() { Name = "Aide sur cette page" }).ClickAsync();
        await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == AppPaths.NotYetImplemented);
        await Assertions.Expect(page.Locator("h1")).ToBeVisibleAsync();

        await page.Locator("main a").ClickAsync();
        await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == AppPaths.Home);
    }

    [Fact]
    public async Task Administration_WithoutAuthentication_LeadsToTheLoginPage()
    {
        var page = await application.NewPageAsync();

        await page.GotoAsync(AppPaths.AdminHome);

        Assert.Equal(new Uri(application.BaseAddress, AppPaths.AdminLogin).AbsolutePath, new Uri(page.Url).AbsolutePath);
    }

    [Fact]
    public async Task UnknownPage_AnswersNotFound_WithThePageOfTheConsultation()
    {
        var page = await application.NewPageAsync();

        var response = await page.GotoAsync("/no-such-page");

        Assert.Equal(404, response!.Status);
        await Assertions.Expect(page.Locator("main h1")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Aide sur cette page" })).ToBeVisibleAsync();
    }
}
