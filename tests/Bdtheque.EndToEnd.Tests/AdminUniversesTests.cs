extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
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

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList(string theme)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Univers" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminUniverses));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Univers");
    }

    [Theory, InEachTheme]
    public async Task New_InThePanel_UnderAParentChosenByItsName_TakesItsPlaceInTheHierarchy(string theme)
    {
        await CreateUniverseAsync($"Univers Hugo Pratt {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminUniverses}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = "Nouvel univers" }).ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Nouvel univers" });
        Assert.True(await panel.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await panel.GetByRole(AriaRole.Textbox, new() { Name = "Nom" }).FillAsync($"Corto Maltese {_marker}");
        await ControlsPage.WaitUntilInteractiveAsync(page);
        await panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }).PressSequentiallyAsync($"Hugo Pratt {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Univers Hugo Pratt {_marker}" }).ClickAsync();
        await Assertions.Expect(panel).ToContainTextAsync($"Ses séries et albums sont aussi ceux de Univers Hugo Pratt {_marker}.");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Créer l'univers" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminUniverses, $"?q={_marker}"));
        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Univers Hugo Pratt {_marker}", $"Corto Maltese {_marker}"]);
        await Assertions.Expect(page.Locator(".universe-row[data-depth='1']")).ToContainTextAsync($"Corto Maltese {_marker}");
    }

    [Theory, InEachTheme]
    public async Task Edit_UnderOneOfItsOwnSubUniverses_IsRefusedInPlace_WithItsReason(string theme)
    {
        var corto = await CreateUniverseAsync($"Corto Maltese {_marker}");
        await CreateUniverseAsync($"Les Antilles {_marker}", corto.Id);
        var page = await OpenAsync(theme, $"{AppPaths.AdminEditUniverse(corto.Id)}&q={_marker}");
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });

        await ControlsPage.WaitUntilInteractiveAsync(page);
        await panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }).PressSequentiallyAsync($"Antilles {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Les Antilles {_marker}" }).ClickAsync();
        await panel.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });
        await Assertions.Expect(panel).ToContainTextAsync("Un univers ne peut pas être rangé sous lui-même, ni sous l'un de ses sous-univers.");
        await Assertions.Expect(panel).ToContainTextAsync("Ni lui-même, ni l'un de ses sous-univers.");
        // The field in cause gets the focus, once interactive (visuel/listes/saisie-selon-l-entite.md).
        await Assertions.Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" })).ToBeFocusedAsync();
        // The parent refused stays chosen, presented with its ancestors, to be corrected.
        await Assertions.Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }))
            .ToHaveValueAsync($"Corto Maltese {_marker} › Les Antilles {_marker}");
    }

    [Theory, InEachTheme]
    public async Task New_ANameTakenUnderTheSameParentInAnotherCaseAndWithoutItsAccents_IsRefusedInThePanel(string theme)
    {
        await CreateUniverseAsync($"Les Mondes d'Aldébaran {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminUniverses}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = "Nouvel univers" }).ClickAsync();
        var name = page.GetByRole(AriaRole.Dialog, new() { Name = "Nouvel univers" }).GetByRole(AriaRole.Textbox, new() { Name = "Nom" });
        await name.FillAsync($"LES MONDES D'ALDEBARAN {_marker}");
        // Enter sends the form of the panel, which holds its submission (wwwroot/modal-dialog.js).
        await name.PressAsync("Enter");

        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Nouvel univers" });
        await Assertions.Expect(panel).ToContainTextAsync(
            $"« LES MONDES D'ALDEBARAN {_marker} » est déjà pris au premier niveau : deux noms qui ne diffèrent que par la casse ou les accents désignent le même univers.");
        await Assertions.Expect(panel.GetByRole(AriaRole.Textbox, new() { Name = "Nom" })).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Les Mondes d'Aldébaran {_marker}"]);
    }

    [Theory, InEachTheme]
    public async Task Edit_UnderAParentCreatedOnTheSpot_CreatesItWithTheUniverse_AtTheTop(string theme)
    {
        // A universe created on the spot at the top of the hierarchy, attached from its panel to a parent created there too
        // (choix-implementation/socle/frontend.md, création sur place, #414), with it, when the panel is saved.
        var universe = await CreateUniverseAsync($"Les Antilles {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminEditUniverse(universe.Id)}&q={_marker}");
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });

        await ControlsPage.WaitUntilInteractiveAsync(page);
        await panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }).PressSequentiallyAsync($"Mers du Sud {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Créer l'univers « Mers du Sud {_marker} »" }).ClickAsync();
        var creation = page.GetByRole(AriaRole.Dialog, new() { Name = "Nouvel univers" });
        var name = creation.GetByRole(AriaRole.Textbox, new() { Name = "Nom" });
        await Assertions.Expect(name).ToHaveValueAsync($"Mers du Sud {_marker}");
        // Enter adds the universe corrected in the dialog, rather than sending the form of the panel behind it.
        await name.FillAsync($"Mers australes {_marker}");
        await name.PressAsync("Enter");
        await Assertions.Expect(creation).ToHaveCountAsync(0);
        await Assertions.Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" })).ToHaveValueAsync($"Mers australes {_marker}");
        // Created only with the universe, when the panel is saved.
        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Les Antilles {_marker}"]);
        await panel.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminUniverses, $"?q={_marker}"));
        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Mers australes {_marker}", $"Les Antilles {_marker}"]);
        await Assertions.Expect(page.Locator(".universe-row[data-depth='1']")).ToContainTextAsync($"Les Antilles {_marker}");
    }

    [Theory, InEachTheme]
    public async Task Edit_UnderAParentCreatedOnTheSpotWhoseNameIsTakenAtTheTop_IsRefusedOnTheParent(string theme)
    {
        // Created at the top of the hierarchy, the parent meets the names of the universes without parent: refused at the
        // saving, it is designated on the field of the parent, which cites it (#414), and nothing is created.
        await CreateUniverseAsync($"Ombrelune {_marker}");
        var universe = await CreateUniverseAsync($"Port-Grisé {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminEditUniverse(universe.Id)}&q={_marker}");
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });

        await ControlsPage.WaitUntilInteractiveAsync(page);
        await panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }).PressSequentiallyAsync($"OMBRELUNE {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Créer l'univers « OMBRELUNE {_marker} »" }).ClickAsync();
        await page.GetByRole(AriaRole.Dialog, new() { Name = "Nouvel univers" }).GetByRole(AriaRole.Button, new() { Name = "Ajouter l'univers" }).ClickAsync();
        await panel.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });
        // Each text states the situation of the parent refused, at the top of the hierarchy (decision of the user, #485).
        await Assertions.Expect(panel).ToContainTextAsync(
            "Deux univers de premier niveau ne peuvent pas porter le même nom, sans tenir compte de la casse ni des accents.");
        await Assertions.Expect(panel).ToContainTextAsync(
            $"« OMBRELUNE {_marker} » est déjà pris au premier niveau : deux noms qui ne diffèrent que par la casse ou les accents désignent le même univers.");
        var parent = panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" });
        await Assertions.Expect(parent).ToHaveValueAsync($"OMBRELUNE {_marker}");
        await Assertions.Expect(parent).ToHaveAttributeAsync("aria-invalid", "true");
        // The field in cause gets the focus, once interactive (visuel/listes/saisie-selon-l-entite.md).
        await Assertions.Expect(parent).ToBeFocusedAsync();
        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Ombrelune {_marker}", $"Port-Grisé {_marker}"]);
    }

    [Theory, InEachTheme]
    public async Task Edit_UnderAParentCreatedOnTheSpotWithItsNameEmptied_IsRefusedOnTheParent_KeepingTheParentSaved(string theme)
    {
        // A parent created on the spot whose name the user emptied is still sent, for the saving to refuse it: never taken for
        // no parent, which would detach the universe from the one it has.
        var pratt = await CreateUniverseAsync($"Univers Hugo Pratt {_marker}");
        var corto = await CreateUniverseAsync($"Corto Maltese {_marker}", pratt.Id);
        var page = await OpenAsync(theme, $"{AppPaths.AdminEditUniverse(corto.Id)}&q={_marker}");
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });

        await ControlsPage.WaitUntilInteractiveAsync(page);
        await panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }).FillAsync("");
        await panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" }).PressSequentiallyAsync($"Mers du Sud {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Créer l'univers « Mers du Sud {_marker} »" }).ClickAsync();
        var name = page.GetByRole(AriaRole.Dialog, new() { Name = "Nouvel univers" }).GetByRole(AriaRole.Textbox, new() { Name = "Nom" });
        await name.FillAsync("");
        await name.PressAsync("Enter");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });
        await Assertions.Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" })).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "Univers parent" })).ToBeFocusedAsync();
        await Assertions.Expect(page.Locator(".universe-row[data-depth='1']")).ToContainTextAsync($"Corto Maltese {_marker}");
    }

    [Theory, InEachTheme]
    public async Task Panel_Escape_ClosesItWithoutSaving(string theme)
    {
        var universe = await CreateUniverseAsync($"Les Cités obscures {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminUniverses}?q={_marker}");
        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier Les Cités obscures {_marker}" }).ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });
        await panel.GetByRole(AriaRole.Textbox, new() { Name = "Nom" }).FillAsync("Autre chose");

        await page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminUniverses, $"?q={_marker}"));
        await Assertions.Expect(Names(page)).ToHaveTextAsync([universe.Name]);
    }

    [Theory, InEachTheme]
    public async Task Deletion_RefusedWhileTheUniverseHasASubUniverse_ThenConfirmedForTheSubUniverse(string theme)
    {
        var parent = await CreateUniverseAsync($"Parent {_marker}");
        await CreateUniverseAsync($"Enfant {_marker}", parent.Id);
        var page = await OpenAsync(theme, $"{AppPaths.AdminUniverses}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Parent {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();
        var refusal = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Suppression impossible" });
        await Assertions.Expect(refusal).ToContainTextAsync("1 univers : rangez-le sous un autre univers, ou supprimez-le.");
        await refusal.GetByRole(AriaRole.Link, new() { Name = "Fermer" }).ClickAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Enfant {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();
        await page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer cet univers ?" })
            .GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminUniverses, $"?q={_marker}"));
        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Parent {_marker}"]);
    }

    // Deleted meanwhile, over its record in the consultation (visuel/listes/saisie-selon-l-entite.md, #658): the record, which no
    // longer exists, says so, and its panel stays open over it with what was typed.
    [Theory, InEachTheme]
    public async Task Edit_FromItsRecord_OfAUniverseDeletedMeanwhile_KeepsThePanelOpen_AndSaysSo(string theme)
    {
        var universe = await CreateUniverseAsync($"Disparu {_marker}");
        // The record being public, the administrator signs in first.
        var page = await OpenAsync(theme, AppPaths.AdminHome);
        await page.GotoAsync(AppPaths.UniverseRecordWithEntry(universe.Id));
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });
        await DeleteUniverseAsync(universe);

        await panel.GetByRole(AriaRole.Textbox, new() { Name = "Description" }).FillAsync("Une description");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Modifier l'univers" });
        await Assertions.Expect(panel.Locator("[data-error-category=functional]")).ToContainTextAsync("Fiche supprimée entre-temps");
        await Assertions.Expect(panel.GetByRole(AriaRole.Textbox, new() { Name = "Description" })).ToHaveValueAsync("Une description");
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Names(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Univers" }).Locator(".row-entry");

    private async Task<UniverseForm> CreateUniverseAsync(string name, Guid? parentId = null)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync("/admin/universes", new CreateUniverseRequest(name, null, parentId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UniverseForm>())!;
    }

    private async Task DeleteUniverseAsync(UniverseForm universe)
    {
        using var api = Application.CreateApiClient();
        var impact = await api.GetFromJsonAsync<DeletionImpact>($"/admin/universes/{universe.Id}/deletion-impact");
        (await api.DeleteAsync($"/admin/universes/{universe.Id}?version={universe.Version}&fingerprint={Uri.EscapeDataString(impact!.Fingerprint)}"))
            .EnsureSuccessStatusCode();
    }
}
