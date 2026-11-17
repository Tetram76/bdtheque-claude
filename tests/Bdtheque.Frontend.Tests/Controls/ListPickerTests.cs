using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The choice in a list drawn by the application (choix-implementation/socle/frontend.md, collection d'un éditeur choisie dans
/// une liste) : a select-only combobox, whose last option may create a record on the spot, with the look of the creation of the
/// choice of a record by its name (visuel/saisie-album/contenu.md, création sur place).
/// </summary>
public sealed class ListPickerTests : BunitContext
{
    private static readonly RecordOption Aire = new(Guid.Parse("0199b6a4-0000-7000-8000-000000000001"), "Aire libre", null);
    private static readonly RecordOption Repérages = new(Guid.Parse("0199b6a4-0000-7000-8000-000000000002"), "Repérages", null);

    private RecordOption? _chosen = Repérages;
    private int _creations;

    public ListPickerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Picker_IsAComboboxButton_ClosedAtFirst_ShowingTheOptionChosen()
    {
        var picker = Render(Aire);

        var button = picker.Find("button#f-collection");
        Assert.Equal("combobox", button.GetAttribute("role"));
        Assert.Equal("listbox", button.GetAttribute("aria-haspopup"));
        Assert.Equal("false", button.GetAttribute("aria-expanded"));
        Assert.Equal("Aire libre", button.TextContent.Trim());
        var list = picker.Find($"#{button.GetAttribute("aria-controls")}");
        Assert.True(list.HasAttribute("hidden"));
        Assert.Equal("listbox", list.GetAttribute("role"));
        Assert.Equal("Collection", list.GetAttribute("aria-label"));
        Assert.Equal("true", button.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Picker_WithoutValue_ShowsWhatChoosesNone()
    {
        var picker = Render(null);

        Assert.Equal("—", picker.Find("button").TextContent.Trim());
    }

    [Fact]
    public async Task Click_OpensTheList_None_TheOptions_ThenTheCreation_TheValueActive()
    {
        var picker = Render(Repérages);

        await picker.ClickAsync("button");

        Assert.Equal("true", picker.Find("button").GetAttribute("aria-expanded"));
        Assert.False(picker.Find("[role=listbox]").HasAttribute("hidden"));
        Assert.Equal(["—", "Aire libre", "Repérages", "Nouvelle collection…"],
            picker.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
        // The creation, with the look of the creation of a choice by the name.
        Assert.True(picker.FindAll("[role=option]")[3].ClassList.Contains("option-list-create"));
        var active = picker.Find("button").GetAttribute("aria-activedescendant");
        Assert.Equal("Repérages", picker.Find($"#{active}").TextContent.Trim());
        Assert.Equal("true", picker.Find($"#{active}").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task ChoosingAnOption_MakesItTheValue_AndClosesTheList()
    {
        var picker = Render(null);

        await picker.ClickAsync("button");
        await picker.ClickAsync("#f-collection-option-1");

        Assert.Equal(Aire, _chosen);
        Assert.Equal("false", picker.Find("button").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task ChoosingNone_RemovesTheValue()
    {
        var picker = Render(Aire);

        await picker.ClickAsync("button");
        await picker.ClickAsync("#f-collection-option-0");

        Assert.Null(_chosen);
    }

    [Fact]
    public async Task ChoosingTheCreation_LeavesItToTheCaller_WithoutChangingTheValue()
    {
        var picker = Render(Aire);

        await picker.ClickAsync("button");
        await picker.ClickAsync(".option-list-create");

        Assert.Equal(1, _creations);
        Assert.Equal(Repérages, _chosen);
        Assert.Equal("false", picker.Find("button").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task Keyboard_OpensTheList_MovesThroughIt_AndChoosesTheActiveOption()
    {
        var picker = Render(null);

        await picker.KeyDownAsync("button", "ArrowDown");
        Assert.Equal("true", picker.Find("button").GetAttribute("aria-expanded"));
        Assert.Equal("f-collection-option-0", picker.Find("button").GetAttribute("aria-activedescendant"));
        await picker.KeyDownAsync("button", "End");
        Assert.Equal("f-collection-option-3", picker.Find("button").GetAttribute("aria-activedescendant"));
        await picker.KeyDownAsync("button", "ArrowDown");
        Assert.Equal("f-collection-option-3", picker.Find("button").GetAttribute("aria-activedescendant"));
        await picker.KeyDownAsync("button", "ArrowUp");
        await picker.KeyDownAsync("button", "ArrowUp");
        await picker.KeyDownAsync("button", "Enter");

        Assert.Equal(Aire, _chosen);
        Assert.Equal("false", picker.Find("button").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task Keyboard_ChoosesTheCreationToo_BySpace()
    {
        var picker = Render(null);

        await picker.KeyDownAsync("button", " ");
        await picker.KeyDownAsync("button", "End");
        await picker.KeyDownAsync("button", " ");

        Assert.Equal(1, _creations);
    }

    [Fact]
    public async Task EscapeOrLeavingTheButton_ClosesTheList_WithoutChoosing()
    {
        var picker = Render(Aire);

        await picker.KeyDownAsync("button", "Home");
        Assert.Equal("f-collection-option-0", picker.Find("button").GetAttribute("aria-activedescendant"));
        await picker.KeyDownAsync("button", "Escape");
        Assert.Equal("false", picker.Find("button").GetAttribute("aria-expanded"));
        await picker.ClickAsync("button");
        await picker.BlurAsync("button");

        Assert.Equal("false", picker.Find("button").GetAttribute("aria-expanded"));
        Assert.Equal(Repérages, _chosen);
    }

    [Fact]
    public async Task Disabled_ByItsOwnAttribute_NeverOpens()
    {
        var picker = Render<ListPicker>(parameters => parameters
            .Add(p => p.Id, "f-collection")
            .Add(p => p.Label, "Collection")
            .Add(p => p.Options, [Aire])
            .Add(p => p.NoneLabel, "Choisissez d'abord l'éditeur")
            .Add(p => p.Disabled, true));

        await picker.KeyDownAsync("button", "ArrowDown");

        Assert.True(picker.Find("button").HasAttribute("disabled"));
        Assert.Equal("Choisissez d'abord l'éditeur", picker.Find("button").TextContent.Trim());
        Assert.Equal("false", picker.Find("button").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task Creation_WithoutCaller_IsNeverOffered()
    {
        var picker = Render<ListPicker>(parameters => parameters
            .Add(p => p.Id, "f-collection")
            .Add(p => p.Label, "Collection")
            .Add(p => p.Options, [Aire])
            .Add(p => p.NoneLabel, "—"));

        await picker.ClickAsync("button");

        Assert.Equal(["—", "Aire libre"], picker.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    // A list of two collections, chosen among, which ends with the creation of a new one; marked in error by its field.
    private IRenderedComponent<ListPicker> Render(RecordOption? value) => Render<ListPicker>(parameters => parameters
        .Add(p => p.Id, "f-collection")
        .Add(p => p.Label, "Collection")
        .Add(p => p.Options, [Aire, Repérages])
        .Add(p => p.Value, value)
        .Add(p => p.ValueChanged, chosen => _chosen = chosen)
        .Add(p => p.NoneLabel, "—")
        .Add(p => p.CreateLabel, "Nouvelle collection…")
        .Add(p => p.OnCreate, () => _creations++)
        .AddUnmatched("aria-invalid", "true"));
}
