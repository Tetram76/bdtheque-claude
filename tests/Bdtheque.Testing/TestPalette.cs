using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;

namespace Bdtheque.Testing;

/// <summary>The colours of a visual, for a test where they play no part.</summary>
public static class TestPalette
{
    /// <summary>Those of a blue cover, with a red accent.</summary>
    public static readonly VisualPalette Any = new(new RgbColor(0x2F, 0x5B, 0xA8), new RgbColor(0x14, 0x29, 0x5A), new RgbColor(0xD8, 0x34, 0x2B));
}
