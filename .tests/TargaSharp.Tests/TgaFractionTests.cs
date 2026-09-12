using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaFraction"/>.
/// </summary>
[TestClass]
public class TgaFractionTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaFraction(null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaFraction.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaFraction(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = [1, 0, 1, 0];

        var original = new TgaFraction(bytes);
        var roundTripped = new TgaFraction(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
