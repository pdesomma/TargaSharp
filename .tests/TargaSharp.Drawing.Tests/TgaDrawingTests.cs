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
#if !NETFRAMEWORK
[SupportedOSPlatform("windows")]
#endif
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
    [DataRow(PixelFormat.Format16bppGrayScale, TgaPixelDepth.Bpp16)]
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

    /// <summary>
    /// Builds a 2x2 24bpp file with four distinct pixels (row-major from the origin corner).
    /// </summary>
    /// <param name="origin">Image origin to declare in the descriptor.</param>
    /// <returns>The file.</returns>
    private static TgaFile CreateFourPixelFile(TgaImageOrigin origin)
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor, newFormat: false);
        tga.Header.ImageSpec.ImageDescriptor.ImageOrigin = origin;
        // B G R per pixel: red, green / blue, white.
        tga.ImageArea.ImageData = [0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255];
        return tga;
    }

    [TestMethod]
    [DataRow(PixelFormat.Format16bppRgb555)]
    [DataRow(PixelFormat.Format24bppRgb)]
    [DataRow(PixelFormat.Format32bppArgb)]
    [DataRow(PixelFormat.Format32bppRgb)]
    public void FromBitmap_ThenToBitmap_RoundTripsEveryPixel(PixelFormat pixelFormat)
    {
        // Width 5 forces GDI+ row padding, so a stride bug would shear the rows.
        using var bmp = new Bitmap(5, 3, pixelFormat);
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
                bmp.SetPixel(x, y, Color.FromArgb(255, x * 40, y * 80, x + y));

        TgaFile tga = TgaDrawing.FromBitmap(bmp);
        using Bitmap back = tga.ToBitmap();

        Assert.AreEqual(bmp.PixelFormat, back.PixelFormat);
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
                Assert.AreEqual(bmp.GetPixel(x, y), back.GetPixel(x, y), $"pixel ({x},{y})");
    }

    [TestMethod]
    public void FromBitmap_UseRle_RoundTripsEveryPixel()
    {
        using var bmp = new Bitmap(6, 2, PixelFormat.Format24bppRgb);
        for (int x = 0; x < 6; x++) { bmp.SetPixel(x, 0, Color.Red); bmp.SetPixel(x, 1, Color.FromArgb(x, x, x)); }

        TgaFile tga = TgaDrawing.FromBitmap(bmp, useRle: true);
        Assert.AreEqual(TgaImageType.RleTrueColor, tga.Header.ImageType);
        using Bitmap back = new TgaFile(tga.ToBytes()).ToBitmap();

        for (int x = 0; x < 6; x++)
        {
            Assert.AreEqual(bmp.GetPixel(x, 0), back.GetPixel(x, 0));
            Assert.AreEqual(bmp.GetPixel(x, 1), back.GetPixel(x, 1));
        }
    }

    [TestMethod]
    public void FromBitmap_LegacyFormat_HasNoExtensionAreaOrFooter()
    {
        using var bmp = new Bitmap(2, 2, PixelFormat.Format24bppRgb);

        TgaFile tga = TgaDrawing.FromBitmap(bmp, newFormat: false);

        Assert.IsNull(tga.ExtensionArea);
        Assert.IsNull(tga.Footer);
        Assert.AreEqual(0, tga.Validate().Count);
    }

    [TestMethod]
    [DataRow(PixelFormat.Format32bppRgb, (byte)0, TgaAttributeType.NoAlpha)]
    [DataRow(PixelFormat.Format32bppArgb, (byte)8, TgaAttributeType.UsefulAlpha)]
    [DataRow(PixelFormat.Format32bppPArgb, (byte)8, TgaAttributeType.PreMultipliedAlpha)]
    [DataRow(PixelFormat.Format16bppArgb1555, (byte)1, TgaAttributeType.UsefulAlpha)]
    public void FromBitmap_AlphaFormats_SetAlphaBitsAndAttributesType(PixelFormat pixelFormat, byte alphaBits, TgaAttributeType attributesType)
    {
        using var bmp = new Bitmap(2, 2, pixelFormat);

        TgaFile tga = TgaDrawing.FromBitmap(bmp);

        Assert.AreEqual(alphaBits, tga.Header.ImageSpec.ImageDescriptor.AlphaChannelBits);
        Assert.AreEqual(attributesType, tga.ExtensionArea!.AttributesType);
    }

    [TestMethod]
    public void FromBitmap_IdentityGrayPalette_ProducesGrayscaleWithoutColorMap()
    {
        // Used to emit ImageType Grayscale *and* a 256-entry color map, which the validator rejects - so an
        // 8-bit grayscale TGA could not survive ToBitmap -> FromBitmap -> Save.
        using var bmp = new Bitmap(3, 2, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        for (int i = 0; i < 256; i++) palette.Entries[i] = Color.FromArgb(i, i, i);
        bmp.Palette = palette;

        TgaFile tga = TgaDrawing.FromBitmap(bmp);

        Assert.AreEqual(TgaImageType.UncompressedGrayscale, tga.Header.ImageType);
        Assert.AreEqual(TgaColorMapType.NoColorMap, tga.Header.ColorMapType);
        Assert.IsNull(tga.ImageArea.ColorMapData);
        Assert.AreEqual(0, tga.Validate().Count);
    }

    [TestMethod]
    public void FromBitmap_NonIdentityGrayPalette_ProducesColorMappedFile()
    {
        // A reversed gray ramp carries information, so it must be kept as a palette rather than dropped.
        using var bmp = new Bitmap(3, 2, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        for (int i = 0; i < 256; i++) palette.Entries[i] = Color.FromArgb(255 - i, 255 - i, 255 - i);
        bmp.Palette = palette;

        TgaFile tga = TgaDrawing.FromBitmap(bmp);

        Assert.AreEqual(TgaImageType.UncompressedColorMapped, tga.Header.ImageType);
        Assert.AreEqual(TgaColorMapType.ColorMap, tga.Header.ColorMapType);
        Assert.AreEqual(0, tga.Validate().Count);
        using Bitmap back = tga.ToBitmap();
        Assert.AreEqual(Color.FromArgb(255, 255, 255), back.Palette.Entries[0]);
    }

    [TestMethod]
    public void GrayscaleTga_ToBitmap_ThenFromBitmap_ValidatesAndKeepsPixels()
    {
        var tga = new TgaFile(3, 1, TgaPixelDepth.Bpp8, TgaImageType.UncompressedGrayscale, newFormat: false);
        tga.ImageArea.ImageData = [0, 128, 255];

        using Bitmap bmp = tga.ToBitmap();
        TgaFile back = TgaDrawing.FromBitmap(bmp);

        Assert.AreEqual(0, back.Validate().Count);
        Assert.AreEqual(TgaImageType.UncompressedGrayscale, back.Header.ImageType);
        CollectionAssert.AreEqual(tga.ImageArea.ImageData, back.ImageArea.ImageData);
    }

    [TestMethod]
    public void FromBitmap_TranslucentPalette_ToBitmapWithoutForce_KeepsPaletteAlpha()
    {
        // Palette alpha is declared by the entry size, not the descriptor's attribute bits (0 for 8bpp), so a
        // plain ToBitmap() used to force every entry opaque; FromBitmap now also marks AttributesType UsefulAlpha.
        using var bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        palette.Entries[0] = Color.FromArgb(0x80, 10, 20, 30);
        bmp.Palette = palette;

        TgaFile tga = TgaDrawing.FromBitmap(bmp);
        Assert.AreEqual(TgaAttributeType.UsefulAlpha, tga.ExtensionArea!.AttributesType);
        using Bitmap back = tga.ToBitmap();

        Assert.AreEqual(Color.FromArgb(0x80, 10, 20, 30), back.Palette.Entries[0]);
    }

    [TestMethod]
    public void ToBitmap_X1R5G5B5PaletteWithForceUseAlpha_EntriesStayOpaque()
    {
        // X1R5G5B5 has no alpha bit; reading bit 15 (always written as 0) as alpha made every entry transparent.
        using var bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        for (int i = 0; i < palette.Entries.Length; i++) palette.Entries[i] = Color.FromArgb(255, i, 0, 0);
        bmp.Palette = palette;
        TgaFile tga = TgaDrawing.FromBitmap(bmp, colorMap2BytesEntry: true);
        Assert.AreEqual(TgaColorMapEntrySize.X1R5G5B5, tga.Header.ColorMapSpec.ColorMapEntrySize);

        using Bitmap back = tga.ToBitmap(forceUseAlpha: true);

        Assert.AreEqual(255, back.Palette.Entries[0].A);
    }

    [TestMethod]
    public void ToBitmap_FirstEntryIndex_PlacesPaletteEntriesAtIndexedSlots()
    {
        // Pixel index N addresses palette slot N; a color map starting at FirstEntryIndex 2 must land at slot 2.
        var tga = new TgaFile(1, 1, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped, newFormat: false);
        tga.Header.ColorMapSpec.FirstEntryIndex = 2;
        tga.Header.ColorMapSpec.ColorMapLength = 1;
        tga.ImageArea.ColorMapData = [30, 20, 10]; // B G R
        tga.ImageArea.ImageData = [2];

        using Bitmap bmp = tga.ToBitmap();

        Assert.AreEqual(Color.FromArgb(10, 20, 30), bmp.Palette.Entries[2]);
        Assert.AreEqual(Color.FromArgb(10, 20, 30), bmp.GetPixel(0, 0));
    }

    [TestMethod]
    [DataRow(TgaImageOrigin.TopLeft, 0, 0)]
    [DataRow(TgaImageOrigin.BottomLeft, 0, 1)]
    [DataRow(TgaImageOrigin.TopRight, 1, 0)]
    [DataRow(TgaImageOrigin.BottomRight, 1, 1)]
    public void ToBitmap_ImageOrigin_PlacesFirstStoredPixelAtOriginCorner(TgaImageOrigin origin, int expectedX, int expectedY)
    {
        using Bitmap bmp = CreateFourPixelFile(origin).ToBitmap();

        Assert.AreEqual(Color.FromArgb(255, 0, 0), bmp.GetPixel(expectedX, expectedY));
    }

    [TestMethod]
    public void ToBitmap_OversizedImageData_ThrowsInvalidOperationException()
    {
        // Marshal.Copy into the GDI+ buffer is unchecked; this used to corrupt the heap and kill the process.
        var tga = new TgaFile(4, 4);
        tga.ImageArea.ImageData = new byte[4 * 4 * 3 * 10];

        Assert.ThrowsExactly<InvalidOperationException>(() => tga.ToBitmap());
    }

    [TestMethod]
    public void ToBitmap_ShortImageData_ThrowsInvalidOperationException()
    {
        var tga = new TgaFile(4, 4);
        tga.ImageArea.ImageData = new byte[5];

        Assert.ThrowsExactly<InvalidOperationException>(() => tga.ToBitmap());
    }

    [TestMethod]
    public void ToBitmap_UnsupportedPixelDepth_ThrowsNotSupportedException()
    {
        var tga = new TgaFile(2, 2);
        tga.Header.ImageSpec.PixelDepth = (TgaPixelDepth)12;
        tga.ImageArea.ImageData = new byte[2 * 2 * 2];

        Assert.ThrowsExactly<NotSupportedException>(() => tga.ToBitmap());
    }

    [TestMethod]
    public void ToBitmap_UnsupportedColorMapEntrySize_ThrowsNotSupportedException()
    {
        // Used to silently leave GDI+'s default halftone palette in place.
        var tga = new TgaFile(1, 1, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped, newFormat: false);
        tga.Header.ColorMapSpec.ColorMapEntrySize = (TgaColorMapEntrySize)8;
        tga.Header.ColorMapSpec.ColorMapLength = 1;
        tga.ImageArea.ColorMapData = [0];
        tga.ImageArea.ImageData = [0];

        Assert.ThrowsExactly<NotSupportedException>(() => tga.ToBitmap());
    }

    [TestMethod]
    public void ToBitmap_KeyColorOn16BppGrayscale_ReturnsFormat32bppArgb()
    {
        // 16bpp gray now lands in 8bpp indexed, which MakeTransparent can rebuild as 32bppArgb like every other depth.
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp16, TgaImageType.UncompressedGrayscale, attrBits: 8);
        tga.ExtensionArea!.KeyColor = new TgaColorKey(255, 1, 2, 3);
        tga.ImageArea.ImageData = new byte[2 * 2 * 2];

        using Bitmap bmp = tga.ToBitmap();

        Assert.AreEqual(PixelFormat.Format32bppArgb, bmp.PixelFormat);
    }

    [TestMethod]
    public void GetPostageStampBitmap_NullFile_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ((TgaFile)null!).GetPostageStampBitmap());
    }

    [TestMethod]
    public void GetPostageStampBitmap_NoStamp_ReturnsNull()
    {
        Assert.IsNull(new TgaFile(2, 2).GetPostageStampBitmap());
    }

    [TestMethod]
    public void GetPostageStampBitmap_StampDataShorterThanDeclared_ReturnsNull()
    {
        // Used to return a zero-filled 4x4 bitmap from an empty Data array.
        var tga = new TgaFile(2, 2);
        tga.ExtensionArea!.PostageStampImage = new TgaPostageStampImage(4, 4, new byte[3]);

        Assert.IsNull(tga.GetPostageStampBitmap());
    }

    [TestMethod]
    public void GetPostageStampBitmap_UpdatedStamp_ReturnsStampSizedBitmapWithMainImagePixels()
    {
        TgaFile tga = CreateFourPixelFile(TgaImageOrigin.TopLeft);
        tga.UpdatePostageStampImage();

        using Bitmap? stamp = tga.GetPostageStampBitmap();

        Assert.IsNotNull(stamp);
        Assert.AreEqual(4, stamp.Width); // UpdatePostageStampImage pads to at least 4x4
        Assert.AreEqual(Color.FromArgb(255, 0, 0), stamp.GetPixel(0, 0));
    }

    [TestMethod]
    public void ToTgaColorKey_ToColor_RoundTripsArgb()
    {
        var color = Color.FromArgb(1, 2, 3, 4);

        TgaColorKey key = color.ToTgaColorKey();

        Assert.AreEqual((byte)1, key.A);
        Assert.AreEqual((byte)2, key.R);
        Assert.AreEqual((byte)3, key.G);
        Assert.AreEqual((byte)4, key.B);
        Assert.AreEqual(color, key.ToColor());
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
        var tga = new TgaFile();
        tga.ToNewFormat();

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

    [TestMethod]
    public void ToBitmap_16BppColorMapped_ThrowsNotSupportedException()
    {
        // GDI+ has no 16-bit indexed format; the indices used to be rendered as RGB555.
        var tga = new TgaFile(1, 1, TgaPixelDepth.Bpp16, TgaImageType.UncompressedColorMapped, newFormat: false);
        tga.Header.ColorMapSpec.ColorMapLength = 1;
        tga.ImageArea.ColorMapData = [30, 20, 10];
        tga.ImageArea.ImageData = [0, 0];

        Assert.ThrowsExactly<NotSupportedException>(() => tga.ToBitmap());
    }

    [TestMethod]
    public void ToBitmap_NullColorMapData_ThrowsInvalidOperationException()
    {
        // Used to leak NullReferenceException from the palette reader.
        var tga = new TgaFile(1, 1, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped, newFormat: false);
        tga.Header.ColorMapSpec.ColorMapLength = 256;
        tga.ImageArea.ColorMapData = null;
        tga.ImageArea.ImageData = [0];

        Assert.ThrowsExactly<InvalidOperationException>(() => tga.ToBitmap());
    }

    [TestMethod]
    public void ToBitmap_ShortColorMapData_ThrowsInvalidOperationException()
    {
        // Used to leak IndexOutOfRangeException from the palette reader.
        var tga = new TgaFile(1, 1, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped, newFormat: false);
        tga.Header.ColorMapSpec.ColorMapLength = 256;
        tga.ImageArea.ColorMapData = [1, 2, 3];
        tga.ImageArea.ImageData = [0];

        Assert.ThrowsExactly<InvalidOperationException>(() => tga.ToBitmap());
    }

    [TestMethod]
    public void ToBitmap_PartialColorMap_FillsUncoveredPaletteSlotsWithBlack()
    {
        // Slots outside the file's map used to keep GDI+'s default halftone palette.
        var tga = new TgaFile(1, 1, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped, newFormat: false);
        tga.Header.ColorMapSpec.ColorMapLength = 1;
        tga.ImageArea.ColorMapData = [30, 20, 10];
        tga.ImageArea.ImageData = [0];

        using Bitmap bmp = tga.ToBitmap();

        Assert.AreEqual(Color.FromArgb(10, 20, 30), bmp.Palette.Entries[0]);
        Assert.AreEqual(Color.FromArgb(0, 0, 0).ToArgb(), bmp.Palette.Entries[255].ToArgb());
    }

    [TestMethod]
    public void ToBitmap_KeyColorOn24Bpp_ReturnsFormat32bppArgb()
    {
        // MakeTransparent rebuilds the bitmap as 32bppArgb; the promotion is part of the documented contract.
        var tga = new TgaFile(1, 1, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor);
        tga.ExtensionArea!.KeyColor = new TgaColorKey(255, 1, 2, 3);
        tga.ImageArea.ImageData = [3, 2, 1];

        using Bitmap bmp = tga.ToBitmap();

        Assert.AreEqual(PixelFormat.Format32bppArgb, bmp.PixelFormat);
        Assert.AreEqual(0, bmp.GetPixel(0, 0).A);
    }

    [TestMethod]
    public void FromBitmap_PArgbWithLegacyFormat_ThrowsNotSupportedException()
    {
        // Without an extension area nothing records that the alpha is pre-multiplied.
        using var bmp = new Bitmap(1, 1, PixelFormat.Format32bppPArgb);

        Assert.ThrowsExactly<NotSupportedException>(() => TgaDrawing.FromBitmap(bmp, newFormat: false));
    }

    [TestMethod]
    [DataRow(PixelFormat.Format16bppArgb1555)]
    [DataRow(PixelFormat.Format32bppArgb)]
    [DataRow(PixelFormat.Format32bppPArgb)]
    public void FromBitmap_ThenToBitmap_TranslucentPixels_RoundTripsFormatAndBytes(PixelFormat pixelFormat)
    {
        using var bmp = new Bitmap(5, 2, pixelFormat);
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
                bmp.SetPixel(x, y, Color.FromArgb(x * 60, 200, y * 100, x + y));

        TgaFile tga = TgaDrawing.FromBitmap(bmp);
        using Bitmap back = tga.ToBitmap();

        Assert.AreEqual(pixelFormat, back.PixelFormat);
        CollectionAssert.AreEqual(TgaBitmapRows.Read(bmp), TgaBitmapRows.Read(back));
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
                Assert.AreEqual(bmp.GetPixel(x, y), back.GetPixel(x, y), $"pixel ({x},{y})");
    }

    [TestMethod]
    public void FromBitmap_ThenToBitmap_ColorMappedNonIdentityPalette_RoundTripsPaletteAndIndices()
    {
        using var bmp = new Bitmap(5, 2, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        for (int i = 0; i < 256; i++) palette.Entries[i] = Color.FromArgb(255, (i * 3) & 255, 255 - i, i);
        bmp.Palette = palette;
        byte[] indices = [0, 1, 2, 3, 4, 250, 200, 150, 100, 50];
        TgaBitmapRows.Write(bmp, indices);

        TgaFile tga = TgaDrawing.FromBitmap(bmp);
        using Bitmap back = tga.ToBitmap();

        Assert.AreEqual(TgaImageType.UncompressedColorMapped, tga.Header.ImageType);
        Assert.AreEqual(PixelFormat.Format8bppIndexed, back.PixelFormat);
        CollectionAssert.AreEqual(indices, TgaBitmapRows.Read(back));
        CollectionAssert.AreEqual(palette.Entries, back.Palette.Entries);
        Assert.AreEqual(bmp.GetPixel(4, 1), back.GetPixel(4, 1));
    }

    /// <summary>
    /// Builds a 2x2 file whose stored pixel k has value (k + 1) * 60 in its first channel (gray or blue).
    /// </summary>
    /// <param name="depth">8 (grayscale) or 32 (true-color).</param>
    /// <param name="origin">Image origin to declare.</param>
    /// <returns>The file.</returns>
    private static TgaFile CreateChannelRampFile(TgaPixelDepth depth, TgaImageOrigin origin)
    {
        TgaImageType type = depth == TgaPixelDepth.Bpp8 ? TgaImageType.UncompressedGrayscale : TgaImageType.UncompressedTrueColor;
        var tga = new TgaFile(2, 2, depth, type, newFormat: false);
        tga.Header.ImageSpec.ImageDescriptor.ImageOrigin = origin;
        int bpp = depth.BytesPerPixel();
        for (int k = 0; k < 4; k++)
        {
            tga.ImageArea.ImageData![k * bpp] = (byte)((k + 1) * 60);
            if (bpp == 4) tga.ImageArea.ImageData[k * bpp + 3] = 255;
        }
        return tga;
    }

    [TestMethod]
    [DataRow(TgaPixelDepth.Bpp8, TgaImageOrigin.BottomLeft, 0, 1)]
    [DataRow(TgaPixelDepth.Bpp8, TgaImageOrigin.TopRight, 1, 0)]
    [DataRow(TgaPixelDepth.Bpp8, TgaImageOrigin.BottomRight, 1, 1)]
    [DataRow(TgaPixelDepth.Bpp32, TgaImageOrigin.BottomLeft, 0, 1)]
    [DataRow(TgaPixelDepth.Bpp32, TgaImageOrigin.TopRight, 1, 0)]
    [DataRow(TgaPixelDepth.Bpp32, TgaImageOrigin.BottomRight, 1, 1)]
    public void ToBitmap_ImageOriginOn8And32Bpp_PlacesFirstStoredPixelAtOriginCorner(TgaPixelDepth depth, TgaImageOrigin origin, int expectedX, int expectedY)
    {
        using Bitmap bmp = CreateChannelRampFile(depth, origin).ToBitmap();

        Assert.AreEqual(60, bmp.GetPixel(expectedX, expectedY).B);
        Assert.AreEqual(240, bmp.GetPixel(1 - expectedX, 1 - expectedY).B);
    }

    [TestMethod]
    public void GetPostageStampBitmap_ColorMappedBottomLeft_AppliesPaletteAndFlips()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped);
        tga.Header.ImageSpec.ImageDescriptor.ImageOrigin = TgaImageOrigin.BottomLeft;
        tga.Header.ColorMapSpec.ColorMapLength = 4;
        tga.ImageArea.ColorMapData = [0, 0, 10, 0, 0, 20, 0, 0, 30, 0, 0, 40]; // B G R: red 10/20/30/40
        tga.ImageArea.ImageData = [0, 1, 2, 3];
        tga.ExtensionArea!.PostageStampImage = new TgaPostageStampImage(2, 2, [0, 1, 2, 3]);

        using Bitmap? stamp = tga.GetPostageStampBitmap();

        Assert.IsNotNull(stamp);
        Assert.AreEqual(PixelFormat.Format8bppIndexed, stamp.PixelFormat);
        // Stored row 0 (indices 0, 1) is the bottom row after the flip.
        Assert.AreEqual(30, stamp.GetPixel(0, 0).R);
        Assert.AreEqual(40, stamp.GetPixel(1, 0).R);
        Assert.AreEqual(10, stamp.GetPixel(0, 1).R);
    }

    [TestMethod]
    public void ToBitmap_A8R8G8B8PaletteWithNoAlphaAttributes_ForceUseAlphaOverridesVeto()
    {
        var tga = new TgaFile(1, 1, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped);
        tga.Header.ColorMapSpec.ColorMapEntrySize = TgaColorMapEntrySize.A8R8G8B8;
        tga.Header.ColorMapSpec.ColorMapLength = 1;
        tga.ImageArea.ColorMapData = [30, 20, 10, 0x80]; // B G R A
        tga.ImageArea.ImageData = [0];
        tga.ExtensionArea!.AttributesType = TgaAttributeType.NoAlpha;

        using Bitmap vetoed = tga.ToBitmap();
        using Bitmap forced = tga.ToBitmap(forceUseAlpha: true);

        Assert.AreEqual(255, vetoed.Palette.Entries[0].A);
        Assert.AreEqual(Color.FromArgb(0x80, 10, 20, 30), forced.Palette.Entries[0]);
    }

    [TestMethod]
    public void FromBitmap_PaletteAllAlpha250_KeepsAlphaEntries()
    {
        // Used to treat A >= 248 as opaque and silently rewrite the palette.
        using var bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        for (int i = 0; i < 256; i++) palette.Entries[i] = Color.FromArgb(250, i, 0, 0);
        bmp.Palette = palette;

        TgaFile tga = TgaDrawing.FromBitmap(bmp);
        using Bitmap back = tga.ToBitmap();

        Assert.AreEqual(TgaColorMapEntrySize.A8R8G8B8, tga.Header.ColorMapSpec.ColorMapEntrySize);
        Assert.AreEqual(TgaAttributeType.UsefulAlpha, tga.ExtensionArea!.AttributesType);
        Assert.AreEqual(250, back.Palette.Entries[7].A);
    }

    [TestMethod]
    public void FromBitmap_PaletteAllTransparent_KeepsAlphaEntries()
    {
        // An all-A=0 palette used to be written opaque because "no entry has alpha > 0".
        using var bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bmp.Palette;
        for (int i = 0; i < 256; i++) palette.Entries[i] = Color.FromArgb(0, i, 0, 0);
        bmp.Palette = palette;

        TgaFile tga = TgaDrawing.FromBitmap(bmp);
        using Bitmap back = tga.ToBitmap();

        Assert.AreEqual(TgaColorMapEntrySize.A8R8G8B8, tga.Header.ColorMapSpec.ColorMapEntrySize);
        Assert.AreEqual(0, back.Palette.Entries[7].A);
    }

    [TestMethod]
    public void ToBitmap_16BppGrayscale_ReturnsFormat8bppIndexedOfHighBytes()
    {
        // Format16bppGrayScale is unusable in GDI+ (GetPixel/Clone/Save all throw) and used to be written byte-inverted.
        var tga = new TgaFile(2, 1, TgaPixelDepth.Bpp16, TgaImageType.UncompressedGrayscale, newFormat: false);
        tga.ImageArea.ImageData = [0x34, 0x12, 0xFF, 0x80]; // little-endian: 0x1234, 0x80FF

        using Bitmap bmp = tga.ToBitmap();

        Assert.AreEqual(PixelFormat.Format8bppIndexed, bmp.PixelFormat);
        Assert.AreEqual(Color.FromArgb(0x12, 0x12, 0x12), bmp.GetPixel(0, 0));
        Assert.AreEqual(Color.FromArgb(0x80, 0x80, 0x80), bmp.GetPixel(1, 0));
    }

    [TestMethod]
    public void ToBitmap_Monochrome16Fixture_PinsHighBytePixelValues()
    {
        var tga = new TgaFile(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "monochrome16_top_left.tga")));

        using Bitmap bmp = tga.ToBitmap();

        Assert.AreEqual(PixelFormat.Format8bppIndexed, bmp.PixelFormat);
        Assert.AreEqual(186, bmp.GetPixel(0, 0).R);
        Assert.AreEqual(55, bmp.GetPixel(1, 0).R);
        Assert.AreEqual(196, bmp.GetPixel(32, 0).R);
        Assert.AreEqual(0, bmp.GetPixel(63, 0).R);
    }

    [TestMethod]
    public void ToBitmap_16BppGrayscale_ThenFromBitmap_Produces8BppGrayscaleOfHighBytes()
    {
        var tga = new TgaFile(2, 1, TgaPixelDepth.Bpp16, TgaImageType.UncompressedGrayscale, newFormat: false);
        tga.ImageArea.ImageData = [0x34, 0x12, 0xFF, 0x80];

        using Bitmap bmp = tga.ToBitmap();
        TgaFile back = TgaDrawing.FromBitmap(bmp);

        Assert.AreEqual(TgaImageType.UncompressedGrayscale, back.Header.ImageType);
        Assert.AreEqual(TgaPixelDepth.Bpp8, back.Header.ImageSpec.PixelDepth);
        CollectionAssert.AreEqual(new byte[] { 0x12, 0x80 }, back.ImageArea.ImageData);
        Assert.AreEqual(0, back.Validate().Count);
    }

    [TestMethod]
    public void FromBitmap_Format16bppGrayScale_CopiesRawBytesWithoutInversion()
    {
        using var bmp = new Bitmap(2, 1, PixelFormat.Format16bppGrayScale);
        byte[] raw = [0x34, 0x12, 0xFF, 0x80];
        TgaBitmapRows.Write(bmp, raw);

        TgaFile tga = TgaDrawing.FromBitmap(bmp);

        Assert.AreEqual(TgaImageType.UncompressedGrayscale, tga.Header.ImageType);
        Assert.AreEqual(TgaPixelDepth.Bpp16, tga.Header.ImageSpec.PixelDepth);
        CollectionAssert.AreEqual(raw, tga.ImageArea.ImageData);
    }
}
