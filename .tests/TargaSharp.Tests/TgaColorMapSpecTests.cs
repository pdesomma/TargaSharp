using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaColorMapSpec"/>.
/// </summary>
[TestClass]
public class TgaColorMapSpecTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaColorMapSpec(null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaColorMapSpec.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaColorMapSpec(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = [0x01, 0x02, 0x03, 0x04, 24];

        var original = new TgaColorMapSpec(bytes);
        var roundTripped = new TgaColorMapSpec(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
