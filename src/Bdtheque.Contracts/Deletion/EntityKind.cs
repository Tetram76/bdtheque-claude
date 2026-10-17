using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Deletion;

/// <summary>
/// Type of the records counted by a <see cref="DeletionImpact"/>, and of those the advanced search returns
/// or compares (<see cref="Search.SearchSchema"/>).
/// </summary>
/// <remarks>
/// Not a copy of a domain enum: it names the entity types themselves, for the frontend to word the
/// impact (e.g. "used by 12 albums and 3 series") or the search.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<EntityKind>))]
public enum EntityKind
{
    Album,
    Series,
    Universe,
    Edition,
    EditionVisual,
    PublisherCollection,
    Contribution,
    PurchaseIntent,
    Author,
    Publisher,
    Genre,
}
