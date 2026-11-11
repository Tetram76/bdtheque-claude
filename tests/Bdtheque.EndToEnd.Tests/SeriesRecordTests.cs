extern alias frontend;

using System.Text.RegularExpressions;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The record of a series in the consultation (visuel/fiche-serie/contenu.md), as its visitor goes through it: under the series
/// of the menu, from the record of one of its albums, and back to the record of another, on the application of the records
/// (<see cref="RecordsApplication"/>).
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class SeriesRecordTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    [Fact]
    public async Task Record_IsUnderTheSeriesOfTheMenu_AndNamesTheSeries()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.SeriesRecord(Application.SeriesId));

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Corto Maltese");
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" })
            .GetByRole(AriaRole.Link, new() { Name = "Séries" })).ToHaveAttributeAsync("aria-current", "page");
    }

    [Fact]
    public async Task RecordOfAnAlbum_LeadsToTheRecordOfItsSeries_WhichLeadsToAnotherAlbum()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));

        // The series above the title of the album, not the universe of the same name.
        await page.Locator(".album-title").GetByRole(AriaRole.Link, new() { Name = "Corto Maltese" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/series/{Application.SeriesId}$"));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Corto Maltese");

        await page.Locator("[data-part='albums']").GetByRole(AriaRole.Link, new() { Name = "Les Celtiques" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/albums/{Application.IntendedAlbumId}$"));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Les Celtiques");
    }

    [Fact]
    public async Task RestOfTheSeries_LeadsToTheRecordOfTheSeries()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));

        await page.Locator("[data-part='series']").GetByRole(AriaRole.Link, new() { Name = "Voir la fiche de la série" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/series/{Application.SeriesId}$"));
    }
}
