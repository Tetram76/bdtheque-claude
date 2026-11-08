using System.Globalization;
using System.Net;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using AngleSharp.Dom;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The home page of the administration (visuel/administration/contenu.md), its blocks told apart by their title, their
/// content by role, link and text, never by appearance.
/// </summary>
public sealed class AdminHomeTests : BunitContext, IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAdminApi _api = new(new FakeAudienceApi());

    public AdminHomeTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api.AddServices(Services, new TestRequest(authenticated: true));
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Entry_SearchesTheSourcesFromAnIsbn_OrCreatesARecordDirectly()
    {
        var page = Render<AdminHome>();

        var entry = BlockOf(page, "Saisir à partir d'un ISBN");
        var form = entry.QuerySelector("form")!;
        Assert.Equal(("get", AppPaths.AdminIsbnLookup), (form.GetAttribute("method"), form.GetAttribute("action")));
        Assert.Equal("ISBN de l'édition", form.QuerySelector("label[for='admin-isbn']")?.TextContent.Trim());
        Assert.Equal("isbn", form.QuerySelector("#admin-isbn")?.GetAttribute("name"));
        Assert.Equal("Chercher dans les sources", form.QuerySelector("button[type='submit']")?.TextContent.Trim());
        Assert.Equal(
            [("Album", AppPaths.AdminNewAlbum), ("Série", AppPaths.AdminNewSeries), ("Auteur", AppPaths.AdminNewAuthor), ("Éditeur", AppPaths.AdminNewPublisher)],
            entry.QuerySelectorAll(".admin-create a").Select(link => (link.TextContent.Trim(), link.GetAttribute("href"))));
    }

    [Fact]
    public void Records_ACardPerType_WithItsNumberItsDetailAndItsLinks()
    {
        var page = Render<AdminHome>();

        var cards = BlockOf(page, "Les fiches").QuerySelectorAll("li").Select(card => (
            card.QuerySelector(".admin-record-name")!.TextContent.Trim(),
            Text(card.QuerySelector(".admin-record-count")!),
            Text(card.QuerySelector(".admin-record-detail")!),
            string.Join(" | ", card.QuerySelectorAll(".admin-record-links a").Select(link => link.TextContent.Trim()))));

        Assert.Equal(
            [
                ("Albums", "2 847", "dont 214 hors de la collection", "Gérer la liste | + Nouvel album"),
                ("Éditions", "2 913", "saisies depuis leur album, avec leurs visuels", "Gérer la liste"),
                ("Séries", "412", "dont 28 sans statut", "Gérer la liste | + Nouvelle série"),
                ("Intentions d'achat", "18", "posées depuis leur album", "Gérer la liste"),
                ("Auteurs", "1 106", "scénaristes, dessinateurs, coloristes…", "Gérer la liste | + Nouvel auteur"),
                ("Éditeurs", "96", "collections éditeur : 214", "Gérer la liste | + Nouvel éditeur"),
                ("Genres", "24", "référentiel", "Gérer la liste | + Nouveau genre"),
                ("Univers", "37", "dont 9 sous-univers", "Gérer la liste | + Nouvel univers"),
            ],
            cards);
        // The lists of the albums (#200), of the series (#301), of the authors (#302) and of the publishers (#303) are implemented; the other pages
        // are still to come.
        var links = BlockOf(page, "Les fiches").QuerySelectorAll("a").Select(link => (link.TextContent.Trim(), link.GetAttribute("href"))).ToList();
        IEnumerable<(string, string?)> implemented =
        [
            ("Albums", AppPaths.AdminAlbums), ("Gérer la liste", AppPaths.AdminAlbums),
            ("Séries", AppPaths.AdminSeries), ("Gérer la liste", AppPaths.AdminSeries),
            ("Auteurs", AppPaths.AdminAuthors), ("Gérer la liste", AppPaths.AdminAuthors),
            ("Éditeurs", AppPaths.AdminPublishers), ("Gérer la liste", AppPaths.AdminPublishers),
        ];
        Assert.Equal(implemented, links.Where(link => link.Item2 != AppPaths.NotYetImplemented));
    }

    [Theory]
    [InlineData(7, true)]
    [InlineData(0, false)]
    public void Checks_TheIsbnToCorrect_InTheAlertColorOnlyWhenThereAreAny(int count, bool alert)
    {
        _api.Answers["/admin/checks"] = new AdminChecks(count);

        var page = Render<AdminHome>();

        var check = BlockOf(page, "À vérifier").QuerySelector("a")!;
        Assert.Equal(AppPaths.AdminInvalidIsbns, check.GetAttribute("href"));
        Assert.Contains("ISBN au chiffre de contrôle incohérent", check.TextContent);
        var number = check.QuerySelector(".admin-check-count")!;
        Assert.Equal(count.ToString(CultureInfo.InvariantCulture), number.TextContent.Trim());
        Assert.Equal(alert, number.HasAttribute("data-alert"));
    }

    [Fact]
    public void LatestChanges_TheRecordsWithTheirTypeTheirOperationAndTheirDate_EachToModify()
    {
        var album = Guid.CreateVersion7();
        _api.Answers["/admin/change-log/latest-records"] = new[]
        {
            new ChangeLogEntryItem(Guid.CreateVersion7(), new DateTimeOffset(2026, 11, 6, 9, 42, 0, TimeSpan.Zero), RecordType.Album, album,
                new AlbumLabel("L'Iris blanc", "Astérix", AlbumType.Regular, false, 40, null, null), ChangeLogOperation.Modification, []),
            new ChangeLogEntryItem(Guid.CreateVersion7(), new DateTimeOffset(2026, 11, 5, 20, 8, 0, TimeSpan.Zero), RecordType.Genre,
                Guid.CreateVersion7(), new NameLabel("Western"), ChangeLogOperation.Creation, []),
            new ChangeLogEntryItem(Guid.CreateVersion7(), new DateTimeOffset(2026, 11, 4, 8, 0, 0, TimeSpan.Zero), RecordType.ApplicationSettings,
                Guid.CreateVersion7(), new ApplicationSettingsLabel(), ChangeLogOperation.Modification, []),
        };

        var page = Render<AdminHome>();

        var rows = BlockOf(page, "Dernières modifications").QuerySelectorAll("li");
        Assert.Equal(
            [
                ("L'Iris blanc (Astérix - T. 40)", "Album modifié · 06/11/2026 10:42", AppPaths.NotYetImplemented),
                ("Western", "Genre créé · 05/11/2026 21:08", AppPaths.NotYetImplemented),
                ("Paramétrage de l'application", "Paramétrage modifié · 04/11/2026 09:00", AppPaths.AdminSettings),
            ],
            rows.Select(row => (
                row.QuerySelector(".admin-change-label")!.TextContent.Trim(),
                Text(row.QuerySelector(".admin-home-note")!),
                row.QuerySelector("a")!.GetAttribute("href"))));
        Assert.All(rows, row => Assert.Equal("Modifier", row.QuerySelector("a")!.TextContent.Trim()));
        Assert.Contains("/admin/change-log/latest-records?count=6", _api.Requested);
    }

    [Fact]
    public void LatestChanges_NoneYet_SaysSo()
    {
        var page = Render<AdminHome>();

        Assert.Equal("Aucune fiche n'a encore été saisie.", BlockOf(page, "Dernières modifications").QuerySelector("p.admin-home-note")!.TextContent.Trim());
    }

    [Fact]
    public void Settings_EachWithItsValue_ToModify()
    {
        _api.Answers["/catalog/settings"] = new ApplicationSettings(AlbumLabelFormat.VolumeTitleSeries, DisplayCulture.FrenchFrance);

        var page = Render<AdminHome>();

        var settings = BlockOf(page, "Paramétrage").QuerySelectorAll(".admin-setting").Select(row => (
            row.QuerySelector("dt")!.TextContent.Trim(), row.QuerySelector("dd")!.TextContent.Trim(), row.QuerySelector("a")!.GetAttribute("href")));
        Assert.Equal(
            [("Libellé des albums", "Tome - Album (Série)", AppPaths.AdminSettings), ("Culture d'affichage", "Français (France)", AppPaths.AdminSettings)],
            settings);
    }

    [Fact]
    public void PrivateStatistics_NoneDefinedYet_SaysSo()
    {
        var page = Render<AdminHome>();

        Assert.StartsWith("Aucune statistique privée n'est encore définie", BlockOf(page, "Statistiques privées").QuerySelector(".admin-statistics-none")!.TextContent.Trim());
    }

    [Fact]
    public void ApiOutOfReach_PresentsATechnicalError_InsteadOfTheBlocks()
    {
        _api.Failures["/admin/checks"] = HttpStatusCode.ServiceUnavailable;

        var page = Render<AdminHome>();

        Assert.Equal("Administration", page.Find("h1").TextContent.Trim());
        Assert.Equal("technical", page.Find("[role='alert']").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll("section.block"));
    }

    [Fact]
    public void Display_IsRecordedAsTheHomeOfTheAdministration()
    {
        var page = Render<AdminHome>();

        Assert.Equal(AudienceFunctions.AdminHome, page.FindComponent<Components.Audience.AudienceScreen>().Instance.Function);
    }

    private static IElement BlockOf(IRenderedComponent<AdminHome> page, string title) =>
        page.FindAll("section.block").Single(block => block.QuerySelector(".block-title")!.TextContent.Trim() == title);

    // The text as read, its spaces of the French culture (narrow and no-break) written as plain ones.
    private static string Text(IElement element) =>
        string.Join(' ', element.TextContent.Replace(' ', ' ').Replace(' ', ' ').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
