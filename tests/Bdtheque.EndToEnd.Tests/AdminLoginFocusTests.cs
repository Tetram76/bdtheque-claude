extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The focus of the login page on its first field, the login, however the page is reached: loaded by the browser, reached
/// by a link of the application, which Blazor follows by an enhanced navigation, or given back after a refusal; in each
/// theme offered, a page of the consultation being in the theme of the visit (gestion-projet/gouvernance/qualite-du-code.md).
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminLoginFocusTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private static readonly ViewportSize Desktop = new() { Width = 1280, Height = 720 };

    public static TheoryData<string> Themes => [.. ThemeCatalog.Offered.Select(theme => theme.Key)];

    [Theory]
    [MemberData(nameof(Themes))]
    public async Task LoginPage_LoadedByTheBrowser_HasTheFocusOnTheLogin(string theme)
    {
        var page = await NewPageInThemeAsync(theme, Desktop);

        await PageReadiness.GotoAsync(page, AppPaths.AdminLogin);

        await Assertions.Expect(page.GetByLabel("Identifiant")).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public async Task LoginPage_ReachedByALinkOfTheApplication_HasTheFocusOnTheLogin(string theme)
    {
        var page = await NewPageInThemeAsync(theme, Desktop);
        await PageReadiness.GotoAsync(page, AppPaths.Home);
        // Kept by an enhanced navigation, which patches the page in place, and lost by a page that the browser loads: the
        // link must lead to the login by the former, where the browser does not apply the autofocus of a field.
        await page.EvaluateAsync("() => window.beforeTheLink = true");

        // The administration, refused without authentication, redirects to the login, which the enhanced navigation follows.
        await page.GetByRole(AriaRole.Contentinfo).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Connexion à l'administration");
        await Assertions.Expect(page.GetByLabel("Identifiant")).ToBeFocusedAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => window.beforeTheLink === true"), "The link led to a page that the browser loaded.");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public async Task LoginPage_GivenBackAfterARefusal_KeepsTheLoginTyped_WithTheFocus(string theme)
    {
        var page = await NewPageInThemeAsync(theme, Desktop);
        await PageReadiness.GotoAsync(page, AppPaths.AdminLogin);
        var login = page.GetByLabel("Identifiant");
        var password = page.GetByLabel("Mot de passe");
        var signIn = page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" });

        await login.FillAsync(ApplicationHost.AdminLogin);
        await password.FillAsync("not-the-password");
        await signIn.ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Identifiant ou mot de passe incorrect");
        await Assertions.Expect(login).ToHaveValueAsync(ApplicationHost.AdminLogin);
        await Assertions.Expect(login).ToBeFocusedAsync();

        // The password typed again: signed in, which forgets the failure. The attempts are counted for the whole application,
        // which the other tests of the collection share: failures left behind, one per theme, would refuse their logins.
        await password.FillAsync(ApplicationHost.AdminPassword);
        await signIn.ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }
}
