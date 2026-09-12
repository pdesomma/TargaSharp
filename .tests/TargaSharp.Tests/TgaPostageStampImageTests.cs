using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaPostageStampImage"/>.
/// </summary>
[TestClass]
public class TgaPostageStampImageTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaPostageStampImage((byte[])null!));
    }

    [TestMethod]
    public void Ctor_BytesShorterThanMinimum_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaPostageStampImage(bytes));
    }

    [TestMethod]
    public void Ctor_MinimumLength_ConstructsWithEmptyData()
    {
        byte[] bytes = [2, 2];

        var image = new TgaPostageStampImage(bytes);

        Assert.AreEqual((byte)2, image.Width);
        Assert.AreEqual((byte)2, image.Height);
        Assert.AreEqual(0, image.Data.Length);
    }

    [TestMethod]
    public void Ctor_WithImageData_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = [2, 1, 10, 20, 30, 40];

        var original = new TgaPostageStampImage(bytes);
        var roundTripped = new TgaPostageStampImage(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
