namespace Bdtheque.Domain.Enums;

/// <summary>
/// An entity type of the model, as the advanced search returns or follows it (fonctionnel.md § Entités
/// recherchées), and as the impact of a deletion counts its records (fonctionnel.md § Suppression des
/// entités).
/// </summary>
public enum EntityKind
{
    Album = 1,
    Series = 2,
    Universe = 3,
    Edition = 4,
    EditionVisual = 5,
    PublisherCollection = 6,
    Contribution = 7,
    PurchaseIntent = 8,
    Author = 9,
    Publisher = 10,
    Genre = 11,
}
