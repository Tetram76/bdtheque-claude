extern alias frontend;

using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Components.Controls;
using frontend::Bdtheque.Frontend.Components.Presentation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bdtheque.EndToEnd.Tests.Controls;

/// <summary>
/// A page of the tests only, which presents the controls and the interactive components shared by the screens, before any
/// screen uses them: their behaviors in a browser, and their look in each theme, are fixed on it. Routed by the
/// application through <c>PageAssemblies</c>; written in C#, the alias of the frontend not being reachable from Razor.
/// </summary>
[Route(Path)]
public sealed class ControlsPage : ComponentBase
{
    public const string Path = "/test/controls";

    /// <summary>Opens the dialog of a deletion, the way a list opens it for the record to delete.</summary>
    [SupplyParameterFromQuery(Name = "dialog")]
    public bool ShowsDialog { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "h1");
        builder.AddContent(1, "Contrôles");
        builder.CloseElement();

        builder.OpenElement(10, "p");
        builder.AddContent(11, "Actions de la ligne : ");
        builder.OpenComponent<ActionsMenu>(12);
        builder.AddComponentParameter(13, nameof(ActionsMenu.Id), "row-1");
        builder.AddComponentParameter(14, nameof(ActionsMenu.Label), "Autres actions sur Le Lotus bleu");
        builder.AddComponentParameter(15, nameof(ActionsMenu.ChildContent), (RenderFragment)MenuItems);
        builder.CloseComponent();
        builder.CloseElement();

        builder.OpenComponent<Block>(20);
        builder.AddComponentParameter(21, nameof(Block.Title), "Fiche");
        builder.AddComponentParameter(22, nameof(Block.ChildContent), (RenderFragment)Form);
        builder.CloseComponent();

        if (ShowsDialog)
        {
            builder.OpenComponent<Dialog>(30);
            builder.AddComponentParameter(31, nameof(Dialog.Id), "deletion");
            builder.AddComponentParameter(32, nameof(Dialog.Title), "Supprimer l'album ?");
            builder.AddComponentParameter(33, nameof(Dialog.Subject), "Le Lotus bleu (Tintin - T. 5)");
            builder.AddComponentParameter(34, nameof(Dialog.Tone), DialogTone.Deletion);
            builder.AddComponentParameter(35, nameof(Dialog.CloseHref), Path);
            builder.AddComponentParameter(36, nameof(Dialog.ChildContent), (RenderFragment)(body =>
            {
                body.OpenComponent<DeletionImpactSummary>(0);
                body.AddComponentParameter(1, nameof(DeletionImpactSummary.Impact), new DeletionImpact(
                    [], [new ImpactCount(EntityKind.Genre, 2)], [new ImpactCount(EntityKind.Edition, 2), new ImpactCount(EntityKind.EditionVisual, 5)],
                    [new ImpactCount(EntityKind.Album, 1)], "fingerprint"));
                body.CloseComponent();
            }));
            builder.AddComponentParameter(37, nameof(Dialog.Actions), (RenderFragment)(actions =>
            {
                actions.OpenComponent<Button>(0);
                actions.AddComponentParameter(1, nameof(Button.Label), "Supprimer définitivement");
                actions.AddComponentParameter(2, nameof(Button.Variant), ButtonVariant.Delete);
                actions.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    // A static form, sent by GET to this page, as the filters of a list are.
    private static void Form(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "form");
        builder.AddAttribute(1, "method", "get");
        builder.AddAttribute(2, "action", Path);
        // Laid out by the page, as every screen lays out its own fields.
        builder.AddAttribute(3, "style", "display: flex; flex-direction: column; gap: 0.75rem");

        builder.AddContent(10, FieldOf("f-title", "Titre", note: "facultatif dans une série", help: "La série et le tome suffisent à identifier l'album.",
            control: context => input =>
            {
                input.OpenElement(0, "input");
                input.AddAttribute(1, "id", context.Id);
                input.AddAttribute(2, "name", "title");
                input.AddAttribute(3, "type", "text");
                input.AddAttribute(4, "placeholder", "Titre propre de l'album");
                input.AddAttribute(5, "aria-describedby", context.DescribedBy);
                input.CloseElement();
            }));

        builder.AddContent(20, FieldOf("f-key", "Clé de tri", error: "La clé de tri ne peut pas être vide.",
            control: context => input =>
            {
                input.OpenElement(0, "input");
                input.AddAttribute(1, "id", context.Id);
                input.AddAttribute(2, "type", "text");
                input.AddAttribute(3, "aria-describedby", context.DescribedBy);
                input.AddAttribute(4, "aria-invalid", context.Invalid);
                input.CloseElement();
            }));

        builder.AddContent(30, FieldOf("f-year", "Année", characters: 4,
            control: context => input =>
            {
                input.OpenElement(0, "input");
                input.AddAttribute(1, "id", context.Id);
                input.AddAttribute(2, "type", "text");
                input.AddAttribute(3, "inputmode", "numeric");
                input.AddAttribute(4, "value", "1934");
                input.CloseElement();
            }));

        builder.AddContent(40, FieldOf("f-month", "Mois", characters: 9,
            control: context => select =>
            {
                select.OpenElement(0, "select");
                select.AddAttribute(1, "id", context.Id);
                foreach (var (month, index) in new[] { "—", "janvier", "février", "mars" }.Select((month, index) => (month, index)))
                {
                    select.OpenElement(2, "option");
                    select.SetKey(index);
                    select.AddContent(3, month);
                    select.CloseElement();
                }
                select.CloseElement();
            }));

        builder.OpenComponent<Choice>(50);
        builder.AddComponentParameter(51, nameof(Choice.Label), "Intégrale");
        builder.AddComponentParameter(52, nameof(Choice.ChildContent), (RenderFragment)(box =>
        {
            box.OpenElement(0, "input");
            box.AddAttribute(1, "type", "checkbox");
            box.AddAttribute(2, "name", "omnibus");
            box.CloseElement();
        }));
        builder.CloseComponent();

        builder.AddContent(60, FieldOf("f-genre", "Genre",
            control: context => picker =>
            {
                picker.OpenComponent<RecordPicker>(0);
                picker.AddComponentParameter(1, nameof(RecordPicker.Id), context.Id);
                picker.AddComponentParameter(2, nameof(RecordPicker.Kind), RecordKind.Genre);
                picker.AddComponentParameter(3, nameof(RecordPicker.Name), "genreId");
                picker.AddComponentParameter(4, nameof(RecordPicker.Placeholder), "Aucun genre");
                picker.AddComponentRenderMode(RenderMode.InteractiveServer);
                picker.CloseComponent();
            }));

        builder.OpenComponent<SaveBar>(70);
        builder.AddComponentParameter(71, nameof(SaveBar.State), SaveBarState.Unsaved);
        builder.AddComponentParameter(72, nameof(SaveBar.SaveLabel), "Enregistrer");
        builder.AddComponentParameter(73, nameof(SaveBar.CancelHref), Path);
        builder.CloseComponent();

        builder.CloseElement();
    }

    private static RenderFragment FieldOf(string id, string label, RenderFragment<FieldContext> control,
        string? note = null, string? help = null, string? error = null, int? characters = null) => builder =>
    {
        builder.OpenComponent<Field>(0);
        builder.AddComponentParameter(1, nameof(Field.Id), id);
        builder.AddComponentParameter(2, nameof(Field.Label), label);
        builder.AddComponentParameter(3, nameof(Field.Note), note);
        builder.AddComponentParameter(4, nameof(Field.Help), help);
        builder.AddComponentParameter(5, nameof(Field.Error), error);
        builder.AddComponentParameter(6, nameof(Field.Characters), characters);
        builder.AddComponentParameter(7, nameof(Field.ChildContent), control);
        builder.CloseComponent();
    };

    private static void MenuItems(RenderTreeBuilder builder)
    {
        builder.OpenComponent<MenuItem>(0);
        builder.AddComponentParameter(1, nameof(MenuItem.Label), "Modifier");
        builder.AddComponentParameter(2, nameof(MenuItem.Icon), (IconName?)IconName.Edit);
        builder.AddComponentParameter(3, nameof(MenuItem.Href), Path + "?edited=true");
        builder.CloseComponent();
        builder.OpenComponent<MenuSeparator>(4);
        builder.CloseComponent();
        builder.OpenComponent<MenuItem>(5);
        builder.AddComponentParameter(6, nameof(MenuItem.Label), "Supprimer…");
        builder.AddComponentParameter(7, nameof(MenuItem.Icon), (IconName?)IconName.Delete);
        builder.AddComponentParameter(8, nameof(MenuItem.Variant), ButtonVariant.Delete);
        builder.AddComponentParameter(9, nameof(MenuItem.Href), Path + "?dialog=true");
        builder.CloseComponent();
    }
}
