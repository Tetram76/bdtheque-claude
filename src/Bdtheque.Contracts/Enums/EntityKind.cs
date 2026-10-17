using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>
/// An entity type of the model: of the records counted by a <see cref="Deletion.DeletionImpact"/>, of those
/// the advanced search returns or follows (<see cref="Search.SearchSchema"/>), and of those a saved search
/// returns.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EntityKind>))]
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

    // Never searched: only counted by the impact of a deletion it blocks.
    SavedSearch = 12,
}
