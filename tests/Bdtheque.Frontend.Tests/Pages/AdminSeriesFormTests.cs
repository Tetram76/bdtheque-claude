using System.Globalization;
using System.Net;
using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The entry form of a series (visuel/saisie-serie/contenu.md): its blocks, its publisher and collection, the values it proposes
/// to the new editions, its saving and the three categories of its errors, told apart by role, field and text, never by
/// appearance. What it shares with the form of an album (authors, genres and universes, sort key) is fixed by the tests of that one.
/// </summary>
public sealed class AdminSeriesFormTests : BunitContext, IDisposable
{
    private static readonly Guid SeriesId = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid Canales = Guid.Parse("01900000-0000-7000-8000-0000000000b1");
    private static readonly Guid Polar = Guid.Parse("01900000-0000-7000-8000-0000000000c1");
    private static readonly Guid Dargaud = Guid.Parse("01900000-0000-7000-8000-0000000000e1");
    private static readonly Guid Ligne = Guid.Parse("01900000-0000-7000-8000-0000000000e2");
    private static readonly Guid Angle = Guid.Parse("01900000-0000-7000-8000-0000000000e3");
    private static readonly Guid Casterman = Guid.Parse("01900000-0000-7000-8000-0000000000e4");
    private static readonly Guid Ecritures = Guid.Parse("01900000-0000-7000-8000-0000000000e5");

    private static readonly SeriesForm Blacksad = new(
        SeriesId, "Blacksad",
        new SeriesContent("Blacksad", null, SeriesStatus.InProgress, 8, false, false, true, "John Blacksad, détective privé.", null,
            new SeriesEditionTemplate(Dargaud, Ligne, null, EditionCondition.Excellent, BindingType.Hardcover, BookOrientation.Portrait,
                ReadingDirection.LeftToRight, EditionFormat.Large, true),
            [Polar], [], [new ContributionContent(Canales, ContributionRole.Scenarist)]),
        12,
        new SeriesFormReferences(
            new PublisherListItem(Dargaud, "Dargaud", null),
            new PublisherCollectionListItem(Ligne, "Ligne noire", Dargaud, "Dargaud"),
            [new GenreListItem(Polar, "Polar")],
            [],
            [new AuthorListItem(Canales, "Díaz Canales", "Juan", null)]),
        new DateTimeOffset(2026, 1, 12, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 10, 8, 20, 8, 0, TimeSpan.Zero));

    // What the API proposes to a new series: its new editions propose what any new edition is proposed (modele-metier/serie.md).
    private static readonly SeriesContent Proposed = new(
        "", null, null, null, false, false, false, null, null,
        new SeriesEditionTemplate(null, null, null, EditionCondition.Excellent, BindingType.Hardcover, BookOrientation.Portrait,
            ReadingDirection.LeftToRight, EditionFormat.Standard, true),
        [], [], []);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, whose audience visit the actions of the form are recorded in.
    private readonly TestRequest _request = new(
        new Dictionary<string, string>
        {
            [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format(),
        },
        authenticated: true);

    public AdminSeriesFormTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        // The choice of a record imports its script once rendered, which these tests do not exercise.
        JSInterop.Mode = JSRuntimeMode.Loose;
        // An entry form is rendered by its circuit (InteractiveServer), which its frame marks.
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _api.Answers[$"/admin/series/{SeriesId}"] = Blacksad;
        _api.Answers["/admin/series/new"] = Proposed;
        _api.Answers["/admin/sort-keys/title"] = new TitleSortKey("Blacksad");
        _api.Answers[$"/admin/publishers/{Dargaud}/collections"] = new List<PublisherCollectionForm>
        {
            new(Angle, Dargaud, "Grand angle", 3), new(Ligne, Dargaud, "Ligne noire", 3),
        };
        _api.Answers[$"/admin/publishers/{Casterman}/collections"] = new List<PublisherCollectionForm> { new(Ecritures, Casterman, "Écritures", 1) };
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheBreadcrumbTheTitleAndTheTitleOfTheSeries()
    {
        var page = OpenSeries();

        Assert.Equal(["Administration", "Séries", "Blacksad"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal("Modifier la série", page.Find("h1").TextContent.Trim());
        Assert.Equal("Blacksad", page.Find(".entry-form-subject").TextContent.Trim());
        // Neither link to the consultation, nor deletion, nor merge: those are the list's.
        Assert.Equal([AppPaths.AdminHome, AppPaths.AdminSeries], page.FindAll("a").Select(link => link.GetAttribute("href")).Distinct());
    }

    [Fact]
    public void Blocks_EachWithItsTitle_InTheOrderOfTheForm()
    {
        var page = OpenSeries();

        Assert.Equal(["La série", "Avancement", "Calculs", "Les auteurs", "Éditeur et collection", "Genres et univers", "Résumé et notes",
                "Nouvelles éditions"],
            page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(["Identification", "Collection", "Réglages", "Contributions", "Publication", "Classement", "Lecture", "Pré-remplissage"],
            page.FindAll(".block-kicker").Select(kicker => kicker.TextContent.Trim()));
    }

    [Fact]
    public void Fields_TheSeriesAsRead()
    {
        var page = OpenSeries();

        Assert.Equal("Blacksad", page.Find("#series-title").GetAttribute("value"));
        Assert.Contains("(obligatoire)", page.Find("label[for=series-title]").TextContent);
        Assert.Equal("Blacksad", page.Find("#series-sort-key").GetAttribute("value"));
        Assert.Equal("InProgress", page.Find("#series-status").GetAttribute("value"));
        Assert.Equal("8", page.Find("#series-volumes").GetAttribute("value"));
        Assert.Equal([false, false, true], page.FindAll("input[type=checkbox]").Select(box => box.HasAttribute("checked")));
        Assert.Equal("Dargaud", page.Find("#series-publisher").GetAttribute("value"));
        Assert.Equal(Ligne.ToString(), page.Find("#series-collection").GetAttribute("value"));
        Assert.Equal("Juan Díaz Canales", page.Find(".role-credits-line .tag").TextContent.Trim());
        Assert.Equal(["Polar"], page.FindAll(".classification-tags")[0].QuerySelectorAll(".tag").Select(tag => tag.TextContent.Trim()));
        Assert.Equal("John Blacksad, détective privé.", page.Find("#series-summary").GetAttribute("value") ?? page.Find("#series-summary").TextContent);
        Assert.Equal("Série créée le 12/01/2026 11:00, modifiée le 08/10/2026 22:08.", page.Find(".entry-form-dates").TextContent.Trim());
    }

    [Fact]
    public void Status_AListOfItsValues_OrNotSet()
    {
        var page = OpenSeries();

        Assert.Equal(["— Non renseigné", "En cours", "Terminée", "Abandonnée"],
            page.FindAll("#series-status option").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void NewEditions_EachValueAList_TheColorOfThreeChoices()
    {
        var page = OpenSeries();

        Assert.Equal(["", "Excellent", "Hardcover", "Large", "Portrait", "LeftToRight", "true"],
            page.FindAll(".series-template select").Select(list => list.GetAttribute("value")));
        Assert.Equal(["—", "En couleur", "Noir et blanc"], page.FindAll("#series-color option").Select(option => option.TextContent.Trim()));
        Assert.Equal(["—", "Édition originale", "Édition spéciale", "Tirage de tête"],
            page.FindAll("#series-category option").Select(option => option.TextContent.Trim()));
        // Pre-filled only for a new series.
        Assert.Empty(page.FindAll(".series-template-notice"));
    }

    [Fact]
    public void Collection_IsChosenInTheListOfThoseOfThePublisher()
    {
        // visuel/saisie-serie/contenu.md: the name of a collection, chosen and sometimes changed by its publisher, is ill
        // remembered: the list shows them all.
        var page = OpenSeries();

        Assert.Equal(["—", "Grand angle", "Ligne noire"],
            page.FindAll("#series-collection option").Select(option => option.TextContent.Trim()));
        page.Find("#series-collection").Change(Angle.ToString());
        Assert.Equal(Angle.ToString(), page.Find("#series-collection").GetAttribute("value"));
        page.Find("#series-collection").Change("");

        Assert.Equal("", page.Find("#series-collection").GetAttribute("value"));
    }

    [Fact]
    public void Collection_WithoutPublisher_IsUnavailable_AndTheChangeOfPublisherRemovesIt_AndListsItsOwn()
    {
        var page = OpenSeries();

        ChoosePublisher(page, new RecordOption(Casterman, "Casterman", null));

        Assert.Equal("", page.Find("#series-collection").GetAttribute("value"));
        Assert.False(page.Find("#series-collection").HasAttribute("disabled"));
        Assert.Equal(["—", "Écritures"], page.FindAll("#series-collection option").Select(option => option.TextContent.Trim()));

        ChoosePublisher(page, null);

        Assert.True(page.Find("#series-collection").HasAttribute("disabled"));
        Assert.Equal(["Choisissez d'abord l'éditeur"], page.FindAll("#series-collection option").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void Collections_ThatCannotBeRead_AreATechnicalErrorUnderTheField()
    {
        // In the compact form of the single component of the errors, which the field describes itself by (#438).
        _api.Failures[$"/admin/publishers/{Casterman}/collections"] = HttpStatusCode.InternalServerError;
        var page = OpenSeries();

        ChoosePublisher(page, new RecordOption(Casterman, "Casterman", null));

        var error = page.Find("#series-collection-failure [role=alert]");
        Assert.Equal("technical", error.GetAttribute("data-error-category"));
        Assert.True(error.HasAttribute("data-error-compact"));
        Assert.Equal("Les collections de l'éditeur n'ont pas pu être lues : choisissez-le de nouveau.", error.QuerySelector("[data-error-text]")!.TextContent.Trim());
        Assert.Empty(page.FindAll("#series-collection-help"));
        Assert.Contains("series-collection-failure", page.Find("#series-collection").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Collections_ThatCouldNotBeRead_AreSoInTheStateHandedToTheInteractiveForm()
    {
        // The form is read once, by the prerendering, which hands the interactive form its persisted state ([PersistentState])
        // and nothing else: the collections not read must be so in that state, or the interactive form would present an
        // empty list of them without saying so.
        _api.Failures[$"/admin/publishers/{Dargaud}/collections"] = HttpStatusCode.InternalServerError;

        var prerendered = OpenSeries().Instance;

        Assert.NotNull(prerendered.Entry!.Publisher);
        Assert.Equal(ProblemCategory.Technical, prerendered.CollectionsProblem?.Category);
        Assert.Empty(prerendered.PublisherCollections!);
    }

    [Fact]
    public void NewSeries_EmptyWithoutDates_ItsNewEditionsPreFilled_AndSaySo()
    {
        var page = OpenNew();

        Assert.Equal("Nouvelle série", page.Find("h1").TextContent.Trim());
        Assert.Equal("<Sans titre>", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Nouvelle série", page.FindAll(".breadcrumb li")[^1].TextContent.Trim());
        Assert.Empty(page.FindAll(".entry-form-dates"));
        Assert.Equal("Nouvelle fiche", page.Find(".save-bar-state-long").TextContent);
        Assert.Equal("Créer la série", page.Find(".save-bar button[type=submit]").TextContent.Trim());
        Assert.True(page.Find("#series-collection").HasAttribute("disabled"));
        Assert.Equal(["", "Excellent", "Hardcover", "Standard", "Portrait", "LeftToRight", "true"],
            page.FindAll(".series-template select").Select(list => list.GetAttribute("value")));
        Assert.Contains("Pré-remplies avec les valeurs proposées pour toute nouvelle édition", page.Find(".series-template-notice").TextContent);
    }

    [Fact]
    public void Title_FollowsTheTyping()
    {
        var page = OpenSeries();

        page.Find("#series-title").Input("Blacksad, l'intégrale");

        Assert.Equal("Blacksad, l'intégrale", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
    }

    [Theory]
    [InlineData(RecordKind.Genre)]
    [InlineData(RecordKind.Universe)]
    [InlineData(RecordKind.Author)]
    public async Task Record_Added_TellsTheSeriesChanged(RecordKind kind)
    {
        var page = OpenSeries();
        var tags = page.FindComponents<RecordTags>().First(t => t.Instance.Kind == kind);

        await tags.InvokeAsync(() => tags.Instance.OnAdd.InvokeAsync(new RecordOption(Guid.NewGuid(), "Ajouté", null)));

        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public void Dates_JustAboveTheFirstBlock()
    {
        // Decision of the user at the functional validation (visuel/saisie-serie/contenu.md).
        var blocks = OpenSeries().Find(".entry-form-blocks").Children;

        Assert.Equal("entry-form-dates", blocks[0].ClassName);
        Assert.Contains("block", blocks[1].ClassList);
    }

    [Fact]
    public void Save_SendsTheWholeFormAtItsVersion_RecordsTheSaving_AndLeadsBackToTheList()
    {
        var page = OpenSeries();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Blacksad with { Version = 13, Content = Blacksad.Content with { IsComplete = true } });

        page.FindAll("input[type=checkbox]")[0].Change(true);
        page.Find("#series-color").Change("false");
        page.Find("form").Submit();

        // A modification saved leads back to the list (visuel/saisie-album/contenu.md).
        page.WaitForAssertion(() => Assert.Equal(AppPaths.AdminSeries, new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath));
        var (method, path, body) = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/series/{SeriesId}"), (method, path));
        var request = body.Deserialize<UpdateSeriesRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(12u, request.Version);
        Assert.True(request.Content.IsComplete);
        Assert.Equal(Blacksad.Content.EditionTemplate with { IsColor = false }, request.Content.EditionTemplate);
        Assert.Equal([Canales], request.Content.Contributions.Select(c => c.AuthorId));
        Assert.Contains("admin.series-form.saved", _audience.Functions);
    }

    [Fact]
    public void Create_LeadsBackToTheList()
    {
        var page = OpenNew();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Blacksad, HttpStatusCode.Created);

        page.Find("#series-title").Input("Blacksad");
        page.Find("form").Submit();

        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.Equal(AppPaths.AdminSeries, new Uri(navigation.Uri).AbsolutePath));
        var (method, path, body) = _api.Written.Single();
        Assert.Equal((HttpMethod.Post, "/admin/series"), (method, path));
        Assert.Equal(Proposed.EditionTemplate, body.Deserialize<SeriesContent>(JsonSerializerOptions.Web)!.EditionTemplate);
        Assert.Contains("admin.series-form.created", _audience.Functions);
    }

    [Theory]
    [InlineData("Series.TitleRequired", "/title", "series-title", "Obligatoire.")]
    [InlineData("Series.SortKeyRequired", "/manualSortKey", "series-sort-key", "Saisissez une clé, ou réinitialisez-la.")]
    [InlineData("Series.TheoreticalVolumeCountPositive", "/theoreticalVolumeCount", "series-volumes", "Supérieur à zéro.")]
    [InlineData("PublisherCollection.NotOfPublisher", "/editionTemplate/publisherCollectionId", "series-collection", "Une collection de l'éditeur choisi.")]
    public void BusinessError_ToCorrect_PointsToItsField(string rule, string pointer, string field, string message)
    {
        var page = OpenNew();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, rule, pointer);

        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("business", error.GetAttribute("data-error-category"));
        Assert.Equal("À corriger", error.QuerySelector("[data-error-label]")!.TextContent);
        Assert.Equal("true", page.Find($"#{field}").GetAttribute("aria-invalid"));
        Assert.Contains(message, page.Find($"#{field}-error").TextContent);
    }

    [Fact]
    public void FunctionalError_ModifiedMeanwhile_OffersToReloadAndToSeeTheChanges()
    {
        var page = OpenSeries();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);

        page.Find("#series-title").Input("Blacksad, l'intégrale");
        page.Find("#series-status").Change("Completed");
        ChoosePublisher(page, null);
        page.Find("#series-binding").Change("Paperback");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        Assert.Contains("La série a été modifiée entre-temps", error.TextContent);
        Assert.Equal(["Recharger la fiche", "Voir mes changements"], error.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));

        error.QuerySelectorAll("button")[1].Click();

        var changes = page.Find("dialog .changes-dialog-list");
        Assert.Equal(["Titre", "Statut", "Éditeur", "Collection éditeur", "Reliure"], changes.QuerySelectorAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["Blacksad, l'intégrale", "Terminée", "—", "—", "Brochée"], changes.QuerySelectorAll("dd").Select(dd => dd.TextContent));
        Assert.Contains("admin.series-form.changes", _audience.Functions);
    }

    [Fact]
    public void TechnicalError_KeepsTheChanges_AndOffersToTryAgain()
    {
        var page = OpenSeries();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);

        page.Find("#series-title").Input("Blacksad, l'intégrale");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("technical", error.GetAttribute("data-error-category"));
        Assert.Contains("n'a pas pu enregistrer la série", error.TextContent);
        Assert.Equal("Blacksad, l'intégrale", page.Find("#series-title").GetAttribute("value"));

        _api.Write = (_, _, _) => FakeAdminApi.Json(Blacksad);
        page.Find("[role=alert] button").Click();

        page.WaitForAssertion(() => Assert.Empty(page.FindAll("[role=alert]")));
        Assert.Equal(2, _api.Written.Count);
    }

    [Fact]
    public async Task RecordsAddedOnTheSpot_AreCreatedWithTheSeries_WhenItIsSaved()
    {
        var page = OpenSeries();
        var tags = page.FindComponents<RecordTags>().First(t => t.Instance.Kind == RecordKind.Author);
        await tags.InvokeAsync(() => tags.Instance.OnCreate.InvokeAsync("Juanjo Guarnido"));
        page.Find(".dialog-actions .button[data-variant=primary]").Click();
        Assert.Empty(_api.Written);
        Assert.Contains("admin.series-form.author-created", _audience.Functions);
        _api.Write = (_, _, _) => FakeAdminApi.Json(Blacksad);

        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Single(_api.Written));
        var added = _api.Written.Single().Body.GetProperty("content").GetProperty("newRecords");
        Assert.Equal("Guarnido", added.GetProperty("authors")[0].GetProperty("lastName").GetString());
    }

    [Fact]
    public void LoadError_IsPresentedInsteadOfTheForm()
    {
        _api.Failures[$"/admin/series/{SeriesId}"] = HttpStatusCode.InternalServerError;

        var page = OpenSeries();

        Assert.Equal("technical", page.Find("[role=alert]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll("form"));
    }

    [Fact]
    public void Screen_IsRecordedWithTheSeriesItPresents()
    {
        OpenSeries();

        var opened = _audience.Sent.Single(sent => sent.Path == "/audience/visits").Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(new FunctionUse("admin.series-form", new PresentedRecord(RecordType.Series, SeriesId), null), opened.FirstUse);
    }

    [Fact]
    public void NewSeries_Screen_IsRecorded()
    {
        OpenNew();

        var opened = _audience.Sent.Single(sent => sent.Path == "/audience/visits").Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(new FunctionUse("admin.series-form.new", null, null), opened.FirstUse);
    }

    [Fact]
    public void NewSeries_FromASearchWithoutResult_TakesTheTextSearchedAsItsTitle_ItsAutomaticSortKeyFollowing()
    {
        var page = OpenNew("Blacksad");

        Assert.Equal("Blacksad", page.Find("#series-title").GetAttribute("value"));
        Assert.Equal("Blacksad", page.Find(".entry-form-subject").TextContent.Trim());
        page.WaitForAssertion(() => Assert.Equal("Blacksad", page.Find("#series-sort-key").GetAttribute("value")));
        Assert.Contains("/admin/sort-keys/title?title=Blacksad", _api.Requested);
        Assert.Equal("Nouvelle fiche", page.Find(".save-bar-state-long").TextContent);
    }

    private IRenderedComponent<AdminSeriesForm> OpenSeries()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AdminEditSeries(SeriesId));
        return Render<AdminSeriesForm>(parameters => parameters
            .Add(p => p.Id, SeriesId)
            .AddCascadingValue(_request.Context));
    }

    private IRenderedComponent<AdminSeriesForm> OpenNew(string? searched = null)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(searched is null ? AppPaths.AdminNewSeries : AppPaths.NewWithSearched(AppPaths.AdminNewSeries, searched));
        return Render<AdminSeriesForm>(parameters => parameters.AddCascadingValue(_request.Context));
    }

    private static void ChoosePublisher(IRenderedComponent<AdminSeriesForm> page, RecordOption? publisher)
    {
        var picker = page.FindComponents<RecordPicker>().Single(p => p.Instance.Kind == RecordKind.Publisher);
        picker.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(publisher)).GetAwaiter().GetResult();
    }
}
