using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>Kind of album (Type) — a standalone volume, or an omnibus collecting several.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AlbumType>))]
public enum AlbumType
{
    Regular = 1,
    Omnibus = 2,
}
