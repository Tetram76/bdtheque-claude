using System.Globalization;
using System.Net;
using System.Text.Json;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The entry form of an album (visuel/saisie-album/contenu.md): its blocks, its label and sort key following the typing,
/// its saving and the three categories of its errors, told apart by role, field and text, never by appearance.
/// </summary>
public sealed class AdminAlbumFormTests : BunitContext, IDisposable
{
    private static readonly Guid AlbumId = Guid.Parse("01900000-0000-7000-8000-000000000001");
    private static readonly Guid SeriesId = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid OtherSeriesId = Guid.Parse("01900000-0000-7000-8000-0000000000a2");
    private static readonly Guid Herge = Guid.Parse("01900000-0000-7000-8000-0000000000b1");
    private static readonly Guid Humour = Guid.Parse("01900000-0000-7000-8000-0000000000c1");
    private static readonly Guid Moulinsart = Guid.Parse("01900000-0000-7000-8000-0000000000d2");

    private static readonly AlbumForm Bijoux = new(
        AlbumId, "Bijoux de la Castafiore [Les]",
        new AlbumContent("Les Bijoux de la Castafiore", null, SeriesId, AlbumType.Regular, false, 21, null, null, 1963, null,
            "La Castafiore s'invite à Moulinsart.", null, AlbumRating.Good, [Humour], [Moulinsart],
            [new ContributionContent(Herge, ContributionRole.Scenarist), new ContributionContent(Herge, ContributionRole.Illustrator)]),
        41,
        new AlbumFormReferences(
            new SeriesListItem(SeriesId, "Tintin"),
            [new GenreListItem(Humour, "Humour")],
            [new UniverseInHierarchy(new UniverseListItem(Moulinsart, "Moulinsart", Guid.Empty, "Univers Hergé"),
                [new UniverseListItem(Guid.Empty, "Univers Hergé", null, null)])],
            [new AuthorListItem(Herge, "Remi", "Georges", "Hergé")]),
        AlbumCollectionState.InCollection,
        new DateTimeOffset(2026, 3, 4, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 8, 20, 8, 0, TimeSpan.Zero));

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

    public AdminAlbumFormTests()
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
        _api.Answers[$"/admin/albums/{AlbumId}"] = Bijoux;
        _api.Answers[$"/catalog/series/{SeriesId}"] = Series(SeriesId, "Tintin", ["Aventure"], ["Univers Hergé"]);
        _api.Answers[$"/catalog/series/{OtherSeriesId}"] = Series(OtherSeriesId, "Blacksad", ["Polar"], []);
        _api.Answers["/admin/sort-keys/title"] = new TitleSortKey("Lotus bleu [Le]");
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheBreadcrumbTheTitleAndTheLabelOfTheAlbum()
    {
        var page = OpenAlbum();

        Assert.Equal(["Administration", "Albums", "Les Bijoux de la Castafiore (Tintin - T. 21)"],
            page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal("Modifier l'album", page.Find("h1").TextContent.Trim());
        Assert.Equal("Les Bijoux de la Castafiore (Tintin - T. 21)", page.Find(".entry-form-subject").TextContent.Trim());
        // Neither link to the consultation, nor deletion, nor merge: those are the list's.
        Assert.Equal([AppPaths.AdminHome, AppPaths.AdminAlbums],
            page.FindAll("a").Select(link => link.GetAttribute("href")).Distinct());
    }

    [Fact]
    public void Blocks_EachWithItsTitle_InTheOrderOfTheForm()
    {
        var page = OpenAlbum();

        Assert.Equal(["L'album", "Tomaison", "Parution", "Les auteurs", "Genres et univers", "Résumé, notes et appréciation"],
            page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(["Identification", "Dans la série", "Calendrier", "Contributions", "Classement", "Lecture"],
            page.FindAll(".block-kicker").Select(kicker => kicker.TextContent.Trim()));
    }

    [Fact]
    public void Fields_TheAlbumAsRead()
    {
        var page = OpenAlbum();

        Assert.Equal("Tintin", page.Find("#album-series").GetAttribute("value"));
        Assert.Equal("Les Bijoux de la Castafiore", page.Find("#album-title").GetAttribute("value"));
        Assert.Empty(page.FindAll("label[for=album-title] .field-note"));
        Assert.Equal("Bijoux de la Castafiore [Les]", page.Find("#album-sort-key").GetAttribute("value"));
        Assert.Equal("Auto", page.Find(".sort-key-mode").TextContent);
        Assert.Equal("21", page.Find("#album-volume").GetAttribute("value"));
        Assert.Equal("1963", page.Find("#album-publication-year").GetAttribute("value"));
        Assert.Equal("La Castafiore s'invite à Moulinsart.", page.Find("#album-summary").GetAttribute("value") ?? page.Find("#album-summary").TextContent);
        Assert.Equal("Bien", page.Find(".rating-name").TextContent);
        Assert.Equal("Album créé le 04/03/2026 11:00, modifié le 08/11/2026 21:08.", page.Find(".entry-form-dates").TextContent.Trim());
    }

    [Fact]
    public void Authors_ALinePerRole_NothingWithoutAny()
    {
        var page = OpenAlbum();

        var roles = page.FindAll(".role-credits-line").Select(role => (
            role.QuerySelector(".role-credits-name")!.TextContent,
            string.Join(", ", role.QuerySelectorAll(".tag").Select(tag => tag.TextContent.Trim())),
            role.QuerySelector(".record-tags")!.TextContent.Trim()));
        Assert.Equal([("Scénario", "Hergé", "HergéAjouter"), ("Dessin", "Hergé", "HergéAjouter"), ("Couleurs", "", "Ajouter")], roles);
        Assert.Equal("Retirer Hergé (Scénario)", page.Find(".role-credits-line .tag-remove").GetAttribute("aria-label"));
    }

    [Fact]
    public void GenresAndUniverses_ThoseOfTheAlbum_ThenThoseOfItsSeries_AUniverseWithItsAncestors()
    {
        var page = OpenAlbum();

        var groups = page.FindAll(".classification-tags");
        Assert.Equal(["Humour"], groups[0].QuerySelectorAll(".record-tags .tag").Select(tag => tag.TextContent.Trim()));
        Assert.Equal(["Aventure"], groups[0].QuerySelectorAll(".inherited-tags .tag").Select(tag => tag.TextContent.Trim()));
        Assert.Equal(["Univers Hergé › Moulinsart"], groups[1].QuerySelectorAll(".record-tags .tag").Select(tag => tag.TextContent.Trim()));
        Assert.Equal(["Univers Hergé"], groups[1].QuerySelectorAll(".inherited-tags .tag").Select(tag => tag.TextContent.Trim()));
        // Those of the series are modified on its form: they cannot be removed here.
        Assert.Empty(page.FindAll(".inherited-tags .tag-remove"));
    }

    [Theory]
    [InlineData(AlbumCollectionState.PurchaseIntent, "intent", "Intention d'achat")]
    [InlineData(AlbumCollectionState.OutsideCollection, "out", "Hors collection")]
    public void Album_NotInTheCollection_IsMarkedByABanner(AlbumCollectionState state, string status, string name)
    {
        _api.Answers[$"/admin/albums/{AlbumId}"] = Bijoux with { CollectionState = state };

        var page = OpenAlbum();

        var banner = page.Find(".collection-status");
        Assert.Equal(status, banner.GetAttribute("data-status"));
        Assert.Equal(name, banner.QuerySelector("strong")!.TextContent);
    }

    [Fact]
    public void Album_InTheCollection_OrNew_HasNoBanner()
    {
        Assert.Empty(OpenAlbum().FindAll(".collection-status"));
        Assert.Empty(OpenNew().FindAll(".collection-status"));
    }

    [Fact]
    public void NewAlbum_EmptyWithoutDates_AndCreated()
    {
        var page = OpenNew();

        Assert.Equal("Nouvel album", page.Find("h1").TextContent.Trim());
        Assert.Equal("<Sans titre>", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Nouvel album", page.FindAll(".breadcrumb li")[^1].TextContent.Trim());
        Assert.Equal("(obligatoire)", page.Find("label[for=album-title] .field-note").TextContent);
        Assert.Empty(page.FindAll(".entry-form-dates"));
        Assert.Equal("Nouvelle fiche", page.Find(".save-bar-state-long").TextContent);
        Assert.Equal("Créer l'album", page.Find(".save-bar button[type=submit]").TextContent.Trim());
    }

    [Fact]
    public async Task Label_FollowsTheTyping()
    {
        var page = OpenAlbum();

        await page.InputAsync("#album-title", "Le Lotus bleu");
        await page.ChangeAsync("#album-volume", "5");

        Assert.Equal("Le Lotus bleu (Tintin - T. 5)", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public async Task SortKey_InTheAutomaticMode_IsTheDomainsForTheTitleTyped()
    {
        var page = OpenAlbum();

        await page.InputAsync("#album-title", "Le Lotus bleu");

        page.WaitForAssertion(() => Assert.Equal("Lotus bleu [Le]", page.Find("#album-sort-key").GetAttribute("value")));
        Assert.Contains("/admin/sort-keys/title?title=Le%20Lotus%20bleu", _api.Requested);
    }

    [Fact]
    public async Task SortKey_TypedByHand_IsManual_UntilReset()
    {
        var page = OpenAlbum();

        await page.InputAsync("#album-sort-key", "Castafiore");

        Assert.Equal("Manuelle", page.Find(".sort-key-mode").TextContent);
        await page.ClickAsync(".sort-key-reset");
        Assert.Equal("Auto", page.Find(".sort-key-mode").TextContent);
        Assert.Equal("Lotus bleu [Le]", page.Find("#album-sort-key").GetAttribute("value"));
    }

    [Fact]
    public async Task Omnibus_ShowsItsNumberAndTheVolumesCovered()
    {
        var page = OpenAlbum();
        Assert.Empty(page.FindAll("#album-range-start"));

        await page.ChangeAsync("input[type=checkbox]", true);

        Assert.Equal("Numéro", page.Find("label[for=album-volume]").TextContent.Trim());
        await page.ChangeAsync("#album-range-start", "1");
        await page.ChangeAsync("#album-range-end", "5");
        Assert.Equal("Les Bijoux de la Castafiore (Tintin - INT. - 21 [1 à 5])", page.Find(".entry-form-subject").TextContent.Trim());
    }

    [Theory]
    [InlineData(RecordKind.Genre)]
    [InlineData(RecordKind.Universe)]
    [InlineData(RecordKind.Author)]
    public async Task Record_Added_TellsTheAlbumChanged(RecordKind kind)
    {
        var page = OpenAlbum();
        var tags = page.FindComponents<RecordTags>().First(t => t.Instance.Kind == kind);

        await tags.InvokeAsync(() => tags.Instance.OnAdd.InvokeAsync(new RecordOption(Guid.NewGuid(), "Ajouté", null)));

        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public void Dates_JustAboveTheFirstBlock()
    {
        // Decision of the user at the functional validation (visuel/saisie-album/contenu.md).
        var blocks = OpenAlbum().Find(".entry-form-blocks").Children;

        Assert.Equal("entry-form-dates", blocks[0].ClassName);
        Assert.Contains("block", blocks[1].ClassList);
    }

    [Fact]
    public async Task Save_SendsTheWholeFormAtItsVersion_RecordsTheSaving_AndLeadsBackToTheList()
    {
        var page = OpenAlbum();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux with { Version = 42, Content = Bijoux.Content with { Summary = "Autre" } });

        await page.ChangeAsync("#album-summary", "Autre");
        await page.SubmitAsync("form");

        // A modification saved leads back to the list (visuel/saisie-album/contenu.md).
        Assert.Equal(AppPaths.AdminAlbums, new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath);
        var (method, path, body) = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/albums/{AlbumId}"), (method, path));
        var request = body.Deserialize<UpdateAlbumRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(41u, request.Version);
        Assert.Equal("Autre", request.Content.Summary);
        Assert.Equal([Herge, Herge], request.Content.Contributions.Select(c => c.AuthorId));
        Assert.Contains("admin.album.saved", _audience.Functions);
    }

    [Fact]
    public async Task Save_InProgress_DisablesTheForm_UntilItEnds()
    {
        // A field changed during the saving would be overwritten by the album as saved.
        var saved = new TaskCompletionSource();
        _api.Hold = (_, body) => body.ValueKind == JsonValueKind.Undefined ? Task.CompletedTask : saved.Task;
        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux);
        var page = OpenAlbum();
        await page.InputAsync("#album-title", "Le Lotus bleu");

        var saving = await page.StartSubmitAsync("form");

        Assert.True(page.Find(".entry-form-blocks").HasAttribute("disabled"));
        saved.SetResult();
        await saving.EndAsync("The saving");
        Assert.False(page.Find(".entry-form-blocks").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Universe_Added_IsSaved_EvenBeforeItsHierarchyIsRead()
    {
        // Its ancestors only name it: a saving made while they are read keeps the universe.
        var hierarchy = new TaskCompletionSource();
        var harbour = Guid.NewGuid();
        _api.Hold = (path, _) => path.EndsWith("/hierarchy", StringComparison.Ordinal) ? hierarchy.Task : Task.CompletedTask;
        _api.Answers[$"/admin/universes/{harbour}/hierarchy"] = new UniverseInHierarchy(
            new UniverseListItem(harbour, "Le port", Guid.Empty, "Univers Hergé"), [new UniverseListItem(Guid.Empty, "Univers Hergé", null, null)]);
        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux);
        var page = OpenAlbum();
        var tags = page.FindComponents<RecordTags>().Single(t => t.Instance.Kind == RecordKind.Universe);

        var adding = tags.InvokeAsync(() => tags.Instance.OnAdd.InvokeAsync(new RecordOption(harbour, "Le port", "Univers Hergé")));
        await page.SubmitAsync("form");

        Assert.Single(_api.Written);
        Assert.Contains(harbour.ToString(), _api.Written.Single().Body.GetProperty("content").GetProperty("universeIds").EnumerateArray().Select(u => u.GetString()));
        hierarchy.SetResult();
        await adding;
    }

    [Fact]
    public async Task Save_DuringTheChoiceOfASeries_WaitsForItsAuthors()
    {
        // The saving takes the form once the series chosen has given its authors: never the series without them.
        var given = new TaskCompletionSource();
        var guarnido = Guid.NewGuid();
        _api.Hold = (path, _) => path.EndsWith("/album-contributions", StringComparison.Ordinal) ? given.Task : Task.CompletedTask;
        _api.Write = (_, path, _) => path.EndsWith("/album-contributions", StringComparison.Ordinal)
            ? FakeAdminApi.Json(new SeriesAlbumContributions(true,
                [new SeriesAlbumContribution(new AuthorListItem(guarnido, "Guarnido", "Juanjo", null), ContributionRole.Illustrator)]))
            : FakeAdminApi.Json(Bijoux, HttpStatusCode.Created);
        var page = OpenNew();

        var choosing = ChooseSeriesAsync(page, new RecordOption(OtherSeriesId, "Blacksad", null));
        var saving = await page.StartSubmitAsync("form");
        given.SetResult();
        await choosing;
        await saving.EndAsync("The saving");

        Assert.Contains(_api.Written, w => w.Path == "/admin/albums");
        var saved = _api.Written.Single(w => w.Path == "/admin/albums").Body;
        Assert.Equal(guarnido.ToString(), saved.GetProperty("contributions")[0].GetProperty("authorId").GetString());
    }

    [Fact]
    public async Task Create_LeadsBackToTheList()
    {
        var page = OpenNew();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux, HttpStatusCode.Created);

        await page.InputAsync("#album-title", "Les Bijoux de la Castafiore");
        await page.SubmitAsync("form");

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(AppPaths.AdminAlbums, new Uri(navigation.Uri).AbsolutePath);
        Assert.Equal((HttpMethod.Post, "/admin/albums"), (_api.Written.Single().Method, _api.Written.Single().Path));
        Assert.Contains("admin.album.created", _audience.Functions);
    }

    [Fact]
    public async Task BusinessError_ToCorrect_PointsToItsField()
    {
        var page = OpenNew();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Album.TitleRequiredWithoutSeries", "/title");

        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("business", error.GetAttribute("data-error-category"));
        Assert.Equal("À corriger", error.QuerySelector("[data-error-label]")!.TextContent);
        Assert.Equal("Le titre est obligatoire pour un album qui n'appartient à aucune série.", error.QuerySelector("[data-error-text]")!.TextContent);
        Assert.Equal("true", page.Find("#album-title").GetAttribute("aria-invalid"));
        Assert.Contains("Obligatoire pour un album sans série.", page.Find("#album-title-error").TextContent);
    }

    [Fact]
    public async Task Error_OfASaving_IsBroughtIntoView()
    {
        // visuel/saisie-album/contenu.md: the first field to correct gets the focus, the page scrolling to it.
        var page = OpenNew();
        var reveal = JSInterop.SetupModule(module => module.Identifier == "import" && ((string)module.Arguments[0]!).EndsWith("entry-form.js"))
            .SetupVoid("revealProblem", _ => true);
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Album.TitleRequiredWithoutSeries");

        await page.SubmitAsync("form");

        Assert.Single(reveal.Invocations);
    }

    [Fact]
    public async Task FunctionalError_ModifiedMeanwhile_OffersToReloadAndToSeeTheChanges()
    {
        var page = OpenAlbum();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);

        await page.InputAsync("#album-title", "Le Lotus rouge");
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        Assert.Equal("Modifiée entre-temps", error.QuerySelector("[data-error-label]")!.TextContent);
        Assert.Contains("rien n'a été écrasé", error.TextContent);
        Assert.Equal(["Recharger la fiche", "Voir mes changements"], error.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));

        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[1]);

        var changes = page.Find("dialog .changes-dialog-list");
        Assert.Equal(["Titre"], changes.QuerySelectorAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["Le Lotus rouge"], changes.QuerySelectorAll("dd").Select(dd => dd.TextContent));
        Assert.Contains("admin.album.changes", _audience.Functions);
    }

    [Fact]
    public async Task TechnicalError_KeepsTheChanges_AndOffersToTryAgain()
    {
        var page = OpenAlbum();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);

        await page.InputAsync("#album-title", "Le Lotus rouge");
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("technical", error.GetAttribute("data-error-category"));
        Assert.Equal("Problème technique", error.QuerySelector("[data-error-label]")!.TextContent);
        Assert.Equal("Le Lotus rouge", page.Find("#album-title").GetAttribute("value"));

        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux);
        await page.ClickAsync("[role=alert] button");

        Assert.Empty(page.FindAll("[role=alert]"));
        Assert.Equal(2, _api.Written.Count);
    }

    [Fact]
    public async Task Series_Chosen_GivesWhatTheAlbumDisplaysThroughIt()
    {
        var page = OpenAlbum();

        await page.ClickAsync(".record-picker-clear");

        Assert.Empty(page.FindAll(".inherited-tags"));
        Assert.Equal("(obligatoire)", page.Find("label[for=album-title] .field-note").TextContent);
        Assert.Equal("Les Bijoux de la Castafiore (T. 21)", page.Find(".entry-form-subject").TextContent.Trim());
    }

    [Fact]
    public async Task Series_Chosen_GivesItsAuthorsAtOnce_AsTheDomainDecides_AndSaysSo()
    {
        // fonctionnel/regles-metier/initialisation-des-contributions-depuis-la-serie.md: as soon as the series is chosen.
        var guarnido = Guid.NewGuid();
        _api.Write = (_, _, _) => FakeAdminApi.Json(new SeriesAlbumContributions(true,
            [new SeriesAlbumContribution(new AuthorListItem(guarnido, "Guarnido", "Juanjo", null), ContributionRole.Illustrator)]));
        var page = OpenNew();

        await ChooseSeriesAsync(page, new RecordOption(OtherSeriesId, "Blacksad", null));

        var request = _api.Written.Single();
        Assert.Equal((HttpMethod.Post, $"/admin/series/{OtherSeriesId}/album-contributions"), (request.Method, request.Path));
        Assert.Equal((true, JsonValueKind.Null, 0), (request.Body.GetProperty("newAlbum").GetBoolean(),
            request.Body.GetProperty("previousSeriesId").ValueKind, request.Body.GetProperty("contributions").GetArrayLength()));
        Assert.Equal("Juanjo Guarnido", page.FindAll(".role-credits-line")[1].QuerySelector(".tag")!.TextContent.Trim());
        Assert.Contains("Auteurs repris de la série Blacksad", page.Find(".notice").TextContent);
    }

    [Fact]
    public async Task AuthorsTakenFromTheSeries_ThenChanged_NoLongerSaySo()
    {
        // Changed, they are personalized: the notice disappears.
        _api.Write = (_, _, _) => FakeAdminApi.Json(new SeriesAlbumContributions(true,
            [new SeriesAlbumContribution(new AuthorListItem(Herge, "Remi", "Georges", "Hergé"), ContributionRole.Scenarist)]));
        var page = OpenNew();
        await ChooseSeriesAsync(page, new RecordOption(SeriesId, "Tintin", null));
        Assert.Equal("Auteurs repris de la série Tintin. Les modifier ici ne change pas ceux de la série.",
            page.Find(".notice-text").TextContent.Trim());

        await page.ClickAsync(".role-credits-line .tag-remove");

        Assert.Empty(page.FindAll(".notice"));
    }

    [Fact]
    public async Task Series_ChosenAgainAfterNone_ComparesTheAuthorsWithTheSeriesTheyWereTakenFrom()
    {
        // A new album: series A chosen, then none (the cross), then B. Its authors are still those of A, which B replaces.
        _api.Write = (_, _, _) => FakeAdminApi.Json(new SeriesAlbumContributions(true,
            [new SeriesAlbumContribution(new AuthorListItem(Herge, "Remi", "Georges", "Hergé"), ContributionRole.Scenarist)]));
        var page = OpenNew();
        await ChooseSeriesAsync(page, new RecordOption(SeriesId, "Tintin", null));
        await ChooseSeriesAsync(page, null);

        await ChooseSeriesAsync(page, new RecordOption(OtherSeriesId, "Blacksad", null));

        var sent = _api.Written.Last().Body;
        Assert.Equal((SeriesId.ToString(), 1),
            (sent.GetProperty("previousSeriesId").GetString(), sent.GetProperty("contributions").GetArrayLength()));
    }

    [Fact]
    public async Task Series_ChosenAgain_BeforeTheAnswerForTheFirst_KeepsTheAuthorsOfTheLast()
    {
        // The circuit handles the next choice while the first one awaits: a late answer never overwrites a newer one.
        var tintin = new TaskCompletionSource();
        _api.Hold = (path, _) => path.Contains(SeriesId.ToString(), StringComparison.Ordinal) ? tintin.Task : Task.CompletedTask;
        _api.Write = (_, path, _) => FakeAdminApi.Json(new SeriesAlbumContributions(true,
        [
            path.Contains(SeriesId.ToString(), StringComparison.Ordinal)
                ? new SeriesAlbumContribution(new AuthorListItem(Herge, "Remi", "Georges", "Hergé"), ContributionRole.Scenarist)
                : new SeriesAlbumContribution(new AuthorListItem(Guid.NewGuid(), "Guarnido", "Juanjo", null), ContributionRole.Scenarist),
        ]));
        var page = OpenNew();

        var first = ChooseSeriesAsync(page, new RecordOption(SeriesId, "Tintin", null));
        await ChooseSeriesAsync(page, new RecordOption(OtherSeriesId, "Blacksad", null));
        tintin.SetResult();
        await first;

        Assert.Equal(["Juanjo Guarnido"], page.FindAll(".role-credits-line")[0].QuerySelectorAll(".tag").Select(t => t.TextContent.Trim()));
        Assert.Contains("Blacksad", page.Find(".notice").TextContent);
    }

    [Fact]
    public async Task Series_Chosen_ThatGivesNothing_LeavesTheAuthorsOfTheAlbum()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(new SeriesAlbumContributions(false, []));
        var page = OpenAlbum();

        await ChooseSeriesAsync(page, new RecordOption(OtherSeriesId, "Blacksad", null));

        var sent = _api.Written.Single().Body;
        Assert.Equal((false, SeriesId.ToString(), 2),
            (sent.GetProperty("newAlbum").GetBoolean(), sent.GetProperty("previousSeriesId").GetString(), sent.GetProperty("contributions").GetArrayLength()));
        Assert.Equal(2, page.FindAll(".role-credits-line .tag").Count);
        Assert.Empty(page.FindAll(".notice"));
    }

    [Fact]
    public async Task RecordsAddedOnTheSpot_AreCreatedWithTheAlbum_WhenItIsSaved()
    {
        // Nothing is written before the album is saved: cancelling creates none of them.
        var page = OpenAlbum();
        await CreateOnTheSpotAsync(page, RecordKind.Genre, " Polar ");
        Assert.Equal("Polar", page.Find("#album-genre-creation-name").GetAttribute("value"));
        await page.ClickAsync(".dialog-actions .button[data-variant=primary]");
        await CreateOnTheSpotAsync(page, RecordKind.Universe, "Le port");
        await page.ClickAsync(".dialog-actions .button[data-variant=primary]");
        var tags = page.FindComponents<RecordTags>().First(t => t.Instance.Kind == RecordKind.Author);
        await tags.InvokeAsync(() => tags.Instance.OnCreate.InvokeAsync("Jean Nouveau"));
        await page.ClickAsync(".dialog-actions .button[data-variant=primary]");

        Assert.Empty(page.FindAll("dialog"));
        Assert.Empty(_api.Written);
        Assert.Contains("Polar", page.FindAll(".classification-tags")[0].QuerySelectorAll(".tag").Select(t => t.TextContent.Trim()));
        Assert.Contains("Le port", page.FindAll(".classification-tags")[1].QuerySelectorAll(".tag").Select(t => t.TextContent.Trim()));
        Assert.Equal(["admin.album.genre-created", "admin.album.universe-created", "admin.album.author-created"],
            _audience.Functions.Where(f => f.EndsWith("-created", StringComparison.Ordinal)));

        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux);
        await page.SubmitAsync("form");

        Assert.Single(_api.Written);
        var added = _api.Written.Single().Body.GetProperty("content").GetProperty("newRecords");
        Assert.Equal(("Polar", "Le port", "Nouveau"), (added.GetProperty("genres")[0].GetProperty("label").GetString(),
            added.GetProperty("universes")[0].GetProperty("name").GetString(), added.GetProperty("authors")[0].GetProperty("lastName").GetString()));
    }

    [Fact]
    public async Task RecordAddedOnTheSpot_ThenRemoved_IsNotCreated()
    {
        var page = OpenAlbum();
        await CreateOnTheSpotAsync(page, RecordKind.Genre, "Polar");
        await page.ClickAsync(".dialog-actions .button[data-variant=primary]");
        await page.ClickAsync(() => page.FindAll(".classification-tags")[0].QuerySelectorAll(".tag-remove")[^1]);
        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux);

        await page.SubmitAsync("form");

        Assert.Single(_api.Written);
        Assert.Equal(JsonValueKind.Null, _api.Written.Single().Body.GetProperty("content").GetProperty("newRecords").ValueKind);
    }

    [Fact]
    public async Task BusinessError_OnAGenreCreatedOnTheSpot_DesignatesItsTag_WithTheMessageOfItsList()
    {
        // visuel/saisie-album/contenu.md (decision of the user, #413): the message the list of the genres gives, citing it.
        var page = OpenAlbum();
        await CreateOnTheSpotAsync(page, RecordKind.Genre, "polar");
        await page.ClickAsync(".dialog-actions .button[data-variant=primary]");
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Genre.LabelAlreadyUsed", "/newRecords/genres/0/label");

        await page.SubmitAsync("form");

        var genres = page.FindAll(".classification-tags")[0];
        var removal = AssertInError(genres, "polar");
        Assert.Equal("« polar » est déjà pris : deux libellés qui ne diffèrent que par la casse ou les accents désignent le même genre.",
            page.Find($"#{removal.GetAttribute("aria-describedby")}").TextContent.Trim());
        Assert.Null(Tag(genres, "Humour").GetAttribute("data-invalid"));
    }

    [Fact]
    public async Task BusinessError_OnAnAuthorCreatedOnTheSpot_DesignatesItsTags_AsTheLastSavingSentIt()
    {
        // The second author added, as the saving sent it, credited with two roles: each of its tags; not the first one.
        var page = OpenNew();
        await CreateAuthorOnTheSpotAsync(page, ContributionRole.Scenarist, "Jean Nouveau");
        await CreateAuthorOnTheSpotAsync(page, ContributionRole.Illustrator, "Juan Diaz Canales");
        var juan = page.Instance.Entry!.Cited.NewAuthors[1].Key;
        var colorists = AuthorTags(page, ContributionRole.Colorist);
        await colorists.InvokeAsync(() => colorists.Instance.OnAdd.InvokeAsync(new RecordOption(juan, "Juan Diaz Canales", null)));
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Author.DisplayNameAlreadyUsed", "/newRecords/authors/1/lastName");

        await page.SubmitAsync("form");

        var lines = page.FindAll(".role-credits-line");
        Assert.Null(Tag(lines[0], "Jean Nouveau").GetAttribute("data-invalid"));
        foreach (var line in lines.Skip(1))
        {
            var removal = AssertInError(line, "Juan Diaz Canales");
            Assert.Equal("« Juan Diaz Canales » est déjà pris : deux noms qui ne diffèrent que par la casse ou les accents désignent le même auteur.",
                line.QuerySelector($"#{removal.GetAttribute("aria-describedby")}")!.TextContent.Trim());
        }
    }

    [Fact]
    public async Task SeriesCreatedOnTheSpot_IsChosen_ItsAuthorsAskedOfTheDomain_AndCreatedWithTheAlbum_WhenItIsSaved()
    {
        // visuel/saisie-album/contenu.md (decision of the user, #414): the title typed, the series created only with the album.
        _api.Write = (_, _, _) => FakeAdminApi.Json(new SeriesAlbumContributions(false, []));
        var page = OpenNew();

        await CreateSeriesOnTheSpotAsync(page, " Les Vieux Fourneaux ");

        Assert.Empty(page.FindAll("dialog"));
        Assert.Equal("Les Vieux Fourneaux", page.Find("#album-series").GetAttribute("value"));
        Assert.Equal("admin.album.series-created", _audience.Functions.Last());
        // A series that does not exist yet is not read: its authors are asked of the domain as those of a series not created yet.
        var asked = _api.Written.Single();
        Assert.Equal((HttpMethod.Post, "/admin/series/new/album-contributions"), (asked.Method, asked.Path));
        Assert.DoesNotContain(_api.Requested, path => path.StartsWith("/catalog/series/", StringComparison.Ordinal));
        Assert.Empty(page.FindAll(".inherited-tags"));

        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux);
        await page.SubmitAsync("form");

        var content = _api.Written.Last().Body;
        var series = content.GetProperty("newRecords").GetProperty("series").EnumerateArray().Single();
        Assert.Equal("Les Vieux Fourneaux", series.GetProperty("title").GetString());
        Assert.Equal(series.GetProperty("key").GetString(), content.GetProperty("seriesId").GetString());
    }

    [Fact]
    public async Task SeriesCreatedOnTheSpot_ThenReplaced_IsNotCreated_ButSuggestedAgain()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(new SeriesAlbumContributions(false, []));
        var page = OpenAlbum();
        await CreateSeriesOnTheSpotAsync(page, "Les Vieux Fourneaux");

        await ChooseSeriesAsync(page, new RecordOption(OtherSeriesId, "Blacksad", null));

        var picker = page.FindComponents<RecordPicker>().Single(p => p.Instance.Kind == RecordKind.Series);
        Assert.Equal(["Les Vieux Fourneaux"], picker.Instance.Pending.Select(p => p.Label));
        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux);
        await page.SubmitAsync("form");
        Assert.Equal(JsonValueKind.Null, _api.Written.Last().Body.GetProperty("content").GetProperty("newRecords").ValueKind);
    }

    [Fact]
    public async Task BusinessError_OnASeriesCreatedOnTheSpot_DesignatesTheFieldOfTheSeries()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(new SeriesAlbumContributions(false, []));
        var page = OpenNew();
        await CreateSeriesOnTheSpotAsync(page, "Les Vieux Fourneaux");
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Text.TooLong", "/newRecords/series/0/title");

        await page.SubmitAsync("form");

        var field = page.Find("#album-series");
        Assert.Equal("true", field.GetAttribute("aria-invalid"));
        Assert.Contains(field.GetAttribute("aria-describedby")!.Split(' '),
            id => page.Find($"#{id}").TextContent.Trim() == "Trop long : raccourcissez ce texte.");
    }

    [Fact]
    public void LoadError_IsPresentedInsteadOfTheForm()
    {
        _api.Failures[$"/admin/albums/{AlbumId}"] = HttpStatusCode.InternalServerError;

        var page = OpenAlbum();

        Assert.Equal("technical", page.Find("[role=alert]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll("form"));
    }

    [Fact]
    public void Screen_IsRecordedWithTheAlbumItPresents()
    {
        OpenAlbum();

        var opened = _audience.Sent.Single(sent => sent.Path == "/audience/visits").Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(new FunctionUse("admin.album", new PresentedRecord(RecordType.Album, AlbumId), null), opened.FirstUse);
    }

    [Fact]
    public void NewAlbum_FromASearchWithoutResult_TakesTheTextSearchedAsItsTitle_ItsAutomaticSortKeyFollowing()
    {
        var page = OpenNew("Le Lotus bleu");

        Assert.Equal("Le Lotus bleu", page.Find("#album-title").GetAttribute("value"));
        Assert.Equal("Le Lotus bleu", page.Find(".entry-form-subject").TextContent.Trim());
        page.WaitForAssertion(() => Assert.Equal("Lotus bleu [Le]", page.Find("#album-sort-key").GetAttribute("value")));
        Assert.Contains("/admin/sort-keys/title?title=Le%20Lotus%20bleu", _api.Requested);
        Assert.Equal("Nouvelle fiche", page.Find(".save-bar-state-long").TextContent);
    }

    private IRenderedComponent<AdminAlbumForm> OpenAlbum()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AdminEditAlbum(AlbumId));
        return Render<AdminAlbumForm>(parameters => parameters
            .Add(p => p.Id, AlbumId)
            .AddCascadingValue(_request.Context));
    }

    private IRenderedComponent<AdminAlbumForm> OpenNew(string? searched = null)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(searched is null ? AppPaths.AdminNewAlbum : AppPaths.NewWithSearched(AppPaths.AdminNewAlbum, searched));
        return Render<AdminAlbumForm>(parameters => parameters.AddCascadingValue(_request.Context));
    }

    // As the choice of a record offers it for the text typed.
    private static Task CreateOnTheSpotAsync(IRenderedComponent<AdminAlbumForm> page, RecordKind kind, string text)
    {
        var tags = page.FindComponents<RecordTags>().Single(t => t.Instance.Kind == kind);
        return tags.InvokeAsync(() => tags.Instance.OnCreate.InvokeAsync(text));
    }

    // As the choice of the series offers it for the title typed, then confirmed in its dialog.
    private static async Task CreateSeriesOnTheSpotAsync(IRenderedComponent<AdminAlbumForm> page, string title)
    {
        var picker = page.FindComponents<RecordPicker>().Single(p => p.Instance.Kind == RecordKind.Series);
        await picker.InvokeAsync(() => picker.Instance.OnCreate.InvokeAsync(title));
        await page.ClickAsync(".dialog-actions .button[data-variant=primary]");
    }

    private static async Task CreateAuthorOnTheSpotAsync(IRenderedComponent<AdminAlbumForm> page, ContributionRole role, string name)
    {
        var tags = AuthorTags(page, role);
        await tags.InvokeAsync(() => tags.Instance.OnCreate.InvokeAsync(name));
        await page.ClickAsync(".dialog-actions .button[data-variant=primary]");
    }

    // The tags of the authors of a role, in the order of the roles.
    private static IRenderedComponent<RecordTags> AuthorTags(IRenderedComponent<AdminAlbumForm> page, ContributionRole role) =>
        page.FindComponents<RecordTags>().Where(t => t.Instance.Kind == RecordKind.Author).ElementAt((int)role - 1);

    // The tag in error, bordered; its removal, which gets the focus, described by its message.
    private static IElement AssertInError(IElement within, string label)
    {
        var tag = Tag(within, label);
        Assert.Equal("true", tag.GetAttribute("data-invalid"));
        var removal = tag.QuerySelector(".tag-remove")!;
        Assert.Equal("true", removal.GetAttribute("data-error-focus"));
        return removal;
    }

    private static IElement Tag(IElement within, string label) => within.QuerySelectorAll(".tag").Single(t => t.TextContent.Trim() == label);

    private static Task ChooseSeriesAsync(IRenderedComponent<AdminAlbumForm> page, RecordOption? series)
    {
        var picker = page.FindComponents<RecordPicker>().Single(p => p.Instance.Kind == RecordKind.Series);
        return picker.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(series));
    }

    private static SeriesDetail Series(Guid id, string title, string[] genres, string[] universes) => new(
        id, title, null, null, false, null, null, null, null,
        [.. genres.Select(g => new GenreListItem(Guid.NewGuid(), g))],
        [.. universes.Select(u => new UniverseInHierarchy(new UniverseListItem(Guid.NewGuid(), u, null, null), []))],
        [], new SeriesOverview(0, []), [], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
