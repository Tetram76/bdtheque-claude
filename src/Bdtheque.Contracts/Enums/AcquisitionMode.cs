using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>How an edition was acquired (Mode d'acquisition).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AcquisitionMode>))]
public enum AcquisitionMode
{
    Purchase = 1,
    Gift = 2,
    Trade = 3,
    Won = 4,
    Inherited = 5,
}
