using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>Physical condition of an edition copy (État).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EditionCondition>))]
public enum EditionCondition
{
    Excellent = 1,
    VeryGood = 2,
    Good = 3,
    Poor = 4,
    VeryPoor = 5,
}
