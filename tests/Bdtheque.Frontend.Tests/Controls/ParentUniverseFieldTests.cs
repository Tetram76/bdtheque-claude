using System.Globalization;
using System.Net;
using System.Text;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The parent of a universe, in its side panel (visuel/listes/saisie-selon-l-entite.md): chosen by typing its name, or created
/// on the spot by its name alone (choix-implementation/socle/frontend.md, création sur place, #414), and sent with the static
/// form of the panel, by its identifier or, created on the spot, by its name.
/// </summary>
public sealed class ParentUniverseFieldTests : BunitContext, IDisposable
{
    private static readonly UniverseListItem Pratt = new(Guid.Parse("0199b6a4-0000-7000-8000-000000000001"), "Univers Hugo Pratt", null, null);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    // The clock of the pause the field awaits before searching, moved on by the test.
    private readonly FakeTimeProvider _clock = new();

    public ParentUniverseFieldTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Services.AddLocalization();
        Services.AddSingleton<TimeProvider>(_clock);
        // The search finds a universe that does not contain the text typed of the tests, which none suits.
        Services.AddSingleton(new ApiClient(new HttpClient(new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
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
    public void AnExistingParent_IsSentByItsIdentifier()
    {
        var field = Open(parent: new UniverseInHierarchy(Pratt, []));

        Assert.Equal("Univers Hugo Pratt", field.Find("input[role=combobox]").GetAttribute("value"));
        Assert.Equal((Pratt.Id.ToString(), ""), Sent(field));
    }

    [Fact]
    public async Task ACreation_OffersTheTextTyped_AsksItsNameAlone_ThenSendsTheParentByItsName()
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
        // Created only with the universe of the panel: sent by its name, without an identifier.
        Assert.Equal(("", "Mers du Sud"), Sent(field));
    }

    [Fact]
    public async Task ACreationCancelled_LeavesTheParentAsItWas()
    {
        var field = Open(parent: new UniverseInHierarchy(Pratt, []));

        await (await field.TypeAsync("input[role=combobox]", "Mers du Sud", _clock)).EndAsync("The search of the text typed");
        await field.ClickAsync(".option-list-create");
        await field.ClickAsync("dialog [data-dialog-close]");

        Assert.Empty(field.FindAll("dialog"));
        Assert.Equal((Pratt.Id.ToString(), ""), Sent(field));
    }

    [Fact]
    public async Task AParentCreatedOnTheSpot_IsSuggestedAgain_RatherThanCreatedTwice()
    {
        var field = Open();
        await (await field.TypeAsync("input[role=combobox]", "Mers du Sud", _clock)).EndAsync("The search of the text typed");
        await field.ClickAsync(".option-list-create");
        await field.ClickAsync("dialog .dialog-actions .button[data-variant=primary]");

        await field.ClickAsync("button.record-picker-clear");
        Assert.Equal(("", ""), Sent(field));
        await (await field.TypeAsync("input[role=combobox]", "mers", _clock)).EndAsync("The search of the text typed");
        await field.ClickAsync(".option-list-pending");

        Assert.Equal(("", "Mers du Sud"), Sent(field));
    }

    [Fact]
    public void AParentCreatedOnTheSpotThatTheSavingRefused_IsPresentedAgain_WithItsError()
    {
        // The form sent back with the error of its saving: the name of the parent created on the spot, which the page passes back.
        var field = Open(newParent: "Ombrelune", error: "« Ombrelune » est déjà pris.");

        Assert.Equal("Ombrelune", field.Find("input[role=combobox]").GetAttribute("value"));
        Assert.Equal("true", field.Find("input[role=combobox]").GetAttribute("aria-invalid"));
        Assert.Equal("« Ombrelune » est déjà pris.", field.Find(".field-error").TextContent.Trim());
        Assert.Equal(("", "Ombrelune"), Sent(field));
    }

    private IRenderedComponent<ParentUniverseField> Open(UniverseInHierarchy? parent = null, string? newParent = null, string? error = null) =>
        Render<ParentUniverseField>(parameters => parameters
            .Add(p => p.Id, "universe-parent")
            .Add(p => p.Name, "parentId")
            .Add(p => p.NewName, "newParent")
            .Add(p => p.Parent, parent)
            .Add(p => p.NewParent, newParent)
            .Add(p => p.Error, error));

    // What the static form of the panel sends: the identifier of an existing parent, the name of a parent created on the spot.
    private static (string? ParentId, string? NewParent) Sent(IRenderedComponent<ParentUniverseField> field) =>
        (field.Find("input[type=hidden][name=parentId]").GetAttribute("value"), field.Find("input[type=hidden][name=newParent]").GetAttribute("value"));

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => answer(request);
    }
}
