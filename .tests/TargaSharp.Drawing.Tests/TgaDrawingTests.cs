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
    [DataRow(PixelFormat.Format1bppIndexed)]
    [DataRow(PixelFormat.Format4bppIndexed)]
    [DataRow(PixelFormat.Format48bppRgb)]
    [DataRow(PixelFormat.Format64bppArgb)]
    [DataRow(PixelFormat.Format64bppPArgb)]
    public void FromBitmap_PixelFormatWithoutTgaEquivalent_ThrowsNotSupportedException(PixelFormat pixelFormat)
    {
        // 1/4bpp used to be copied as if they were 8bpp, reading past the end of the locked GDI+ buffer
        // (a fatal, uncatchable CLR error); 48/64bpp produced an invalid PixelDepth or threw from
        // AlphaChannelBits. All five must be rejected up front instead.
        using var bmp = new Bitmap(64, 64, pixelFormat);

        Assert.ThrowsExactly<NotSupportedException>(() => TgaDrawing.FromBitmap(bmp));
    }

    [TestMethod]
    [DataRow(PixelFormat.Format8bppIndexed, TgaPixelDepth.Bpp8)]
    [DataRow(PixelFormat.Format16bppRgb555, TgaPixelDepth.Bpp16)]
    [DataRow(PixelFormat.Format16bppArgb1555, TgaPixelDepth.Bpp16)]
    [DataRow(PixelFormat.Format24bppRgb, TgaPixelDepth.Bpp24)]
    [DataRow(PixelFormat.Format32bppRgb, TgaPixelDepth.Bpp32)]
    [DataRow(PixelFormat.Format32bppArgb, TgaPixelDepth.Bpp32)]
    [DataRow(PixelFormat.Format32bppPArgb, TgaPixelDepth.Bpp32)]
    public void FromBitmap_SupportedPixelFormat_ProducesValidFileWithMatchingDepth(PixelFormat pixelFormat, TgaPixelDepth expectedDepth)
    {
        using var bmp = new Bitmap(5, 3, pixelFormat); // width 5 forces GDI+ row padding on every depth

        var tga = TgaDrawing.FromBitmap(bmp);

        Assert.AreEqual(expectedDepth, tga.Header.ImageSpec.PixelDepth);
        Assert.AreEqual(5 * 3 * expectedDepth.BytesPerPixel(), tga.ImageArea.ImageData!.Length);
        var errors = tga.Validate();
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors.Select(e => $"{e.Path}: {e.Message}")));
    }

    [TestMethod]
    public void ToBitmap_NullFile_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ((TgaFile)null!).ToBitmap());
    }

    [TestMethod]
    public void ToBitmap_NoImageDataFile_ThrowsInvalidOperationException()
    {
        // Used to surface as GDI+'s opaque ArgumentException "Parameter is not valid" from new Bitmap(0, 0, ...).
        var tga = new TgaFile(0, 0);

        Assert.ThrowsExactly<InvalidOperationException>(() => tga.ToBitmap());
    }

    [TestMethod]
    public void ToBitmap_NullImageData_ThrowsInvalidOperationException()
    {
        var tga = new TgaFile(2, 2);
        tga.ImageArea.ImageData = null;

        Assert.ThrowsExactly<InvalidOperationException>(() => tga.ToBitmap());
    }

    [TestMethod]
    public void FromBitmap_X1R5G5B5Palette_PacksChannelsInSpecOrderLittleEndian()
    {
        // Pure red must land in bits 10-14 (0x7C00), stored little-endian as [0x00, 0x7C]. The writer used to
        // put red in bits 0-4 - the reader's blue - so palettes came back with R and B swapped.
        using var bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        for (int i = 0; i < palette.Entries.Length; i++) palette.Entries[i] = Color.FromArgb(255, 0, 0, 0);
        palette.Entries[0] = Color.FromArgb(255, 255, 0, 0);
        bmp.Palette = palette;

        var tga = TgaDrawing.FromBitmap(bmp, colorMap2BytesEntry: true);

        Assert.AreEqual(TgaColorMapEntrySize.X1R5G5B5, tga.Header.ColorMapSpec.ColorMapEntrySize);
        Assert.AreEqual(0x00, tga.ImageArea.ColorMapData![0]);
        Assert.AreEqual(0x7C, tga.ImageArea.ColorMapData[1]);

        using Bitmap roundTripped = tga.ToBitmap();
        Color entry = roundTripped.Palette.Entries[0];
        Assert.AreEqual((255, 0, 0), (entry.R, entry.G, entry.B));
    }

    [TestMethod]
    public void FromBitmap_A8R8G8B8PaletteWithTranslucentEntry_RoundTripsStoredAlphaWhenAlphaIsUsed()
    {
        // ReadEntry's 32-bit branch had the useAlpha test inverted: asking for alpha forced it to 255.
        using var bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        palette.Entries[0] = Color.FromArgb(0x80, 10, 20, 30);
        bmp.Palette = palette;

        var tga = TgaDrawing.FromBitmap(bmp);
        Assert.AreEqual(TgaColorMapEntrySize.A8R8G8B8, tga.Header.ColorMapSpec.ColorMapEntrySize);

        using Bitmap withAlpha = tga.ToBitmap(forceUseAlpha: true);
        Assert.AreEqual(Color.FromArgb(0x80, 10, 20, 30), withAlpha.Palette.Entries[0]);
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
        Assert.IsNotNull(tga.ImageArea.ColorMapData);
        byte[] colorMapData = tga.ImageArea.ColorMapData;

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
