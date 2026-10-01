using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>Physical binding of an edition (Reliure).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BindingType>))]
public enum BindingType
{
    Paperback = 1,
    Hardcover = 2,
}
