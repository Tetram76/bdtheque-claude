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
using Microsoft.AspNetCore.Components.Web;
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

        Assert.Equal(["Administration", "Albums", "Les Bijoux de la Castafiore (Tintin - T. 21)", "Casterman [2007]"],
            page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
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
        Assert.Equal("", page.Find("#edition-collection").GetAttribute("value"));
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

        Assert.All(Amounts, id => Assert.True(string.IsNullOrEmpty(page.Find($"#edition-{id}").GetAttribute("value"))));
        Assert.All(AmountControls, id => Assert.True(page.Find($"#edition-{id}").HasAttribute("disabled")));

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
                "FRF · franc français", "BEF · franc belge", "LUF · franc luxembourgeois", "QZF · ancien franc français", "Autre devise…"],
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
        Assert.Equal(["L'édition", "Fabrication", "Les visuels"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
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
        Assert.Equal(["L'édition", "Fabrication", "Les visuels"], page.FindAll(".block-title").Select(title => title.TextContent.Trim()));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FunctionalError_ModifiedMeanwhileOnCreation_OffersToReloadTheNewRecordAndToSeeTheChanges(bool intent)
    {
        // A new edition, or intent, is written under the version of its album, as a modified one: the album modified meanwhile,
        // the creation is to start again from its current state, which only a reload reads.
        var page = intent ? OpenNewIntent() : OpenNewOwned();
        var path = intent ? AppPaths.AdminNewPurchaseIntent(AlbumId) : AppPaths.AdminNewAlbumEdition(AlbumId);
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        ChoosePublisher(page, new RecordOption(Dargaud, "Dargaud", null));
        page.Find("#edition-year").Change("2011");
        if (!intent)
            page.Find("#edition-mode").Change("Gift");
        page.Find("form").Submit();

        var error = page.WaitForElement("[role=alert]");
        Assert.Equal("functional", error.GetAttribute("data-error-category"));
        var buttons = error.QuerySelectorAll("button");
        Assert.Equal(["Recharger la fiche", "Voir mes changements"], buttons.Select(button => button.TextContent.Trim()));
        buttons[1].Click();
        Assert.Contains("Année d'édition", page.Find("dialog .changes-dialog-list").TextContent);
        Assert.Contains(intent ? "admin.purchase-intent-form.changes" : "admin.edition-form.changes", _audience.Functions);

        page.Find("[role=alert]").QuerySelectorAll("button")[0].Click();

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(path, new Uri(navigation.Uri).AbsolutePath);
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
    public void Visuals_ABlockOfTheForm_InASingleGrid_InTheOrderOfTheTypes_EachMarkedWithItsType()
    {
        // visuel/saisie-edition/contenu.md, Les visuels: the last block of the form, saved with it, which the block says, then the
        // drop zone, the number of visuals and their grid, a plate counted among the plates.
        var page = OpenOwned();
        var block = page.Find("form .edition-visuals");

        Assert.Contains("Les visuels s'enregistrent avec la fiche", block.TextContent);
        Assert.Contains("conservé 24 heures", block.TextContent);
        Assert.Equal("JPEG, PNG, WebP ou GIF, 30 Mio au plus.", block.QuerySelector(".edition-visuals-formats")!.TextContent);
        Assert.Equal("3 visuels", block.QuerySelector(".edition-visuals-count")!.TextContent);
        var visuals = block.QuerySelectorAll(".edition-visual");
        Assert.Equal(["Couv.", "Pl. 1", "Pl. 2"], visuals.Select(v => v.QuerySelector(".edition-visual-mark")!.TextContent));
        Assert.Equal(["Couverture", "Planche 1", "Planche 2"], visuals.Select(v => v.QuerySelector("img")!.GetAttribute("alt")));
        Assert.Equal(VisualMediaRoutes.Display(CoverId), visuals[0].QuerySelector("img")!.GetAttribute("src"));
        // Its menu: the type, the current one pressed, the original beside the application, the deletion.
        var menu = visuals[1].QuerySelector(".actions-menu-panel")!;
        Assert.Equal("Actions sur Planche 1", visuals[1].QuerySelector(".actions-menu-toggle")!.GetAttribute("aria-label"));
        Assert.Equal(["Planche"], menu.QuerySelectorAll("[aria-pressed=true]").Select(item => item.TextContent.Trim()));
        Assert.Equal(VisualMediaRoutes.Original(FirstPlateId), menu.QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal("_blank", menu.QuerySelector("a")!.GetAttribute("target"));
        Assert.Equal("Supprimer le visuel…", menu.QuerySelectorAll("button").Last().TextContent.Trim());
    }

    [Fact]
    public void Visuals_Added_WaitInTheForm_WhichSavesThemInTheOrderOfTheTypes()
    {
        var page = OpenOwned();
        _api.Write = (method, path, _) => path == "/admin/visuals/pending"
            ? FakeAdminApi.Json(new PendingVisualForm(AddedId, DateTimeOffset.UtcNow.AddHours(24)), HttpStatusCode.Created)
            : FakeAdminApi.Json(Owned);

        page.Find("#edition-visuals-type").Change("Dedication");
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[2048], "dedicace.png"));

        page.WaitForAssertion(() => Assert.Equal("4 visuels", page.Find(".edition-visuals-count").TextContent));
        var (_, _, form) = _api.Written.Single();
        Assert.Equal("dedicace.png", form.GetProperty(UploadVisualFields.File).GetProperty("name").GetString());
        Assert.Equal(2048, form.GetProperty(UploadVisualFields.File).GetProperty("length").GetInt64());
        // After the cover, before the plates; shown from the waiting area; an unsaved change of the form.
        var added = page.FindAll(".edition-visual")[1];
        Assert.Equal(("Déd.", "/admin/visuals/pending/" + AddedId + "/display"),
            (added.QuerySelector(".edition-visual-mark")!.TextContent, added.QuerySelector("img")!.GetAttribute("src")));
        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
        Assert.Contains("admin.edition-form.visual-added", _audience.Functions);

        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Equal(HttpMethod.Put, _api.Written.Last().Method));
        Assert.Equal(
            [(CoverId, (Guid?)null, "Cover"), (null, AddedId, "Dedication"), (FirstPlateId, null, "Plate"), (SecondPlateId, null, "Plate")],
            VisualsSent());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Visuals_OfANewEdition_AreSavedWithIt(bool intent)
    {
        var page = intent ? OpenNewIntent() : OpenNewOwned();
        _api.Write = (_, path, _) => path == "/admin/visuals/pending"
            ? FakeAdminApi.Json(new PendingVisualForm(AddedId, DateTimeOffset.UtcNow.AddHours(24)), HttpStatusCode.Created)
            : intent ? FakeAdminApi.Json(new PurchaseIntentsForm(AlbumId, [], 13), HttpStatusCode.Created) : FakeAdminApi.Json(Owned, HttpStatusCode.Created);
        ChoosePublisher(page, new RecordOption(Dargaud, "Dargaud", null));
        if (!intent)
            page.Find("#edition-mode").Change("Gift");

        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[16], "couverture.png"));
        page.WaitForAssertion(() => Assert.Equal("1 visuel", page.Find(".edition-visuals-count").TextContent));
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Equal(2, _api.Written.Count));
        Assert.Equal([((Guid?)null, (Guid?)AddedId, "Cover")], VisualsSent());
    }

    [Fact]
    public void Visuals_ARefusedFile_SaysWhy_UntilItIsRemoved()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.BadRequest, "EditionVisual.FileNotSupportedImage");

        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("pas une image", "scan-dos.tif"));

        var refused = page.WaitForElement(".edition-visuals-upload[data-refused]");
        Assert.Equal("scan-dos.tif", refused.QuerySelector(".edition-visuals-upload-name")!.TextContent);
        Assert.Equal("Refusé", refused.QuerySelector(".edition-visuals-upload-status")!.TextContent);
        Assert.Contains("n'est pas une image lisible", refused.QuerySelector(".edition-visuals-upload-reason")!.TextContent);
        Assert.Equal("3 visuels", page.Find(".edition-visuals-count").TextContent);
        Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent);

        page.Find(".edition-visuals-upload-remove").Click();

        Assert.Empty(page.FindAll(".edition-visuals-upload"));
    }

    [Fact]
    public void Visuals_AnotherType_PlacesTheVisualAfterThoseOfItsNewType_SavedWithTheForm()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);

        page.FindAll(".edition-visual")[0].QuerySelectorAll(".actions-menu-panel button").First(item => item.TextContent.Trim() == "Planche").Click();

        Assert.Empty(_api.Written);
        Assert.Equal(["Pl. 1", "Pl. 2", "Pl. 3"], page.FindAll(".edition-visual-mark").Select(mark => mark.TextContent));
        Assert.Contains("admin.edition-form.visual-type-changed", _audience.Functions);
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Single(_api.Written));
        Assert.Equal([(FirstPlateId, (Guid?)null, "Plate"), (SecondPlateId, null, "Plate"), (CoverId, null, "Plate")], VisualsSent());
    }

    [Fact]
    public void Visuals_Dragged_TakesThePlaceOfAnotherOfItsType_ButNotOfAnotherType()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);

        page.FindAll(".edition-visual")[2].TriggerEvent("ondragstart", new DragEventArgs());
        page.FindAll(".edition-visual")[0].TriggerEvent("ondrop", new DragEventArgs());
        Assert.Equal("Aucune modification", page.Find(".save-bar-state-long").TextContent);

        page.FindAll(".edition-visual")[2].TriggerEvent("ondragstart", new DragEventArgs());
        page.FindAll(".edition-visual")[1].TriggerEvent("ondrop", new DragEventArgs());

        Assert.Equal("Modifications non enregistrées", page.Find(".save-bar-state-long").TextContent);
        Assert.Contains("admin.edition-form.visuals-arranged", _audience.Functions);
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Single(_api.Written));
        Assert.Equal([CoverId, SecondPlateId, FirstPlateId], VisualsSent().Select(v => v.Id!.Value));
    }

    [Fact]
    public void Visuals_Deleted_OnceConfirmed_LeaveTheFormWhichSavesWithoutThem()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(Owned);

        page.FindAll(".edition-visual")[1].QuerySelectorAll(".actions-menu-panel button").Last().Click();
        var dialog = page.Find("dialog");
        Assert.Equal("Supprimer ce visuel ?", dialog.QuerySelector(".dialog-title")!.TextContent);
        Assert.Equal("Planche 1", dialog.QuerySelector(".dialog-subject")!.TextContent.Trim());
        Assert.Contains("à l'enregistrement de la fiche", dialog.TextContent);

        dialog.QuerySelectorAll(".dialog-actions .button").Last().Click();

        Assert.Empty(page.FindAll("dialog"));
        Assert.Equal("2 visuels", page.Find(".edition-visuals-count").TextContent);
        Assert.Equal(["admin.edition-form.visual-deletion", "admin.edition-form.visual-deleted"],
            _audience.Functions.Where(function => function.Contains("visual", StringComparison.Ordinal)));
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Single(_api.Written));
        Assert.Equal([CoverId, SecondPlateId], VisualsSent().Select(v => v.Id!.Value));
    }

    [Fact]
    public void Visuals_AddedLongAgo_AreRemovedFromTheForm_WhichADialogTells()
    {
        // A form left open past the time a visual added is kept: removed from it, as a saving would refuse it.
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Json(new PendingVisualForm(AddedId, DateTimeOffset.UtcNow.AddSeconds(30)), HttpStatusCode.Created);

        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[16], "planche-oubliee.png"));

        var dialog = page.WaitForElement("dialog");
        Assert.Equal("Visuels retirés de la fiche", dialog.QuerySelector(".dialog-title")!.TextContent);
        Assert.Equal(["planche-oubliee.png"], dialog.QuerySelectorAll(".edition-visuals-expired li").Select(item => item.TextContent));
        Assert.Equal("3 visuels", page.Find(".edition-visuals-count").TextContent);
        dialog.QuerySelector(".dialog-actions .button")!.Click();
        Assert.Empty(page.FindAll("dialog"));
    }

    [Fact]
    public void Visuals_Changed_AreShownAmongTheChanges_OfAnEditionModifiedMeanwhile()
    {
        var page = OpenOwned();
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        page.FindAll(".edition-visual")[2].TriggerEvent("ondragstart", new DragEventArgs());
        page.FindAll(".edition-visual")[1].TriggerEvent("ondrop", new DragEventArgs());

        page.Find("form").Submit();
        page.WaitForElement("[role=alert]").QuerySelectorAll("button")[1].Click();

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
