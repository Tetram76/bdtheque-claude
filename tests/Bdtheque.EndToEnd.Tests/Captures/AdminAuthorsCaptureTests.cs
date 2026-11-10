extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the authors of the administration, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose authors and dates are fixed: the list, the dialog of the deletion of an
/// author credited on nothing, and the side panel of an author known under a pseudonym over the list.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminAuthorsCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["list", "deletion", "panel"];

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
            "deletion" => $"{AppPaths.AdminAuthors}?delete={await AuthorIdAsync("Gomez")}",
            "panel" => AppPaths.AdminEditAuthor(await AuthorIdAsync("Gotlib")),
            _ => AppPaths.AdminAuthors,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-authors-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<Guid> AuthorIdAsync(string name)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<Page<AuthorAdminListItem>>($"/admin/authors?q={Uri.EscapeDataString(name)}");
        return Assert.Single(list!.Items).Id;
    }
}
