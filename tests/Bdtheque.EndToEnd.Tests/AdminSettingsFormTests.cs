extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The entry form of the setting of the application (modele-metier/parametrage-de-l-application.md), on an application of its
/// own: the setting is a single record, which every page reads to label the albums, and which these tests write. Each journey
/// of the administrator, through the browser: reaching the form from the home of the administration and from its menu, changing
/// the format of the label of the albums, and the setting modified meanwhile. Its captures are in
/// <see cref="Captures.AdminSettingsFormCaptureTests"/>.
/// </summary>
[Collection(SettingsApplication.Name)]
public sealed class AdminSettingsFormTests(SettingsApplication application) : BrowserTest<SettingsApplication>(application)
{
    [Theory, InEachTheme]
    public async Task Home_LeadsToTheForm_ByTheModifyOfEachSetting(string theme)
    {
        var page = await OpenAsync(theme, AppPaths.AdminHome);
        var settings = page.Locator(".admin-setting");
        await Assertions.Expect(settings).ToHaveCountAsync(2);

        for (var index = 0; index < 2; index++)
        {
            await settings.Nth(index).GetByRole(AriaRole.Link, new() { Name = "Modifier" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSettings));
            await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Modifier le paramétrage");
            await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
        }
    }

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheForm_ItsEntryCurrentThere(string theme)
    {
        var page = await OpenAsync(theme, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true }).ClickAsync();
        var plan = page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" });
        await plan.GetByRole(AriaRole.Link, new() { Name = "Paramétrage" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSettings));
        // The address changes before the enhanced navigation brings the page, its header with it.
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Modifier le paramétrage");
        // The entry chosen has served: the page it leads to is shown without the menu (wwwroot/popover-menus.js), which the
        // enhanced navigation would otherwise leave open over it.
        await Assertions.Expect(plan).ToBeHiddenAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Menu", Exact = true }).ClickAsync();
        await Assertions.Expect(plan.Locator("a[aria-current='page']")).ToHaveTextAsync("Paramétrage");
    }

    [Theory, InEachTheme]
    public async Task Save_ChangesTheFormatOfTheLabels_AndLeadsBackToTheHome_ThatPresentsIt(string theme)
    {
        await StartFromTheDefaultsAsync();
        var page = await OpenAsync(theme, AppPaths.AdminSettings);
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        var example = page.Locator(".settings-example");
        await Assertions.Expect(example).ToHaveTextAsync("Exemple : Le Lotus bleu (Tintin - T. 5)");

        await page.GetByLabel("Libellé des albums(obligatoire)").SelectOptionAsync(new SelectOptionValue { Label = "Tome - Album (Série)" });
        await Assertions.Expect(example).ToHaveTextAsync("Exemple : T. 5 - Le Lotus bleu (Tintin)");
        await Assertions.Expect(page.GetByText("Modifications non enregistrées")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        // Saved, the setting leads back to the home of the administration, which presents it.
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
        await Assertions.Expect(page.Locator(".admin-setting").First.Locator("dd").First).ToHaveTextAsync("Tome - Album (Série)");
        Assert.Equal(AlbumLabelFormat.VolumeTitleSeries, (await ReadAsync()).AlbumLabelFormat);
    }

    [Theory, InEachTheme]
    public async Task Save_OfASettingModifiedMeanwhile_OverwritesNothing_AndOffersToReload(string theme)
    {
        await StartFromTheDefaultsAsync();
        var page = await OpenAsync(theme, AppPaths.AdminSettings);
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        // Modified from another tab or device once the form is read.
        await SetFormatAsync(AlbumLabelFormat.VolumeTitleSeries);

        await page.GetByLabel("Libellé des albums(obligatoire)").SelectOptionAsync(new SelectOptionValue { Label = "Tome - Album (Série)" });
        await Assertions.Expect(page.GetByText("Modifications non enregistrées")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        var error = page.GetByRole(AriaRole.Alert);
        await Assertions.Expect(error).ToHaveAttributeAsync("data-error-category", "functional");
        await Assertions.Expect(error).ToContainTextAsync("Le paramétrage a été modifié entre-temps");
        Assert.Equal(AlbumLabelFormat.VolumeTitleSeries, (await ReadAsync()).AlbumLabelFormat);

        await error.GetByRole(AriaRole.Button, new() { Name = "Recharger la fiche" }).ClickAsync();

        // Reloaded, the form presents the setting as it is now.
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".settings-example")).ToHaveTextAsync("Exemple : T. 5 - Le Lotus bleu (Tintin)");
    }

    // The setting as a fresh deployment has it, whatever a test before saved: the tests of every theme share the application.
    private Task StartFromTheDefaultsAsync() => SetFormatAsync(AlbumLabelFormat.TitleSeriesVolume);

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private async Task<ApplicationSettingsForm> ReadAsync()
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<ApplicationSettingsForm>("/admin/settings"))!;
    }

    private async Task SetFormatAsync(AlbumLabelFormat format)
    {
        var current = await ReadAsync();
        using var api = Application.CreateApiClient();
        (await api.PutAsJsonAsync("/admin/settings", new UpdateApplicationSettingsRequest(format, current.DisplayCulture, current.Version)))
            .EnsureSuccessStatusCode();
    }
}

/// <summary>
/// An application whose setting the tests of its form write: every other page reads it to label the albums, so that it
/// cannot be that of the database the other tests share, nor that of the captures of the administration.
/// </summary>
public sealed class SettingsApplication : ApplicationHost
{
    public const string Name = "Settings";
}

[CollectionDefinition(SettingsApplication.Name)]
public sealed class SettingsCollection : ICollectionFixture<SettingsApplication>;
