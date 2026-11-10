using System.Net;
using System.Text;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Lists;
using Bdtheque.Frontend.Tests.Controls;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// Two filters of a list linked as a record and the one it belongs to (a collection and its publisher, an album and its
/// series, visuel/listes/principes-communs.md): the record chosen sets the one it belongs to, and, that one chosen, only its
/// records are offered.
/// </summary>
public sealed class LinkedFiltersTests : BunitContext
{
    private static readonly RecordOption Dargaud = new(Guid.Parse("0199b6a4-0000-7000-8000-000000000001"), "Dargaud", null);
    private static readonly RecordOption Dupuis = new(Guid.Parse("0199b6a4-0000-7000-8000-000000000002"), "Dupuis", null);
    private static readonly RecordOption LuckyLuke = new(Guid.Parse("0199b6a4-0000-7000-8000-000000000003"), "Lucky Luke", "Dargaud", Dargaud);

    private readonly FakeTimeProvider _clock = new();
    private readonly List<string> _requests = [];

    public LinkedFiltersTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<TimeProvider>(_clock);
        Services.AddSingleton(new ApiClient(new HttpClient(new StubHandler(request =>
        {
            lock (_requests)
                _requests.Add(request.RequestUri!.PathAndQuery);
            return Json(request.RequestUri.AbsolutePath == "/catalog/publishers"
                ? $$"""{"items":[{"id":"{{Dupuis.Id}}","name":"Dupuis","website":null}],"number":1,"size":10,"totalCount":1}"""
                : $$"""{"items":[{"id":"{{LuckyLuke.Id}}","name":"Lucky Luke","publisherId":"{{Dargaud.Id}}","publisherName":"Dargaud"}],"number":1,"size":10,"totalCount":1}""");
        }))
        {
            BaseAddress = new Uri("http://api"),
        }));
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton<Bdtheque.Frontend.Formatting.IsbnFormatter>().AddSingleton<Bdtheque.Frontend.Formatting.RecordLabels>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Filters_AreTwoFieldsOfTheForm_EachSendingItsRecord()
    {
        var filters = Open(Dargaud, LuckyLuke);

        Assert.Equal(["Éditeur", "Collection éditeur"], filters.FindAll("label").Select(label => label.TextContent.Trim()));
        Assert.Equal((Dargaud.Id.ToString(), LuckyLuke.Id.ToString()), Sent(filters));
    }

    [Fact]
    public async Task ChoosingTheRecord_SetsTheOneItBelongsTo()
    {
        var filters = Open(null, null);

        await ChooseAsync(filters, "#filter-collection", "luc");

        Assert.Equal((Dargaud.Id.ToString(), LuckyLuke.Id.ToString()), Sent(filters));
        Assert.Equal("Dargaud", filters.Find("#filter-publisher").GetAttribute("value"));
    }

    [Fact]
    public async Task OneItBelongsToChosen_OnlyItsRecordsAreOffered()
    {
        var filters = Open(Dargaud, null);

        await ChooseAsync(filters, "#filter-collection", "luc");

        Assert.Contains($"/catalog/publisher-collections?q=luc&pageSize={RecordSearch.SuggestionCount}&publisherId={Dargaud.Id}", _requests);
    }

    [Fact]
    public async Task AnotherOneItBelongsTo_RemovesARecordThatIsNotItsOwn()
    {
        var filters = Open(Dargaud, LuckyLuke);

        await ChooseAsync(filters, "#filter-publisher", "dup");

        Assert.Equal((Dupuis.Id.ToString(), ""), Sent(filters));
    }

    [Fact]
    public async Task RemovingTheOneItBelongsTo_KeepsTheRecord()
    {
        var filters = Open(Dargaud, LuckyLuke);

        await filters.InvokeAsync(() => filters.Find("#filter-publisher + .record-picker-clear").Click());

        // A collection belongs to a single publisher: the list filtered on it is the same, with or without its publisher.
        Assert.Equal(("", LuckyLuke.Id.ToString()), Sent(filters));
    }

    private IRenderedComponent<LinkedFilters> Open(RecordOption? parent, RecordOption? child) =>
        Render<LinkedFilters>(parameters => parameters
            .Add(p => p.ParentKind, RecordKind.Publisher).Add(p => p.ParentId, "filter-publisher").Add(p => p.ParentName, "publisherId")
            .Add(p => p.ParentLabel, "Éditeur").Add(p => p.Parent, parent)
            .Add(p => p.ChildKind, RecordKind.PublisherCollection).Add(p => p.ChildId, "filter-collection").Add(p => p.ChildName, "publisherCollectionId")
            .Add(p => p.ChildLabel, "Collection éditeur").Add(p => p.Child, child)
            .Add(p => p.Placeholder, "Tous"));

    private static (string? Parent, string? Child) Sent(IRenderedComponent<LinkedFilters> filters) =>
        (filters.Find("input[type=hidden][name=publisherId]").GetAttribute("value"),
            filters.Find("input[type=hidden][name=publisherCollectionId]").GetAttribute("value"));

    // Types in the field, lets the typing pause until its suggestions are rendered, and chooses the first one.
    private async Task ChooseAsync(IRenderedComponent<LinkedFilters> filters, string field, string text)
    {
        await (await filters.TypeAsync(field, text, _clock)).EndAsync("The search of the text typed");
        Assert.NotEmpty(filters.FindAll("[role=option]"));
        await (await filters.RaiseAsync("[role=option]", option => option.ClickAsync(new MouseEventArgs()))).EndAsync("The choice of the suggestion");
    }

    private static HttpResponseMessage Json(string content) =>
        new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
