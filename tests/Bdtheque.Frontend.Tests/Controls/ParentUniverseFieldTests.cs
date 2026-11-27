using System.Globalization;
using System.Net;
using System.Text;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Entries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The parent of a universe, in its side panel (visuel/listes/saisie-selon-l-entite.md): chosen by typing its name, or created
/// on the spot by its name alone (choix-implementation/socle/frontend.md, création sur place, #414), each choice given to the
/// interactive panel, which holds it, as the test does here.
/// </summary>
public sealed class ParentUniverseFieldTests : BunitContext, IDisposable
{
    private static readonly UniverseListItem Pratt = new(Guid.Parse("0199b6a4-0000-7000-8000-000000000001"), "Univers Hugo Pratt", null, null);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    // The clock of the pause the field awaits before searching, moved on by the test.
    private readonly FakeTimeProvider _clock = new();
    private Func<HttpResponseMessage> _hierarchy = () => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(
            $$"""{"universe":{"id":"{{Pratt.Id}}","name":"{{Pratt.Name}}"},"ancestors":[]}""", Encoding.UTF8, "application/json"),
    };

    // What the panel holds, as the field gives it: the parent chosen, and the one created on the spot.
    private RecordOption? _chosen;
    private bool _changed;
    private NewUniverse? _created;

    public ParentUniverseFieldTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Services.AddLocalization();
        Services.AddSingleton<TimeProvider>(_clock);
        // The search finds a universe that does not contain the text typed of the tests, which none suits; the reading of its
        // ancestors answers what the test sets.
        Services.AddSingleton(new ApiClient(new HttpClient(new StubHandler(request => Task.FromResult(
            request.RequestUri!.AbsolutePath.EndsWith("/hierarchy", StringComparison.Ordinal)
                ? _hierarchy()
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $$"""{"items":[{"id":"{{Pratt.Id}}","name":"{{Pratt.Name}}"}],"number":1,"size":10,"totalCount":1}""",
                        Encoding.UTF8, "application/json"),
                })))
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
    public void AnExistingParent_IsPresentedWithItsAncestors()
    {
        var field = Open(parent: UniverseEntry.Option(new UniverseInHierarchy(Pratt, [])));

        Assert.Equal("Univers Hugo Pratt", field.Find("input[role=combobox]").GetAttribute("value"));
        Assert.Equal("Ses séries et albums sont aussi ceux de Univers Hugo Pratt.", field.Find(".field-help").TextContent.Trim());
    }

    [Fact]
    public async Task AParentChosen_IsGivenToThePanel_ThenWithItsAncestors()
    {
        var field = Open();

        await (await field.TypeAsync("input[role=combobox]", "Pratt", _clock)).EndAsync("The search of the text typed");
        await field.ClickAsync("[role=option]");

        Assert.Equal((Pratt.Id, "Univers Hugo Pratt"), (_chosen?.Id, _chosen?.Label));
        Assert.Null(_created);
    }

    [Fact]
    public async Task ACreation_OffersTheTextTyped_AsksItsNameAlone_ThenChoosesTheParentCreated()
    {
        var field = Open();

        await (await field.TypeAsync("input[role=combobox]", " Mers du Sud ", _clock)).EndAsync("The search of the text typed");
        Assert.Equal("Créer l'univers « Mers du Sud »", field.Find(".option-list-create").TextContent.Trim());
        await field.ClickAsync(".option-list-create");

        // The common dialog of a universe created on the spot: its name alone, the text typed.
        Assert.Equal("Nouvel univers", field.Find("dialog h2").TextContent.Trim());
        Assert.Equal(["Nom(obligatoire)"], field.FindAll("dialog label").Select(label => label.TextContent.Trim()));
        Assert.Equal("Mers du Sud", field.Find("dialog input#universe-parent-creation-name").GetAttribute("value"));
        await field.ClickAsync("dialog .dialog-actions .button[data-variant=primary]");

        Assert.Empty(field.FindAll("dialog"));
        Assert.Equal("Mers du Sud", field.Find("input[role=combobox]").GetAttribute("value"));
        Assert.Equal("Ses séries et albums sont aussi ceux de Mers du Sud.", field.Find(".field-help").TextContent.Trim());
        // Created only with the universe of the panel, which cites it by its key.
        Assert.Equal("Mers du Sud", _created?.Name);
        Assert.Equal(_created!.Key, _chosen?.Id);
    }

    [Fact]
    public async Task ACreationWithItsNameEmptied_IsStillChosen_ForTheSavingToRefuseIt()
    {
        // Told by its key, a parent whose name the user emptied is still a parent created on the spot, never no parent: the
        // saving refuses its name, rather than leaving the universe without its parent.
        var field = Open(parent: UniverseEntry.Option(new UniverseInHierarchy(Pratt, [])));
        await (await field.TypeAsync("input[role=combobox]", "Mers du Sud", _clock)).EndAsync("The search of the text typed");
        await field.ClickAsync(".option-list-create");

        await field.ChangeAsync("dialog input#universe-parent-creation-name", "  ");
        await field.ClickAsync("dialog .dialog-actions .button[data-variant=primary]");

        Assert.Equal("", _created?.Name);
        Assert.Equal(_created!.Key, _chosen?.Id);
    }

    [Fact]
    public async Task ACreationCancelled_LeavesTheParentAsItWas()
    {
        var field = Open(parent: UniverseEntry.Option(new UniverseInHierarchy(Pratt, [])));

        await (await field.TypeAsync("input[role=combobox]", "Mers du Sud", _clock)).EndAsync("The search of the text typed");
        await field.ClickAsync(".option-list-create");
        await field.ClickAsync("dialog [data-dialog-close]");

        Assert.Empty(field.FindAll("dialog"));
        Assert.False(_changed);
        Assert.Null(_created);
    }

    [Fact]
    public async Task AParentCreatedOnTheSpot_IsSuggestedAgain_RatherThanCreatedTwice()
    {
        var field = Open();
        await (await field.TypeAsync("input[role=combobox]", "Mers du Sud", _clock)).EndAsync("The search of the text typed");
        await field.ClickAsync(".option-list-create");
        await field.ClickAsync("dialog .dialog-actions .button[data-variant=primary]");
        // The panel renders the field anew with what it holds.
        field.Render(parameters => parameters.Add(p => p.Value, _chosen).Add(p => p.NewParent, _created));

        await field.ClickAsync("button.record-picker-clear");
        Assert.Null(_chosen);
        await (await field.TypeAsync("input[role=combobox]", "mers", _clock)).EndAsync("The search of the text typed");
        await field.ClickAsync(".option-list-pending");

        Assert.Equal((_created!.Key, "Mers du Sud"), (_chosen?.Id, _chosen?.Label));
    }

    [Fact]
    public void AParentCreatedOnTheSpotThatTheSavingRefused_IsPresentedAgain_WithItsError()
    {
        var sent = new NewUniverse(Guid.NewGuid(), "Ombrelune");
        var field = Open(parent: UniverseEntry.Option(sent), newParent: sent, error: "« Ombrelune » est déjà pris.");

        Assert.Equal("Ombrelune", field.Find("input[role=combobox]").GetAttribute("value"));
        Assert.Equal("true", field.Find("input[role=combobox]").GetAttribute("aria-invalid"));
        Assert.Equal("« Ombrelune » est déjà pris.", field.Find(".field-error").TextContent.Trim());
    }

    // Ancestors that cannot be read leave the parent named alone: the failure is said under the field, as any control says its
    // own (choix-implementation/socle/frontend.md, erreur dans un contrôle, #480), until another parent is chosen.
    [Fact]
    public async Task AParentWhoseAncestorsCannotBeRead_IsNamedAlone_AndTheFailureSaidUnderTheField()
    {
        _hierarchy = () => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var field = Open();

        await (await field.TypeAsync("input[role=combobox]", "Pratt", _clock)).EndAsync("The search of the text typed");
        await field.ClickAsync("[role=option]");

        Assert.Equal("Univers Hugo Pratt", field.Find("input[role=combobox]").GetAttribute("value"));
        Assert.Equal(Pratt.Id, _chosen?.Id);
        var failure = field.Find("#universe-parent-failure [role=alert]");
        Assert.Equal("technical", failure.GetAttribute("data-error-category"));
        Assert.True(failure.HasAttribute("data-error-compact"));
        Assert.Equal("La lecture des univers parents n'a pas abouti : l'univers est présenté par son nom seul.", failure.QuerySelector("[data-error-text]")!.TextContent.Trim());

        await field.ClickAsync("button.record-picker-clear");

        Assert.Empty(field.FindAll("#universe-parent-failure"));
    }

    private IRenderedComponent<ParentUniverseField> Open(RecordOption? parent = null, NewUniverse? newParent = null, string? error = null)
    {
        (_chosen, _created) = (parent, newParent);
        return Render<ParentUniverseField>(parameters => parameters
            .Add(p => p.Id, "universe-parent")
            .Add(p => p.Value, parent)
            .Add(p => p.ValueChanged, (RecordOption? chosen) =>
            {
                _chosen = chosen;
                _changed = true;
            })
            .Add(p => p.NewParent, newParent)
            .Add(p => p.NewParentChanged, (NewUniverse created) => _created = created)
            .Add(p => p.Error, error));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => answer(request);
    }
}
