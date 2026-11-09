extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the universes of the administration (visuel/listes/liste-par-liste.md), on the application shared by the tests:
/// the order of the hierarchy, and the side panel of a universe over the list (visuel/listes/saisie-selon-l-entite.md), its
/// parent chosen by typing its name. Each test names its universes with a marker of its own, and searches it.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminUniversesTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Menu_LeadsToTheList()
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Univers" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminUniverses));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Univers");
    }

    [Fact]
    public async Task New_InThePanel_UnderAParentChosenByItsName_TakesItsPlaceInTheHierarchy()
    {
        await CreateUniverseAsync($"Univers Hugo Pratt {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminUniverses}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = "Nouvel univers" }).ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Nouvel univers" });
        Assert.True(await panel.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await panel.GetByRole(AriaRole.Textbox, new() { Name = "Nom" }).FillAsync($"Corto Maltese {_marker}");
        await ControlsPage.WaitUntilInteractiveAsync(page);
        await panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }).PressSequentiallyAsync($"Hugo Pratt {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Univers Hugo Pratt {_marker}" }).ClickAsync();
        await Assertions.Expect(panel).ToContainTextAsync($"Ses albums et séries sont aussi ceux de Univers Hugo Pratt {_marker}.");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Créer l'univers" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminUniverses, $"?q={_marker}"));
        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Univers Hugo Pratt {_marker}", $"Corto Maltese {_marker}"]);
        await Assertions.Expect(page.Locator(".universe-row[data-depth='1']")).ToContainTextAsync($"Corto Maltese {_marker}");
    }

    [Fact]
    public async Task Edit_UnderOneOfItsOwnSubUniverses_IsRefusedInPlace_WithItsReason()
    {
        var corto = await CreateUniverseAsync($"Corto Maltese {_marker}");
        await CreateUniverseAsync($"Les Antilles {_marker}", corto.Id);
        var page = await OpenAsync($"{AppPaths.AdminEditUniverse(corto.Id)}&q={_marker}");
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });

        await ControlsPage.WaitUntilInteractiveAsync(page);
        await panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }).PressSequentiallyAsync($"Antilles {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Les Antilles {_marker}" }).ClickAsync();
        await panel.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });
        await Assertions.Expect(panel).ToContainTextAsync("Un univers ne peut pas être placé sous lui-même, ni sous l'un de ses propres sous-univers.");
        // The parent refused stays chosen, presented with its ancestors, to be corrected.
        await Assertions.Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }))
            .ToHaveValueAsync($"Corto Maltese {_marker} › Les Antilles {_marker}");
    }

    [Fact]
    public async Task Panel_Escape_ClosesItWithoutSaving()
    {
        var universe = await CreateUniverseAsync($"Les Cités obscures {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminUniverses}?q={_marker}");
        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier Les Cités obscures {_marker}" }).ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });
        await panel.GetByRole(AriaRole.Textbox, new() { Name = "Nom" }).FillAsync("Autre chose");

        await page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminUniverses, $"?q={_marker}"));
        await Assertions.Expect(Names(page)).ToHaveTextAsync([universe.Name]);
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Names(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Univers" }).Locator(".universe-name");

    private async Task<UniverseForm> CreateUniverseAsync(string name, Guid? parentId = null)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync("/admin/universes", new CreateUniverseRequest(name, null, parentId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UniverseForm>())!;
    }
}
