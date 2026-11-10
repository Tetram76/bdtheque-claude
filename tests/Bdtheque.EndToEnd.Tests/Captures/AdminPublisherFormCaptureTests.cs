extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The entry form of a publisher, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose publishers and dates are fixed, without writing anything: a publisher whose
/// only collection is in use, as read; a publisher with its website, one of its collections removed and another added, as the
/// user left them before saving; and a new publisher whose saving is to correct. In a window as high as the page, the bar of the
/// form at its bottom (<see cref="ScreenCaptureTests.CaptureAsync"/>).
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminPublisherFormCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["edit", "edit-changed", "new-error"];

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in ThemeCatalog.Offered)
            foreach (var state in States)
            foreach (var viewport in ScreenCaptureTests.Viewports.Keys)
                captures.Add(theme.Key, state, viewport);
            return captures;
        }
    }

    [LinuxTheory]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string state, string viewport)
    {
        var path = state switch
        {
            "new-error" => AppPaths.AdminNewPublisher,
            "edit-changed" => AppPaths.AdminEditPublisher(await PublisherIdAsync("Dargaud")),
            _ => AppPaths.AdminEditPublisher(await PublisherIdAsync("Dupuis")),
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        if (state == "edit-changed")
        {
            await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
            await page.GetByRole(AriaRole.Button, new() { Name = "Retirer Long courrier" }).ClickAsync();
            await Assertions.Expect(page.GetByText("Retirée à l'enregistrement")).ToBeVisibleAsync();
            await AdminPublisherFormTests.AddCollectionAsync(page, "Grand Angle");
            await page.GetByLabel("Nom de la collection").Last.BlurAsync();
            // Attached rather than visible: the long state is hidden on a smartphone, which shows the short one.
            await Assertions.Expect(page.GetByText("Modifications non enregistrées")).ToBeAttachedAsync();
        }
        else if (state == "new-error")
        {
            await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
            await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'éditeur" }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
            // The name to correct gets the focus once the error is rendered (EntryForm.RevealProblemAsync): left only once it
            // has it, it would otherwise get it after the field is left, and be captured.
            var name = page.Locator("#publisher-name");
            await Assertions.Expect(name).ToBeFocusedAsync();
            await name.BlurAsync();
        }

        CaptureStore.Verify(theme, $"admin-publisher-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<Guid> PublisherIdAsync(string name)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<Page<PublisherAdminListItem>>($"/admin/publishers?q={Uri.EscapeDataString(name)}");
        return Assert.Single(list!.Items).Id;
    }
}
