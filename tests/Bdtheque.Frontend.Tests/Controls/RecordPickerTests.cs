using System.Net;
using System.Text;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The choice of a record by typing its name (visuel/liste-albums/contenu.md, filtres ; visuel/saisie-album/contenu.md,
/// série) : a combobox whose suggestions are the records of the consultation that contain the text typed.
/// </summary>
public sealed class RecordPickerTests : BunitContext
{
    private static readonly Guid Tintin = Guid.Parse("0199b6a4-0000-7000-8000-000000000001");
    private static readonly Guid Titeuf = Guid.Parse("0199b6a4-0000-7000-8000-000000000002");

    // The clock of the pause the field awaits before searching, moved on by the test.
    private readonly FakeTimeProvider _clock = new();
    private readonly List<string> _requests = [];
    // Completed by the next request the field sends, from the circuit, apart from the test: taken before typing.
    private TaskCompletionSource _nextRequest = NewRequestSignal();
    // A response still awaited, until the test gives it; none, the answer is immediate.
    private Task<HttpResponseMessage>? _pending;
    private Func<HttpRequestMessage, HttpResponseMessage> _answer = _ => Json(
        $$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"},{"id":"{{Titeuf}}","title":"Titeuf"}],"number":1,"size":10,"totalCount":2}""");


    public RecordPickerTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<TimeProvider>(_clock);
        Services.AddSingleton(new ApiClient(new HttpClient(new StubHandler(request =>
        {
            // Sent from the circuit, at the end of a pause, apart from the test.
            lock (_requests)
                _requests.Add(request.RequestUri!.PathAndQuery);
            var answer = _pending ?? Task.FromResult(_answer(request));
            Interlocked.Exchange(ref _nextRequest, NewRequestSignal()).SetResult();
            return answer;
        }))
        {
            BaseAddress = new Uri("http://api"),
        }));
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton<Bdtheque.Frontend.Formatting.IsbnFormatter>().AddSingleton<Bdtheque.Frontend.Formatting.RecordLabels>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Picker_IsACombobox_ClosedAtFirst_ShowingTheRecordChosen()
    {
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.Value, new RecordOption(Tintin, "Tintin", null))
            .Add(p => p.Name, "seriesId")
            .Add(p => p.Placeholder, "Aucune série : album isolé"));

        var input = picker.Find("input#f-series");
        Assert.Equal("combobox", input.GetAttribute("role"));
        Assert.Equal("list", input.GetAttribute("aria-autocomplete"));
        Assert.Equal("false", input.GetAttribute("aria-expanded"));
        Assert.Equal("Tintin", input.GetAttribute("value"));
        Assert.Equal("Aucune série : album isolé", input.GetAttribute("placeholder"));
        Assert.True(picker.Find($"#{input.GetAttribute("aria-controls")}").HasAttribute("hidden"));
        // In a static form, the record chosen is sent by its identifier.
        Assert.Equal(Tintin.ToString(), picker.Find("input[type=hidden][name=seriesId]").GetAttribute("value"));
        Assert.Equal("Retirer « Tintin »", picker.Find("button.record-picker-clear").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Typing_SuggestsTheRecordsContainingTheText()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        await TypeAsync(picker, "ti n");

        Assert.Equal(2, picker.FindAll("[role=option]").Count);
        Assert.Equal("/catalog/series?q=ti%20n&pageSize=10", Assert.Single(_requests));
        Assert.Equal("true", picker.Find("input").GetAttribute("aria-expanded"));
        Assert.False(picker.Find("[role=listbox]").HasAttribute("hidden"));
        Assert.Equal(["Tintin", "Titeuf"], picker.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public async Task Typing_SuggestsTheRecordsAddedOnTheSpotToo_UnderTheirKey()
    {
        // An author added on the spot for a role is created with the record only: the choice for another role finds it
        // among the pending records, under the same key, rather than offering to create it twice.
        var pending = new RecordOption(Guid.NewGuid(), "Jean Nouveau", "nouvel auteur");
        RecordOption? chosen = null;
        _answer = _ => Json("""{"items":[],"number":1,"size":10,"totalCount":0}""");
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-author")
            .Add(p => p.Kind, RecordKind.Author)
            .Add(p => p.Pending, [pending, new RecordOption(Guid.NewGuid(), "Moebius", "nouvel auteur")])
            .Add(p => p.ValueChanged, value => chosen = value));

        await TypeAsync(picker, "nouveau");

        Assert.Single(picker.FindAll("[role=option]"));
        // Marked apart: its mention is not read as the end of the name.
        Assert.True(picker.Find("[role=option]").ClassList.Contains("record-picker-pending"));
        await picker.ClickAsync("#f-author-suggestion-0");
        Assert.Equal(pending, chosen);
    }

    [Fact]
    public async Task ChoosingASuggestion_MakesItTheValue()
    {
        RecordOption? chosen = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.ValueChanged, value => chosen = value));

        await TypeAsync(picker, "ti");
        Assert.Equal(2, picker.FindAll("[role=option]").Count);
        Assert.Empty(picker.FindAll(".record-picker-pending"));
        await picker.ClickAsync("#f-series-suggestion-1");

        Assert.Equal(new RecordOption(Titeuf, "Titeuf", null), chosen);
        Assert.Equal("Titeuf", picker.Find("input").GetAttribute("value"));
        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task Keyboard_MovesThroughTheSuggestions_AndChoosesTheActiveOne()
    {
        RecordOption? chosen = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.ValueChanged, value => chosen = value));

        await TypeAsync(picker, "ti");
        Assert.Equal(2, picker.FindAll("[role=option]").Count);
        await picker.KeyDownAsync("input", "ArrowDown");
        await picker.KeyDownAsync("input", "ArrowDown");

        var active = picker.Find("input").GetAttribute("aria-activedescendant");
        Assert.Equal("true", picker.Find($"#{active}").GetAttribute("aria-selected"));
        Assert.Equal("Titeuf", picker.Find($"#{active}").TextContent.Trim());

        await picker.KeyDownAsync("input", "ArrowUp");
        await picker.KeyDownAsync("input", "Enter");

        Assert.Equal(Tintin, chosen?.Id);
    }

    [Fact]
    public async Task Creation_IsOfferedAfterTheSuggestions_WithTheTextTyped_ByTheMouse()
    {
        string? created = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-author")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.OnCreate, text => created = text)
            .Add(p => p.CreateLabel, text => $"Créer « {text} »"));

        await TypeAsync(picker, " Jean Nouvel ");
        Assert.Equal(3, picker.FindAll("[role=option]").Count);
        var creation = picker.FindAll("[role=option]")[2];
        Assert.Equal("Créer « Jean Nouvel »", creation.TextContent.Trim());

        await picker.ClickAsync(".record-picker-create");

        Assert.Equal("Jean Nouvel", created);
        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task Creation_IsChosenByTheKeyboard_AfterTheLastSuggestion()
    {
        string? created = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-author")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.OnCreate, text => created = text)
            .Add(p => p.CreateLabel, text => $"Créer « {text} »"));

        await TypeAsync(picker, "ti");
        Assert.Equal(3, picker.FindAll("[role=option]").Count);
        foreach (var _ in Enumerable.Range(0, 4))
            await picker.KeyDownAsync("input", "ArrowDown");
        await picker.KeyDownAsync("input", "Enter");

        Assert.Equal("ti", created);
    }

    [Fact]
    public async Task Creation_WithoutCaller_IsNeverOffered()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        await TypeAsync(picker, "ti");

        Assert.Equal(2, picker.FindAll("[role=option]").Count);
        Assert.Empty(picker.FindAll(".record-picker-create"));
    }

    [Fact]
    public async Task SuggestionsOfAnEarlierText_AreNeitherShownNorChosen_WhileTheNewOnesAreAwaited()
    {
        RecordOption? chosen = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.ValueChanged, value => chosen = value));
        await TypeAsync(picker, "ti");
        Assert.Equal(2, picker.FindAll("[role=option]").Count);
        await picker.KeyDownAsync("input", "ArrowDown");
        await picker.KeyDownAsync("input", "ArrowDown");

        var answer = new TaskCompletionSource<HttpResponseMessage>();
        _pending = answer.Task;
        var search = await TypeUntilSentAsync(picker, "tin");

        Assert.Empty(picker.FindAll("[role=option]"));
        Assert.Null(picker.Find("input").GetAttribute("aria-activedescendant"));
        await picker.KeyDownAsync("input", "ArrowDown");

        answer.SetResult(Json($$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"}],"number":1,"size":10,"totalCount":1}"""));
        await search.EndAsync("The search of the text typed");
        Assert.Single(picker.FindAll("[role=option]"));
        Assert.Null(picker.Find("input").GetAttribute("aria-activedescendant"));
        await picker.KeyDownAsync("input", "Enter");

        Assert.Null(chosen);
        await picker.KeyDownAsync("input", "ArrowDown");
        Assert.Equal("f-series-suggestion-0", picker.Find("input").GetAttribute("aria-activedescendant"));
        await picker.KeyDownAsync("input", "Enter");
        Assert.Equal(Tintin, chosen?.Id);
    }

    [Fact]
    public async Task AnswerToAnEarlierText_ArrivingLast_NeverReplacesTheSuggestionsOfTheTextShown()
    {
        // The circuit handles the next keystroke while the search of the earlier text is under way (#275).
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));
        var earlier = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = earlier.Task;
        var earlierSearch = await TypeUntilSentAsync(picker, "ti");
        _pending = null;
        _answer = _ => Json($$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"}],"number":1,"size":10,"totalCount":1}""");
        await TypeAsync(picker, "tin");
        Assert.Equal(["Tintin"], picker.FindAll("[role=option]").Select(option => option.TextContent.Trim()));

        earlier.SetResult(Json($$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"},{"id":"{{Titeuf}}","title":"Titeuf"}],"number":1,"size":10,"totalCount":2}"""));
        await earlierSearch.EndAsync("The search of the earlier text");

        Assert.Equal(["Tintin"], picker.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public async Task KeystrokesInQuickSuccession_SearchOnlyOnceTheTypingPauses()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        // Typed within a pause, the clock standing still: each keystroke awaits its pause on the clock once raised.
        var keystrokes = new List<Task>();
        foreach (var text in new[] { "t", "ti", "tin" })
            keystrokes.Add(await picker.RaiseAsync("input", field => field.InputAsync(new ChangeEventArgs { Value = text })));
        _clock.Advance(RecordPicker.TypingPause - TimeSpan.FromMilliseconds(1));

        lock (_requests)
            Assert.Empty(_requests);
        // Until then, the suggestions are those of no text: the search of the text shown is under way.
        Assert.Equal("Recherche…", picker.Find("[role=listbox] .record-picker-status").TextContent.Trim());

        _clock.Advance(TimeSpan.FromMilliseconds(1));
        await Task.WhenAll(keystrokes).EndAsync("The search of the text typed");

        Assert.Equal(2, picker.FindAll("[role=option]").Count);
        lock (_requests)
            Assert.Equal("/catalog/series?q=tin&pageSize=10", Assert.Single(_requests));
    }

    // Types the text, and lets the typing pause until the field has searched it and rendered its suggestions.
    private async Task TypeAsync(IRenderedComponent<RecordPicker> picker, string text) =>
        await (await picker.TypeAsync("input", text, _clock)).EndAsync("The search of the text typed");

    // Types the text, and lets the typing pause until the field has sent its search, whose answer the test may hold.
    // Returns the task of the keystroke, which ends with the search.
    private async Task<Task> TypeUntilSentAsync(IRenderedComponent<RecordPicker> picker, string text)
    {
        var sent = Volatile.Read(ref _nextRequest).Task;
        var keystroke = await picker.TypeAsync("input", text, _clock);
        await sent.EndAsync("Sending the search of the text typed");
        return keystroke;
    }

    private static TaskCompletionSource NewRequestSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    [Fact]
    public async Task LeavingTheField_EndsTheSuggestionsOfTheTextLeft_NoneReopeningForTheRecordRestored()
    {
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.Value, new RecordOption(Tintin, "Tintin", null)));
        await TypeAsync(picker, "tit");
        Assert.Equal(2, picker.FindAll("[role=option]").Count);

        await picker.BlurAsync("input");
        await picker.KeyDownAsync("input", "ArrowDown");

        Assert.Equal("Tintin", picker.Find("input").GetAttribute("value"));
        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
        Assert.Empty(picker.FindAll("[role=option]"));
    }

    [Fact]
    public async Task SearchAnsweredAfterLeavingTheField_SuggestsNothing()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));
        var answer = new TaskCompletionSource<HttpResponseMessage>();
        _pending = answer.Task;
        var search = await TypeUntilSentAsync(picker, "ti");

        await picker.BlurAsync("input");
        answer.SetResult(Json($$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"}],"number":1,"size":10,"totalCount":1}"""));
        await search.EndAsync("The search of the text left");
        await picker.KeyDownAsync("input", "ArrowDown");

        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
        Assert.Empty(picker.FindAll("[role=option]"));
    }

    [Fact]
    public async Task ChoosingASuggestion_EndsTheSuggestionsOfTheTextTyped()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));
        await TypeAsync(picker, "ti");
        Assert.Equal(2, picker.FindAll("[role=option]").Count);

        await picker.ClickAsync("#f-series-suggestion-1");
        await picker.KeyDownAsync("input", "ArrowDown");

        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
        Assert.Empty(picker.FindAll("[role=option]"));
    }

    [Fact]
    public async Task Escape_ClosesTheSuggestions_AndBlurRestoresTheRecordChosen()
    {
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.Value, new RecordOption(Tintin, "Tintin", null)));

        await TypeAsync(picker, "tit");
        Assert.Equal(2, picker.FindAll("[role=option]").Count);
        await picker.KeyDownAsync("input", "Escape");
        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));

        await picker.BlurAsync("input");
        Assert.Equal("Tintin", picker.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task Clearing_LeavesNoRecordChosen()
    {
        RecordOption? chosen = new(Tintin, "Tintin", null);
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.Name, "seriesId")
            .Add(p => p.Value, chosen)
            .Add(p => p.ValueChanged, value => chosen = value));

        await picker.ClickAsync("button.record-picker-clear");

        Assert.Null(chosen);
        Assert.Equal("", picker.Find("input#f-series").GetAttribute("value"));
        Assert.Equal("", picker.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Empty(picker.FindAll("button.record-picker-clear"));
    }

    [Fact]
    public async Task NoSuggestion_SaysSo()
    {
        _answer = _ => Json("""{"items":[],"number":1,"size":10,"totalCount":0}""");
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        await TypeAsync(picker, "zz");

        Assert.Equal("Aucun résultat", picker.Find("[role=listbox] .record-picker-status").TextContent.Trim());
        Assert.Empty(picker.FindAll("[role=option]"));
    }

    [Fact]
    public async Task FailedSearch_SaysSo_WithoutDisclosingItsCause()
    {
        _answer = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        await TypeAsync(picker, "ti");

        Assert.Equal("La recherche n'a pas abouti. Réessayez dans un instant.", picker.Find("[role=listbox] .record-picker-status").TextContent.Trim());
    }

    [Fact]
    public async Task ErasingTheText_ClosesTheSuggestions_WithoutSearching()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        await (await picker.RaiseAsync("input", field => field.InputAsync(new ChangeEventArgs { Value = "   " }))).EndAsync("Erasing the text");

        Assert.Empty(_requests);
        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            answer(request);
    }
}
