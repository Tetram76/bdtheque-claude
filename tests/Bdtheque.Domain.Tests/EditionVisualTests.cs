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

        var visual = new EditionVisual(edition, VisualType.Cover, "covers/lotus-bleu.jpg", 1);

        Assert.Same(edition, visual.Edition);
        Assert.Equal(edition.Id, visual.EditionId);
        Assert.Equal(VisualType.Cover, visual.Type);
        Assert.Equal("covers/lotus-bleu.jpg", visual.MediaReference);
        Assert.Equal(1, visual.DisplayOrder);
        Assert.NotEqual(Guid.Empty, visual.Id);
    }

    [Fact]
    public void Constructor_NullEdition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EditionVisual(null!, VisualType.Cover, "cover.jpg", 0));
    }

    [Fact]
    public void Constructor_UndefinedType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EditionVisual(CreateEdition(), (VisualType)42, "cover.jpg", 0));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyMediaReference_Throws(string? mediaReference)
    {
        DomainAssert.Violates(DomainRules.EditionVisualMediaReferenceRequired, () => new EditionVisual(CreateEdition(), VisualType.Cover, mediaReference!, 0));
    }

    [Fact]
    public void Constructor_NegativeDisplayOrder_Throws()
    {
        DomainAssert.Violates(DomainRules.EditionVisualDisplayOrderNotNegative, () => new EditionVisual(CreateEdition(), VisualType.Cover, "cover.jpg", -1));
    }

    [Fact]
    public void Constructor_TrimsMediaReference()
    {
        var visual = new EditionVisual(CreateEdition(), VisualType.Cover, "  cover.jpg  ", 0);

        Assert.Equal("cover.jpg", visual.MediaReference);
    }

    [Fact]
    public void SetType_UndefinedValue_Throws()
    {
        var visual = new EditionVisual(CreateEdition(), VisualType.Cover, "cover.jpg", 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => visual.SetType((VisualType)42));
    }

    [Fact]
    public void SetType_ValidValue_Succeeds()
    {
        var visual = new EditionVisual(CreateEdition(), VisualType.Cover, "cover.jpg", 0);

        visual.SetType(VisualType.Plate);

        Assert.Equal(VisualType.Plate, visual.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetMediaReference_Empty_Throws(string? mediaReference)
    {
        var visual = new EditionVisual(CreateEdition(), VisualType.Cover, "cover.jpg", 0);

        DomainAssert.Violates(DomainRules.EditionVisualMediaReferenceRequired, () => visual.SetMediaReference(mediaReference!));
    }

    [Fact]
    public void SetMediaReference_TrimsAndStores()
    {
        var visual = new EditionVisual(CreateEdition(), VisualType.Cover, "cover.jpg", 0);

        visual.SetMediaReference("  plates/01.jpg  ");

        Assert.Equal("plates/01.jpg", visual.MediaReference);
    }

    [Fact]
    public void SetDisplayOrder_Negative_Throws()
    {
        var visual = new EditionVisual(CreateEdition(), VisualType.Cover, "cover.jpg", 0);

        DomainAssert.Violates(DomainRules.EditionVisualDisplayOrderNotNegative, () => visual.SetDisplayOrder(-1));
    }

    [Fact]
    public void SetDisplayOrder_ValidValue_Succeeds()
    {
        var visual = new EditionVisual(CreateEdition(), VisualType.Cover, "cover.jpg", 0);

        visual.SetDisplayOrder(3);

        Assert.Equal(3, visual.DisplayOrder);
    }
}
