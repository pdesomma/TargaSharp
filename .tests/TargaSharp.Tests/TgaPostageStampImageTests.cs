using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaPostageStampImage"/>.
/// </summary>
[TestClass]
public class TgaPostageStampImageTests
{
    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        // Only the 2-byte Width/Height header is a fixed lower bound; Data is variable-length trailing
        // bytes, so an over-long array is valid (larger Data), not an error - hence the minLength variant.
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaPostageStampImage(bytes),
            x => x.ToBytes(),
            size: 6,
            sample: () => new TgaPostageStampImage(2, 1, [10, 20, 30, 40]),
            minLength: 2);
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

    [TestMethod]
    public void DefaultCtor_NewInstance_Is1x1AndRoundTrips()
    {
        var image = new TgaPostageStampImage();

        Assert.AreEqual((byte)1, image.Width);
        Assert.AreEqual((byte)1, image.Height);
        Assert.AreEqual(image, new TgaPostageStampImage(image.ToBytes()));
    }

    [TestMethod]
    public void DataSetter_Null_ThrowsArgumentNullException()
    {
        var stamp = new TgaPostageStampImage();

        Assert.ThrowsExactly<ArgumentNullException>(() => stamp.Data = null!);
    }

    [TestMethod]
    public void DataSetter_NonNull_IsStored()
    {
        var stamp = new TgaPostageStampImage();
        byte[] data = [1, 2, 3];

        stamp.Data = data;

        Assert.AreSame(data, stamp.Data);
    }

    [TestMethod]
    public void Ctor_Bytes_WidthZero_ThrowsArgumentOutOfRangeExceptionNamingBytes()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaPostageStampImage([0, 1]));

        Assert.AreEqual("bytes", ex.ParamName);
    }

    [TestMethod]
    public void Ctor_Bytes_HeightAboveMax_ThrowsArgumentOutOfRangeExceptionNamingBytes()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaPostageStampImage([1, TgaPostageStampImage.MaxSize + 1]));

        Assert.AreEqual("bytes", ex.ParamName);
    }

    [TestMethod]
    [DataRow(TgaPixelDepth.Bpp8, 2, 3, 6)]
    [DataRow(TgaPixelDepth.Bpp16, 2, 3, 12)]
    [DataRow(TgaPixelDepth.Bpp24, 4, 4, 48)]
    [DataRow(TgaPixelDepth.Bpp32, 64, 64, 16384)]
    public void DataLength_SizeAndDepth_ReturnsWidthTimesHeightTimesBytesPerPixel(TgaPixelDepth depth, int width, int height, int expected)
    {
        var stamp = new TgaPostageStampImage((byte)width, (byte)height, []);

        Assert.AreEqual(expected, stamp.DataLength(depth));
    }

    [TestMethod]
    public void DataLength_PixelDepthOther_ReturnsZero()
    {
        var stamp = new TgaPostageStampImage(2, 2, []);

        Assert.AreEqual(0, stamp.DataLength(TgaPixelDepth.Other));
    }
}
