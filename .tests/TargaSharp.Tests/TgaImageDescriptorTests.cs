using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaImageDescriptor"/>.
/// </summary>
[TestClass]
public class TgaImageDescriptorTests
{
    [TestMethod]
    public void AlphaChannelBits_SetTo15_SetsValue()
    {
        var descriptor = new TgaImageDescriptor { AlphaChannelBits = 15 };

        Assert.AreEqual((byte)15, descriptor.AlphaChannelBits);
    }

    [TestMethod]
    public void AlphaChannelBits_SetTo16_ThrowsArgumentOutOfRangeException()
    {
        var descriptor = new TgaImageDescriptor();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => descriptor.AlphaChannelBits = 16);
    }

    [TestMethod]
    public void ToByte_AlphaBits8AndOriginTopLeft_Returns0x28()
    {
        var descriptor = new TgaImageDescriptor { AlphaChannelBits = 8, ImageOrigin = TgaImageOrigin.TopLeft };

        Assert.AreEqual((byte)0x28, descriptor.ToByte());
    }

    [TestMethod]
    public void Ctor_FromByte0x2F_SetsAlphaChannelBitsAndImageOrigin()
    {
        var descriptor = new TgaImageDescriptor(0x2F);

        Assert.AreEqual((byte)15, descriptor.AlphaChannelBits);
        Assert.AreEqual(TgaImageOrigin.TopLeft, descriptor.ImageOrigin);
    }

    [TestMethod]
    public void ToByte_FromByte0x2F_RoundTripsTo0x2F()
    {
        var descriptor = new TgaImageDescriptor(0x2F);

        Assert.AreEqual((byte)0x2F, descriptor.ToByte());
    }
}
