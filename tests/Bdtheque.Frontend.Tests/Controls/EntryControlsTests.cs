using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Presentation;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The controls of the entry forms, shared by every form of the administration (visuel/saisie-album/contenu.md, model of
/// the other entry forms), told apart by role, field and text, never by their look.
/// </summary>
public sealed class EntryControlsTests : BunitContext, IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly List<(HttpMethod Method, string Path, JsonElement? Body)> _sent = [];
    private Func<HttpRequestMessage, HttpResponseMessage> _answer = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

    public EntryControlsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Services.AddLocalization();
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<ValueLabels>();
        Services.AddSingleton(new ApiClient(new HttpClient(new StubHandler(async request =>
        {
            JsonElement? body = request.Content is null ? null : await request.Content.ReadFromJsonAsync<JsonElement>();
            lock (_sent)
                _sent.Add((request.Method, request.RequestUri!.PathAndQuery, body));
            return _answer(request);
        }))
        {
            BaseAddress = new Uri("http://api"),
        }));
        Services.AddScoped<RecordSearch>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void RecordTags_EachRemovable_NoneWithoutAny_AndAddOpensTheChoice()
    {
        RecordOption? removed = null;
        var hergé = new RecordOption(Guid.NewGuid(), "Hergé", null);
        var tags = Render<RecordTags>(parameters => parameters
            .Add(p => p.Id, "add-scenarist")
            .Add(p => p.Kind, RecordKind.Author)
            .Add(p => p.Items, [hergé])
            .Add(p => p.None, "Personne")
            .Add(p => p.AddText, "Ajouter")
            .Add(p => p.AddLabel, "Ajouter un auteur au scénario")
            .Add(p => p.OnRemove, option => removed = option)
            .Add(p => p.RemoveLabel, option => $"Retirer {option.Label} (Scénario)"));

        Assert.Equal("Hergé", tags.Find(".tag").TextContent.Trim());
        Assert.Empty(tags.FindAll(".record-tags-none"));
        tags.Find("button[aria-label='Retirer Hergé (Scénario)']").Click();
        Assert.Equal(hergé, removed);

        tags.Find("button[aria-label='Ajouter un auteur au scénario']").Click();

        var field = tags.Find("input[role=combobox]");
        Assert.Equal("add-scenarist", field.Id);
        Assert.Equal("Ajouter un auteur au scénario", field.GetAttribute("aria-label"));
        Assert.Empty(tags.FindAll(".record-tags-add"));

        // Left without a record chosen, the choice closes.
        field.Blur();
        Assert.Single(tags.FindAll(".record-tags-add"));
    }

    [Fact]
    public void RecordTags_WithoutAny_SaysNone()
    {
        var tags = Render<RecordTags>(parameters => parameters
            .Add(p => p.Id, "add-genre")
            .Add(p => p.Kind, RecordKind.Genre)
            .Add(p => p.Items, [])
            .Add(p => p.None, "Aucun")
            .Add(p => p.AddText, "Ajouter")
            .Add(p => p.AddLabel, "Ajouter un genre"));

        Assert.Equal("Aucun", tags.Find(".record-tags-none").TextContent);
        Assert.Empty(tags.FindAll(".tag"));
    }

    [Fact]
    public void InheritedTags_DashedAndNotRemovable_NothingWithoutAny()
    {
        var inherited = Render<InheritedTags>(parameters => parameters.Add(p => p.Label, "Par la série :").Add(p => p.Items, ["Aventure", "Polar"]));

        Assert.Equal(["Aventure", "Polar"], inherited.FindAll(".tag").Select(tag => tag.TextContent.Trim()));
        Assert.All(inherited.FindAll(".tag"), tag => Assert.Equal("dashed", tag.GetAttribute("data-variant")));
        Assert.Empty(inherited.FindAll(".tag-remove"));

        var none = Render<InheritedTags>(parameters => parameters.Add(p => p.Label, "Par la série :").Add(p => p.Items, []));
        Assert.Empty(none.Markup.Trim());
    }

    [Fact]
    public void RatingInput_FiveStarsAsRadioButtons_FilledUpToTheOneChosen()
    {
        AlbumRating? chosen = AlbumRating.Good;
        var rating = Render<RatingInput>(parameters => parameters
            .Add(p => p.Id, "rating")
            .Add(p => p.Legend, "Appréciation")
            .Add(p => p.Value, AlbumRating.Good)
            .Add(p => p.ValueChanged, value => chosen = value));

        var stars = rating.FindAll("input[type=radio][name=rating]");
        Assert.Equal(5, stars.Count);
        Assert.True(stars[3].HasAttribute("checked"));
        Assert.Equal(4, rating.FindAll(".rating-star[data-filled]").Count);
        Assert.Equal("Bien", rating.Find(".rating-name").TextContent);

        stars[4].Change(true);
        Assert.Equal(AlbumRating.VeryGood, chosen);
        rating.Find(".rating-clear").Click();
        Assert.Null(chosen);
    }

    [Fact]
    public void RatingInput_NotRated_HasNoRemoval()
    {
        var rating = Render<RatingInput>(parameters => parameters.Add(p => p.Id, "rating").Add(p => p.Legend, "Appréciation"));

        Assert.Equal("Pas encore noté", rating.Find(".rating-name").TextContent);
        Assert.Empty(rating.FindAll(".rating-clear"));
        Assert.Empty(rating.FindAll(".rating-star[data-filled]"));
    }

    [Fact]
    public void PartialDateInput_TheMonthsByName_AnUnknownMonth_AndTheYear()
    {
        int? month = 10;
        int? year = 2023;
        var date = Render<PartialDateInput>(parameters => parameters
            .Add(p => p.Id, "publication")
            .Add(p => p.Legend, "Première publication")
            .Add(p => p.Month, 10)
            .Add(p => p.Year, 2023)
            .Add(p => p.MonthChanged, value => month = value)
            .Add(p => p.YearChanged, value => year = value));

        var options = date.FindAll("#publication-month option");
        Assert.Equal(13, options.Count);
        Assert.Equal(("—", "octobre"), (options[0].TextContent, options[10].TextContent));
        Assert.True(options[10].HasAttribute("selected"));
        Assert.Equal("2023", date.Find("#publication-year").GetAttribute("value"));
        Assert.Equal("Première publication", date.Find("legend").TextContent);

        date.Find("#publication-month").Change("");
        date.Find("#publication-year").Change("1963");

        Assert.Equal((null, 1963), (month, year));
    }

    [Fact]
    public void PartialDateInput_InError_MarksBothFields()
    {
        var date = Render<PartialDateInput>(parameters => parameters
            .Add(p => p.Id, "publication")
            .Add(p => p.Legend, "Première publication")
            .Add(p => p.Error, "Le mois demande son année."));

        Assert.Equal("true", date.Find("#publication-month").GetAttribute("aria-invalid"));
        Assert.Equal("true", date.Find("#publication-year").GetAttribute("aria-invalid"));
        Assert.Equal("publication-error publication-help", date.Find("fieldset").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void TitleSortKeyField_WithoutTitle_TellsWhereTheRecordIsSorted()
    {
        var field = Render<TitleSortKeyField>(parameters => parameters
            .Add(p => p.Id, "sort-key")
            .Add(p => p.UntitledPlaceholder, "Sans titre, l'album est rangé à la clé de la série")
            .Add(p => p.UntitledHelp, "Sans titre propre, l'album est rangé à la clé de sa série."));

        Assert.Equal("Sans titre, l'album est rangé à la clé de la série", field.Find("#sort-key").GetAttribute("placeholder"));
        Assert.Equal("Sans titre propre, l'album est rangé à la clé de sa série.", field.Find("#sort-key-help").TextContent);
        Assert.Empty(_sent);
    }

    [Fact]
    public void TitleSortKeyField_FollowsTheTitle_ByAskingTheDomain()
    {
        _answer = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new TitleSortKey("Lotus bleu [Le]")) };
        var field = Render<TitleSortKeyField>(parameters => parameters
            .Add(p => p.Id, "sort-key")
            .Add(p => p.Title, "Tintin")
            .Add(p => p.StoredKey, "Tintin")
            .Add(p => p.UntitledPlaceholder, "—")
            .Add(p => p.UntitledHelp, "—"));
        Assert.Equal("Tintin", field.Find("#sort-key").GetAttribute("value"));

        field.Render(parameters => parameters.Add(p => p.Title, "Le Lotus bleu"));

        field.WaitForAssertion(() => Assert.Equal("Lotus bleu [Le]", field.Find("#sort-key").GetAttribute("value")));
        Assert.Equal("/admin/sort-keys/title?title=Le%20Lotus%20bleu", Assert.Single(_sent).Path);
    }

    [Fact]
    public void TitleSortKeyField_Manual_KeepsItsKeyWhateverTheTitle_UntilReset()
    {
        string? manual = "Castafiore";
        var field = Render<TitleSortKeyField>(parameters => parameters
            .Add(p => p.Id, "sort-key")
            .Add(p => p.Title, "Les Bijoux de la Castafiore")
            .Add(p => p.ManualKey, "Castafiore")
            .Add(p => p.ManualKeyChanged, value => manual = value)
            .Add(p => p.UntitledPlaceholder, "—")
            .Add(p => p.UntitledHelp, "—"));

        Assert.Equal(("Castafiore", "Manuelle"), (field.Find("#sort-key").GetAttribute("value"), field.Find(".sort-key-mode").TextContent));
        field.Find("#sort-key").Input("Bijoux");
        Assert.Equal("Bijoux", manual);

        field.Find(".sort-key-reset").Click();
        Assert.Null(manual);
    }

    [Fact]
    public void AuthorCreationDialog_SplitsTheNameTyped_AndCreatesTheAuthorCorrected()
    {
        var created = Guid.NewGuid();
        _answer = _ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new AuthorForm(created, "Van Hamme", "Jean", null, null, null, 1)),
        };
        AuthorListItem? author = null;
        var dialog = Render<AuthorCreationDialog>(parameters => parameters
            .Add(p => p.Id, "author-creation")
            .Add(p => p.Name, " Jean  Van Hamme ")
            .Add(p => p.OnCreated, value => author = value));

        Assert.Equal("Jean", dialog.Find("#author-creation-first-name").GetAttribute("value"));
        Assert.Equal("Van Hamme", dialog.Find("#author-creation-last-name").GetAttribute("value"));
        dialog.Find("#author-creation-pseudonym").Change("JVH");
        dialog.Find(".dialog-actions .button[data-variant=primary]").Click();

        dialog.WaitForAssertion(() => Assert.Equal(new AuthorListItem(created, "Van Hamme", "Jean", null), author));
        var request = Assert.Single(_sent);
        Assert.Equal((HttpMethod.Post, "/admin/authors"), (request.Method, request.Path));
        Assert.Equal(new CreateAuthorRequest("Van Hamme", "Jean", "JVH", null, null),
            request.Body!.Value.Deserialize<CreateAuthorRequest>(JsonSerializerOptions.Web));
    }

    [Fact]
    public void AuthorCreationDialog_OneWord_IsTheLastName_AndAnErrorKeepsItOpen()
    {
        _answer = _ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(
                new Dictionary<string, object> { ["type"] = Contracts.Errors.ProblemTypes.Business, ["status"] = 422, ["ruleCode"] = "Author.LastNameOrPseudonymRequired" },
                mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
        };
        var dialog = Render<AuthorCreationDialog>(parameters => parameters.Add(p => p.Id, "author-creation").Add(p => p.Name, "Moebius"));

        Assert.Null(dialog.Find("#author-creation-first-name").GetAttribute("value"));
        Assert.Equal("Moebius", dialog.Find("#author-creation-last-name").GetAttribute("value"));
        dialog.Find("#author-creation-last-name").Change("");
        dialog.Find(".dialog-actions .button[data-variant=primary]").Click();

        var error = dialog.WaitForElement("[role=alert]");
        Assert.Equal("business", error.GetAttribute("data-error-category"));
        Assert.Single(dialog.FindAll("dialog"));
    }

    [Fact]
    public void Notice_IsAStatusWithItsIcon()
    {
        var notice = Render<Notice>(parameters => parameters.Add(p => p.Icon, IconName.Copy).AddChildContent("Repris de la série Tintin."));

        Assert.Equal("status", notice.Find(".notice").GetAttribute("role"));
        Assert.Equal("Repris de la série Tintin.", notice.Find(".notice-text").TextContent);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => answer(request);
    }
}
