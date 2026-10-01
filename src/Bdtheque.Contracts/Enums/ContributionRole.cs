using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>The role an author is credited with on a contribution (Rôle).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContributionRole>))]
public enum ContributionRole
{
    Scenarist = 1,
    Illustrator = 2,
    Colorist = 3,
}
