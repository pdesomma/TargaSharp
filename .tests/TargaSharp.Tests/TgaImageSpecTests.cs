using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaImageSpec"/>.
/// </summary>
[TestClass]
public class TgaImageSpecTests
{
    [TestMethod]
    public void Ctor_AllFields_PopulatesProperties()
    {
        var descriptor = new TgaImageDescriptor { AlphaChannelBits = 8, ImageOrigin = TgaImageOrigin.TopLeft };

        var spec = new TgaImageSpec(1, 2, 3, 4, TgaPixelDepth.Bpp32, descriptor);

        Assert.AreEqual((ushort)1, spec.XOrigin);
        Assert.AreEqual((ushort)2, spec.YOrigin);
        Assert.AreEqual((ushort)3, spec.ImageWidth);
        Assert.AreEqual((ushort)4, spec.ImageHeight);
        Assert.AreEqual(TgaPixelDepth.Bpp32, spec.PixelDepth);
        Assert.AreEqual(descriptor, spec.ImageDescriptor);
    }

    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaImageSpec(bytes),
            x => x.ToBytes(),
            TgaImageSpec.Size,
            () => new TgaImageSpec(1, 2, 4, 4, TgaPixelDepth.Bpp24, new TgaImageDescriptor()));
    }
}
