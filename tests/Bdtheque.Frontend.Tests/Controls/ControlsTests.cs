using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Presentation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The controls and the interactive components shared by the screens, told apart by what does not depend on the theme
/// (element, role, link, accessible name, the attribute a theme or a script hooks onto), never by their look.
/// </summary>
public sealed class ControlsTests : BunitContext
{
    public ControlsTests() => Services.AddLocalization();

    [Fact]
    public void Field_LabelsItsControl_AndDescribesItByItsHelp()
    {
        var field = Render<Field>(parameters => parameters
            .Add(p => p.Id, "f-title")
            .Add(p => p.Label, "Titre")
            .Add(p => p.Note, "facultatif dans une série")
            .Add(p => p.Help, "La série et le tome suffisent à identifier l'album.")
            .Add(p => p.ChildContent, context => builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "id", context.Id);
                builder.AddAttribute(2, "aria-describedby", context.DescribedBy);
                builder.AddAttribute(3, "aria-invalid", context.Invalid);
                builder.CloseElement();
            }));

        var label = field.Find(".field label");
        Assert.Equal("f-title", label.GetAttribute("for"));
        Assert.Equal("facultatif dans une série", label.QuerySelector(".field-note")?.TextContent);
        var input = field.Find("input#f-title");
        Assert.Equal("f-title-help", input.GetAttribute("aria-describedby"));
        Assert.Equal("La série et le tome suffisent à identifier l'album.", field.Find("#f-title-help").TextContent);
        Assert.Null(input.GetAttribute("aria-invalid"));
        Assert.Empty(field.FindAll(".field-error"));
    }

    [Fact]
    public void Field_InError_MarksItsControlInvalid_AndDescribesItByTheErrorFirst()
    {
        var field = Render<Field>(parameters => parameters
            .Add(p => p.Id, "f-title")
            .Add(p => p.Label, "Titre")
            .Add(p => p.Help, "Aide")
            .Add(p => p.Error, "Le titre est obligatoire sans série.")
            .Add(p => p.ChildContent, context => builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "id", context.Id);
                builder.AddAttribute(2, "aria-describedby", context.DescribedBy);
                builder.AddAttribute(3, "aria-invalid", context.Invalid);
                builder.CloseElement();
            }));

        var input = field.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal("f-title-error f-title-help", input.GetAttribute("aria-describedby"));
        Assert.Contains("Le titre est obligatoire sans série.", field.Find("#f-title-error").TextContent);
        Assert.Equal("true", field.Find("#f-title-error svg").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Field_SizedToItsContent_CarriesItsWidthInCharacters()
    {
        var sized = Render<Field>(parameters => parameters.Add(p => p.Id, "f-year").Add(p => p.Label, "Année").Add(p => p.Characters, 4));
        var wide = Render<Field>(parameters => parameters.Add(p => p.Id, "f-notes").Add(p => p.Label, "Notes"));

        Assert.Equal("--field-characters: 4", sized.Find(".field").GetAttribute("style"));
        Assert.Null(wide.Find(".field").GetAttribute("style"));
    }

    [Fact]
    public void Choice_LabelsTheBoxItWraps()
    {
        var choice = Render<Choice>(parameters => parameters
            .Add(p => p.Label, "Intégrale")
            .AddChildContent("<input type=\"checkbox\" name=\"omnibus\" />"));

        var label = choice.Find("label.choice");
        Assert.NotNull(label.QuerySelector("input[type=checkbox]"));
        Assert.Equal("Intégrale", label.TextContent.Trim());
    }

    [Theory]
    [InlineData(SaveBarState.New, "new", "Nouvelle fiche")]
    [InlineData(SaveBarState.Unsaved, "unsaved", "Modifications non enregistrées")]
    [InlineData(SaveBarState.Unchanged, "unchanged", "Aucune modification")]
    public void SaveBar_TellsTheStateOfTheRecord(SaveBarState state, string name, string text)
    {
        var bar = Render<SaveBar>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.SaveLabel, "Créer l'album")
            .Add(p => p.CancelHref, "/admin/albums"));

        var status = bar.Find(".save-bar [role=status]");
        Assert.Equal(name, status.GetAttribute("data-state"));
        Assert.Equal(text, status.QuerySelector(".save-bar-state-long")?.TextContent);
        Assert.NotEmpty(status.QuerySelector(".save-bar-state-short")!.TextContent);
    }

    [Fact]
    public void SaveBar_CancelsByLeaving_AndSavesBySubmittingItsForm()
    {
        var bar = Render<SaveBar>(parameters => parameters
            .Add(p => p.State, SaveBarState.Unsaved)
            .Add(p => p.SaveLabel, "Enregistrer")
            .Add(p => p.CancelHref, "/admin/albums"));

        var cancel = bar.Find(".save-bar a.button");
        Assert.Equal("/admin/albums", cancel.GetAttribute("href"));
        Assert.Equal("Annuler", cancel.TextContent.Trim());
        var save = bar.Find(".save-bar button.button");
        Assert.Equal("submit", save.GetAttribute("type"));
        Assert.Equal("primary", save.GetAttribute("data-variant"));
        Assert.Equal("Enregistrer", save.TextContent.Trim());
    }

    [Fact]
    public void ActionsMenu_IsAPopoverOpenedByItsButton_AnchoredToIt()
    {
        var menu = Render<ActionsMenu>(parameters => parameters
            .Add(p => p.Id, "album-1")
            .Add(p => p.Label, "Autres actions sur Le Lotus bleu"));

        var toggle = menu.Find("button.actions-menu-toggle");
        Assert.Equal("button", toggle.GetAttribute("type"));
        Assert.Equal("album-1", toggle.GetAttribute("popovertarget"));
        Assert.Equal("Autres actions sur Le Lotus bleu", toggle.GetAttribute("aria-label"));
        var panel = menu.Find("#album-1");
        Assert.Equal("auto", panel.GetAttribute("popover"));
        Assert.Equal("anchor-name: --actions-menu-album-1", menu.Find(".actions-menu").GetAttribute("style"));
        Assert.Equal("position-anchor: --actions-menu-album-1", panel.GetAttribute("style"));
    }

    [Fact]
    public void ActionsMenu_ClosesWhenAButtonOfItIsChosen_AndLeadsByItsLinks()
    {
        var chosen = false;
        var menu = Render<ActionsMenu>(parameters => parameters
            .Add(p => p.Id, "album-1")
            .Add(p => p.Label, "Autres actions")
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<MenuItem>(0);
                builder.AddAttribute(1, nameof(MenuItem.Label), "Voir dans la consultation");
                builder.AddAttribute(2, nameof(MenuItem.Href), "/albums/1");
                builder.CloseComponent();
                builder.OpenComponent<MenuSeparator>(3);
                builder.CloseComponent();
                builder.OpenComponent<MenuItem>(4);
                builder.AddAttribute(5, nameof(MenuItem.Label), "Supprimer…");
                builder.AddAttribute(6, nameof(MenuItem.Icon), (IconName?)IconName.Delete);
                builder.AddAttribute(7, nameof(MenuItem.Variant), ButtonVariant.Delete);
                builder.AddAttribute(8, nameof(MenuItem.OnClick), EventCallback.Factory.Create(this, () => chosen = true));
                builder.CloseComponent();
            }));

        var link = menu.Find("#album-1 a.menu-item");
        Assert.Equal("/albums/1", link.GetAttribute("href"));
        Assert.NotNull(menu.Find("#album-1 hr.menu-separator"));
        var deletion = menu.Find("#album-1 button.menu-item");
        Assert.Equal("delete", deletion.GetAttribute("data-variant"));
        Assert.Equal("album-1", deletion.GetAttribute("popovertarget"));
        Assert.Equal("hide", deletion.GetAttribute("popovertargetaction"));
        deletion.Click();
        Assert.True(chosen);
    }

    [Fact]
    public void Dialog_IsAModalNativeDialog_NamedByItsTitle_DescribedByItsBody()
    {
        var dialog = Render<Dialog>(parameters => parameters
            .Add(p => p.Id, "deletion")
            .Add(p => p.Title, "Supprimer l'album ?")
            .Add(p => p.Subject, "Le Lotus bleu (Tintin - T. 5)")
            .Add(p => p.Tone, DialogTone.Deletion)
            .Add(p => p.CloseHref, "/admin/albums")
            .Add(p => p.ChildContent, "<p>Corps</p>")
            .Add(p => p.Actions, "<button type=\"submit\">Supprimer définitivement</button>"));

        var element = dialog.Find("app-modal-dialog > dialog.dialog");
        Assert.Equal("alertdialog", element.GetAttribute("role"));
        Assert.Equal("deletion", element.GetAttribute("data-tone"));
        Assert.Equal("Supprimer l'album ?", dialog.Find($"#{element.GetAttribute("aria-labelledby")}").TextContent);
        Assert.NotNull(dialog.Find($"#{element.GetAttribute("aria-describedby")} p"));
        Assert.Equal("Le Lotus bleu (Tintin - T. 5)", dialog.Find(".dialog-subject").TextContent);
        // Closing comes first, the safe choice that the opening of the dialog focuses.
        var actions = dialog.Find(".dialog-actions").Children;
        Assert.Equal("/admin/albums", actions[0].GetAttribute("href"));
        Assert.True(actions[0].HasAttribute("data-dialog-close"));
        Assert.Equal("Annuler", actions[0].TextContent.Trim());
        Assert.Equal("submit", actions[1].GetAttribute("type"));
    }

    [Theory]
    [InlineData(DialogTone.Refusal, "alertdialog")]
    [InlineData(DialogTone.Choice, "dialog")]
    public void Dialog_OfAnotherTone_IsAnAlertOnlyForARefusal(DialogTone tone, string role)
    {
        var dialog = Render<Dialog>(parameters => parameters
            .Add(p => p.Id, "d")
            .Add(p => p.Title, "Titre")
            .Add(p => p.Tone, tone)
            .Add(p => p.CloseHref, "/")
            .Add(p => p.CloseLabel, "Fermer"));

        Assert.Equal(role, dialog.Find("dialog").GetAttribute("role"));
        Assert.Equal("Fermer", dialog.Find("[data-dialog-close]").TextContent.Trim());
        Assert.Empty(dialog.FindAll(".dialog-subject"));
    }

    [Fact]
    public void DeletionImpact_AnnouncesEachNatureOfLink_ByEntityType()
    {
        var impact = new DeletionImpact(
            BlockedBy: [],
            AssociationsRemoved: [new ImpactCount(EntityKind.Genre, 2)],
            DeletedWith: [new ImpactCount(EntityKind.Edition, 2), new ImpactCount(EntityKind.EditionVisual, 5), new ImpactCount(EntityKind.PurchaseIntent, 1)],
            LeavingCollection: [new ImpactCount(EntityKind.Album, 1)],
            Fingerprint: "abc");

        var summary = Render<DeletionImpactSummary>(parameters => parameters.Add(p => p.Impact, impact));

        Assert.Equal(
            [
                "Suppression aussi de 2 éditions, 5 visuels et 1 intention d'achat",
                "Retrait de l'association avec 2 genres",
                "Sortie de la collection de 1 album",
            ],
            summary.FindAll(".deletion-impact li").Select(line => Normalize(line.TextContent)));
    }

    [Fact]
    public void DeletionImpact_OfARefusal_AnnouncesWhatBlocksIt_Only()
    {
        var impact = new DeletionImpact(
            BlockedBy: [new ImpactCount(EntityKind.SavedSearch, 1)],
            AssociationsRemoved: [],
            DeletedWith: [new ImpactCount(EntityKind.Edition, 2)],
            LeavingCollection: [],
            Fingerprint: "abc");

        var summary = Render<DeletionImpactSummary>(parameters => parameters.Add(p => p.Impact, impact));

        var line = Assert.Single(summary.FindAll(".deletion-impact li"));
        Assert.Equal("blocked", line.GetAttribute("data-nature"));
        Assert.Equal("Suppression bloquée par 1 recherche enregistrée", Normalize(line.TextContent));
    }

    [Fact]
    public void DeletionImpact_WithoutAnyOtherRecord_SaysSo()
    {
        var summary = Render<DeletionImpactSummary>(parameters => parameters.Add(p => p.Impact, new DeletionImpact([], [], [], [], "abc")));

        Assert.Empty(summary.FindAll("li"));
        Assert.Equal("Aucune autre fiche n'est concernée.", summary.Find(".deletion-impact").TextContent.Trim());
    }

    private static string Normalize(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
