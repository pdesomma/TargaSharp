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

    [TestMethod]
    public void Empty_CalledTwice_ReturnsDistinctReferences()
    {
        TgaFraction first = TgaFraction.Empty;
        TgaFraction second = TgaFraction.Empty;

        Assert.AreNotSame(first, second);
    }

    [TestMethod]
    public void One_CalledTwice_ReturnsDistinctReferences()
    {
        TgaFraction first = TgaFraction.One;
        TgaFraction second = TgaFraction.One;

        Assert.AreNotSame(first, second);
    }
}
