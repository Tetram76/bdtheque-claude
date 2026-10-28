using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>
/// Type of a record written by the application, as the change log names it (modele-metier.md §
/// Entrée du journal des modifications).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecordType>))]
public enum RecordType
{
    Album = 1,
    Edition = 2,
    Series = 3,
    PurchaseIntent = 4,
    Author = 5,
    Publisher = 6,
    PublisherCollection = 7,
    Genre = 8,
    Universe = 9,
    SavedSearch = 10,
    ApplicationSettings = 11,
}
