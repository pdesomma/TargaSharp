using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaImageSpec"/>.
/// </summary>
[TestClass]
public class TgaImageSpecTests
{
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
