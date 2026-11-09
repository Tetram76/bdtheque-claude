using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The list of the purchase intents of the administration (visuel/listes/liste-par-liste.md): its heading without creation, its
/// rows and what each intent targets, its actions, its removal dialog and its errors, told apart by role, link and text, never
/// by appearance. What it shares with the list of the albums (search, page beyond the last) is pinned by AdminAlbumsTests.
/// </summary>
public sealed class AdminPurchaseIntentsTests : BunitContext, IDisposable
{
    private static readonly AlbumCover Cover = new(Guid.Parse("01900000-0000-7000-8000-0000000000c1"), TestCovers.Palette);

    private static readonly AlbumSummary Blacksad = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000a1"), null, Guid.NewGuid(), "Blacksad", AlbumType.Omnibus, false, null, 1, 5, false);

    private static readonly AlbumSummary Castafiore = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000a2"), "Les Bijoux de la Castafiore", Guid.NewGuid(), "Tintin", AlbumType.Regular, false, 21,
        null, null, true);

    private static readonly AlbumSummary Longway = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000a3"), null, Guid.NewGuid(), "Buddy Longway", AlbumType.Regular, false, 2, null, null, false);

    // On the whole album, which has no edition.
    private static readonly PurchaseIntentAdminListItem OnAlbum = new(
        Guid.Parse("01900000-0000-7000-8000-000000000001"), Blacksad, null, null, true,
        new DateTimeOffset(2026, 10, 8, 8, 0, 0, TimeSpan.Zero), 41);

    // On an edition of an album of the collection, which has another one.
    private static readonly PurchaseIntentAdminListItem OnEdition = new(
        Guid.Parse("01900000-0000-7000-8000-000000000002"), Castafiore,
        new EditionSummary(Guid.Parse("01900000-0000-7000-8000-0000000000e2"), Guid.NewGuid(), "Casterman", Guid.NewGuid(), "Fac-similé", 2013, null),
        Cover, false, new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.Zero), 42);

    // On the only edition of its album.
    private static readonly PurchaseIntentAdminListItem OnOnlyEdition = new(
        Guid.Parse("01900000-0000-7000-8000-000000000003"), Longway,
        new EditionSummary(Guid.Parse("01900000-0000-7000-8000-0000000000e3"), Guid.NewGuid(), "Dupuis", null, null, 1974, null),
        null, true, new DateTimeOffset(2026, 10, 9, 8, 0, 0, TimeSpan.Zero), 43);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminPurchaseIntentsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddSingleton(TimeProvider.System);
        Answer([OnAlbum, OnEdition, OnOnlyEdition], totalCount: 18);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleTheNumberOfIntents_AndWhereAnIntentIsSetInsteadOfACreation()
    {
        var heading = Open("").Find(".list-heading");

        Assert.Equal(["Administration", "Intentions d'achat"], heading.QuerySelectorAll(".breadcrumb li").Select(Text));
        Assert.Equal(("Intentions d'achat", "18"), (heading.QuerySelector("h1")!.FirstChild!.TextContent.Trim(), Text(heading.QuerySelector(".list-heading-count")!)));
        Assert.Empty(heading.QuerySelectorAll("a.button"));
        Assert.Equal(
            "Une intention se pose depuis la page des éditions et intentions d'achat de son album.",
            Text(heading.QuerySelector(".list-heading-note")!));
    }

    [Fact]
    public void Search_OnTheTitleOfTheAlbumOrOfItsSeries_WithoutFilters()
    {
        var form = Open("").Find("form.list-search");

        Assert.Equal("Titre de l'album ou de sa série…", form.QuerySelector("input[type='search']")!.GetAttribute("placeholder"));
        Assert.Empty(form.QuerySelectorAll("button[aria-controls]"));
    }

    [Fact]
    // What an intent targets is told apart at first glance: the whole album, any edition doing, or one edition, named.
    public void Rows_TheAlbumWhatTheIntentTargetsItsMembershipAndItsDate()
    {
        var rows = Open("").FindAll(".intent-row:not(.intent-row-head)").Select(row => (
            Text(row.QuerySelector(".intent-label")!),
            Text(row.QuerySelector(".intent-target .tag")!),
            Text(row.QuerySelector(".intent-target-detail")!),
            Text(row.QuerySelector(".intent-collection")!),
            Text(row.QuerySelector(".intent-created")!)));

        Assert.Equal(
            [
                ("Blacksad - INT. [1 à 5]", "Album", "toute édition", "Hors collection", "Posée le 08/10/2026"),
                ("Les Bijoux de la Castafiore (Tintin - T. 21)", "Édition", "Casterman (Fac-similé) [2013]", "Dans la collection", "Posée le 08/10/2026"),
                ("Buddy Longway - T. 2", "Édition", "Dupuis [1974]", "Hors collection", "Posée le 09/10/2026"),
            ],
            rows);
    }

    [Fact]
    public void Rows_TheCoverOfTheAlbum_OrTheGenericVisual()
    {
        var covers = Open("").FindAll(".intent-row:not(.intent-row-head) .intent-cover .cover");

        Assert.Equal([null, VisualMediaRoutes.Display(Cover.VisualId), null], covers.Select(cover => cover.GetAttribute("src")));
    }

    [Fact]
    public void Rows_ConfirmThePurchase_AndOfferTheEditionsOfTheAlbumTheConversionAndTheRemoval()
    {
        var rows = Open("q=Bla").FindAll(".intent-row:not(.intent-row-head)");
        var id = OnAlbum.Id;

        Assert.Equal(AppPaths.AdminAlbumEditions(Blacksad.Id), rows[0].QuerySelector(".intent-label")!.GetAttribute("href"));
        var confirm = rows[0].QuerySelector(".intent-actions a.button")!;
        Assert.Equal(
            ("Confirmer l'achat", "Confirmer l'achat de Blacksad - INT. [1 à 5]", AppPaths.AdminConfirmPurchase(Blacksad.Id, id)),
            (Text(confirm), Normalized(confirm.GetAttribute("aria-label")!), confirm.GetAttribute("href")));
        Assert.Equal(
            [
                ("Éditions et intentions d'achat", AppPaths.AdminAlbumEditions(Blacksad.Id)),
                ("Viser plutôt une édition…", AppPaths.AdminConvertPurchaseIntent(Blacksad.Id, id)),
                ("Retirer l'intention…", $"{AppPaths.AdminPurchaseIntents}?q=Bla&delete={id}"),
            ],
            Items(rows[0]));
        // An intent on an edition turns into one on the album only when that edition is the only one of its album.
        Assert.DoesNotContain(Items(rows[1]), item => item.Text.StartsWith("Viser", StringComparison.Ordinal));
        Assert.Contains(("Viser plutôt l'album…", AppPaths.AdminConvertPurchaseIntent(Longway.Id, OnOnlyEdition.Id)), Items(rows[2]));
    }

    [Theory]
    [InlineData("", "18 intentions")]
    [InlineData("q=Bla", "18 intentions sur 18")]
    public void Summary_TheCountOutOfAllTheIntents_AndTheOrder(string criteria, string count)
    {
        var summary = Open(criteria).Find(".list-summary");

        Assert.Equal(count, Text(summary.Children[0]));
        Assert.Equal("Dans l'ordre des albums", Text(summary.Children[1]));
    }

    [Fact]
    public void Empty_ASearchWithoutResult_SaysItAndOffersToClearIt()
    {
        Answer([], totalCount: 0);

        var empty = Open("q=Zorglub").Find(".empty-list");

        Assert.Equal("Aucune intention d'achat ne correspond à « Zorglub ».", Text(empty.QuerySelector("strong")!));
        Assert.Equal([("Effacer la recherche", AppPaths.AdminPurchaseIntents)], empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_WithoutAnyIntent_SaysWhereAnIntentIsSet()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Equal("Aucune intention d'achat.", Text(empty.QuerySelector("strong")!));
        Assert.Empty(empty.QuerySelectorAll("a"));
    }

    [Fact]
    public void Removal_ADialogThatAnnouncesTheEditionTargetedRemovedWithTheIntent_AndConfirmsWithTheVersionOfTheAlbum()
    {
        var id = OnOnlyEdition.Id;
        _api.Answers[$"/admin/albums/{Longway.Id}/purchase-intents/{id}/deletion-impact"] = new DeletionImpact(
            [], [], [new ImpactCount(EntityKind.Edition, 1)], [], "f1");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal(("Retirer cette intention d'achat ?", "Buddy Longway - T. 2 · Dupuis [1974]"),
            (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        // An edition not owned goes with its intent (fonctionnel/regles-metier/suppression-des-entites.md).
        Assert.Equal(["Suppression aussi de 1 édition"], dialog.QuerySelectorAll(".deletion-impact li").Select(Text));
        var form = dialog.QuerySelector("form[method='post']")!;
        Assert.Equal(
            [("Version", "43"), ("Fingerprint", "f1")],
            form.QuerySelectorAll("input[type='hidden']").Where(input => input.GetAttribute("name") is "Version" or "Fingerprint")
                .Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
    }

    [Fact]
    public void Removal_OfAnIntentOnTheAlbum_TakesNothingElse()
    {
        _api.Answers[$"/admin/albums/{Blacksad.Id}/purchase-intents/{OnAlbum.Id}/deletion-impact"] = new DeletionImpact([], [], [], [], "f2");

        var dialog = Open($"delete={OnAlbum.Id}").Find("dialog");

        Assert.Equal("Blacksad - INT. [1 à 5]", Text(dialog.QuerySelector(".dialog-subject")!));
        Assert.Equal("Aucune autre fiche n'est concernée.", Text(dialog.QuerySelector(".deletion-impact p")!));
    }

    [Fact]
    public void Removal_RefusedWhileASavedSearchDesignatesTheIntent_SaysWhatToDo()
    {
        _api.Answers[$"/admin/albums/{Castafiore.Id}/purchase-intents/{OnEdition.Id}/deletion-impact"] = new DeletionImpact(
            [new ImpactCount(EntityKind.SavedSearch, 1)], [], [], [], "f3");

        var dialog = Open($"delete={OnEdition.Id}").Find("dialog");

        Assert.Equal("Suppression impossible", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Contains("Retirez d'abord l'intention des critères de ces recherches enregistrées, puis recommencez.", Text(dialog));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/purchase-intents"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".intent-row"));
    }

    [Fact]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen()
    {
        Open("q=Bla", recorded: true);

        // The action of the list first, which displays the screen.
        Assert.Equal(["admin.purchase-intents.search", "admin.purchase-intents"], _audience.Functions);
    }

    private IRenderedComponent<AdminPurchaseIntents> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminPurchaseIntents}?{criteria}");
        return Render<AdminPurchaseIntents>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<PurchaseIntentAdminListItem> items, int totalCount) =>
        _api.Answers["/admin/purchase-intents"] = new Page<PurchaseIntentAdminListItem>(items, 1, AdminListQuery.PageSize, totalCount);

    private static List<(string Text, string? Href)> Items(IElement row) =>
        [.. row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href")))];

    private static string Text(IElement element) => Normalized(element.TextContent);

    // The non-breaking spaces of a label as plain ones.
    private static string Normalized(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
