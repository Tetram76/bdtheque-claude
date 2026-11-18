extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The focus of the login page on its first field, the login, however the page is reached: loaded by the browser, reached
/// by a link of the application, which Blazor follows by an enhanced navigation, or given back after a refusal; and its
/// keyboard path, through each of its controls, the login included.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminLoginFocusTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    [Theory, InEachTheme]
    public async Task LoginPage_LoadedByTheBrowser_HasTheFocusOnTheLogin(string theme)
    {
        var page = await NewPageAsync(theme);

        await PageReadiness.GotoAsync(page, AppPaths.AdminLogin);

        await Assertions.Expect(page.GetByLabel("Identifiant")).ToBeFocusedAsync();
    }

    [Theory, InEachTheme]
    public async Task LoginPage_ReachedByALinkOfTheApplication_HasTheFocusOnTheLogin(string theme)
    {
        var page = await NewPageAsync(theme);
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

    [Theory, InEachTheme]
    public async Task LoginPage_GivenBackAfterARefusal_KeepsTheLoginTyped_WithTheFocus(string theme)
    {
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, AppPaths.AdminLogin);
        var login = page.GetByLabel("Identifiant");

        await login.FillAsync(ApplicationHost.AdminLogin);
        await page.GetByLabel("Mot de passe").FillAsync("not-the-password");
        await page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Identifiant ou mot de passe incorrect");
        await Assertions.Expect(login).ToHaveValueAsync(ApplicationHost.AdminLogin);
        await Assertions.Expect(login).ToBeFocusedAsync();

        // Signed in, which forgets the failure: the attempts are counted for the whole application, which the other tests
        // of the collection share, and failures left behind, one per theme, would end up refusing their logins.
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);
    }

    [Theory, InEachTheme]
    public async Task LoginPage_LoadedByTheBrowser_HasEachOfItsControlsInTheKeyboardPath(string theme)
    {
        var page = await NewPageAsync(theme);

        await PageReadiness.GotoAsync(page, AppPaths.AdminLogin);

        await AssertTheKeyboardPathAsync(page);
    }

    [Theory, InEachTheme]
    public async Task LoginPage_ReachedByALinkOfTheApplication_HasEachOfItsControlsInTheKeyboardPath(string theme)
    {
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, AppPaths.Home);
        await page.EvaluateAsync("() => window.beforeTheLink = true");

        // The focus that Blazor gives after an enhanced navigation is the one that could take the login out of the path.
        await page.GetByRole(AriaRole.Contentinfo).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Connexion à l'administration");
        Assert.True(await page.EvaluateAsync<bool>("() => window.beforeTheLink === true"), "The link led to a page that the browser loaded.");

        await AssertTheKeyboardPathAsync(page);
    }

    // From the login, which has the focus, Tab goes through the form to its button, and Shift+Tab back to the login: each
    // control of the form is in the path, both ways.
    private static async Task AssertTheKeyboardPathAsync(IPage page)
    {
        var login = page.GetByLabel("Identifiant");
        var password = page.GetByLabel("Mot de passe");
        var signIn = page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" });
        await Assertions.Expect(login).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(password).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(signIn).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Assertions.Expect(password).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Assertions.Expect(login).ToBeFocusedAsync();
    }
}
