extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the purchase intents of the administration, captured in each offered theme on the application of the
/// administration (<see cref="AdministrationApplication"/>), whose intents and dates are fixed: the list, an intent on a whole
/// album and one on an edition, and the dialog of the removal of the intent on an edition, which takes the edition along.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminPurchaseIntentsCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["list", "removal"];

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
            "removal" => $"{AppPaths.AdminPurchaseIntents}?delete={await IntentOnAnEditionAsync()}",
            _ => AppPaths.AdminPurchaseIntents,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-purchase-intents-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<Guid> IntentOnAnEditionAsync()
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<Page<PurchaseIntentAdminListItem>>("/admin/purchase-intents");
        return Assert.Single(list!.Items, intent => intent.Edition is not null).Id;
    }
}
