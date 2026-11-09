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
            // Sent from the circuit, at the end of a pause, while the test watches it.
            lock (_requests)
                _requests.Add(request.RequestUri!.PathAndQuery);
            return _pending ?? Task.FromResult(_answer(request));
        }))
        {
            BaseAddress = new Uri("http://api"),
        }));
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton<Bdtheque.Frontend.Formatting.RecordLabels>();
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
    public void Typing_SuggestsTheRecordsContainingTheText()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        Type(picker, "ti n");

        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
        Assert.Equal("/catalog/series?q=ti%20n&pageSize=10", Assert.Single(_requests));
        Assert.Equal("true", picker.Find("input").GetAttribute("aria-expanded"));
        Assert.False(picker.Find("[role=listbox]").HasAttribute("hidden"));
        Assert.Equal(["Tintin", "Titeuf"], picker.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void Typing_SuggestsTheRecordsAddedOnTheSpotToo_UnderTheirKey()
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

        Type(picker, "nouveau");

        picker.WaitForAssertion(() => Assert.Single(picker.FindAll("[role=option]")));
        // Marked apart: its mention is not read as the end of the name.
        Assert.True(picker.Find("[role=option]").ClassList.Contains("record-picker-pending"));
        picker.FindAll("[role=option]")[0].Click();
        Assert.Equal(pending, chosen);
    }

    [Fact]
    public void ChoosingASuggestion_MakesItTheValue()
    {
        RecordOption? chosen = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.ValueChanged, value => chosen = value));

        Type(picker, "ti");
        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
        Assert.Empty(picker.FindAll(".record-picker-pending"));
        picker.FindAll("[role=option]")[1].Click();

        Assert.Equal(new RecordOption(Titeuf, "Titeuf", null), chosen);
        Assert.Equal("Titeuf", picker.Find("input").GetAttribute("value"));
        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Keyboard_MovesThroughTheSuggestions_AndChoosesTheActiveOne()
    {
        RecordOption? chosen = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.ValueChanged, value => chosen = value));

        Type(picker, "ti");
        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
        picker.Find("input").KeyDown("ArrowDown");
        picker.Find("input").KeyDown("ArrowDown");

        var active = picker.Find("input").GetAttribute("aria-activedescendant");
        Assert.Equal("true", picker.Find($"#{active}").GetAttribute("aria-selected"));
        Assert.Equal("Titeuf", picker.Find($"#{active}").TextContent.Trim());

        picker.Find("input").KeyDown("ArrowUp");
        picker.Find("input").KeyDown("Enter");

        Assert.Equal(Tintin, chosen?.Id);
    }

    [Fact]
    public void Creation_IsOfferedAfterTheSuggestions_WithTheTextTyped_ByTheMouse()
    {
        string? created = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-author")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.OnCreate, text => created = text)
            .Add(p => p.CreateLabel, text => $"Créer « {text} »"));

        Type(picker, " Jean Nouvel ");
        picker.WaitForAssertion(() => Assert.Equal(3, picker.FindAll("[role=option]").Count));
        var creation = picker.FindAll("[role=option]")[2];
        Assert.Equal("Créer « Jean Nouvel »", creation.TextContent.Trim());

        creation.Click();

        Assert.Equal("Jean Nouvel", created);
        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Creation_IsChosenByTheKeyboard_AfterTheLastSuggestion()
    {
        string? created = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-author")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.OnCreate, text => created = text)
            .Add(p => p.CreateLabel, text => $"Créer « {text} »"));

        Type(picker, "ti");
        picker.WaitForAssertion(() => Assert.Equal(3, picker.FindAll("[role=option]").Count));
        foreach (var _ in Enumerable.Range(0, 4))
            picker.Find("input").KeyDown("ArrowDown");
        picker.Find("input").KeyDown("Enter");

        Assert.Equal("ti", created);
    }

    [Fact]
    public void Creation_WithoutCaller_IsNeverOffered()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        Type(picker, "ti");

        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
        Assert.Empty(picker.FindAll(".record-picker-create"));
    }

    [Fact]
    public void SuggestionsOfAnEarlierText_AreNeitherShownNorChosen_WhileTheNewOnesAreAwaited()
    {
        RecordOption? chosen = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.ValueChanged, value => chosen = value));
        Type(picker, "ti");
        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
        picker.Find("input").KeyDown("ArrowDown");
        picker.Find("input").KeyDown("ArrowDown");

        var answer = new TaskCompletionSource<HttpResponseMessage>();
        _pending = answer.Task;
        Type(picker, "tin");

        Assert.Empty(picker.FindAll("[role=option]"));
        Assert.Null(picker.Find("input").GetAttribute("aria-activedescendant"));
        picker.Find("input").KeyDown("ArrowDown");

        answer.SetResult(Json($$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"}],"number":1,"size":10,"totalCount":1}"""));
        picker.WaitForAssertion(() => Assert.Single(picker.FindAll("[role=option]")));
        Assert.Null(picker.Find("input").GetAttribute("aria-activedescendant"));
        picker.Find("input").KeyDown("Enter");

        Assert.Null(chosen);
        picker.Find("input").KeyDown("ArrowDown");
        picker.WaitForAssertion(() => Assert.Equal("f-series-suggestion-0", picker.Find("input").GetAttribute("aria-activedescendant")));
        picker.Find("input").KeyDown("Enter");
        Assert.Equal(Tintin, chosen?.Id);
    }

    [Fact]
    public async Task AnswerToAnEarlierText_ArrivingLast_NeverReplacesTheSuggestionsOfTheTextShown()
    {
        // The circuit handles the next keystroke while the search of the earlier text is under way (#275).
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));
        var earlier = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = earlier.Task;
        var earlierSearch = picker.Find("input").InputAsync(new ChangeEventArgs { Value = "ti" });
        PauseUntil(() => { lock (_requests) return _requests.Count == 1; });
        _pending = null;
        _answer = _ => Json($$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"}],"number":1,"size":10,"totalCount":1}""");
        Type(picker, "tin");
        picker.WaitForAssertion(() => Assert.Equal(["Tintin"], picker.FindAll("[role=option]").Select(option => option.TextContent.Trim())));

        earlier.SetResult(Json($$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"},{"id":"{{Titeuf}}","title":"Titeuf"}],"number":1,"size":10,"totalCount":2}"""));
        await earlierSearch.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(["Tintin"], picker.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public async Task KeystrokesInQuickSuccession_SearchOnlyOnceTheTypingPauses()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        // Typed within a pause, the clock standing still. Each keystroke is found and raised on the circuit, the field
        // being rendered again between two of them (bUnit, InvokeAsync).
        foreach (var text in new[] { "t", "ti", "tin" })
            await picker.InvokeAsync(() => picker.Find("input").Input(text));
        _clock.Advance(RecordPicker.TypingPause - TimeSpan.FromMilliseconds(1));

        lock (_requests)
            Assert.Empty(_requests);
        // Until then, the suggestions are those of no text: the search of the text shown is under way.
        Assert.Equal("Recherche…", picker.Find("[role=listbox] .record-picker-status").TextContent.Trim());

        PauseUntil(() => { lock (_requests) return _requests.Count > 0; });

        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
        lock (_requests)
            Assert.Equal("/catalog/series?q=tin&pageSize=10", Assert.Single(_requests));
    }

    // Types the text, then pauses until the field has searched for it.
    private void Type(IRenderedComponent<RecordPicker> picker, string text)
    {
        int Sent() { lock (_requests) return _requests.Count; }
        var sent = Sent();
        // Found and raised on the circuit: the field may be rendered again meanwhile (bUnit, InvokeAsync).
        picker.InvokeAsync(() => picker.Find("input").Input(text)).Wait(TimeSpan.FromSeconds(5));
        PauseUntil(() => Sent() > sent);
    }

    // The typing pauses, as long as needed for what the field does at the end of a pause (search sent from the circuit,
    // apart from the test) to be done.
    private void PauseUntil(Func<bool> done) => Assert.True(
        SpinWait.SpinUntil(() =>
        {
            _clock.Advance(RecordPicker.TypingPause);
            return done();
        }, TimeSpan.FromSeconds(5)),
        "The field never searched once the typing paused.");

    [Fact]
    public void LeavingTheField_EndsTheSuggestionsOfTheTextLeft_NoneReopeningForTheRecordRestored()
    {
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.Value, new RecordOption(Tintin, "Tintin", null)));
        Type(picker, "tit");
        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));

        picker.Find("input").Blur();
        picker.Find("input").KeyDown("ArrowDown");

        Assert.Equal("Tintin", picker.Find("input").GetAttribute("value"));
        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
        Assert.Empty(picker.FindAll("[role=option]"));
    }

    [Fact]
    public void SearchAnsweredAfterLeavingTheField_SuggestsNothing()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));
        var answer = new TaskCompletionSource<HttpResponseMessage>();
        _pending = answer.Task;
        Type(picker, "ti");

        picker.Find("input").Blur();
        answer.SetResult(Json($$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"}],"number":1,"size":10,"totalCount":1}"""));
        picker.Find("input").KeyDown("ArrowDown");

        picker.WaitForAssertion(() => Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded")));
        Assert.Empty(picker.FindAll("[role=option]"));
    }

    [Fact]
    public void ChoosingASuggestion_EndsTheSuggestionsOfTheTextTyped()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));
        Type(picker, "ti");
        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));

        picker.FindAll("[role=option]")[1].Click();
        picker.Find("input").KeyDown("ArrowDown");

        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));
        Assert.Empty(picker.FindAll("[role=option]"));
    }

    [Fact]
    public void Escape_ClosesTheSuggestions_AndBlurRestoresTheRecordChosen()
    {
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.Value, new RecordOption(Tintin, "Tintin", null)));

        Type(picker, "tit");
        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
        picker.Find("input").KeyDown("Escape");
        Assert.Equal("false", picker.Find("input").GetAttribute("aria-expanded"));

        picker.Find("input").Blur();
        Assert.Equal("Tintin", picker.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Clearing_LeavesNoRecordChosen()
    {
        RecordOption? chosen = new(Tintin, "Tintin", null);
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.Name, "seriesId")
            .Add(p => p.Value, chosen)
            .Add(p => p.ValueChanged, value => chosen = value));

        picker.Find("button.record-picker-clear").Click();

        Assert.Null(chosen);
        Assert.Equal("", picker.Find("input#f-series").GetAttribute("value"));
        Assert.Equal("", picker.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Empty(picker.FindAll("button.record-picker-clear"));
    }

    [Fact]
    public void NoSuggestion_SaysSo()
    {
        _answer = _ => Json("""{"items":[],"number":1,"size":10,"totalCount":0}""");
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        Type(picker, "zz");

        picker.WaitForAssertion(() => Assert.Equal("Aucun résultat", picker.Find("[role=listbox] .record-picker-status").TextContent.Trim()));
        Assert.Empty(picker.FindAll("[role=option]"));
    }

    [Fact]
    public void FailedSearch_SaysSo_WithoutDisclosingItsCause()
    {
        _answer = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        Type(picker, "ti");

        picker.WaitForAssertion(() => Assert.Equal(
            "La recherche n'a pas abouti. Réessayez dans un instant.", picker.Find("[role=listbox] .record-picker-status").TextContent.Trim()));
    }

    [Fact]
    public void ErasingTheText_ClosesTheSuggestions_WithoutSearching()
    {
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        picker.Find("input").Input("   ");

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
