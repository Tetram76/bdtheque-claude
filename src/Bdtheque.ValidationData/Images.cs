using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Bdtheque.ValidationData;

/// <summary>
/// The images of the validation data, drawn here rather than kept as files: a vertical gradient of a hue,
/// enough for every visual to be told apart from the others, encoded as PNG with no image library.
/// </summary>
internal static class Images
{
    public static byte[] Portrait(double hue) => Png(300, 420, hue);

    public static byte[] Landscape(double hue) => Png(420, 300, hue);

    private static byte[] Png(int width, int height, double hue)
    {
        var (top, bottom) = (Rgb(hue, 0.55, 0.62), Rgb(hue, 0.65, 0.22));
        var stride = 1 + width * 3;
        var pixels = new byte[height * stride];
        for (var y = 0; y < height; y++)
        {
            // Each scanline starts with its filter type, 0 for none.
            var ratio = (double)y / (height - 1);
            for (var x = 0; x < width; x++)
            for (var channel = 0; channel < 3; channel++)
                pixels[y * stride + 1 + x * 3 + channel] = (byte)(top[channel] + (bottom[channel] - top[channel]) * ratio);
        }

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; // bit depth
        header[9] = 2; // truecolour

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal))
            zlib.Write(pixels);

        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        return [.. signature, .. Chunk("IHDR", header), .. Chunk("IDAT", compressed.ToArray()), .. Chunk("IEND", [])];
    }

    private static byte[] Chunk(string type, byte[] content)
    {
        var chunk = new byte[12 + content.Length];
        BinaryPrimitives.WriteInt32BigEndian(chunk, content.Length);
        Encoding.ASCII.GetBytes(type).CopyTo(chunk, 4);
        content.CopyTo(chunk, 8);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8 + content.Length), Crc32(chunk.AsSpan(4, 4 + content.Length)));
        return chunk;
    }

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }

        return ~crc;
    }

    // HSL to RGB, the hue in turns (0 to 1).
    private static byte[] Rgb(double hue, double saturation, double lightness)
    {
        var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var sector = (hue - Math.Floor(hue)) * 6;
        var second = chroma * (1 - Math.Abs(sector % 2 - 1));
        var (r, g, b) = (int)sector switch
        {
            0 => (chroma, second, 0d),
            1 => (second, chroma, 0d),
            2 => (0d, chroma, second),
            3 => (0d, second, chroma),
            4 => (second, 0d, chroma),
            _ => (chroma, 0d, second),
        };
        var match = lightness - chroma / 2;
        return [(byte)((r + match) * 255), (byte)((g + match) * 255), (byte)((b + match) * 255)];
    }
}
