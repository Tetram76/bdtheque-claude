extern alias frontend;

using frontend::Bdtheque.Frontend.Components.Presentation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bdtheque.EndToEnd.Tests.Theming;

/// <summary>
/// A page of the tests only, which presents several blocks side by side and one above the other, titled by a statement,
/// a question or an exclamation, before any screen lays out so many: what a theme draws on its blocks at random (tints,
/// edges, balloons of « La planche encrée », themes/planche.js) is fixed on it, and its look in each theme by its captures.
/// Routed by the application through <c>PageAssemblies</c>; written in C#, the alias of the frontend not being reachable
/// from Razor.
/// </summary>
[Route(Path)]
public sealed class CasesPage : ComponentBase
{
    public const string Path = "/test/cases";

    /// <summary>The titles of the blocks: statements, questions and exclamations, as the screens title their blocks.</summary>
    public static readonly string[] Titles =
    [
        "Saisir à partir d'un ISBN",
        "Tout juste arrivés !",
        "Ça vaut combien, tout ça ?",
        "Les fiches",
        "Plutôt quel genre ?",
        "À la une aujourd'hui !",
        "Chez quels éditeurs ?",
        "Dernières modifications",
    ];

    /// <summary>The mention above each title, which only some themes show (e.g. « La gazette »).</summary>
    public static readonly string[] Kickers = ["Saisie", "Arrivages", "Valeur", "Fiches", "Genres", "À la une", "Éditeurs", "Journal"];

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "h1");
        builder.AddContent(1, "Cases");
        builder.CloseElement();

        builder.OpenElement(10, "div");
        builder.AddAttribute(11, "style", "display: grid; grid-template-columns: repeat(auto-fit, minmax(20rem, 1fr)); gap: 1.5rem 1rem; align-items: start");
        for (var index = 0; index < Titles.Length; index++)
        {
            var title = Titles[index];
            builder.OpenComponent<Block>(20);
            builder.SetKey(title);
            builder.AddComponentParameter(21, nameof(Block.Title), title);
            builder.AddComponentParameter(22, nameof(Block.Raised), index == 0);
            builder.AddComponentParameter(24, nameof(Block.Kicker), Kickers[index]);
            builder.AddComponentParameter(23, nameof(Block.ChildContent), (RenderFragment)Content);
            builder.CloseComponent();
        }
        builder.CloseElement();

        builder.OpenElement(30, "p");
        builder.OpenElement(31, "a");
        builder.AddAttribute(32, "href", $"{Path}?again=1");
        builder.AddContent(33, "Afficher de nouveau");
        builder.CloseElement();
        builder.CloseElement();
    }

    private static void Content(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "La base documentaire se parcourt déjà : fiches d'albums, de séries et d'auteurs. ");
        builder.OpenElement(2, "a");
        builder.AddAttribute(3, "href", "/albums");
        builder.AddContent(4, "Tous les albums");
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(10, "p");
        builder.OpenComponent<Tag>(11);
        builder.AddComponentParameter(12, nameof(Tag.Text), "Dans la collection");
        builder.CloseComponent();
        builder.AddContent(13, " ");
        builder.OpenComponent<Button>(14);
        builder.AddComponentParameter(15, nameof(Button.Label), "Modifier");
        builder.CloseComponent();
        builder.CloseElement();
    }
}
