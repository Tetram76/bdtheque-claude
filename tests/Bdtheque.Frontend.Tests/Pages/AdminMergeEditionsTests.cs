using System.Globalization;
using System.Net;
using System.Text.Json;
using Bdtheque.Contracts.Admin;
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
/// The merge of two editions (visuel/fusion/fusion-de-deux-fiches.md): a row per data, in the blocks of the entry form of an
/// edition, the album first, the publisher and its collection together, the acquisition whole, and a row per visual, on its
/// thumbnail; what is retained at once and what waits for the user; the label the data chosen give; the intent the merge withdraws
/// once the edition is owned, said apart; the confirmation, which sets apart what the merge will delete; what the merge sends.
/// What every merge presents the same is fixed by MergeTests.
/// </summary>
public sealed class AdminMergeEditionsTests : BunitContext, IDisposable
{
    private static readonly Guid First = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid Second = Guid.Parse("01900000-0000-7000-8000-0000000000a2");
    private static readonly Guid FirstCover = Guid.Parse("01900000-0000-7000-8000-0000000000b1");
    private static readonly Guid SecondCover = Guid.Parse("01900000-0000-7000-8000-0000000000b2");
    private static readonly Guid SecondPlate = Guid.Parse("01900000-0000-7000-8000-0000000000b3");
    private static readonly PublisherListItem Dupuis = new(Guid.Parse("01900000-0000-7000-8000-0000000000c1"), "Dupuis", null);
    private static readonly PublisherCollectionListItem Spirou = new(Guid.Parse("01900000-0000-7000-8000-0000000000c2"), "Spirou", Dupuis.Id, "Dupuis");
    private static readonly AlbumSummary NewYork = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000d1"), "Spirou à New York", null, null, AlbumType.Regular, false, null, null, null, true);
    private static readonly AlbumSummary NewYorkAgain = NewYork with { Id = Guid.Parse("01900000-0000-7000-8000-0000000000d2"), Title = "Spirou a New-York" };
    private static readonly DateTimeOffset Modified = new(2026, 10, 2, 8, 0, 0, TimeSpan.Zero);

    // « Dupuis (Spirou) [1987] », of « Spirou à New York », bought, its cover; « Dupuis [1987] », of « Spirou a New-York », the
    // same year and format, offered, free, its cover and a plate.
    private static readonly EditionMergeRecord FirstEdition = new(
        First, NewYork, Dupuis, Spirou, 1987, null, EditionCategory.FirstEdition, EditionFormat.Standard, BindingType.Hardcover, null, null,
        48, true, AcquisitionMode.Purchase, new DateOnly(2004, 3, 12), 12.5m, "EUR", false, null, null, EditionCondition.Excellent, false,
        false, null, null, false, [new EditionVisualItem(FirstCover, VisualType.Cover, 0, Modified, Modified)], Modified, 11);

    private static readonly EditionMergeRecord SecondEdition = FirstEdition with
    {
        Id = Second, Album = NewYorkAgain, PublisherCollection = null, Category = null, AcquisitionMode = AcquisitionMode.Gift,
        AcquisitionDate = null, AcquisitionAmount = null, AcquisitionCurrency = null, IsFree = true, PersonalReference = "B-12",
        Visuals = [new EditionVisualItem(SecondCover, VisualType.Cover, 0, Modified, Modified), new EditionVisualItem(SecondPlate, VisualType.Plate, 0, Modified, Modified)],
        Version = 12,
    };

    private static readonly EditionMergeOutcome NothingWithdrawn = new(MergeSide.First, [new MergeLinkCount(EntityKind.PurchaseIntent, 0, 0, 0, 0)], []);

    private static readonly EditionMergePreview Preview = new(FirstEdition, SecondEdition, [NothingWithdrawn, NothingWithdrawn with { Acquisition = MergeSide.Second }]);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "planche", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminMergeEditionsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _api.Answers[$"/admin/editions/merge?first={First}&second={Second}"] = Preview;
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public async Task Rows_EachDataInTheBlocksOfTheEntryForm_AndEachVisualOfEitherEdition()
    {
        var page = Open();
        // Every data presented (« Tout afficher »).
        await page.ChangeAsync(".merge-show-all", true);

        Assert.Equal("Fusionner deux éditions", page.Find("h1").TextContent.Trim());
        Assert.Equal(["L'édition", "Fabrication", "L'exemplaire", "Les visuels"],
            page.FindAll(".block-title").Where(title => title.Closest(".merge-data") is not null).Select(title => title.TextContent.Trim()));
        Assert.Equal(["Publication", "Objet", "Possession", "Images"], page.FindAll(".merge-data .block-kicker").Select(kicker => kicker.TextContent.Trim()));
        Assert.Equal(
            [
                "Album", "Éditeur et collection", "Année d'édition", "ISBN", "Catégorie", "Format", "Reliure", "Orientation", "Sens de lecture",
                "Pages", "En couleur", "Acquisition", "État", "D'occasion", "Dédicacée", "Numérotation personnelle", "Notes personnelles",
                "Visuel", "Visuel", "Visuel",
            ],
            page.FindAll(".merge-choice-label").Select(label => label.TextContent.Trim()));
        // The album of each edition; the publisher and its collection together; the acquisition whole, its amounts included.
        Assert.Equal(["Spirou à New York", "Spirou a New-York"], Values(page, "merge-album"));
        Assert.Equal(["Dupuis (Spirou)", "Dupuis"], Values(page, "merge-publisher"));
        Assert.Equal(["Achat, le 12/03/2004 · prix d'achat 12,50 €", "Offerte · gratuite"], Values(page, "merge-acquisition"));
        // Each visual on its thumbnail, named by its type, on the side of its edition.
        Assert.Equal(["Couverture", "—Non renseigné"], Values(page, $"merge-visual-{FirstCover}"));
        Assert.Equal(["—Non renseigné", "Planche 1"], Values(page, $"merge-visual-{SecondPlate}"));
        Assert.Single(page.FindAll($"input[name=merge-visual-{FirstCover}] ~ .merge-choice-picture img"));
        Assert.Equal("« Dupuis (Spirou) [1987] » et « Dupuis [1987] »", page.Find(".entry-form-subject").TextContent.Trim());
    }

    [Fact]
    public void Choices_AValueOfOneEditionRetained_TwoValuesWaitingForTheUser_EachVisualRetainedByItsEdition()
    {
        var page = Open();

        Assert.Equal("First", Checked(page, "merge-category"));
        Assert.Equal("Second", Checked(page, "merge-reference"));
        Assert.Equal("First", Checked(page, $"merge-visual-{FirstCover}"));
        Assert.Equal("Second", Checked(page, $"merge-visual-{SecondPlate}"));
        // The album, the publisher with its collection and the acquisition differ.
        Assert.Equal("3 données restent à choisir", page.Find(".save-bar-state-long").TextContent);
        Assert.All(["merge-album", "merge-publisher", "merge-acquisition"], id => Assert.Null(Checked(page, id)));
        // The year, the same in both editions, left out of the reduced display.
        Assert.DoesNotContain("Année d'édition", page.FindAll(".merge-choice-label").Select(label => label.TextContent.Trim()));
    }

    [Fact]
    public async Task Result_TheLabelTheDataChosenGive()
    {
        var page = Open();
        Assert.Empty(page.FindAll(".merge-data-result"));

        await page.ChangeAsync("input[name=merge-publisher][value=Second]", true);

        Assert.Equal("Affiché : Dupuis [1987]", string.Join(' ', page.Find(".merge-data-result").TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));
    }

    [Fact]
    public async Task Links_OfAnEditionTargeted_SayItsIntentWithdrawn_OnlyOnceTheAcquisitionOfTheOwnedOneIsChosen()
    {
        _api.Answers[$"/admin/editions/merge?first={First}&second={Second}"] = Preview with
        {
            Second = SecondEdition with { AcquisitionMode = null, IsFree = false, IsTargeted = true },
            Outcomes =
            [
                NothingWithdrawn with { WithdrawnPurchaseIntents = [MergeSide.Second] },
                NothingWithdrawn with { Acquisition = MergeSide.Second, Links = [new MergeLinkCount(EntityKind.PurchaseIntent, 1, 0, 1, 0)] },
            ],
        };
        var page = Open();
        // The edition targeted presented as the intent.
        Assert.Contains("intention d'acquisition", page.FindAll(".merge-data-head-detail")[1].TextContent);
        Assert.Equal(["Achat, le 12/03/2004 · prix d'achat 12,50 €", "Intention d'acquisition"], Values(page, "merge-acquisition"));
        Assert.Empty(page.FindAll(".merge-links-withdrawn"));

        await page.ChangeAsync("input[name=merge-acquisition][value=First]", true);
        Assert.Equal(
            "L'intention d'acquisition de l'édition « Dupuis [1987] » sera retirée : l'édition sera possédée.",
            page.Find(".merge-links-withdrawn").TextContent.Trim());

        await page.ChangeAsync("input[name=merge-acquisition][value=Second]", true);
        Assert.Empty(page.FindAll(".merge-links-withdrawn"));
        Assert.Contains("1 intention d'acquisition", page.Find(".merge-links").TextContent);
    }

    [Fact]
    public async Task Merge_ConfirmsTheVisualsDiscardedApart_SendsTheEditionOfEachData_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(new MergeResult(First));
        var page = Open();
        foreach (var (row, side) in new[] { ("album", "Second"), ("publisher", "First"), ("acquisition", "First") })
            await page.ChangeAsync($"input[name=merge-{row}][value={side}]", true);
        // The cover of the second edition discarded: the side without it is retained.
        await page.ChangeAsync($"input[name=merge-visual-{SecondCover}][value=First]", true);
        Assert.Equal("Tout est choisi", page.Find(".save-bar-state-long").TextContent);

        await page.SubmitAsync("form");
        var confirmation = page.Find("dialog");
        Assert.Contains("Les deux fiches n'en feront plus qu'une : « Dupuis (Spirou) [1987] ».", confirmation.TextContent);
        Assert.Equal(["1 visuel : Couverture, de « Dupuis [1987] »"], page.FindAll(".merge-removals li").Select(line => line.TextContent.Trim()));
        await page.ClickAsync("dialog button[data-variant=delete]");

        var (_, path, body) = Assert.Single(_api.Written);
        Assert.Equal("/admin/editions/merge", path);
        var request = body.Deserialize<EditionMergeRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal((First, 11u, Second, 12u), (request.First.Id, request.First.Version, request.Second.Id, request.Second.Version));
        Assert.Equal((MergeSide.Second, MergeSide.First, MergeSide.First), (request.Album, request.Publisher, request.Acquisition));
        Assert.Equal((MergeSide.First, MergeSide.Second), (request.Category, request.PersonalReference));
        Assert.Equal(
            [new MergeElement(FirstCover, MergeSide.First), new MergeElement(SecondCover, MergeSide.First), new MergeElement(SecondPlate, MergeSide.Second)],
            request.Visuals);
        Assert.EndsWith(AppPaths.AdminEditions, Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.edition-merge.merged", _audience.Functions);
    }

    [Fact]
    public async Task Merge_OfAnEditionModifiedMeanwhile_SaysSoAndOffersToReload()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        var page = Open();
        foreach (var row in new[] { "album", "publisher", "acquisition" })
            await page.ChangeAsync($"input[name=merge-{row}][value=First]", true);

        await page.SubmitAsync("form");
        await page.ClickAsync("dialog button[data-variant=delete]");

        Assert.Contains("L'une des deux éditions a été modifiée depuis l'ouverture de la fusion.", page.Markup);
        Assert.Contains(page.FindAll("button"), button => button.TextContent.Trim() == "Recharger la fiche");
    }

    private IRenderedComponent<AdminMergeEditions> Open()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminMergeEditions}?edition={First}&other={Second}");
        return Render<AdminMergeEditions>(parameters => parameters.AddCascadingValue(_request.Context));
    }

    private static string[] Values(IRenderedComponent<AdminMergeEditions> page, string row) =>
        [.. page.FindAll($"input[name={row}]").Select(radio => radio.ParentElement!.QuerySelector(".merge-choice-value")!.TextContent.Trim())];

    private static string? Checked(IRenderedComponent<AdminMergeEditions> page, string row) =>
        page.FindAll($"input[name={row}]").SingleOrDefault(radio => radio.HasAttribute("checked"))?.GetAttribute("value");
}
