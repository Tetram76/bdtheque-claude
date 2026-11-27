using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Deletion;
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
            new PublisherCollectionUse(Ecritures, "Écritures", 58, 1, 3, 1),
            new PublisherCollectionUse(FacSimile, "Fac-similé", 34, 0, 0, 0),
            new PublisherCollectionUse(Romans, "Les Romans (À suivre)", 0, 0, 0, 0),
            new PublisherCollectionUse(Univers, "Univers d'auteurs", 0, 0, 1, 0),
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
            [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "planche", new DateOnly(2026, 11, 3)).Format(),
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
    public async Task Subject_FollowsTheName_UnnamedWhileBlank()
    {
        var page = OpenPublisher();

        await page.InputAsync("#publisher-name", "  ");

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
    public async Task Website_OpensInANewTab_OnlyOnceEntered()
    {
        var page = OpenPublisher();

        var open = page.Find(".publisher-website-open");
        Assert.Equal(("https://www.casterman.com", "_blank", "noopener noreferrer"),
            (open.GetAttribute("href"), open.GetAttribute("target"), open.GetAttribute("rel")));
        // Its globe is its icon, the mark of a link leading out of the application
        // (visuel/charte-graphique/liens-qui-font-sortir-de-l-application.md), its name read with the announcement.
        Assert.Equal("Ouvrir le site web, site externe", open.TextContent);
        Assert.Equal("Ouvrir le site web", open.GetAttribute("title"));
        Assert.NotNull(open.QuerySelector(".external-mark svg.icon"));

        await page.InputAsync("#publisher-website", "");

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
            ["3 séries · 58 éditions · 1 intention · 1 recherche enregistrée", "34 éditions", "Inutilisée", "1 série"],
            rows.Select(row => row.QuerySelector(".publisher-collection-count")!.TextContent.Trim()));
        // A lock where the removal of a collection in use would be.
        Assert.Equal([false, false, true, false], rows.Select(row => row.QuerySelector("button[aria-label^='Retirer']") is not null));
        Assert.Equal([true, true, false, true], rows.Select(row => row.QuerySelector("[role=img]") is not null));
        Assert.Equal("Utilisée : ne peut pas être retirée", rows[0].QuerySelector("[role=img]")!.GetAttribute("aria-label"));
    }

    [Fact]
    public void Collection_DesignatedBySavedSearchesOnly_CountsThem_AndCannotBeRemoved()
    {
        // What the line shows is what decides of the removal of the collection (visuel/saisie-editeur/contenu.md, #471).
        _api.Answers[$"/admin/publishers/{PublisherId}"] = Casterman with
        {
            Collections = [new PublisherCollectionUse(Romans, "Les Romans (À suivre)", 0, 0, 0, 2)],
        };
        var page = OpenPublisher();

        var row = page.Find(".publisher-collection");
        Assert.Equal("2 recherches enregistrées", row.QuerySelector(".publisher-collection-count")!.TextContent.Trim());
        Assert.Null(row.QuerySelector("button[aria-label^='Retirer']"));
        Assert.NotNull(row.QuerySelector("[role=img]"));
        Assert.Equal(
            "Elles s'enregistrent avec l'éditeur. Une collection que désigne une série, une édition, une intention d'acquisition ou une recherche enregistrée ne peut pas être retirée.",
            page.Find(".publisher-help").TextContent.Trim());
    }

    [Fact]
    public void Mentions_TheNameAndTheNameOfEachCollectionRequired_TheWebsiteOptional()
    {
        // The name of a collection has no label shown: its mention is for screen readers (visuel/saisie-album/contenu.md, mentions).
        var page = OpenPublisher();

        Assert.Equal("(obligatoire)", page.Find("label[for=publisher-name] .field-note").TextContent);
        Assert.Empty(page.FindAll("label[for=publisher-website] .field-note"));
        Assert.All(page.FindAll(".publisher-collection label"), label =>
        {
            Assert.Equal("visually-hidden", label.GetAttribute("class"));
            Assert.Equal("(obligatoire)", label.QuerySelector(".field-note")?.TextContent);
        });
    }

    [Fact]
    public async Task RemovedCollection_StaysStruckUntilTheSaving_AndCanBeRestored()
    {
        var page = OpenPublisher();

        await page.ClickAsync("button[aria-label='Retirer Les Romans (À suivre)']");

        var removed = page.FindAll(".publisher-collection")[2];
        Assert.Equal("true", removed.GetAttribute("data-removed"));
        Assert.Null(removed.QuerySelector("input"));
        Assert.Equal("Les Romans (À suivre)", removed.QuerySelector(".publisher-collection-name")!.TextContent.Trim());
        Assert.Equal("Retirée à l'enregistrement", removed.QuerySelector(".publisher-collection-count")!.TextContent.Trim());
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);

        await page.ClickAsync(() => page.FindAll(".publisher-collection")[2].QuerySelector("button")!);

        Assert.Equal("Les Romans (À suivre)", page.FindAll(".publisher-collection")[2].QuerySelector("input")!.GetAttribute("value"));
        Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public async Task AddedCollection_IsMarkedNew_AndLeavesTheListWhenRemoved()
    {
        var page = OpenPublisher();

        await page.ClickAsync(".publisher-collections-add");

        var added = page.FindAll(".publisher-collection")[4];
        Assert.Equal("Nouvelle", added.QuerySelector(".tag")!.TextContent.Trim());
        Assert.Equal("Inutilisée", added.QuerySelector(".publisher-collection-count")!.TextContent.Trim());

        await page.ClickAsync(() => page.FindAll(".publisher-collection")[4].QuerySelector("button[aria-label^='Retirer']")!);

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
    public async Task Save_SendsTheWholeFormAtItsVersion_TheRemovedCollectionsLeftOut_AndLeadsBackToTheList()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Casterman with { Version = 13 });

        await page.InputAsync($"#publisher-collection-{FacSimile}", "Fac-similé (rééditions)");
        await page.ClickAsync("button[aria-label='Retirer Les Romans (À suivre)']");
        await page.ClickAsync(".publisher-collections-add");
        await page.InputAsync(() => page.FindAll(".publisher-collection")[4].QuerySelector("input")!, "Sakka");
        await page.SubmitAsync("form");

        // A modification saved leads back to the list (visuel/saisie-album/contenu.md).
        Assert.Equal(AppPaths.AdminPublishers, new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath);
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
    public async Task Create_LeadsBackToTheList()
    {
        var page = OpenNew();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Casterman, HttpStatusCode.Created);

        Assert.Equal(("Nouvel éditeur", "<Sans nom>", "Créer l'éditeur"),
            (page.Find("h1").TextContent.Trim(), page.Find(".entry-form-subject").TextContent.Trim(),
                page.Find("button[type=submit]").TextContent.Trim()));
        await page.InputAsync("#publisher-name", "Casterman");
        await page.SubmitAsync("form");

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(AppPaths.AdminPublishers, new Uri(navigation.Uri).AbsolutePath);
        var (method, path, body) = _api.Written.Single();
        Assert.Equal((HttpMethod.Post, "/admin/publishers"), (method, path));
        Assert.Equal("Casterman", body.Deserialize<PublisherContent>(JsonSerializerOptions.Web)!.Name);
        Assert.Contains("admin.publisher-form.created", _audience.Functions);
    }

    [Theory]
    [InlineData("Publisher.NameRequired", "/name", "publisher-name", "Obligatoire.")]
    [InlineData("Publisher.NameAlreadyUsed", "/name", "publisher-name",
        "« Dargaud » est déjà pris : deux noms qui ne diffèrent que par la casse ou les accents désignent le même éditeur.")]
    [InlineData("Publisher.WebsiteInvalid", "/website", "publisher-website", "Une adresse web complète, commençant par https:// ou http://.")]
    public async Task BusinessError_ToCorrect_PointsToItsField(string rule, string pointer, string field, string message)
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, rule, pointer);

        await page.InputAsync("#publisher-name", "Dargaud");
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("business", error.GetAttribute("data-error-category"));
        Assert.Equal("true", page.Find($"#{field}").GetAttribute("aria-invalid"));
        Assert.Contains(message, page.Find($"#{field}-error").TextContent);
    }

    [Fact]
    public async Task BusinessError_OnACollectionName_BordersThatCollectionsRow()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "PublisherCollection.NameRequired", "/collections/1/name");

        await page.SubmitAsync("form");

        page.Find("[role=alert]");
        var fields = page.FindAll("[aria-invalid=true]");
        var field = Assert.Single(fields);
        // The second collection sent, by the order of the kept collections.
        Assert.Equal("Obligatoire.", page.Find($"#{field.Id}-error").TextContent);
    }

    [Fact]
    public async Task BusinessError_OnACollectionNamedAsAnother_TellsTheRenamedOneFromTheOneItClashesWith()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.BusinessProblem(
            "PublisherCollection.NameAlreadyUsed", fields: ["/collections/1/name"], related: ["/collections/0/name"]);

        await page.InputAsync($"#publisher-collection-{FacSimile}", " écritures ");
        await page.SubmitAsync("form");

        // Above the form, a text that needs no field to make sense.
        Assert.Equal(
            "Deux collections d'un même éditeur ne peuvent pas porter le même nom, sans tenir compte de la casse ni des accents.",
            page.Find("[role=alert]").QuerySelector("[data-error-text]")!.TextContent);
        var (clashing, renamed) = (page.FindAll("[aria-invalid=true]")[0], page.FindAll("[aria-invalid=true]")[1]);
        Assert.Equal(
            "« écritures » est déjà pris : deux noms qui ne diffèrent que par la casse ou les accents désignent la même collection de cet éditeur.",
            page.Find($"#{renamed.Id}-error").TextContent);
        Assert.Equal("Une autre collection prend aussi ce nom, à la casse ou aux accents près.", page.Find($"#{clashing.Id}-error").TextContent);
        // The focus goes to the field to correct, not to the one it clashes with (wwwroot/entry-form.js).
        Assert.NotNull(clashing.Closest("[data-error-related]"));
        Assert.Null(renamed.Closest("[data-error-related]"));
    }

    [Fact]
    public async Task BusinessError_NameAlreadyUsed_RemindsThatNeitherCaseNorAccentsTellTwoPublishersApart()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Publisher.NameAlreadyUsed");

        await page.SubmitAsync("form");

        Assert.Equal(
            "Deux éditeurs ne peuvent pas porter le même nom, sans tenir compte de la casse ni des accents.",
            page.Find("[role=alert]").QuerySelector("[data-error-text]")!.TextContent);
    }

    [Fact]
    public async Task BusinessError_CollectionInUse_NamesTheCollection_AndPresentsWhatBlocksItLineByLine()
    {
        // A removal refused as any deletion is (fonctionnel/regles-metier/suppression-des-entites.md), naming the collection
        // (visuel/saisie-editeur/contenu.md, decision of the user, #471).
        var page = OpenPublisher();
        _api.Write = (_, _, _) => CollectionInUse(Romans, new ImpactCount(EntityKind.Edition, 2), new ImpactCount(EntityKind.SavedSearch, 1));

        await page.ClickAsync("button[aria-label='Retirer Les Romans (À suivre)']");
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal(
            "La collection « Les Romans (À suivre) » ne peut pas être retirée : elle est encore utilisée par",
            error.QuerySelector("[data-error-text]")!.TextContent);
        Assert.Equal(
            ["2 éditions : rattachez-les à une autre collection, ou supprimez-les ;", "1 recherche enregistrée : retirez la collection de ses critères."],
            error.QuerySelectorAll("[data-error-impact] li").Select(item => item.TextContent.Trim()));
        Assert.Empty(page.FindAll("[aria-invalid=true]"));
    }

    [Theory]
    [InlineData(EntityKind.Series, 1, "1 série : retirez la collection des valeurs de ses nouvelles éditions.")]
    [InlineData(EntityKind.Series, 2, "2 séries : retirez la collection des valeurs de leurs nouvelles éditions.")]
    [InlineData(EntityKind.Edition, 1, "1 édition : rattachez-la à une autre collection, ou supprimez-la.")]
    [InlineData(EntityKind.PurchaseIntent, 1, "1 intention d'acquisition : rattachez son édition à une autre collection, ou retirez-la.")]
    [InlineData(EntityKind.PurchaseIntent, 2, "2 intentions d'acquisition : rattachez leurs éditions à une autre collection, ou retirez-les.")]
    [InlineData(EntityKind.SavedSearch, 2, "2 recherches enregistrées : retirez la collection de leurs critères.")]
    public async Task BusinessError_CollectionInUse_SaysWhatToDoOfEachTypeBlocking(EntityKind kind, int count, string line)
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => CollectionInUse(Romans, new ImpactCount(kind, count));

        await page.ClickAsync("button[aria-label='Retirer Les Romans (À suivre)']");
        await page.SubmitAsync("form");

        Assert.Equal(line, page.Find("[data-error-impact] li").TextContent.Trim());
    }

    // The refusal of the removal of a collection, as the API answers it: the impact of its deletion, and the collection.
    private static HttpResponseMessage CollectionInUse(Guid collection, params ImpactCount[] blockedBy) =>
        new(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(
                new Dictionary<string, object>
                {
                    ["type"] = ProblemTypes.Business, ["status"] = 422, [ProblemTypes.RuleCodeExtension] = "PublisherCollection.InUse",
                    [ProblemTypes.ImpactExtension] = new DeletionImpact(blockedBy, [], [], [], "fingerprint"),
                    [ProblemTypes.RecordExtension] = collection,
                },
                options: JsonSerializerOptions.Web,
                mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
        };

    [Fact]
    public async Task FunctionalError_ModifiedMeanwhile_OffersToReloadAndToSeeTheChanges()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);

        await page.InputAsync("#publisher-name", "Casterman BD");
        await page.ClickAsync("button[aria-label='Retirer Les Romans (À suivre)']");
        await page.ClickAsync(".publisher-collections-add");
        await page.InputAsync(() => page.FindAll(".publisher-collection")[4].QuerySelector("input")!, "Sakka");
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        Assert.Contains("L'éditeur a été modifié entre-temps", error.TextContent);

        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[1]);

        var changes = page.Find("dialog .changes-dialog-list");
        Assert.Equal(["Nom", "Collections"], changes.QuerySelectorAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["Casterman BD", "Écritures, Fac-similé, Univers d'auteurs, Sakka"], changes.QuerySelectorAll("dd").Select(dd => dd.TextContent));
        Assert.Contains("admin.publisher-form.changes", _audience.Functions);
    }

    [Fact]
    public async Task TechnicalError_KeepsTheChanges_AndOffersToTryAgain()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);

        await page.InputAsync("#publisher-name", "Casterman BD");
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
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

    [Fact]
    public async Task Datum_ChangedSinceTheReading_IsMarkedAfterItsLabel_UntilSetBackToWhatWasRead()
    {
        // visuel/saisie-album/contenu.md, données modifiées (#355): the mark compares what is entered to what was read.
        var page = OpenPublisher();
        Assert.Empty(page.FindAll(".modified-mark"));

        await page.InputAsync("#publisher-website", "https://www.casterman.fr");

        Assert.Equal("Modifié", page.Find("label[for=publisher-website] .modified-mark").TextContent);
        Assert.Single(page.FindAll(".modified-mark"));

        await page.InputAsync("#publisher-website", "https://www.casterman.com");

        Assert.Empty(page.FindAll(".modified-mark"));
    }

    [Fact]
    public async Task RenamedCollection_IsMarkedRightAfterItsName_TheMarkReadWithItsHiddenLabel()
    {
        var page = OpenPublisher();

        await page.InputAsync($"#publisher-collection-{FacSimile}", "Fac-similé (rééditions)");

        var row = page.FindAll(".publisher-collection")[1];
        // Seen beside the name, which has no label shown; read once, with the label of the field, by screen readers.
        var seen = row.QuerySelector(".publisher-collection-mark")!;
        Assert.Equal("true", seen.GetAttribute("aria-hidden"));
        Assert.Equal("Modifié", seen.QuerySelector(".modified-mark")!.TextContent);
        Assert.NotNull(row.QuerySelector($"label.visually-hidden[for=publisher-collection-{FacSimile}] .modified-mark"));
        Assert.Equal(2, page.FindAll(".modified-mark").Count);

        await page.InputAsync($"#publisher-collection-{FacSimile}", "Fac-similé");

        Assert.Empty(page.FindAll(".modified-mark"));
    }

    [Fact]
    public async Task CollectionAddedOrRemoved_KeepsItsOwnMention_WithoutTheMark()
    {
        var page = OpenPublisher();

        await page.ClickAsync("button[aria-label='Retirer Les Romans (À suivre)']");
        await page.ClickAsync(".publisher-collections-add");

        Assert.Equal("Nouvelle", page.FindAll(".publisher-collection")[4].QuerySelector(".tag")!.TextContent.Trim());
        Assert.Equal("Retirée à l'enregistrement", page.FindAll(".publisher-collection")[2].QuerySelector(".publisher-collection-count")!.TextContent.Trim());
        Assert.Empty(page.FindAll(".modified-mark"));
    }

    [Fact]
    public async Task NewPublisher_HasNoMark_AllOfItBeingNew()
    {
        var page = OpenNew();

        await page.InputAsync("#publisher-name", "Casterman");
        await page.ClickAsync(".publisher-collections-add");
        await page.InputAsync(() => page.Find(".publisher-collection input"), "Écritures");

        Assert.Empty(page.FindAll(".modified-mark"));
    }

    [Fact]
    public async Task Reload_AfterAModificationMeanwhile_OffersUnderEachDatum_OnlyWhatDiffersFromTheRecordReloaded()
    {
        // visuel/saisie-album/contenu.md (#329): the record reloaded within the page, what the user had entered offered again.
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        await page.InputAsync("#publisher-name", "Casterman BD");
        await page.InputAsync("#publisher-website", "https://www.casterman.fr");
        await page.SubmitAsync("form");
        // The other modification entered the same website, and renamed a collection the user did not touch.
        _api.Answers[$"/admin/publishers/{PublisherId}"] = Casterman with
        {
            Website = "https://www.casterman.fr",
            Collections = [.. Casterman.Collections.Select(c => c.Id == Univers ? c with { Name = "Univers" } : c)],
            Version = 13,
        };

        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[0]);

        Assert.Empty(page.FindAll("[role=alert]"));
        Assert.Equal("Casterman", page.Find("#publisher-name").GetAttribute("value"));
        Assert.Equal("Univers", page.Find($"#publisher-collection-{Univers}").GetAttribute("value"));
        var line = page.Find("#publisher-name-redo");
        Assert.Equal("Votre saisie : Casterman BD", line.QuerySelector("span")!.TextContent.Trim());
        Assert.Equal(["Reprendre", "Rejeter"], line.QuerySelectorAll("button").Select(b => b.TextContent));
        Assert.Single(page.FindAll(".redo-line"));
        Assert.Empty(page.FindAll(".modified-mark"));
        Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public async Task Taking_WhatWasEntered_PutsItBack_TheDatumThenMarked()
    {
        var page = await ReloadedAfterAsync(form => form.InputAsync("#publisher-name", "Casterman BD"));

        await page.ClickAsync(() => page.Find("#publisher-name-redo").QuerySelectorAll("button")[0]);

        Assert.Equal("Casterman BD", page.Find("#publisher-name").GetAttribute("value"));
        Assert.Empty(page.FindAll(".redo-line"));
        Assert.NotNull(page.Find("label[for=publisher-name] .modified-mark"));
    }

    [Fact]
    public async Task Rejecting_WhatWasEntered_LeavesTheRecordReloaded()
    {
        var page = await ReloadedAfterAsync(form => form.InputAsync("#publisher-name", "Casterman BD"));

        await page.ClickAsync(() => page.Find("#publisher-name-redo").QuerySelectorAll("button")[1]);

        Assert.Equal("Casterman", page.Find("#publisher-name").GetAttribute("value"));
        Assert.Empty(page.FindAll(".redo-line"));
    }

    [Fact]
    public async Task Taking_TheNameEnteredOfACollection_RenamesItAgain_UnderItsName()
    {
        var page = await ReloadedAfterAsync(form => form.InputAsync($"#publisher-collection-{FacSimile}", "Fac-similé (rééditions)"));

        var line = page.Find($"#publisher-collection-{FacSimile}-redo");
        Assert.Equal("Votre saisie : Fac-similé (rééditions)", line.QuerySelector("span")!.TextContent.Trim());
        Assert.Single(page.FindAll(".redo-line"));

        await page.ClickAsync(() => page.Find($"#publisher-collection-{FacSimile}-redo").QuerySelectorAll("button")[0]);

        Assert.Equal("Fac-similé (rééditions)", page.Find($"#publisher-collection-{FacSimile}").GetAttribute("value"));
        Assert.Empty(page.FindAll(".redo-line"));
        Assert.NotNull(page.FindAll(".publisher-collection")[1].QuerySelector(".publisher-collection-mark .modified-mark"));
    }

    [Fact]
    public async Task NameEnteredOfACollection_RemovedSince_IsNoLongerOffered_NorWarnedOfOnSaving()
    {
        // A collection removed presents no field for its name: its name entered before the reload is no longer offered.
        var page = await ReloadedAfterAsync(form => form.InputAsync($"#publisher-collection-{Romans}", "Les Romans"));
        _api.Write = (_, _, _) => FakeAdminApi.Json(Casterman with { Version = 13 });

        await page.ClickAsync("button[aria-label='Retirer Les Romans (À suivre)']");

        Assert.Empty(page.FindAll(".redo-line"));
        var written = _api.Written.Count;
        await page.SubmitAsync("form");
        Assert.Empty(page.FindAll("dialog"));
        Assert.Equal(written + 1, _api.Written.Count);
    }
    [Fact]
    public async Task Taking_TheCollectionsEntered_AddsAndRemovesThemAgain_UnderTheirList()
    {
        var page = await ReloadedAfterAsync(async form =>
        {
            await form.ClickAsync("button[aria-label='Retirer Les Romans (À suivre)']");
            await form.ClickAsync(".publisher-collections-add");
            await form.InputAsync(() => form.FindAll(".publisher-collection")[4].QuerySelector("input")!, "Sakka");
        });
        Assert.Equal(4, page.FindAll(".publisher-collection").Count);

        var line = page.Find("#publisher-collections-redo");
        Assert.Equal("Votre saisie : Écritures, Fac-similé, Univers d'auteurs, Sakka", line.QuerySelector("span")!.TextContent.Trim());
        Assert.Single(page.FindAll(".redo-line"));

        await page.ClickAsync(() => page.Find("#publisher-collections-redo").QuerySelectorAll("button")[0]);

        var rows = page.FindAll(".publisher-collection");
        Assert.Equal(5, rows.Count);
        Assert.Equal("true", rows[2].GetAttribute("data-removed"));
        Assert.Equal("Nouvelle", rows[4].QuerySelector(".tag")!.TextContent.Trim());
        Assert.Equal("Sakka", rows[4].QuerySelector("input")!.GetAttribute("value"));
        Assert.Empty(page.FindAll(".redo-line"));
    }

    [Fact]
    public async Task Saving_WhileWhatWasEnteredIsNeitherTakenNorRejected_WarnsFirst()
    {
        var page = await ReloadedAfterAsync(form => form.InputAsync("#publisher-name", "Casterman BD"));
        _api.Write = (_, _, _) => FakeAdminApi.Json(Casterman);
        var written = _api.Written.Count;

        await page.SubmitAsync("form");

        var dialog = page.Find("dialog");
        Assert.Equal("Saisie précédente à reprendre ou rejeter", dialog.QuerySelector(".dialog-title")!.TextContent);
        Assert.Equal(written, _api.Written.Count);

        await page.ClickAsync("dialog .dialog-actions .button[data-variant=primary]");

        Assert.Equal(written + 1, _api.Written.Count);
    }

    [Fact]
    public async Task Reload_ThatCannotReadThePublisher_SaysSo_TheEntryStayingAsItIs()
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        await page.InputAsync("#publisher-name", "Casterman BD");
        await page.SubmitAsync("form");
        _api.Failures[$"/admin/publishers/{PublisherId}"] = HttpStatusCode.InternalServerError;

        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[0]);

        Assert.Equal("technical", page.Find("[role=alert]").GetAttribute("data-error-category"));
        Assert.Equal("Casterman BD", page.Find("#publisher-name").GetAttribute("value"));
        Assert.Empty(page.FindAll(".redo-line"));
    }

    // The form once reloaded after a saving refused, the publisher modified meanwhile, what the user entered left behind.
    private async Task<IRenderedComponent<AdminPublisherForm>> ReloadedAfterAsync(Func<IRenderedComponent<AdminPublisherForm>, Task> entering)
    {
        var page = OpenPublisher();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        await entering(page);
        await page.SubmitAsync("form");
        _api.Answers[$"/admin/publishers/{PublisherId}"] = Casterman with { Version = 13 };
        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[0]);
        return page;
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
