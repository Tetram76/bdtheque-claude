using System.Net.Http.Json;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

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
    public async Task Publishers_WithAndWithoutWebsiteCollectionsOrEditions_TheDeletionOfAnUnusedOneAccepted()
    {
        var list = (await _client.GetFromJsonAsync<Page<Bdtheque.Contracts.Admin.PublisherAdminListItem>>("/admin/publishers?pageSize=100"))!.Items;

        Assert.Contains(list, publisher => publisher.Website is not null && publisher.Collections.Count > 0 && publisher.EditionCount > 0);
        Assert.Contains(list, publisher => publisher.Website is null && publisher.EditionCount > 0);
        Assert.Contains(list, publisher => publisher.Collections.Count >= 5);
        var unused = Assert.Single(list, publisher => publisher.Name == ValidationDataLoader.UnusedPublisherName);
        Assert.Equal((null, 0, 0), (unused.Website, unused.Collections.Count, unused.EditionCount));
        // A publisher only named by the editions to buy of a purchase intent has none in the collection.
        Assert.Contains(list, publisher => publisher.Name == ValidationDataLoader.IntendedPublisherName && publisher.EditionCount == 0);
        foreach (var publisher in list.Where(publisher => publisher.EditionCount == 0 && publisher.Name != ValidationDataLoader.IntendedPublisherName))
        {
            var impact = await _client.GetFromJsonAsync<Bdtheque.Contracts.Deletion.DeletionImpact>($"/admin/publishers/{publisher.Id}/deletion-impact");
            Assert.Empty(impact!.BlockedBy);
        }
    }

    [Fact]
    public async Task PurchaseIntents_TargetWholeAlbumsAndEditions()
    {
        var intents = await _client.GetFromJsonAsync<Page<PurchaseIntentListItem>>("/catalog/purchase-intents?pageSize=100");

        Assert.Contains(intents!.Items, intent => intent.Edition is null);
        Assert.Contains(intents.Items, intent => intent.Edition is not null && intent.Album.IsInCollection);
    }

    [Fact]
    public async Task Universes_NestOnSeveralLevels()
    {
        var universes = (await _client.GetFromJsonAsync<Page<UniverseListItem>>("/catalog/universes?pageSize=100"))!.Items;

        Assert.Contains(universes, universe =>
            universes.Any(parent => parent.Id == universe.ParentId && parent.ParentId is not null));
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
        var page = await _client.GetFromJsonAsync<Page<object>>($"/catalog/{list}?entry={Uri.EscapeDataString(entry)}");

        Assert.NotEmpty(page!.Items);
    }

    // The home page of the administration: a record modified among the latest changes, a deleted one left out.
    [Fact]
    public async Task LatestChanges_PresentAModification_AndLeaveTheDeletedRecordOut()
    {
        var latest = await _client.GetFromJsonAsync<IReadOnlyList<Contracts.Admin.ChangeLogEntryItem>>("/admin/change-log/latest-records?count=6");

        Assert.Equal((ChangeLogOperation.Modification, (Contracts.Admin.RecordLabel)new Contracts.Admin.NameLabel("Western spaghetti")),
            (latest![0].Operation, latest[0].Label));
        Assert.DoesNotContain(latest, entry => entry.Label == new Contracts.Admin.NameLabel(ValidationDataLoader.DeletedGenre));
    }

    // « La couverture du jour » (visuel/dashboard/la-couverture-du-jour.md, Lisibilité): the cover of the day, which colours
    // every page of the theme, is pale, with an accent of middle tone, which neither a white nor a nearly black text reaches.
    [Fact]
    public async Task SpotlightOfTheDay_IsAPaleCover_WithAnAccentOfMiddleTone()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var day in new[] { today.AddDays(-1), today, today.AddDays(1) })
        {
            var palette = (await _client.GetFromJsonAsync<SpotlightColors>($"/catalog/spotlight/colors?day={day:yyyy-MM-dd}"))!.Palette!;

            Assert.True(Luminance(palette.Dominant) > 0.6, $"The cover of {day} is not pale: {palette.Dominant}.");
            Assert.True(ContrastRatio(palette.Accent, "#FFFFFF") < 4.5 && ContrastRatio(palette.Accent, "#15151A") < 4.5,
                $"The accent of {day} is not of middle tone: {palette.Accent}.");
        }
    }

    private static double ContrastRatio(string first, string second)
    {
        var (lighter, darker) = (Luminance(first), Luminance(second)) is var (a, b) && a > b ? (a, b) : (b, a);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(string hex)
    {
        double Channel(int start)
        {
            var value = int.Parse(hex.AsSpan(start, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture) / 255d;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
    }

    private async Task<SeriesDetail> SeriesAsync(string title)
    {
        var found = await _client.GetFromJsonAsync<Page<SeriesListItem>>($"/catalog/series?q={Uri.EscapeDataString(title)}");
        return (await _client.GetFromJsonAsync<SeriesDetail>($"/catalog/series/{Assert.Single(found!.Items).Id}"))!;
    }

    private async Task<AlbumDetail> AlbumAsync(string title)
    {
        var found = await _client.GetFromJsonAsync<Page<AlbumSummary>>($"/catalog/albums?q={Uri.EscapeDataString(title)}");
        return (await _client.GetFromJsonAsync<AlbumDetail>($"/catalog/albums/{Assert.Single(found!.Items).Id}"))!;
    }

    /// <summary>An API on an empty database, into which the validation data is loaded once for the whole class.</summary>
    public sealed class LoadedApi : ApiHostFactory, IAsyncLifetime
    {
        async Task IAsyncLifetime.InitializeAsync()
        {
            await InitializeAsync();
            await new ValidationDataLoader(CreateApiClient()).LoadAsync(CancellationToken.None);
        }

        Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
    }
}
