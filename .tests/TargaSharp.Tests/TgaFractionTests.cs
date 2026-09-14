using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaFraction"/>.
/// </summary>
[TestClass]
public class TgaFractionTests
{
    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaFraction(bytes),
            x => x.ToBytes(),
            TgaFraction.Size,
            () => new TgaFraction(1, 1));
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

    [TestMethod]
    public void Value_ZeroOverZero_ReturnsNullAndIsUnspecified()
    {
        var fraction = new TgaFraction(0, 0);

        Assert.IsNull(fraction.Value);
        Assert.IsTrue(fraction.IsUnspecified);
    }

    [TestMethod]
    public void Value_OneOverOne_ReturnsOne()
    {
        var fraction = new TgaFraction(1, 1);

        Assert.AreEqual(1f, fraction.Value);
        Assert.IsFalse(fraction.IsUnspecified);
    }

    [TestMethod]
    public void Value_FourOverThree_ReturnsFourThirds()
    {
        var fraction = new TgaFraction(4, 3);

        Assert.AreEqual(4f / 3f, fraction.Value);
        Assert.IsFalse(fraction.IsUnspecified);
    }

    [TestMethod]
    public void Value_EqualNonZeroNumeratorAndDenominator_ReturnsOne()
    {
        var fraction = new TgaFraction(7, 7);

        Assert.AreEqual(1f, fraction.Value);
    }
}
