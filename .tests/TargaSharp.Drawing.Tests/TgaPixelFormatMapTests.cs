using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using TargaSharp;
using TargaSharp.Drawing;

namespace TargaSharp.Drawing.Tests;

/// <summary>
/// Tests for <see cref="TgaPixelFormatMap"/>, the single supported-format table used in both conversion directions.
/// </summary>
[TestClass]
#if !NETFRAMEWORK
[SupportedOSPlatform("windows")]
#endif
public class TgaPixelFormatMapTests
{
    [TestMethod]
    [DataRow(PixelFormat.Format8bppIndexed, TgaPixelDepth.Bpp8, (byte)0, false, false)]
    [DataRow(PixelFormat.Format16bppGrayScale, TgaPixelDepth.Bpp16, (byte)0, true, false)]
    [DataRow(PixelFormat.Format16bppRgb555, TgaPixelDepth.Bpp16, (byte)0, false, false)]
    [DataRow(PixelFormat.Format16bppArgb1555, TgaPixelDepth.Bpp16, (byte)1, false, false)]
    [DataRow(PixelFormat.Format24bppRgb, TgaPixelDepth.Bpp24, (byte)0, false, false)]
    [DataRow(PixelFormat.Format32bppRgb, TgaPixelDepth.Bpp32, (byte)0, false, false)]
    [DataRow(PixelFormat.Format32bppArgb, TgaPixelDepth.Bpp32, (byte)8, false, false)]
    [DataRow(PixelFormat.Format32bppPArgb, TgaPixelDepth.Bpp32, (byte)8, false, true)]
    public void TryGet_SupportedFormat_ReturnsMapping(PixelFormat pixelFormat, TgaPixelDepth depth, byte alphaBits, bool grayscale, bool preMultiplied)
    {
        Assert.IsTrue(TgaPixelFormatMap.TryGet(pixelFormat, out TgaPixelFormatMapping mapping));

        Assert.AreEqual(new TgaPixelFormatMapping(pixelFormat, depth, alphaBits, grayscale, preMultiplied), mapping);
        Assert.AreEqual(alphaBits > 0, mapping.HasAlpha);
    }

    [TestMethod]
    [DataRow(PixelFormat.Format1bppIndexed)]
    [DataRow(PixelFormat.Format4bppIndexed)]
    [DataRow(PixelFormat.Format48bppRgb)]
    [DataRow(PixelFormat.Format64bppArgb)]
    [DataRow(PixelFormat.Undefined)]
    public void TryGet_UnsupportedFormat_ReturnsFalse(PixelFormat pixelFormat)
    {
        Assert.IsFalse(TgaPixelFormatMap.TryGet(pixelFormat, out _));
    }

    [TestMethod]
    [DataRow(TgaPixelDepth.Bpp8, false, false, false, PixelFormat.Format8bppIndexed)]
    [DataRow(TgaPixelDepth.Bpp8, true, true, false, PixelFormat.Format8bppIndexed)]
    [DataRow(TgaPixelDepth.Bpp16, false, true, false, PixelFormat.Format8bppIndexed)]
    [DataRow(TgaPixelDepth.Bpp16, false, false, false, PixelFormat.Format16bppRgb555)]
    [DataRow(TgaPixelDepth.Bpp16, true, false, false, PixelFormat.Format16bppArgb1555)]
    [DataRow(TgaPixelDepth.Bpp16, true, false, true, PixelFormat.Format16bppArgb1555)]
    [DataRow(TgaPixelDepth.Bpp24, false, false, false, PixelFormat.Format24bppRgb)]
    [DataRow(TgaPixelDepth.Bpp24, true, false, false, PixelFormat.Format24bppRgb)]
    [DataRow(TgaPixelDepth.Bpp32, false, false, true, PixelFormat.Format32bppRgb)]
    [DataRow(TgaPixelDepth.Bpp32, true, false, false, PixelFormat.Format32bppArgb)]
    [DataRow(TgaPixelDepth.Bpp32, true, false, true, PixelFormat.Format32bppPArgb)]
    public void Resolve_SupportedDepth_ReturnsMatchingFormat(TgaPixelDepth depth, bool useAlpha, bool grayscale, bool preMultiplied, PixelFormat expected)
    {
        Assert.AreEqual(expected, TgaPixelFormatMap.Resolve(depth, useAlpha, grayscale, preMultiplied));
    }

    [TestMethod]
    [DataRow(TgaPixelDepth.Other)]
    [DataRow((TgaPixelDepth)12)]
    public void Resolve_UnsupportedDepth_ThrowsNotSupportedException(TgaPixelDepth depth)
    {
        Assert.ThrowsExactly<NotSupportedException>(() => TgaPixelFormatMap.Resolve(depth, false, false, false));
    }

    [TestMethod]
    public void Mappings_EveryEntry_HasBytesPerPixelMatchingItsDepth()
    {
        foreach (TgaPixelFormatMapping m in TgaPixelFormatMap.Mappings)
            Assert.AreEqual(m.Depth.BytesPerPixel() * 8, Image.GetPixelFormatSize(m.PixelFormat), m.PixelFormat.ToString());
    }
}
