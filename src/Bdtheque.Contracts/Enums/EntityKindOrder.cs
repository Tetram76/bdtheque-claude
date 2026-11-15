namespace Bdtheque.Contracts.Enums;

/// <summary>
/// The single order of the entity types wherever the records linked to a record are cited — impact of a deletion and its
/// refusal, what references a collection, counts of a list, candidates of a merge —, from container to content
/// (fonctionnel/presentation/ordre-des-types-de-fiche.md). Written once, shared by the API and its clients.
/// </summary>
public static class EntityKindOrder
{
    public static readonly IReadOnlyList<EntityKind> Presentation =
    [
        EntityKind.Series,
        EntityKind.Album,
        EntityKind.Publisher,
        EntityKind.PublisherCollection,
        EntityKind.Edition,
        EntityKind.PurchaseIntent,
        EntityKind.Author,
        EntityKind.Contribution,
        EntityKind.EditionVisual,
        EntityKind.Genre,
        EntityKind.Universe,
        EntityKind.SavedSearch,
    ];

    // After Presentation: static fields are initialized in the order of their declaration.
    private static readonly Dictionary<EntityKind, int> Places = Presentation.Select((kind, place) => (kind, place)).ToDictionary();

    /// <summary>The place of the type in <see cref="Presentation"/>, by which records of several types are sorted.</summary>
    public static int Of(EntityKind kind) => Places[kind];
}
