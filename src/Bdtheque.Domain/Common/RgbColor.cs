namespace Bdtheque.Domain.Common;

/// <summary>An opaque colour, in sRGB, as the browser paints it.</summary>
public readonly record struct RgbColor(byte R, byte G, byte B)
{
    /// <summary>The colour as one 24-bit number, <c>0xRRGGBB</c>: how it is stored.</summary>
    public int Value => (R << 16) | (G << 8) | B;

    /// <summary>The colour of a 24-bit number, <c>0xRRGGBB</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> does not fit in 24 bits.</exception>
    public static RgbColor FromValue(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 0xFFFFFF);
        return new RgbColor((byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }

    /// <summary>The hexadecimal notation of CSS, <c>#RRGGBB</c>.</summary>
    public override string ToString() => $"#{Value:X6}";
}
