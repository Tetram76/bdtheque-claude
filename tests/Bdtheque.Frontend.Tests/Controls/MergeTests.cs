using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Merge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// What every merge presents the same (visuel/fusion/fusion-de-deux-fiches.md): a data to choose, what passes to the record,
/// what is left to choose and the confirmation, in the future.
/// </summary>
public sealed class MergeTests : BunitContext
{
    public MergeTests() => Services.AddLocalization();

    private MergeTexts Texts => new(Services.GetRequiredService<IStringLocalizer<MergeTexts>>());

    [Fact]
    public async Task Choice_OfTwoValues_OffersBoth_AndRetainsTheOneClicked()
    {
        MergeSide? retained = null;
        var choice = Render<MergeChoice>(parameters => parameters
            .Add(p => p.Id, "merge-last-name").Add(p => p.Label, "Nom").Add(p => p.FirstRecord, "Gotlib").Add(p => p.SecondRecord, "Marcel Gotlib")
            .Add(p => p.First, "Gotlieb").Add(p => p.Second, "Gotlib")
            .Add(p => p.SelectedChanged, side => retained = side));

        var radios = choice.FindAll("input[type=radio]");
        Assert.Equal(2, radios.Count);
        Assert.All(radios, radio => Assert.False(radio.HasAttribute("checked")));

        await choice.ChangeAsync("input[value=Second]", true);

        Assert.Equal(MergeSide.Second, retained);
    }

    [Fact]
    public void Choice_OfAValueMissingOnOneSide_OffersTheMissingValueToo()
    {
        var choice = Render<MergeChoice>(parameters => parameters
            .Add(p => p.Id, "merge-pseudonym").Add(p => p.Label, "Pseudonyme").Add(p => p.FirstRecord, "Gotlib").Add(p => p.SecondRecord, "Marcel Gotlib")
            .Add(p => p.First, "Gotlib").Add(p => p.Second, null).Add(p => p.Selected, MergeSide.First));

        Assert.Equal(2, choice.FindAll("input[type=radio]").Count);
        Assert.Equal("Non renseigné", choice.FindAll(".merge-choice-option")[1].QuerySelector(".visually-hidden")!.TextContent);
    }

    [Fact]
    public void Choice_OfTheSameValue_PresentsItOnce_WithNothingToChoose()
    {
        var choice = Render<MergeChoice>(parameters => parameters
            .Add(p => p.Id, "merge-first-name").Add(p => p.Label, "Prénom").Add(p => p.FirstRecord, "Gotlib").Add(p => p.SecondRecord, "Marcel Gotlib")
            .Add(p => p.First, "Marcel").Add(p => p.Second, "Marcel"));

        Assert.Empty(choice.FindAll("input[type=radio]"));
        Assert.Equal("Marcel", choice.Find(".merge-choice-same .merge-choice-value").TextContent);
        Assert.Equal("Identique dans les deux fiches", choice.Find(".merge-choice-same .merge-choice-note").TextContent);
    }

    // What was linked to both is said common, never a duplicate removed: nothing of it is lost (decision of the user).
    [Theory]
    [InlineData(24, 3, 1, "les 24 de Gotlib et les 3 de Marcel Gotlib, dont 1 commun aux deux fiches")]
    [InlineData(2, 1, 1, "les 2 de Gotlib et celui de Marcel Gotlib, dont 1 commun aux deux fiches")]
    [InlineData(0, 3, 0, "les 3 de Marcel Gotlib")]
    public void Links_SayWhereEachTypeComesFrom(int first, int second, int common, string detail)
    {
        var links = Render<MergeLinks>(parameters => parameters
            .Add(p => p.Links, [new MergeLinkCount(EntityKind.Album, first + second - common, first, second, common)])
            .Add(p => p.FirstName, "Gotlib").Add(p => p.SecondName, "Marcel Gotlib")
            .Add(p => p.Count, link => $"Crédité sur {link.Total} albums"));

        Assert.Equal(detail, links.Find(".merge-links-detail").TextContent);
    }

    [Fact]
    public void Links_InTheOrderOfTheEntityTypes_AgreeInGender_AndLeaveOutAnEmptyType()
    {
        var links = Render<MergeLinks>(parameters => parameters
            .Add(p => p.Links, [new MergeLinkCount(EntityKind.Series, 2, 2, 1, 1), new MergeLinkCount(EntityKind.Album, 0, 0, 0, 0)])
            .Add(p => p.FirstName, "Gotlib").Add(p => p.SecondName, "Marcel Gotlib")
            .Add(p => p.Count, link => $"{link.Total}"));

        Assert.Equal(["les 2 de Gotlib et celle de Marcel Gotlib, dont 1 commune aux deux fiches"],
            links.FindAll(".merge-links-detail").Select(d => d.TextContent));
    }

    [Theory]
    [InlineData(0, "Tout est choisi", "Tout est choisi")]
    [InlineData(1, "1 donnée reste à choisir", "1 à choisir")]
    [InlineData(2, "2 données restent à choisir", "2 à choisir")]
    public void Pending_SaysWhatIsLeftToChoose(int pending, string text, string shortText)
    {
        Assert.Equal(text, Texts.PendingText(pending));
        Assert.Equal(shortText, Texts.PendingShortText(pending));
    }

    // The confirmation precedes what it says: in the future (decision of the user).
    [Fact]
    public void Confirmation_SaysWhatTheMergeWillDo()
    {
        Assert.Equal("Les deux fiches n'en feront plus qu'une : Gotlib.", Texts.OneRecord("Gotlib"));
        Assert.Equal("les 2 séries et l'album des deux fiches", Texts.Linked(
            [new MergeLinkCount(EntityKind.Series, 2, 2, 1, 1), new MergeLinkCount(EntityKind.Album, 1, 1, 0, 0)]));
        Assert.Null(Texts.Linked([new MergeLinkCount(EntityKind.Album, 0, 0, 0, 0)]));
    }

    // Every type a merge of #642 links is named by the texts of the merge.
    [Theory]
    [InlineData(EntityKind.Series)]
    [InlineData(EntityKind.Album)]
    public void Texts_NameEveryTypeAMergeLinks(EntityKind kind) => Assert.True(Texts.Names(kind));
}
