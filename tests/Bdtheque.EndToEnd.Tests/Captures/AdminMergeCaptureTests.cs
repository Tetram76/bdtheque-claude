extern alias frontend;

using System.Text.RegularExpressions;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The merge of two authors, that of two publishers, that of two series and that of two albums, captured in each offered theme on the application of
/// the merges (<see cref="MergeApplication"/>), whose dates are fixed, without merging anything
/// (visuel/fusion/fusion-de-deux-fiches.md): those of two authors and of two publishers as they open, two data left to choose, then
/// every data chosen, with the confirmation open, the publishers with their homonymous collections, in a block of their own; that of
/// two series as it opens, with the credits, the genres and the publisher of either series; that of two albums as it opens, the intent
/// on the whole album withdrawn said apart, then every data chosen, with the confirmation open, what the merge will delete set apart.
/// In a window as high as the page, the bar
/// of the merge at its bottom
/// (<see cref="ScreenCaptureTests.CaptureAsync"/>).
/// </summary>
[Collection(MergeApplication.Name)]
public sealed partial class AdminMergeCaptureTests(MergeApplication application) : BrowserTest<MergeApplication>(application)
{
    private static readonly string[] States =
    [
        "authors", "authors-confirmation", "publishers", "publishers-confirmation", "series", "universes", "universes-confirmation", "albums",
        "albums-confirmation",
    ];

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
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        var path = state.Split('-')[0] switch
        {
            "authors" => $"{AppPaths.AdminMergeAuthors}?author={Application.Gotlib.Id}&other={Application.MarcelGotlib.Id}",
            "publishers" => $"{AppPaths.AdminMergePublishers}?publisher={Application.Dupuis.Id}&other={Application.EditionsDupuis.Id}",
            "series" => $"{AppPaths.AdminMergeSeries}?series={Application.Asterix.Id}&other={Application.AsterixAgain.Id}",
            "albums" => $"{AppPaths.AdminMergeAlbums}?album={Application.Tour.Id}&other={Application.TourAgain.Id}",
            _ => $"{AppPaths.AdminMergeUniverses}?universe={Application.StarWars.Id}&other={Application.StarWarsTwin.Id}",
        };
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        if (state.EndsWith("-confirmation", StringComparison.Ordinal))
        {
            if (state == "albums-confirmation")
            {
                // The title and the first publication of the first album; the genre « Humour » of the second album discarded.
                await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Titre" })
                    .GetByRole(AriaRole.Radio, new() { NameRegex = TourOfAsterix() }).CheckAsync();
                await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Première publication" })
                    .GetByRole(AriaRole.Radio, new() { NameRegex = March() }).CheckAsync();
                await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Genre", Exact = true }).Last
                    .GetByRole(AriaRole.Radio).First.CheckAsync();
                await Assertions.Expect(page.Locator(".merge-data-result")).ToContainTextAsync("Affiché : Le Tour de Gaule d'Astérix");
            }
            else if (state == "authors-confirmation")
            {
                // A value is named after its record too on a smartphone: « Gotlieb » is only that of Gotlib.
                await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true }).GetByRole(AriaRole.Radio, new() { Name = "Gotlieb" }).CheckAsync();
                await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Biographie" }).GetByRole(AriaRole.Radio, new() { Name = "Né à Paris" }).CheckAsync();
                await Assertions.Expect(page.Locator(".merge-data-result")).ToContainTextAsync("Affiché : Gotlib");
            }
            else if (state == "publishers-confirmation")
            {
                await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true })
                    .GetByRole(AriaRole.Radio, new() { NameRegex = StartsWithDupuis() }).CheckAsync();
                await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Aire Libre" })
                    .GetByRole(AriaRole.Radio, new() { NameRegex = CapitalizedAireLibre() }).CheckAsync();
            }
            else
            {
                // Every name chosen, by the value of its radio: the names differ only by their case or their accents.
                foreach (var group in await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true }).AllAsync())
                    await group.Locator("input[value=First]").CheckAsync();
                await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("Tout est choisi");
            }

            await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Alertdialog)).ToBeVisibleAsync();
        }
        else if (state == "albums")
        {
            // The title and the first publication differ; the series, of the first album alone, and the summary, the notes and the
            // rating, each of one album, are retained; the intent on the whole second album said withdrawn.
            await Assertions.Expect(page.GetByText("2 données restent à choisir")).ToBeAttachedAsync();
            await Assertions.Expect(page.Locator(".merge-links-withdrawn")).ToBeVisibleAsync();
        }
        else if (state == "series")
        {
            // The title, the status, the completeness and the format differ; the publisher, of the first series alone, and the
            // sort key entered by hand, against the one following the title, are retained.
            await Assertions.Expect(page.GetByText("4 données restent à choisir")).ToBeAttachedAsync();
        }
        else if (state == "universes")
        {
            await Assertions.Expect(page.GetByText("3 données restent à choisir")).ToBeAttachedAsync();
        }
        else
        {
            // Attached rather than visible: the long state is hidden on a smartphone, which shows the short one.
            await Assertions.Expect(page.GetByText("2 données restent à choisir")).ToBeAttachedAsync();
        }

        CaptureStore.Verify(theme, $"admin-merge-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    // Named after its value, preceded on a smartphone by the name of its record: matched at the start or at the end, and with
    // its case, which alone tells the homonyms apart.
    [GeneratedRegex("^Dupuis")]
    private static partial Regex StartsWithDupuis();

    [GeneratedRegex("Aire Libre$")]
    private static partial Regex CapitalizedAireLibre();

    [GeneratedRegex("Astérix$")]
    private static partial Regex TourOfAsterix();

    [GeneratedRegex("mars 1965$")]
    private static partial Regex March();
}
