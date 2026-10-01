using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>Page orientation of an edition (Orientation).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BookOrientation>))]
public enum BookOrientation
{
    Portrait = 1,
    Landscape = 2,
}
