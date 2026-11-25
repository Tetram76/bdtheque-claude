extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The merge of two universes and of their homonymous sub-universes (visuel/fusion/fusion-de-deux-fiches.md), on the application of
/// the merges (<see cref="MergeApplication"/>): from the dialog of the list to the merge, the data of the universe and of each pair
/// chosen, the confirmation and the list again; and the merge of a universe with one of its sub-universes, whose parent cannot be
/// kept. Its captures are in <see cref="Captures.AdminMergeCaptureTests"/>.
/// </summary>
[Collection(MergeApplication.Name)]
public sealed class AdminUniverseMergeTests(MergeApplication application) : BrowserTest<MergeApplication>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task Merge_FromTheDialogOfTheList_MergesTheHomonymousSubUniverses_AndLeadsBackToTheList(string theme)
    {
        // Homonyms under two parents, the name of a universe being unique under its parent.
        var first = await CreateAsync($"Star Wars {_marker}");
        var second = await CreateAsync($"Star wars {_marker}", (await CreateAsync($"Lucasfilm {_marker}")).Id);
        var cloneWars = await CreateAsync("Clone Wars", first.Id);
        var twin = await CreateAsync("Clone wars", second.Id);
        var page = await OpenAsync(theme, $"{AppPaths.AdminUniverses}?q={_marker}&merge={first.Id}&mergeQ={Uri.EscapeDataString($"Star wars {_marker}")}");

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux univers" });
        await dialog.GetByRole(AriaRole.Radio, new() { Name = $"Star wars {_marker}" }).CheckAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergeUniverses, $"?other={second.Id}&universe={first.Id}"));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Fusionner deux univers");
        // The names of the universes and of the pair differ, and the parent is set on one side only, retained at once.
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("2 données restent à choisir");
        var pair = page.GetByRole(AriaRole.Region, new() { Name = "Clone Wars" });
        await Assertions.Expect(pair).ToBeVisibleAsync();

        // The names differ by their case only: the value of each radio says its record.
        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true }).First.Locator("input[value=First]").CheckAsync();
        await pair.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true }).Locator("input[value=Second]").CheckAsync();
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("Tout est choisi");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();

        var confirmation = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Fusionner les deux univers ?" });
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : «\u00A0Star Wars {_marker}\u00A0».");
        await Assertions.Expect(confirmation).ToContainTextAsync("Les sous-univers homonymes n'en feront plus qu'un chacun : «\u00A0Clone Wars\u00A0».");
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminUniverses));
        using var api = Application.CreateApiClient();
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await api.GetAsync($"/admin/universes/{second.Id}")).StatusCode);
        // As many data of each sub-universe kept: the first one remains, with the name of the second.
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await api.GetAsync($"/admin/universes/{twin.Id}")).StatusCode);
        var keptPair = (await api.GetFromJsonAsync<UniverseForm>($"/admin/universes/{cloneWars.Id}"))!;
        Assert.Equal(("Clone wars", first.Id), (keptPair.Name, keptPair.ParentId));
    }

    [Theory, InEachTheme]
    public async Task Merge_WithOneOfItsSubUniverses_PresentsItsParentAsImpossible_AndRetainsTheOther(string theme)
    {
        var pratt = await CreateAsync($"Univers Hugo Pratt {_marker}");
        var first = await CreateAsync("Corto Maltese", pratt.Id);
        var second = await CreateAsync("Corto", first.Id);
        var page = await OpenAsync(theme, $"{AppPaths.AdminMergeUniverses}?universe={first.Id}&other={second.Id}");
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        // Nothing to choose: left out of the reduced display, the default; presented by « Tout afficher ».
        var parent = page.GetByRole(AriaRole.Radiogroup, new() { Name = "Univers parent" });
        await Assertions.Expect(parent).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Tout afficher" }).CheckAsync();
        await Assertions.Expect(parent.Locator("input[value=Second]")).ToBeDisabledAsync();
        await Assertions.Expect(parent.Locator("input[value=First]")).ToBeCheckedAsync();
        await Assertions.Expect(parent).ToContainTextAsync("Impossible : l'univers qui reste en serait son propre ancêtre.");
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private async Task<UniverseForm> CreateAsync(string name, Guid? parentId = null)
    {
        using var api = Application.CreateApiClient();
        var response = await api.PostAsJsonAsync("/admin/universes", new CreateUniverseRequest(name, null, parentId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UniverseForm>())!;
    }
}
