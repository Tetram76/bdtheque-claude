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
        Assert.Contains("(facultatif dans une série)", page.Find("label[for=album-title]").TextContent);
        Assert.Equal("Bijoux de la Castafiore [Les]", page.Find("#album-sort-key").GetAttribute("value"));
        Assert.Equal("Auto", page.Find(".sort-key-mode").TextContent);
        Assert.Equal("21", page.Find("#album-volume").GetAttribute("value"));
        Assert.Equal("1963", page.Find("#album-publication-year").GetAttribute("value"));
        Assert.Equal("La Castafiore s'invite à Moulinsart.", page.Find("#album-summary").GetAttribute("value") ?? page.Find("#album-summary").TextContent);
        Assert.Equal("Bien", page.Find(".rating-name").TextContent);
        Assert.Equal("Album créé le 04/03/2026 11:00, modifié le 08/11/2026 21:08.", page.Find(".entry-form-dates").TextContent.Trim());
    }

    [Fact]
    public void Authors_ALinePerRole_PersonneWithoutAny()
    {
        var page = OpenAlbum();

        var roles = page.FindAll(".album-role").Select(role => (
            role.QuerySelector(".album-role-name")!.TextContent,
            string.Join(", ", role.QuerySelectorAll(".tag").Select(tag => tag.TextContent.Trim())),
            role.QuerySelector(".record-tags-none")?.TextContent));
        Assert.Equal([("Scénario", "Hergé", null), ("Dessin", "Hergé", null), ("Couleurs", "", "Personne")], roles);
        Assert.Equal("Retirer Hergé (Scénario)", page.Find(".album-role .tag-remove").GetAttribute("aria-label"));
    }

    [Fact]
    public void GenresAndUniverses_ThoseOfTheAlbum_ThenThoseOfItsSeries_AUniverseWithItsAncestors()
    {
        var page = OpenAlbum();

        var groups = page.FindAll(".album-tags");
        Assert.Equal(["Humour"], groups[0].QuerySelectorAll(".record-tags .tag").Select(tag => tag.TextContent.Trim()));
        Assert.Equal(["Aventure"], groups[0].QuerySelectorAll(".inherited-tags .tag").Select(tag => tag.TextContent.Trim()));
        Assert.Equal(["Univers Hergé › Moulinsart"], groups[1].QuerySelectorAll(".record-tags .tag").Select(tag => tag.TextContent.Trim()));
        Assert.Equal(["Univers Hergé"], groups[1].QuerySelectorAll(".inherited-tags .tag").Select(tag => tag.TextContent.Trim()));
        // Those of the series are modified on its form: they cannot be removed here.
        Assert.Empty(page.FindAll(".inherited-tags .tag-remove"));
    }

    [Fact]
    public void NewAlbum_EmptyWithoutDates_AndCreated()
    {
        var page = OpenNew();

        Assert.Equal("Nouvel album", page.Find("h1").TextContent.Trim());
        Assert.Equal("<Sans titre>", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Nouvel album", page.FindAll(".breadcrumb li").Last().TextContent.Trim());
        Assert.Contains("(obligatoire sans série)", page.Find("label[for=album-title]").TextContent);
        Assert.Empty(page.FindAll(".entry-form-dates"));
        Assert.Equal("Nouvelle fiche", page.Find(".save-bar-state-long").TextContent);
        Assert.Equal("Créer l'album", page.Find(".save-bar button[type=submit]").TextContent.Trim());
    }

    [Fact]
    public void Label_FollowsTheTyping()
    {
        var page = OpenAlbum();

        page.Find("#album-title").Input("Le Lotus bleu");
        page.Find("#album-volume").Change("5");

        Assert.Equal("Le Lotus bleu (Tintin - T. 5)", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public void SortKey_InTheAutomaticMode_IsTheDomainsForTheTitleTyped()
    {
        var page = OpenAlbum();

        page.Find("#album-title").Input("Le Lotus bleu");

        page.WaitForAssertion(() => Assert.Equal("Lotus bleu [Le]", page.Find("#album-sort-key").GetAttribute("value")));
        Assert.Contains("/admin/sort-keys/title?title=Le%20Lotus%20bleu", _api.Requested);
    }

    [Fact]
    public void SortKey_TypedByHand_IsManual_UntilReset()
    {
        var page = OpenAlbum();

        page.Find("#album-sort-key").Input("Castafiore");

        Assert.Equal("Manuelle", page.Find(".sort-key-mode").TextContent);
        page.Find(".sort-key-reset").Click();
        Assert.Equal("Auto", page.Find(".sort-key-mode").TextContent);
        page.WaitForAssertion(() => Assert.Equal("Lotus bleu [Le]", page.Find("#album-sort-key").GetAttribute("value")));
    }

    [Fact]
    public void Omnibus_ShowsItsNumberAndTheVolumesCovered()
    {
        var page = OpenAlbum();
        Assert.Empty(page.FindAll("#album-range-start"));

        page.FindAll("input[type=checkbox]")[0].Change(true);

        Assert.Equal("Numéro", page.Find("label[for=album-volume]").TextContent.Trim());
        page.Find("#album-range-start").Change("1");
        page.Find("#album-range-end").Change("5");
        Assert.Equal("Les Bijoux de la Castafiore (Tintin - INT. - 21 [1 à 5])", page.Find(".entry-form-subject").TextContent.Trim());
    }

    [Fact]
    public void Save_SendsTheWholeFormAtItsVersion_AndRecordsTheSaving()
    {
        var page = OpenAlbum();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux with { Version = 42, Content = Bijoux.Content with { Summary = "Autre" } });

        page.Find("#album-summary").Change("Autre");
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent));
        var (method, path, body) = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/albums/{AlbumId}"), (method, path));
        var request = body.Deserialize<UpdateAlbumRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(41u, request.Version);
        Assert.Equal("Autre", request.Content.Summary);
        Assert.Equal([Herge, Herge], request.Content.Contributions.Select(c => c.AuthorId));
        Assert.Contains("admin.album.saved", _audience.Functions);
    }

    [Fact]
    public void Save_ThatTookTheAuthorsOfTheSeries_SaysSo()
    {
        var page = OpenAlbum();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux);

        foreach (var remove in page.FindAll(".album-role .tag-remove").ToList())
            page.Find(".album-role .tag-remove").Click();
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Contains("Repris de la série Tintin", page.Find(".notice").TextContent));
    }

    [Fact]
    public void Create_LeadsToTheFormOfTheAlbumCreated()
    {
        var page = OpenNew();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux, HttpStatusCode.Created);

        page.Find("#album-title").Input("Les Bijoux de la Castafiore");
        page.Find("form").Submit();

        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.Equal(AppPaths.AdminEditAlbum(AlbumId), new Uri(navigation.Uri).AbsolutePath));
        Assert.Equal((HttpMethod.Post, "/admin/albums"), (_api.Written.Single().Method, _api.Written.Single().Path));
        Assert.Contains("admin.album.created", _audience.Functions);
    }

    [Fact]
    public void BusinessError_ToCorrect_PointsToItsField()
    {
        var page = OpenNew();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Album.TitleRequiredWithoutSeries");

        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("business", error.GetAttribute("data-error-category"));
        Assert.Equal("À corriger", error.QuerySelector("[data-error-label]")!.TextContent);
        Assert.Equal("Le titre est obligatoire pour un album qui n'appartient à aucune série.", error.QuerySelector("[data-error-text]")!.TextContent);
        Assert.Equal("true", page.Find("#album-title").GetAttribute("aria-invalid"));
        Assert.Contains("Obligatoire pour un album sans série.", page.Find("#album-title-error").TextContent);
    }

    [Fact]
    public void FunctionalError_ModifiedMeanwhile_OffersToReloadAndToSeeTheChanges()
    {
        var page = OpenAlbum();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);

        page.Find("#album-title").Input("Le Lotus rouge");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        Assert.Equal("Modifiée entre-temps", error.QuerySelector("[data-error-label]")!.TextContent);
        Assert.Contains("rien n'a été écrasé", error.TextContent);
        Assert.Equal(["Recharger la fiche", "Voir mes changements"], error.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));

        error.QuerySelectorAll("button")[1].Click();

        var changes = page.Find("dialog .album-changes");
        Assert.Equal(["Titre"], changes.QuerySelectorAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["Le Lotus rouge"], changes.QuerySelectorAll("dd").Select(dd => dd.TextContent));
        Assert.Contains("admin.album.changes", _audience.Functions);
    }

    [Fact]
    public void TechnicalError_KeepsTheChanges_AndOffersToTryAgain()
    {
        var page = OpenAlbum();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);

        page.Find("#album-title").Input("Le Lotus rouge");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("technical", error.GetAttribute("data-error-category"));
        Assert.Equal("Problème technique", error.QuerySelector("[data-error-label]")!.TextContent);
        Assert.Equal("Le Lotus rouge", page.Find("#album-title").GetAttribute("value"));

        _api.Write = (_, _, _) => FakeAdminApi.Json(Bijoux);
        page.Find("[role=alert] button").Click();

        page.WaitForAssertion(() => Assert.Empty(page.FindAll("[role=alert]")));
        Assert.Equal(2, _api.Written.Count);
    }

    [Fact]
    public void Series_Chosen_GivesWhatTheAlbumDisplaysThroughIt()
    {
        var page = OpenAlbum();

        page.Find(".record-picker-clear").Click();

        Assert.Empty(page.FindAll(".inherited-tags"));
        Assert.Contains("(obligatoire sans série)", page.Find("label[for=album-title]").TextContent);
        Assert.Equal("Les Bijoux de la Castafiore (T. 21)", page.Find(".entry-form-subject").TextContent.Trim());
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

    private IRenderedComponent<AdminAlbumForm> OpenAlbum()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AdminEditAlbum(AlbumId));
        return Render<AdminAlbumForm>(parameters => parameters
            .Add(p => p.Id, AlbumId)
            .AddCascadingValue(_request.Context));
    }

    private IRenderedComponent<AdminAlbumForm> OpenNew()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AdminNewAlbum);
        return Render<AdminAlbumForm>(parameters => parameters.AddCascadingValue(_request.Context));
    }

    private static SeriesDetail Series(Guid id, string title, string[] genres, string[] universes) => new(
        id, title, null, null, false, null, null, null, null,
        [.. genres.Select(g => new GenreListItem(Guid.NewGuid(), g))],
        [.. universes.Select(u => new UniverseInHierarchy(new UniverseListItem(Guid.NewGuid(), u, null, null), []))],
        [], new SeriesOverview(0, []), [], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
