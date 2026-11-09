using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The page of the editions and purchase intents of an album (visuel/editions-album/contenu.md): its heading, the album in brief,
/// its editions and its intents with their actions, the button that adds an intent according to the state of the album
/// (fonctionnel/regles-metier/intention-d-achat.md), its dialogs and its errors, told apart by role, link and text, never by
/// appearance.
/// </summary>
public sealed class AdminAlbumEditionsTests : BunitContext, IDisposable
{
    private static readonly Guid AlbumId = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly AlbumCover Cover = new(Guid.Parse("01900000-0000-7000-8000-0000000000c1"), TestCovers.Palette);

    private static readonly AlbumSummary Castafiore = new(
        AlbumId, "Les Bijoux de la Castafiore", Guid.NewGuid(), "Tintin", AlbumType.Regular, false, 21, null, null, true);

    private static readonly AlbumEditionItem Original = new(
        new EditionSummary(Guid.Parse("01900000-0000-7000-8000-0000000000e1"), Guid.NewGuid(), "Casterman", null, null, 1963, null),
        Cover, EditionCategory.FirstEdition, BindingType.Hardcover, 62, AcquisitionMode.Purchase, 1987, 2);

    private static readonly AlbumEditionItem Reissue = new(
        new EditionSummary(Guid.Parse("01900000-0000-7000-8000-0000000000e2"), Guid.NewGuid(), "Casterman", null, null, 2007, "978-2-203-00120-8"),
        null, null, BindingType.Hardcover, 62, AcquisitionMode.Gift, null, 0);

    private static readonly AlbumPurchaseIntentItem OnEdition = new(
        Guid.Parse("01900000-0000-7000-8000-000000000001"),
        new EditionSummary(Guid.Parse("01900000-0000-7000-8000-0000000000e3"), Guid.NewGuid(), "Casterman", Guid.NewGuid(), "Fac-similé", 2013, null),
        EditionCategory.SpecialEdition, BindingType.Hardcover, false, new DateTimeOffset(2026, 10, 8, 8, 0, 0, TimeSpan.Zero));

    private static readonly AlbumPurchaseIntentItem OnAlbum = new(
        Guid.Parse("01900000-0000-7000-8000-000000000002"), null, null, null, true, new DateTimeOffset(2026, 10, 8, 8, 0, 0, TimeSpan.Zero));

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminAlbumEditionsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddSingleton(TimeProvider.System);
        Answer(AlbumState.InCollection, [Original, Reissue], [OnEdition]);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    private enum AlbumState
    {
        InCollection,
        Intended,
        Outside,
    }

    [Fact]
    public void Heading_TheFilDAriane_TheTitleAndTheLabelOfTheAlbum_WithoutLinks()
    {
        var page = Open("");

        Assert.Equal(
            [
                ("Administration", AppPaths.AdminHome), ("Éditions et intentions d'achat", AppPaths.AdminPurchaseIntents),
                ("Les Bijoux de la Castafiore (Tintin - T. 21)", null),
            ],
            page.FindAll(".breadcrumb li").Select(item => (Text(item), item.QuerySelector("a")?.GetAttribute("href"))));
        Assert.Equal("Éditions et intentions d'achat", Text(page.Find("h1")));
        Assert.Equal("Les Bijoux de la Castafiore (Tintin - T. 21)", Text(page.Find(".album-editions-subject")));
        Assert.Empty(page.Find(".album-editions-heading").QuerySelectorAll("a.button"));
    }

    [Fact]
    // The album is modified, or seen in the consultation, from its block (decision of the user at the functional validation).
    public void Album_ItsBlockLeadsToItsEntryForm_AndToItsRecord()
    {
        var links = Block(Open(""), "L'album").QuerySelectorAll(".album-brief-links a");

        Assert.Equal(
            [("Voir dans la consultation", null, AppPaths.Album(AlbumId)), ("Modifier", "Modifier l'album", AppPaths.AdminEditAlbum(AlbumId))],
            links.Select(link => (Text(link), link.GetAttribute("aria-label"), link.GetAttribute("href"))));
    }

    [Fact]
    public void Album_InBrief_ItsCoverLabelAuthorsDateAndMembership()
    {
        var album = Open("").Find(".album-brief");

        Assert.Equal(VisualMediaRoutes.Display(Cover.VisualId), album.QuerySelector(".cover")!.GetAttribute("src"));
        Assert.Equal("Les Bijoux de la Castafiore (Tintin - T. 21)", Text(album.QuerySelector(".album-brief-label")!));
        Assert.Equal("Hergé", Text(album.QuerySelector(".album-brief-authors")!));
        Assert.Equal("Première publication : avril 1963", Text(album.QuerySelector(".album-brief-date")!));
        Assert.Equal("Dans la collection", Text(album.QuerySelector(".tag")!));
        Assert.Equal(
            "L'album lui-même se modifie sur sa fiche de saisie ; cette page ne réunit que ses éditions et ses intentions d'achat.",
            Text(Block(Open(""), "L'album").QuerySelector(".album-editions-help")!));
    }

    [Fact]
    public void Editions_TheirCountTheNewEdition_AndEachRowWithItsDetail()
    {
        var editions = Block(Open(""), "Les éditions");

        Assert.Equal("2 éditions", Text(editions.QuerySelector(".album-editions-count")!));
        var create = editions.QuerySelector(".album-editions-toolbar a.button")!;
        Assert.Equal(("Nouvelle édition", AppPaths.AdminNewAlbumEdition(AlbumId)), (Text(create), create.GetAttribute("href")));
        Assert.Equal(
            [
                ("Casterman [1963]", "Édition originale · Cartonnée · 62 pages", "Achat, 1987 · 2 visuels"),
                ("Casterman [2007] - ISBN 978-2-203-00120-8", "Cartonnée · 62 pages", "Offerte · Aucun visuel"),
            ],
            editions.QuerySelectorAll(".edition-row").Select(row => (
                Text(row.QuerySelector(".edition-label")!), Text(row.QuerySelector(".edition-sub")!), Text(row.QuerySelector(".edition-detail")!))));
        Assert.Equal("Chaque édition, avec ses visuels, se saisit sur sa propre fiche.", Text(editions.QuerySelector(".album-editions-help")!));
    }

    [Fact]
    public void Editions_EachIsModifiedOnItsForm_SeenInTheConsultation_OrDeleted()
    {
        var row = Block(Open(""), "Les éditions").QuerySelectorAll(".edition-row")[0];
        var id = Original.Edition.Id;

        var edit = row.QuerySelector(".edition-actions a.button")!;
        Assert.Equal(
            ("Modifier", "Modifier Casterman [1963]", AppPaths.AdminEditEdition(AlbumId, id)),
            (Text(edit), edit.GetAttribute("aria-label"), edit.GetAttribute("href")));
        Assert.Equal(AppPaths.AdminEditEdition(AlbumId, id), row.QuerySelector(".edition-label")!.GetAttribute("href"));
        Assert.Equal(
            [
                ("Voir dans la consultation", AppPaths.Album(AlbumId)),
                ("Supprimer l'édition…", $"{AppPaths.AdminAlbumEditions(AlbumId)}?delete-edition={id}"),
            ],
            Items(row));
    }

    [Fact]
    public void Editions_None_SaysIt()
    {
        Answer(AlbumState.Outside, [], []);

        var editions = Block(Open(""), "Les éditions");

        Assert.Equal("Aucune édition", Text(editions.QuerySelector(".album-editions-count")!));
        Assert.Equal("Aucune édition dans la collection.", Text(editions.QuerySelector(".album-editions-none")!));
        Assert.Empty(editions.QuerySelectorAll(".edition-row"));
    }

    [Fact]
    // An album targeted by an intent on itself has no edition: its first one is added by confirming the purchase.
    public void Editions_OfAnAlbumItselfTargeted_HaveNoCreation_ButTheHelpOfThePurchase()
    {
        Answer(AlbumState.Intended, [], [OnAlbum]);

        var editions = Block(Open(""), "Les éditions");

        Assert.Empty(editions.QuerySelectorAll("a.button"));
        Assert.Equal(
            "L'album est visé par une intention d'achat : sa première édition s'ajoute en confirmant l'achat.",
            Text(editions.QuerySelector(".album-editions-help")!));
    }

    [Fact]
    public void Intents_OnAnEdition_TheEditionItsDetailAndItsDate_AndTheirActions()
    {
        var intents = Block(Open(""), "Les intentions d'achat");

        Assert.Equal("1 intention d'achat", Text(intents.QuerySelector(".album-editions-count")!));
        var row = Assert.Single(intents.QuerySelectorAll(".intent-row"));
        Assert.Equal(
            ("Édition", "Casterman (Fac-similé) [2013]", "Édition spéciale · Cartonnée", "Posée le 08/10/2026"),
            (Text(row.QuerySelector(".tag")!), Text(row.QuerySelector(".intent-target-detail")!), Text(row.QuerySelector(".intent-sub")!),
                Text(row.QuerySelector(".intent-created")!)));
        var confirm = row.QuerySelector(".intent-actions a.button")!;
        Assert.Equal(
            ("Confirmer l'achat", "Confirmer l'achat : Casterman (Fac-similé) [2013]", AppPaths.AdminConfirmPurchase(AlbumId, OnEdition.Id)),
            (Text(confirm), Normalized(confirm.GetAttribute("aria-label")!), confirm.GetAttribute("href")));
        // The album has other editions: the intent does not turn into one on the whole album.
        Assert.Equal(
            [
                ("Modifier l'intention", AppPaths.AdminEditEdition(AlbumId, OnEdition.Edition!.Id)),
                ("Retirer l'intention…", $"{AppPaths.AdminAlbumEditions(AlbumId)}?remove-intent={OnEdition.Id}"),
            ],
            Items(row));
        Assert.Equal(
            "Une intention ne vise l'album entier que tant qu'il n'a aucune édition, possédée ou visée ; ensuite, elle vise une édition précise, saisie sans exemplaire ni valeur.",
            Text(intents.QuerySelector(".album-editions-help")!));
    }

    [Fact]
    public void Intents_OnTheAlbum_AnyEditionDoing_AndConvertibleToAnEdition()
    {
        Answer(AlbumState.Intended, [], [OnAlbum]);

        var row = Assert.Single(Block(Open(""), "Les intentions d'achat").QuerySelectorAll(".intent-row"));

        Assert.Equal(("Album", "toute édition"), (Text(row.QuerySelector(".tag")!), Text(row.QuerySelector(".intent-target-detail")!)));
        Assert.Null(row.QuerySelector(".intent-sub"));
        Assert.Equal(
            [
                ("Viser plutôt une édition…", AppPaths.AdminConvertPurchaseIntent(AlbumId, OnAlbum.Id)),
                ("Retirer l'intention…", $"{AppPaths.AdminAlbumEditions(AlbumId)}?remove-intent={OnAlbum.Id}"),
            ],
            Items(row));
    }

    [Fact]
    public void Intents_OnTheOnlyEditionOfAnAlbum_ConvertibleToTheAlbum()
    {
        Answer(AlbumState.Intended, [], [OnEdition with { IsConvertible = true }]);

        var row = Assert.Single(Block(Open(""), "Les intentions d'achat").QuerySelectorAll(".intent-row"));

        Assert.Contains(("Viser plutôt l'album…", AppPaths.AdminConvertPurchaseIntent(AlbumId, OnEdition.Id)), Items(row));
    }

    [Fact]
    // Without edition, owned or targeted, the album itself may be targeted, or a new edition: a choice of two.
    public void NewIntent_OfAnAlbumWithoutEditionNorIntent_ChoosesTheAlbumOrANewEdition()
    {
        Answer(AlbumState.Outside, [], []);

        var intents = Block(Open(""), "Les intentions d'achat");

        Assert.Equal("Aucune intention d'achat", Text(intents.QuerySelector(".album-editions-count")!));
        Assert.Equal("Aucune intention d'achat.", Text(intents.QuerySelector(".album-editions-none")!));
        Assert.Equal("Nouvelle intention d'achat", Text(intents.QuerySelector(".actions-menu button.button")!));
        Assert.Equal(
            [
                ("Sur l'album… Toute édition de l'album fera l'affaire.", null),
                ("Sur une nouvelle édition… Une édition précise, saisie sans exemplaire ni valeur.", AppPaths.AdminNewPurchaseIntent(AlbumId)),
            ],
            Items(intents));
        // Nothing to enter for an intent on the whole album: the choice sets it at once, under the version of the album.
        var form = intents.QuerySelector(".actions-menu-panel form[method='post']")!;
        Assert.Equal(("submit", "42"), (form.QuerySelector(".menu-item")!.GetAttribute("type"), form.QuerySelector("input[name='Version']")!.GetAttribute("value")));
    }

    [Fact]
    // Once an edition is known, owned or targeted, only a precise edition may be targeted: a plain button.
    public void NewIntent_OfAnAlbumWithAnEdition_TargetsAnEdition()
    {
        var intents = Block(Open(""), "Les intentions d'achat");

        var create = intents.QuerySelector(".album-editions-toolbar a.button")!;
        Assert.Equal(("Nouvelle intention sur une édition…", AppPaths.AdminNewPurchaseIntent(AlbumId)), (Text(create), create.GetAttribute("href")));
    }

    [Fact]
    public void NewIntent_OfAnAlbumItselfTargeted_IsNotOffered()
    {
        Answer(AlbumState.Intended, [], [OnAlbum]);

        var intents = Block(Open(""), "Les intentions d'achat");

        Assert.Empty(intents.QuerySelectorAll(".album-editions-toolbar .button"));
    }

    [Fact]
    public void Dates_OfTheAlbum_InItsBlock_IntroducedByWhatTheyConcern()
    {
        Assert.Equal(
            "Album créé le 04/03/2026 11:00, modifié le 08/11/2026 21:08.", Text(Block(Open(""), "L'album").QuerySelector(".album-editions-dates")!));
        // The last of the block, nothing after them.
        Assert.EndsWith("modifié le 08/11/2026 21:08.", Text(Block(Open(""), "L'album")), StringComparison.Ordinal);
    }

    [Fact]
    public void Deletion_OfAnEdition_AnnouncesWhatGoesWithIt_AndTheAlbumLeavingTheCollection()
    {
        var id = Original.Edition.Id;
        _api.Answers[$"/admin/albums/{AlbumId}/editions/{id}/deletion-impact"] = new DeletionImpact(
            [], [], [new ImpactCount(EntityKind.EditionVisual, 2)], [new ImpactCount(EntityKind.Album, 1)], "f1");

        var dialog = Open($"delete-edition={id}").Find("dialog");

        Assert.Equal(("Supprimer cette édition ?", "Casterman [1963]"),
            (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Equal(
            ["Suppression aussi de 2 visuels", "Sortie de la collection de 1 album"],
            dialog.QuerySelectorAll(".deletion-impact li").Select(Text));
        var form = dialog.QuerySelector("form[method='post']")!;
        Assert.Equal(
            [("Version", "42"), ("Fingerprint", "f1")],
            form.QuerySelectorAll("input[type='hidden']").Where(input => input.GetAttribute("name") is "Version" or "Fingerprint")
                .Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
        Assert.Equal(AppPaths.AdminAlbumEditions(AlbumId), dialog.QuerySelector("[data-dialog-close]")!.GetAttribute("href"));
    }

    [Fact]
    public void Removal_OfAnIntentOnAnEdition_AnnouncesTheEditionRemovedWithIt()
    {
        _api.Answers[$"/admin/albums/{AlbumId}/purchase-intents/{OnEdition.Id}/deletion-impact"] = new DeletionImpact(
            [], [], [new ImpactCount(EntityKind.Edition, 1)], [], "f2");

        var dialog = Open($"remove-intent={OnEdition.Id}").Find("dialog");

        Assert.Equal(("Retirer cette intention d'achat ?", "Les Bijoux de la Castafiore (Tintin - T. 21) · Casterman (Fac-similé) [2013]"),
            (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Equal(["Suppression aussi de 1 édition"], dialog.QuerySelectorAll(".deletion-impact li").Select(Text));
    }

    [Fact]
    public void Removal_OfAnIntentOnTheAlbum_TakesNothingElse()
    {
        Answer(AlbumState.Intended, [], [OnAlbum]);
        _api.Answers[$"/admin/albums/{AlbumId}/purchase-intents/{OnAlbum.Id}/deletion-impact"] = new DeletionImpact([], [], [], [], "f3");

        var dialog = Open($"remove-intent={OnAlbum.Id}").Find("dialog");

        Assert.Equal("Les Bijoux de la Castafiore (Tintin - T. 21)", Text(dialog.QuerySelector(".dialog-subject")!));
        Assert.Equal("Aucune autre fiche n'est concernée.", Text(dialog.QuerySelector(".deletion-impact p")!));
    }

    [Fact]
    // The dialogs open on what the page offers: an address written by hand opens none.
    public void Dialogs_OnWhatThePageDoesNotOffer_OpenNone()
    {
        Assert.Empty(Open($"delete-edition={Guid.NewGuid()}").FindAll("dialog"));
        Assert.Empty(Open($"remove-intent={Guid.NewGuid()}").FindAll("dialog"));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfThePage()
    {
        _api.Failures[ApiPath] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".album-brief"));
    }

    [Fact]
    public void Audience_TheScreenWithItsAlbum()
    {
        Open("", recorded: true);

        Assert.Equal(["admin.album-editions"], _audience.Functions);
    }

    [Fact]
    public void Audience_TheActionOfADialog_ThenTheScreen()
    {
        _api.Answers[$"/admin/albums/{AlbumId}/editions/{Original.Edition.Id}/deletion-impact"] = new DeletionImpact([], [], [], [], "f1");

        Open($"delete-edition={Original.Edition.Id}", recorded: true);

        Assert.Equal(["admin.album-editions.edition-deletion", "admin.album-editions"], _audience.Functions);
    }

    private static string ApiPath => $"/admin/albums/{AlbumId}/editions-and-purchase-intents";

    private IRenderedComponent<AdminAlbumEditions> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminAlbumEditions(AlbumId)}?{criteria}");
        return Render<AdminAlbumEditions>(parameters =>
        {
            parameters.Add(page => page.Id, AlbumId);
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(AlbumState state, IReadOnlyList<AlbumEditionItem> editions, IReadOnlyList<AlbumPurchaseIntentItem> intents) =>
        _api.Answers[ApiPath] = new AlbumEditionsPage(
            new AlbumAdminListItem(
                Castafiore with { IsInCollection = state == AlbumState.InCollection },
                editions.Count > 0 ? Cover : null,
                [new AuthorListItem(Guid.NewGuid(), null, null, "Hergé")],
                1963, 4, editions.Count, intents.Count,
                state switch
                {
                    AlbumState.InCollection => AlbumCollectionState.InCollection,
                    AlbumState.Intended => AlbumCollectionState.PurchaseIntent,
                    _ => AlbumCollectionState.OutsideCollection,
                },
                new DateTimeOffset(2026, 11, 8, 20, 8, 0, TimeSpan.Zero), 42),
            new DateTimeOffset(2026, 3, 4, 10, 0, 0, TimeSpan.Zero),
            editions,
            intents);

    // A block of the page, by its title.
    private static IElement Block(IRenderedComponent<AdminAlbumEditions> page, string title) =>
        page.FindAll("section.block").Single(block => Text(block.QuerySelector(".block-title")!) == title);

    private static List<(string Text, string? Href)> Items(IElement element) =>
        [.. element.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href")))];

    private static string Text(IElement element) => Normalized(element.TextContent);

    // The non-breaking spaces of a label as plain ones.
    private static string Normalized(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
