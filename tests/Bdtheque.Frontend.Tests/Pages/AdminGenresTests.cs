using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The list of the genres of the administration (visuel/listes/liste-par-liste.md): its rows, its count, its search, a genre
/// changed in its row and a new one added in a frame above the list (visuel/listes/saisie-selon-l-entite.md), its dialogs and
/// its errors, told apart by role, link and text, never by appearance. What it shares with the list of the publishers
/// (dialogs, empty list, errors of the list) is pinned by AdminPublishersTests.
/// </summary>
public sealed class AdminGenresTests : BunitContext, IDisposable
{
    private static readonly GenreAdminListItem Adventure = new(
        Guid.Parse("01900000-0000-7000-8000-000000000001"), "Aventure", 612, 118, new DateTimeOffset(2026, 1, 12, 8, 0, 0, TimeSpan.Zero), 41);

    private static readonly GenreAdminListItem Biography = new(
        Guid.Parse("01900000-0000-7000-8000-000000000002"), "Biographie", 37, 0, new DateTimeOffset(2026, 2, 2, 8, 0, 0, TimeSpan.Zero), 42);

    private static readonly GenreAdminListItem Western = new(
        Guid.Parse("01900000-0000-7000-8000-000000000003"), "Western", 0, 0, new DateTimeOffset(2026, 10, 8, 8, 0, 0, TimeSpan.Zero), 43);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminGenresTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([Adventure, Biography, Western], totalCount: 3);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleTheNumberOfGenresAndTheFrameOfANewOne()
    {
        var page = Open("q=a");

        Assert.Equal(["Administration", "Genres"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal(("Genres", "24"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        // « Nouveau genre » opens the frame above the list, which keeps its search.
        Assert.Equal(("Nouveau genre", $"{AppPaths.AdminGenres}?q=a&new=open"),
            (Text(page.Find(".list-heading a.button")), page.Find(".list-heading a.button").GetAttribute("href")));
        Assert.Equal("Référentiel", Text(page.Find(".block-kicker")));
    }

    [Fact]
    public void Rows_TheLabelAndItsNumbers()
    {
        var rows = Open("").FindAll(".genre-row:not(.genre-row-head)").Select(row => (
            Text(row.QuerySelector(".row-entry")!),
            Text(row.QuerySelector(".genre-albums")!),
            Text(row.QuerySelector(".genre-series")!),
            row.QuerySelector(".genre-series")!.HasAttribute("data-empty")));

        // On two lines, each number is preceded by the name of its column, which the styles hide in a table.
        Assert.Equal([("Aventure", "Albums 612", "Séries 118", false), ("Biographie", "Albums 37", "Séries 0", true), ("Western", "Albums 0", "Séries 0", true)], rows);
    }

    [Fact]
    public void Rows_ChangeTheGenreInTheList_AndOpenTheirDialogsThere()
    {
        var row = Open("q=a").FindAll(".genre-row:not(.genre-row-head)")[0];
        var id = Adventure.Id;

        var edit = row.QuerySelector(".genre-actions a.button")!;
        Assert.Equal(($"{AppPaths.AdminGenres}?q=a&edit={id}", "Modifier Aventure"), (edit.GetAttribute("href"), edit.GetAttribute("aria-label")));
        // « Modifier » alone opens the record: the entry of the row is no link (visuel/liste-albums/contenu.md, Actions d'une ligne).
        Assert.Equal(["Modifier Aventure"], row.QuerySelectorAll($"a[href='{AppPaths.AdminGenres}?q=a&edit={id}']").Select(link => link.GetAttribute("aria-label")));
        Assert.Equal(
            [
                ("Voir ses œuvres dans la consultation", AppPaths.GenreWorks(id)),
                ("Fusionner avec un autre genre…", $"{AppPaths.AdminGenres}?q=a&merge={id}"),
                ("Supprimer…", $"{AppPaths.AdminGenres}?q=a&delete={id}"),
            ],
            row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"))));
    }

    [Fact]
    public void Summary_TheCountAndTheOrder()
    {
        var summary = Open("q=a").Find(".list-summary");

        Assert.Equal(("3 genres sur 24", "Triés par libellé"), (Text(summary.Children[0]), Text(summary.Children[1])));
    }

    [Fact]
    public void Edit_ChangesTheRowIntoAField_WithItsSavingAndItsCancelling()
    {
        var page = Open($"q=a&edit={Biography.Id}");

        var form = page.Find("form.genre-row");
        Assert.Equal("Biographie", form.QuerySelector("input[type='text']")!.GetAttribute("value"));
        // The label of the field, under the head of its column, is for screen readers, with its mention.
        Assert.Equal("Libellé de Biographie", form.QuerySelector("label")!.FirstChild!.TextContent.Trim());
        Assert.Equal("(obligatoire)", form.QuerySelector("label.visually-hidden .field-note")!.TextContent);
        Assert.Equal(["Enregistrer"], form.QuerySelectorAll("button[type='submit']").Select(Text));
        Assert.Equal($"{AppPaths.AdminGenres}?q=a", form.QuerySelector("a.button")!.GetAttribute("href"));
        // The other rows stay as they are, the genre changed being alone in its form.
        Assert.Equal(2, page.FindAll("div.genre-row:not(.genre-row-head)").Count);
    }

    [Fact]
    public void Edit_SavesTheLabelWithTheVersion_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(new GenreForm(Biography.Id, "Biographies", 44));
        var page = Open($"q=a&edit={Biography.Id}", recorded: true);

        page.Find("form.genre-row input[type='text']").Change("Biographies");
        page.Find("form.genre-row").Submit();

        var written = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/genres/{Biography.Id}", "Biographies", 42u),
            (written.Method, written.Path, written.Body.GetProperty("label").GetString(), written.Body.GetProperty("version").GetUInt32()));
        Assert.EndsWith($"{AppPaths.AdminGenres}?q=a", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.genre.saved", _audience.Functions);
    }

    [Fact]
    public void Edit_ALabelAlreadyTaken_IsRefusedInPlace_RemindingThatCaseAndAccentsDoNotTellTwoGenresApart()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Genre.LabelAlreadyUsed");
        var page = Open($"edit={Biography.Id}");

        page.Find("form.genre-row input[type='text']").Change("aventure");
        page.Find("form.genre-row").Submit();

        var form = page.Find("form.genre-row");
        Assert.Equal(("aventure", "true"),
            (form.QuerySelector("input[type='text']")!.GetAttribute("value"), form.QuerySelector("input[type='text']")!.GetAttribute("aria-invalid")));
        Assert.Equal(
            "« aventure » est déjà pris : deux libellés qui ne diffèrent que par la casse ou les accents désignent le même genre.",
            Text(form.QuerySelector(".field-error")!));
        Assert.EndsWith($"edit={Biography.Id}", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Edit_ALabelLeftBlank_IsRefusedInPlace_WithItsOwnText()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Genre.LabelRequired");
        var page = Open($"edit={Biography.Id}");

        page.Find("form.genre-row input[type='text']").Change(" ");
        page.Find("form.genre-row").Submit();

        var form = page.Find("form.genre-row");
        Assert.Equal("true", form.QuerySelector("input[type='text']")!.GetAttribute("aria-invalid"));
        Assert.Equal("Le libellé du genre est obligatoire.", Text(form.QuerySelector(".field-error")!));
    }

    [Fact]
    public void Edit_OfAGenreChangedMeanwhile_SaysSoInItsRow()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        var page = Open($"edit={Biography.Id}");

        page.Find("form.genre-row").Submit();

        Assert.Equal("functional", page.Find("form.genre-row [data-error-category]").GetAttribute("data-error-category"));
    }

    [Fact]
    public void Edit_OfAGenreOutsideThePage_LeadsToThePageOfTheListSearchedOnItsLabelThatShowsIt()
    {
        var id = Guid.Parse("01900000-0000-7000-8000-000000000009");
        _api.Answers[$"/admin/genres/{id}"] = new GenreForm(id, "Fantasy", 7);
        // Labels that contain it sort before it, and fill the first page of its search.
        var searched = new GenreListQuery(Text: "Fantasy");
        _api.Answers[searched.ApiPath()] = new Page<GenreAdminListItem>([Adventure, Biography], 1, AdminListQuery.PageSize, 52);
        _api.Answers[(searched with { Page = 2 }).ApiPath()] = new Page<GenreAdminListItem>(
            [Western, Western with { Id = id, Label = "Fantasy" }], 2, AdminListQuery.PageSize, 52);

        Open($"q=a&page=3&edit={id}");

        Assert.EndsWith($"{AppPaths.AdminGenres}?q=Fantasy&page=2&edit={id}", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Edit_OfAGenreThatNoPageOfItsSearchShows_LeadsNowhere_TheListShownWithoutTheChange()
    {
        // Renamed or deleted meanwhile: never a redirection to a page that does not show it, which would repeat itself.
        var id = Guid.Parse("01900000-0000-7000-8000-000000000009");
        _api.Answers[$"/admin/genres/{id}"] = new GenreForm(id, "Fantasy", 7);
        _api.Answers[new GenreListQuery(Text: "Fantasy").ApiPath()] = new Page<GenreAdminListItem>([Adventure], 1, AdminListQuery.PageSize, 1);

        var page = Open($"q=a&edit={id}");

        Assert.EndsWith($"{AppPaths.AdminGenres}?q=a&edit={id}", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Empty(page.FindAll("form.genre-row"));
        Assert.Equal(3, page.FindAll("div.genre-row:not(.genre-row-head)").Count);
    }

    [Fact]
    public void New_TheDialogOfAGenreCreatedOnTheSpot_ClosedByDefault()
    {
        Assert.Empty(Open("").FindAll("form.genre-new"));

        var dialog = Open("q=a&new=open").Find("form.genre-new dialog");

        // The dialog of a genre created on the spot from the entry form of an album (GenreCreationDialog).
        Assert.Equal(("Nouveau genre", "Libellé", "(obligatoire)"),
            (Text(dialog.QuerySelector(".dialog-title")!), dialog.QuerySelector("label")!.FirstChild!.TextContent.Trim(),
             dialog.QuerySelector("label .field-note")!.TextContent));
        Assert.Equal(
            [("Annuler", $"{AppPaths.AdminGenres}?q=a"), ("Ajouter le genre", null)],
            dialog.QuerySelectorAll(".dialog-actions .button").Select(button => (Text(button), button.GetAttribute("href"))));
        Assert.Equal("submit", dialog.QuerySelector(".dialog-actions button")!.GetAttribute("type"));
    }

    [Fact]
    public void New_AddsTheGenre_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(new GenreForm(Guid.NewGuid(), "Polar", 1), HttpStatusCode.Created);
        var page = Open("new=open", recorded: true);

        page.Find("form.genre-new input[type='text']").Change("Polar");
        page.Find("form.genre-new").Submit();

        var written = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Post, "/admin/genres", "Polar"), (written.Method, written.Path, written.Body.GetProperty("label").GetString()));
        Assert.EndsWith(AppPaths.AdminGenres, Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.genre.created", _audience.Functions);
    }

    [Fact]
    public void New_ALabelAlreadyTaken_IsRefusedInTheDialog()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Genre.LabelAlreadyUsed");
        var page = Open("new=open");

        page.Find("form.genre-new input[type='text']").Change("AVENTURE");
        page.Find("form.genre-new").Submit();

        var form = page.Find("form.genre-new");
        Assert.Equal("AVENTURE", form.QuerySelector("input[type='text']")!.GetAttribute("value"));
        Assert.Equal("true", form.QuerySelector("input[type='text']")!.GetAttribute("aria-invalid"));
        Assert.StartsWith("« AVENTURE » est déjà pris", Text(form.QuerySelector(".field-error")!));
    }

    [Fact]
    public void Deletion_ADialogThatAnnouncesTheAlbumsAndSeriesItLeaves()
    {
        var id = Adventure.Id;
        _api.Answers[$"/admin/genres/{id}/deletion-impact"] = new DeletionImpact(
            [], [new ImpactCount(EntityKind.Album, 612), new ImpactCount(EntityKind.Series, 118)], [], [], "f1");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal(("Supprimer ce genre ?", "Aventure"), (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Contains("612 albums", Text(dialog.QuerySelector(".deletion-impact")!));
    }

    [Fact]
    public void Merge_ADialogThatSearchesTheOtherGenre()
    {
        var dialog = Open($"merge={Adventure.Id}&mergeQ=e").Find("dialog");

        Assert.Equal("Fusionner deux genres", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal(
            [("Biographie", "37 albums · aucune série"), ("Western", "aucun album · aucune série")],
            dialog.QuerySelectorAll(".merge-candidate").Select(c => (Text(c.QuerySelector("strong")!), Text(c.QuerySelector("strong + span")!))));
        Assert.Equal(AppPaths.AdminMergeGenres, dialog.QuerySelector("#genre-merge-continue")!.GetAttribute("action"));
    }

    [Fact]
    public void Empty_ASearchWithoutResult_OffersToAddTheGenreSearched()
    {
        Answer([], totalCount: 0);

        var empty = Open("q=Zorglub").Find(".empty-list");

        Assert.Equal("Aucun genre ne correspond à « Zorglub ».", Text(empty.QuerySelector("strong")!));
        Assert.Equal(
            [("Effacer la recherche", AppPaths.AdminGenres), ("Créer le genre « Zorglub »", $"{AppPaths.AdminGenres}?q=Zorglub&new=open")],
            empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_TheGenreSearched_IsTheOneTheDialogAdds()
    {
        Answer([], totalCount: 0);

        var page = Open("q=Zorglub&new=open");

        Assert.Equal("Zorglub", page.Find("form.genre-new input[type='text']").GetAttribute("value"));
    }

    [Fact]
    public void Deletion_RefusedWhileASavedSearchDesignatesTheGenre_SaysWhatToDo()
    {
        var id = Western.Id;
        _api.Answers[$"/admin/genres/{id}/deletion-impact"] = new DeletionImpact([new ImpactCount(EntityKind.SavedSearch, 1)], [], [], [], "f3");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal("Suppression impossible", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal(
            ["Retirez d'abord le genre des critères de ces recherches enregistrées, puis recommencez."],
            dialog.QuerySelectorAll(".dialog-note").Select(Text));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/genres"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".genre-row"));
    }

    [Fact]
    public void APageBeyondTheLast_LeadsToTheLastOne()
    {
        _api.Answers["/admin/genres"] = new Page<GenreAdminListItem>([], 3, AdminListQuery.PageSize, 60);

        Open("q=e&page=3");

        Assert.EndsWith($"{AppPaths.AdminGenres}?q=e&page=2", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Edit_OfAGenreDeletedMeanwhile_ShowsTheListWithoutIt()
    {
        var page = Open($"edit={Guid.NewGuid()}");

        Assert.Empty(page.FindAll("form.genre-row"));
        Assert.Equal(3, page.FindAll("div.genre-row:not(.genre-row-head)").Count);
    }

    [Fact]
    public void Audience_TheActionOfItsAddress_ThenTheScreen()
    {
        Open($"q=a&edit={Adventure.Id}", recorded: true);

        Assert.Equal(["admin.genres.edit", "admin.genres"], _audience.Functions);
    }

    private IRenderedComponent<AdminGenres> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminGenres}?{criteria}");
        return Render<AdminGenres>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<GenreAdminListItem> items, int totalCount) =>
        _api.Answers["/admin/genres"] = new Page<GenreAdminListItem>(items, 1, AdminListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
