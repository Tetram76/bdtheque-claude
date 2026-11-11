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
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The entry form of an edition and of an edition targeted by a purchase intent (visuel/saisie-edition/contenu.md): its blocks,
/// its contextual labels, its freeness, its ISBN check, its pre-filling, its saving and the three categories of its errors, told
/// apart by role, field and text, never by appearance. What it shares with the other forms (frame, choice of a record) is fixed by
/// their tests.
/// </summary>
public sealed class AdminEditionFormTests : BunitContext, IDisposable
{
    private static readonly Guid AlbumId = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid EditionId = Guid.Parse("01900000-0000-7000-8000-0000000000a2");
    private static readonly Guid WishedId = Guid.Parse("01900000-0000-7000-8000-0000000000a3");
    private static readonly Guid IntentId = Guid.Parse("01900000-0000-7000-8000-0000000000a4");
    private static readonly Guid SeriesId = Guid.Parse("01900000-0000-7000-8000-0000000000a5");
    private static readonly Guid Casterman = Guid.Parse("01900000-0000-7000-8000-0000000000e1");
    private static readonly Guid FacSimile = Guid.Parse("01900000-0000-7000-8000-0000000000e2");
    private static readonly Guid Pastiches = Guid.Parse("01900000-0000-7000-8000-0000000000e3");
    private static readonly Guid Dargaud = Guid.Parse("01900000-0000-7000-8000-0000000000e4");

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

    public AdminEditionFormTests()
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
        _api.Answers[$"/admin/albums/{AlbumId}/editions/{EditionId}"] = Owned;
        _api.Answers[$"/admin/albums/{AlbumId}/editions/{WishedId}"] = Wished;
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

        Assert.Equal(["Administration", "Albums", "Les Bijoux de la Castafiore (Tintin - T. 21)", "Casterman [2007]"],
            page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal("Modifier l'édition", page.Find("h1").TextContent.Trim());
        Assert.Equal("Casterman [2007] - ISBN 978-2-203-00120-8", page.Find(".entry-form-subject").TextContent.Trim());
        // The album leads to the page of its editions from the breadcrumb, and to its own form from the heading; neither link
        // to the consultation, nor deletion, nor confirmation of a purchase: those are the page of the editions'.
        Assert.Equal(
            [AppPaths.AdminHome, AppPaths.AdminAlbums, AppPaths.AdminAlbumEditions(AlbumId), AppPaths.AdminEditAlbum(AlbumId)],
            page.FindAll("a").Select(link => link.GetAttribute("href")).Distinct());
    }

    [Fact]
    public void Blocks_EachWithItsTitle_InTheOrderOfTheForm()
    {
        var page = OpenOwned();

        Assert.Equal(["L'édition", "Fabrication", "L'exemplaire", "Valeur"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(["Publication", "Objet", "Possession", "Montants"], page.FindAll(".block-kicker").Select(kicker => kicker.TextContent.Trim()));
    }

    [Fact]
    public void Fields_TheEditionAsRead()
    {
        var page = OpenOwned();

        Assert.Equal("Casterman", page.Find("#edition-publisher").GetAttribute("value"));
        Assert.Equal("", page.Find("#edition-collection").GetAttribute("value"));
        Assert.Equal("2007", page.Find("#edition-year").GetAttribute("value"));
        Assert.Equal("978-2-203-00120-8", page.Find("#edition-isbn").GetAttribute("value"));
        Assert.Equal(["", "Standard", "Hardcover", "Portrait", "LeftToRight"],
            new[] { "category", "format", "binding", "orientation", "direction" }.Select(id => page.Find($"#edition-{id}").GetAttribute("value")));
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
        Assert.Equal(["—", "Fac-similé", "Pastiches"], page.FindAll("#edition-collection option").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void Labels_FollowTheModeOfAcquisition()
    {
        // fonctionnel/presentation/libelles-contextuels-sur-l-edition.md
        var page = OpenOwned();

        Assert.Equal("Date d'achat", page.Find("label[for=edition-date]").TextContent.Trim());
        Assert.Equal("Prix d'achat", page.Find("#edition-price-legend").TextContent.Trim());

        page.Find("#edition-mode").Change("Gift");

        Assert.Equal("Date d'acquisition", page.Find("label[for=edition-date]").TextContent.Trim());
        Assert.Equal("Valeur d'acquisition", page.Find("#edition-price-legend").TextContent.Trim());
    }

    [Fact]
    public void Free_IsUnavailableForAPurchase_AndEmptiesAndDisablesTheAmounts()
    {
        var page = OpenOwned();

        Assert.True(Free(page).HasAttribute("disabled"));

        page.Find("#edition-mode").Change("Gift");
        Free(page).Change(true);

        Assert.All(new[] { "price", "initial" }, id => Assert.True(string.IsNullOrEmpty(page.Find($"#edition-{id}").GetAttribute("value"))));
        Assert.All(new[] { "price", "price-currency", "initial", "initial-currency" }, id => Assert.True(page.Find($"#edition-{id}").HasAttribute("disabled")));

        // A purchase is never free: choosing it unchecks the box, which it disables.
        page.Find("#edition-mode").Change("Purchase");

        Assert.False(Free(page).HasAttribute("checked"));
        Assert.False(page.Find("#edition-price").HasAttribute("disabled"));
    }

    [Fact]
    public void Isbn_CheckedAsItIsEntered_WithoutBlocking()
    {
        // fonctionnel/regles-metier/validation-de-l-isbn.md
        var page = OpenOwned();
        Assert.Contains("Chiffre de contrôle correct.", page.Find("#edition-isbn-help").TextContent);
        _api.Answers["/admin/editions/isbn-check?isbn=978-2-203-00120-7"] = new IsbnCheck(false, "978-2-203-00120-7");

        page.Find("#edition-isbn").Input("978-2-203-00120-7");

        page.WaitForAssertion(() => Assert.Contains("Chiffre de contrôle incohérent", page.Find("#edition-isbn-help").TextContent));
        Assert.Contains("peut être enregistré tel quel", page.Find("#edition-isbn-help").TextContent);
        Assert.Equal("true", page.Find("#edition-isbn").GetAttribute("data-warning"));
    }

    [Fact]
    public void Isbn_TheCheckCharacterIsWrittenInCapitalsAsItIsEntered()
    {
        var page = OpenOwned();
        _api.Answers["/admin/editions/isbn-check?isbn=2-905123-01-x"] = new IsbnCheck(false, "2-905123-01-X");

        page.Find("#edition-isbn").Input("2-905123-01-x");

        page.WaitForAssertion(() => Assert.Equal("2-905123-01-X", page.Find("#edition-isbn").GetAttribute("value")));
    }

    [Fact]
    public void Label_FollowsTheTyping()
    {
        var page = OpenOwned();

        page.Find("#edition-year").Change("2011");

        Assert.Equal("Casterman [2011] - ISBN 978-2-203-00120-8", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
    }

    [Fact]
    public void Collection_TheChangeOfPublisherRemovesIt_AndListsItsOwn()
    {
        var page = OpenOwned();
        page.Find("#edition-collection").Change(Pastiches.ToString());

        ChoosePublisher(page, new RecordOption(Dargaud, "Dargaud", null));

        Assert.Equal("", page.Find("#edition-collection").GetAttribute("value"));
        Assert.Equal(["—"], page.FindAll("#edition-collection option").Select(option => option.TextContent.Trim()));

        ChoosePublisher(page, null);

        Assert.True(page.Find("#edition-collection").HasAttribute("disabled"));
    }

    [Fact]
    public void Currency_NamedInItsList_AnyOtherEnteredByItsCode()
    {
        // fonctionnel/valorisation/gestion-des-devises.md: any currency, the old franc included.
        var page = OpenOwned();
        var currencies = page.FindAll("#edition-price-currency option").Select(option => option.TextContent.Trim()).ToList();
        // In the order of their likely use for the purchase of a new edition today, the old franc among them.
        Assert.Equal(
            ["EUR · euro", "USD · dollar des États-Unis", "CHF · franc suisse", "CAD · dollar canadien", "GBP · livre sterling",
                "FRF · franc français", "BEF · franc belge", "QZF · ancien franc français", "LUF · franc luxembourgeois", "Autre devise…"],
            currencies);

        page.Find("#edition-price-currency").Change("other");
        page.Find("#edition-price-code").Change("jpy");
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Single(_api.Written));
        Assert.Equal("JPY", _api.Written.Single().Body.GetProperty("content").GetProperty("acquisitionCurrency").GetString());
    }

    [Fact]
    public void Save_SendsTheWholeFormAtTheVersionOfTheAlbum_RecordsTheSaving_AndLeadsBackToTheEditionsOfTheAlbum()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);

        page.Find("#edition-pages").Change("64");
        page.Find("form").Submit();

        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.Equal(AppPaths.AdminAlbumEditions(AlbumId), new Uri(navigation.Uri).AbsolutePath));
        var (method, path, body) = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/albums/{AlbumId}/editions/{EditionId}"), (method, path));
        var request = body.Deserialize<UpdateEditionRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(12u, request.AlbumVersion);
        Assert.Equal(Owned.Content with { PageCount = 64 }, request.Content);
        Assert.Contains("admin.edition-form.saved", _audience.Functions);
    }

    [Fact]
    public void NewEdition_PreFilledFromTheSeries_SaysWhichFields_AndIsCreated()
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

        page.Find("#edition-mode").Change("Purchase");
        page.Find("form").Submit();

        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.Equal(AppPaths.AdminAlbumEditions(AlbumId), new Uri(navigation.Uri).AbsolutePath));
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

        Assert.Equal("Modifier l'intention d'achat", page.Find("h1").TextContent.Trim());
        Assert.Equal(["L'édition", "Fabrication"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(["En couleur"], page.FindAll(".choice").Select(choice => choice.TextContent.Trim()));
        Assert.Equal("Intention d'achat posée le 08/10/2026 12:00, modifiée le 12/10/2026 12:00.", page.Find(".entry-form-dates").TextContent.Trim());
        Assert.Equal(FacSimile.ToString(), page.Find("#edition-collection").GetAttribute("value"));
    }

    [Fact]
    public void PurchaseIntent_IsSavedAsAnEditionNotOwned()
    {
        var page = OpenWished();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Wished);

        page.Find("#edition-year").Change("2014");
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Single(_api.Written));
        var (method, path, body) = _api.Written.Single();
        Assert.Equal((HttpMethod.Put, $"/admin/albums/{AlbumId}/editions/{WishedId}"), (method, path));
        Assert.Equal(Wished.Content with { PublicationYear = 2014 }, body.Deserialize<UpdateEditionRequest>(JsonSerializerOptions.Web)!.Content);
        Assert.Contains("admin.purchase-intent-form.saved", _audience.Functions);
    }

    [Fact]
    public void NewPurchaseIntent_OnANewEdition_IsCreated()
    {
        var page = OpenNewIntent();

        Assert.Equal("Nouvelle intention d'achat", page.Find("h1").TextContent.Trim());
        Assert.Equal("Nouvelle intention d'achat", page.FindAll(".breadcrumb li")[^1].TextContent.Trim());
        Assert.Equal(["L'édition", "Fabrication"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
        Assert.Equal("Créer l'intention d'achat", page.Find(".save-bar button[type=submit]").TextContent.Trim());
        Assert.Contains("Pré-remplie d'après la série Tintin", page.Find(".edition-preset").TextContent);
        _api.Write = (_, _, _) => FakeAdminApi.Json(new PurchaseIntentsForm(AlbumId, [new PurchaseIntentForm(IntentId, WishedId)], 13),
            HttpStatusCode.Created);

        page.Find("#edition-year").Change("2013");
        page.Find("form").Submit();

        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.Equal(AppPaths.AdminAlbumEditions(AlbumId), new Uri(navigation.Uri).AbsolutePath));
        var (method, path, body) = _api.Written.Single();
        Assert.Equal((HttpMethod.Post, $"/admin/albums/{AlbumId}/purchase-intents"), (method, path));
        var request = body.Deserialize<CreatePurchaseIntentRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal(NewIntent.Edition with { PublicationYear = 2013 }, request.Edition);
        Assert.Equal(12u, request.AlbumVersion);
        Assert.Contains("admin.purchase-intent-form.created", _audience.Functions);
    }

    [Theory]
    [InlineData("Edition.PublisherRequired", "edition-publisher", "Obligatoire.")]
    [InlineData("Edition.AcquisitionModeRequired", "edition-mode", "Obligatoire pour une édition possédée.")]
    [InlineData("Edition.AcquisitionPriceReferenceDateRequired", "edition-price", "Sans date de référence, ce prix ne peut pas être enregistré.")]
    [InlineData("Edition.InitialValueReferenceDateRequired", "edition-initial", "Sans date de référence, cette valeur ne peut pas être enregistrée.")]
    [InlineData("Edition.PageCountPositive", "edition-pages", "Supérieur à zéro.")]
    public void BusinessError_ToCorrect_PointsToItsField(string rule, string field, string message)
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, rule);

        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("business", error.GetAttribute("data-error-category"));
        Assert.Equal("true", page.Find($"#{field}").GetAttribute("aria-invalid"));
        Assert.Contains(message, page.Find($"#{field}-error").TextContent);
    }

    [Fact]
    public void FunctionalError_ModifiedMeanwhile_OffersToReloadAndToSeeTheChanges()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);

        page.Find("#edition-year").Change("2011");
        page.Find("#edition-mode").Change("Gift");
        page.Find("#edition-price").Change("12,5");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        Assert.Contains("L'album de cette édition a été modifié entre-temps", error.TextContent);

        error.QuerySelectorAll("button")[1].Click();

        var changes = page.Find("dialog .changes-dialog-list");
        Assert.Equal(["Année d'édition", "Mode d'acquisition", "Valeur d'acquisition"], changes.QuerySelectorAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["2011", "Offerte", "12,50 €"], changes.QuerySelectorAll("dd").Select(dd => dd.TextContent));
        Assert.Contains("admin.edition-form.changes", _audience.Functions);
    }

    [Fact]
    public void TechnicalError_KeepsTheChanges_AndOffersToTryAgain()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);

        page.Find("#edition-year").Change("2011");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
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

    private FunctionUse FirstUse() =>
        _audience.Sent.Single(sent => sent.Path == "/audience/visits").Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!.FirstUse;

    private static AngleSharp.Dom.IElement Free(IRenderedComponent<AdminEditionForm> page) => page.Find("#edition-free");

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
}
