using System.Net;
using System.Text;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The suggestions of a <see cref="RecordPicker"/>: the records of the list of the consultation of their kind whose name
/// contains the text typed, each named as the lists name it.
/// </summary>
public sealed class RecordSearchTests
{
    private static readonly Guid Id = Guid.Parse("0199b6a4-0000-7000-8000-000000000001");
    private static readonly Guid ParentId = Guid.Parse("0199b6a4-0000-7000-8000-000000000002");

    public static TheoryData<RecordKind, string, string, string, string?> Kinds => new()
    {
        { RecordKind.Series, "/catalog/series", $$"""{"id":"{{Id}}","title":"Tintin"}""", "Tintin", null },
        { RecordKind.Publisher, "/catalog/publishers", $$"""{"id":"{{Id}}","name":"Dargaud","website":null}""", "Dargaud", null },
        {
            RecordKind.PublisherCollection, "/catalog/publisher-collections",
            $$"""{"id":"{{Id}}","name":"Lucky Luke","publisherId":"{{ParentId}}","publisherName":"Dargaud"}""", "Lucky Luke", "Dargaud"
        },
        { RecordKind.Genre, "/catalog/genres", $$"""{"id":"{{Id}}","label":"Aventure"}""", "Aventure", null },
        {
            RecordKind.Universe, "/catalog/universes",
            $$"""{"id":"{{Id}}","name":"Spirou","parentId":"{{ParentId}}","parentName":"Dupuis"}""", "Spirou", "Dupuis"
        },
        {
            RecordKind.Author, "/catalog/authors",
            $$"""{"id":"{{Id}}","lastName":"Van Hamme","firstName":"Jean","pseudonym":null}""", "Jean Van Hamme", null
        },
        {
            // Named by its label in the format of the settings, its series beside it (fonctionnel/presentation/libelle-d-un-album.md).
            RecordKind.Album, "/catalog/albums",
            $$"""
              {"id":"{{Id}}","title":"Le Lotus bleu","seriesId":"{{ParentId}}","seriesTitle":"Tintin","type":"Regular","isSpecialIssue":false,
               "volumeNumber":5,"startVolumeNumber":null,"endVolumeNumber":null,"isInCollection":true}
              """,
            "T. 5 - Le Lotus bleu (Tintin)", null
        },
    };

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Find_ReadsTheListOfItsKind_NamingEachRecordAsTheListsDo(RecordKind kind, string path, string item, string label, string? detail)
    {
        string? requested = null;
        var search = new RecordSearch(new ApiClient(new HttpClient(new StubHandler(request =>
        {
            // The settings, in the format whose volume comes first, which a label in the default format would not follow.
            if (request.RequestUri!.AbsolutePath == "/catalog/settings")
                return Json("""{"albumLabelFormat":"VolumeTitleSeries","displayCulture":"FrenchFrance"}""");
            requested = request.RequestUri!.PathAndQuery;
            return Json($$"""{"items":[{{item}}],"number":1,"size":10,"totalCount":1}""");
        }))
        {
            BaseAddress = new Uri("http://api"),
        }), Labels);

        var options = await search.FindAsync(kind, "  Dar & co ", CancellationToken.None);

        Assert.Equal(new RecordOption(Id, label, detail), Assert.Single(options) with { Parent = null });
        Assert.Equal($"{path}?q=Dar%20%26%20co&pageSize={RecordSearch.SuggestionCount}", requested);
    }

    [Fact]
    public async Task Find_CollectionsOfAPublisher_OnlyThose_EachWithItsPublisher()
    {
        string? requested = null;
        var search = new RecordSearch(new ApiClient(new HttpClient(new StubHandler(request =>
        {
            requested = request.RequestUri!.PathAndQuery;
            return Json($$"""{"items":[{"id":"{{Id}}","name":"Lucky Luke","publisherId":"{{ParentId}}","publisherName":"Dargaud"}],"number":1,"size":10,"totalCount":1}""");
        }))
        {
            BaseAddress = new Uri("http://api"),
        }), Labels);

        var options = await search.FindAsync(RecordKind.PublisherCollection, "luc", CancellationToken.None, within: ParentId);

        // Its publisher, which the choice of the collection sets (visuel/listes/principes-communs.md).
        Assert.Equal(new RecordOption(Id, "Lucky Luke", "Dargaud", new RecordOption(ParentId, "Dargaud", null)), Assert.Single(options));
        Assert.Equal($"/catalog/publisher-collections?q=luc&pageSize={RecordSearch.SuggestionCount}&publisherId={ParentId}", requested);
    }

    [Fact]
    public async Task Find_AlbumsOfASeries_OnlyThose_EachWithItsSeries_NoneForAnAlbumWithout()
    {
        var alone = Guid.Parse("0199b6a4-0000-7000-8000-000000000003");
        string? requested = null;
        var search = new RecordSearch(new ApiClient(new HttpClient(new StubHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/catalog/settings")
                return Json("""{"albumLabelFormat":"TitleSeriesVolume","displayCulture":"FrenchFrance"}""");
            requested = request.RequestUri!.PathAndQuery;
            return Json($$"""
                {"items":[
                  {"id":"{{Id}}","title":"Le Lotus bleu","seriesId":"{{ParentId}}","seriesTitle":"Tintin","type":"Regular","isSpecialIssue":false,
                   "volumeNumber":5,"startVolumeNumber":null,"endVolumeNumber":null,"isInCollection":true},
                  {"id":"{{alone}}","title":"Silence","seriesId":null,"seriesTitle":null,"type":"Regular","isSpecialIssue":false,
                   "volumeNumber":null,"startVolumeNumber":null,"endVolumeNumber":null,"isInCollection":true}],
                 "number":1,"size":10,"totalCount":2}
                """);
        }))
        {
            BaseAddress = new Uri("http://api"),
        }), Labels);

        var options = await search.FindAsync(RecordKind.Album, "l", CancellationToken.None, within: ParentId);

        Assert.Equal(
            [new RecordOption(Id, "Le Lotus bleu (Tintin - T. 5)", null, new RecordOption(ParentId, "Tintin", null)), new RecordOption(alone, "Silence", null)],
            options);
        Assert.Equal($"/catalog/albums?q=l&pageSize={RecordSearch.SuggestionCount}&seriesId={ParentId}", requested);
    }

    [Theory]
    [InlineData("Remi", "Georges", "Hergé", "Hergé")]
    [InlineData("Van Hamme", "Jean", null, "Jean Van Hamme")]
    [InlineData("Moebius", null, null, "Moebius")]
    [InlineData(null, null, "Moebius", "Moebius")]
    public void AuthorName_IsItsMainIdentifier(string? lastName, string? firstName, string? pseudonym, string expected) =>
        Assert.Equal(expected, RecordSearch.AuthorName(lastName, firstName, pseudonym));

    private static readonly RecordLabels Labels = new ServiceCollection().AddLogging().AddLocalization().AddSingleton<IsbnFormatter>().AddSingleton<RecordLabels>()
        .BuildServiceProvider().GetRequiredService<RecordLabels>();

    private static HttpResponseMessage Json(string content) =>
        new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
