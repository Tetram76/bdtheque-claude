extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The entry form of the setting of the application, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose dates are fixed, without writing anything: the setting as read, and the
/// format of the label of the albums changed, as the user left it before saving, its example following it. In a window as high
/// as the page, the bar of the form at its bottom (<see cref="ScreenCaptureTests.CaptureAsync"/>).
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminSettingsFormCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["edit", "edit-changed"];

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in TestThemes.Covered)
            foreach (var state in States)
            foreach (var viewport in ScreenCaptureTests.Viewports.Keys)
                captures.Add(theme.Key, state, viewport);
            return captures;
        }
    }

    [LinuxTheory, EachTheme]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string state, string viewport)
    {
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminSettings);
        await PageReadiness.WaitAsync(page);

        if (state == "edit-changed")
        {
            await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
            var format = page.GetByLabel("Libellé des albums(obligatoire)");
            await format.SelectOptionAsync(new SelectOptionValue { Label = "Tome - Album (Série)" });
            await Assertions.Expect(page.Locator(".settings-example")).ToHaveTextAsync("Exemple : T. 5 - Le Lotus bleu (Tintin)");
            await format.BlurAsync();
            // Attached rather than visible: the long state is hidden on a smartphone, which shows the short one.
            await Assertions.Expect(page.GetByText("Modifications non enregistrées")).ToBeAttachedAsync();
        }

        CaptureStore.Verify(theme, $"admin-settings-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
