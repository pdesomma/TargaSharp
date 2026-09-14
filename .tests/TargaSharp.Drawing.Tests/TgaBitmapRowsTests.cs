using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using TargaSharp.Drawing;

namespace TargaSharp.Drawing.Tests;

/// <summary>
/// Tests for <see cref="TgaBitmapRows"/>, the shared LockBits row copier.
/// </summary>
[TestClass]
#if !NETFRAMEWORK
[SupportedOSPlatform("windows")]
#endif
public class TgaBitmapRowsTests
{
    [TestMethod]
    public void Read_NullBitmap_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => TgaBitmapRows.Read(null!));
    }

    [TestMethod]
    public void Write_NullBitmap_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => TgaBitmapRows.Write(null!, []));
    }

    [TestMethod]
    public void Write_NullPixels_ThrowsArgumentNullException()
    {
        using var bmp = new Bitmap(1, 1, PixelFormat.Format24bppRgb);

        Assert.ThrowsExactly<ArgumentNullException>(() => TgaBitmapRows.Write(bmp, null!));
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    public void Write_WrongLength_ThrowsArgumentException(int length)
    {
        // Marshal.Copy into Scan0 is unchecked; a wrong-sized buffer must be refused before it touches GDI+ memory.
        using var bmp = new Bitmap(1, 1, PixelFormat.Format24bppRgb);

        Assert.ThrowsExactly<ArgumentException>(() => TgaBitmapRows.Write(bmp, new byte[length]));
    }

    [TestMethod]
    [DataRow(PixelFormat.Format8bppIndexed)]
    [DataRow(PixelFormat.Format16bppRgb555)]
    [DataRow(PixelFormat.Format24bppRgb)]
    [DataRow(PixelFormat.Format32bppArgb)]
    public void Write_ThenRead_RoundTripsUnpaddedRows(PixelFormat pixelFormat)
    {
        // Width 5 forces GDI+ row padding on every depth, so a stride bug would shear the rows.
        using var bmp = new Bitmap(5, 3, pixelFormat);
        byte[] pixels = new byte[TgaBitmapRows.StrideBytes(bmp) * 3];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = (byte)(i * 7);

        TgaBitmapRows.Write(bmp, pixels);

        Assert.AreEqual(5 * Image.GetPixelFormatSize(pixelFormat) / 8, TgaBitmapRows.StrideBytes(bmp));
        CollectionAssert.AreEqual(pixels, TgaBitmapRows.Read(bmp));
    }

    [TestMethod]
    public void Write_24bpp_PlacesBytesAtExpectedPixels()
    {
        using var bmp = new Bitmap(2, 2, PixelFormat.Format24bppRgb);

        // B G R per pixel: red, green / blue, white.
        TgaBitmapRows.Write(bmp, [0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255]);

        Assert.AreEqual(Color.FromArgb(255, 0, 0), bmp.GetPixel(0, 0));
        Assert.AreEqual(Color.FromArgb(0, 255, 0), bmp.GetPixel(1, 0));
        Assert.AreEqual(Color.FromArgb(0, 0, 255), bmp.GetPixel(0, 1));
        Assert.AreEqual(Color.FromArgb(255, 255, 255), bmp.GetPixel(1, 1));
    }
}
