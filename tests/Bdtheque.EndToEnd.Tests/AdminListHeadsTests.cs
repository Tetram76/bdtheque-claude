extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The heads of the columns of every list of the administration (visuel/listes/, visuel/liste-albums/), on the application
/// of the administration, whose base holds a record of every type: each head has the style of the others, whatever the
/// style of the values of its column in the rows (#319).
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminListHeadsTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    // Wide enough for every column, the secondary ones (the last modification) included.
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 900 };

    [Theory]
    [InEachTheme(AppPaths.AdminAlbums), InEachTheme(AppPaths.AdminSeries), InEachTheme(AppPaths.AdminEditions)]
    [InEachTheme(AppPaths.AdminPurchaseIntents), InEachTheme(AppPaths.AdminAuthors), InEachTheme(AppPaths.AdminPublishers)]
    [InEachTheme(AppPaths.AdminGenres), InEachTheme(AppPaths.AdminUniverses)]
    public async Task HeadsOfTheColumns_AllHaveTheSameStyle(string theme, string list)
    {
        var page = await NewPageAsync(theme, Desktop);
        await ApplicationHost.SignInAsync(page, list);
        await PageReadiness.WaitAsync(page);

        var styles = await page.EvaluateAsync<string[]>("""
            () => [...document.querySelectorAll('[role="columnheader"]')]
                .filter(head => head.checkVisibility())
                .map(head => {
                    const style = getComputedStyle(head);
                    return [style.fontSize, style.fontWeight, style.letterSpacing, style.textTransform, style.fontVariantNumeric, style.color].join(' | ');
                })
            """);

        Assert.NotEmpty(styles);
        Assert.Single(styles.Distinct());
    }
}
