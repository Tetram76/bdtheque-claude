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
public sealed partial class ControlsTests(ApplicationHost application)
{
    [Fact]
    public async Task Menu_OpensFromItsButton_UnderIt_AndClosesOnAClickOutside()
    {
        var page = await OpenAsync();
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

    [Fact]
    public async Task Menu_ClosesOnEscape_AndLeadsByItsItems()
    {
        var page = await OpenAsync();
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Autres actions sur Le Lotus bleu" });
        var menu = page.Locator("#row-1");

        await toggle.ClickAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menu).ToBeHiddenAsync();

        await toggle.ClickAsync();
        await menu.GetByRole(AriaRole.Link, new() { Name = "Modifier" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"\?edited=true$"));
    }

    [Fact]
    public async Task Dialog_ChosenFromAMenu_OpensAsAModal_FocusingItsClosing_AndEscapeLeadsToThePageWithoutIt()
    {
        var page = await OpenAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Autres actions sur Le Lotus bleu" }).ClickAsync();
        await page.Locator("#row-1").GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer l'album ?" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.True(await dialog.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await Assertions.Expect(dialog.GetByRole(AriaRole.Link, new() { Name = "Annuler" })).ToBeFocusedAsync();
        await Assertions.Expect(page.Locator("#row-1")).ToBeHiddenAsync();
        await Assertions.Expect(dialog.Locator(".deletion-impact li")).ToHaveCountAsync(3);

        await page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"{Regex.Escape(ControlsPage.Path)}$"));
        await Assertions.Expect(page.Locator("dialog.dialog")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Dialog_OfAPageLoadedWithIt_OpensAsAModal_AndClosesByItsLink()
    {
        var page = await application.NewPageAsync();
        await page.GotoAsync(ControlsPage.Path + "?dialog=true");

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer l'album ?" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.True(await dialog.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));

        await dialog.GetByRole(AriaRole.Link, new() { Name = "Annuler" }).ClickAsync();

        await Assertions.Expect(page.Locator("dialog.dialog")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task RecordPicker_SuggestsByTheTextTyped_AndTheKeyboardChoosesWithoutSubmittingTheForm()
    {
        var token = $"Zorglub{Guid.NewGuid():N}"[..14];
        var genre = await CreateGenreAsync($"{token} cosmique");
        await CreateGenreAsync($"{token} galactique");
        var page = await OpenAsync();
        var input = page.GetByRole(AriaRole.Combobox, new() { Name = "Genre" });

        await TypeUntilSuggestedAsync(page, input, token);
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

    [Fact]
    public async Task RecordPicker_Escape_ClosesTheSuggestions_WithoutErasingTheText()
    {
        var token = $"Marsupilami{Guid.NewGuid():N}"[..16];
        await CreateGenreAsync(token);
        var page = await OpenAsync();
        var input = page.GetByRole(AriaRole.Combobox, new() { Name = "Genre" });

        await TypeUntilSuggestedAsync(page, input, token);
        await input.PressAsync("Escape");

        await Assertions.Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(input).ToHaveValueAsync(token);
    }

    [Fact]
    public async Task Placeholder_KeepsTheMutedText_WithoutOpacity()
    {
        var page = await OpenAsync();

        var placeholder = await page.EvaluateAsync<string[]>("""
            () => {
                const style = getComputedStyle(document.getElementById('f-title'), '::placeholder');
                return [style.color, style.opacity, resolve('--color-text-muted')];
            }
            """.Replace("resolve(", ResolveColor));

        Assert.Equal(placeholder[2], placeholder[0]);
        Assert.Equal("1", placeholder[1]);
    }

    [Fact]
    public async Task Focus_IsAnOutlineOfTheText_SetApartByABandOfTheSurfaceAround()
    {
        var page = await OpenAsync();

        await page.Locator("#f-title").FocusAsync();
        var inBlock = await FocusOfAsync(page, "#f-title", "--color-surface");
        await page.Locator(".actions-menu-toggle").FocusAsync();
        var onPage = await FocusOfAsync(page, ".actions-menu-toggle", "--color-background");

        foreach (var focus in new[] { inBlock, onPage })
        {
            Assert.Equal(focus[2], focus[0]);
            Assert.Contains(focus[3], focus[1], StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ControlInError_IsBorderedWithTheAlertColor()
    {
        var page = await OpenAsync();

        var colors = await page.EvaluateAsync<string[]>("""
            () => [getComputedStyle(document.getElementById('f-key')).borderTopColor, resolve('--color-alert')]
            """.Replace("resolve(", ResolveColor));

        Assert.Equal(colors[1], colors[0]);
    }

    // The computed color of a custom property of the theme, as the browser writes the colors it computes (rgb()).
    private const string ResolveColor = "(name => { const probe = document.createElement('span'); probe.style.color = `var(${name})`; " +
        "document.body.append(probe); const color = getComputedStyle(probe).color; probe.remove(); return color; })(";

    // The outline color, the box shadow, then the expected colors of the text and of the band.
    private static Task<string[]> FocusOfAsync(IPage page, string selector, string band) =>
        page.EvaluateAsync<string[]>($$"""
            () => {
                const style = getComputedStyle(document.querySelector('{{selector}}'));
                return [style.outlineColor, style.boxShadow, resolve('--color-text'), resolve('{{band}}')];
            }
            """.Replace("resolve(", ResolveColor));

    private async Task<IPage> OpenAsync()
    {
        var page = await application.NewPageAsync();
        await page.GotoAsync(ControlsPage.Path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        return page;
    }

    // The combobox answers once its circuit is open, which nothing in the page announces: the text is typed again until it does.
    private static async Task TypeUntilSuggestedAsync(IPage page, ILocator input, string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await input.FillAsync("");
            await input.PressSequentiallyAsync(text);
            try
            {
                await Assertions.Expect(Suggestions(page).First).ToBeVisibleAsync(new() { Timeout = 5000 });
                return;
            }
            catch (PlaywrightException)
            {
                // Not interactive yet.
            }
        }

        throw new InvalidOperationException("The record picker never suggested anything.");
    }

    private static ILocator Suggestions(IPage page) =>
        page.GetByRole(AriaRole.Listbox, new() { Name = "Suggestions" }).GetByRole(AriaRole.Option);

    private async Task<Guid> CreateGenreAsync(string label)
    {
        using var api = application.CreateApiClient();
        using var response = await api.PostAsJsonAsync("/admin/genres", new CreateGenreRequest(label));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GenreForm>())!.Id;
    }
}
