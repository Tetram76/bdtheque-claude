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
        Services.AddSingleton<Bdtheque.Frontend.Formatting.IsbnFormatter>().AddSingleton<Bdtheque.Frontend.Formatting.RecordLabels>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public async Task RecordTags_EachRemovable_AndAddOpensTheChoice()
    {
        RecordOption? removed = null;
        var hergé = new RecordOption(Guid.NewGuid(), "Hergé", null);
        var tags = Render<RecordTags>(parameters => parameters
            .Add(p => p.Id, "add-scenarist")
            .Add(p => p.Kind, RecordKind.Author)
            .Add(p => p.Items, [hergé])
            .Add(p => p.AddText, "Ajouter")
            .Add(p => p.AddLabel, "Ajouter un auteur au scénario")
            .Add(p => p.OnRemove, option => removed = option)
            .Add(p => p.RemoveLabel, option => $"Retirer {option.Label} (Scénario)"));

        Assert.Equal("Hergé", tags.Find(".tag").TextContent.Trim());
        await tags.ClickAsync("button[aria-label='Retirer Hergé (Scénario)']");
        Assert.Equal(hergé, removed);

        await tags.ClickAsync("button[aria-label='Ajouter un auteur au scénario']");

        var field = tags.Find("input[role=combobox]");
        Assert.Equal("add-scenarist", field.Id);
        Assert.Equal("Ajouter un auteur au scénario", field.GetAttribute("aria-label"));
        Assert.Empty(tags.FindAll(".record-tags-add"));

        // Left without a record chosen, the choice closes.
        await tags.BlurAsync("input[role=combobox]");
        Assert.Single(tags.FindAll(".record-tags-add"));
    }

    [Fact]
    public void RecordTags_WithoutAny_OnlyTheAddition()
    {
        var tags = Render<RecordTags>(parameters => parameters
            .Add(p => p.Id, "add-genre")
            .Add(p => p.Kind, RecordKind.Genre)
            .Add(p => p.Items, [])
            .Add(p => p.AddText, "Ajouter")
            .Add(p => p.AddLabel, "Ajouter un genre"));

        Assert.Empty(tags.FindAll(".tag"));
        Assert.Equal("Ajouter", tags.Find(".record-tags").TextContent.Trim());
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
    public async Task RatingInput_FiveStarsAsRadioButtons_FilledUpToTheOneChosen()
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

        await rating.ChangeAsync("input[type=radio][name=rating][value=VeryGood]", true);
        Assert.Equal(AlbumRating.VeryGood, chosen);
        await rating.ClickAsync(".rating-clear");
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
    public async Task PartialDateInput_TheMonthsByName_AnUnknownMonth_AndTheYear()
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

        await date.ChangeAsync("#publication-month", "");
        await date.ChangeAsync("#publication-year", "1963");

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
    public async Task TitleSortKeyField_Manual_KeepsItsKeyWhateverTheTitle_UntilReset()
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
        await field.InputAsync("#sort-key", "Bijoux");
        Assert.Equal("Bijoux", manual);

        await field.ClickAsync(".sort-key-reset");
        Assert.Null(manual);
    }

    [Theory]
    [InlineData("Tintin", null)]
    [InlineData("Tintin", "Castafiore")]
    [InlineData(null, null)]
    public void TitleSortKeyField_IsRequired_InEitherMode_WithOrWithoutTitle(string? title, string? manualKey)
    {
        // The field always shows a key, computed, entered or that of the series (visuel/saisie-album/contenu.md, #465): its
        // mention is that of the shared field, never its own.
        var field = Render<TitleSortKeyField>(parameters => parameters
            .Add(p => p.Id, "sort-key")
            .Add(p => p.Title, title)
            .Add(p => p.StoredKey, title)
            .Add(p => p.ManualKey, manualKey)
            .Add(p => p.UntitledPlaceholder, "—")
            .Add(p => p.UntitledHelp, "—"));

        var label = field.Find("label[for=sort-key]");
        Assert.Equal("(obligatoire)", label.QuerySelector(".field-note")?.TextContent);
        Assert.Null(field.Find("#sort-key").GetAttribute("aria-required"));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("  ", true)]
    [InlineData("Tintin", false)]
    public void TitleSortKeyField_ReadOnly_UntilATitleIsEntered(string? title, bool readOnly)
    {
        // An album without a title of its own stays sorted by the key of its series (visuel/saisie-album/contenu.md, #465).
        var field = Render<TitleSortKeyField>(parameters => parameters
            .Add(p => p.Id, "sort-key")
            .Add(p => p.Title, title)
            .Add(p => p.StoredKey, string.IsNullOrWhiteSpace(title) ? null : title)
            .Add(p => p.UntitledPlaceholder, "—")
            .Add(p => p.UntitledHelp, "—"));

        Assert.Equal(readOnly, field.Find("#sort-key").HasAttribute("readonly"));
    }

    [Fact]
    public void TitleSortKeyField_InError_PresentsItAsEveryField()
    {
        var field = Render<TitleSortKeyField>(parameters => parameters
            .Add(p => p.Id, "sort-key")
            .Add(p => p.Title, "Tintin")
            .Add(p => p.ManualKey, "")
            .Add(p => p.UntitledPlaceholder, "—")
            .Add(p => p.UntitledHelp, "—")
            .Add(p => p.Error, "La clé de tri ne peut pas être vide."));

        var input = field.Find("#sort-key");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal("sort-key-error sort-key-help", input.GetAttribute("aria-describedby"));
        Assert.Equal("La clé de tri ne peut pas être vide.", field.Find("p.field-error#sort-key-error").TextContent);
    }

    [Fact]
    public async Task AuthorCreationDialog_SplitsTheNameTyped_AndGivesTheAuthorCorrected_WithoutCreatingIt()
    {
        // The author is created with the record that cites it, when that record is saved (choix-implementation/socle/frontend.md).
        NewAuthor? author = null;
        var dialog = Render<AuthorCreationDialog>(parameters => parameters
            .Add(p => p.Id, "author-creation")
            .Add(p => p.Name, " Jean  Van Hamme ")
            .Add(p => p.OnCreated, value => author = value));

        Assert.Equal("Jean", dialog.Find("#author-creation-first-name").GetAttribute("value"));
        Assert.Equal("Van Hamme", dialog.Find("#author-creation-last-name").GetAttribute("value"));
        await dialog.ChangeAsync("#author-creation-pseudonym", "JVH");
        await dialog.ClickAsync(".dialog-actions .button[data-variant=primary]");

        Assert.Equal(("Van Hamme", "Jean", "JVH"), (author!.LastName, author.FirstName, author.Pseudonym));
        Assert.NotEqual(Guid.Empty, author.Key);
        Assert.Empty(_sent);
    }

    [Fact]
    public void AuthorCreationDialog_OneWord_IsTheLastName()
    {
        var dialog = Render<AuthorCreationDialog>(parameters => parameters.Add(p => p.Id, "author-creation").Add(p => p.Name, "Moebius"));

        Assert.Null(dialog.Find("#author-creation-first-name").GetAttribute("value"));
        Assert.Equal("Moebius", dialog.Find("#author-creation-last-name").GetAttribute("value"));
    }

    [Fact]
    public async Task UniverseCreationDialog_ItsNameAlone_Required_TheTextTyped_ForAUniverseAtTheTop()
    {
        // A universe created on the spot asks for its name only, never its parent: it is created at the top of the hierarchy,
        // and attached to a parent from its own panel (choix-implementation/socle/frontend.md, création sur place, #414).
        NewUniverse? created = null;
        var dialog = Render<UniverseCreationDialog>(parameters => parameters
            .Add(p => p.Id, "universe-creation")
            .Add(p => p.Name, " Le port ")
            .Add(p => p.OnCreated, (NewUniverse universe) => created = universe));

        Assert.Equal("Nouvel univers", dialog.Find("dialog h2").TextContent.Trim());
        Assert.Equal(["Nom(obligatoire)"], dialog.FindAll("dialog label").Select(l => l.TextContent.Trim()));
        Assert.Empty(dialog.FindAll("[role=combobox]"));
        Assert.Equal("Le port", dialog.Find("#universe-creation-name").GetAttribute("value"));
        await dialog.ClickAsync(".dialog-actions .button[data-variant=primary]");

        Assert.Equal("Le port", created!.Name);
        Assert.NotEqual(Guid.Empty, created.Key);
        Assert.Empty(_sent);
    }

    [Fact]
    public void GenreCreationDialog_ItsLabelRequired_AsInTheListOfGenres()
    {
        var dialog = Render<GenreCreationDialog>(parameters => parameters.Add(p => p.Id, "genre-creation").Add(p => p.Label, "Polar"));

        Assert.Equal("(obligatoire)", dialog.Find("label[for=genre-creation-name] .field-note").TextContent);
    }

    [Fact]
    public async Task SeriesCreationDialog_ItsTitleAlone_Required_TheTextTyped_ForTheSeriesToCreate()
    {
        // A series created on the spot asks for what it requires only, its title (visuel/saisie-album/contenu.md, #414).
        NewSeries? created = null;
        var dialog = Render<SeriesCreationDialog>(parameters => parameters
            .Add(p => p.Id, "series-creation").Add(p => p.Text, " Les Vieux Fourneaux ")
            .Add(p => p.OnCreated, (NewSeries series) => created = series));

        Assert.Equal("Nouvelle série", dialog.Find("dialog h2").TextContent.Trim());
        Assert.Equal(["Titre(obligatoire)"], dialog.FindAll("dialog label").Select(l => l.TextContent.Trim()));
        Assert.Equal("Les Vieux Fourneaux", dialog.Find("#series-creation-name").GetAttribute("value"));
        await dialog.ClickAsync(".dialog-actions .button[data-variant=primary]");

        Assert.Equal("Les Vieux Fourneaux", created!.Title);
        Assert.Equal("Ajouter la série", dialog.Find(".dialog-actions .button[data-variant=primary]").TextContent.Trim());
    }

    [Fact]
    public void AuthorCreationDialog_NoMention_NeitherNameNorPseudonymBeingRequiredAlone()
    {
        var dialog = Render<AuthorCreationDialog>(parameters => parameters.Add(p => p.Id, "author-creation").Add(p => p.Name, "Moebius"));

        Assert.Empty(dialog.FindAll(".field-note"));
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
