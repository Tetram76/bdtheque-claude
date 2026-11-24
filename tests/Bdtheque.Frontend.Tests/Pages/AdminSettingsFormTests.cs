using System.Globalization;
using System.Net;
using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using DisplayCulture = Bdtheque.Contracts.Enums.DisplayCulture;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The entry form of the setting of the application (modele-metier/parametrage-de-l-application.md), a single record on the
/// model of the entry forms: its title without a label of its own, its two settings, the example of the label of an album in
/// the format chosen, the saving, which leads back to the home of the administration, and the categories of its errors, told
/// apart by role, field and text, never by appearance. What it shares with the other entry forms (frame, save bar) is fixed by
/// the tests of the form of an album.
/// </summary>
public sealed class AdminSettingsFormTests : BunitContext, IDisposable
{
    private static readonly ApplicationSettingsForm Settings = new(
        AlbumLabelFormat.TitleSeriesVolume, DisplayCulture.FrenchFrance,
        new DateTimeOffset(2026, 1, 12, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 9, 17, 40, 0, TimeSpan.Zero),
        7);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, whose audience visit the actions of the form are recorded in.
    private readonly TestRequest _request = new(
        new Dictionary<string, string>
        {
            [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "planche", new DateOnly(2026, 11, 3)).Format(),
        },
        authenticated: true);

    public AdminSettingsFormTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        // An entry form is rendered by its circuit (InteractiveServer), which its frame marks.
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _api.Answers["/admin/settings"] = Settings;
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheBreadcrumbAndTheTitle_WithoutALabelOfItsOwn()
    {
        var page = Open();

        Assert.Equal(["Administration", "Paramétrage"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal("Modifier le paramétrage", page.Find("h1").TextContent.Trim());
        // A single record, which the title names whole.
        Assert.Empty(page.FindAll(".entry-form-subject"));
        // Its only link leads back to the home of the administration, where the setting is presented, as does « Annuler ».
        Assert.Equal([AppPaths.AdminHome], page.FindAll("a").Select(link => link.GetAttribute("href")).Distinct());
    }

    [Fact]
    public void Block_WithItsTitleAndKicker_AfterTheDatesOfTheRecord()
    {
        var page = Open();

        Assert.Equal(["L'affichage"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(["Présentation"], page.FindAll(".block-kicker").Select(kicker => kicker.TextContent.Trim()));
        // Its modification only: provisioned with the deployment, the setting is never created by anyone (decision of the user).
        Assert.Equal("Paramétrage modifié le 09/11/2026 18:40.", Normalized(page.Find(".entry-form-dates").TextContent));
    }

    [Fact]
    public void Dates_NoneBeforeAnyModification_ThatOfTheProvisioningBeingNoModification()
    {
        _api.Answers["/admin/settings"] = Settings with { ModifiedAt = Settings.CreatedAt };

        var page = Open();

        Assert.Empty(page.FindAll(".entry-form-dates"));
        Assert.Equal(["L'affichage"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
    }

    [Fact]
    public void Settings_EachARequiredList_OfItsValues_AsTheModelNamesThem()
    {
        var page = Open();

        // The mention follows the label in a note of its own, without a space of the markup (Field).
        Assert.Equal(["Libellé des albums(obligatoire)", "Culture d'affichage(obligatoire)"],
            page.FindAll(".field-label").Select(label => Normalized(label.TextContent)));
        Assert.Equal(["Album (Série - Tome)", "Tome - Album (Série)"],
            page.FindAll("#settings-album-label-format option").Select(option => option.TextContent));
        Assert.Equal(["Français (France)"], page.FindAll("#settings-display-culture option").Select(option => option.TextContent));
        Assert.Equal(nameof(AlbumLabelFormat.TitleSeriesVolume), page.Find("#settings-album-label-format").GetAttribute("value"));
        Assert.Equal(nameof(DisplayCulture.FrenchFrance), page.Find("#settings-display-culture").GetAttribute("value"));
    }

    [Fact]
    public async Task Example_LabelsAnAlbumInTheFormatChosen_AndTheFormIsUnsavedUntilItIsChosenBack()
    {
        var page = Open();

        Assert.Equal("Exemple : Le Lotus bleu (Tintin - T. 5)", Example(page));
        Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent);
        // The example is read with the list, which it describes.
        Assert.StartsWith("settings-album-label-format-example", page.Find("#settings-album-label-format").GetAttribute("aria-describedby"));

        await page.ChangeAsync("#settings-album-label-format", nameof(AlbumLabelFormat.VolumeTitleSeries));

        Assert.Equal("Exemple : T. 5 - Le Lotus bleu (Tintin)", Example(page));
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);

        await page.ChangeAsync("#settings-album-label-format", nameof(AlbumLabelFormat.TitleSeriesVolume));

        Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public async Task Save_SendsTheWholeFormAtItsVersion_AndLeadsBackToTheHomeOfTheAdministration()
    {
        var page = Open();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Settings with { AlbumLabelFormat = AlbumLabelFormat.VolumeTitleSeries, Version = 8 });

        await page.ChangeAsync("#settings-album-label-format", nameof(AlbumLabelFormat.VolumeTitleSeries));
        await page.SubmitAsync("form");

        Assert.Equal(AppPaths.AdminHome, new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath);
        var (method, path, body) = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, "/admin/settings"), (method, path));
        Assert.Equal(
            new UpdateApplicationSettingsRequest(AlbumLabelFormat.VolumeTitleSeries, DisplayCulture.FrenchFrance, 7),
            body.Deserialize<UpdateApplicationSettingsRequest>(JsonSerializerOptions.Web));
        Assert.Contains("admin.settings.saved", _audience.Functions);
    }

    [Fact]
    public async Task FunctionalError_ModifiedMeanwhile_OffersToReloadAndToSeeTheChanges()
    {
        var page = Open();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);

        await page.ChangeAsync("#settings-album-label-format", nameof(AlbumLabelFormat.VolumeTitleSeries));
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        Assert.Contains("Le paramétrage a été modifié entre-temps", error.TextContent);
        Assert.Equal(["Recharger la fiche", "Voir mes changements"], error.QuerySelectorAll("button").Select(button => button.TextContent.Trim()));

        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[1]);

        // The record named as the change log names it, its label without component.
        Assert.Equal("Paramétrage de l'application", page.Find("dialog .dialog-subject").TextContent.Trim());
        var changes = page.Find("dialog .changes-dialog-list");
        Assert.Equal(["Libellé des albums"], changes.QuerySelectorAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["Tome - Album (Série)"], changes.QuerySelectorAll("dd").Select(dd => dd.TextContent));
        Assert.Contains("admin.settings.changes", _audience.Functions);

        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[0]);

        // Reloaded whole: the setting as it is now, the changes of the form left behind.
        Assert.Equal(AppPaths.AdminSettings, new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath);
    }

    [Fact]
    public async Task TechnicalError_KeepsTheChanges_AndOffersToTryAgain()
    {
        var page = Open();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);

        await page.ChangeAsync("#settings-album-label-format", nameof(AlbumLabelFormat.VolumeTitleSeries));
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("technical", error.GetAttribute("data-error-category"));
        Assert.Contains("n'a pas pu enregistrer le paramétrage", error.TextContent);
        Assert.Equal(nameof(AlbumLabelFormat.VolumeTitleSeries), page.Find("#settings-album-label-format").GetAttribute("value"));
        Assert.Equal(AppPaths.AdminSettings, new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath);
    }

    [Fact]
    public void LoadError_IsPresentedInsteadOfTheForm()
    {
        _api.Failures["/admin/settings"] = HttpStatusCode.InternalServerError;

        var page = Open();

        Assert.Equal("technical", page.Find("[role=alert]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll("form"));
        Assert.Equal(["Administration", "Paramétrage"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public void Screen_IsRecorded_WithoutDesignatingTheSingleRecord()
    {
        Open();

        var opened = _audience.Sent.Single(sent => sent.Path == "/audience/visits").Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(new FunctionUse("admin.settings", null, null), opened.FirstUse);
    }

    private static string Example(IRenderedComponent<AdminSettingsForm> page) => Normalized(page.Find(".settings-example").TextContent);

    // A text as read, its spaces and line breaks of the markup reduced to single spaces.
    private static string Normalized(string text) =>
        string.Join(' ', text.Split((char[])[' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries));

    private IRenderedComponent<AdminSettingsForm> Open()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AdminSettings);
        return Render<AdminSettingsForm>(parameters => parameters.AddCascadingValue(_request.Context));
    }
}
