extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the universes of the administration, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose universes and dates are fixed: the list, in the order of the hierarchy, and
/// the side panel of a
/// sub-universe over the list, its parent presented with its ancestors.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminUniversesCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["list", "panel"];

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
        var path = state switch
        {
            "panel" => AppPaths.AdminEditUniverse(await UniverseIdAsync("Champignac")),
            _ => AppPaths.AdminUniverses,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-universes-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<Guid> UniverseIdAsync(string name)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<Page<UniverseAdminListItem>>($"/admin/universes?q={Uri.EscapeDataString(name)}");
        return Assert.Single(list!.Items).Id;
    }
}
