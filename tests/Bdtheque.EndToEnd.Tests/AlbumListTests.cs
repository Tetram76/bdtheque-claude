extern alias frontend;

using System.Text.RegularExpressions;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the albums of the consultation, as its visitor goes through it, on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose albums are fixed: the list presents the whole base, which the other tests
/// change. One album in each state of the possession: « Le Nid des Marsupilamis » and its untitled neighbour owned, « Le
/// Prisonnier du Bouddha » wished for, « Z comme Zorglub » neither. Its captures are in
/// <see cref="Captures.AlbumListCaptureTests"/>.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AlbumListTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    // By sort key, the series standing in for the title of the album that has none: « Nid des Marsupilamis [Le] », « Prisonnier du
    // Bouddha [Le] », « Spirou et Fantasio », « Z comme Zorglub ».
    private static readonly string[] AllAlbums =
    [
        "Le Nid des Marsupilamis (Spirou et Fantasio - T. 12)",
        "Le Prisonnier du Bouddha (Spirou et Fantasio - T. 14)",
        "Spirou et Fantasio - T. 13",
        "Z comme Zorglub (Spirou et Fantasio - T. 15)",
    ];

    private static readonly string[] OwnedAlbums = [AllAlbums[0], AllAlbums[2]];

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList_OfAllTheAlbums_ItsEntryCurrent(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Home);

        var menu = page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" });
        await menu.GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Albums));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Albums");
        await Assertions.Expect(menu.GetByRole(AriaRole.Link, new() { Name = "Albums" })).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(Labels(page)).ToHaveTextAsync(AllAlbums);
    }

    // The filter is kept as long as the list: by its navigation by initial, by the return of the browser from the record of an
    // album; the entry of the menu opens the list on the whole base again (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
    [Theory, InEachTheme]
    public async Task Possession_NarrowsTheList_AsLongAsTheList(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Albums);

        var possession = page.GetByRole(AriaRole.Navigation, new() { Name = "Possession" });
        await possession.GetByRole(AriaRole.Link, new() { Name = "Possédé", Exact = true }).ClickAsync();
        await Assertions.Expect(Labels(page)).ToHaveTextAsync(OwnedAlbums);
        await Assertions.Expect(possession.GetByRole(AriaRole.Link, new() { Name = "Possédé", Exact = true })).ToHaveAttributeAsync("aria-current", "true");

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" })
            .GetByRole(AriaRole.Link, new() { Name = "N", Exact = true }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Albums, "?entry=N&state=InCollection"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([AllAlbums[0]]);

        await page.GetByRole(AriaRole.Link, new() { Name = AllAlbums[0] }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/albums/[0-9a-f-]{36}$"));
        await page.GoBackAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Albums, "?entry=N&state=InCollection"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([AllAlbums[0]]);

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" }).GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Albums));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync(AllAlbums);
    }

    [Theory, InEachTheme]
    public async Task Possession_ThatNoAlbumHasUnderAnEntry_SaysSo_AndClearsTheFilterUnderTheSameEntry(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync($"{AppPaths.Albums}?entry=Z&state=InCollection");

        await Assertions.Expect(page.Locator(".empty-list strong")).ToHaveTextAsync("Aucun album ne correspond à ce filtre.");
        await page.GetByRole(AriaRole.Link, new() { Name = "Effacer le filtre" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Albums, "?entry=Z"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([AllAlbums[3]]);
    }

    private static ILocator Labels(IPage page) => page.Locator(".album-row:not(.album-row-head) .row-entry");
}
