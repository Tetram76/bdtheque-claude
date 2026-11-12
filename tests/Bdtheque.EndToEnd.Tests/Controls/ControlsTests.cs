using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Admin;

namespace Bdtheque.EndToEnd.Tests.Controls;

/// <summary>
/// The controls and the interactive components shared by the screens, in a browser (<see cref="ControlsPage"/>): what only
/// a browser does — the popover of a menu, the modal of a dialog and its script, the keyboard of a combobox and its
/// script, the styles of the controls — fixed before any screen uses them.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed partial class ControlsTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    [Theory, InEachTheme]
    public async Task Menu_OpensFromItsButton_UnderIt_AndClosesOnAClickOutside(string theme)
    {
        var page = await OpenAsync(theme);
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Autres actions sur Le Lotus bleu" });
        var menu = page.Locator("#row-1");

        await toggle.ClickAsync();

        await Assertions.Expect(menu).ToBeVisibleAsync();
        var (button, panel) = (await toggle.BoundingBoxAsync(), await menu.BoundingBoxAsync());
        Assert.True(panel!.Y >= button!.Y + button.Height, "The menu is not under its button.");
        Assert.Equal(button.X + button.Width, panel.X + panel.Width, 1);

        await page.Locator("h1").ClickAsync();

        await Assertions.Expect(menu).ToBeHiddenAsync();
    }

    [Theory, InEachTheme]
    public async Task Menu_ClosesOnEscape_AndLeadsByItsItems(string theme)
    {
        var page = await OpenAsync(theme);
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Autres actions sur Le Lotus bleu" });
        var menu = page.Locator("#row-1");

        await toggle.ClickAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menu).ToBeHiddenAsync();

        await toggle.ClickAsync();
        await menu.GetByRole(AriaRole.Link, new() { Name = "Modifier" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(EditedUrl());
    }

    [Theory, InEachTheme]
    public async Task Dialog_ChosenFromAMenu_OpensAsAModal_FocusingItsClosing_AndEscapeLeadsToThePageWithoutIt(string theme)
    {
        var page = await OpenAsync(theme);
        await page.GetByRole(AriaRole.Button, new() { Name = "Autres actions sur Le Lotus bleu" }).ClickAsync();
        await page.Locator("#row-1").GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer l'album ?" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.True(await dialog.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await Assertions.Expect(dialog.GetByRole(AriaRole.Link, new() { Name = "Annuler" })).ToBeFocusedAsync();
        await Assertions.Expect(page.Locator("#row-1")).ToBeHiddenAsync();
        await Assertions.Expect(dialog.Locator(".deletion-impact li")).ToHaveCountAsync(3);

        await page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(page).ToHaveURLAsync(PageWithoutDialogUrl());
        await Assertions.Expect(page.Locator("dialog.dialog")).ToHaveCountAsync(0);
    }

    [Theory, InEachTheme]
    public async Task Dialog_OfAPageLoadedWithIt_OpensAsAModal_AndClosesByItsLink(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.GotoAsync(ControlsPage.Path + "?dialog=true");

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer l'album ?" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.True(await dialog.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));

        await dialog.GetByRole(AriaRole.Link, new() { Name = "Annuler" }).ClickAsync();

        await Assertions.Expect(page.Locator("dialog.dialog")).ToHaveCountAsync(0);
    }

    [Theory, InEachTheme]
    public async Task RecordPicker_SuggestsByTheTextTyped_AndTheKeyboardChoosesWithoutSubmittingTheForm(string theme)
    {
        var token = $"Zorglub{Guid.NewGuid():N}"[..14];
        var genre = await CreateGenreAsync($"{token} cosmique");
        await CreateGenreAsync($"{token} galactique");
        await CreateDecoyAsync(token);
        var page = await OpenAsync(theme);
        var input = page.GetByRole(AriaRole.Combobox, new() { Name = "Genre" });

        await TypeAndAwaitSuggestionsAsync(page, input, token);
        await Assertions.Expect(Suggestions(page)).ToHaveTextAsync([$"{token} cosmique", $"{token} galactique"]);

        await input.PressAsync("ArrowDown");
        await input.PressAsync("Enter");

        await Assertions.Expect(input).ToHaveValueAsync($"{token} cosmique");
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.EndsWith(ControlsPage.Path, page.Url, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("input[type=hidden][name=genreId]")).ToHaveValueAsync(genre.ToString());

        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"genreId={genre}"));
    }

    [Theory, InEachTheme]
    public async Task RecordPicker_KeepsTheActiveSuggestionInView_WhenTheListOverflows(string theme)
    {
        var token = $"Gaston{Guid.NewGuid():N}"[..12];
        for (var index = 0; index < 10; index++)
            await CreateGenreAsync($"{token} {index}");
        var page = await OpenAsync(theme);
        var input = page.GetByRole(AriaRole.Combobox, new() { Name = "Genre" });

        // At once rather than letter by letter: a single search, whose answer no later one replaces.
        await TypeAndAwaitSuggestionsAsync(page, input, token, atOnce: true);
        // The ten records of the text, in their order.
        await Assertions.Expect(Suggestions(page)).ToHaveTextAsync([.. Enumerable.Range(0, 10).Select(index => $"{token} {index}")]);
        for (var index = 0; index < 10; index++)
            await input.PressAsync("ArrowDown");

        var last = Suggestions(page).Last;
        await Assertions.Expect(last).ToHaveAttributeAsync("aria-selected", "true");
        // Within the visible part of the list, to the rounding of a pixel, once the list has scrolled, which follows the
        // rendering; the list, within the page.
        await page.WaitForFunctionAsync("id => { const option = document.getElementById(id); const o = option.getBoundingClientRect(); const l = option.parentElement.getBoundingClientRect(); return o.top >= l.top - 1 && o.bottom <= l.bottom + 1; }", await last.GetAttributeAsync("id"), new() { Timeout = 5000 });
        await Assertions.Expect(last).ToBeInViewportAsync();
    }

    [Theory, InEachTheme]
    public async Task RecordPicker_Escape_ClosesTheSuggestions_WithoutErasingTheText(string theme)
    {
        var token = $"Marsupilami{Guid.NewGuid():N}"[..16];
        await CreateGenreAsync(token);
        await CreateDecoyAsync(token);
        var page = await OpenAsync(theme);
        var input = page.GetByRole(AriaRole.Combobox, new() { Name = "Genre" });

        await TypeAndAwaitSuggestionsAsync(page, input, token);
        await Assertions.Expect(Suggestions(page)).ToHaveTextAsync([token]);
        await input.PressAsync("Escape");

        await Assertions.Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(input).ToHaveValueAsync(token);
    }

    [Theory, InEachTheme]
    public async Task Placeholder_KeepsItsColor_WithoutOpacity(string theme)
    {
        var page = await OpenAsync(theme);

        var placeholder = await page.EvaluateAsync<string[]>("""
            () => {
                const style = getComputedStyle(document.getElementById('f-title'), '::placeholder');
                return [style.color, style.opacity, resolve('--color-placeholder')];
            }
            """.Replace("resolve(", ResolveColor));

        Assert.Equal(placeholder[2], placeholder[0]);
        Assert.Equal("1", placeholder[1]);
    }

    [Theory, InEachTheme]
    public async Task Focus_IsAnOutlineOfTheText_SetApartByABandOfTheSurfaceAround(string theme)
    {
        var page = await OpenAsync(theme);

        await page.Locator("#f-title").FocusAsync();
        // In a block, the band is its fill: the tint a theme deals it, or its surface.
        var inBlock = await FocusOfAsync(page, "#f-title", "var(--block-fill, var(--color-surface))");
        await page.Locator(".actions-menu-toggle").FocusAsync();
        var onPage = await FocusOfAsync(page, ".actions-menu-toggle", "var(--color-background)");

        foreach (var focus in new[] { inBlock, onPage })
        {
            Assert.Equal(focus[2], focus[0]);
            Assert.Contains(focus[3], focus[1], StringComparison.Ordinal);
        }
    }

    [Theory, InEachTheme]
    public async Task ControlInError_IsBorderedWithTheAlertColor(string theme)
    {
        var page = await OpenAsync(theme);

        var colors = await page.EvaluateAsync<string[]>("""
            () => [getComputedStyle(document.getElementById('f-key')).borderTopColor, resolve('--color-alert')]
            """.Replace("resolve(", ResolveColor));

        Assert.Equal(colors[1], colors[0]);
    }

    [GeneratedRegex(@"\?edited=true$")]
    private static partial Regex EditedUrl();

    [GeneratedRegex(@"/test/controls$")]
    private static partial Regex PageWithoutDialogUrl();

    // The computed color of a custom property of the theme, as the browser writes the colors it computes (rgb()).
    private const string ResolveColor = "(name => { const probe = document.createElement('span'); probe.style.color = `var(${name})`; " +
        "document.body.append(probe); const color = getComputedStyle(probe).color; probe.remove(); return color; })(";

    // The outline color, the box shadow, then the expected colors of the text and of the band, a color of the surface
    // around the element, computed beside it.
    private static Task<string[]> FocusOfAsync(IPage page, string selector, string band) =>
        page.EvaluateAsync<string[]>($$"""
            () => {
                const element = document.querySelector('{{selector}}');
                const style = getComputedStyle(element);
                const probe = document.createElement('span');
                probe.style.color = '{{band}}';
                element.parentElement.append(probe);
                const around = getComputedStyle(probe).color;
                probe.remove();
                return [style.outlineColor, style.boxShadow, resolve('--color-text'), around];
            }
            """.Replace("resolve(", ResolveColor));

    private async Task<IPage> OpenAsync(string theme)
    {
        var page = await NewPageAsync(theme);
        await PageReadiness.GotoAsync(page, ControlsPage.Path);
        await ControlsPage.WaitUntilInteractiveAsync(page);
        return page;
    }

    // The page is interactive (OpenAsync): the text typed once is answered.
    private static async Task TypeAndAwaitSuggestionsAsync(IPage page, ILocator input, string text, bool atOnce = false)
    {
        if (atOnce)
            await input.FillAsync(text);
        else
            await input.PressSequentiallyAsync(text);
        await Assertions.Expect(Suggestions(page).First).ToBeVisibleAsync();
    }

    private static ILocator Suggestions(IPage page) =>
        page.GetByRole(AriaRole.Listbox, new() { Name = "Suggestions" }).GetByRole(AriaRole.Option);

    // A genre that only the beginnings of the text find: the suggestions of an earlier text, which the field must never
    // show once the text has changed (#275), include it, those of the whole text not.
    private Task<Guid> CreateDecoyAsync(string text) => CreateGenreAsync($"{text[..^1]} leurre");

    private async Task<Guid> CreateGenreAsync(string label)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync("/admin/genres", new CreateGenreRequest(label));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GenreForm>())!.Id;
    }
}
