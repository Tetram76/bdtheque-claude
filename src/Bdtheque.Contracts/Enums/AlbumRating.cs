using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>User appreciation of an album (Note), from 1 (very poor) to 5 (very good).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AlbumRating>))]
public enum AlbumRating
{
    VeryPoor = 1,
    Poor = 2,
    Average = 3,
    Good = 4,
    VeryGood = 5,
}
