using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Search;

namespace Bdtheque.ValidationData;

/// <summary>
/// Loads the fictitious data of the validation environment
/// (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md) through the
/// administration API, like any client of the API:
/// every rule of the application applies to it, and the change log records it. The data covers the cases the
/// screens present — a long series with omnibuses, special issues and missing volumes, purchase intents on a
/// whole album and on an edition, many authors, nested universes, editions without cover or with every type of
/// visual, amounts in several currencies, navigation entries outside the letters, a pale cover that can be put in the spotlight
/// on demand — and evolves with the screens.
/// </summary>
public sealed class ValidationDataLoader(HttpClient client, TimeProvider clock)
{
    /// <summary>Header of the secret shared by `api` and its internal clients (choix-implementation/socle/transverse/authentification-mise-en-oeuvre.md).</summary>
    public const string InternalApiKeyHeader = "X-Internal-Api-Key";

    public const string LongSeriesTitle = "Les Veilleurs d'Ombrelune";
    public const string MultiEditionOneShotTitle = "Le Phare aux mille lanternes";
    public const string CollectiveTitle = "...Et après ?";
    public const string SeriesWithoutAlbumTitle = "Les Gardiens du phare";
    public const string PaleCoverTitle = "Soleil pâle";

    public const string UncreditedAuthorLastName = "Delorme";

    /// <summary>A publisher with nothing but its name, whose deletion is accepted.</summary>
    public const string UnusedPublisherName = "Éditions Sans Rivage";

    /// <summary>A publisher only named by the edition to buy of a purchase intent.</summary>
    public const string IntendedPublisherName = "Éditions de l'Horizon";

    /// <summary>The volumes of the series catalogued only, enough for the albums to count more than a page of a list.</summary>
    private const int ChroniclesVolumeCount = 25;

    private static readonly string[] LongSeriesVolumeTitles =
    [
        "La Lanterne froide", "Le Guet des marées", "Les Cloches de brume", "Le Serment du phare", "La Nuit des sept lunes",
        "L'Horloger d'Ombrelune", "", "Les Ailes de cendre", "Le Pont des soupirs", "La Ronde des veilleurs",
        "Les Murmures du port", "L'Écume noire", "", "Le Retour de l'horloger", "La Marée haute", "Les Enfants du guet",
        "Le Feu sur la digue", "La Clé des songes", "L'Île sans nom", "", "Les Lanternes éteintes", "Le Grand Reflux",
        "La Veille ultime", "L'Aube des veilleurs",
    ];

    /// <summary>The only argument of the program, passed by scripts/Start-Validation.ps1 on demand.</summary>
    public const string PaleCoverSpotlightArgument = "--pale-cover-spotlight";

    /// <summary>Whether the arguments of the program ask for the pale cover in the spotlight.</summary>
    /// <exception cref="ArgumentException">Any other argument: the load stops rather than load data other than those asked for.</exception>
    public static bool PaleCoverInTheSpotlight(string[] arguments) => arguments switch
    {
        [] => false,
        [PaleCoverSpotlightArgument] => true,
        _ => throw new ArgumentException(
            $"Unexpected arguments '{string.Join(' ', arguments)}': the only one accepted is {PaleCoverSpotlightArgument}.", nameof(arguments)),
    };

    /// <param name="paleCoverInTheSpotlight">
    /// Puts the pale cover in the spotlight of the days around the load, on demand, to judge a screen in « La couverture du
    /// jour » on its most demanding case. Otherwise the load draws nothing: the application draws the edition of the day among
    /// all the owned editions at the first display of the day, as in production.
    /// </param>
    /// <param name="cancellationToken">Stops the load.</param>
    public async Task LoadAsync(bool paleCoverInTheSpotlight, CancellationToken cancellationToken)
    {
        // The day of the load, read once: the data around it (the spotlight of the days around it on demand, a volume to be published
        // the year after) is consistent even when the load runs over midnight.
        var data = new Loading(client, DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), cancellationToken);

        var adventure = await data.GenreAsync("Aventure");
        var scienceFiction = await data.GenreAsync("Science-fiction");
        var fantasy = await data.GenreAsync("Fantastique");
        var humour = await data.GenreAsync("Humour");
        var history = await data.GenreAsync("Historique");
        var western = await data.GenreAsync("Western");
        await data.GenreAsync("Policier");
        // Accented and without album: the list of the genres refuses « epopee » as already taken (modele-metier/genre.md).
        await data.GenreAsync(AccentedGenre);

        var ombrelune = await data.UniverseAsync("Ombrelune", "Monde imaginaire de lunes et de marées.", null);
        var archipelago = await data.UniverseAsync("Archipel des Brumes", "Îles perdues aux confins d'Ombrelune.", ombrelune);
        var greyPort = await data.UniverseAsync("Port-Grisé", null, archipelago);
        var watchersCity = await data.UniverseAsync("Cité des Veilleurs", null, ombrelune);
        var wildWest = await data.UniverseAsync("Far West imaginaire", null, null);

        var lagoon = await data.PublisherAsync("Éditions du Lagon", "https://www.example.org/lagon", "Grand Large", "Escales");
        var bubbles = await data.PublisherAsync("Bulles & Cie", null, "Petit Format");
        var blackPlate = await data.PublisherAsync("Planche Noire", "https://www.example.com/planche-noire");
        // The cases of the list of the publishers: many collections and no edition, whose deletion takes the collections with
        // it; nothing but a name, whose deletion is accepted; only named by an edition to buy, none in the collection.
        await data.PublisherAsync("Atelier Mistral", "https://www.example.net/atelier-mistral", "Carnets", "Escapades", "Horizons", "Lumières", "Traversées");
        await data.PublisherAsync(UnusedPublisherName, null);
        var horizon = await data.PublisherAsync(IntendedPublisherName, "https://www.example.org/horizon");

        var arnaud = await data.AuthorAsync("Arnaud", "Émile", nationality: "Française", biography: "Scénariste au long cours, conteur des marées d'Ombrelune.");
        var vanSluys = await data.AuthorAsync("Van Sluys", "Jeanne", nationality: "Belge");
        var ostrova = await data.AuthorAsync("Ostrova", "Mina");
        var brume = await data.AuthorAsync(pseudonym: "Brume");
        var pinceau = await data.AuthorAsync(pseudonym: "Les Frères Pinceau", biography: "Duo de dessinateurs signant d'un nom commun.");
        var kador = await data.AuthorAsync("Kervella", "Antoine", "Kador");
        var etienne = await data.AuthorAsync("Étienne", "Élodie");
        var lund = await data.AuthorAsync("Lund", "Ørjan", nationality: "Norvégienne");
        var tanabe = await data.AuthorAsync("Tanabe", "Yuki", nationality: "Japonaise");
        var quintin = await data.AuthorAsync("Quintin", "Zoé");
        var ganier = await data.AuthorAsync("Ganier", "Paul");
        var keita = await data.AuthorAsync("Keïta", "Nora");
        var rambert = await data.AuthorAsync("Rambert", "Théo");
        var ferraz = await data.AuthorAsync("Ferraz", "Inès");
        var benali = await data.AuthorAsync("Benali", "Malik");
        var lemaire = await data.AuthorAsync("Lemaire");
        // An author credited on nothing yet, without nationality: the list of the authors says so, and its deletion is accepted.
        await data.AuthorAsync(UncreditedAuthorLastName, "Sacha");

        // A pale cover with an accent of middle tone, the most demanding one for « La couverture du jour », which takes its
        // colours on every page (visuel/dashboard/la-couverture-du-jour.md, Lisibilité). On demand, it is drawn for the days
        // around the load while it is the only edition with a cover. Acquired lately, it is among the latest arrivals.
        var paleSun = await data.AlbumAsync(OneShot(PaleCoverTitle, 2026, 9, [humour], [], [Credit(quintin, ContributionRole.Illustrator)]));
        var paleSunEdition = await data.EditionAsync(paleSun, Owned(bubbles.Id, null, 2026, AcquisitionMode.Purchase, new DateOnly(2026, 10, 25)));
        await data.VisualAsync(paleSun, paleSunEdition, VisualType.Cover, Images.Banded("#F7E98E", "#B85AA2"));
        if (paleCoverInTheSpotlight)
            await data.SpotlightAroundTodayAsync();

        await LoadLongSeriesAsync(data, lagoon, bubbles, [adventure, fantasy], watchersCity, archipelago,
            [Credit(arnaud, ContributionRole.Scenarist), Credit(vanSluys, ContributionRole.Illustrator), Credit(ostrova, ContributionRole.Colorist)]);

        // A completed series, under the `#` navigation entry.
        var moons = await data.SeriesAsync(Series("13 Lunes", SeriesStatus.Completed,
            Template(blackPlate.Id, null, EditionFormat.Standard), [scienceFiction], [],
            [Credit(brume, ContributionRole.Scenarist), Credit(pinceau, ContributionRole.Illustrator)]) with
        {
            TheoreticalVolumeCount = 3, IsComplete = true,
        });
        var redMoon = await data.AlbumAsync(Album("Lune rousse", moons, 1, 1985, rating: AlbumRating.Good));
        var redMoonEdition = await data.EditionAsync(redMoon, Owned(blackPlate.Id, null, 1985, AcquisitionMode.Purchase, new DateOnly(2005, 6, 18)) with
        {
            Isbn = "2-905123-01-X", Category = EditionCategory.FirstEdition, IsSecondHand = true, Condition = EditionCondition.Good, AcquisitionAmount = 7.50m, AcquisitionCurrency = "EUR",
        });
        await data.VisualAsync(redMoon, redMoonEdition, VisualType.Cover, Images.Portrait(0.02));
        var inkMoon = await data.AlbumAsync(Album("Lune d'encre", moons, 2, 1987, month: 9));
        var inkMoonEdition = await data.EditionAsync(inkMoon, Owned(blackPlate.Id, null, 1987, AcquisitionMode.Gift, new DateOnly(1987, 12, 25)) with { IsFree = true });
        await data.VisualAsync(inkMoon, inkMoonEdition, VisualType.Cover, Images.Portrait(0.62));
        var lastMoon = await data.AlbumAsync(Album("Dernière lune", moons, 3, 1990, rating: AlbumRating.VeryGood));
        var lastMoonEdition = await data.EditionAsync(lastMoon, Owned(blackPlate.Id, null, 1990, AcquisitionMode.Trade, new DateOnly(1994, 3, 2)) with
        {
            AcquisitionAmount = 45m, AcquisitionCurrency = "FRF", InitialValueAmount = 52m, InitialValueCurrency = "FRF",
        });
        await data.VisualAsync(lastMoon, lastMoonEdition, VisualType.Cover, Images.Portrait(0.75));

        // An abandoned series, sorted under its article; its colourist is credited on the series alone, the
        // albums carrying contributions of their own. Two volumes only: the pile of covers is incomplete. In black and
        // white, as its new editions are proposed.
        var sparrowhawk = await data.SeriesAsync(Series("L'Épervier des sables", SeriesStatus.Abandoned,
            Template(bubbles.Id, bubbles.Collections[0], EditionFormat.Pocket, BindingType.Paperback) with { IsColor = false }, [adventure, history], [],
            [Credit(kador, ContributionRole.Scenarist), Credit(etienne, ContributionRole.Illustrator), Credit(ostrova, ContributionRole.Colorist)]));
        ContributionContent[] sparrowhawkCredits = [Credit(kador, ContributionRole.Scenarist), Credit(etienne, ContributionRole.Illustrator)];
        var dunes = await data.AlbumAsync(Album("Le Désert des dunes", sparrowhawk, 1, 1955) with { Contributions = sparrowhawkCredits });
        // An old-franc value, before the new franc of 1960, and an inherited copy with no amount.
        var dunesEdition = await data.EditionAsync(dunes, Owned(bubbles.Id, bubbles.Collections[0], 1955, AcquisitionMode.Inherited, null) with
        {
            Binding = BindingType.Paperback, Format = EditionFormat.Pocket, Category = EditionCategory.FirstEdition, Condition = EditionCondition.Average,
            IsSecondHand = true,
            IsColor = false, InitialValueAmount = 350m, InitialValueCurrency = "QZF",
        });
        await data.VisualAsync(dunes, dunesEdition, VisualType.Cover, Images.Portrait(0.11));
        var oasis = await data.AlbumAsync(Album("L'Oasis perdue", sparrowhawk, 2, 1957) with { Contributions = sparrowhawkCredits });
        var oasisEdition = await data.EditionAsync(oasis, Owned(bubbles.Id, bubbles.Collections[0], 1957, AcquisitionMode.Purchase, new DateOnly(2012, 4, 7)) with
        {
            Binding = BindingType.Paperback, Format = EditionFormat.Pocket, IsSecondHand = true, AcquisitionAmount = 18m, AcquisitionCurrency = "EUR",
        });
        await data.VisualAsync(oasis, oasisEdition, VisualType.Cover, Images.Portrait(0.13));

        // A series whose title begins with a letter of another alphabet, under `@`, catalogued only.
        var omega = await data.SeriesAsync(Series("Ωmega", null, Template(null, null, null), [scienceFiction], [],
            [Credit(lund, ContributionRole.Scenarist), Credit(tanabe, ContributionRole.Illustrator)]));
        var pointZero = await data.AlbumAsync(Album("Point zéro", omega, 1, 2024));
        // Designated by a saved search: the deletion of the album is refused while the search designates it.
        await data.SavedSearchAsync("Albums à suivre", pointZero.Id);

        // A series catalogued only, volume after volume: the list of the albums of the administration counts more than a page.
        var chronicles = await data.SeriesAsync(Series("Chroniques de Port-Grisé", SeriesStatus.InProgress, Template(null, null, null),
            [humour], [greyPort], [Credit(quintin, ContributionRole.Scenarist), Credit(keita, ContributionRole.Illustrator)]));
        for (var volume = 1; volume <= ChroniclesVolumeCount; volume++)
            await data.AlbumAsync(Album(null, chronicles, volume, 1999 + volume));

        // A series announced, without any album yet: the list of the series says so, and its deletion is accepted. Its entry
        // form proposes nothing to its new editions, and leaves it out of the computations.
        await data.SeriesAsync(Series(SeriesWithoutAlbumTitle, SeriesStatus.InProgress,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [fantasy], [],
            [Credit(rambert, ContributionRole.Scenarist)]) with
        {
            ExcludeFromMissingVolumes = true, ExcludeFromReleaseEstimates = true,
        });

        // A one-shot in two editions: the first one with every type of visual, two covers among them; the
        // second one with an ISBN whose check digit is wrong, as printed by some publishers.
        var lighthouse = await data.AlbumAsync(OneShot(MultiEditionOneShotTitle, 1989, 11, [fantasy], [greyPort],
            [Credit(quintin, ContributionRole.Scenarist), Credit(quintin, ContributionRole.Illustrator)]) with
        {
            Rating = AlbumRating.VeryGood, PersonalNotes = "Le préféré de la collection.",
        });
        var lighthouseFirst = await data.EditionAsync(lighthouse, Owned(blackPlate.Id, null, 1989, AcquisitionMode.Purchase, new DateOnly(1990, 2, 14)) with
        {
            Isbn = Isbn("978290512302"), Category = EditionCategory.FirstEdition, IsDedicated = true, PageCount = 62,
            PersonalNotes = "Exemplaire dédicacé lors d'un festival.",
            AcquisitionAmount = 89m, AcquisitionCurrency = "FRF", InitialValueAmount = 79m, InitialValueCurrency = "FRF",
        });
        await data.VisualAsync(lighthouse, lighthouseFirst, VisualType.Cover, Images.Portrait(0.55));
        await data.VisualAsync(lighthouse, lighthouseFirst, VisualType.Cover, Images.Portrait(0.58));
        await data.VisualAsync(lighthouse, lighthouseFirst, VisualType.Dedication, Images.Landscape(0.08));
        await data.VisualAsync(lighthouse, lighthouseFirst, VisualType.Endpaper, Images.Landscape(0.30));
        await data.VisualAsync(lighthouse, lighthouseFirst, VisualType.Plate, Images.Portrait(0.40));
        await data.VisualAsync(lighthouse, lighthouseFirst, VisualType.Plate, Images.Landscape(0.45));
        await data.VisualAsync(lighthouse, lighthouseFirst, VisualType.BackCover, Images.Portrait(0.52));
        var lighthouseReissue = await data.EditionAsync(lighthouse, Owned(lagoon.Id, lagoon.Collections[1], 2015, AcquisitionMode.Purchase, new DateOnly(2016, 1, 9)) with
        {
            Isbn = WrongIsbn("979109123499"), AcquisitionAmount = 24.90m, AcquisitionCurrency = "EUR", InitialValueAmount = 24.90m, InitialValueCurrency = "EUR",
        });
        await data.VisualAsync(lighthouse, lighthouseReissue, VisualType.Cover, Images.Portrait(0.60));

        // A collective album, many authors credited, under `@`, won in a contest: more drawers than the line of a role
        // of its record holds on a smartphone (visuel/fiche-album/contenu.md, Contributions).
        var collective = await data.AlbumAsync(OneShot(CollectiveTitle, 2019, null, [humour], [],
        [
            Credit(ganier, ContributionRole.Scenarist), Credit(keita, ContributionRole.Scenarist), Credit(rambert, ContributionRole.Illustrator),
            Credit(ferraz, ContributionRole.Illustrator), Credit(benali, ContributionRole.Illustrator), Credit(tanabe, ContributionRole.Illustrator),
            Credit(vanSluys, ContributionRole.Illustrator), Credit(etienne, ContributionRole.Illustrator), Credit(pinceau, ContributionRole.Illustrator),
            Credit(lund, ContributionRole.Illustrator), Credit(lemaire, ContributionRole.Colorist),
        ]));
        var collectiveEdition = await data.EditionAsync(collective, Owned(bubbles.Id, null, 2019, AcquisitionMode.Won, new DateOnly(2019, 10, 31)) with
        {
            Category = EditionCategory.SpecialEdition, AcquisitionAmount = 15m, AcquisitionCurrency = "EUR",
        });
        await data.VisualAsync(collective, collectiveEdition, VisualType.Cover, Images.Portrait(0.85));

        // Bought abroad, in dollars; sorted under its article, under `E`.
        var summer = await data.AlbumAsync(OneShot("Un été à Port-Grisé", 2021, 6, [adventure], [greyPort], [Credit(etienne, ContributionRole.Illustrator), Credit(arnaud, ContributionRole.Scenarist)]));
        var summerEdition = await data.EditionAsync(summer, Owned(lagoon.Id, lagoon.Collections[1], 2021, AcquisitionMode.Purchase, new DateOnly(2021, 7, 3)) with
        {
            AcquisitionAmount = 19.99m, AcquisitionCurrency = "USD",
        });
        await data.VisualAsync(summer, summerEdition, VisualType.Cover, Images.Portrait(0.48));

        // A free gift, without cover.
        var fog = await data.AlbumAsync(OneShot("Brouillard sur Ombrelune", 2008, null, [fantasy], [ombrelune], [Credit(brume, ContributionRole.Scenarist), Credit(pinceau, ContributionRole.Illustrator)]));
        await data.EditionAsync(fog, Owned(lagoon.Id, null, 2008, AcquisitionMode.Gift, new DateOnly(2008, 12, 24)) with { IsFree = true });
        // Owned, and wished for again from another publisher, which no owned edition names.
        await data.IntentOnEditionAsync(fog, new PurchaseIntentEditionContent(horizon.Id, null, 2023, null, null, null, null, null, null, null, true));

        // Wished for as a whole, with no edition known yet.
        var convoy = await data.AlbumAsync(OneShot("Le Dernier Convoi", 2025, 3, [western], [wildWest], [Credit(rambert, ContributionRole.Illustrator), Credit(ganier, ContributionRole.Scenarist)]));
        await data.IntentOnAlbumAsync(convoy);

        // Wished for in one edition, the only one of the album, outside the collection: the intent may turn into one on the
        // whole album (visuel/listes/saisie-selon-l-entite.md).
        var tide = await data.AlbumAsync(OneShot("La Marée des phares", 2024, 9, [adventure], [], [Credit(arnaud, ContributionRole.Scenarist), Credit(keita, ContributionRole.Illustrator)]));
        await data.IntentOnEditionAsync(tide, new PurchaseIntentEditionContent(horizon.Id, null, 2024, null, null, null, null, null, null, null, true));

        // The cases of the entry form of an album (visuel/saisie-album/contenu.md): a sort key entered by hand, a universe
        // two levels down, shown with its ancestors, and no author nor series yet, so that attaching it to a series with
        // authors credits them and says so.
        await data.AlbumAsync(new AlbumContent("Les Carnets de Port-Grisé", "Port-Grisé (carnets)", null, AlbumType.Regular, false, null, null, null,
            2019, null, null, "À relier à sa série.", null, [humour], [greyPort], []));

        // The latest writes, which the home page of the administration presents: a record modified, under its new name,
        // and a record deleted, left out of the latest changes.
        await data.RenameGenreAsync(western, "Western spaghetti");
        await data.DeleteGenreAsync(await data.GenreAsync(DeletedGenre));
    }

    /// <summary>A genre whose label differs from another typed only by its case and its accents.</summary>
    public const string AccentedGenre = "Épopée";

    /// <summary>A genre created then deleted, which the latest changes leave out.</summary>
    public const string DeletedGenre = "Genre supprimé";

    private static async Task LoadLongSeriesAsync(
        Loading data, Publisher lagoon, Publisher bubbles, Guid[] genres, Guid universe, Guid specialIssueUniverse, ContributionContent[] credits)
    {
        var series = await data.SeriesAsync(Series(LongSeriesTitle, SeriesStatus.InProgress,
            Template(lagoon.Id, lagoon.Collections[0], EditionFormat.Standard, BindingType.Hardcover), genres, [universe], credits) with
        {
            TheoreticalVolumeCount = 30,
            Summary = "Dans la cité d'Ombrelune, les veilleurs gardent les lanternes qui retiennent la marée des songes.",
            PersonalNotes = "Collection commencée en 1999.",
        });

        for (var volume = 1; volume <= LongSeriesVolumeTitles.Length; volume++)
        {
            // Missing from the catalogue: the gaps of the series.
            if (volume is not (13 or 20))
                await LoadLongSeriesVolumeAsync(data, lagoon, series, volume);
        }

        await LoadLongSeriesSpecialsAsync(data, lagoon, bubbles, series, specialIssueUniverse);
    }

    private static async Task LoadLongSeriesVolumeAsync(Loading data, Publisher lagoon, Guid series, int volume)
    {
        // The last volume is announced, to be published.
        var year = volume == LongSeriesVolumeTitles.Length ? data.Today.Year + 1 : 1998 + volume;
        var title = LongSeriesVolumeTitles[volume - 1];
        var album = await data.AlbumAsync(Album(title.Length > 0 ? title : null, series, volume, year,
            month: volume % 3 == 0 ? 1 + volume % 12 : null, rating: volume % 4 == 0 ? null : (AlbumRating)(1 + volume % 5)));

        switch (volume)
        {
            // Catalogued, neither owned nor wished for.
            case 22:
                return;
            // Announced: wished for as a whole.
            case 24:
                await data.IntentOnAlbumAsync(album);
                return;
        }

        var francs = year < 2002;
        var edition = await data.EditionAsync(album, Owned(lagoon.Id, lagoon.Collections[0], year, LongSeriesAcquisition(volume),
            new DateOnly(year, 1 + volume % 12, 1 + volume % 27)) with
        {
            Isbn = Isbn($"9791091234{volume:D2}"), PageCount = 48 + volume % 3 * 8, IsSecondHand = volume % 6 == 0,
            // Most volumes with a category, every one of them, some without (at least half of the owned editions have one).
            Category = (volume % 4) switch
            {
                1 or 3 => EditionCategory.FirstEdition,
                2 => EditionCategory.SpecialEdition,
                _ => volume % 8 == 0 ? EditionCategory.LimitedEdition : null,
            },
            AcquisitionAmount = francs ? 59m + volume : 9.50m + volume / 2m, AcquisitionCurrency = francs ? "FRF" : "EUR",
        });

        // Owned without cover.
        if (volume is not (10 or 11 or 12))
            await data.VisualAsync(album, edition, VisualType.Cover, Images.Portrait(volume / 24d));

        // Owned, and wished for again in two other editions: its limited edition, every fact of it known, and a reissue of
        // which only the year is (visuel/fiche-album/contenu.md, Intentions d'achat).
        if (volume == 23)
        {
            await data.IntentOnEditionAsync(album, new PurchaseIntentEditionContent(lagoon.Id, lagoon.Collections[0], 2022, Isbn("979109123450"),
                BindingType.Hardcover, BookOrientation.Portrait, ReadingDirection.LeftToRight, EditionFormat.Large, 96, EditionCategory.LimitedEdition, IsColor: true));
            await data.IntentOnEditionAsync(album, new PurchaseIntentEditionContent(lagoon.Id, null, 2025, null, null, null, null, null, null, null, true));
        }
    }

    // The omnibuses and the special issues of the long series.
    private static async Task LoadLongSeriesSpecialsAsync(Loading data, Publisher lagoon, Publisher bubbles, Guid series, Guid specialIssueUniverse)
    {
        var firstOmnibus = await data.AlbumAsync(Album(null, series, 1, 2010) with { Type = AlbumType.Omnibus, StartVolumeNumber = 1, EndVolumeNumber = 4 });
        var firstOmnibusEdition = await data.EditionAsync(firstOmnibus, Owned(lagoon.Id, lagoon.Collections[1], 2010, AcquisitionMode.Purchase, new DateOnly(2010, 11, 20)) with
        {
            Isbn = Isbn("979109123460"), PageCount = 192, AcquisitionAmount = 29.95m, AcquisitionCurrency = "EUR",
        });
        await data.VisualAsync(firstOmnibus, firstOmnibusEdition, VisualType.Cover, Images.Portrait(0.33));
        await data.AlbumAsync(Album(null, series, 2, 2012) with { Type = AlbumType.Omnibus, StartVolumeNumber = 5, EndVolumeNumber = 8 });

        var bestiary = await data.AlbumAsync(Album("Le Bestiaire des marées", series, 1, 2004) with { IsSpecialIssue = true, UniverseIds = [specialIssueUniverse] });
        var bestiaryEdition = await data.EditionAsync(bestiary, Owned(lagoon.Id, null, 2004, AcquisitionMode.Purchase, new DateOnly(2004, 9, 1)) with
        {
            AcquisitionAmount = 14m, AcquisitionCurrency = "EUR",
        });
        await data.VisualAsync(bestiary, bestiaryEdition, VisualType.Cover, Images.Landscape(0.16));
        var sketchbook = await data.AlbumAsync(Album("Carnet de croquis", series, null, 2016) with { IsSpecialIssue = true });
        var sketchbookEdition = await data.EditionAsync(sketchbook, Owned(bubbles.Id, null, 2016, AcquisitionMode.Purchase, new DateOnly(2017, 1, 15)) with
        {
            Format = EditionFormat.Special, Category = EditionCategory.LimitedEdition, IsColor = false, AcquisitionAmount = 22m, AcquisitionCurrency = "EUR",
        });
        await data.VisualAsync(sketchbook, sketchbookEdition, VisualType.Cover, Images.Portrait(0.70));
        var tales = await data.AlbumAsync(Album("Les Contes de la Cité", series, null, 2020) with { Type = AlbumType.Omnibus, IsSpecialIssue = true });
        await data.EditionAsync(tales, Owned(lagoon.Id, lagoon.Collections[0], 2020, AcquisitionMode.Purchase, new DateOnly(2020, 12, 5)) with
        {
            Category = EditionCategory.SpecialEdition, AcquisitionAmount = 35m, AcquisitionCurrency = "EUR",
        });
    }

    private static AcquisitionMode LongSeriesAcquisition(int volume) => volume switch
    {
        5 => AcquisitionMode.Gift,
        9 => AcquisitionMode.Trade,
        _ => AcquisitionMode.Purchase,
    };

    private static ContributionContent Credit(Guid author, ContributionRole role) => new(author, role);

    private static SeriesEditionTemplate Template(Guid? publisher, Guid? collection, EditionFormat? format, BindingType? binding = null) =>
        new(publisher, collection, null, EditionCondition.Excellent, binding, BookOrientation.Portrait, ReadingDirection.LeftToRight, format, true);

    private static SeriesContent Series(
        string title, SeriesStatus? status, SeriesEditionTemplate template, Guid[] genres, Guid[] universes, ContributionContent[] credits) =>
        new(title, null, status, null, false, false, false, null, null, template, genres, universes, credits);

    // Without contributions of its own, an album attached to a series gets those of the series (AlbumAsync).
    private static AlbumContent Album(string? title, Guid series, int? volume, int year, int? month = null, AlbumRating? rating = null) =>
        new(title, null, series, AlbumType.Regular, false, volume, null, null, year, month, null, null, rating, [], [], []);

    private static AlbumContent OneShot(string title, int year, int? month, Guid[] genres, Guid[] universes, ContributionContent[] credits) =>
        new(title, null, null, AlbumType.Regular, false, null, null, null, year, month,
            "Un récit complet en un seul album.", null, AlbumRating.Good, genres, universes, credits);

    private static EditionContent Owned(Guid publisher, Guid? collection, int year, AcquisitionMode mode, DateOnly? acquiredOn) =>
        new(publisher, collection, year, null, BindingType.Hardcover, BookOrientation.Portrait, ReadingDirection.LeftToRight, EditionFormat.Standard,
            48, null, false, true, EditionCondition.Excellent, mode, false, acquiredOn, null, null, false, null, null, null, null);

    // An ISBN-13 from its first twelve digits, with its check digit.
    private static string Isbn(string digits) => digits + CheckDigit(digits);

    private static string WrongIsbn(string digits) => digits + (CheckDigit(digits) + 1) % 10;

    private static int CheckDigit(string digits) =>
        (10 - digits.Select((digit, index) => (digit - '0') * (index % 2 == 0 ? 1 : 3)).Sum() % 10) % 10;

    private sealed record Publisher(Guid Id, Guid[] Collections);

    /// <summary>An album being loaded, with the version its next write sends back.</summary>
    private sealed class AlbumWrite(Guid id, uint version)
    {
        public Guid Id { get; } = id;
        public uint Version { get; set; } = version;
    }

    /// <summary>The calls to the administration API, which stop the load at the first refusal, with its reason.</summary>
    private sealed class Loading(HttpClient client, DateOnly today, CancellationToken cancellationToken)
    {
        /// <summary>The day of the load, in universal time.</summary>
        public DateOnly Today => today;

        public async Task<Guid> GenreAsync(string label) =>
            (await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new(label))).Id;

        public async Task RenameGenreAsync(Guid id, string label)
        {
            var genre = await GetAsync<GenreForm>($"/admin/genres/{id}");
            using var response = await client.PutAsJsonAsync($"/admin/genres/{id}", new UpdateGenreRequest(label, genre.Version), cancellationToken);
            await ReadAsync<GenreForm>(response);
        }

        // As the administration confirms a deletion: with the version of the record and the fingerprint of its impact.
        public async Task DeleteGenreAsync(Guid id)
        {
            var genre = await GetAsync<GenreForm>($"/admin/genres/{id}");
            var impact = await GetAsync<Contracts.Deletion.DeletionImpact>($"/admin/genres/{id}/deletion-impact");
            using var response = await client.DeleteAsync(
                $"/admin/genres/{id}?version={genre.Version}&fingerprint={Uri.EscapeDataString(impact.Fingerprint)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                await ReadAsync<object>(response);
        }

        public async Task<Guid> UniverseAsync(string name, string? description, Guid? parent) =>
            (await PostAsync<CreateUniverseRequest, UniverseForm>("/admin/universes", new(name, description, parent))).Id;

        public async Task<Publisher> PublisherAsync(string name, string? website, params string[] collections)
        {
            var publisher = await PostAsync<PublisherContent, PublisherForm>("/admin/publishers", new(name, website, []));
            var version = publisher.Version;
            var ids = new List<Guid>();
            foreach (var collection in collections)
            {
                var created = await PostAsync<CreatePublisherCollectionRequest, PublisherCollectionForm>(
                    $"/admin/publishers/{publisher.Id}/collections", new(collection, version));
                version = created.PublisherVersion;
                ids.Add(created.Id);
            }

            return new Publisher(publisher.Id, [.. ids]);
        }

        public async Task<Guid> AuthorAsync(
            string? lastName = null, string? firstName = null, string? pseudonym = null, string? biography = null, string? nationality = null) =>
            (await PostAsync<CreateAuthorRequest, AuthorForm>("/admin/authors", new(lastName, firstName, pseudonym, biography, nationality))).Id;

        public async Task<Guid> SeriesAsync(SeriesContent content) =>
            (await PostAsync<SeriesContent, SeriesForm>("/admin/series", content)).Id;

        // As the entry form does once the series is chosen, an album attached to a series gets the authors it gives it
        // (fonctionnel/regles-metier/initialisation-des-contributions-depuis-la-serie.md).
        public async Task<AlbumWrite> AlbumAsync(AlbumContent content)
        {
            if (content.SeriesId is { } series)
            {
                var given = await PostAsync<SeriesAlbumContributionsRequest, SeriesAlbumContributions>(
                    $"/admin/series/{series}/album-contributions", new SeriesAlbumContributionsRequest(true, null, content.Contributions));
                if (given.Applied)
                    content = content with { Contributions = [.. given.Contributions.Select(c => new ContributionContent(c.Author.Id, c.Role))] };
            }

            var album = await PostAsync<AlbumContent, AlbumForm>("/admin/albums", content);
            return new AlbumWrite(album.Id, album.Version);
        }

        public async Task<Guid> EditionAsync(AlbumWrite album, EditionContent content)
        {
            var edition = await PostAsync<CreateEditionRequest, EditionForm>($"/admin/albums/{album.Id}/editions", new(content, album.Version));
            album.Version = edition.AlbumVersion;
            return edition.Id;
        }

        /// <summary>A saved search of the albums that designates one of them.</summary>
        public async Task SavedSearchAsync(string name, Guid album) =>
            await PostAsync<SavedSearchContent, SavedSearchForm>("/admin/saved-searches", new(
                name, EntityKind.Album, new SearchCriterion("record", SearchComparison.Is, [album.ToString()]), []));

        /// <summary>
        /// Draws the edition in the spotlight of the days around today in universal time, which cover the date of the browser
        /// of the validation, whatever its time zone.
        /// </summary>
        public async Task SpotlightAroundTodayAsync()
        {
            foreach (var day in new[] { today.AddDays(-1), today, today.AddDays(1) })
                await GetAsync<Contracts.Catalog.SpotlightColors>($"/catalog/spotlight/colors?day={day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }

        public async Task VisualAsync(AlbumWrite album, Guid edition, VisualType type, byte[] image)
        {
            var file = new ByteArrayContent(image);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            using var form = new MultipartFormDataContent
            {
                { file, UploadVisualFields.File, "visual.png" },
                { new StringContent(type.ToString()), UploadVisualFields.Type },
                { new StringContent(album.Version.ToString(CultureInfo.InvariantCulture)), UploadVisualFields.AlbumVersion },
            };
            using var response = await client.PostAsync($"/admin/albums/{album.Id}/editions/{edition}/visuals", form, cancellationToken);
            album.Version = (await ReadAsync<EditionVisualsForm>(response)).AlbumVersion;
        }

        public Task IntentOnAlbumAsync(AlbumWrite album) => IntentAsync(album, null);

        public Task IntentOnEditionAsync(AlbumWrite album, PurchaseIntentEditionContent edition) => IntentAsync(album, edition);

        private async Task IntentAsync(AlbumWrite album, PurchaseIntentEditionContent? edition) =>
            album.Version = (await PostAsync<CreatePurchaseIntentRequest, PurchaseIntentsForm>(
                $"/admin/albums/{album.Id}/purchase-intents", new(edition, album.Version))).AlbumVersion;

        private async Task<T> GetAsync<T>(string path)
        {
            using var response = await client.GetAsync(path, cancellationToken);
            return await ReadAsync<T>(response);
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request)
        {
            using var response = await client.PostAsJsonAsync(path, request, cancellationToken);
            return await ReadAsync<TResponse>(response);
        }

        private async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} refused ({(int)response.StatusCode}): "
                    + await response.Content.ReadAsStringAsync(cancellationToken),
                    null, response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new InvalidOperationException($"Empty response to {response.RequestMessage?.RequestUri}.");
        }
    }
}
