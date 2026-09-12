using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaImageSpec"/>.
/// </summary>
[TestClass]
public class TgaImageSpecTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaImageSpec((byte[])null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaImageSpec.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaImageSpec(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = [1, 0, 2, 0, 4, 0, 4, 0, (byte)TgaPixelDepth.Bpp24, 0];

        var original = new TgaImageSpec(bytes);
        var roundTripped = new TgaImageSpec(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
