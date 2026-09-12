using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using TargaSharp;
using TargaSharp.Drawing;

namespace TargaSharp.Drawing.Tests;

/// <summary>
/// Tests for <see cref="TgaDrawing.FromBitmap(Bitmap, bool, bool, bool)"/> and the
/// <see cref="TgaFileDrawingExtensions"/> <see cref="Bitmap"/> bridge.
/// </summary>
[TestClass]
[SupportedOSPlatform("windows")]
public class TgaDrawingTests
{
    [TestMethod]
    public void FromBitmap_NullBitmap_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => TgaDrawing.FromBitmap(null!));
    }

    [TestMethod]
    public void FromBitmap_BitmapWithA1R5G5B5Palette_PreservesAlphaBitInColorMapData()
    {
        // Format8bppIndexed => IsColorMapped, and colorMap2BytesEntry: true + a palette that mixes
        // opaque/transparent entries pushes FromBitmap down the A1R5G5B5 write path (see bug #1: the
        // alpha bit was shifted into bit 22 instead of bit 15, so it was always lost on write since
        // only the low 2 bytes of the packed int are ever copied into ColorMapData).
        using var bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        palette.Entries[0] = Color.FromArgb(255, 10, 20, 30); // opaque -> alpha bit must be set
        palette.Entries[1] = Color.FromArgb(0, 40, 50, 60);   // transparent -> alpha bit must be clear
        bmp.Palette = palette;

        var tga = TgaDrawing.FromBitmap(bmp, colorMap2BytesEntry: true);

        Assert.AreEqual(TgaColorMapEntrySize.A1R5G5B5, tga.Header.ColorMapSpec.ColorMapEntrySize);
        Assert.IsNotNull(tga.ImageOrColorMapArea.ColorMapData);
        byte[] colorMapData = tga.ImageOrColorMapArea.ColorMapData;

        // Each packed A1R5G5B5 entry is 2 bytes, little-endian; the alpha bit is bit 15, i.e. bit 7 of
        // the high byte (byte[1] & 0x80).
        Assert.IsTrue((colorMapData[1] & 0x80) != 0, "Opaque palette entry (A=255) must have its alpha bit set.");
        Assert.IsTrue((colorMapData[3] & 0x80) == 0, "Transparent palette entry (A=0) must have its alpha bit clear.");

        using Bitmap roundTripped = tga.ToBitmap(forceUseAlpha: true);
        Color[] roundTrippedPalette = roundTripped.Palette.Entries;
        Assert.AreEqual(255, roundTrippedPalette[0].A);
        Assert.AreEqual(0, roundTrippedPalette[1].A);
    }
}
