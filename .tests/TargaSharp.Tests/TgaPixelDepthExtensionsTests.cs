using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaPixelDepthExtensions"/>.
/// </summary>
[TestClass]
public class TgaPixelDepthExtensionsTests
{
    [TestMethod]
    [DataRow(TgaPixelDepth.Other, 0)]
    [DataRow(TgaPixelDepth.Bpp8, 1)]
    [DataRow(TgaPixelDepth.Bpp16, 2)]
    [DataRow(TgaPixelDepth.Bpp24, 3)]
    [DataRow(TgaPixelDepth.Bpp32, 4)]
    [DataRow((TgaPixelDepth)15, 2)]
    public void BytesPerPixel_PixelDepth_RoundsUpToWholeBytes(TgaPixelDepth depth, int expected)
    {
        Assert.AreEqual(expected, depth.BytesPerPixel());
    }

    [TestMethod]
    [DataRow(TgaColorMapEntrySize.Other, 0)]
    [DataRow(TgaColorMapEntrySize.X1R5G5B5, 2)]
    [DataRow(TgaColorMapEntrySize.A1R5G5B5, 2)]
    [DataRow(TgaColorMapEntrySize.R8G8B8, 3)]
    [DataRow(TgaColorMapEntrySize.A8R8G8B8, 4)]
    public void BytesPerPixel_ColorMapEntrySize_RoundsUpToWholeBytes(TgaColorMapEntrySize entrySize, int expected)
    {
        Assert.AreEqual(expected, entrySize.BytesPerPixel());
    }
}
