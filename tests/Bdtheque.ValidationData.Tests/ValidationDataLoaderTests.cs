using System.Net.Http.Json;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Search;

namespace Bdtheque.ValidationData.Tests;

/// <summary>
/// The validation data, loaded through the API as the validation environment loads it: the load must
/// keep going through every rule of the API as they evolve, and the data must keep presenting the cases
/// the screens show (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md).
/// </summary>
public sealed class ValidationDataLoaderTests(ValidationDataLoaderTests.LoadedApi api) : IClassFixture<ValidationDataLoaderTests.LoadedApi>
{
    private readonly HttpClient _client = api.CreateApiClient();

    [Fact]
    public async Task AudienceIndicators_OfVisitorsOnEachDevice_AndOfTheAdministrator_WithRepeats()
    {
        async Task<List<Bdtheque.Contracts.Audience.FunctionIndicators>> ReadAsync(string criteria) =>
            (await _client.GetFromJsonAsync<List<Bdtheque.Contracts.Audience.FunctionIndicators>>($"/admin/audience/indicators{criteria}"))!;

        var album = Assert.Single(await ReadAsync(""), function => function.Function == "album");
        // Three records of an album, one of them presented twice at once, over two visits.
        Assert.Equal((4, 2, 1, 2), (album.UsageCount, album.VisitCount, album.RepeatedUsageCount, album.DistinctObjectCount));
        Assert.Contains(await ReadAsync("?administrator=true"), function => function is { Function: "admin.albums.search", RepeatedUsageCount: 1 });
        foreach (var device in Enum.GetValues<DeviceType>())
            Assert.Contains(await ReadAsync($"?administrator=false&deviceType={device}"), function => function.Function == "series");
        foreach (var engine in new[] { RenderingEngine.Blink, RenderingEngine.Gecko, RenderingEngine.WebKit })
            Assert.NotEmpty(await ReadAsync($"?renderingEngine={engine}"));
    }

    [Fact]
    public async Task EditionList_PresentsEveryCategory_OnAtLeastHalfOfTheEditions()
    {
        var list = await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.EditionAdminList>("/admin/editions?pageSize=100");

        // The editions of the collection the list presents, each category among them, and editions without one, at least half
        // of them having one (decision of the user at the functional validation of #333).
        var rows = list!.Editions.Items;
        Assert.Equal(list.Editions.TotalCount, rows.Count);
        Assert.Superset(new HashSet<EditionCategory?>([null, .. Enum.GetValues<EditionCategory>().Cast<EditionCategory?>()]),
            rows.Select(row => row.Category).ToHashSet());
        Assert.True(rows.Count(row => row.Category is not null) * 2 >= rows.Count,
            $"{rows.Count(row => row.Category is not null)} of the {rows.Count} owned editions have a category.");
    }

    [Fact]
    public async Task CatalogAlbumList_PresentsEachPossession_AndACoverThatTheFilterOfThePossessionChanges()
    {
        foreach (var state in Enum.GetValues<Bdtheque.Contracts.Admin.AlbumCollectionState>())
            Assert.NotEmpty((await _client.GetFromJsonAsync<AlbumCatalogList>($"/catalog/albums?state={state}"))!.Albums.Items);

        // Owned without cover, and wished for in an edition with one: its cover among all its editions, the generic visual
        // among the owned ones (fonctionnel/regles-metier/couverture-d-un-album.md).
        var search = $"q={Uri.EscapeDataString("Brouillard sur Ombrelune")}";
        var all = Assert.Single((await _client.GetFromJsonAsync<AlbumCatalogList>($"/catalog/albums?{search}"))!.Albums.Items);
        var owned = Assert.Single((await _client.GetFromJsonAsync<AlbumCatalogList>($"/catalog/albums?{search}&state=InCollection"))!.Albums.Items);
        Assert.NotNull(all.Cover);
        Assert.Null(owned.Cover);
    }

    [Fact]
    public async Task CatalogSeriesList_PresentsEachPossession_AndASeriesOnlyWishedForWithTheCoverOfItsEdition()
    {
        foreach (var state in Enum.GetValues<Bdtheque.Contracts.Admin.AlbumCollectionState>())
            Assert.NotEmpty((await _client.GetFromJsonAsync<SeriesCatalogList>($"/catalog/series?state={state}"))!.Series.Items);

        // Nothing owned, one album wished for in an edition with a cover: « Intention », its pile showing that cover.
        var search = $"q={Uri.EscapeDataString(ValidationDataLoader.IntendedSeriesTitle)}";
        var intended = Assert.Single((await _client.GetFromJsonAsync<SeriesCatalogList>($"/catalog/series?{search}&state=PurchaseIntent"))!.Series.Items);
        Assert.NotNull(intended.Overview.FirstVolumeCovers[0]);
    }

    [Fact]
    public async Task AlbumList_CountsMoreThanAPage_AndAnAlbumWhoseDeletionASavedSearchBlocks()
    {
        var list = await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.AlbumAdminList>("/admin/albums");
        Assert.True(list!.Albums.TotalCount > list.Albums.Size);

        var blocked = await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.AlbumAdminList>("/admin/albums?q=Point%20z%C3%A9ro");
        var impact = await _client.GetFromJsonAsync<Bdtheque.Contracts.Deletion.DeletionImpact>(
            $"/admin/albums/{Assert.Single(blocked!.Albums.Items).Album.Id}/deletion-impact");
        Assert.Contains(impact!.BlockedBy, count => count.Kind == EntityKind.SavedSearch);
    }

    [Fact]
    public async Task PublisherForm_HasACollectionThatOnlyASavedSearchDesignates()
    {
        var publishers = await _client.GetFromJsonAsync<Page<Bdtheque.Contracts.Admin.PublisherAdminListItem>>(
            $"/admin/publishers?q={Uri.EscapeDataString(ValidationDataLoader.DesignatedCollectionPublisherName)}");
        var form = await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.PublisherForm>(
            $"/admin/publishers/{Assert.Single(publishers!.Items).Id}");

        var designated = Assert.Single(form!.Collections, c => c.Name == ValidationDataLoader.DesignatedCollectionName);
        Assert.Equal((0, 0, 0, 1), (designated.EditionCount, designated.PurchaseIntentCount, designated.SeriesCount, designated.SavedSearchCount));
    }

    [Fact]
    public async Task LongSeries_HasRegularVolumesOmnibusesAndSpecialIssues_WithAndWithoutCover()
    {
        var series = await SeriesAsync(ValidationDataLoader.LongSeriesTitle);

        Assert.True(series.Albums.Count >= 20);
        Assert.Contains(series.Albums, card => card.Album is { Type: AlbumType.Omnibus, IsSpecialIssue: false });
        Assert.Contains(series.Albums, card => card.Album is { Type: AlbumType.Regular, IsSpecialIssue: true });
        Assert.Contains(series.Albums, card => card.Album is { Type: AlbumType.Omnibus, IsSpecialIssue: true });
        Assert.Contains(series.Albums, card => card.Album.Title is null);
        Assert.Contains(series.Albums, card => card.Cover is not null);
        Assert.Contains(series.Albums, card => card.Album.IsInCollection && card.Cover is null);
        Assert.Contains(series.Albums, card => !card.Album.IsInCollection);
    }

    [Fact]
    public async Task Authors_UnderAPseudonym_WithSeveralRoles_AndOneCreditedOnNothing_WhoseDeletionIsAccepted()
    {
        var list = (await _client.GetFromJsonAsync<Bdtheque.Contracts.Catalog.Page<Bdtheque.Contracts.Admin.AuthorAdminListItem>>(
            "/admin/authors?pageSize=100"))!.Items;

        Assert.Contains(list, author => author is { Pseudonym: not null, LastName: not null });
        Assert.Contains(list, author => author.Roles.Count > 1);
        Assert.Contains(list, author => author.Nationality is null);
        var uncredited = Assert.Single(list, author => author.LastName == ValidationDataLoader.UncreditedAuthorLastName);
        Assert.Equal((0, 0), (uncredited.CreditedAlbumCount, uncredited.Roles.Count));
        var impact = await _client.GetFromJsonAsync<Bdtheque.Contracts.Deletion.DeletionImpact>($"/admin/authors/{uncredited.Id}/deletion-impact");
        Assert.Empty(impact!.BlockedBy);
    }

    [Fact]
    public async Task CatalogAuthorList_PresentsEachPossession_AlbumsAllOrPartlyInTheCollection_AndRolesOnASeriesAlone()
    {
        foreach (var state in Enum.GetValues<Bdtheque.Contracts.Admin.AlbumCollectionState>())
            Assert.NotEmpty((await _client.GetFromJsonAsync<Page<AuthorCatalogListItem>>($"/catalog/authors?state={state}"))!.Items);

        var list = (await _client.GetFromJsonAsync<Page<AuthorCatalogListItem>>("/catalog/authors?pageSize=100"))!.Items;
        Assert.Contains(list, a => a.CreditedAlbumsInCollectionCount > 0 && a.CreditedAlbumsInCollectionCount == a.CreditedAlbumCount);
        Assert.Contains(list, a => a.CreditedAlbumsInCollectionCount > 0 && a.CreditedAlbumsInCollectionCount < a.CreditedAlbumCount);
        Assert.Contains(list, a => a is { CreditedAlbumCount: > 0, CreditedAlbumsInCollectionCount: 0 });
        // Credited on a series and none of its albums: no album, yet a role.
        Assert.Contains(list, a => a is { CreditedAlbumCount: 0, Roles.Count: > 0 });
        Assert.Contains(list, a => a.Nationality is not null);
    }

    [Fact]
    public async Task Series_ACompleteOneAndOneWithoutAlbum_WhoseDeletionIsAccepted()
    {
        var list = (await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.SeriesAdminList>("/admin/series?pageSize=100"))!.Series.Items;

        Assert.Contains(list, series => series.IsComplete);
        var empty = Assert.Single(list, series => series.Title == ValidationDataLoader.SeriesWithoutAlbumTitle);
        Assert.Equal(0, empty.Overview.AlbumCount);
        var impact = await _client.GetFromJsonAsync<Bdtheque.Contracts.Deletion.DeletionImpact>($"/admin/series/{empty.Id}/deletion-impact");
        Assert.Empty(impact!.BlockedBy);
    }

    [Fact]
    public async Task Series_TheCasesOfTheirEntryForm()
    {
        // visuel/saisie-serie/contenu.md: a publisher and its collection, nothing proposed to the new editions, left out of the
        // computations, and in black and white.
        var list = (await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.SeriesAdminList>("/admin/series?pageSize=100"))!.Series.Items;
        var forms = new List<Bdtheque.Contracts.Admin.SeriesForm>();
        foreach (var series in list)
            forms.Add((await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.SeriesForm>($"/admin/series/{series.Id}"))!);

        Assert.Contains(forms, form => form.References.PublisherCollection is not null && form.References.Universes.Count > 0);
        Assert.Contains(forms, form => form.References.Publisher is null);
        Assert.Contains(forms, form => form.Content.EditionTemplate == new Bdtheque.Contracts.Admin.SeriesEditionTemplate(null, null, null, null, null, null, null, null, null)
                                       && form.Content is { ExcludeFromMissingVolumes: true, ExcludeFromReleaseEstimates: true });
        Assert.Contains(forms, form => form.Content.EditionTemplate.IsColor == false);
    }

    [Fact]
    public async Task Publishers_WithAndWithoutWebsiteCollectionsOrEditions_TheDeletionOfAnUnusedOneAccepted()
    {
        var list = (await _client.GetFromJsonAsync<Page<Bdtheque.Contracts.Admin.PublisherAdminListItem>>("/admin/publishers?pageSize=100"))!.Items;

        Assert.Contains(list, publisher => publisher.Website is not null && publisher.Collections.Count > 0 && publisher.EditionCount > 0);
        Assert.Contains(list, publisher => publisher.Website is null && publisher.EditionCount > 0);
        Assert.Contains(list, publisher => publisher.Collections.Count >= 5);
        var unused = Assert.Single(list, publisher => publisher.Name == ValidationDataLoader.UnusedPublisherName);
        Assert.Equal((null, 0, 0), (unused.Website, unused.Collections.Count, unused.EditionCount));
        // A publisher only named by the editions to buy of a purchase intent has none in the collection.
        Assert.Contains(list, publisher => publisher.Name == ValidationDataLoader.IntendedPublisherName
                                           && (publisher.EditionCount, publisher.PurchaseIntentCount) is (0, > 0));
        // Intents counted apart from the editions of a publisher that has both (visuel/listes/liste-par-liste.md).
        Assert.Contains(list, publisher => publisher is { EditionCount: > 0, PurchaseIntentCount: > 0 });
        foreach (var publisher in list.Where(publisher => publisher.EditionCount == 0 && publisher.Name != ValidationDataLoader.IntendedPublisherName))
        {
            var impact = await _client.GetFromJsonAsync<Bdtheque.Contracts.Deletion.DeletionImpact>($"/admin/publishers/{publisher.Id}/deletion-impact");
            Assert.Empty(impact!.BlockedBy);
        }
    }

    // visuel/fusion/fusion-de-deux-fiches.md: two publishers to merge, whose homonymous collections are spelled the same or
    // otherwise, each with what passes to the publisher that remains.
    [Fact]
    public async Task PublisherTwin_HasCollectionsHomonymousToThoseOfTheFirstOne_SpelledTheSameOrOtherwise()
    {
        var list = (await _client.GetFromJsonAsync<Page<Bdtheque.Contracts.Admin.PublisherAdminListItem>>("/admin/publishers?pageSize=100"))!.Items;
        var (first, twin) = (list.Single(p => p.Name == "Éditions du Lagon"), list.Single(p => p.Name == ValidationDataLoader.TwinPublisherName));

        var preview = await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.PublisherMergePreview>($"/admin/publishers/merge?first={first.Id}&second={twin.Id}");

        Assert.Equal([("Escales", "Escales"), ("Grand Large", "grand large")], preview!.Homonyms.Select(h => (h.FirstName, h.SecondName)));
        Assert.Superset(
            new HashSet<EntityKind>([EntityKind.Series, EntityKind.PublisherCollection, EntityKind.Edition]),
            preview.Links.Where(link => link.Second > 0).Select(link => link.Kind).ToHashSet());
        Assert.NotEqual(preview.First.Website, preview.Second.Website);
    }

    [Fact]
    public async Task PurchaseIntents_TargetWholeAlbumsAndEditions_OfAlbumsInAndOutsideTheCollection()
    {
        var intents = (await _client.GetFromJsonAsync<Page<Bdtheque.Contracts.Admin.PurchaseIntentAdminListItem>>(
            "/admin/purchase-intents?pageSize=100"))!.Items;

        // What the list of the intents presents (visuel/listes/liste-par-liste.md): an intent on the whole album, one on an
        // edition of an album of the collection, which has others, and one on the only edition of an album outside it, which
        // may turn into one on the album; covers and generic visuals.
        Assert.Contains(intents, intent => intent.Edition is null);
        Assert.Contains(intents, intent => intent is { Edition: not null, Album.IsInCollection: true, IsConvertible: false });
        Assert.Contains(intents, intent => intent is { Edition: not null, Album.IsInCollection: false, IsConvertible: true });
        Assert.Contains(intents, intent => intent.Cover is not null);
        Assert.Contains(intents, intent => intent.Cover is null);
        // Several intents on editions of one album, which its record presents under a plural mention
        // (visuel/fiche-album/contenu.md).
        Assert.Contains(intents.Where(intent => intent.Edition is not null).GroupBy(intent => intent.Album.Id), album => album.Count() > 1);
    }

    [Fact]
    public async Task PublicListOfThePurchaseIntents_PresentsEditionsWhosePublisherLeadsToItsSiteOrNot()
    {
        var intents = (await _client.GetFromJsonAsync<PurchaseIntentCatalogList>("/catalog/purchase-intents?pageSize=100"))!.Intents.Items;

        // The publisher of an edition targeted leads to its site when it is known, and is otherwise no link
        // (fonctionnel/presentation/libelle-d-une-edition.md); albums of the collection and albums wished for.
        Assert.Contains(intents, intent => intent is { Edition: not null, PublisherWebsite: not null });
        Assert.Contains(intents, intent => intent is { Edition: not null, PublisherWebsite: null });
        Assert.Contains(intents, intent => intent.State == Bdtheque.Contracts.Admin.AlbumCollectionState.InCollection);
        Assert.Contains(intents, intent => intent.State == Bdtheque.Contracts.Admin.AlbumCollectionState.PurchaseIntent);
    }

    [Fact]
    public async Task AlbumOnlyWishedFor_HasTheCoverOfTheEditionItTargets()
    {
        var album = await AlbumAsync(ValidationDataLoader.WishedForOnlyTitle);

        // Its record, of the documentary base, takes its cover among all its editions (fonctionnel/regles-metier/couverture-d-un-album.md).
        Assert.False(album.Album.IsInCollection);
        var edition = Assert.Single(album.Editions);
        Assert.NotNull(edition.PurchaseIntent);
        Assert.Equal(edition.Visuals.Single(visual => visual.Type == VisualType.Cover).Id, album.Cover?.VisualId);
    }

    [Fact]
    public async Task Universes_NestOnSeveralLevels_OneWithSubUniversesAndOneWithout()
    {
        var universes = (await _client.GetFromJsonAsync<Page<Bdtheque.Contracts.Admin.UniverseAdminListItem>>("/admin/universes?pageSize=100"))!.Items;

        // A universe three levels down, in retreat under its ancestors; one whose deletion its sub-universes refuse, and one at
        // the top whose deletion is accepted; one at the top with nothing yet, to attach to a parent created on the spot.
        Assert.Contains(universes, universe => universe.Ancestors.Count == 2);
        Assert.Contains(universes, universe => universe.SubUniverseCount > 0);
        Assert.Contains(universes, universe => universe is { ParentId: null, SubUniverseCount: 0 });
        Assert.Contains(universes, universe => universe is { Name: ValidationDataLoader.UnattachedUniverse, ParentId: null, SeriesCount: 0, AlbumCount: 0 });
    }

    // The entries of the navigation by initial other than a letter (fonctionnel/presentation/tri-et-navigation-par-initiale.md),
    // in the lists of the publishers and of the genres (visuel/listes/liste-par-liste.md).
    [Fact]
    public async Task PublishersAndGenres_UnderTheEntriesOfADigitAndOfAnotherCharacter()
    {
        Assert.NotEmpty((await _client.GetFromJsonAsync<Page<Bdtheque.Contracts.Admin.PublisherAdminListItem>>("/admin/publishers?entry=%23"))!.Items);
        Assert.NotEmpty((await _client.GetFromJsonAsync<Page<Bdtheque.Contracts.Admin.GenreAdminListItem>>("/admin/genres?entry=%40"))!.Items);
    }

    [Fact]
    public async Task Genres_OneAccentedWithoutAlbum_ThatTheSearchFindsWithoutItsAccents()
    {
        var genres = (await _client.GetFromJsonAsync<Page<Bdtheque.Contracts.Admin.GenreAdminListItem>>("/admin/genres?q=epopee"))!.Items;

        var accented = Assert.Single(genres);
        Assert.Equal((ValidationDataLoader.AccentedGenre, 0), (accented.Label, accented.AlbumCount));
    }

    [Fact]
    public async Task OneShot_HasSeveralEditions_OneOfThemWithEveryTypeOfVisual()
    {
        var album = await AlbumAsync(ValidationDataLoader.MultiEditionOneShotTitle);

        Assert.Null(album.Album.SeriesId);
        Assert.True(album.Editions.Count >= 2);
        Assert.Contains(album.Editions, edition =>
            Enum.GetValues<VisualType>().All(type => edition.Visuals.Any(visual => visual.Type == type))
            && edition.Visuals.Count(visual => visual.Type == VisualType.Cover) >= 2);
        Assert.Contains(album.Editions, edition => edition.IsbnChecksumValid == false);
        // The notes of a copy belong to its edition, not to the album (visuel/fiche-album/contenu.md).
        Assert.Contains(album.Editions, edition => edition.IsDedicated && edition.PersonalNotes is not null);
    }

    [Fact]
    // The cases of the list of the ISBN to check (visuel/listes/liste-par-liste.md): an owned edition and an intent to correct,
    // an ISBN no check character corrects, an error accepted, which the home page does not count.
    public async Task InvalidIsbns_EveryCaseOfTheirList()
    {
        var toCorrect = (await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.InvalidIsbnList>("/admin/editions/invalid-isbns"))!.Editions.Items;
        var accepted = (await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.InvalidIsbnList>("/admin/editions/invalid-isbns?state=Accepted"))!.Editions.Items;

        Assert.Contains(toCorrect, row => row is { IsOwned: true, CorrectedIsbn: not null });
        Assert.Contains(toCorrect, row => row is { IsOwned: false, CorrectedIsbn: not null });
        Assert.Contains(toCorrect, row => row.CorrectedIsbn is null);
        Assert.NotEmpty(accepted);
        Assert.Equal(toCorrect.Count, (await _client.GetFromJsonAsync<Bdtheque.Contracts.Admin.AdminChecks>("/admin/checks"))!.InvalidIsbnCount);
    }

    [Fact]
    public async Task Collective_CreditsManyAuthors_MoreThanTheRecordShowsInARoleBeforeFoldingThem()
    {
        var album = await AlbumAsync(ValidationDataLoader.CollectiveTitle);

        Assert.True(album.Contributions.Select(contribution => contribution.Author.Id).Distinct().Count() >= 6);
        // More names in a role than the line of a smartphone holds (visuel/fiche-album/contenu.md).
        Assert.Contains(album.Contributions.GroupBy(contribution => contribution.Role), role => role.Count() > 6);
    }

    [Theory]
    [InlineData("series", "#")]
    [InlineData("series", "@")]
    [InlineData("albums", "@")]
    [InlineData("albums", "E")]
    public async Task NavigationEntries_OutsideTheLetters_AndUnderAnArticle_AreCovered(string list, string entry)
    {
        var path = $"/catalog/{list}?entry={Uri.EscapeDataString(entry)}";
        // The lists of the albums and of the series carry their page with the records of their filters (AlbumCatalogList,
        // SeriesCatalogList).
        var items = list == "albums"
            ? (await _client.GetFromJsonAsync<AlbumCatalogList>(path))!.Albums.Items.Cast<object>()
            : (await _client.GetFromJsonAsync<SeriesCatalogList>(path))!.Series.Items;

        Assert.NotEmpty(items);
    }

    // The latest arrivals of the dashboard, with and without cover: « La couverture du jour » shows them as books in volume,
    // the generic visual of one without cover included (visuel/dashboard/la-couverture-du-jour.md, #326).
    [Fact]
    public async Task LatestArrivals_OfTheDashboard_WithAndWithoutCover()
    {
        var dashboard = await _client.GetFromJsonAsync<Dashboard>("/catalog/dashboard");

        var latest = dashboard!.Content!.LatestEntries;
        Assert.Contains(latest, entry => entry.Cover is not null);
        Assert.Contains(latest, entry => entry.Cover is null);
    }

    // The publishers of the latest arrivals, with and without website: one leads to its site, the other is no link
    // (visuel/dashboard/contenu-commun.md, #397).
    [Fact]
    public async Task LatestArrivals_OfTheDashboard_WithAndWithoutTheWebsiteOfTheirPublisher()
    {
        var dashboard = await _client.GetFromJsonAsync<Dashboard>("/catalog/dashboard");

        var latest = dashboard!.Content!.LatestEntries;
        Assert.Contains(latest, entry => entry.PublisherWebsite is not null);
        Assert.Contains(latest, entry => entry.PublisherWebsite is null);
    }

    // The home page of the administration: a record modified among the latest changes, a deleted one left out, and the
    // records whose page is their own or under their parent (an edition and an intent under their album, a collection under
    // its publisher), which carry it.
    [Fact]
    public async Task LatestChanges_PresentAModification_AndLeaveTheDeletedRecordOut()
    {
        var latest = await _client.GetFromJsonAsync<IReadOnlyList<Contracts.Admin.ChangeLogEntryItem>>("/admin/change-log/latest-records?count=6");

        Assert.Equal((ChangeLogOperation.Modification, (Contracts.Admin.RecordLabel)new Contracts.Admin.NameLabel("Western spaghetti")),
            (latest![0].Operation, latest[0].Label));
        Assert.DoesNotContain(latest, entry => entry.Label == new Contracts.Admin.NameLabel(ValidationDataLoader.DeletedGenre));
        Assert.Equal(
            [RecordType.Edition, RecordType.Series, RecordType.PurchaseIntent, RecordType.Publisher, RecordType.PublisherCollection, RecordType.Genre],
            latest.Select(entry => entry.RecordType).Order());
        Assert.All(latest, entry => Assert.Equal(
            entry.RecordType is RecordType.Edition or RecordType.PurchaseIntent or RecordType.PublisherCollection, entry.ParentId is not null));
    }

    // Without the demand of the pale cover, the edition in the spotlight is the one the application draws among all the owned
    // editions, at the first display of a day (fonctionnel/page-accueil.md, À la une): the load draws none itself.
    [Fact]
    public async Task Spotlight_IsLeftToTheDrawOfTheApplication()
    {
        var response = await _client.PostAsJsonAsync(
            "/catalog/search/editions",
            new AdvancedSearchRequest(new SearchCriterion("spotlightCount", SearchComparison.GreaterThan, ["0"])));
        response.EnsureSuccessStatusCode();
        var found = await response.Content.ReadFromJsonAsync<Page<EditionListItem>>();

        Assert.Empty(found!.Items);
    }

    private async Task<SeriesDetail> SeriesAsync(string title)
    {
        var found = await _client.GetFromJsonAsync<SeriesCatalogList>($"/catalog/series?q={Uri.EscapeDataString(title)}");
        return (await _client.GetFromJsonAsync<SeriesDetail>($"/catalog/series/{Assert.Single(found!.Series.Items).Id}"))!;
    }

    private async Task<AlbumDetail> AlbumAsync(string title)
    {
        var found = await _client.GetFromJsonAsync<AlbumCatalogList>($"/catalog/albums?q={Uri.EscapeDataString(title)}");
        return (await _client.GetFromJsonAsync<AlbumDetail>($"/catalog/albums/{Assert.Single(found!.Albums.Items).Album.Id}"))!;
    }

    /// <summary>The validation data as a start of the validation environment loads it, without the pale cover in the spotlight.</summary>
    public sealed class LoadedApi() : LoadedApiFixture(paleCoverInTheSpotlight: false);
}
