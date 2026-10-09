namespace ThemeProto.Data;

/// <summary>Stands for the API: the same fictitious data as the dashboard mockups.</summary>
public static class FakeData
{
    private static readonly string[] Greetings =
    [
        "Entrez donc, il y a de quoi lire !",
        "Installez-vous, la collection vous attend !",
        "Alors, on bouquine ?",
        "Encore de la place sur les étagères ? Pas sûr…",
        "Attention, ça déborde des étagères !",
    ];

    public const int AlbumCount = 2847;

    public static DashboardData Dashboard()
    {
        var types = new (AlbumKind Kind, string Label, int Count)[]
        {
            (AlbumKind.Regular, "réguliers", 2571), (AlbumKind.Omnibus, "intégrales", 184), (AlbumKind.Special, "hors-séries", 92),
        };
        var total = types.Sum(t => t.Count);

        return new DashboardData(
            new HeroData(Greetings[Random.Shared.Next(Greetings.Length)], AlbumCount, 412, 2913),
            new NewArrivalsData(
            [
                new("Alors, tout tombe (Blacksad - T. 7)", "Dargaud", new(2026, 10, 3), "/albums"),
                new("L'Iris blanc (Astérix - T. 40)", "Albert René", new(2026, 9, 28), "/albums"),
                new("Les Indes fourbes", "Delcourt", new(2026, 9, 21), "/albums"),
                new("Le Château des étoiles - INT. [1 à 2]", "Rue de Sèvres", null, "/albums"),
                new("Mister Prairie (Undertaker - T. 7)", "Dargaud", new(2026, 9, 9), "/albums"),
                new("Spirou et Fantasio - Hors-série 2", "Dupuis", new(2026, 9, 2), "/albums"),
            ], "/albums"),
            new ValueData(new(38760, 55230), 497, new(31482, 44910), 2391,
            [
                new(PriceKind.Average, "Prix moyen", new(13.10m, 18.70m)), new(PriceKind.Median, "Prix médian", new(11.90m, 16.40m)),
                new(PriceKind.Min, "Le moins cher", new(0.90m, 3.20m)), new(PriceKind.Max, "Le plus cher", new(189m, 214.50m)),
            ], 12, 25),
            new TypeDistributionData(types
                .Select(t => new TypeShare(t.Kind, t.Label, t.Count, (int)Math.Round(t.Count * 100m / total)))
                .ToList()),
            new GenresData(Links(("Aventure", 612), ("Humour", 488), ("Science-fiction", 341), ("Policier", 297),
                ("Fantastique", 254), ("Historique", 231), ("Western", 118)), new CountedLink("Autres", 506, "/albums")),
            new PublishersData(Links(("Dargaud", 548), ("Dupuis", 506), ("Casterman", 351), ("Glénat", 318),
                ("Delcourt", 294), ("Le Lombard", 247), ("Soleil", 181)), new CountedLink("Autres", 468, "/albums")));
    }

    public static AlbumListData Albums() => new(Links(
        ("Alors, tout tombe (Blacksad - T. 7)", 1), ("Les Indes fourbes", 2), ("L'Iris blanc (Astérix - T. 40)", 1),
        ("Le Lotus bleu (Tintin - T. 5)", 3), ("Mister Prairie (Undertaker - T. 7)", 1), ("Spirou et Fantasio - Hors-série 2", 1)));

    private static List<CountedLink> Links(params (string Name, int Count)[] items) =>
        items.Select(i => new CountedLink(i.Name, i.Count, "/albums")).ToList();
}
