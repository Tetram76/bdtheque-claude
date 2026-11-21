using System.Globalization;
using System.Net;
using System.Text.Json;
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
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The entry form of an edition and of an edition targeted by a purchase intent (visuel/saisie-edition/contenu.md): its blocks,
/// its contextual labels, its freeness, its ISBN check, its pre-filling, its saving and the three categories of its errors, told
/// apart by role, field and text, never by appearance. What it shares with the other forms (frame, choice of a record) is fixed by
/// their tests.
/// </summary>
public sealed class AdminEditionFormTests : BunitContext, IDisposable
{
    // The lists of the block of the edition and of the making, by the end of their identifier.
    private static readonly string[] ListedFields = ["category", "format", "binding", "orientation", "direction"];

    // The two amounts of the block of the value.
    private static readonly string[] Amounts = ["price", "initial"];

    // The controls of the two amounts, each with its currency.
    private static readonly string[] AmountControls = ["price", "price-currency", "initial", "initial-currency"];

    private static readonly Guid AlbumId = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid EditionId = Guid.Parse("01900000-0000-7000-8000-0000000000a2");
    private static readonly Guid WishedId = Guid.Parse("01900000-0000-7000-8000-0000000000a3");
    private static readonly Guid IntentId = Guid.Parse("01900000-0000-7000-8000-0000000000a4");
    private static readonly Guid SeriesId = Guid.Parse("01900000-0000-7000-8000-0000000000a5");
    private static readonly Guid Casterman = Guid.Parse("01900000-0000-7000-8000-0000000000e1");
    private static readonly Guid FacSimile = Guid.Parse("01900000-0000-7000-8000-0000000000e2");
    private static readonly Guid Pastiches = Guid.Parse("01900000-0000-7000-8000-0000000000e3");
    private static readonly Guid Dargaud = Guid.Parse("01900000-0000-7000-8000-0000000000e4");

    private static readonly Guid CoverId = Guid.Parse("01900000-0000-7000-8000-0000000000c1");
    private static readonly Guid FirstPlateId = Guid.Parse("01900000-0000-7000-8000-0000000000c2");
    private static readonly Guid SecondPlateId = Guid.Parse("01900000-0000-7000-8000-0000000000c3");
    private static readonly Guid AddedId = Guid.Parse("01900000-0000-7000-8000-0000000000c4");

    // The visuals of the owned edition, in their presentation order, read under the version of the album of the edition.
    private static readonly EditionVisualsForm OwnedVisuals = new(
        EditionId, [new(CoverId, VisualType.Cover, 0), new(FirstPlateId, VisualType.Plate, 0), new(SecondPlateId, VisualType.Plate, 1)], 12);

    private static readonly AlbumSummary Castafiore = new(
        AlbumId, "Les Bijoux de la Castafiore", SeriesId, "Tintin", AlbumType.Regular, false, 21, null, null, true);

    private static readonly EditionFormReferences CastermanOnly = new(new PublisherListItem(Casterman, "Casterman", null), null);

    private static readonly EditionForm Owned = new(
        EditionId, AlbumId,
        new EditionContent(
            Casterman, null, 2007, "978-2-203-00120-8", BindingType.Hardcover, BookOrientation.Portrait, ReadingDirection.LeftToRight,
            EditionFormat.Standard, 62, null, false, true, EditionCondition.VeryGood, AcquisitionMode.Purchase, false,
            new DateOnly(2008, 3, 14), 10.95m, "EUR", false, 9.95m, "EUR", "T-21b", null),
        true, 12, Castafiore, CastermanOnly, null,
        new DateTimeOffset(2026, 3, 4, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 10, 8, 20, 8, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 3, 4, 9, 0, 0, TimeSpan.Zero));

    private static readonly EditionForm Wished = new(
        WishedId, AlbumId,
        new EditionContent(
            Casterman, FacSimile, 2013, "978-2-203-03312-4", BindingType.Hardcover, BookOrientation.Portrait, ReadingDirection.LeftToRight,
            EditionFormat.Large, 62, EditionCategory.SpecialEdition, false, true, null, null, false, null, null, null, false, null, null,
            null, null),
        true, 12, Castafiore with { IsInCollection = false },
        new EditionFormReferences(
            new PublisherListItem(Casterman, "Casterman", null), new PublisherCollectionListItem(FacSimile, "Fac-similé", Casterman, "Casterman")),
        new PurchaseIntentItem(IntentId, new DateTimeOffset(2026, 10, 8, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 8, 10, 0, 0, TimeSpan.Zero)),
        new DateTimeOffset(2026, 10, 8, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero),
        null);

    // The template of the series pre-fills the publisher, the category is left empty by it (fonctionnel/regles-metier/
    // initialisation-d-une-nouvelle-edition-depuis-la-serie.md).
    private static readonly SeriesEditionTemplate TintinTemplate = new(
        Casterman, null, null, EditionCondition.Excellent, BindingType.Hardcover, BookOrientation.Portrait, ReadingDirection.LeftToRight,
        EditionFormat.Standard, true);

    private static readonly NewEditionForm NewOwned = new(
        AlbumId,
        new EditionContent(
            Casterman, null, null, null, BindingType.Hardcover, BookOrientation.Portrait, ReadingDirection.LeftToRight, EditionFormat.Standard,
            null, null, false, true, EditionCondition.Excellent, null, false, null, null, null, false, null, null, null, null),
        12, Castafiore, CastermanOnly, TintinTemplate);

    private static readonly NewPurchaseIntentForm NewIntent = new(
        AlbumId,
        new PurchaseIntentEditionContent(
            Casterman, null, null, null, BindingType.Hardcover, BookOrientation.Portrait, ReadingDirection.LeftToRight, EditionFormat.Standard,
            null, null, true),
        12, Castafiore, CastermanOnly, TintinTemplate);

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

    // The clock of the form, the system's unless a test needs the time a visual added is kept to elapse.
    private TimeProvider _clock = TimeProvider.System;

    public AdminEditionFormTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(_ => _clock);
        // The choice of a record imports its script once rendered, which these tests do not exercise.
        JSInterop.Mode = JSRuntimeMode.Loose;
        // An entry form is rendered by its circuit (InteractiveServer), which its frame marks.
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _api.Answers[$"/admin/albums/{AlbumId}/editions/{EditionId}"] = Owned;
        _api.Answers[$"/admin/albums/{AlbumId}/editions/{WishedId}"] = Wished;
        _api.Answers[$"/admin/albums/{AlbumId}/editions/{EditionId}/visuals"] = OwnedVisuals;
        _api.Answers[$"/admin/albums/{AlbumId}/editions/{WishedId}/visuals"] = new EditionVisualsForm(WishedId, [], Wished.AlbumVersion);
        _api.Answers["/admin/visuals/limits"] = new VisualLimits(30 * 1024 * 1024, TimeSpan.FromHours(24));
        _api.Answers[$"/admin/albums/{AlbumId}/editions/new"] = NewOwned;
        _api.Answers[$"/admin/albums/{AlbumId}/purchase-intents/new"] = NewIntent;
        _api.Answers[$"/admin/publishers/{Casterman}/collections"] = new List<PublisherCollectionForm>
        {
            new(FacSimile, Casterman, "Fac-similé", 3), new(Pastiches, Casterman, "Pastiches", 3),
        };
        _api.Answers[$"/admin/publishers/{Dargaud}/collections"] = new List<PublisherCollectionForm>();
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheBreadcrumbTheTitleTheLabelOfTheEditionAndItsAlbum()
    {
        var page = OpenOwned();

        // The edition by its full label, ISBN included, as everywhere (visuel/saisie-edition/contenu.md, #470): its ISBN in a
        // block that a line never breaks.
        Assert.Equal(["Administration", "Albums", "Les Bijoux de la Castafiore (Tintin - T. 21)", "Casterman [2007] - ISBN\u00a0978-2-203-00120-8"],
            page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal("ISBN\u00a0978-2-203-00120-8", page.Find(".breadcrumb [aria-current] .label-isbn").TextContent);
        Assert.Equal("Modifier l'édition", page.Find("h1").TextContent.Trim());
        Assert.Equal("Casterman [2007] - ISBN 978-2-203-00120-8", page.Find(".entry-form-subject").TextContent.Trim());
        // The album leads to the page of its editions from the breadcrumb, and to its own form from the heading; neither link
        // to the consultation, nor deletion, nor confirmation of a purchase: those are the page of the editions'. The menus of the visuals
        // lead to their originals, as their own tests show.
        Assert.Equal(
            [AppPaths.AdminHome, AppPaths.AdminAlbums, AppPaths.AdminAlbumEditions(AlbumId), AppPaths.AdminEditAlbum(AlbumId)],
            page.FindAll(".entry-form-heading a, .entry-form-body a:not(.menu-item)").Select(link => link.GetAttribute("href")).Distinct());
    }

    [Fact]
    public void Blocks_EachWithItsTitle_InTheOrderOfTheForm()
    {
        var page = OpenOwned();

        Assert.Equal(["L'édition", "Fabrication", "L'exemplaire", "Valeur", "Les visuels"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(["Publication", "Objet", "Possession", "Montants", "Images"], page.FindAll(".block-kicker").Select(kicker => kicker.TextContent.Trim()));
    }

    [Fact]
    public void Fields_TheEditionAsRead()
    {
        var page = OpenOwned();

        Assert.Equal("Casterman", page.Find("#edition-publisher").GetAttribute("value"));
        // Required: the publisher, and the mode of acquisition of an edition owned; no other field has a mention.
        Assert.Equal(["edition-publisher", "edition-mode"], page.FindAll("label:has(.field-note)").Select(label => label.GetAttribute("for")));
        Assert.All(page.FindAll(".field-note"), note => Assert.Equal("(obligatoire)", note.TextContent));
        Assert.Equal("—", page.Find("#edition-collection").TextContent.Trim());
        Assert.Equal("2007", page.Find("#edition-year").GetAttribute("value"));
        Assert.Equal("978-2-203-00120-8", page.Find("#edition-isbn").GetAttribute("value"));
        Assert.Equal(["", "Standard", "Hardcover", "Portrait", "LeftToRight"],
            ListedFields.Select(id => page.Find($"#edition-{id}").GetAttribute("value")));
        Assert.Equal("62", page.Find("#edition-pages").GetAttribute("value"));
        Assert.Equal("Purchase", page.Find("#edition-mode").GetAttribute("value"));
        Assert.Equal("2008-03-14", page.Find("#edition-date").GetAttribute("value"));
        Assert.Equal("VeryGood", page.Find("#edition-condition").GetAttribute("value"));
        Assert.Equal("T-21b", page.Find("#edition-reference").GetAttribute("value"));
        Assert.Equal("10,95", page.Find("#edition-price").GetAttribute("value"));
        Assert.Equal("EUR", page.Find("#edition-price-currency").GetAttribute("value"));
        Assert.Equal("9,95", page.Find("#edition-initial").GetAttribute("value"));
        // En couleur, D'occasion, Dédicacée, Gratuite.
        Assert.Equal([true, false, false, false], page.FindAll("input[type=checkbox]").Select(box => box.HasAttribute("checked")));
        Assert.Equal("Édition créée le 04/03/2026 10:00, entrée dans la collection le 04/03/2026 10:00, modifiée le 08/10/2026 22:08.",
            page.Find(".entry-form-dates").TextContent.Trim());
    }

    [Fact]
    public void Dates_JustAboveTheFirstBlock()
    {
        // Decision of the user (#209), as on the other entry forms.
        var blocks = OpenOwned().Find(".entry-form-blocks").Children;

        Assert.Equal("entry-form-dates", blocks[0].ClassName);
        Assert.Contains("block", blocks[1].ClassList);
    }

    [Fact]
    public void Lists_EachWithItsValues_OrLeftEmpty()
    {
        var page = OpenOwned();

        Assert.Equal(["—", "Édition originale", "Édition spéciale", "Tirage de tête"],
            page.FindAll("#edition-category option").Select(option => option.TextContent.Trim()));
        Assert.Equal(["—", "Achat", "Offerte", "Échange", "Gagnée", "Héritée"],
            page.FindAll("#edition-mode option").Select(option => option.TextContent.Trim()));
        Assert.Equal(["—", "Fac-similé", "Pastiches", "Nouvelle collection…"], CollectionOptions(page));
    }

    [Fact]
    public async Task Labels_FollowTheModeOfAcquisition()
    {
        // fonctionnel/presentation/libelles-contextuels-sur-l-edition.md
        var page = OpenOwned();

        Assert.Equal("Date d'achat", page.Find("label[for=edition-date]").TextContent.Trim());
        Assert.Equal("Prix d'achat", page.Find("#edition-price-legend").TextContent.Trim());

        await page.ChangeAsync("#edition-mode", "Gift");

        Assert.Equal("Date d'acquisition", page.Find("label[for=edition-date]").TextContent.Trim());
        Assert.Equal("Valeur d'acquisition", page.Find("#edition-price-legend").TextContent.Trim());
    }

    [Fact]
    public async Task Free_IsUnavailableForAPurchase_AndEmptiesAndDisablesTheAmounts()
    {
        var page = OpenOwned();

        Assert.True(Free(page).HasAttribute("disabled"));

        await page.ChangeAsync("#edition-mode", "Gift");
        await page.ChangeAsync("#edition-free", true);

        Assert.All(Amounts, id => Assert.True(string.IsNullOrEmpty(page.Find($"#edition-{id}").GetAttribute("value"))));
        Assert.All(AmountControls, id => Assert.True(page.Find($"#edition-{id}").HasAttribute("disabled")));

        // A purchase is never free: choosing it unchecks the box, which it disables.
        await page.ChangeAsync("#edition-mode", "Purchase");

        Assert.False(Free(page).HasAttribute("checked"));
        Assert.False(page.Find("#edition-price").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Isbn_CheckedAsItIsEntered_WithoutBlocking()
    {
        // fonctionnel/regles-metier/validation-de-l-isbn.md
        var page = OpenOwned();
        Assert.Contains("Chiffre de contrôle correct.", page.Find("#edition-isbn-help").TextContent);
        _api.Answers["/admin/editions/isbn-check?isbn=978-2-203-00120-7"] = new IsbnCheck(false, "978-2-203-00120-7");

        await page.InputAsync("#edition-isbn", "978-2-203-00120-7");

        Assert.Contains("Chiffre de contrôle incohérent", page.Find("#edition-isbn-help").TextContent);
        Assert.Contains("peut être enregistré tel quel", page.Find("#edition-isbn-help").TextContent);
        Assert.Equal("true", page.Find("#edition-isbn").GetAttribute("data-warning"));
    }

    // A check that fails is an error with nothing to correct in the ISBN: presented at the scale of the field, as any control
    // presents its own (choix-implementation/socle/frontend.md, erreur dans un contrôle, #480), until the next ISBN is checked.
    [Fact]
    public async Task Isbn_CheckThatFails_IsPresentedUnderTheField_UntilTheNextCheck()
    {
        var page = OpenOwned();
        _api.Failures["/admin/editions/isbn-check"] = HttpStatusCode.InternalServerError;

        await page.InputAsync("#edition-isbn", "978-2-203-00120-7");

        var failure = page.Find("#edition-isbn-failure [role=alert]");
        Assert.Equal("technical", failure.GetAttribute("data-error-category"));
        Assert.True(failure.HasAttribute("data-error-compact"));
        Assert.Equal("La vérification de l'ISBN n'a pas abouti. Elle reprendra à sa prochaine modification.", failure.QuerySelector("[data-error-text]")!.TextContent.Trim());
        Assert.Contains("edition-isbn-failure", page.Find("#edition-isbn").GetAttribute("aria-describedby"));

        _api.Failures.Clear();
        _api.Answers["/admin/editions/isbn-check?isbn=978-2-203-00120-8"] = new IsbnCheck(true, "978-2-203-00120-8");
        await page.InputAsync("#edition-isbn", "978-2-203-00120-8");

        Assert.Empty(page.FindAll("#edition-isbn-failure"));
        Assert.Contains("Chiffre de contrôle correct.", page.Find("#edition-isbn-help").TextContent);
    }

    [Fact]
    public async Task Isbn_TheCheckCharacterIsWrittenInCapitalsAsItIsEntered()
    {
        var page = OpenOwned();
        _api.Answers["/admin/editions/isbn-check?isbn=2-905123-01-x"] = new IsbnCheck(false, "2-905123-01-X");

        await page.InputAsync("#edition-isbn", "2-905123-01-x");

        Assert.Equal("2-905123-01-X", page.Find("#edition-isbn").GetAttribute("value"));
    }

    [Fact]
    public async Task Label_FollowsTheTyping()
    {
        var page = OpenOwned();

        await page.ChangeAsync("#edition-year", "2011");

        Assert.Equal("Casterman [2011] - ISBN 978-2-203-00120-8", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public async Task Collection_TheChangeOfPublisherRemovesIt_AndListsItsOwn()
    {
        var page = OpenOwned();
        await ChooseCollectionAsync(page, 2);
        Assert.Equal("Pastiches", page.Find("#edition-collection").TextContent.Trim());

        ChoosePublisher(page, new RecordOption(Dargaud, "Dargaud", null));

        Assert.Equal("—", page.Find("#edition-collection").TextContent.Trim());
        Assert.Equal(["—", "Nouvelle collection…"], CollectionOptions(page));

        ChoosePublisher(page, null);

        Assert.True(page.Find("#edition-collection").HasAttribute("disabled"));
    }

    [Fact]
    public async Task PublisherCreatedOnTheSpot_IsChosen_WithoutCollection_AndCreatedWithTheEdition_WhenItIsSaved()
    {
        // visuel/saisie-edition/contenu.md (decision of the user, #414): the name typed, the publisher created only with the edition.
        var page = OpenNewOwned();

        await CreatePublisherOnTheSpotAsync(page, " Delcourt ");

        Assert.Empty(page.FindAll("dialog"));
        Assert.Equal("Delcourt", page.Find("#edition-publisher").GetAttribute("value"));
        Assert.Equal("admin.edition-form.publisher-created", _audience.Functions.Last());
        Assert.Equal(["—", "Nouvelle collection…"], CollectionOptions(page));
        Assert.DoesNotContain(_api.Requested, path => path.StartsWith("/admin/publishers/", StringComparison.Ordinal) && !path.Contains(Casterman.ToString(), StringComparison.Ordinal));

        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned, HttpStatusCode.Created);
        await page.SubmitAsync("form");

        var content = _api.Written.Single(w => w.Path == $"/admin/albums/{AlbumId}/editions").Body.GetProperty("content");
        var publisher = content.GetProperty("newRecords").GetProperty("publishers").EnumerateArray().Single();
        Assert.Equal("Delcourt", publisher.GetProperty("name").GetString());
        Assert.Equal(publisher.GetProperty("key").GetString(), content.GetProperty("publisherId").GetString());
    }

    [Fact]
    public async Task PublisherCreatedOnTheSpot_ForANewPurchaseIntent_IsCreatedWithTheEditionTargeted()
    {
        var page = OpenNewIntent();
        await CreatePublisherOnTheSpotAsync(page, "Delcourt");
        Assert.Equal("admin.purchase-intent-form.publisher-created", _audience.Functions.Last());

        _api.Write = (_, _, _) => FakeAdminApi.Json(new PurchaseIntentsForm(AlbumId, [], 13), HttpStatusCode.Created);
        await page.SubmitAsync("form");

        var edition = _api.Written.Single(w => w.Path == $"/admin/albums/{AlbumId}/purchase-intents").Body.GetProperty("edition");
        var publisher = edition.GetProperty("newRecords").GetProperty("publishers").EnumerateArray().Single();
        Assert.Equal(("Delcourt", publisher.GetProperty("key").GetString()),
            (publisher.GetProperty("name").GetString(), edition.GetProperty("publisherId").GetString()));
    }

    [Fact]
    public async Task PublisherCreatedOnTheSpot_ThenReplaced_IsNotCreated_ButSuggestedAgain()
    {
        var page = OpenOwned();
        await CreatePublisherOnTheSpotAsync(page, "Delcourt");

        ChoosePublisher(page, new RecordOption(Casterman, "Casterman", null));

        var picker = page.FindComponents<RecordPicker>().Single(p => p.Instance.Kind == RecordKind.Publisher);
        Assert.Equal(["Delcourt"], picker.Instance.Pending.Select(p => p.Label));
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);
        await page.SubmitAsync("form");
        Assert.Equal(JsonValueKind.Null,
            _api.Written.Single(w => w.Method == HttpMethod.Put).Body.GetProperty("content").GetProperty("newRecords").ValueKind);
    }

    [Fact]
    public async Task BusinessError_OnAPublisherCreatedOnTheSpot_DesignatesTheFieldOfThePublisher()
    {
        var page = OpenOwned();
        await CreatePublisherOnTheSpotAsync(page, "Dargaud");
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Publisher.NameAlreadyUsed", "/newRecords/publishers/0/name");

        await page.SubmitAsync("form");

        var field = page.Find("#edition-publisher");
        Assert.Equal("true", field.GetAttribute("aria-invalid"));
        Assert.Contains(field.GetAttribute("aria-describedby")!.Split(' '),
            id => page.Find($"#{id}").TextContent.Trim().StartsWith("« Dargaud » est déjà pris", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CollectionCreatedOnTheSpot_IsChosen_AndCreatedInThePublisherWithTheEdition_WhenItIsSaved()
    {
        // visuel/saisie-edition/contenu.md, as for a series: « Nouvelle collection… », its name alone, created with the edition.
        var page = OpenOwned();

        await CreateCollectionOnTheSpotAsync(page, " Aire libre ");

        Assert.Equal("Aire libre", page.Find("#edition-collection").TextContent.Trim());
        Assert.Equal(["—", "Aire libre", "Fac-similé", "Pastiches", "Nouvelle collection…"], CollectionOptions(page));
        Assert.Equal("admin.edition-form.collection-created", _audience.Functions.Last());
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);
        await page.SubmitAsync("form");

        var content = _api.Written.Single(w => w.Method == HttpMethod.Put).Body.GetProperty("content");
        var collection = content.GetProperty("newRecords").GetProperty("publisherCollections").EnumerateArray().Single();
        Assert.Equal(("Aire libre", Casterman.ToString()), (collection.GetProperty("name").GetString(), collection.GetProperty("publisherId").GetString()));
        Assert.Equal(collection.GetProperty("key").GetString(), content.GetProperty("publisherCollectionId").GetString());
    }

    [Fact]
    public async Task CollectionCreatedOnTheSpot_ForANewPurchaseIntent_IsCreatedWithTheEditionTargeted()
    {
        var page = OpenNewIntent();
        await CreateCollectionOnTheSpotAsync(page, "Aire libre");
        Assert.Equal("admin.purchase-intent-form.collection-created", _audience.Functions.Last());

        _api.Write = (_, _, _) => FakeAdminApi.Json(new PurchaseIntentsForm(AlbumId, [], 13), HttpStatusCode.Created);
        await page.SubmitAsync("form");

        var edition = _api.Written.Single(w => w.Path == $"/admin/albums/{AlbumId}/purchase-intents").Body.GetProperty("edition");
        var collection = edition.GetProperty("newRecords").GetProperty("publisherCollections").EnumerateArray().Single();
        Assert.Equal(("Aire libre", collection.GetProperty("key").GetString()),
            (collection.GetProperty("name").GetString(), edition.GetProperty("publisherCollectionId").GetString()));
    }

    [Fact]
    public async Task BusinessError_OnACollectionCreatedOnTheSpot_DesignatesTheFieldOfTheCollection()
    {
        var page = OpenOwned();
        await CreateCollectionOnTheSpotAsync(page, "PASTICHES");
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "PublisherCollection.NameAlreadyUsed", "/newRecords/publisherCollections/0/name");

        await page.SubmitAsync("form");

        var field = page.Find("#edition-collection");
        Assert.Equal("true", field.GetAttribute("aria-invalid"));
        Assert.Contains(field.GetAttribute("aria-describedby")!.Split(' '),
            id => page.Find($"#{id}").TextContent.Trim().StartsWith("« PASTICHES » est déjà pris", StringComparison.Ordinal));
    }

    // Under the field, in the compact form of the single component of the errors, which the field describes itself by; its
    // help stays what it is (#438).
    [Fact]
    public void Collections_ThatCannotBeRead_AreATechnicalErrorUnderTheField()
    {
        _api.Failures[$"/admin/publishers/{Dargaud}/collections"] = HttpStatusCode.InternalServerError;
        var page = OpenOwned();

        ChoosePublisher(page, new RecordOption(Dargaud, "Dargaud", null));

        var error = page.Find("#edition-collection-failure [role=alert]");
        Assert.Equal("technical", error.GetAttribute("data-error-category"));
        Assert.True(error.HasAttribute("data-error-compact"));
        Assert.Equal("Les collections de l'éditeur n'ont pas pu être lues : choisissez-le de nouveau.", error.QuerySelector("[data-error-text]")!.TextContent.Trim());
        Assert.DoesNotContain("n'ont pas pu être lues", page.Find("#edition-collection-help").TextContent);
        Assert.Contains("edition-collection-failure", page.Find("#edition-collection").GetAttribute("aria-describedby"));
    }

    [Fact]
    public async Task Currency_NamedInItsList_AnyOtherEnteredByItsCode()
    {
        // fonctionnel/valorisation/gestion-des-devises.md: any currency, the old franc included.
        var page = OpenOwned();
        var currencies = page.FindAll("#edition-price-currency option").Select(option => option.TextContent.Trim()).ToList();
        // In the order of their likely use for the purchase of a new edition today, the old franc among them.
        Assert.Equal(
            ["EUR · euro", "USD · dollar des États-Unis", "CHF · franc suisse", "CAD · dollar canadien", "GBP · livre sterling",
                "FRF · franc français", "BEF · franc belge", "LUF · franc luxembourgeois", "QZF · ancien franc français", "Autre devise…"],
            currencies);

        await page.ChangeAsync("#edition-price-currency", "other");
        await page.ChangeAsync("#edition-price-code", "jpy");
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);
        await page.SubmitAsync("form");

        Assert.Single(_api.Written);
        Assert.Equal("JPY", _api.Written.Single().Body.GetProperty("content").GetProperty("acquisitionCurrency").GetString());
    }

    [Fact]
    public async Task Save_SendsTheWholeFormAtTheVersionOfTheAlbum_RecordsTheSaving_AndLeadsBackToTheEditionsOfTheAlbum()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);

        await page.ChangeAsync("#edition-pages", "64");
        await page.SubmitAsync("form");

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(AppPaths.AdminAlbumEditions(AlbumId), new Uri(navigation.Uri).AbsolutePath);
        var (method, path, body) = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/albums/{AlbumId}/editions/{EditionId}"), (method, path));
        var request = body.Deserialize<UpdateEditionRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(12u, request.AlbumVersion);
        Assert.Equal(Owned.Content with { PageCount = 64 }, request.Content);
        Assert.Contains("admin.edition-form.saved", _audience.Functions);
    }

    [Fact]
    public async Task NewEdition_PreFilledFromTheSeries_SaysWhichFields_AndIsCreated()
    {
        var page = OpenNewOwned();

        Assert.Equal("Nouvelle édition", page.Find("h1").TextContent.Trim());
        Assert.Equal("Nouvelle édition", page.FindAll(".breadcrumb li")[^1].TextContent.Trim());
        Assert.Empty(page.FindAll(".entry-form-dates"));
        Assert.Equal("Pré-remplie d'après la série Tintin : éditeur, état, reliure, orientation, sens de lecture, format et couleur. "
                     + "Ce ne sont que des propositions : chaque valeur reste modifiable.",
            page.Find(".edition-preset").TextContent.Trim());
        Assert.Equal("Créer l'édition", page.Find(".save-bar button[type=submit]").TextContent.Trim());
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned, HttpStatusCode.Created);

        await page.ChangeAsync("#edition-mode", "Purchase");
        await page.SubmitAsync("form");

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(AppPaths.AdminAlbumEditions(AlbumId), new Uri(navigation.Uri).AbsolutePath);
        var (method, path, body) = _api.Written.Single();
        Assert.Equal((HttpMethod.Post, $"/admin/albums/{AlbumId}/editions"), (method, path));
        Assert.Equal(NewOwned.Content with { AcquisitionMode = AcquisitionMode.Purchase },
            body.Deserialize<CreateEditionRequest>(JsonSerializerOptions.Web)!.Content);
        Assert.Contains("admin.edition-form.created", _audience.Functions);
    }

    [Fact]
    public void NewEdition_OfAnAlbumWithoutSeries_SaysTheValuesAreThoseProposedToAnyNewEdition()
    {
        _api.Answers[$"/admin/albums/{AlbumId}/editions/new"] = NewOwned with
        {
            Album = Castafiore with { SeriesId = null, SeriesTitle = null }, SeriesTemplate = null,
        };

        var page = OpenNewOwned();

        Assert.StartsWith("Pré-remplie avec les valeurs proposées pour toute nouvelle édition", page.Find(".edition-preset").TextContent.Trim());
    }

    [Fact]
    public void PurchaseIntent_HasNeitherCopyNorValue_AndItsOwnDates()
    {
        var page = OpenWished();

        Assert.Equal("Modifier l'intention d'acquisition", page.Find("h1").TextContent.Trim());
        Assert.Equal(["L'édition", "Fabrication", "Les visuels"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(["En couleur"], page.FindAll(".choice").Select(choice => choice.TextContent.Trim()));
        // The publisher alone is required (visuel/saisie-edition/contenu.md).
        Assert.Equal(["edition-publisher"], page.FindAll("label:has(.field-note)").Select(label => label.GetAttribute("for")));
        Assert.Equal("Intention d'acquisition posée le 08/10/2026 12:00, modifiée le 12/10/2026 12:00.", page.Find(".entry-form-dates").TextContent.Trim());
        Assert.Equal("Fac-similé", page.Find("#edition-collection").TextContent.Trim());
    }

    [Fact]
    public async Task PurchaseIntent_IsSavedAsAnEditionNotOwned()
    {
        var page = OpenWished();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Wished);

        await page.ChangeAsync("#edition-year", "2014");
        await page.SubmitAsync("form");

        Assert.Single(_api.Written);
        var (method, path, body) = _api.Written.Single();
        Assert.Equal((HttpMethod.Put, $"/admin/albums/{AlbumId}/editions/{WishedId}"), (method, path));
        Assert.Equal(Wished.Content with { PublicationYear = 2014 }, body.Deserialize<UpdateEditionRequest>(JsonSerializerOptions.Web)!.Content);
        Assert.Contains("admin.purchase-intent-form.saved", _audience.Functions);
    }

    [Fact]
    public async Task NewPurchaseIntent_OnANewEdition_IsCreated()
    {
        var page = OpenNewIntent();

        Assert.Equal("Nouvelle intention d'acquisition", page.Find("h1").TextContent.Trim());
        Assert.Equal("Nouvelle intention d'acquisition", page.FindAll(".breadcrumb li")[^1].TextContent.Trim());
        Assert.Equal(["L'édition", "Fabrication", "Les visuels"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal("Créer l'intention d'acquisition", page.Find(".save-bar button[type=submit]").TextContent.Trim());
        Assert.Contains("Pré-remplie d'après la série Tintin", page.Find(".edition-preset").TextContent);
        _api.Write = (_, _, _) => FakeAdminApi.Json(new PurchaseIntentsForm(AlbumId, [new PurchaseIntentForm(IntentId, WishedId)], 13),
            HttpStatusCode.Created);

        await page.ChangeAsync("#edition-year", "2013");
        await page.SubmitAsync("form");

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(AppPaths.AdminAlbumEditions(AlbumId), new Uri(navigation.Uri).AbsolutePath);
        var (method, path, body) = _api.Written.Single();
        Assert.Equal((HttpMethod.Post, $"/admin/albums/{AlbumId}/purchase-intents"), (method, path));
        var request = body.Deserialize<CreatePurchaseIntentRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(NewIntent.Edition with { PublicationYear = 2013 }, request.Edition);
        Assert.Equal(12u, request.AlbumVersion);
        Assert.Contains("admin.purchase-intent-form.created", _audience.Functions);
    }

    [Theory]
    [InlineData("Edition.PublisherRequired", "/publisherId", "edition-publisher", "Obligatoire.")]
    [InlineData("Edition.AcquisitionModeRequired", "/acquisitionMode", "edition-mode", "Obligatoire pour une édition possédée.")]
    [InlineData("Edition.AcquisitionPriceReferenceDateRequired", "/acquisitionAmount", "edition-price", "Sans date de référence, ce prix ne peut pas être enregistré.")]
    [InlineData("Edition.InitialValueReferenceDateRequired", "/initialValueAmount", "edition-initial", "Sans date de référence, cette valeur ne peut pas être enregistrée.")]
    [InlineData("Edition.PageCountPositive", "/pageCount", "edition-pages", "Supérieur à zéro.")]
    public async Task BusinessError_ToCorrect_PointsToItsField(string rule, string pointer, string field, string message)
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, rule, pointer);

        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("business", error.GetAttribute("data-error-category"));
        Assert.Equal("true", page.Find($"#{field}").GetAttribute("aria-invalid"));
        Assert.Contains(message, page.Find($"#{field}-error").TextContent);
    }

    [Fact]
    public async Task FunctionalError_ModifiedMeanwhile_OffersToReloadAndToSeeTheChanges()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);

        await page.ChangeAsync("#edition-year", "2011");
        await page.ChangeAsync("#edition-mode", "Gift");
        await page.ChangeAsync("#edition-price", "12,5");
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        Assert.Contains("L'album de cette édition a été modifié entre-temps", error.TextContent);

        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[1]);

        var changes = page.Find("dialog .changes-dialog-list");
        Assert.Equal(["Année d'édition", "Mode d'acquisition", "Valeur d'acquisition"], changes.QuerySelectorAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["2011", "Offerte", "12,50 €"], changes.QuerySelectorAll("dd").Select(dd => dd.TextContent));
        Assert.Contains("admin.edition-form.changes", _audience.Functions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FunctionalError_ModifiedMeanwhileOnCreation_OffersToReloadTheNewRecordAndToSeeTheChanges(bool intent)
    {
        // A new edition, or intent, is written under the version of its album, as a modified one: the album modified meanwhile,
        // the creation is to start again from its current state, which only a reload reads.
        var page = intent ? OpenNewIntent() : OpenNewOwned();
        var path = intent ? AppPaths.AdminNewPurchaseIntent(AlbumId) : AppPaths.AdminNewAlbumEdition(AlbumId);
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        ChoosePublisher(page, new RecordOption(Dargaud, "Dargaud", null));
        await page.ChangeAsync("#edition-year", "2011");
        if (!intent)
            await page.ChangeAsync("#edition-mode", "Gift");
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        Assert.Equal(["Recharger la fiche", "Voir mes changements"], error.QuerySelectorAll("button").Select(button => button.TextContent.Trim()));
        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[1]);
        Assert.Contains("Année d'édition", page.Find("dialog .changes-dialog-list").TextContent);
        Assert.Contains(intent ? "admin.purchase-intent-form.changes" : "admin.edition-form.changes", _audience.Functions);

        await page.ClickAsync("[role=alert] button");

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(path, new Uri(navigation.Uri).AbsolutePath);
    }

    [Fact]
    public async Task TechnicalError_KeepsTheChanges_AndOffersToTryAgain()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);

        await page.ChangeAsync("#edition-year", "2011");
        await page.SubmitAsync("form");

        var error = page.Find("[role=alert]");
        Assert.Equal("technical", error.GetAttribute("data-error-category"));
        Assert.Contains("n'a pas pu enregistrer l'édition", error.TextContent);
        Assert.Equal("2011", page.Find("#edition-year").GetAttribute("value"));
    }

    [Fact]
    public void LoadError_IsPresentedInsteadOfTheForm()
    {
        _api.Failures[$"/admin/albums/{AlbumId}/editions/{EditionId}"] = HttpStatusCode.InternalServerError;

        var page = OpenOwned();

        Assert.Equal("technical", page.Find("[role=alert]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll("form"));
    }

    [Fact]
    public void Screen_IsRecordedWithTheEditionItPresents()
    {
        OpenOwned();

        Assert.Equal(new FunctionUse("admin.edition-form", new PresentedRecord(RecordType.Edition, EditionId), null), FirstUse());
    }

    [Fact]
    public void NewEdition_Screen_IsRecordedWithItsAlbum()
    {
        OpenNewOwned();

        Assert.Equal(new FunctionUse("admin.edition-form.new", new PresentedRecord(RecordType.Album, AlbumId), null), FirstUse());
    }

    [Fact]
    public void PurchaseIntent_Screen_IsRecordedWithTheIntentItPresents()
    {
        OpenWished();

        Assert.Equal(new FunctionUse("admin.purchase-intent-form", new PresentedRecord(RecordType.PurchaseIntent, IntentId), null), FirstUse());
    }

    [Fact]
    public void NewPurchaseIntent_Screen_IsRecordedWithItsAlbum()
    {
        OpenNewIntent();

        Assert.Equal(new FunctionUse("admin.purchase-intent-form.new", new PresentedRecord(RecordType.Album, AlbumId), null), FirstUse());
    }

    [Theory]
    [InlineData("16.9500", "16,95")]
    [InlineData("10", "10,00")]
    [InlineData("0.1234", "0,1234")]
    public void Amount_ReadWithTheScaleOfTheDatabase_ShownAsAnAmountOfMoney(string stored, string shown)
    {
        // Stored with four decimals, an amount reads back with its trailing zeros (16.9500): shown with two at least, the others
        // only when they count.
        var field = Render<AmountField>(parameters => parameters
            .Add(p => p.Id, "price")
            .Add(p => p.Label, "Prix d'achat")
            .Add(p => p.Amount, decimal.Parse(stored, CultureInfo.InvariantCulture))
            .Add(p => p.DefaultCurrency, "EUR"));

        Assert.Equal(shown, field.Find("#price").GetAttribute("value"));
    }

    [Fact]
    public void Visuals_ABlockOfTheForm_InASingleGrid_InTheOrderOfTheTypes_EachMarkedWithItsFullType()
    {
        // visuel/saisie-edition/contenu.md, Les visuels: the last block of the form, saved with it, which the block says, then the
        // drop zone, the number of visuals and their grid, each marked with the full name of its type, a plate numbered among the
        // plates (#469).
        var page = OpenOwned();
        var block = page.Find("form .edition-visuals");

        Assert.Contains("Les visuels s'enregistrent avec la fiche", block.TextContent);
        Assert.Contains("conservé 24 heures", block.TextContent);
        Assert.Equal("JPEG, PNG, WebP ou GIF, 30 Mio au plus.", block.QuerySelector(".edition-visuals-formats")!.TextContent);
        Assert.Equal("3 visuels", block.QuerySelector(".edition-visuals-count")!.TextContent);
        var visuals = block.QuerySelectorAll(".edition-visual");
        // Its full name, and the short mark of the album record that the theme shows where the full name does not hold.
        Assert.Equal(["Couverture", "Planche 1", "Planche 2"], visuals.Select(v => v.QuerySelector(".edition-visual-mark-full")!.TextContent));
        Assert.Equal(["Couv.", "1", "2"], visuals.Select(v => v.QuerySelector(".edition-visual-mark-short")!.TextContent));
        Assert.Equal(["Cover", "Plate", "Plate"], visuals.Select(v => v.QuerySelector(".edition-visual-mark")!.GetAttribute("data-type")));
        Assert.Equal(["Couverture", "Planche 1", "Planche 2"], visuals.Select(v => v.QuerySelector("img")!.GetAttribute("alt")));
        Assert.Equal(VisualMediaRoutes.Display(CoverId), visuals[0].QuerySelector("img")!.GetAttribute("src"));
        // Its menu: the type, the current one pressed, the original beside the application, the deletion.
        var menu = visuals[1].QuerySelector(".actions-menu-panel")!;
        Assert.Equal("Actions sur Planche 1", visuals[1].QuerySelector(".actions-menu-toggle")!.GetAttribute("aria-label"));
        Assert.Equal(["Planche"], menu.QuerySelectorAll("[aria-pressed=true]").Select(item => item.TextContent.Trim()));
        Assert.Equal(VisualMediaRoutes.Original(FirstPlateId), menu.QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal("_blank", menu.QuerySelector("a")!.GetAttribute("target"));
        Assert.Equal("Supprimer le visuel…", menu.QuerySelectorAll("button")[^1].TextContent.Trim());
    }

    [Fact]
    public async Task Visuals_Added_WaitInTheForm_WhichSavesThemInTheOrderOfTheTypes()
    {
        var page = OpenOwned();
        _api.Write = (method, path, _) => path == "/admin/visuals/pending"
            ? FakeAdminApi.Json(new PendingVisualForm(AddedId, DateTimeOffset.UtcNow.AddHours(24)), HttpStatusCode.Created)
            : FakeAdminApi.Json(Owned);

        await page.ChangeAsync("#edition-visuals-type", "Dedication");
        await SendAsync(page, new TestBrowserFile("dedicace.png", new byte[2048]));

        Assert.Equal("4 visuels", page.Find(".edition-visuals-count").TextContent);
        var (_, _, form) = _api.Written.Single();
        Assert.Equal("dedicace.png", form.GetProperty(UploadVisualFields.File).GetProperty("name").GetString());
        Assert.Equal(2048, form.GetProperty(UploadVisualFields.File).GetProperty("length").GetInt64());
        // After the cover, before the plates; shown from the waiting area; an unsaved change of the form.
        var added = page.FindAll(".edition-visual")[1];
        Assert.Equal(("Dédicace", "/admin/visuals/pending/" + AddedId + "/display"),
            (added.QuerySelector(".edition-visual-mark-full")!.TextContent, added.QuerySelector("img")!.GetAttribute("src")));
        // Its original as uploaded, from the waiting area too, before the form is saved.
        // Opened beside the form, but in the application: without the mark of a site outside it.
        var original = added.QuerySelectorAll(".actions-menu-panel a").Single(link => link.TextContent.Trim() == "Ouvrir l'original");
        Assert.Equal(("/admin/visuals/pending/" + AddedId + "/original", "_blank"), (original.GetAttribute("href"), original.GetAttribute("target")));
        Assert.Null(original.QuerySelector(".external-mark"));
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
        Assert.Contains("admin.edition-form.visual-added", _audience.Functions);

        await page.SubmitAsync("form");

        Assert.Equal(HttpMethod.Put, _api.Written.Last().Method);
        Assert.Equal(
            [(CoverId, (Guid?)null, "Cover"), (null, AddedId, "Dedication"), (FirstPlateId, null, "Plate"), (SecondPlateId, null, "Plate")],
            VisualsSent());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Visuals_OfANewEdition_AreSavedWithIt(bool intent)
    {
        var page = intent ? OpenNewIntent() : OpenNewOwned();
        _api.Write = (_, path, _) => path == "/admin/visuals/pending"
            ? FakeAdminApi.Json(new PendingVisualForm(AddedId, DateTimeOffset.UtcNow.AddHours(24)), HttpStatusCode.Created)
            : intent ? FakeAdminApi.Json(new PurchaseIntentsForm(AlbumId, [], 13), HttpStatusCode.Created) : FakeAdminApi.Json(Owned, HttpStatusCode.Created);
        ChoosePublisher(page, new RecordOption(Dargaud, "Dargaud", null));
        if (!intent)
            await page.ChangeAsync("#edition-mode", "Gift");

        await SendAsync(page, new TestBrowserFile("couverture.png", new byte[16]));
        Assert.Equal("1 visuel", page.Find(".edition-visuals-count").TextContent);
        await page.SubmitAsync("form");

        Assert.Equal(2, _api.Written.Count);
        Assert.Equal([((Guid?)null, (Guid?)AddedId, "Cover")], VisualsSent());
    }

    [Fact]
    public async Task Visuals_ARefusedFile_SaysWhy_UntilItIsRemoved()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.BadRequest, "EditionVisual.FileNotSupportedImage");

        await SendAsync(page, new TestBrowserFile("scan-dos.tif", "pas une image"u8.ToArray()));

        var refused = page.Find(".edition-visuals-upload[data-refused]");
        Assert.Equal("scan-dos.tif", refused.QuerySelector(".edition-visuals-upload-name")!.TextContent);
        Assert.Equal("Refusé", refused.QuerySelector(".edition-visuals-upload-status")!.TextContent);
        Assert.Contains("n'est pas une image lisible", refused.QuerySelector(".edition-visuals-upload-reason")!.TextContent);
        Assert.Equal("3 visuels", page.Find(".edition-visuals-count").TextContent);
        Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent);

        await page.ClickAsync(".edition-visuals-upload-remove");

        Assert.Empty(page.FindAll(".edition-visuals-upload"));
    }

    // An upload that fails is not a refusal of the file: « Non envoyé », with a technical error in the compact form of the
    // single component of the errors, and none of the alert color of a refused file (#438).
    [Fact]
    public async Task Visuals_AnUploadThatFails_IsNotSent_WithATechnicalError_UntilItIsRemoved()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);

        await SendAsync(page, new TestBrowserFile("dedicace.png", new byte[16]));

        var failed = page.Find(".edition-visuals-upload");
        Assert.False(failed.HasAttribute("data-refused"));
        Assert.Equal("Non envoyé", failed.QuerySelector(".edition-visuals-upload-status")!.TextContent);
        Assert.Null(failed.QuerySelector(".edition-visuals-upload-reason"));
        var error = failed.QuerySelector("[role=alert]")!;
        Assert.Equal("technical", error.GetAttribute("data-error-category"));
        Assert.True(error.HasAttribute("data-error-compact"));
        Assert.Equal("L'envoi n'a pas pu aboutir : réessayez dans un instant.", error.QuerySelector("[data-error-text]")!.TextContent.Trim());
        Assert.Equal("3 visuels", page.Find(".edition-visuals-count").TextContent);

        await page.ClickAsync(".edition-visuals-upload-remove");

        Assert.Empty(page.FindAll(".edition-visuals-upload"));
    }

    [Fact]
    public async Task Visuals_AFileHeavierThanTheLimit_IsRefused_WithoutBeingSent()
    {
        _api.Answers["/admin/visuals/limits"] = new VisualLimits(1024 * 1024, TimeSpan.FromHours(24));
        var page = OpenOwned();

        await SendAsync(page, new TestBrowserFile("scan-600dpi.png", new byte[1024 * 1024 + 1]));

        var refused = page.Find(".edition-visuals-upload[data-refused]");
        Assert.Equal("Ce fichier dépasse le poids maximal de 1 Mio.", refused.QuerySelector(".edition-visuals-upload-reason")!.TextContent);
        Assert.Empty(_api.Written);
    }

    [Theory]
    [InlineData("visuals")]
    [InlineData("limits")]
    public void Load_FailingAfterTheEdition_SaysSo_WithoutTheForm(string reading)
    {
        // Read after the edition, what the form needs as well: the record is not shown until all of it is read.
        _api.Failures[reading switch
        {
            "visuals" => $"/admin/albums/{AlbumId}/editions/{EditionId}/visuals",
            _ => "/admin/visuals/limits",
        }] = HttpStatusCode.InternalServerError;

        var page = OpenOwned();

        Assert.Equal("technical", page.Find("[role=alert]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll("form"));
    }

    [Fact]
    public async Task Visuals_BeingSent_HoldTheSaving_UntilTheyAreInTheForm()
    {
        // Saved with the form: saving while a file is on its way would leave it out, then cancel its sending.
        var page = OpenOwned();
        var sent = new TaskCompletionSource();
        _api.Hold = (path, _) => path == "/admin/visuals/pending" ? sent.Task : Task.CompletedTask;
        _api.Write = (_, path, _) => path == "/admin/visuals/pending"
            ? FakeAdminApi.Json(new PendingVisualForm(AddedId, DateTimeOffset.UtcNow.AddHours(24)), HttpStatusCode.Created)
            : FakeAdminApi.Json(Owned);
        var sending = await ChooseAsync(page, new TestBrowserFile("dedicace.png", new byte[16]));
        Assert.Single(_api.Written);

        Assert.Equal("Envoi de fichiers en cours", page.Find(".save-bar-state-long").TextContent);
        Assert.True(page.Find(".save-bar-actions button[type=submit]").HasAttribute("disabled"));
        await page.SubmitAsync("form");
        Assert.Single(_api.Written);

        sent.SetResult();
        await sending.EndAsync("Sending dedicace.png");
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
        Assert.False(page.Find(".save-bar-actions button[type=submit]").HasAttribute("disabled"));
        await page.SubmitAsync("form");
        Assert.Equal(2, _api.Written.Count);
        Assert.Contains(((Guid?)null, (Guid?)AddedId, "Cover"), VisualsSent());
    }

    [Fact]
    public async Task Visuals_ChosenAfterFilesAllRefusedWithoutBeingSent_AreSent()
    {
        // Refused at once, these files ended the sending before any wait: the next choice is sent all the same.
        _api.Answers["/admin/visuals/limits"] = new VisualLimits(1024 * 1024, TimeSpan.FromHours(24));
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(new PendingVisualForm(AddedId, DateTimeOffset.UtcNow.AddHours(24)), HttpStatusCode.Created);
        await SendAsync(page, new TestBrowserFile("scan-600dpi.png", new byte[1024 * 1024 + 1]));
        Assert.NotNull(page.Find(".edition-visuals-upload[data-refused]"));

        await SendAsync(page, new TestBrowserFile("dedicace.png", new byte[16]));

        Assert.Equal("4 visuels", page.Find(".edition-visuals-count").TextContent);
        Assert.Single(_api.Written);
    }

    [Fact]
    public async Task Visuals_AnotherType_PlacesTheVisualAfterThoseOfItsNewType_SavedWithTheForm()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);

        await page.ClickAsync(() => page.FindAll(".edition-visual")[0].QuerySelectorAll(".actions-menu-panel button").First(item => item.TextContent.Trim() == "Planche"));

        Assert.Empty(_api.Written);
        Assert.Equal(["Planche 1", "Planche 2", "Planche 3"], page.FindAll(".edition-visual-mark-full").Select(mark => mark.TextContent));
        Assert.Contains("admin.edition-form.visual-type-changed", _audience.Functions);
        await page.SubmitAsync("form");
        Assert.Single(_api.Written);
        Assert.Equal([(FirstPlateId, (Guid?)null, "Plate"), (SecondPlateId, null, "Plate"), (CoverId, null, "Plate")], VisualsSent());
    }

    [Fact]
    public async Task Visuals_Dragged_TakesThePlaceOfAnotherOfItsType_ButNotOfAnotherType()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);

        await page.DragAndDropAsync(() => page.FindAll(".edition-visual")[2], () => page.FindAll(".edition-visual")[0]);
        Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent);

        await page.DragAndDropAsync(() => page.FindAll(".edition-visual")[2], () => page.FindAll(".edition-visual")[1]);

        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
        Assert.Contains("admin.edition-form.visuals-arranged", _audience.Functions);
        await page.SubmitAsync("form");
        Assert.Single(_api.Written);
        Assert.Equal([CoverId, SecondPlateId, FirstPlateId], VisualsSent().Select(v => v.Id!.Value));
    }

    [Fact]
    public async Task Visuals_Deleted_OnceConfirmed_LeaveTheFormWhichSavesWithoutThem()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);

        await page.ClickAsync(() => page.FindAll(".edition-visual")[1].QuerySelectorAll(".actions-menu-panel button")[^1]);
        var dialog = page.Find("dialog");
        Assert.Equal("Supprimer ce visuel ?", dialog.QuerySelector(".dialog-title")!.TextContent);
        Assert.Equal("Planche 1", dialog.QuerySelector(".dialog-subject")!.TextContent.Trim());
        Assert.Contains("à l'enregistrement de la fiche", dialog.TextContent);

        await page.ClickAsync(() => page.Find("dialog").QuerySelectorAll(".dialog-actions .button")[^1]);

        Assert.Empty(page.FindAll("dialog"));
        Assert.Equal("2 visuels", page.Find(".edition-visuals-count").TextContent);
        Assert.Equal(["admin.edition-form.visual-deletion", "admin.edition-form.visual-deleted"],
            _audience.Functions.Where(function => function.Contains("visual", StringComparison.Ordinal)));
        await page.SubmitAsync("form");
        Assert.Single(_api.Written);
        Assert.Equal([CoverId, SecondPlateId], VisualsSent().Select(v => v.Id!.Value));
    }

    [Fact]
    public async Task Visuals_AddedLongAgo_AreRemovedFromTheForm_WhichADialogTells()
    {
        // A form left open past the time a visual added is kept: removed from it, as a saving would refuse it.
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(new PendingVisualForm(AddedId, DateTimeOffset.UtcNow.AddSeconds(30)), HttpStatusCode.Created);

        await SendAsync(page, new TestBrowserFile("planche-oubliee.png", new byte[16]));

        var dialog = page.WaitForElement("dialog");
        Assert.Equal("Visuels retirés de la fiche", dialog.QuerySelector(".dialog-title")!.TextContent);
        Assert.Equal(["planche-oubliee.png"], dialog.QuerySelectorAll(".edition-visuals-expired li").Select(item => item.TextContent));
        Assert.Equal("3 visuels", page.Find(".edition-visuals-count").TextContent);
        await page.ClickAsync("dialog .dialog-actions .button");
        Assert.Empty(page.FindAll("dialog"));
    }

    [Fact]
    public async Task Visuals_AddedLongAgo_WhoseRemovalIsAsked_CloseTheRemovalDialog_ForTheOneThatTellsTheyAreRemoved()
    {
        // The removal of a visual asked, then the time it is kept elapsed: it is no longer in the form, nothing is left to remove.
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        _clock = clock;
        var page = OpenNewOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(new PendingVisualForm(AddedId, clock.GetUtcNow().AddHours(24)), HttpStatusCode.Created);
        await SendAsync(page, new TestBrowserFile("planche-oubliee.png", new byte[16]));
        await page.ClickAsync(() => page.Find(".edition-visual").QuerySelectorAll(".actions-menu-panel button")[^1]);
        Assert.Equal("Supprimer ce visuel ?", page.Find("dialog .dialog-title").TextContent);

        await page.InvokeAsync(() => clock.Advance(TimeSpan.FromHours(24)));

        var dialog = page.WaitForElement("dialog");
        Assert.Single(page.FindAll("dialog"));
        Assert.Equal("Visuels retirés de la fiche", dialog.QuerySelector(".dialog-title")!.TextContent);
        Assert.Equal(["planche-oubliee.png"], dialog.QuerySelectorAll(".edition-visuals-expired li").Select(item => item.TextContent));
        Assert.Equal("Aucun visuel", page.Find(".edition-visuals-count").TextContent);
    }

    [Fact]
    public async Task Visuals_Changed_AreShownAmongTheChanges_OfAnEditionModifiedMeanwhile()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        await page.DragAndDropAsync(() => page.FindAll(".edition-visual")[2], () => page.FindAll(".edition-visual")[1]);

        await page.SubmitAsync("form");
        await page.ClickAsync(() => page.Find("[role=alert]").QuerySelectorAll("button")[1]);

        var changes = page.Find("dialog .changes-dialog-list");
        Assert.Equal(["Visuels"], changes.QuerySelectorAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["3 visuels"], changes.QuerySelectorAll("dd").Select(dd => dd.TextContent));
    }

    // The visuals the last saving sent, in their order.
    private List<(Guid? Id, Guid? PendingId, string? Type)> VisualsSent() =>
    [
        .. _api.Written.Last().Body.GetProperty("visuals").EnumerateArray().Select(v => (
            v.GetProperty("id").ValueKind == JsonValueKind.Null ? (Guid?)null : v.GetProperty("id").GetGuid(),
            v.GetProperty("pendingId").ValueKind == JsonValueKind.Null ? (Guid?)null : v.GetProperty("pendingId").GetGuid(),
            v.GetProperty("type").GetString())),
    ];

    private FunctionUse FirstUse() =>
        _audience.Sent.Single(sent => sent.Path == "/audience/visits").Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!.FirstUse;

    private static AngleSharp.Dom.IElement Free(IRenderedComponent<AdminEditionForm> page) => page.Find("#edition-free");

    // A choice of files on the input of the next choice, raised on the renderer's dispatcher as the circuit receives it
    // (CircuitEvents): returns the task of its handling, which sends them and ends once the rendering it causes is done.
    private static async Task<Task> ChooseAsync(IRenderedComponent<AdminEditionForm> page, params IBrowserFile[] files)
    {
        var input = page.FindComponents<InputFile>()[^1];
        Task handling = null!;
        await page.InvokeAsync(() => { handling = input.Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs(files)); })
            .EndAsync("Choosing " + string.Join(", ", files.Select(file => file.Name)));
        return handling;
    }

    private static async Task SendAsync(IRenderedComponent<AdminEditionForm> page, params IBrowserFile[] files) =>
        await (await ChooseAsync(page, files)).EndAsync("Sending " + string.Join(", ", files.Select(file => file.Name)));

    private IRenderedComponent<AdminEditionForm> OpenOwned() => Open(AppPaths.AdminEditEdition(AlbumId, EditionId), EditionId);

    private IRenderedComponent<AdminEditionForm> OpenWished() => Open(AppPaths.AdminEditEdition(AlbumId, WishedId), WishedId);

    private IRenderedComponent<AdminEditionForm> OpenNewOwned() => Open(AppPaths.AdminNewAlbumEdition(AlbumId), null);

    private IRenderedComponent<AdminEditionForm> OpenNewIntent() => Open(AppPaths.AdminNewPurchaseIntent(AlbumId), null);

    private IRenderedComponent<AdminEditionForm> Open(string path, Guid? id)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);
        return Render<AdminEditionForm>(parameters => parameters
            .Add(p => p.AlbumId, AlbumId)
            .Add(p => p.Id, id)
            .AddCascadingValue(_request.Context));
    }

    private static void ChoosePublisher(IRenderedComponent<AdminEditionForm> page, RecordOption? publisher)
    {
        var picker = page.FindComponents<RecordPicker>().Single(p => p.Instance.Kind == RecordKind.Publisher);
        picker.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(publisher)).GetAwaiter().GetResult();
    }

    private static async Task CreatePublisherOnTheSpotAsync(IRenderedComponent<AdminEditionForm> page, string name)
    {
        var picker = page.FindComponents<RecordPicker>().Single(p => p.Instance.Kind == RecordKind.Publisher);
        await picker.InvokeAsync(() => picker.Instance.OnCreate.InvokeAsync(name));
        await page.ClickAsync(".dialog-actions .button[data-variant=primary]");
    }

    // The options of the list of the collections, its creation last, open or not.
    private static List<string> CollectionOptions(IRenderedComponent<AdminEditionForm> page) =>
        [.. page.FindAll("#edition-collection-options [role=option]").Select(option => option.TextContent.Trim())];

    // Opens the list of the collections, and chooses its option at that place: « — » first, its creation last.
    private static async Task ChooseCollectionAsync(IRenderedComponent<AdminEditionForm> page, int index)
    {
        await page.ClickAsync("#edition-collection");
        await page.ClickAsync($"#edition-collection-option-{index}");
    }

    private static async Task CreateCollectionOnTheSpotAsync(IRenderedComponent<AdminEditionForm> page, string name)
    {
        await page.ClickAsync("#edition-collection");
        await page.ClickAsync("#edition-collection-options .option-list-create");
        await page.ChangeAsync("#edition-collection-creation-name", name);
        await page.ClickAsync(".dialog-actions .button[data-variant=primary]");
    }
}
