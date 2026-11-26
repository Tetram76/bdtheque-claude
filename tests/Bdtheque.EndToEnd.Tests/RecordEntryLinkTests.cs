extern alias frontend;

using System.Text.RegularExpressions;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The link of a record of the consultation to its entry form, for the authenticated administrator only (visuel/fiche-album/contenu.md,
/// decisions of the user, #658): absent for a visitor; for the administrator, on the line of the breadcrumb, leading to the
/// entry form, which leads back to the record once cancelled, a form on a page of its own as a panel over a list. Nothing is
/// saved, the application of the records being shared with its captures (<see cref="RecordsApplication"/>).
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class RecordEntryLinkTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    [Theory, InEachTheme]
    public async Task Visitor_IsLedToNoEntryForm(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));

        await Assertions.Expect(page.Locator("h1")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Modifier l'album" })).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("[data-part^='edit-']")).ToHaveCountAsync(0);
    }

    [Theory, InEachTheme]
    public async Task Administrator_ModifiesTheAlbum_FromItsRecord_AndComesBackToIt(string theme)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);
        await page.GotoAsync(AppPaths.Album(Application.AlbumId));

        await page.GetByRole(AriaRole.Link, new() { Name = "Modifier l'album" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditAlbumFromRecord(Application.AlbumId)));
        // Interactive once its circuit is open: before that, a click on « Annuler » would be lost.
        await Assertions.Expect(page.Locator("[data-interactive]")).ToHaveCountAsync(1);
        await page.Locator(".save-bar").GetByRole(AriaRole.Link, new() { Name = "Annuler" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/albums/{Application.AlbumId}$"));
    }

    [Theory, InEachTheme]
    public async Task Administrator_ModifiesTheAuthor_FromItsRecord_InThePanelOverItsList_AndComesBackToIt(string theme)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);
        await page.GotoAsync(AppPaths.AuthorRecord(Application.AuthorId));

        await page.GetByRole(AriaRole.Link, new() { Name = "Modifier l'auteur" }).ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'auteur" });
        await Assertions.Expect(panel).ToBeVisibleAsync();
        // The list behind the panel is the one searched on the name of the author, which shows it.
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditAuthorFromRecord(Application.AuthorId, "Pratt")));
        await Assertions.Expect(page.Locator(".author-row:not(.author-row-head)").Filter(new() { HasText = "Hugo Pratt" })).ToHaveCountAsync(1);
        await panel.GetByRole(AriaRole.Link, new() { Name = "Annuler" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/authors/{Application.AuthorId}$"));
    }
}
