using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// The visuals of an edition saved with its entry (visuel/saisie-edition/contenu.md), and the pending visuals that become some of
/// them (choix-implementation/domaine/visuels-stockage-et-traitement.md, Zone d'attente).
/// </summary>
public sealed class EditionVisualsEntryTests
{
    private static Edition CreateEdition()
    {
        var album = new Album("Tintin au Tibet", null);
        return new Edition(album, new Publisher("Casterman"));
    }

    [Fact]
    public void SetVisuals_KeepsAddsAndRemoves_InTheOrderAndTypesOfTheEntry()
    {
        var edition = CreateEdition();
        var cover = edition.AppendVisual(VisualType.Cover, "cover.jpg", TestPalette.Any);
        var plate = edition.AppendVisual(VisualType.Plate, "plate.jpg", TestPalette.Any);
        var backCover = edition.AppendVisual(VisualType.BackCover, "back.jpg", TestPalette.Any);
        var pending = new PendingVisual("new-plate.png", TestPalette.Any);

        var removed = edition.SetVisuals([(null, pending, VisualType.Plate), (plate.Id, null, VisualType.Plate), (cover.Id, null, VisualType.Cover)]);

        Assert.Equal([backCover], removed);
        var visuals = edition.GetOrderedVisuals().ToList();
        Assert.Equal(["cover.jpg", "new-plate.png", "plate.jpg"], visuals.Select(v => v.MediaReference));
        Assert.Equal([0, 0, 1], visuals.Select(v => v.DisplayOrder));
        Assert.Equal(pending.Palette, visuals[1].Palette);
    }

    [Fact]
    public void SetVisuals_ANewTypeForAKeptVisual_PlacesItAmongItsNewType()
    {
        var edition = CreateEdition();
        var cover = edition.AppendVisual(VisualType.Cover, "cover.jpg", TestPalette.Any);
        var plate = edition.AppendVisual(VisualType.Plate, "plate.jpg", TestPalette.Any);

        edition.SetVisuals([(cover.Id, null, VisualType.Plate), (plate.Id, null, VisualType.Cover)]);

        Assert.Equal([(VisualType.Cover, "plate.jpg"), (VisualType.Plate, "cover.jpg")],
            edition.GetOrderedVisuals().Select(v => (v.Type, v.MediaReference)));
    }

    public static TheoryData<string> RefusedEntries => ["neither", "both", "twice", "foreign", "pending twice"];

    [Theory]
    [MemberData(nameof(RefusedEntries))]
    public void SetVisuals_RefusedEntries_ChangeNothing(string refused)
    {
        var edition = CreateEdition();
        var cover = edition.AppendVisual(VisualType.Cover, "cover.jpg", TestPalette.Any);
        var pending = new PendingVisual("new.png", TestPalette.Any);
        (Guid?, PendingVisual?, VisualType)[] entries = refused switch
        {
            "neither" => [(null, null, VisualType.Cover)],
            "both" => [(cover.Id, pending, VisualType.Cover)],
            "twice" => [(cover.Id, null, VisualType.Cover), (cover.Id, null, VisualType.Plate)],
            "foreign" => [(cover.Id, null, VisualType.Cover), (Guid.NewGuid(), null, VisualType.Plate)],
            _ => [(cover.Id, null, VisualType.Cover), (null, pending, VisualType.Plate), (null, pending, VisualType.Plate)],
        };

        Assert.Throws<ArgumentException>(() => edition.SetVisuals(entries));

        Assert.Equal([("cover.jpg", VisualType.Cover)], edition.Visuals.Select(v => (v.MediaReference, v.Type)));
    }

    [Fact]
    public void PendingVisual_WithoutItsFile_IsRefused()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => new PendingVisual("  ", TestPalette.Any));

        Assert.Equal(DomainRules.EditionVisualMediaReferenceRequired, exception.Rule);
    }

    [Fact]
    public void PendingVisual_IsKeptTwentyFourHours()
    {
        var pending = new PendingVisual("new.png", TestPalette.Any);

        Assert.Equal(TimeSpan.FromHours(24), PendingVisual.Lifetime);
        Assert.False(pending.IsExpiredAt(pending.CreatedAt + TimeSpan.FromHours(24) - TimeSpan.FromTicks(1)));
        Assert.True(pending.IsExpiredAt(pending.CreatedAt + TimeSpan.FromHours(24)));
    }
}
