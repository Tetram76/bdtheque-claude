using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Components.Presentation;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Presentation;

/// <summary>
/// The presentation components shared by the screens. They are told apart by what does not depend on the theme (element,
/// role, link, accessible name, the attribute a theme hooks onto), never by their look.
/// </summary>
public sealed class PresentationTests : BunitContext
{
    public PresentationTests() => Services.AddLocalization();

    [Fact]
    public void Block_IsASectionTitledAtItsLevel_WithItsContent()
    {
        var block = Render<Block>(parameters => parameters
            .Add(p => p.Title, "Plutôt quel genre ?")
            .Add(p => p.HeadingLevel, 3)
            .AddChildContent("<p>Contenu</p>"));

        var section = block.Find("section.block");
        Assert.Equal("Plutôt quel genre ?", section.QuerySelector("h3.block-title")?.TextContent);
        Assert.NotNull(section.QuerySelector("p"));
        Assert.Null(section.QuerySelector(".block-kicker"));
        Assert.False(section.ClassList.Contains("block-raised"));
    }

    [Fact]
    public void Block_CarriesItsKicker_AboveItsTitle_AndCanBeRaised()
    {
        var block = Render<Block>(parameters => parameters
            .Add(p => p.Title, "Saisir à partir d'un ISBN")
            .Add(p => p.Kicker, "Saisie")
            .Add(p => p.Raised, true));

        var header = block.Find("section.block > header");
        Assert.Equal(["block-kicker", "block-title"], header.Children.Select(child => child.ClassName));
        Assert.Equal("H2", header.Children[1].TagName);
        Assert.Contains("block-raised", block.Find("section").ClassList);
    }

    [Fact]
    public void Button_WithAnAddress_IsALink()
    {
        var button = Render<Button>(parameters => parameters
            .Add(p => p.Label, "Nouvel album")
            .Add(p => p.Href, "/admin/albums/new")
            .Add(p => p.Variant, ButtonVariant.Primary));

        var link = button.Find("a.button");
        Assert.Equal("/admin/albums/new", link.GetAttribute("href"));
        Assert.Equal("primary", link.GetAttribute("data-variant"));
        Assert.Equal("Nouvel album", link.TextContent.Trim());
    }

    [Fact]
    public void Button_WithoutAddress_IsAButtonOfItsType()
    {
        var clicked = false;
        var button = Render<Button>(parameters => parameters
            .Add(p => p.Label, "Supprimer définitivement")
            .Add(p => p.Variant, ButtonVariant.Delete)
            .Add(p => p.OnClick, () => clicked = true));

        var element = button.Find("button.button");
        Assert.Equal("button", element.GetAttribute("type"));
        Assert.Equal("delete", element.GetAttribute("data-variant"));
        element.Click();
        Assert.True(clicked);
    }

    [Fact]
    public void Button_ReducedToItsIconOnASmartphone_KeepsItsLabelAsItsName()
    {
        var button = Render<Button>(parameters => parameters
            .Add(p => p.Label, "Modifier")
            .Add(p => p.Icon, IconName.Edit)
            .Add(p => p.IconOnlyOnSmartphone, true)
            .Add(p => p.Href, "/admin/albums/1"));

        var link = button.Find("a");
        Assert.NotNull(link.GetAttribute("data-icon-only-on-smartphone"));
        Assert.Equal("true", link.QuerySelector("svg")?.GetAttribute("aria-hidden"));
        Assert.Equal("Modifier", link.QuerySelector(".button-label")?.TextContent);
    }

    [Fact]
    public void Button_SecondaryByDefault_CanBeDisabled()
    {
        var button = Render<Button>(parameters => parameters
            .Add(p => p.Label, "Enregistrer")
            .Add(p => p.Type, "submit")
            .Add(p => p.Disabled, true));

        var element = button.Find("button");
        Assert.Equal("secondary", element.GetAttribute("data-variant"));
        Assert.Equal("submit", element.GetAttribute("type"));
        Assert.True(element.HasAttribute("disabled"));
    }

    [Theory]
    [InlineData(TagVariant.Filled, "filled")]
    [InlineData(TagVariant.Dashed, "dashed")]
    public void Tag_CarriesItsVariant_AndNoRemovalByDefault(TagVariant variant, string expected)
    {
        var tag = Render<Tag>(parameters => parameters.Add(p => p.Variant, variant).Add(p => p.Text, "Dans la collection"));

        var element = tag.Find(".tag");
        Assert.Equal(expected, element.GetAttribute("data-variant"));
        Assert.Equal("Dans la collection", element.TextContent.Trim());
        Assert.Empty(tag.FindAll("a"));
    }

    [Fact]
    public void RemovableTag_LeadsToItsRemoval_NamedAfterItsText()
    {
        var tag = Render<Tag>(parameters => parameters
            .Add(p => p.RemoveHref, "/admin/albums?genre=")
            .Add(p => p.Text, "Genre : Aventure"));

        var removal = tag.Find(".tag a");
        Assert.Equal("/admin/albums?genre=", removal.GetAttribute("href"));
        Assert.Equal("Retirer « Genre : Aventure »", removal.GetAttribute("aria-label"));
    }

    [Fact]
    public void Cover_OfAVisual_IsItsDisplayMedia_RelayedByTheFrontend()
    {
        var visual = Guid.CreateVersion7();

        var cover = Render<Cover>(parameters => parameters.Add(p => p.VisualId, visual));

        var image = cover.Find(".cover img");
        Assert.Equal(VisualMediaRoutes.Display(visual), image.GetAttribute("src"));
        Assert.Equal("", image.GetAttribute("alt"));
        Assert.Empty(cover.FindAll("svg"));
    }

    [Fact]
    public void Cover_WithoutVisual_IsTheGenericVisual()
    {
        var cover = Render<Cover>();

        Assert.Empty(cover.FindAll("img"));
        Assert.Equal("true", cover.Find(".cover svg").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Cover_WithAText_NamesTheGenericVisualAsSuch()
    {
        var withVisual = Render<Cover>(parameters => parameters
            .Add(p => p.VisualId, Guid.CreateVersion7())
            .Add(p => p.Alt, "Couverture du Lotus bleu"));
        var generic = Render<Cover>(parameters => parameters.Add(p => p.Alt, "Couverture du Lotus bleu"));

        Assert.Equal("Couverture du Lotus bleu", withVisual.Find("img").GetAttribute("alt"));
        var svg = generic.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("Pas de couverture", svg.GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(GrammaticalGender.Masculine, "Non renseigné")]
    [InlineData(GrammaticalGender.Feminine, "Non renseignée")]
    public void NotSet_AgreesWithTheData_ItStandsFor(GrammaticalGender gender, string expected)
    {
        var mention = Render<NotSet>(parameters => parameters.Add(p => p.Gender, gender));

        Assert.Equal(expected, mention.Find(".not-set").TextContent);
    }

    [Fact]
    public void Icon_IsDecorative()
    {
        var icon = Render<Icon>(parameters => parameters.Add(p => p.Name, IconName.Add));

        var svg = icon.Find("svg");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Equal("false", svg.GetAttribute("focusable"));
        Assert.NotEmpty(svg.QuerySelectorAll("path"));
    }

    [Fact]
    public void EveryIcon_IsDrawn()
    {
        foreach (var name in Enum.GetValues<IconName>())
            Assert.NotEmpty(Render<Icon>(parameters => parameters.Add(p => p.Name, name)).FindAll("path"));
    }
}
