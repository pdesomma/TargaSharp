using System.Reflection;
using TargaSharp.IO;

namespace TargaSharp.Tests.IO;

/// <summary>
/// Pixel-level round trips through <see cref="TgaWriter"/>/<see cref="TgaReader"/>: a small,
/// deterministically-patterned image is written and read back at every pixel depth, compression mode
/// and origin, asserting the image bytes and the relevant header fields survive byte-for-byte.
/// </summary>
[TestClass]
public class PixelRoundTripTests
{
    /// <summary>
    /// Test image width, chosen odd/small so RLE packets both start and end mid-run across scanlines.
    /// </summary>
    private const int Width = 5;

    /// <summary>
    /// Test image height.
    /// </summary>
    private const int Height = 3;

    /// <summary>
    /// One row per (pixel depth, compression, origin) combination: 4 depths x 2 compression modes x 4
    /// origins = 32 cases. Each depth is paired with the image type (grayscale for 8bpp, true-color for
    /// the rest) and alpha bit count that <see cref="TargaSharp.Validation.TgaValidator"/> requires for it.
    /// </summary>
    /// <returns>One object[] of (depth, imageType, alphaBits, origin) per case.</returns>
    public static IEnumerable<object[]> GetCases()
    {
        (TgaPixelDepth depth, TgaImageType uncompressed, TgaImageType rle, byte alphaBits)[] formats =
        [
            (TgaPixelDepth.Bpp8, TgaImageType.UncompressedGrayscale, TgaImageType.RleGrayscale, 0),
            (TgaPixelDepth.Bpp16, TgaImageType.UncompressedTrueColor, TgaImageType.RleTrueColor, 1),
            (TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor, TgaImageType.RleTrueColor, 0),
            (TgaPixelDepth.Bpp32, TgaImageType.UncompressedTrueColor, TgaImageType.RleTrueColor, 8),
        ];
        TgaImageOrigin[] origins = [TgaImageOrigin.BottomLeft, TgaImageOrigin.BottomRight, TgaImageOrigin.TopLeft, TgaImageOrigin.TopRight];

        foreach (var format in formats)
            foreach (bool rle in new[] { false, true })
                foreach (var origin in origins)
                    yield return new object[] { format.depth, rle ? format.rle : format.uncompressed, format.alphaBits, origin };
    }

    /// <summary>
    /// Returns the display name for a pixel round-trip test case, shown in the test explorer.
    /// </summary>
    /// <param name="methodInfo">The test method being invoked.</param>
    /// <param name="data">The data row for this case: (depth, imageType, alphaBits, origin).</param>
    /// <returns>A human-readable test case name.</returns>
    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data) =>
        $"{methodInfo.Name} ({data[0]}, {data[1]}, alpha={data[2]}, {data[3]})";

    /// <summary>
    /// Writes a deterministically-patterned image at the given depth/type/origin, reads it back, and
    /// asserts the image data is byte-identical and the origin/depth/type header fields are preserved.
    /// </summary>
    /// <param name="depth">Pixel depth under test.</param>
    /// <param name="imageType">Image type (compressed or not, grayscale or true-color) under test.</param>
    /// <param name="alphaBits">Alpha channel bit count valid for <paramref name="depth"/>.</param>
    /// <param name="origin">Image origin under test.</param>
    [TestMethod]
    [DynamicData(nameof(GetCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public void WriteThenRead_DeterministicPixelPattern_RoundTripsExactly(TgaPixelDepth depth, TgaImageType imageType, byte alphaBits, TgaImageOrigin origin)
    {
        var file = new TgaFile(Width, Height, depth, imageType, alphaBits);
        file.Header.ImageSpec.ImageDescriptor.ImageOrigin = origin;
        file.ImageArea.ImageData = BuildPixelPattern(Width, Height, depth.BytesPerPixel());

        byte[] bytes = new TgaWriter().Write(file);
        TgaFile reloaded = new TgaReader().Read(bytes);

        CollectionAssert.AreEqual(file.ImageArea.ImageData, reloaded.ImageArea.ImageData);
        Assert.AreEqual(origin, reloaded.Header.ImageSpec.ImageDescriptor.ImageOrigin);
        Assert.AreEqual(depth, reloaded.Header.ImageSpec.PixelDepth);
        Assert.AreEqual(imageType, reloaded.Header.ImageType);
    }

    /// <summary>
    /// Same round trip as <see cref="WriteThenRead_DeterministicPixelPattern_RoundTripsExactly"/>, but
    /// for the one pixel format the depth/type matrix above cannot express: 8bpp color-mapped RLE with a
    /// real (non-empty) palette.
    /// </summary>
    [TestMethod]
    public void WriteThenRead_ColorMapped8BppRle_RoundTripsPaletteAndIndices()
    {
        var file = new TgaFile(Width, Height, TgaPixelDepth.Bpp8, TgaImageType.RleColorMapped);
        file.Header.ColorMapSpec.ColorMapLength = 4;
        file.ImageArea.ColorMapData =
        [
            0, 0, 0,        // index 0: black
            0, 0, 255,      // index 1: red (BGR order, see TgaImageArea.ImageData)
            0, 255, 0,      // index 2: green
            255, 0, 0,      // index 3: blue
        ];
        file.ImageArea.ImageData = BuildIndexPattern(Width, Height, paletteSize: 4);

        byte[] bytes = new TgaWriter().Write(file);
        TgaFile reloaded = new TgaReader().Read(bytes);

        CollectionAssert.AreEqual(file.ImageArea.ColorMapData, reloaded.ImageArea.ColorMapData);
        CollectionAssert.AreEqual(file.ImageArea.ImageData, reloaded.ImageArea.ImageData);
        Assert.AreEqual(TgaColorMapType.ColorMap, reloaded.Header.ColorMapType);
        Assert.AreEqual((ushort)4, reloaded.Header.ColorMapSpec.ColorMapLength);
        Assert.AreEqual(TgaColorMapEntrySize.R8G8B8, reloaded.Header.ColorMapSpec.ColorMapEntrySize);
    }

    /// <summary>
    /// Builds a deterministic (x, y, channel)-dependent pixel pattern, so a byte-order or scanline-order
    /// mistake in either the writer or the reader would be caught by a plain byte comparison.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="bytesPerPixel">Bytes per pixel for the depth under test.</param>
    /// <returns>Image data of length <paramref name="width"/> * <paramref name="height"/> * <paramref name="bytesPerPixel"/>.</returns>
    private static byte[] BuildPixelPattern(int width, int height, int bytesPerPixel)
    {
        byte[] data = new byte[width * height * bytesPerPixel];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                for (int c = 0; c < bytesPerPixel; c++)
                    data[(y * width + x) * bytesPerPixel + c] = unchecked((byte)((x * 31) + (y * 17) + (c * 7) + 5));
        return data;
    }

    /// <summary>
    /// Builds a deterministic (x + y) mod <paramref name="paletteSize"/> color-map index pattern.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="paletteSize">Number of palette entries the indices must stay within.</param>
    /// <returns>Image data of length <paramref name="width"/> * <paramref name="height"/> (one index byte per pixel).</returns>
    private static byte[] BuildIndexPattern(int width, int height, int paletteSize)
    {
        byte[] data = new byte[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                data[(y * width) + x] = unchecked((byte)((x + y) % paletteSize));
        return data;
    }
}
