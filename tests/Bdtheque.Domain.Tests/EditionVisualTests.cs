using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class EditionVisualTests
{
    private static Edition CreateEdition() => new(new Album("Le Lotus bleu", null), new Publisher("Casterman"));

    [Fact]
    public void Constructor_ValidArguments_Succeeds()
    {
        var edition = CreateEdition();

        var visual = edition.AddVisual(VisualType.Cover, "covers/lotus-bleu.jpg", 1);

        Assert.Same(edition, visual.Edition);
        Assert.Equal(edition.Id, visual.EditionId);
        Assert.Equal(VisualType.Cover, visual.Type);
        Assert.Equal("covers/lotus-bleu.jpg", visual.MediaReference);
        Assert.Equal(1, visual.DisplayOrder);
        Assert.NotEqual(Guid.Empty, visual.Id);
    }

    [Fact]
    public void Constructor_UndefinedType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateEdition().AddVisual((VisualType)42, "cover.jpg", 0));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyMediaReference_Throws(string? mediaReference)
    {
        DomainAssert.Violates(DomainRules.EditionVisualMediaReferenceRequired, () => CreateEdition().AddVisual(VisualType.Cover, mediaReference!, 0));
    }

    [Fact]
    public void Constructor_NegativeDisplayOrder_Throws()
    {
        DomainAssert.Violates(DomainRules.EditionVisualDisplayOrderNotNegative, () => CreateEdition().AddVisual(VisualType.Cover, "cover.jpg", -1));
    }

    [Fact]
    public void Constructor_TrimsMediaReference()
    {
        var visual = CreateEdition().AddVisual(VisualType.Cover, "  cover.jpg  ", 0);

        Assert.Equal("cover.jpg", visual.MediaReference);
    }

    [Fact]
    public void SetType_UndefinedValue_Throws()
    {
        var visual = CreateEdition().AddVisual(VisualType.Cover, "cover.jpg", 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => visual.SetType((VisualType)42));
    }

    [Fact]
    public void SetType_ValidValue_Succeeds()
    {
        var visual = CreateEdition().AddVisual(VisualType.Cover, "cover.jpg", 0);

        visual.SetType(VisualType.Plate);

        Assert.Equal(VisualType.Plate, visual.Type);
    }

    [Fact]
    public void SetDisplayOrder_Negative_Throws()
    {
        var visual = CreateEdition().AddVisual(VisualType.Cover, "cover.jpg", 0);

        DomainAssert.Violates(DomainRules.EditionVisualDisplayOrderNotNegative, () => visual.SetDisplayOrder(-1));
    }

    [Fact]
    public void SetDisplayOrder_ValidValue_Succeeds()
    {
        var visual = CreateEdition().AddVisual(VisualType.Cover, "cover.jpg", 0);

        visual.SetDisplayOrder(3);

        Assert.Equal(3, visual.DisplayOrder);
    }

    [Fact]
    public void AppendVisual_PlacesTheVisualAfterThoseOfTheSameType()
    {
        var edition = CreateEdition();
        edition.AddVisual(VisualType.Plate, "plate-a.jpg", 4);
        edition.AddVisual(VisualType.Cover, "cover.jpg", 7);

        var plate = edition.AppendVisual(VisualType.Plate, "plate-b.jpg");
        var dedication = edition.AppendVisual(VisualType.Dedication, "dedication.jpg");

        Assert.Equal(5, plate.DisplayOrder);
        Assert.Equal(0, dedication.DisplayOrder);
        Assert.Contains(plate, edition.Visuals);
        Assert.Equal("plate-b.jpg", plate.MediaReference);
    }

    [Fact]
    public void AppendVisual_UndefinedType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateEdition().AppendVisual((VisualType)42, "cover.jpg"));
    }

    [Fact]
    public void ArrangeVisuals_SetsTypesAndRanksInTheGivenOrder()
    {
        var edition = CreateEdition();
        var first = edition.AddVisual(VisualType.Cover, "a.jpg", 0);
        var second = edition.AddVisual(VisualType.Plate, "b.jpg", 0);
        var third = edition.AddVisual(VisualType.Plate, "c.jpg", 1);

        // The second becomes a cover placed before the first; the third stays the only plate.
        edition.ArrangeVisuals([(second.Id, VisualType.Cover), (third.Id, VisualType.Plate), (first.Id, VisualType.Cover)]);

        Assert.Equal([second, first, third], edition.GetOrderedVisuals());
        Assert.Equal((VisualType.Cover, 0), (second.Type, second.DisplayOrder));
        Assert.Equal((VisualType.Cover, 1), (first.Type, first.DisplayOrder));
        Assert.Equal((VisualType.Plate, 0), (third.Type, third.DisplayOrder));
    }

    [Fact]
    public void ArrangeVisuals_MissingAVisual_ThrowsAndChangesNothing()
    {
        var edition = CreateEdition();
        var first = edition.AddVisual(VisualType.Cover, "a.jpg", 0);
        edition.AddVisual(VisualType.Plate, "b.jpg", 0);

        Assert.Throws<ArgumentException>(() => edition.ArrangeVisuals([(first.Id, VisualType.Plate)]));
        Assert.Equal(VisualType.Cover, first.Type);
    }

    [Fact]
    public void ArrangeVisuals_ListingAVisualTwice_Throws()
    {
        var edition = CreateEdition();
        var first = edition.AddVisual(VisualType.Cover, "a.jpg", 0);
        edition.AddVisual(VisualType.Plate, "b.jpg", 0);

        Assert.Throws<ArgumentException>(() => edition.ArrangeVisuals([(first.Id, VisualType.Cover), (first.Id, VisualType.Cover)]));
    }

    [Fact]
    public void ArrangeVisuals_ListingAVisualOfAnotherEdition_Throws()
    {
        var edition = CreateEdition();
        edition.AddVisual(VisualType.Cover, "a.jpg", 0);
        var foreign = CreateEdition().AddVisual(VisualType.Cover, "b.jpg", 0);

        Assert.Throws<ArgumentException>(() => edition.ArrangeVisuals([(foreign.Id, VisualType.Cover)]));
    }

    [Fact]
    public void ArrangeVisuals_UndefinedType_ThrowsAndChangesNothing()
    {
        var edition = CreateEdition();
        var first = edition.AddVisual(VisualType.Cover, "a.jpg", 0);
        var second = edition.AddVisual(VisualType.Cover, "b.jpg", 1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => edition.ArrangeVisuals([(second.Id, VisualType.Cover), (first.Id, (VisualType)42)]));
        Assert.Equal((VisualType.Cover, 1), (second.Type, second.DisplayOrder));
    }
}
