extern alias frontend;

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

[CollectionDefinition(SecondFactorApplication.Name)]
public sealed class SecondFactorCollection : ICollectionFixture<SecondFactorApplication>;

/// <summary>
/// An application whose administrator account has a second factor, as on a deployment exposed in HTTPS
/// (contraintes-techniques/architecture-deploiement/authentification.md).
/// </summary>
public sealed class SecondFactorApplication : ApplicationHost
{
    public const string Name = "SecondFactor";

    // "12345678901234567890" in Base32, the secret of the test vectors of RFC 6238.
    public const string Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    private static readonly byte[] SecretBytes = "12345678901234567890"u8.ToArray();

    private int _steps;

    protected override string? TotpSecret => Secret;

    /// <summary>
    /// Moves the clock of the application to a step of the code that no sign-in has used yet, and gives its code, computed
    /// here after RFC 6238 rather than by the application: a code is accepted once only, and the tests of every theme
    /// sign in.
    /// </summary>
    public string NextCode()
    {
        var step = 60_000_000L + Interlocked.Add(ref _steps, 2);
        Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(step * 30);
        return CodeOf(step);
    }

    private static string CodeOf(long step)
    {
        var counter = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);
        var hash = HMACSHA1.HashData(SecretBytes, counter);
        var offset = hash[^1] & 0x0F;
        var binary = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset)) & 0x7FFFFFFF;
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// The login of an administrator account with a second factor (fonctionnel/structure-de-l-application.md): the code in the
/// same form, under the password, and a refusal that never tells which of the three was wrong.
/// </summary>
[Collection(SecondFactorApplication.Name)]
public sealed class AdminSecondFactorLoginTests(SecondFactorApplication application) : BrowserTest<SecondFactorApplication>(application)
{
    private const string CodeLabel = "Code de l'application d'authentification";

    [Theory, InEachTheme]
    public async Task LoginPage_AsksForTheCode_UnderThePassword_InTheKeyboardPath(string theme)
    {
        var page = await NewPageAsync(theme);

        await PageReadiness.GotoAsync(page, AppPaths.AdminLogin);

        var password = page.GetByLabel("Mot de passe");
        var code = page.GetByLabel(CodeLabel);
        var signIn = page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" });
        await Assertions.Expect(page.GetByLabel("Identifiant")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(password).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(code).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(signIn).ToBeFocusedAsync();
    }

    [Theory, InEachTheme]
    public async Task SignIn_WithTheCodeOfTheMoment_OpensTheAdministration(string theme)
    {
        var page = await NewPageAsync(theme);

        await SignInAsync(page, Application.NextCode());

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Theory, InEachTheme]
    public async Task SignIn_WithAWrongCode_IsRefused_WithoutTellingWhichWasWrong(string theme)
    {
        var page = await NewPageAsync(theme);
        var code = Application.NextCode();

        await SignInAsync(page, code == "000000" ? "111111" : "000000");

        var error = page.GetByRole(AriaRole.Alert);
        await Assertions.Expect(error).ToHaveAttributeAsync("data-error-category", "business");
        await Assertions.Expect(error).ToContainTextAsync("Identifiant, mot de passe ou code incorrect");

        // Signed in, which forgets the failure: the attempts are counted for the whole application.
        await SignInAsync(page, code);
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Theory, InEachTheme]
    public async Task SignIn_WithACodeAlreadyUsed_IsRefused(string theme)
    {
        var code = Application.NextCode();
        var first = await NewPageAsync(theme);
        await SignInAsync(first, code);
        await Assertions.Expect(first).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));

        var second = await NewPageAsync(theme);
        await SignInAsync(second, code);

        await Assertions.Expect(second.GetByRole(AriaRole.Alert)).ToContainTextAsync("Identifiant, mot de passe ou code incorrect");
        await SignInAsync(second, Application.NextCode());
        await Assertions.Expect(second).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    // Active all along, a request every hour and a half, under the idle timeout of two hours: the session still ends twelve
    // hours after the login (choix-implementation/socle/transverse/authentification-mise-en-oeuvre.md).
    [Theory, InEachTheme]
    public async Task Session_EvenActive_EndsTwelveHoursAfterTheLogin(string theme)
    {
        var page = await NewPageAsync(theme);
        await SignInAsync(page, Application.NextCode());
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
        var signedIn = Application.Clock.UtcNow!.Value;

        for (var hours = 1.5; hours < 12; hours += 1.5)
        {
            Application.Clock.UtcNow = signedIn.AddHours(hours);
            await PageReadiness.GotoAsync(page, AppPaths.AdminHome);
            await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
        }

        Application.Clock.UtcNow = signedIn.AddHours(12);
        await PageReadiness.GotoAsync(page, AppPaths.AdminHome);

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Connexion à l'administration");
    }

    private static async Task SignInAsync(IPage page, string code)
    {
        if (!page.Url.Contains(AppPaths.AdminLogin, StringComparison.Ordinal))
            await PageReadiness.GotoAsync(page, AppPaths.AdminLogin);
        await page.GetByLabel("Identifiant").FillAsync(ApplicationHost.AdminLogin);
        await page.GetByLabel("Mot de passe").FillAsync(ApplicationHost.AdminPassword);
        await page.GetByLabel(CodeLabel).FillAsync(code);
        await page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" }).ClickAsync();
    }
}
