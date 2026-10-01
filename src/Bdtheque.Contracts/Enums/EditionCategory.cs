using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>Publishing category of an edition (Catégorie d'édition).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EditionCategory>))]
public enum EditionCategory
{
    FirstEdition = 1,
    SpecialEdition = 2,
    LimitedEdition = 3,
}
