using System.Net;
using System.Text;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The choice of a record by typing its name (visuel/liste-albums.md § Contenu, filtres ; visuel/saisie-album.md § Contenu,
/// série) : a combobox whose suggestions are the records of the consultation that contain the text typed.
/// </summary>
public sealed class RecordPickerTests : BunitContext
{
    private static readonly Guid Tintin = Guid.Parse("0199b6a4-0000-7000-8000-000000000001");
    private static readonly Guid Titeuf = Guid.Parse("0199b6a4-0000-7000-8000-000000000002");

    private readonly List<string> _requests = [];
    // A response still awaited, until the test gives it; none, the answer is immediate.
    private Task<HttpResponseMessage>? _pending;
    private Func<HttpRequestMessage, HttpResponseMessage> _answer = _ => Json(
        $$"""{"items":[{"id":"{{Tintin}}","title":"Tintin"},{"id":"{{Titeuf}}","title":"Titeuf"}],"number":1,"size":10,"totalCount":2}""");

    public RecordPickerTests()
    {
        Services.AddLocalization();
        Services.AddSingleton(new ApiClient(new HttpClient(new StubHandler(request =>
        {
            _requests.Add(request.RequestUri!.PathAndQuery);
            return _pending ?? Task.FromResult(_answer(request));
        }))
        {
            BaseAddress = new Uri("http://api"),
        }));
        Services.AddScoped<RecordSearch>();
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

        picker.Find("input").Input("ti n");

        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
        Assert.Equal("/catalog/series?q=ti%20n&pageSize=10", Assert.Single(_requests));
        Assert.Equal("true", picker.Find("input").GetAttribute("aria-expanded"));
        Assert.False(picker.Find("[role=listbox]").HasAttribute("hidden"));
        Assert.Equal(["Tintin", "Titeuf"], picker.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void ChoosingASuggestion_MakesItTheValue()
    {
        RecordOption? chosen = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.ValueChanged, value => chosen = value));

        picker.Find("input").Input("ti");
        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
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
        var input = picker.Find("input");

        input.Input("ti");
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
    public void SuggestionsOfAnEarlierText_AreNeitherShownNorChosen_WhileTheNewOnesAreAwaited()
    {
        RecordOption? chosen = null;
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.ValueChanged, value => chosen = value));
        picker.Find("input").Input("ti");
        picker.WaitForAssertion(() => Assert.Equal(2, picker.FindAll("[role=option]").Count));
        picker.Find("input").KeyDown("ArrowDown");
        picker.Find("input").KeyDown("ArrowDown");

        var answer = new TaskCompletionSource<HttpResponseMessage>();
        _pending = answer.Task;
        picker.Find("input").Input("tin");

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
    public void LeavingTheField_EndsTheSuggestionsOfTheTextLeft_NoneReopeningForTheRecordRestored()
    {
        var picker = Render<RecordPicker>(parameters => parameters
            .Add(p => p.Id, "f-series")
            .Add(p => p.Kind, RecordKind.Series)
            .Add(p => p.Value, new RecordOption(Tintin, "Tintin", null)));
        picker.Find("input").Input("tit");
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
        picker.Find("input").Input("ti");

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
        picker.Find("input").Input("ti");
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

        picker.Find("input").Input("tit");
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

        picker.Find("input").Input("zz");

        picker.WaitForAssertion(() => Assert.Equal("Aucun résultat", picker.Find("[role=listbox] .record-picker-status").TextContent.Trim()));
        Assert.Empty(picker.FindAll("[role=option]"));
    }

    [Fact]
    public void FailedSearch_SaysSo_WithoutDisclosingItsCause()
    {
        _answer = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var picker = Render<RecordPicker>(parameters => parameters.Add(p => p.Id, "f-series").Add(p => p.Kind, RecordKind.Series));

        picker.Find("input").Input("ti");

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
