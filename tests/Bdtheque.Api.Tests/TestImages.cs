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
