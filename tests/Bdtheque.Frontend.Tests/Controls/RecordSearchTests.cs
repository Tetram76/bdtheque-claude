using System.Net;
using System.Text;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The suggestions of a <see cref="RecordPicker"/>: the records of the list of the consultation of their kind whose name
/// contains the text typed, each named as the lists name it.
/// </summary>
public sealed class RecordSearchTests
{
    private static readonly Guid Id = Guid.Parse("0199b6a4-0000-7000-8000-000000000001");
    private static readonly Guid ParentId = Guid.Parse("0199b6a4-0000-7000-8000-000000000002");

    public static TheoryData<RecordKind, string, string, RecordOption> Kinds => new()
    {
        { RecordKind.Series, "/catalog/series", $$"""{"id":"{{Id}}","title":"Tintin"}""", new(Id, "Tintin", null) },
        { RecordKind.Publisher, "/catalog/publishers", $$"""{"id":"{{Id}}","name":"Dargaud","website":null}""", new(Id, "Dargaud", null) },
        {
            RecordKind.PublisherCollection, "/catalog/publisher-collections",
            $$"""{"id":"{{Id}}","name":"Lucky Luke","publisherId":"{{ParentId}}","publisherName":"Dargaud"}""", new(Id, "Lucky Luke", "Dargaud")
        },
        { RecordKind.Genre, "/catalog/genres", $$"""{"id":"{{Id}}","label":"Aventure"}""", new(Id, "Aventure", null) },
        {
            RecordKind.Universe, "/catalog/universes",
            $$"""{"id":"{{Id}}","name":"Spirou","parentId":"{{ParentId}}","parentName":"Dupuis"}""", new(Id, "Spirou", "Dupuis")
        },
        {
            RecordKind.Author, "/catalog/authors",
            $$"""{"id":"{{Id}}","lastName":"Van Hamme","firstName":"Jean","pseudonym":null}""", new(Id, "Jean Van Hamme", null)
        },
    };

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Find_ReadsTheListOfItsKind_NamingEachRecordAsTheListsDo(RecordKind kind, string path, string item, RecordOption expected)
    {
        string? requested = null;
        var search = new RecordSearch(new ApiClient(new HttpClient(new StubHandler(request =>
        {
            requested = request.RequestUri!.PathAndQuery;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"items":[{{item}}],"number":1,"size":10,"totalCount":1}""", Encoding.UTF8, "application/json"),
            };
        }))
        {
            BaseAddress = new Uri("http://api"),
        }));

        var options = await search.FindAsync(kind, "  Dar & co ", CancellationToken.None);

        Assert.Equal(expected, Assert.Single(options));
        Assert.Equal($"{path}?q=Dar%20%26%20co&pageSize={RecordSearch.SuggestionCount}", requested);
    }

    [Theory]
    [InlineData("Remi", "Georges", "Hergé", "Hergé")]
    [InlineData("Van Hamme", "Jean", null, "Jean Van Hamme")]
    [InlineData("Moebius", null, null, "Moebius")]
    [InlineData(null, null, "Moebius", "Moebius")]
    public void AuthorName_IsItsMainIdentifier(string? lastName, string? firstName, string? pseudonym, string expected) =>
        Assert.Equal(expected, RecordSearch.AuthorName(lastName, firstName, pseudonym));

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
