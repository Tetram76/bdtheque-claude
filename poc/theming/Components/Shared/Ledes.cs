using ThemeProto.Data;

namespace ThemeProto.Components.Shared;

/// <summary>Editorial sentences derived from the data, shared by the themes that tell rather than chart.</summary>
public static class Ledes
{
    public static string Types(TypeDistributionData data) =>
        data.Shares.MaxBy(s => s.Percent) is { Kind: AlbumKind.Regular, Percent: >= 85 }
            ? "Neuf albums sur dix sont des tomes classiques"
            : "Un peu de tout sur les rayonnages";
}
