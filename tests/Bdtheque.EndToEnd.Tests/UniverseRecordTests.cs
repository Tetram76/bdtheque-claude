extern alias frontend;

using System.Text.RegularExpressions;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The record of a universe in the consultation (visuel/fiche-univers/contenu.md), and where a genre leads, as their visitor goes
/// through them: from the record of an album, from one universe to another, on the application of the records
/// (<see cref="RecordsApplication"/>).
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class UniverseRecordTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    [Theory, InEachTheme]
    public async Task Record_IsReachedFromAnAncestorOfTheUniverseOfAnAlbum_WithoutAnyEntryOfTheMenuCurrent(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));

        await page.Locator(".universe-chain").GetByRole(AriaRole.Link, new() { Name = "Univers Hugo Pratt" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/universes/{Application.UniverseId}$"));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Univers Hugo Pratt");
        // A universe has no entry in the menu of the consultation.
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" })
            .Locator("[aria-current='page']")).ToHaveCountAsync(0);
    }

    [Theory, InEachTheme]
    public async Task SubUniverse_LeadsToItsRecord_WhoseBreadcrumbLeadsBack(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.UniverseRecord(Application.UniverseId));

        await page.Locator(".universe-children").GetByRole(AriaRole.Link, new() { Name = "Corto Maltese" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/universes/{Application.SubUniverseId}$"));

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Univers Hugo Pratt" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/universes/{Application.UniverseId}$"));
    }

    [Theory, InEachTheme]
    public async Task Genre_LeadsToTheListOfTheAlbums_FilteredOnItAlone(string theme)
    {
        var page = await NewPageAsync(theme);
        // A list of the albums left searched: the genre forgets what it kept.
        await page.GotoAsync(new frontend::Bdtheque.Frontend.Lists.CatalogAlbumListQuery(Text: "Celtiques").Href());
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));

        await page.Locator(".classification-genres").GetByRole(AriaRole.Link, new() { Name = "Aventure" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/albums\\?genreId={Application.GenreId}$"));
        await Assertions.Expect(page.Locator("#albums-search")).ToHaveValueAsync("");
    }
}
