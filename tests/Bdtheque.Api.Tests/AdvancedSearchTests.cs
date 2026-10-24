using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Common;
using SkiaSharp;
using static Bdtheque.Contracts.Search.SearchComparison;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The advanced search of the consultation (<c>/catalog/search/…</c>): criteria on the data of each entity,
/// links to related records, combinations, and the description of what can be queried.
/// </summary>
/// <remarks>
/// The search covers the whole base, shared by the tests of the class: each test names its records with a
/// marker of its own, and narrows every search to it.
/// </remarks>
public sealed class AdvancedSearchTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public AdvancedSearchTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Schema_LeavesOutTheMediaReferenceOfAVisual()
    {
        // A technical datum, meaningless to the user (fonctionnel/recherche-avancee.md § Données interrogées).
        var schema = (await _client.GetFromJsonAsync<SearchSchema>("/catalog/search/schema"))!;

        var visual = schema.Entities.Single(e => e.Entity == EntityKind.EditionVisual);
        Assert.Equal(["createdAt", "displayOrder", "modifiedAt", "record", "type"], visual.Fields.Select(f => f.Key).Order());
    }

    [Fact]
    public async Task Schema_DescribesEveryEntityWithItsDataAndLinks()
    {
        var schema = (await _client.GetFromJsonAsync<SearchSchema>("/catalog/search/schema"))!;

        // Every entity but the saved searches themselves (fonctionnel/recherche-avancee.md § Entités recherchées).
        Assert.Equal(
            Enum.GetValues<EntityKind>().Where(k => k != EntityKind.SavedSearch).Order(), schema.Entities.Select(e => e.Entity).Order());
        foreach (var entity in schema.Entities)
        {
            Assert.Equal(entity.Fields.Count, entity.Fields.Select(f => f.Key).Distinct().Count());
            Assert.Equal(entity.Links.Count, entity.Links.Select(l => l.Key).Distinct().Count());
            Assert.Contains(entity.Fields, f => f is { Key: "record", Type: SearchDataType.Record } && f.Record == entity.Entity);
            Assert.Contains(entity.Fields, f => f is { Key: "createdAt", Type: SearchDataType.Date });
            Assert.Contains(entity.Fields, f => f is { Key: "modifiedAt", Type: SearchDataType.Date });
            foreach (var field in entity.Fields)
            {
                Assert.Equal(field.IsOptional, field.Comparisons.Contains(IsSet));
                Assert.Equal(field.IsOptional, field.Comparisons.Contains(IsNotSet));
                Assert.Equal(field.Type == SearchDataType.Enumeration, field.Members is { Count: > 0 });
                Assert.Equal(field.Type == SearchDataType.Date, field.DatePrecision is not null);
            }
        }

        var album = schema.Entities.Single(e => e.Entity == EntityKind.Album);
        var rating = album.Fields.Single(f => f.Key == "rating");
        Assert.Equal(
            new SearchFieldDescription("rating", SearchDataType.Enumeration, true, [Is, IsNot, IsOneOf, IsNoneOf, IsSet, IsNotSet], [Is, IsNot], null,
                nameof(AlbumRating), Enum.GetNames<AlbumRating>()),
            rating with { Comparisons = rating.Comparisons.ToList(), Members = rating.Members!.ToList() },
            new RecordComparer());
        Assert.Contains(new SearchLinkDescription("editions", EntityKind.Edition, true), album.Links);
        Assert.Contains(new SearchLinkDescription("series", EntityKind.Series, false), album.Links);
        var acquisitionPrice = schema.Entities.Single(e => e.Entity == EntityKind.Edition).Fields.Single(f => f.Key == "acquisitionPrice");
        Assert.Equal([IsSet, IsNotSet], acquisitionPrice.Comparisons);
        Assert.Empty(acquisitionPrice.DataComparisons);
    }

    /// <summary>
    /// To another data, a data is compared by the comparisons of its type that need a single value: neither
    /// "between", nor "is one of", nor "is set".
    /// </summary>
    [Fact]
    public async Task Schema_DescribesTheComparisonsOfEachDataToAnother()
    {
        var schema = (await _client.GetFromJsonAsync<SearchSchema>("/catalog/search/schema"))!;

        foreach (var field in schema.Entities.SelectMany(e => e.Fields))
        {
            SearchComparison[] expected = field.Type switch
            {
                SearchDataType.Text => [Contains, DoesNotContain, StartsWith, EqualTo, NotEqualTo],
                SearchDataType.Number or SearchDataType.Date => [EqualTo, NotEqualTo, LessThan, LessThanOrEqualTo, GreaterThan, GreaterThanOrEqualTo],
                SearchDataType.Boolean or SearchDataType.Enumeration or SearchDataType.Record => [Is, IsNot],
                _ => [],
            };
            Assert.Equal(expected, field.DataComparisons);
        }
    }

    /// <summary>
    /// Every data of every entity, with every comparison it allows, and every link with every quantifier,
    /// is translated into a query PostgreSQL runs.
    /// </summary>
    [Fact]
    public async Task EveryDataAndLink_IsTranslatedIntoAQuery()
    {
        var schema = (await _client.GetFromJsonAsync<SearchSchema>("/catalog/search/schema"))!;

        foreach (var entity in schema.Entities)
        {
            foreach (var field in entity.Fields)
            {
                foreach (var comparison in field.Comparisons)
                {
                    var criterion = new SearchCriterion(field.Key, comparison, SampleValues(field, comparison));
                    var response = await PostSearchAsync(entity.Entity, criterion);
                    Assert.True(response.IsSuccessStatusCode, $"{entity.Entity}.{field.Key} {comparison}: {response.StatusCode}");
                }

                foreach (var comparison in field.DataComparisons)
                {
                    var precision = field.Type == SearchDataType.Date ? SearchDatePrecision.Day : (SearchDatePrecision?)null;
                    var response = await PostSearchAsync(entity.Entity, Data(field.Key, comparison, new SearchOperand(field.Key), precision));
                    Assert.True(response.IsSuccessStatusCode, $"{entity.Entity}.{field.Key} {comparison} {field.Key}: {response.StatusCode}");
                }
            }

            foreach (var link in entity.Links)
            {
                SearchQuantifier?[] quantifiers = link.IsMultiple
                    ? [new(SearchQuantifierKind.Any), new(SearchQuantifierKind.None), new(SearchQuantifierKind.All), new(SearchQuantifierKind.Count, GreaterThan, ["1"])]
                    : [null];
                foreach (var quantifier in quantifiers)
                {
                    var response = await PostSearchAsync(entity.Entity, new SearchLink(link.Key, new SearchCriterion("createdAt", LessThan, ["9999"]), quantifier));
                    Assert.True(response.IsSuccessStatusCode, $"{entity.Entity}.{link.Key} {quantifier?.Kind}: {response.StatusCode}");
                }
            }
        }
    }

    [Fact]
    public async Task Text_IsComparedIgnoringCaseAndAccentsUnlessTheCriterionAsksOtherwise()
    {
        var epee = await CreateAlbumAsync($"L'Épée {_marker}");
        var lower = await CreateAlbumAsync($"l'epee {_marker}");
        var other = await CreateAlbumAsync($"Le Sabre {_marker}");

        AssertIds([epee.Id, lower.Id], await AlbumIdsAsync(Text("title", Contains, $"EPEE {_marker}")));
        AssertIds([epee.Id], await AlbumIdsAsync(new SearchCriterion("title", Contains, [$"Épée {_marker}"], MatchCaseAndAccents: true)));
        AssertIds([epee.Id, lower.Id], await AlbumIdsAsync(Text("title", StartsWith, "l'EPÉE")));
        AssertIds([lower.Id], await AlbumIdsAsync(new SearchCriterion("title", EqualTo, [$"l'epee {_marker}"], MatchCaseAndAccents: true)));
        AssertIds([epee.Id, lower.Id], await AlbumIdsAsync(Text("title", EqualTo, $"L'EPEE {_marker}")));
        AssertIds([other.Id], await AlbumIdsAsync(Text("title", DoesNotContain, "epee")));
        AssertIds([other.Id], await AlbumIdsAsync(Text("title", NotEqualTo, $"l'épée {_marker}")));
    }

    [Fact]
    public async Task Text_LooksForTheWildcardsOfLikeAsTheyAre()
    {
        var percent = await CreateAlbumAsync($"{_marker} 100%");
        await CreateAlbumAsync($"{_marker} 1000");

        AssertIds([percent.Id], await AlbumIdsAsync(Text("title", Contains, "100%")));
    }

    [Fact]
    public async Task AbsentData_SatisfiesNoComparisonButIsNotSet_WhereasNotKeepsWhatTheCriterionDoesNotEstablish()
    {
        var good = await CreateAlbumAsync($"Bon {_marker}", rating: AlbumRating.Good);
        var poor = await CreateAlbumAsync($"Mauvais {_marker}", rating: AlbumRating.Poor);
        var unrated = await CreateAlbumAsync($"Sans note {_marker}");

        AssertIds([poor.Id], await AlbumIdsAsync(Text("rating", IsNot, "Good")));
        AssertIds([poor.Id, unrated.Id], await AlbumIdsAsync(new SearchNot(Text("rating", Is, "Good"))));
        AssertIds([unrated.Id], await AlbumIdsAsync(new SearchCriterion("rating", IsNotSet)));
        AssertIds([good.Id, poor.Id], await AlbumIdsAsync(Text("rating", IsOneOf, "Good", "Poor")));
        AssertIds([good.Id], await AlbumIdsAsync(Text("rating", IsNoneOf, "Poor", "VeryPoor")));
        AssertIds([good.Id, poor.Id], await AlbumIdsAsync(Text("rating", IsSet)));
    }

    /// <summary>
    /// The values of "is one of" form a single parameter, whatever their number: one parameter per value
    /// would exceed the 65,535 parameters PostgreSQL accepts in a query.
    /// </summary>
    [Fact]
    public async Task IsOneOf_SendsItsValuesAsASingleParameter()
    {
        var album = await CreateAlbumAsync($"Album {_marker}");
        var values = Enumerable.Range(0, 70_000).Select(_ => Guid.NewGuid().ToString()).Append(album.Id.ToString()).ToArray();

        AssertIds([album.Id], await AlbumIdsAsync(Text("record", IsOneOf, values)));
    }

    [Fact]
    public async Task PartialDate_SatisfiesACriterionOnlyIfItsPrecisionEstablishesIt()
    {
        var march2020 = await CreateAlbumAsync($"Mars 2020 {_marker}", publication: (2020, 3));
        var year2020 = await CreateAlbumAsync($"2020 {_marker}", publication: (2020, null));
        var december2019 = await CreateAlbumAsync($"Décembre 2019 {_marker}", publication: (2019, 12));
        var unknown = await CreateAlbumAsync($"Inconnue {_marker}");

        AssertIds([march2020.Id], await AlbumIdsAsync(Text("firstPublicationDate", EqualTo, "2020-03")));
        AssertIds([march2020.Id, year2020.Id], await AlbumIdsAsync(Text("firstPublicationDate", EqualTo, "2020")));
        AssertIds([december2019.Id], await AlbumIdsAsync(Text("firstPublicationDate", NotEqualTo, "2020-03")));
        AssertIds(
            [year2020.Id, december2019.Id, unknown.Id],
            await AlbumIdsAsync(new SearchNot(Text("firstPublicationDate", EqualTo, "2020-03"))));
        AssertIds([december2019.Id], await AlbumIdsAsync(Text("firstPublicationDate", LessThan, "2020")));
        AssertIds([march2020.Id, december2019.Id], await AlbumIdsAsync(Text("firstPublicationDate", LessThanOrEqualTo, "2020-03-31")));
        AssertIds([march2020.Id], await AlbumIdsAsync(Text("firstPublicationDate", GreaterThan, "2020-02")));
        AssertIds([march2020.Id, year2020.Id], await AlbumIdsAsync(Text("firstPublicationDate", GreaterThanOrEqualTo, "2020-01-01")));
        AssertIds([march2020.Id, december2019.Id], await AlbumIdsAsync(Text("firstPublicationDate", Between, "2019-12", "2020-03")));
        AssertIds([unknown.Id], await AlbumIdsAsync(Text("firstPublicationDate", IsNotSet)));
    }

    [Fact]
    public async Task Dates_AreComparedAtThePrecisionChosen()
    {
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        var album = await CreateAlbumAsync($"Album {_marker}");
        // The last minute of a year: the editions are created in it, and not in the next one.
        _factory.Clock.UtcNow = new DateTimeOffset(2025, 12, 31, 23, 59, 0, TimeSpan.Zero);
        EditionForm june, july;
        try
        {
            june = await CreateEditionAsync(album.Id, publisher.Id, publicationYear: 1985, acquisitionDate: new DateOnly(2021, 6, 15));
            july = await CreateEditionAsync(album.Id, publisher.Id, publicationYear: 2001, acquisitionDate: new DateOnly(2021, 7, 1));
        }
        finally
        {
            _factory.Clock.UtcNow = null;
        }

        var mine = new SearchLink("publisher", Text("name", Contains, _marker));
        AssertIds([june.Id], await EditionIdsAsync(And(mine, Text("acquisitionDate", EqualTo, "2021-06"))));
        AssertIds([june.Id, july.Id], await EditionIdsAsync(And(mine, Text("acquisitionDate", EqualTo, "2021"))));
        AssertIds([july.Id], await EditionIdsAsync(And(mine, Text("acquisitionDate", GreaterThan, "2021-06-15"))));
        AssertIds([june.Id], await EditionIdsAsync(And(mine, Text("publicationYear", EqualTo, "1985"))));
        Assert.Empty(await EditionIdsAsync(And(mine, Text("publicationYear", EqualTo, "1985-01"))));
        AssertIds([june.Id, july.Id], await EditionIdsAsync(And(mine, Text("createdAt", EqualTo, "2025"))));
        Assert.Empty(await EditionIdsAsync(And(mine, Text("createdAt", EqualTo, "2026"))));
    }

    [Fact]
    public async Task Dates_AreComparedToEachOtherAtThePrecisionChosen_EachCoveringTheWholePeriodItIsKnownTo()
    {
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        var album = await CreateAlbumAsync($"Album {_marker}", publication: (2000, 6));
        var reprint = await CreateEditionAsync(album.Id, publisher.Id, publicationYear: 2010, acquisitionDate: new DateOnly(2009, 12, 24));
        var sameYear = await CreateEditionAsync(album.Id, publisher.Id, publicationYear: 2010, acquisitionDate: new DateOnly(2010, 3, 1));
        var original = await CreateEditionAsync(album.Id, publisher.Id, publicationYear: 2000, acquisitionDate: new DateOnly(2000, 6, 30));
        var notAcquiredOn = await CreateEditionAsync(album.Id, publisher.Id, publicationYear: 2000);

        var mine = new SearchLink("publisher", Text("name", Contains, _marker));
        var acquisition = new SearchOperand("acquisitionDate");
        var albumPublication = new SearchOperand("firstPublicationDate", ["album"]);

        // fonctionnel/recherche-avancee.md § Comparaisons: editions published after their acquisition, or before the first publication of their album.
        AssertIds([reprint.Id], await EditionIdsAsync(And(mine, Data("publicationYear", GreaterThan, acquisition, SearchDatePrecision.Year))));
        AssertIds([sameYear.Id, original.Id], await EditionIdsAsync(And(mine, Data("publicationYear", EqualTo, acquisition, SearchDatePrecision.Year))));
        AssertIds([original.Id, notAcquiredOn.Id], await EditionIdsAsync(And(mine, Data("publicationYear", LessThanOrEqualTo, albumPublication, SearchDatePrecision.Year))));
        Assert.Empty(await EditionIdsAsync(And(mine, Data("publicationYear", LessThan, albumPublication, SearchDatePrecision.Year))));

        // At the month, a year covers its twelve months: no month of 2010 is established equal to, before or after March 2010.
        Assert.Empty(await EditionIdsAsync(And(mine, Data("publicationYear", EqualTo, acquisition, SearchDatePrecision.Month))));
        AssertIds([reprint.Id], await EditionIdsAsync(And(mine, Data("publicationYear", GreaterThan, acquisition, SearchDatePrecision.Month))));
        AssertIds([reprint.Id], await EditionIdsAsync(And(mine, Data("publicationYear", NotEqualTo, acquisition, SearchDatePrecision.Year))));
        AssertIds(
            [sameYear.Id, original.Id, notAcquiredOn.Id],
            await EditionIdsAsync(And(mine, new SearchNot(Data("publicationYear", GreaterThan, acquisition, SearchDatePrecision.Month)))));
        AssertIds(
            [sameYear.Id, original.Id],
            await EditionIdsAsync(And(mine, Data("acquisitionDate", GreaterThanOrEqualTo, new SearchOperand("publicationYear"), SearchDatePrecision.Year))));
    }

    [Fact]
    public async Task Data_OfEachType_IsComparedToAnotherOfTheSameType_OfTheRecordOrOfARecordItLeadsTo()
    {
        var series = await CreateSeriesAsync($"Série {_marker} 1_0");
        var same = await CreateAlbumAsync($"SERIE {_marker} 1_0", series.Id, volumeNumber: 1);
        var sequel = await CreateAlbumAsync($"Série {_marker} 1_0 : la suite", series.Id, volumeNumber: 1);
        var other = await CreateAlbumAsync($"Série {_marker} 1x0", series.Id, volumeNumber: 2);
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        await CreateEditionAsync(sequel.Id, publisher.Id);
        var seriesTitle = new SearchOperand("title", ["series"]);

        AssertIds([same.Id], await AlbumIdsAsync(Data("title", EqualTo, seriesTitle)));
        Assert.Empty(await AlbumIdsAsync(new SearchDataComparison("title", EqualTo, seriesTitle, MatchCaseAndAccents: true)));
        AssertIds([same.Id, sequel.Id], await AlbumIdsAsync(Data("title", StartsWith, seriesTitle)));
        // The wildcards of LIKE in the other data are looked for as they are: "1x0" neither starts with nor contains "1_0".
        AssertIds([other.Id], await AlbumIdsAsync(Data("title", DoesNotContain, seriesTitle)));
        AssertIds([same.Id, other.Id], await AlbumIdsAsync(Data("volumeNumber", GreaterThan, new SearchOperand("editionCount"))));
        AssertIds([same.Id, sequel.Id, other.Id], await AlbumIdsAsync(Data("manualSortKey", Is, new SearchOperand("manualSortKey", ["series"]))));
        Assert.Empty(await AlbumIdsAsync(Data("record", IsNot, new SearchOperand("record"))));
    }

    [Fact]
    public async Task OtherData_IsReachedThroughALinkToAtMostOneRecordWithoutNavigation()
    {
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        await CreateIntentAsync((await CreateAlbumAsync($"Intention {_marker}")).Id, publisher.Id);
        var album = await CreateAlbumAsync($"Sans intention {_marker}");
        await CreateEditionAsync(album.Id, publisher.Id);
        var mine = new SearchLink("publisher", Text("name", Contains, _marker));
        var intended = Assert.Single(await EditionIdsAsync(And(mine, new SearchLink("purchaseIntent"))));

        AssertIds([intended], await EditionIdsAsync(And(mine, Data("record", Is, new SearchOperand("record", ["purchaseIntent", "edition"])))));
    }

    [Fact]
    public async Task Link_ToSeveralRecords_IsSatisfiedByAtLeastOneNoneAllOrANumberOfThem()
    {
        var dargaud = await CreatePublisherAsync($"Dargaud {_marker}");
        var other = await CreatePublisherAsync($"Autre {_marker}");
        var sameEdition = await CreateAlbumAsync($"Même édition {_marker}");
        await CreateEditionAsync(sameEdition.Id, dargaud.Id, publicationYear: 2000);
        var twoEditions = await CreateAlbumAsync($"Deux éditions {_marker}");
        await CreateEditionAsync(twoEditions.Id, dargaud.Id, publicationYear: 1990);
        await CreateEditionAsync(twoEditions.Id, other.Id, publicationYear: 2000);
        var noEdition = await CreateAlbumAsync($"Sans édition {_marker}");

        var atDargaud = new SearchLink("publisher", Text("name", StartsWith, "Dargaud"));
        var in2000 = Text("publicationYear", EqualTo, "2000");

        // fonctionnel/recherche-avancee.md § Liens entre entités: one edition satisfying both, or each by a different edition.
        AssertIds([sameEdition.Id], await AlbumIdsAsync(Editions(SearchQuantifierKind.Any, And(atDargaud, in2000))));
        AssertIds(
            [twoEditions.Id, sameEdition.Id],
            await AlbumIdsAsync(And(Editions(SearchQuantifierKind.Any, atDargaud), Editions(SearchQuantifierKind.Any, in2000))));
        AssertIds([noEdition.Id], await AlbumIdsAsync(Editions(SearchQuantifierKind.None, null)));
        AssertIds([sameEdition.Id], await AlbumIdsAsync(Editions(SearchQuantifierKind.All, atDargaud)));
        AssertIds([noEdition.Id], await AlbumIdsAsync(new SearchNot(Editions(SearchQuantifierKind.Any, null))));
        AssertIds(
            [twoEditions.Id],
            await AlbumIdsAsync(new SearchLink("editions", null, new SearchQuantifier(SearchQuantifierKind.Count, GreaterThan, ["1"]))));
        AssertIds([twoEditions.Id], await AlbumIdsAsync(Text("editionCount", EqualTo, "2")));
    }

    [Fact]
    public async Task Path_OfSeveralLinks_CarriesAQuantifierOnEachLinkToSeveralRecords()
    {
        var dargaud = await CreatePublisherAsync($"Dargaud {_marker}");
        var other = await CreatePublisherAsync($"Autre {_marker}");
        var allAtDargaud = await CreateSeriesAsync($"Toute chez Dargaud {_marker}");
        await CreateEditionAsync((await CreateAlbumAsync(null, allAtDargaud.Id, volumeNumber: 1)).Id, dargaud.Id);
        var second = await CreateAlbumAsync(null, allAtDargaud.Id, volumeNumber: 2);
        await CreateEditionAsync(second.Id, other.Id);
        await CreateEditionAsync(second.Id, dargaud.Id);
        var partly = await CreateSeriesAsync($"En partie {_marker}");
        await CreateEditionAsync((await CreateAlbumAsync(null, partly.Id, volumeNumber: 1)).Id, dargaud.Id);
        await CreateAlbumAsync(null, partly.Id, volumeNumber: 2);

        // fonctionnel/recherche-avancee.md § Liens entre entités: series all of whose albums have at least one edition at Dargaud.
        var criteria = And(
            Text("title", Contains, _marker),
            new SearchLink("albums", Editions(SearchQuantifierKind.Any, new SearchLink("publisher", Text("name", StartsWith, "Dargaud"))),
                new SearchQuantifier(SearchQuantifierKind.All)));

        AssertIds([allAtDargaud.Id], Ids(await SearchAsync<SeriesListItem>(EntityKind.Series, criteria)));
    }

    [Fact]
    public async Task Count_OfRelatedRecords_IsComparedAsAnyNumber()
    {
        var first = await CreateAuthorAsync($"Premier {_marker}");
        var second = await CreateAuthorAsync($"Second {_marker}");
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        var twoIllustrators = await CreateAlbumAsync(
            $"Deux dessinateurs {_marker}",
            contributions: [new(first.Id, ContributionRole.Illustrator), new(second.Id, ContributionRole.Illustrator)]);
        var oneIllustrator = await CreateAlbumAsync(
            $"Un dessinateur {_marker}",
            contributions: [new(first.Id, ContributionRole.Illustrator), new(second.Id, ContributionRole.Scenarist)]);
        var edition = await CreateEditionAsync(twoIllustrators.Id, publisher.Id);
        await CreateEditionAsync(oneIllustrator.Id, publisher.Id);

        // fonctionnel/recherche-avancee.md § Liens entre entités: editions whose album has more than one illustrator.
        var criteria = And(
            new SearchLink("publisher", Text("name", Contains, _marker)),
            new SearchLink("album", new SearchLink(
                "contributions", Text("role", Is, nameof(ContributionRole.Illustrator)),
                new SearchQuantifier(SearchQuantifierKind.Count, GreaterThan, ["1"]))));

        AssertIds([edition.Id], await EditionIdsAsync(criteria));
    }

    [Fact]
    public async Task Universe_CoversItsSubUniverses_AndAnAlbumShowsTheGenresAndUniversesOfItsSeries()
    {
        var parent = await CreateUniverseAsync($"Parent {_marker}", null);
        var child = await CreateUniverseAsync($"Enfant {_marker}", parent.Id);
        var grandChild = await CreateUniverseAsync($"Petit-enfant {_marker}", child.Id);
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        var series = await CreateSeriesAsync($"Série {_marker}", genreIds: [genre.Id], universeIds: [grandChild.Id]);
        var inSeries = await CreateAlbumAsync($"Dans la série {_marker}", series.Id, volumeNumber: 1);
        var own = await CreateAlbumAsync($"Album {_marker}", universeIds: [child.Id]);

        var inParent = Text("record", Is, parent.Id.ToString());
        AssertIds([own.Id], await AlbumIdsAsync(Universes("universes", inParent)));
        AssertIds([own.Id, inSeries.Id], await AlbumIdsAsync(Universes("displayedUniverses", inParent)));
        AssertIds([own.Id], await AlbumIdsAsync(Universes("universes", Text("name", StartsWith, "Parent"))));
        AssertIds(
            [inSeries.Id],
            await AlbumIdsAsync(new SearchLink("displayedGenres", Text("record", Is, genre.Id.ToString()), new SearchQuantifier(SearchQuantifierKind.Any))));
        Assert.Empty(await AlbumIdsAsync(new SearchLink("genres", null, new SearchQuantifier(SearchQuantifierKind.Any))));

        var universes = await SearchAsync<UniverseListItem>(
            EntityKind.Universe,
            And(Text("name", Contains, _marker), new SearchLink("albums", null, new SearchQuantifier(SearchQuantifierKind.Any))));
        // The albums attached to the universe or a descendant themselves: those of a series through the series.
        AssertIds([child.Id, parent.Id], Ids(universes));
        var throughSeries = await SearchAsync<UniverseListItem>(
            EntityKind.Universe,
            And(Text("name", Contains, _marker), new SearchLink("series", new SearchLink("albums", null, new SearchQuantifier(SearchQuantifierKind.Any)), new SearchQuantifier(SearchQuantifierKind.Any))));
        AssertIds([grandChild.Id, child.Id, parent.Id], Ids(throughSeries));

        var anyAlbum = new SearchQuantifier(SearchQuantifierKind.Any);
        Assert.Empty(await SearchAsync<GenreListItem>(EntityKind.Genre, And(Text("record", Is, genre.Id.ToString()), new SearchLink("albums", null, anyAlbum))));
        Assert.Single(await SearchAsync<GenreListItem>(
            EntityKind.Genre, And(Text("record", Is, genre.Id.ToString()), new SearchLink("series", new SearchLink("albums", null, anyAlbum), anyAlbum))));
    }

    [Fact]
    public async Task PurchaseIntent_LinksTheAlbumOnlyWhenItTargetsTheAlbumItself()
    {
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        var onAlbum = await CreateAlbumAsync($"Intention album {_marker}");
        var albumIntent = await CreateIntentAsync(onAlbum.Id, null);
        var onEdition = await CreateAlbumAsync($"Intention édition {_marker}");
        var editionIntent = await CreateIntentAsync(onEdition.Id, publisher.Id);

        // modele-metier.md § Intention d'achat — relations: an intent targets an album or an edition, exclusively.
        AssertIds([onAlbum.Id], await AlbumIdsAsync(new SearchLink("purchaseIntent")));
        AssertIds(
            [onEdition.Id],
            await AlbumIdsAsync(new SearchLink("editions", new SearchLink("purchaseIntent"), new SearchQuantifier(SearchQuantifierKind.Any))));

        var mine = new SearchLink("album", Text("title", Contains, _marker));
        var viaEdition = new SearchLink("edition", new SearchLink("album", Text("title", Contains, _marker)));
        AssertIds([albumIntent], Ids(await SearchAsync<PurchaseIntentListItem>(EntityKind.PurchaseIntent, mine)));
        AssertIds([editionIntent], Ids(await SearchAsync<PurchaseIntentListItem>(EntityKind.PurchaseIntent, viaEdition)));
    }

    [Fact]
    public async Task DeducedData_AreQueried()
    {
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        var series = await CreateSeriesAsync($"Zorglub {_marker}");
        var untitled = await CreateAlbumAsync(null, series.Id, volumeNumber: 1);
        var owned = await CreateAlbumAsync($"Possédé {_marker}");
        await CreateEditionAsync(owned.Id, publisher.Id);

        AssertIds([owned.Id], await AlbumIdsAsync(Text("inCollection", Is, "true")));
        AssertIds([untitled.Id], await AlbumIdsAsync(Text("initial", EqualTo, "Z")));
        AssertIds([untitled.Id], await AlbumIdsAsync(Text("sortKey", StartsWith, "zorglub")));
    }

    [Fact]
    public async Task Isbn_IsComparedWhateverItsSeparators()
    {
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}");
        var album = await CreateAlbumAsync($"Album {_marker}");
        var edition = await CreateEditionAsync(album.Id, publisher.Id, isbn: "978-2-205-01234-5");

        AssertIds(
            [edition.Id],
            await EditionIdsAsync(And(new SearchLink("publisher", Text("name", Contains, _marker)), Text("isbn", EqualTo, "978 2205 012345"))));
    }

    [Fact]
    public async Task Groups_CombineByAndOrAndNotAtAnyLevel()
    {
        var a = await CreateAlbumAsync($"Alpha {_marker}", rating: AlbumRating.Good, publication: (2000, null));
        var b = await CreateAlbumAsync($"Bêta {_marker}", rating: AlbumRating.Poor, publication: (2010, null));
        var c = await CreateAlbumAsync($"Gamma {_marker}", publication: (2000, null));

        AssertIds(
            [a.Id, c.Id],
            await AlbumIdsAsync(new SearchGroup(SearchOperator.Or, [Text("title", StartsWith, "alpha"), Text("title", StartsWith, "gamma")])));
        AssertIds(
            [c.Id],
            await AlbumIdsAsync(And(Text("firstPublicationDate", EqualTo, "2000"), new SearchNot(Text("rating", IsSet)))));
        AssertIds([a.Id, b.Id, c.Id], await AlbumIdsAsync(new SearchGroup(SearchOperator.And, [])));
        Assert.Empty(await AlbumIdsAsync(new SearchGroup(SearchOperator.Or, [])));
    }

    [Fact]
    public async Task Results_ArePresentedAsTheConsultationPresentsTheirEntity()
    {
        var author = await CreateAuthorAsync($"Auteur {_marker}");
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}", "https://editeur.example");
        var series = await CreateSeriesAsync($"Série {_marker}", contributions: [new(author.Id, ContributionRole.Scenarist)]);
        var album = await CreateAlbumAsync(null, series.Id, volumeNumber: 1);
        var edition = await CreateEditionAsync(album.Id, publisher.Id);
        var visual = Assert.Single((await UploadCoverAsync(edition)).Visuals);

        var albumSummary = new AlbumSummary(album.Id, null, series.Id, $"Série {_marker}", AlbumType.Regular, false, 1, null, null, true);
        var editionSummary = new EditionSummary(edition.Id, publisher.Id, $"Éditeur {_marker}", null, null, null, null);
        var ofMyEdition = new SearchLink("edition", Text("record", Is, edition.Id.ToString()));

        Assert.Equal(
            [new EditionVisualListItem(visual.Id, VisualType.Cover, albumSummary, editionSummary)],
            await SearchAsync<EditionVisualListItem>(EntityKind.EditionVisual, ofMyEdition));
        Assert.Equal(
            [new EditionListItem(albumSummary, editionSummary, true)],
            await SearchAsync<EditionListItem>(EntityKind.Edition, Text("record", Is, edition.Id.ToString())));
        Assert.Equal(
            [new PublisherListItem(publisher.Id, $"Éditeur {_marker}", "https://editeur.example")],
            await SearchAsync<PublisherListItem>(EntityKind.Publisher, Text("name", Contains, _marker)));

        var contributions = await SearchAsync<ContributionListItem>(
            EntityKind.Contribution, new SearchLink("author", Text("record", Is, author.Id.ToString())));
        var authorItem = new AuthorListItem(author.Id, null, null, $"Auteur {_marker}");
        Assert.Equal(2, contributions.Count);
        Assert.Contains(contributions, c => c.Series == new SeriesListItem(series.Id, $"Série {_marker}") && c.Album is null && c.Author == authorItem);
        Assert.Contains(contributions, c => c.Album == albumSummary && c.Series is null && c.Role == ContributionRole.Scenarist);
    }

    [Fact]
    public async Task MissingValue_IsABusinessError()
    {
        var response = await PostSearchAsync(EntityKind.Album, new SearchCriterion("title", Contains, [""]));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AdvancedSearchValueMissing);
    }

    [Theory]
    [InlineData("""{"kind":"criterion","field":"unknown","comparison":"IsSet"}""")]
    [InlineData("""{"kind":"criterion","field":"title","comparison":"GreaterThan","values":["a"]}""")]
    [InlineData("""{"kind":"criterion","field":"volumeNumber","comparison":"EqualTo","values":["1","2"]}""")]
    [InlineData("""{"kind":"link","link":"editions"}""")]
    [InlineData("""{"kind":"link","link":"series","quantifier":{"kind":"Any"}}""")]
    [InlineData("""{"kind":"dataComparison","field":"title","comparison":"EqualTo","other":{"field":"volumeNumber"}}""")]
    [InlineData("""{"kind":"dataComparison","field":"rating","comparison":"Is","other":{"field":"type"}}""")]
    [InlineData("""{"kind":"dataComparison","field":"record","comparison":"Is","other":{"field":"record","links":["series"]}}""")]
    [InlineData("""{"kind":"dataComparison","field":"title","comparison":"EqualTo","other":{"field":"title","links":["contributions"]}}""")]
    [InlineData("""{"kind":"dataComparison","field":"title","comparison":"EqualTo","other":{"field":"unknown"}}""")]
    [InlineData("""{"kind":"dataComparison","field":"volumeNumber","comparison":"Between","other":{"field":"editionCount"}}""")]
    [InlineData("""{"kind":"dataComparison","field":"firstPublicationDate","comparison":"EqualTo","other":{"field":"createdAt"}}""")]
    [InlineData("""{"kind":"dataComparison","field":"firstPublicationDate","comparison":"EqualTo","other":{"field":"createdAt"},"datePrecision":7}""")]
    [InlineData("""{"kind":"dataComparison","field":"title","comparison":"EqualTo","other":{"field":"summary"},"datePrecision":"Year"}""")]
    [InlineData("""{"kind":"dataComparison","field":"volumeNumber","comparison":"EqualTo","other":{"field":"editionCount"},"matchCaseAndAccents":true}""")]
    public async Task MalformedCriteria_AreATechnicalError(string criteria)
    {
        var response = await _client.PostAsync(
            "/catalog/search/albums", new StringContent($$"""{"criteria":{{criteria}}}""", System.Text.Encoding.UTF8, "application/json"));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private static IReadOnlyList<string>? SampleValues(SearchFieldDescription field, SearchComparison comparison)
    {
        string value = field.Type switch
        {
            SearchDataType.Text => "x",
            SearchDataType.Number => "1",
            SearchDataType.Date => "2020-03",
            SearchDataType.Boolean => "true",
            SearchDataType.Enumeration => field.Members![0],
            SearchDataType.Record => Guid.NewGuid().ToString(),
            _ => "",
        };
        return comparison switch
        {
            IsSet or IsNotSet => null,
            Between => field.Type == SearchDataType.Date ? ["2019", "2020-03-15"] : [value, value],
            IsOneOf or IsNoneOf => [value, value],
            _ => [value],
        };
    }

    private static void AssertIds(IEnumerable<Guid> expected, IEnumerable<Guid> actual) => Assert.Equivalent(expected, actual, strict: true);

    private static SearchCriterion Text(string field, SearchComparison comparison, params string[] values) =>
        new(field, comparison, values.Length == 0 ? null : values);

    private static SearchDataComparison Data(string field, SearchComparison comparison, SearchOperand other, SearchDatePrecision? precision = null) =>
        new(field, comparison, other, DatePrecision: precision);

    private static SearchGroup And(params SearchCondition[] conditions) => new(SearchOperator.And, conditions);

    private static SearchLink Editions(SearchQuantifierKind kind, SearchCondition? condition) => new("editions", condition, new SearchQuantifier(kind));

    private static SearchLink Universes(string link, SearchCondition condition) =>
        new(link, condition, new SearchQuantifier(SearchQuantifierKind.Any));

    // The albums of the test: those whose label contains its marker, by their title or that of their series.
    private async Task<IEnumerable<Guid>> AlbumIdsAsync(SearchCondition criteria)
    {
        var mine = new SearchGroup(SearchOperator.Or, [Text("title", Contains, _marker), new SearchLink("series", Text("title", Contains, _marker))]);
        return Ids(await SearchAsync<AlbumSummary>(EntityKind.Album, And(mine, criteria)));
    }

    private async Task<IEnumerable<Guid>> EditionIdsAsync(SearchCondition criteria) =>
        (await SearchAsync<EditionListItem>(EntityKind.Edition, criteria)).Select(e => e.Edition.Id);

    private async Task<IReadOnlyList<T>> SearchAsync<T>(EntityKind entity, SearchCondition criteria)
    {
        var response = await PostSearchAsync(entity, criteria);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Page<T>>())!.Items;
    }

    private Task<HttpResponseMessage> PostSearchAsync(EntityKind entity, SearchCondition criteria) =>
        _client.PostAsJsonAsync($"/catalog/search/{Route(entity)}?pageSize=100", new AdvancedSearchRequest(criteria));

    private static string Route(EntityKind entity) => entity switch
    {
        EntityKind.Album => "albums",
        EntityKind.Series => "series",
        EntityKind.Edition => "editions",
        EntityKind.Author => "authors",
        EntityKind.Publisher => "publishers",
        EntityKind.PublisherCollection => "publisher-collections",
        EntityKind.Genre => "genres",
        EntityKind.Universe => "universes",
        EntityKind.EditionVisual => "edition-visuals",
        EntityKind.PurchaseIntent => "purchase-intents",
        EntityKind.Contribution => "contributions",
        _ => throw new ArgumentOutOfRangeException(nameof(entity)),
    };

    private static IEnumerable<Guid> Ids<T>(IEnumerable<T> items) =>
        items.Select(item => (Guid)typeof(T).GetProperty("Id")!.GetValue(item)!);

    private Task<SeriesForm> CreateSeriesAsync(
        string title, Guid[]? genreIds = null, Guid[]? universeIds = null, ContributionContent[]? contributions = null) =>
        PostAsync<SeriesForm>(
            "/admin/series",
            new SeriesContent(title, null, null, null, false, false, false, null, null, NoTemplate, genreIds ?? [], universeIds ?? [], contributions ?? []));

    private Task<AlbumForm> CreateAlbumAsync(
        string? title, Guid? seriesId = null, int? volumeNumber = null, Guid[]? universeIds = null, AlbumRating? rating = null,
        (int Year, int? Month)? publication = null, ContributionContent[]? contributions = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volumeNumber, null, null, publication?.Year, publication?.Month, null, null, rating,
            [], universeIds ?? [], contributions ?? []));

    private Task<AuthorForm> CreateAuthorAsync(string pseudonym) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(null, null, pseudonym, null, null));

    private Task<PublisherForm> CreatePublisherAsync(string name, string? website = null) =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest(name, website));

    private Task<UniverseForm> CreateUniverseAsync(string name, Guid? parentId) =>
        PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest(name, null, parentId));

    private async Task<EditionForm> CreateEditionAsync(
        Guid albumId, Guid publisherId, int? publicationYear = null, string? isbn = null, DateOnly? acquisitionDate = null) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(
                    publisherId, null, publicationYear, isbn, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Gift, false, acquisitionDate, null, null, false, null, null, null, null),
                (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{albumId}"))!.Version));

    /// <returns>The identifier of the intent: on the whole album without <paramref name="publisherId"/>, on a new edition otherwise.</returns>
    private async Task<Guid> CreateIntentAsync(Guid albumId, Guid? publisherId)
    {
        var edition = publisherId is null
            ? null
            : new PurchaseIntentEditionContent(publisherId, null, null, null, null, null, null, null, null, null, true);
        var intents = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(edition, (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{albumId}"))!.Version));
        return Assert.Single(intents.Intents).Id;
    }

    private async Task<EditionVisualsForm> UploadCoverAsync(EditionForm edition)
    {
        var version = (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{edition.AlbumId}"))!.Version;
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(TestImages.Encode(20, 30, SKEncodedImageFormat.Jpeg)), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent(version.ToString()), UploadVisualFields.AlbumVersion },
        };
        var response = await _client.PostAsync($"/admin/albums/{edition.AlbumId}/editions/{edition.Id}/visuals", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!;
    }

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }

    // Compares the descriptions of a data by value, lists included (a record compares its lists by reference).
    private sealed class RecordComparer : IEqualityComparer<SearchFieldDescription>
    {
        public bool Equals(SearchFieldDescription? x, SearchFieldDescription? y) =>
            x is not null && y is not null
            && x with { Comparisons = [], DataComparisons = [], Members = null } == y with { Comparisons = [], DataComparisons = [], Members = null }
            && x.Comparisons.SequenceEqual(y.Comparisons)
            && x.DataComparisons.SequenceEqual(y.DataComparisons)
            && (x.Members ?? []).SequenceEqual(y.Members ?? []);

        public int GetHashCode(SearchFieldDescription obj) => obj.Key.GetHashCode(StringComparison.Ordinal);
    }
}
