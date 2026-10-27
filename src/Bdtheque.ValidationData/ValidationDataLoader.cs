using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.ValidationData;

/// <summary>
/// Loads the fictitious data of the validation environment (choix-implementation/deploiement.md
/// § Environnement de validation fonctionnelle) through the administration API, like any client of the API:
/// every rule of the application applies to it, and the change log records it. The data covers the cases the
/// screens present — a long series with omnibuses, special issues and missing volumes, purchase intents on a
/// whole album and on an edition, many authors, nested universes, editions without cover or with every type of
/// visual, amounts in several currencies, navigation entries outside the letters — and evolves with the screens.
/// </summary>
public sealed class ValidationDataLoader(HttpClient client)
{
    /// <summary>Header of the secret shared by `api` and its internal clients (choix-implementation/socle.md § Authentification : mise en œuvre).</summary>
    public const string InternalApiKeyHeader = "X-Internal-Api-Key";

    public const string LongSeriesTitle = "Les Veilleurs d'Ombrelune";
    public const string MultiEditionOneShotTitle = "Le Phare aux mille lanternes";
    public const string CollectiveTitle = "...Et après ?";

    private static readonly string[] LongSeriesVolumeTitles =
    [
        "La Lanterne froide", "Le Guet des marées", "Les Cloches de brume", "Le Serment du phare", "La Nuit des sept lunes",
        "L'Horloger d'Ombrelune", "", "Les Ailes de cendre", "Le Pont des soupirs", "La Ronde des veilleurs",
        "Les Murmures du port", "L'Écume noire", "", "Le Retour de l'horloger", "La Marée haute", "Les Enfants du guet",
        "Le Feu sur la digue", "La Clé des songes", "L'Île sans nom", "", "Les Lanternes éteintes", "Le Grand Reflux",
        "La Veille ultime", "L'Aube des veilleurs",
    ];

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var data = new Loading(client, cancellationToken);

        var adventure = await data.GenreAsync("Aventure");
        var scienceFiction = await data.GenreAsync("Science-fiction");
        var fantasy = await data.GenreAsync("Fantastique");
        var humour = await data.GenreAsync("Humour");
        var history = await data.GenreAsync("Historique");
        var western = await data.GenreAsync("Western");
        await data.GenreAsync("Policier");

        var ombrelune = await data.UniverseAsync("Ombrelune", "Monde imaginaire de lunes et de marées.", null);
        var archipelago = await data.UniverseAsync("Archipel des Brumes", "Îles perdues aux confins d'Ombrelune.", ombrelune);
        var greyPort = await data.UniverseAsync("Port-Grisé", null, archipelago);
        var watchersCity = await data.UniverseAsync("Cité des Veilleurs", null, ombrelune);
        var wildWest = await data.UniverseAsync("Far West imaginaire", null, null);

        var lagoon = await data.PublisherAsync("Éditions du Lagon", "https://www.example.org/lagon", "Grand Large", "Escales");
        var bubbles = await data.PublisherAsync("Bulles & Cie", null, "Petit Format");
        var blackPlate = await data.PublisherAsync("Planche Noire", "https://www.example.com/planche-noire");

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

        await LoadLongSeriesAsync(data, lagoon, bubbles, [adventure, fantasy], watchersCity, archipelago,
            [Credit(arnaud, ContributionRole.Scenarist), Credit(vanSluys, ContributionRole.Illustrator), Credit(ostrova, ContributionRole.Colorist)]);

        // A completed series, under the `#` navigation entry.
        var moons = await data.SeriesAsync(Series("13 Lunes", SeriesStatus.Completed, 3, isComplete: true,
            Template(blackPlate.Id, null, EditionFormat.Standard), [scienceFiction], [],
            [Credit(brume, ContributionRole.Scenarist), Credit(pinceau, ContributionRole.Illustrator)]));
        var redMoon = await data.AlbumAsync(Album("Lune rousse", moons, 1, 1985, rating: AlbumRating.Good));
        var redMoonEdition = await data.EditionAsync(redMoon, Owned(blackPlate.Id, null, 1985, AcquisitionMode.Purchase, new DateOnly(2005, 6, 18)) with
        {
            Isbn = "2-905123-01-X", IsSecondHand = true, Condition = EditionCondition.Good, AcquisitionAmount = 7.50m, AcquisitionCurrency = "EUR",
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
        // albums carrying contributions of their own. Two volumes only: the pile of covers is incomplete.
        var sparrowhawk = await data.SeriesAsync(Series("L'Épervier des sables", SeriesStatus.Abandoned, null, isComplete: false,
            Template(bubbles.Id, bubbles.Collections[0], EditionFormat.Pocket, BindingType.Paperback), [adventure, history], [],
            [Credit(kador, ContributionRole.Scenarist), Credit(etienne, ContributionRole.Illustrator), Credit(ostrova, ContributionRole.Colorist)]));
        ContributionContent[] sparrowhawkCredits = [Credit(kador, ContributionRole.Scenarist), Credit(etienne, ContributionRole.Illustrator)];
        var dunes = await data.AlbumAsync(Album("Le Désert des dunes", sparrowhawk, 1, 1955) with { Contributions = sparrowhawkCredits });
        // An old-franc value, before the new franc of 1960, and an inherited copy with no amount.
        var dunesEdition = await data.EditionAsync(dunes, Owned(bubbles.Id, bubbles.Collections[0], 1955, AcquisitionMode.Inherited, null) with
        {
            Binding = BindingType.Paperback, Format = EditionFormat.Pocket, Condition = EditionCondition.Average, IsSecondHand = true,
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
        var omega = await data.SeriesAsync(Series("Ωmega", null, null, isComplete: false, Template(null, null, null), [scienceFiction], [],
            [Credit(lund, ContributionRole.Scenarist), Credit(tanabe, ContributionRole.Illustrator)]));
        await data.AlbumAsync(Album("Point zéro", omega, 1, 2024));

        // A one-shot in two editions: the first one with every type of visual, two covers among them; the
        // second one with an ISBN whose check digit is wrong, as printed by some publishers.
        var lighthouse = await data.AlbumAsync(OneShot(MultiEditionOneShotTitle, 1989, 11, [fantasy], [greyPort],
            [Credit(quintin, ContributionRole.Scenarist), Credit(quintin, ContributionRole.Illustrator)]) with
        {
            Rating = AlbumRating.VeryGood, PersonalNotes = "Exemplaire dédicacé lors d'un festival.",
        });
        var lighthouseFirst = await data.EditionAsync(lighthouse, Owned(blackPlate.Id, null, 1989, AcquisitionMode.Purchase, new DateOnly(1990, 2, 14)) with
        {
            Isbn = Isbn("978290512302"), Category = EditionCategory.FirstEdition, IsDedicated = true, PageCount = 62,
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

        // A collective album, many authors credited, under `@`, won in a contest.
        var collective = await data.AlbumAsync(OneShot(CollectiveTitle, 2019, null, [humour], [],
        [
            Credit(ganier, ContributionRole.Scenarist), Credit(keita, ContributionRole.Scenarist), Credit(rambert, ContributionRole.Illustrator),
            Credit(ferraz, ContributionRole.Illustrator), Credit(benali, ContributionRole.Illustrator), Credit(tanabe, ContributionRole.Illustrator),
            Credit(lemaire, ContributionRole.Colorist),
        ]));
        var collectiveEdition = await data.EditionAsync(collective, Owned(bubbles.Id, null, 2019, AcquisitionMode.Won, new DateOnly(2019, 10, 31)) with
        {
            AcquisitionAmount = 15m, AcquisitionCurrency = "EUR",
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

        // Wished for as a whole, with no edition known yet.
        var convoy = await data.AlbumAsync(OneShot("Le Dernier Convoi", 2025, 3, [western], [wildWest], [Credit(rambert, ContributionRole.Illustrator), Credit(ganier, ContributionRole.Scenarist)]));
        await data.IntentOnAlbumAsync(convoy);
    }

    private static async Task LoadLongSeriesAsync(
        Loading data, Publisher lagoon, Publisher bubbles, Guid[] genres, Guid universe, Guid specialIssueUniverse, ContributionContent[] credits)
    {
        var series = await data.SeriesAsync(Series(LongSeriesTitle, SeriesStatus.InProgress, 30, isComplete: false,
            Template(lagoon.Id, lagoon.Collections[0], EditionFormat.Standard, BindingType.Hardcover), genres, [universe], credits) with
        {
            Summary = "Dans la cité d'Ombrelune, les veilleurs gardent les lanternes qui retiennent la marée des songes.",
            PersonalNotes = "Collection commencée en 1999.",
        });

        for (var volume = 1; volume <= LongSeriesVolumeTitles.Length; volume++)
        {
            // Missing from the catalogue: the gaps of the series.
            if (volume is 13 or 20)
                continue;

            // The last volume is announced, to be published.
            var year = volume == LongSeriesVolumeTitles.Length ? DateTime.UtcNow.Year + 1 : 1998 + volume;
            var album = await data.AlbumAsync(Album(LongSeriesVolumeTitles[volume - 1] is { Length: > 0 } title ? title : null, series, volume, year,
                month: volume % 3 == 0 ? 1 + volume % 12 : null, rating: volume % 4 == 0 ? null : (AlbumRating)(1 + volume % 5)));

            switch (volume)
            {
                // Catalogued, neither owned nor wished for.
                case 22:
                    continue;
                // Announced: wished for as a whole.
                case 24:
                    await data.IntentOnAlbumAsync(album);
                    continue;
            }

            var acquiredOn = new DateOnly(year, 1 + volume % 12, 1 + volume % 27);
            var edition = await data.EditionAsync(album, Owned(lagoon.Id, lagoon.Collections[0], year, volume == 5 ? AcquisitionMode.Gift : volume == 9 ? AcquisitionMode.Trade : AcquisitionMode.Purchase, acquiredOn) with
            {
                Isbn = Isbn($"9791091234{volume:D2}"), PageCount = 48 + volume % 3 * 8, IsSecondHand = volume % 6 == 0,
                Category = volume == 1 ? EditionCategory.FirstEdition : null,
                AcquisitionAmount = year < 2002 ? 59m + volume : 9.50m + volume / 2m, AcquisitionCurrency = year < 2002 ? "FRF" : "EUR",
            });

            // Owned without cover.
            if (volume is not (10 or 11 or 12))
                await data.VisualAsync(album, edition, VisualType.Cover, Images.Portrait(volume / 24d));

            // Owned, and wished for again in its limited edition.
            if (volume == 23)
            {
                await data.IntentOnEditionAsync(album, new PurchaseIntentEditionContent(lagoon.Id, lagoon.Collections[0], 2022, Isbn("979109123450"),
                    BindingType.Hardcover, BookOrientation.Portrait, ReadingDirection.LeftToRight, EditionFormat.Large, 96, EditionCategory.LimitedEdition, IsColor: true));
            }
        }

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
            Format = EditionFormat.Special, IsColor = false, AcquisitionAmount = 22m, AcquisitionCurrency = "EUR",
        });
        await data.VisualAsync(sketchbook, sketchbookEdition, VisualType.Cover, Images.Portrait(0.70));
        var tales = await data.AlbumAsync(Album("Les Contes de la Cité", series, null, 2020) with { Type = AlbumType.Omnibus, IsSpecialIssue = true });
        await data.EditionAsync(tales, Owned(lagoon.Id, lagoon.Collections[0], 2020, AcquisitionMode.Purchase, new DateOnly(2020, 12, 5)) with
        {
            AcquisitionAmount = 35m, AcquisitionCurrency = "EUR",
        });
    }

    private static ContributionContent Credit(Guid author, ContributionRole role) => new(author, role);

    private static SeriesEditionTemplate Template(Guid? publisher, Guid? collection, EditionFormat? format, BindingType? binding = null) =>
        new(publisher, collection, null, EditionCondition.Excellent, binding, BookOrientation.Portrait, ReadingDirection.LeftToRight, format, true);

    private static SeriesContent Series(
        string title, SeriesStatus? status, int? volumeCount, bool isComplete, SeriesEditionTemplate template, Guid[] genres, Guid[] universes,
        ContributionContent[] credits) =>
        new(title, null, status, volumeCount, isComplete, false, false, null, null, template, genres, universes, credits);

    // Without contributions of its own, an album attached to a series gets those of the series.
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
    private sealed class Loading(HttpClient client, CancellationToken cancellationToken)
    {
        public async Task<Guid> GenreAsync(string label) =>
            (await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new(label))).Id;

        public async Task<Guid> UniverseAsync(string name, string? description, Guid? parent) =>
            (await PostAsync<CreateUniverseRequest, UniverseForm>("/admin/universes", new(name, description, parent))).Id;

        public async Task<Publisher> PublisherAsync(string name, string? website, params string[] collections)
        {
            var publisher = await PostAsync<CreatePublisherRequest, PublisherForm>("/admin/publishers", new(name, website));
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

        public async Task<AlbumWrite> AlbumAsync(AlbumContent content)
        {
            var album = await PostAsync<AlbumContent, AlbumForm>("/admin/albums", content);
            return new AlbumWrite(album.Id, album.Version);
        }

        public async Task<Guid> EditionAsync(AlbumWrite album, EditionContent content)
        {
            var edition = await PostAsync<CreateEditionRequest, EditionForm>($"/admin/albums/{album.Id}/editions", new(content, album.Version));
            album.Version = edition.AlbumVersion;
            return edition.Id;
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
