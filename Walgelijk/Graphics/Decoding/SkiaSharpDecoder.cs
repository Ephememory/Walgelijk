using SkiaSharp;
using System;

namespace Walgelijk;

/// <summary>
/// Decodes the most common image formats (PNG, JPEG, BMP, etc.)
/// </summary>
public class SkiaSharpDecoder : IImageDecoder
{
    private static readonly byte[][] supportedHeaders =
    {
        "BM".ToByteArray(), //BPM
        "GIF".ToByteArray(),
        "GIF8".ToByteArray(),
        "P1".ToByteArray(), //PBM
        new byte[] { 0xFF, 0xD8 }, //JPEG
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, //PNG
        new byte[] { 0x89, 0x50, 0x4e, 0x47 }, //PNG
        new byte[] { 0x4d, 0x4d, 0x00, 0x2a }, //TIFF
        new byte[] { 0x49, 0x49, 0x2a, 0x00 }, //TIFF
        new byte[] { 0x00, 0x00 }, //TGA (success lol)
        "RIFF".ToByteArray(), //WebP (RIFF)
    };

    public DecodedImage Decode(in ReadOnlySpan<byte> bytes, bool flipY)
    {
        using var image = SKImage.FromEncodedData(bytes);
        using var rasterImage = image.ToRasterImage(ensurePixelData: true);
        var colors = new Color[image.Width * image.Height];
        CopyPixels(rasterImage, ref colors, flipY);
        return new DecodedImage(image.Width, image.Height, colors);
    }

    public DecodedImage Decode(in byte[] bytes, int count, bool flipY) => Decode(bytes.AsSpan(0, count), flipY);

    /// <summary>
    /// Copies pixels from <see cref="SKImage"/> to an array
    /// </summary>
    public static void CopyPixels(SKImage image, ref Color[] destination, bool flipY = true)
    {
        var pixelData = image.PeekPixels();
        var skColors = pixelData.GetPixelSpan<SKColor>();

        var width = pixelData.Width;
        var height = pixelData.Height;

        for (var index = 0; index < skColors.Length; index++)
        {
            var x = index % width;
            var y = index / width;
            var destIndex = 0;
            if (flipY)
                destIndex = x + (height - 1 - y) * width;
            else
                destIndex = index;
            
            destination[destIndex] = skColors[index].ToWalgelijk();
        }
    }

    public bool CanDecode(in string filename)
    {
        return
            e(filename, ".png") ||
            e(filename, ".jpeg") ||
            e(filename, ".jpg") ||
            e(filename, ".bmp") ||
            e(filename, ".webp") ||
            e(filename, ".tga") ||
            e(filename, ".tif") ||
            e(filename, ".tiff") ||
            e(filename, ".pbm") ||
            e(filename, ".gif");

        static bool e(in string filename, in string ex) =>
            filename.EndsWith(ex, StringComparison.InvariantCultureIgnoreCase);
    }

    public bool CanDecode(ReadOnlySpan<byte> raw)
    {
        foreach (var item in supportedHeaders)
            if (raw.StartsWith(item))
                return true;
        return false;
    }
}