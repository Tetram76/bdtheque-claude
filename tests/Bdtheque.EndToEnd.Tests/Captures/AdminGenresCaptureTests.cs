extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the genres of the administration, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose genres and dates are fixed: the list, a genre changed in its row, and the
/// frame of a new genre above the list.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminGenresCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["list", "edit", "new"];

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
            "edit" => AppPaths.AdminEditGenre(await GenreIdAsync("Humour")),
            "new" => AppPaths.AdminNewGenre,
            _ => AppPaths.AdminGenres,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-genres-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<Guid> GenreIdAsync(string label)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<Page<GenreAdminListItem>>($"/admin/genres?q={Uri.EscapeDataString(label)}");
        return Assert.Single(list!.Items).Id;
    }
}
