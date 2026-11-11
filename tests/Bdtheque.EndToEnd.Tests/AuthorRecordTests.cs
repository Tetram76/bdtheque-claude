extern alias frontend;

using System.Text.RegularExpressions;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The record of an author in the consultation (visuel/fiche-auteur/contenu.md), as its visitor goes through it: from the
/// record of an album, under the authors of the menu, its series unfolded and folded one by one or all at once, on the
/// application of the records (<see cref="RecordsApplication"/>).
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed partial class AuthorRecordTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    [Fact]
    public async Task Record_IsReachedFromTheAuthorsOfAnAlbum_UnderTheAuthorsOfTheMenu()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));

        await page.Locator(".contributions").GetByRole(AriaRole.Link, new() { Name = "Hugo Pratt" }).First.ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/authors/{Application.AuthorId}$"));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Hugo Pratt");
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Menu de la consultation" })
            .GetByRole(AriaRole.Link, new() { Name = "Auteurs" })).ToHaveAttributeAsync("aria-current", "page");
    }

    [Fact]
    public async Task Series_OfMoreThanSixAlbums_IsFoldedOnArrival_AndEachSeriesUnfoldsAndFolds()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.AuthorRecord(Application.AuthorId));
        var kirk = Series(page, "Sergent Kirk");
        var corto = Series(page, "Corto Maltese");

        await Assertions.Expect(AlbumOf(kirk, "Le Fort de l'Ouest")).ToBeHiddenAsync();
        await Assertions.Expect(AlbumOf(corto, "Les Celtiques")).ToBeVisibleAsync();

        await kirk.Locator("summary").ClickAsync();
        await Assertions.Expect(AlbumOf(kirk, "Le Fort de l'Ouest")).ToBeVisibleAsync();
        await corto.Locator("summary").ClickAsync();
        await Assertions.Expect(AlbumOf(corto, "Les Celtiques")).ToBeHiddenAsync();
    }

    [Fact]
    public async Task UnfoldAll_ThenFoldAll_ActOnEverySeries()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.AuthorRecord(Application.AuthorId));
        var kirk = AlbumOf(Series(page, "Sergent Kirk"), "Le Fort de l'Ouest");
        var corto = AlbumOf(Series(page, "Corto Maltese"), "Les Celtiques");

        await page.GetByRole(AriaRole.Button, new() { Name = "Tout déplier" }).ClickAsync();
        await Assertions.Expect(kirk).ToBeVisibleAsync();
        await Assertions.Expect(corto).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Tout replier" }).ClickAsync();
        await Assertions.Expect(kirk).ToBeHiddenAsync();
        await Assertions.Expect(corto).ToBeHiddenAsync();
    }

    [Fact]
    public async Task AnAlbumOfTheBibliography_LeadsToItsRecord()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.AuthorRecord(Application.AuthorId));

        await AlbumOf(Series(page, "Corto Maltese"), "Lointaines îles du vent").ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/albums/{Application.AlbumId}$"));
    }

    // The entry of a series of the bibliography, by its title.
    private static ILocator Series(IPage page, string title) =>
        page.Locator(".bibliography-entry[data-kind='series']").Filter(new() { Has = page.Locator(".entry-row > .entry-text > .entry-title", new() { HasText = title }) });

    private static ILocator AlbumOf(ILocator series, string title) =>
        series.Locator("details.entry-albums").GetByRole(AriaRole.Link, new() { Name = title, Exact = true });
}
