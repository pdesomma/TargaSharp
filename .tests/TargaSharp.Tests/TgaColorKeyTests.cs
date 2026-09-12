using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaColorKey"/>.
/// </summary>
[TestClass]
public class TgaColorKeyTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaColorKey((byte[])null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaColorKey.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaColorKey(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = [0x11, 0x22, 0x33, 0x44];

        var original = new TgaColorKey(bytes);
        var roundTripped = new TgaColorKey(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
