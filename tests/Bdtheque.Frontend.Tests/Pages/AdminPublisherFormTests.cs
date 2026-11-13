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

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The entry form of a publisher and of its collections (visuel/saisie-editeur/contenu.md): its blocks, its collections, each with
/// what references it, removed, restored or added until the saving, the saving itself and the three categories of its errors,
/// told apart by role, field and text, never by appearance. What it shares with the other entry forms (frame, save bar) is fixed
/// by the tests of the form of an album.
/// </summary>
public sealed class AdminPublisherFormTests : BunitContext, IDisposable
{
    private static readonly Guid PublisherId = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid Ecritures = Guid.Parse("01900000-0000-7000-8000-0000000000b1");
    private static readonly Guid FacSimile = Guid.Parse("01900000-0000-7000-8000-0000000000b2");
    private static readonly Guid Romans = Guid.Parse("01900000-0000-7000-8000-0000000000b3");
    private static readonly Guid Univers = Guid.Parse("01900000-0000-7000-8000-0000000000b4");

    private static readonly PublisherForm Casterman = new(
        PublisherId, "Casterman", "https://www.casterman.com",
        [
            new PublisherCollectionUse(Ecritures, "Écritures", 58, 3),
            new PublisherCollectionUse(FacSimile, "Fac-similé", 34, 0),
            new PublisherCollectionUse(Romans, "Les Romans (À suivre)", 0, 0),
            new PublisherCollectionUse(Univers, "Univers d'auteurs", 0, 1),
        ],
        new DateTimeOffset(2026, 1, 12, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 9, 17, 40, 0, TimeSpan.Zero),
        12);

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

    public AdminPublisherFormTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        // An entry form is rendered by its circuit (InteractiveServer), which its frame marks.
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _api.Answers[$"/admin/publishers/{PublisherId}"] = Casterman;
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheBreadcrumbTheTitleAndTheNameOfThePublisher()
    {
        var page = OpenPublisher();

        Assert.Equal(["Administration", "Éditeurs", "Casterman"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal("Modifier l'éditeur", page.Find("h1").TextContent.Trim());
        Assert.Equal("Casterman", page.Find(".entry-form-subject").TextContent.Trim());
        // Neither link to the consultation, nor deletion, nor merge: those are the list's. Its website opens apart.
        Assert.Equal(
            [AppPaths.AdminHome, AppPaths.AdminPublishers, "https://www.casterman.com"],
            page.FindAll("a").Select(link => link.GetAttribute("href")).Distinct());
    }

    [Fact]
    public void Subject_FollowsTheName_UnnamedWhileBlank()
    {
        var page = OpenPublisher();

        page.Find("#publisher-name").Input("  ");

        Assert.Equal("<Sans nom>", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public void Blocks_EachWithItsTitleAndKicker()
    {
        var page = OpenPublisher();

        Assert.Equal(["L'éditeur", "Les collections"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(["Identification", "Catalogue"], page.FindAll(".block-kicker").Select(kicker => kicker.TextContent.Trim()));
    }

    [Fact]
    public void Website_OpensInANewTab_OnlyOnceEntered()
    {
        var page = OpenPublisher();

        var open = page.Find(".publisher-website-open");
        Assert.Equal(("https://www.casterman.com", "_blank", "noopener noreferrer"),
            (open.GetAttribute("href"), open.GetAttribute("target"), open.GetAttribute("rel")));
        Assert.Equal("Ouvrir le site web", open.GetAttribute("aria-label"));

        page.Find("#publisher-website").Input("");

        Assert.Empty(page.FindAll(".publisher-website-open"));
    }

    [Fact]
    public void Collections_OneRowEach_WithWhatReferencesIt_RemovableOnlyWhenNothingDoes()
    {
        var page = OpenPublisher();

        var rows = page.FindAll(".publisher-collection");
        Assert.Equal(
            ["Écritures", "Fac-similé", "Les Romans (À suivre)", "Univers d'auteurs"],
            rows.Select(row => row.QuerySelector("input")!.GetAttribute("value")));
        Assert.Equal(
            ["58 éditions · 3 séries", "34 éditions", "Aucune édition ni série", "1 série"],
            rows.Select(row => row.QuerySelector(".publisher-collection-count")!.TextContent.Trim()));
        // A lock where the removal of a collection in use would be.
        Assert.Equal([false, false, true, false], rows.Select(row => row.QuerySelector("button[aria-label^='Retirer']") is not null));
        Assert.Equal([true, true, false, true], rows.Select(row => row.QuerySelector("[role=img]") is not null));
        Assert.Equal("Utilisée : ne peut pas être retirée", rows[0].QuerySelector("[role=img]")!.GetAttribute("aria-label"));
    }

    [Fact]
    public void RemovedCollection_StaysStruckUntilTheSaving_AndCanBeRestored()
    {
        var page = OpenPublisher();

        page.Find("button[aria-label='Retirer Les Romans (À suivre)']").Click();

        var removed = page.FindAll(".publisher-collection")[2];
        Assert.Equal("true", removed.GetAttribute("data-removed"));
        Assert.Null(removed.QuerySelector("input"));
        Assert.Equal("Les Romans (À suivre)", removed.QuerySelector(".publisher-collection-name")!.TextContent.Trim());
        Assert.Equal("Retirée à l'enregistrement", removed.QuerySelector(".publisher-collection-count")!.TextContent.Trim());
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);

        removed.QuerySelector("button")!.Click();

        Assert.Equal("Les Romans (À suivre)", page.FindAll(".publisher-collection")[2].QuerySelector("input")!.GetAttribute("value"));
        Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public void AddedCollection_IsMarkedNew_AndLeavesTheListWhenRemoved()
    {
        var page = OpenPublisher();

        page.Find(".publisher-collections-add").Click();

        var added = page.FindAll(".publisher-collection")[4];
        Assert.Equal("Nouvelle", added.QuerySelector(".tag")!.TextContent.Trim());
        Assert.Equal("Aucune édition ni série", added.QuerySelector(".publisher-collection-count")!.TextContent.Trim());

        added.QuerySelector("button[aria-label^='Retirer']")!.Click();

        Assert.Equal(4, page.FindAll(".publisher-collection").Count);
    }

    [Fact]
    public void NoCollection_IsSaid()
    {
        var page = OpenNew();

        Assert.Empty(page.FindAll(".publisher-collection"));
        Assert.Equal("Aucune collection.", page.Find(".publisher-collections-none").TextContent.Trim());
    }

    [Fact]
    public void Frame_IsMarkedInteractive_OnceRenderedByItsCircuit()
    {
        Assert.Equal("true", OpenPublisher().Find(".entry-form").GetAttribute("data-interactive"));
    }

    [Fact]
    public void Dates_JustAboveTheFirstBlock()
    {
        var blocks = OpenPublisher().Find(".entry-form-blocks").Children;

        Assert.Equal("entry-form-dates", blocks[0].ClassName);
        Assert.Contains("block", blocks[1].ClassList);
    }

    [Fact]
    public void Save_SendsTheWholeFormAtItsVersion_TheRemovedCollectionsLeftOut_AndLeadsBackToTheList()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Casterman with { Version = 13 });

        page.Find($"#publisher-collection-{FacSimile}").Input("Fac-similé (rééditions)");
        page.Find("button[aria-label='Retirer Les Romans (À suivre)']").Click();
        page.Find(".publisher-collections-add").Click();
        page.FindAll(".publisher-collection")[4].QuerySelector("input")!.Input("Sakka");
        page.Find("form").Submit();

        // A modification saved leads back to the list (visuel/saisie-album/contenu.md).
        page.WaitForAssertion(() => Assert.Equal(AppPaths.AdminPublishers, new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath));
        var (method, path, body) = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/publishers/{PublisherId}"), (method, path));
        var request = body.Deserialize<UpdatePublisherRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(12u, request.Version);
        Assert.Equal(("Casterman", "https://www.casterman.com"), (request.Content.Name, request.Content.Website));
        Assert.Equal(
            [(Ecritures, "Écritures"), (FacSimile, "Fac-similé (rééditions)"), (Univers, "Univers d'auteurs"), (null, "Sakka")],
            request.Content.Collections.Select(c => (c.Id, c.Name)));
        Assert.Contains("admin.publisher-form.saved", _audience.Functions);
    }

    [Fact]
    public void Create_LeadsBackToTheList()
    {
        var page = OpenNew();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Casterman, HttpStatusCode.Created);

        Assert.Equal(("Nouvel éditeur", "<Sans nom>", "Créer l'éditeur"),
            (page.Find("h1").TextContent.Trim(), page.Find(".entry-form-subject").TextContent.Trim(),
                page.Find("button[type=submit]").TextContent.Trim()));
        page.Find("#publisher-name").Input("Casterman");
        page.Find("form").Submit();

        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.Equal(AppPaths.AdminPublishers, new Uri(navigation.Uri).AbsolutePath));
        var (method, path, body) = _api.Written.Single();
        Assert.Equal((HttpMethod.Post, "/admin/publishers"), (method, path));
        Assert.Equal("Casterman", body.Deserialize<PublisherContent>(JsonSerializerOptions.Web)!.Name);
        Assert.Contains("admin.publisher-form.created", _audience.Functions);
    }

    [Theory]
    [InlineData("Publisher.NameRequired", "/name", "publisher-name", "Obligatoire.")]
    [InlineData("Publisher.NameAlreadyUsed", "/name", "publisher-name", "« Dargaud » est déjà le nom d'un autre éditeur.")]
    [InlineData("Publisher.WebsiteInvalid", "/website", "publisher-website", "Une adresse web complète, commençant par https:// ou http://.")]
    public void BusinessError_ToCorrect_PointsToItsField(string rule, string pointer, string field, string message)
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, rule, pointer);

        page.Find("#publisher-name").Input("Dargaud");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("business", error.GetAttribute("data-error-category"));
        Assert.Equal("true", page.Find($"#{field}").GetAttribute("aria-invalid"));
        Assert.Contains(message, page.Find($"#{field}-error").TextContent);
    }

    [Fact]
    public void BusinessError_OnACollectionName_BordersThatCollectionsRow()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "PublisherCollection.NameRequired", "/collections/1/name");

        page.Find("form").Submit();

        page.WaitForElement("[role=alert]");
        var fields = page.FindAll("[aria-invalid=true]");
        var field = Assert.Single(fields);
        // The second collection sent, by the order of the kept collections.
        Assert.Equal("Obligatoire.", page.Find($"#{field.Id}-error").TextContent);
    }

    [Fact]
    public void BusinessError_OnACollectionNamedAsAnother_TellsTheRenamedOneFromTheOneItClashesWith()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.BusinessProblem(
            "PublisherCollection.NameAlreadyUsed", fields: ["/collections/1/name"], related: ["/collections/0/name"]);

        page.Find("form").Submit();

        // Above the form, a text that needs no field to make sense.
        Assert.Equal(
            "Deux collections d'un même éditeur ne peuvent pas porter le même nom.",
            page.WaitForElement("[role=alert]").QuerySelector("[data-error-text]")!.TextContent);
        var (clashing, renamed) = (page.FindAll("[aria-invalid=true]")[0], page.FindAll("[aria-invalid=true]")[1]);
        Assert.Equal("Une autre collection de cet éditeur porte aussi ce nom.", page.Find($"#{renamed.Id}-error").TextContent);
        Assert.Equal("Une autre collection prend aussi ce nom.", page.Find($"#{clashing.Id}-error").TextContent);
        // The focus goes to the field to correct, not to the one it clashes with (wwwroot/entry-form.js).
        Assert.NotNull(clashing.Closest("[data-error-related]"));
        Assert.Null(renamed.Closest("[data-error-related]"));
    }

    [Fact]
    public void BusinessError_NameAlreadyUsed_RemindsThatTheCaseSufficesToTellTwoPublishersApart()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Publisher.NameAlreadyUsed");

        page.Find("form").Submit();

        Assert.Contains("une différence de casse suffit à les distinguer", page.WaitForElement("[role=alert]").TextContent);
    }

    [Fact]
    public void BusinessError_CollectionInUse_IsPresentedAboveTheForm()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "PublisherCollection.InUse");

        page.Find("button[aria-label='Retirer Les Romans (À suivre)']").Click();
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Contains("ne peut pas être retirée", error.TextContent);
        Assert.Empty(page.FindAll("[aria-invalid=true]"));
    }

    [Fact]
    public void FunctionalError_ModifiedMeanwhile_OffersToReloadAndToSeeTheChanges()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);

        page.Find("#publisher-name").Input("Casterman BD");
        page.Find("button[aria-label='Retirer Les Romans (À suivre)']").Click();
        page.Find(".publisher-collections-add").Click();
        page.FindAll(".publisher-collection")[4].QuerySelector("input")!.Input("Sakka");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        Assert.Contains("L'éditeur a été modifié entre-temps", error.TextContent);

        error.QuerySelectorAll("button")[1].Click();

        var changes = page.Find("dialog .changes-dialog-list");
        Assert.Equal(["Nom", "Collections"], changes.QuerySelectorAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["Casterman BD", "Écritures, Fac-similé, Univers d'auteurs, Sakka"], changes.QuerySelectorAll("dd").Select(dd => dd.TextContent));
        Assert.Contains("admin.publisher-form.changes", _audience.Functions);
    }

    [Fact]
    public void TechnicalError_KeepsTheChanges_AndOffersToTryAgain()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);

        page.Find("#publisher-name").Input("Casterman BD");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("technical", error.GetAttribute("data-error-category"));
        Assert.Contains("n'a pas pu enregistrer l'éditeur", error.TextContent);
        Assert.Equal("Casterman BD", page.Find("#publisher-name").GetAttribute("value"));
    }

    [Fact]
    public void LoadError_IsPresentedInsteadOfTheForm()
    {
        _api.Failures[$"/admin/publishers/{PublisherId}"] = HttpStatusCode.InternalServerError;

        var page = OpenPublisher();

        Assert.Equal("technical", page.Find("[role=alert]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll("form"));
    }

    [Fact]
    public void Screen_IsRecordedWithThePublisherItPresents()
    {
        OpenPublisher();

        var opened = _audience.Sent.Single(sent => sent.Path == "/audience/visits").Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(new FunctionUse("admin.publisher-form", new PresentedRecord(RecordType.Publisher, PublisherId), null), opened.FirstUse);
    }

    [Fact]
    public void NewPublisher_Screen_IsRecorded()
    {
        OpenNew();

        var opened = _audience.Sent.Single(sent => sent.Path == "/audience/visits").Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(new FunctionUse("admin.publisher-form.new", null, null), opened.FirstUse);
    }

    [Fact]
    public void NewPublisher_FromASearchWithoutResult_TakesTheTextSearchedAsItsName()
    {
        var page = OpenNew("Dargaud");

        Assert.Equal("Dargaud", page.Find("#publisher-name").GetAttribute("value"));
        Assert.Equal("Dargaud", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Nouvelle fiche", page.Find(".save-bar-state-long").TextContent);
    }

    private IRenderedComponent<AdminPublisherForm> OpenPublisher()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AdminEditPublisher(PublisherId));
        return Render<AdminPublisherForm>(parameters => parameters
            .Add(p => p.Id, PublisherId)
            .AddCascadingValue(_request.Context));
    }

    private IRenderedComponent<AdminPublisherForm> OpenNew(string? searched = null)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(searched is null ? AppPaths.AdminNewPublisher : AppPaths.NewWithSearched(AppPaths.AdminNewPublisher, searched));
        return Render<AdminPublisherForm>(parameters => parameters.AddCascadingValue(_request.Context));
    }
}
