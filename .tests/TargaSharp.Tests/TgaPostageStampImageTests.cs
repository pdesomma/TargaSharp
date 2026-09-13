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

    [TestMethod]
    public void Ctor_WidthZero_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = [0, 2];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaPostageStampImage(bytes));
    }

    [TestMethod]
    public void Ctor_HeightGreaterThan64_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = [2, 65];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaPostageStampImage(bytes));
    }

    [TestMethod]
    public void Ctor_WidthHeightBytes_MaxSize64_ConstructsSuccessfully()
    {
        var image = new TgaPostageStampImage(64, 64, []);

        Assert.AreEqual((byte)64, image.Width);
        Assert.AreEqual((byte)64, image.Height);
    }

    [TestMethod]
    public void Ctor_WidthHeightBytes_WidthGreaterThan64_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaPostageStampImage(65, 10, []));
    }

    [TestMethod]
    public void Ctor_WidthHeightBytes_HeightZero_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaPostageStampImage(10, 0, []));
    }

    [TestMethod]
    public void WidthSetter_Zero_ThrowsArgumentOutOfRangeException()
    {
        var image = new TgaPostageStampImage();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.Width = 0);
    }

    [TestMethod]
    public void HeightSetter_GreaterThan64_ThrowsArgumentOutOfRangeException()
    {
        var image = new TgaPostageStampImage();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.Height = 65);
    }
}
