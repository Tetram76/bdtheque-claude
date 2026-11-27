using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Formatting;

/// <summary>
/// The summary of an edition targeted by a purchase intent, the same wherever the intent is presented
/// (fonctionnel/regles-metier/intention-d-acquisition.md, résumé de l'édition visée).
/// </summary>
public sealed class EditionTraitTextsTests
{
    private readonly EditionTraitTexts _texts =
        new ServiceCollection().AddLogging().AddLocalization().AddSingleton(new BrowserTimeZone()).AddScoped<DisplayFormatter>()
            .AddSingleton<ValueLabels>().AddScoped<EditionTraitTexts>()
            .BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<EditionTraitTexts>();

    [Fact]
    public void Summary_GivesEveryTraitInItsOrder_ThenBlackAndWhite() =>
        Assert.Equal(
            "Édition spéciale · Grand (> A4) · Cartonnée · Italienne · Droite à gauche · 48 pages · Noir et blanc",
            _texts.Summary(new EditionTraits(
                EditionCategory.SpecialEdition, EditionFormat.Large, BindingType.Hardcover, BookOrientation.Landscape,
                ReadingDirection.RightToLeft, 48, IsColor: false)));

    [Fact]
    public void Summary_LeavesOutWhatIsNotSet_AndColour() =>
        Assert.Equal(
            "Brochée · 62 pages",
            _texts.Summary(new EditionTraits(null, null, BindingType.Paperback, null, null, 62, IsColor: true)));

    [Fact]
    public void Summary_OfAnEditionInColourWithoutAnyTrait_IsNothing() =>
        Assert.Null(_texts.Summary(new EditionTraits(null, null, null, null, null, null, IsColor: true)));

    [Fact]
    public void Summary_OfAnEditionInBlackAndWhiteWithoutAnyOtherTrait_SaysSo() =>
        Assert.Equal("Noir et blanc", _texts.Summary(new EditionTraits(null, null, null, null, null, null, IsColor: false)));
}
