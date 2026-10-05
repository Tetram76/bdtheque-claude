using System.Buffers.Binary;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>Images built on the fly for the tests of the visuals.</summary>
internal static class TestImages
{
    public static byte[] Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.DarkOrange);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    /// <summary>A JPEG whose left half is <paramref name="left"/> and right half <paramref name="right"/>.</summary>
    public static byte[] EncodeHalves(int width, int height, SKColor left, SKColor right)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(left);
            using var paint = new SKPaint { Color = right };
            canvas.DrawRect(width / 2f, 0, width / 2f, height, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    public static SKSizeI SizeOf(byte[] image)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(image));
        return codec.Info.Size;
    }

    /// <summary>
    /// Inserts, right after the start of a JPEG, an EXIF segment holding only the given orientation
    /// tag: Skia writes no EXIF, whereas phones store the orientation of their photos this way.
    /// </summary>
    public static byte[] WithExifOrientation(byte[] jpeg, ushort orientation)
    {
        // TIFF header (big endian), one IFD with a single entry: tag 0x0112 (orientation), SHORT, 1 value.
        var tiff = new byte[8 + 2 + 12 + 4];
        "MM"u8.CopyTo(tiff);
        BinaryPrimitives.WriteUInt16BigEndian(tiff.AsSpan(2), 42);
        BinaryPrimitives.WriteUInt32BigEndian(tiff.AsSpan(4), 8);
        BinaryPrimitives.WriteUInt16BigEndian(tiff.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt16BigEndian(tiff.AsSpan(10), 0x0112);
        BinaryPrimitives.WriteUInt16BigEndian(tiff.AsSpan(12), 3);
        BinaryPrimitives.WriteUInt32BigEndian(tiff.AsSpan(14), 1);
        BinaryPrimitives.WriteUInt16BigEndian(tiff.AsSpan(18), orientation);

        byte[] exif = [.. "Exif\0\0"u8, .. tiff];
        var segment = new byte[4 + exif.Length];
        segment[0] = 0xFF;
        segment[1] = 0xE1;
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(2 + exif.Length));
        exif.CopyTo(segment, 4);

        return [.. jpeg[..2], .. segment, .. jpeg[2..]];
    }

    /// <summary>
    /// A PNG declaring the given dimensions, whose pixel data is a single empty row: enough for its
    /// header to be read, without the test ever building an image of that size.
    /// </summary>
    public static byte[] PngHeader(int width, int height)
    {
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; // bit depth
        header[9] = 2; // truecolour
        // An empty zlib stream: the decoding would fail, the header alone is meant to be read.
        byte[] data = [0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        return [.. signature, .. Chunk("IHDR", header), .. Chunk("IDAT", data), .. Chunk("IEND", [])];

        static byte[] Chunk(string type, byte[] content)
        {
            var chunk = new byte[12 + content.Length];
            BinaryPrimitives.WriteInt32BigEndian(chunk, content.Length);
            System.Text.Encoding.ASCII.GetBytes(type).CopyTo(chunk, 4);
            content.CopyTo(chunk, 8);
            BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8 + content.Length), Crc32(chunk.AsSpan(4, 4 + content.Length)));
            return chunk;
        }

        static uint Crc32(ReadOnlySpan<byte> bytes)
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
    }

    /// <summary>A 1×1 BMP, a format Skia decodes but that is not accepted for a visual.</summary>
    public static byte[] Bitmap1x1()
    {
        var bmp = new byte[58];
        "BM"u8.CopyTo(bmp);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), 54);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), 1);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28), 24);
        bmp[54] = 0x00;
        bmp[55] = 0x80;
        bmp[56] = 0xFF;
        return bmp;
    }
}
