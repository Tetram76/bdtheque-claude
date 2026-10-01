using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>Physical size format of an edition (Format).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EditionFormat>))]
public enum EditionFormat
{
    Pocket = 1,
    Medium = 2,
    Standard = 3,
    Large = 4,
    Special = 5,
}
