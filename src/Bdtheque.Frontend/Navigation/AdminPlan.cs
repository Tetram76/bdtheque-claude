namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// The plan of the administration (visuel/administration/en-tete-et-menu.md): its pages by groups, which its menu presents and
/// whose order the cards of its home follow (visuel/administration/contenu.md). Written once, so that the home and the menu
/// cannot present the types of record in two orders (#477).
/// </summary>
public static class AdminPlan
{
    /// <summary>
    /// The groups of the plan, in order; no creation of an edition, which is entered from its album
    /// (visuel/listes/principes-communs.md).
    /// </summary>
    public static IReadOnlyList<AdminPlanGroup> Groups { get; } =
    [
        new("Home", [new("Admin.Dashboard", AppPaths.AdminHome, Exact: true)]),
        new("Collection", [new("Admin.Series", AppPaths.AdminSeries), new("Admin.Albums", AppPaths.AdminAlbums),
            new("Admin.Editions", AppPaths.AdminEditions), new("Admin.PurchaseIntents", AppPaths.AdminPurchaseIntents)]),
        new("AuthorsAndPublishers", [new("Admin.Authors", AppPaths.AdminAuthors), new("Admin.Publishers", AppPaths.AdminPublishers)]),
        new("Referentials", [new("Admin.Genres", AppPaths.AdminGenres), new("Admin.Universes", AppPaths.AdminUniverses)]),
        new("Settings", [new("Admin.Settings", AppPaths.AdminSettings), new("Admin.Statistics", AppPaths.AdminStatistics)]),
    ];

    private static readonly string[] Paths = [.. Groups.SelectMany(group => group.Entries).Select(entry => entry.Path)];

    /// <summary>The place of a page in the plan, by which another presentation of its pages follows its order.</summary>
    /// <exception cref="ArgumentException">The page is not in the plan: it would have no place among the others.</exception>
    public static int PlaceOf(string path)
    {
        var place = Array.IndexOf(Paths, path);
        return place >= 0 ? place : throw new ArgumentException($"The page {path} is not in the plan of the administration.", nameof(path));
    }
}

/// <summary>A group of the plan of the administration: the key of its title in the texts of the menu, and its entries.</summary>
public sealed record AdminPlanGroup(string Name, IReadOnlyList<NavigationEntry> Entries);
