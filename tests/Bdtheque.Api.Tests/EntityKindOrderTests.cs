using Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The single order of the entity types wherever the records linked to a record are cited
/// (fonctionnel/presentation/ordre-des-types-de-fiche.md).
/// </summary>
public sealed class EntityKindOrderTests
{
    [Fact]
    public void Presentation_IsThatOfTheSpecification()
    {
        Assert.Equal(
            [
                EntityKind.Series, EntityKind.Album, EntityKind.Publisher, EntityKind.PublisherCollection, EntityKind.Edition,
                EntityKind.PurchaseIntent, EntityKind.Author, EntityKind.Contribution, EntityKind.EditionVisual, EntityKind.Genre,
                EntityKind.Universe, EntityKind.SavedSearch,
            ],
            EntityKindOrder.Presentation);
    }

    // A type added to the model without its place would be cited anywhere, which the order exists to prevent.
    [Fact]
    public void Presentation_PlacesEveryEntityType()
    {
        Assert.Equal(Enum.GetValues<EntityKind>().Order(), EntityKindOrder.Presentation.Order());
    }
}
