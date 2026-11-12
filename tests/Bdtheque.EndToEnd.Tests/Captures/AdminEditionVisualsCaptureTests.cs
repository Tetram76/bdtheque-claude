extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The visuals of an edition beside its entry form (visuel/saisie-edition/contenu.md, Les visuels), captured in each offered
/// theme on the application of the records (<see cref="RecordsApplication"/>), whose first edition of « Lointaines îles du vent »
/// has a cover, two plates and a back cover, without writing anything: as read; the menu of a plate open; and a file refused,
/// which api writes nothing for. In a window as high as the page, the bar of the form at its bottom
/// (<see cref="ScreenCaptureTests.CaptureAsync"/>).
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class AdminEditionVisualsCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    private static readonly string[] States = ["visuals", "visual-menu", "visual-refused"];

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
        await ApplicationHost.SignInAsync(page, AppPaths.AdminEditEdition(Application.AlbumId, Application.FirstEditionId));
        await PageReadiness.WaitAsync(page);
        var visuals = page.Locator(".edition-visuals");
        // Every thumbnail loaded, none captured half drawn.
        foreach (var thumbnail in await visuals.Locator(".edition-visual img").AllAsync())
            await Assertions.Expect(thumbnail).ToHaveJSPropertyAsync("complete", true);

        if (state == "visual-menu")
        {
            await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
            await visuals.GetByRole(AriaRole.Button, new() { Name = "Actions sur Planche 1" }).ClickAsync();
            await Assertions.Expect(page.Locator(".actions-menu-panel:popover-open")).ToBeVisibleAsync();
        }
        else if (state == "visual-refused")
        {
            await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
            await visuals.GetByLabel("Choisir des images à ajouter").SetInputFilesAsync(
                new FilePayload { Name = "scan-dos.tif", MimeType = "image/tiff", Buffer = "pas une image"u8.ToArray() });
            await Assertions.Expect(visuals.Locator(".edition-visuals-upload[data-refused]")).ToBeVisibleAsync();
        }

        CaptureStore.Verify(theme, $"admin-edition-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
