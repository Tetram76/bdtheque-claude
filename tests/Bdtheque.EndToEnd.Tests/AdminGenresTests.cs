extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the genres of the administration (visuel/listes/liste-par-liste.md), on the application shared by the tests:
/// a genre changed in its row and a new one added in the dialog of a genre created on the spot
/// (visuel/listes/saisie-selon-l-entite.md), each
/// test naming its genres with a marker of its own, and searching it. What the list shares with the other lists (dialogs,
/// Escape, widths) is pinned by AdminAlbumsTests and AdminPublishersTests.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminGenresTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Menu_LeadsToTheList()
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Genres" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminGenres));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Genres");
    }

    [Fact]
    public async Task Edit_InItsRow_SavesTheLabel_AndLeadsBackToTheList()
    {
        await CreateGenreAsync($"Polar {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminGenres}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier Polar {_marker}" }).ClickAsync();
        var label = page.GetByRole(AriaRole.Textbox, new() { Name = $"Libellé de Polar {_marker}" });
        await Assertions.Expect(label).ToBeFocusedAsync();
        await label.FillAsync($"Roman noir {_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminGenres, $"?q={_marker}"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Roman noir {_marker}"]);
    }

    [Fact]
    public async Task New_InTheDialogOfAGenreCreatedOnTheSpot_AddsTheGenre()
    {
        var page = await OpenAsync($"{AppPaths.AdminGenres}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = "Nouveau genre" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Nouveau genre" });
        Assert.True(await dialog.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await dialog.GetByRole(AriaRole.Textbox, new() { Name = "Libellé" }).FillAsync($"Western {_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Ajouter le genre" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminGenres, $"?q={_marker}"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Western {_marker}"]);
    }

    [Fact]
    public async Task New_ALabelTakenInAnotherCaseAndWithoutItsAccents_IsRefusedInTheDialog()
    {
        await CreateGenreAsync($"Épopée {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminNewGenre}&q={_marker}");

        var label = page.GetByRole(AriaRole.Dialog, new() { Name = "Nouveau genre" }).GetByRole(AriaRole.Textbox, new() { Name = "Libellé" });
        await label.FillAsync($"EPOPEE {_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Ajouter le genre" }).ClickAsync();

        await Assertions.Expect(page.Locator("form.genre-new")).ToContainTextAsync(
            $"« EPOPEE {_marker} » est déjà pris : deux libellés qui ne diffèrent que par la casse ou les accents désignent le même genre.");
        await Assertions.Expect(label).ToHaveValueAsync($"EPOPEE {_marker}");
        await Assertions.Expect(label).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Épopée {_marker}"]);
    }

    [Fact]
    public async Task Edit_Cancelled_LeavesTheGenreAsItWas()
    {
        await CreateGenreAsync($"Humour {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminGenres}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier Humour {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = $"Libellé de Humour {_marker}" }).FillAsync("Autre chose");
        await page.GetByRole(AriaRole.Link, new() { Name = "Annuler" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminGenres, $"?q={_marker}"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Humour {_marker}"]);
    }

    [Fact]
    public async Task Deletion_ConfirmedInAModalDialog_TakesTheGenreOutOfTheList()
    {
        await CreateGenreAsync($"À supprimer {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminGenres}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur À supprimer {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();
        await page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer ce genre ?" })
            .GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminGenres, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Aucun genre ne correspond à « {_marker} ».");
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Labels(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Genres" }).Locator(".row-entry");

    private async Task CreateGenreAsync(string label)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync("/admin/genres", new CreateGenreRequest(label));
        response.EnsureSuccessStatusCode();
    }
}
