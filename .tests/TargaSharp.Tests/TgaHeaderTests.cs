using System.Reflection;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaHeader"/>.
/// </summary>
[TestClass]
public class TgaHeaderTests
{
    [TestMethod]
    public void IdLength_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaHeader).GetProperty(nameof(TgaHeader.IdLength))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void DefaultCtor_NewInstance_ColorMapSpecAndImageSpecAreNonNull()
    {
        var header = new TgaHeader();

        Assert.IsNotNull(header.ColorMapSpec);
        Assert.IsNotNull(header.ImageSpec);
    }

    [TestMethod]
    public void ToBytes_DefaultInstance_LengthEqualsSize()
    {
        var header = new TgaHeader();

        byte[] bytes = header.ToBytes();

        Assert.AreEqual(TgaHeader.Size, bytes.Length);
    }

    [TestMethod]
    public void ImageDataLength_WidthHeightDepth_MultipliesInLong()
    {
        var header = new TgaHeader { ImageType = TgaImageType.UncompressedTrueColor };
        header.ImageSpec.ImageWidth = ushort.MaxValue;
        header.ImageSpec.ImageHeight = ushort.MaxValue;
        header.ImageSpec.PixelDepth = TgaPixelDepth.Bpp32;

        Assert.AreEqual(65535L * 65535 * 4, header.ImageDataLength);
    }

    [TestMethod]
    public void ImageDataLength_NoImageData_ReturnsZero()
    {
        var header = new TgaHeader { ImageType = TgaImageType.NoImageData };
        header.ImageSpec.ImageWidth = 4;
        header.ImageSpec.ImageHeight = 4;
        header.ImageSpec.PixelDepth = TgaPixelDepth.Bpp24;

        Assert.AreEqual(0L, header.ImageDataLength);
    }

    [TestMethod]
    public void ColorMapDataLength_ColorMap_MultipliesLengthByEntryBytes()
    {
        var header = new TgaHeader { ColorMapType = TgaColorMapType.ColorMap };
        header.ColorMapSpec.ColorMapLength = 256;
        header.ColorMapSpec.ColorMapEntrySize = TgaColorMapEntrySize.A1R5G5B5;

        Assert.AreEqual(512, header.ColorMapDataLength);
    }

    [TestMethod]
    public void ColorMapDataLength_NoColorMapWithStaleSpec_ReturnsZero()
    {
        var header = new TgaHeader { ColorMapType = TgaColorMapType.NoColorMap };
        header.ColorMapSpec.ColorMapLength = 256;
        header.ColorMapSpec.ColorMapEntrySize = TgaColorMapEntrySize.R8G8B8;

        Assert.AreEqual(0, header.ColorMapDataLength);
    }

    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaHeader(bytes),
            x => x.ToBytes(),
            TgaHeader.Size,
            () => new TgaHeader());
    }
}
