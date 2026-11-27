using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Api.Tests;

/// <summary>Reading of the change log (<c>/admin/change-log</c>).</summary>
public sealed class ChangeLogEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateApiClient();

    [Fact]
    public async Task Entries_ReadFromTheLatestToTheOldest_AsData()
    {
        var album = await CreateAlbumAsync("Le Lotus bleu", AlbumRating.Good);
        var response = await _client.PutAsJsonAsync(
            $"/admin/albums/{album.Id}", new UpdateAlbumRequest(Content("Les Cigares du pharaon", AlbumRating.VeryGood), album.Version));
        response.EnsureSuccessStatusCode();

        var entries = (await ReadAsync($"record={album.Id}")).Entries.Items.ToList();

        Assert.Equal([ChangeLogOperation.Modification, ChangeLogOperation.Creation], entries.Select(e => e.Operation));
        Assert.All(entries, e => Assert.Equal(RecordType.Album, e.RecordType));
        var modification = entries[0];
        Assert.Equal(new AlbumLabel("Les Cigares du pharaon", null, AlbumType.Regular, false, null, null, null), modification.Label);
        Assert.Contains(
            new ChangeLogChange(ChangeLogAttribute.Title, new TextValue("Le Lotus bleu"), new TextValue("Les Cigares du pharaon")),
            modification.Changes);
        Assert.Contains(
            new ChangeLogChange(
                ChangeLogAttribute.Rating, new EnumValue(nameof(AlbumRating), nameof(AlbumRating.Good)),
                new EnumValue(nameof(AlbumRating), nameof(AlbumRating.VeryGood))),
            modification.Changes);
        Assert.Empty(entries[1].Changes);
    }

    [Fact]
    public async Task SameEntry_AlwaysReadsTheSame()
    {
        await CreateAlbumAsync("Tintin au Tibet", AlbumRating.Good);

        var first = await _client.GetStringAsync("/admin/change-log?pageSize=100");
        var second = await _client.GetStringAsync("/admin/change-log?pageSize=100");

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Entries_ArePaginated()
    {
        await CreateAlbumAsync("Coke en stock", null);
        await CreateAlbumAsync("Vol 714 pour Sydney", null);

        var page = (await ReadAsync("pageSize=1")).Entries;

        var latest = Assert.Single(page.Items);
        Assert.Equal(1, page.Size);
        Assert.True(page.TotalCount >= 2);
        Assert.Equal(latest.Id, (await ReadAsync("pageSize=100")).Entries.Items[0].Id);
    }

    [Fact]
    public async Task PageSizeOutOfBounds_IsATechnicalError()
    {
        var response = await _client.GetAsync("/admin/change-log?pageSize=0");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    // The search looks into every text of the label, those of the album of an edition too, ignoring case and accents, and finds
    // the records deleted since (visuel/journal/journal-des-modifications.md); never the kinds of the label.
    [Fact]
    public async Task Search_LooksIntoTheTextsOfTheLabel_IgnoringCaseAndAccents()
    {
        var album = await CreateAlbumAsync("Les Bijoux de la Castafiore", null);
        var publisher = await PostAsync<PublisherContent, PublisherForm>("/admin/publishers", new("Moulinsart", null, []));
        var edition = await PostAsync<CreateEditionRequest, EditionForm>(
            $"/admin/albums/{album.Id}/editions", new(Edition(publisher.Id), album.Version));
        var genre = await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new("Castafiore lyrique"));
        await DeleteAsync($"/admin/genres/{genre.Id}", genre.Version);

        var found = (await ReadAsync("q=CASTAFIORÉ&pageSize=100")).Entries.Items;

        Assert.Equal(
            [(genre.Id, ChangeLogOperation.Deletion), (genre.Id, ChangeLogOperation.Creation), (edition.Id, ChangeLogOperation.Creation),
                (album.Id, ChangeLogOperation.Creation)],
            found.Where(e => e.RecordId == genre.Id || e.RecordId == edition.Id || e.RecordId == album.Id).Select(e => (e.RecordId, e.Operation)));
        Assert.Empty((await ReadAsync("q=purchaseIntent&pageSize=100")).Entries.Items);
        Assert.DoesNotContain((await ReadAsync("q=edition&pageSize=100")).Entries.Items, e => e.RecordId == edition.Id);
        // The wildcards of LIKE are looked for as they are.
        Assert.Empty((await ReadAsync("q=Castafiore%25lyrique")).Entries.Items);
    }

    // As in the lists (choix-implementation/api/recherche.md): an author by the full name in its two orders, a text by its own
    // characters, never those of its storage, and an ISBN without its separators, on both sides.
    [Fact]
    public async Task Search_FindsWhatIdentifiesTheRecord_AsTheListsDo()
    {
        var author = await PostAsync<CreateAuthorRequest, AuthorForm>("/admin/authors", new("Uderzo", "Albert", null, null, null));
        var quoted = await CreateAlbumAsync("Le \"Grand\" fossé", null);
        var publisher = await PostAsync<PublisherContent, PublisherForm>("/admin/publishers", new("Dargaud", null, []));
        var edition = await PostAsync<CreateEditionRequest, EditionForm>(
            $"/admin/albums/{quoted.Id}/editions", new(Edition(publisher.Id) with { Isbn = "978-2-205-04600-5" }, quoted.Version));

        Assert.Contains((await ReadAsync("q=albert uderzo&pageSize=100")).Entries.Items, e => e.RecordId == author.Id);
        Assert.Contains((await ReadAsync("q=Uderzo Albert&pageSize=100")).Entries.Items, e => e.RecordId == author.Id);
        Assert.Contains((await ReadAsync("q=%22Grand%22&pageSize=100")).Entries.Items, e => e.RecordId == quoted.Id);
        Assert.Contains((await ReadAsync("q=978 2205 046005&pageSize=100")).Entries.Items, e => e.RecordId == edition.Id);
        Assert.Contains((await ReadAsync("q=2-205-04600&pageSize=100")).Entries.Items, e => e.RecordId == edition.Id);
        // An ISBN-10 whose check digit is an X, searched without its separators and in lower case, as the lists find it.
        var tenDigits = await PostAsync<CreateEditionRequest, EditionForm>(
            $"/admin/albums/{quoted.Id}/editions", new(Edition(publisher.Id) with { Isbn = "0-8044-2957-X" }, edition.AlbumVersion));
        Assert.Contains((await ReadAsync("q=080442957x&pageSize=100")).Entries.Items, e => e.RecordId == tenDigits.Id);
        // Two texts of a label are searched apart, never their storage between them.
        Assert.DoesNotContain((await ReadAsync("q=Uderzo%22,&pageSize=100")).Entries.Items, e => e.RecordId == author.Id);
    }

    // As in the lists (choix-implementation/api/recherche.md): the typographic variants of an apostrophe, a space or a hyphen
    // are the same character, on both sides, in a text as in the full name of an author.
    [Fact]
    public async Task Search_TreatsTheTypographicVariantsOfACharacterAsTheSame()
    {
        var album = await CreateAlbumAsync("L\u2019Étoile\u00A0mystérieuse", null);
        var author = await PostAsync<CreateAuthorRequest, AuthorForm>("/admin/authors", new("D'Arras", "Jean-Michel", null, null, null));

        Assert.Contains((await ReadAsync("q=l'etoile mysterieuse&pageSize=100")).Entries.Items, e => e.RecordId == album.Id);
        Assert.Contains(
            (await ReadAsync($"q={Uri.EscapeDataString("jean\u2011michel d\u2019arras")}&pageSize=100")).Entries.Items, e => e.RecordId == author.Id);
    }

    // The entries of one write share its instant: they come in a fixed order, by the type of their records, never by the
    // random part of their identifiers.
    [Fact]
    public async Task EntriesOfOneWrite_ComeInTheOrderOfTheirTypes()
    {
        var album = await CreateAlbumAsync("Les Voleurs de Marsupilamis", null);
        var publisher = await PostAsync<PublisherContent, PublisherForm>("/admin/publishers", new("Marsu Productions", null, []));
        var intents = await PostAsync<CreatePurchaseIntentRequest, PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents",
            new(new PurchaseIntentEditionContent(publisher.Id, null, null, null, null, null, null, null, null, null, true), album.Version));
        var intent = intents.Intents.Single();

        var found = (await ReadAsync("q=Voleurs de Marsupilamis&pageSize=100")).Entries.Items;

        Assert.Equal(
            [(RecordType.PurchaseIntent, intent.Id), (RecordType.Edition, intent.EditionId!.Value), (RecordType.Album, album.Id)],
            found.Select(e => (e.RecordType, e.RecordId)));
        Assert.Equal(found[0].Date, found[1].Date);
    }

    [Fact]
    public async Task Filters_OnTypeOperationAndRecord_Combine()
    {
        var genre = await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new("Fantastique urbain"));
        (await _client.PutAsJsonAsync($"/admin/genres/{genre.Id}", new UpdateGenreRequest("Fantastique", genre.Version))).EnsureSuccessStatusCode();
        await CreateAlbumAsync("Le Temple du Soleil", null);

        var all = await ReadAsync($"record={genre.Id}");
        var modifications = await ReadAsync($"record={genre.Id}&recordType=Genre&operation=Modification");
        var albumsOnly = await ReadAsync($"record={genre.Id}&recordType=Album");

        Assert.Equal(2, all.Entries.TotalCount);
        Assert.Equal(new ChangeLogRecord(RecordType.Genre, new NameLabel("Fantastique")), all.Record);
        Assert.Equal(ChangeLogOperation.Modification, Assert.Single(modifications.Entries.Items).Operation);
        Assert.Empty(albumsOnly.Entries.Items);
        Assert.True(albumsOnly.WholeCount >= 3);
    }

    // A period is two days, both included, in the time zone of the browser: the day of an entry is that of its date there.
    [Theory]
    [InlineData(null)]
    [InlineData("Pacific/Kiritimati")]
    [InlineData("Pacific/Pago_Pago")]
    public async Task Period_IsTwoDaysIncluded_InTheTimeZoneOfTheBrowser(string? timeZone)
    {
        var genre = await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new($"Western {timeZone}"));
        var date = Assert.Single((await ReadAsync($"record={genre.Id}")).Entries.Items).Date;
        var zone = timeZone is null ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(date, zone).DateTime);
        var inZone = timeZone is null ? "" : $"&timeZone={Uri.EscapeDataString(timeZone)}";
        static string Day(DateOnly d) => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Single((await ReadAsync($"record={genre.Id}&from={Day(day)}&to={Day(day)}{inZone}")).Entries.Items);
        Assert.Single((await ReadAsync($"record={genre.Id}&from={Day(day)}{inZone}")).Entries.Items);
        Assert.Single((await ReadAsync($"record={genre.Id}&to={Day(day)}{inZone}")).Entries.Items);
        Assert.Empty((await ReadAsync($"record={genre.Id}&to={Day(day.AddDays(-1))}{inZone}")).Entries.Items);
        Assert.Empty((await ReadAsync($"record={genre.Id}&from={Day(day.AddDays(1))}{inZone}")).Entries.Items);
    }

    [Fact]
    public async Task Record_UnknownToTheChangeLog_IsNamedByNothing()
    {
        var list = await ReadAsync($"record={Guid.CreateVersion7()}");

        Assert.Empty(list.Entries.Items);
        Assert.Null(list.Record);
    }

    // A record deleted since leads nowhere: every entry of it says so, not only its deletion.
    [Fact]
    public async Task Entries_TellWhetherTheirRecordStillExists()
    {
        var deleted = await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new("Péplum"));
        var kept = await PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new("Uchronie"));
        await DeleteAsync($"/admin/genres/{deleted.Id}", deleted.Version);

        var entries = (await ReadAsync("recordType=Genre&pageSize=100")).Entries.Items;

        Assert.All(entries.Where(e => e.RecordId == deleted.Id), e => Assert.False(e.RecordExists));
        Assert.True(Assert.Single(entries, e => e.RecordId == kept.Id).RecordExists);
    }

    private async Task<ChangeLogList> ReadAsync(string query) =>
        (await _client.GetFromJsonAsync<ChangeLogList>($"/admin/change-log?{query}"))!;

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request)
    {
        var response = await _client.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    // A deletion confirms the impact it was shown, by its fingerprint.
    private async Task DeleteAsync(string path, uint version)
    {
        var impact = await _client.GetFromJsonAsync<Contracts.Deletion.DeletionImpact>($"{path}/deletion-impact");
        (await _client.DeleteAsync($"{path}?version={version}&fingerprint={Uri.EscapeDataString(impact!.Fingerprint)}")).EnsureSuccessStatusCode();
    }

    private static EditionContent Edition(Guid publisher) =>
        new(publisher, null, 2001, null, null, null, null, null, null, null, false, true, null,
            AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null);

    private async Task<AlbumForm> CreateAlbumAsync(string title, AlbumRating? rating)
    {
        var response = await _client.PostAsJsonAsync("/admin/albums", Content(title, rating));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AlbumForm>())!;
    }

    private static AlbumContent Content(string title, AlbumRating? rating) =>
        new(title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, rating, [], [], []);
}
