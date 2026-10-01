using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>Reading direction of an edition (Sens de lecture).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReadingDirection>))]
public enum ReadingDirection
{
    LeftToRight = 1,
    RightToLeft = 2,
}
