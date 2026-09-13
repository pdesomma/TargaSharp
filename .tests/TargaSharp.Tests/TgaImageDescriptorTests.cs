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

    [TestMethod]
    public void Ctor_FromByteWithReservedBits76Set_IgnoresReservedBits()
    {
        // 0xEF = 11101111: reserved bits 7-6 set, origin bits 5-4 = 10 (TopLeft), alpha bits 3-0 = 1111.
        var descriptor = new TgaImageDescriptor(0xEF);

        Assert.AreEqual(TgaImageOrigin.TopLeft, descriptor.ImageOrigin);
        Assert.AreEqual((byte)15, descriptor.AlphaChannelBits);
    }

    [TestMethod]
    public void ToByte_ImageOriginOutOfRangeFromUncheckedCast_MasksReservedBits()
    {
        var descriptor = new TgaImageDescriptor { ImageOrigin = (TgaImageOrigin)0xFF, AlphaChannelBits = 0 };

        Assert.AreEqual((byte)0x30, descriptor.ToByte());
    }
}
