using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>Progress status of a series (Statut).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SeriesStatus>))]
public enum SeriesStatus
{
    InProgress = 1,
    Completed = 2,
    Abandoned = 3,
}
