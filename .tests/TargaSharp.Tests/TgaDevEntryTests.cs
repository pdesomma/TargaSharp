using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaDevEntry"/>.
/// </summary>
[TestClass]
public class TgaDevEntryTests
{
    [TestMethod]
    public void DefaultCtor_NewInstance_DataIsEmptyAndFieldSizeIsZero()
    {
        var entry = new TgaDevEntry();

        Assert.IsNotNull(entry.Data);
        Assert.AreEqual(0, entry.Data.Length);
        Assert.AreEqual(0, entry.FieldSize);
    }

    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaDevEntry((byte[])null!));
    }

    [TestMethod]
    public void Ctor_BytesShorterThanSize_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaDevEntry.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaDevEntry(bytes));
    }

    [TestMethod]
    public void Ctor_BytesLongerThanSize_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaDevEntry.Size + 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaDevEntry(bytes));
    }

    [TestMethod]
    public void Ctor_ExactSize_ConstructsWithEmptyData()
    {
        byte[] bytes = new byte[TgaDevEntry.Size];

        var entry = new TgaDevEntry(bytes);

        Assert.AreEqual(0, entry.FieldSize);
    }

    [TestMethod]
    public void Ctor_LongerThanMinimum_RoundTripsThroughDataAndEquals()
    {
        ushort tag = 7;
        uint offset = 123;
        byte[] data = [10, 20, 30, 40];

        var original = new TgaDevEntry(tag, offset, data);
        var roundTripped = new TgaDevEntry(tag, offset, (byte[])original.Data.Clone());

        Assert.IsTrue(roundTripped.Equals(original));
    }

    [TestMethod]
    public void Ctor_FromToBytesOfEntryWithData_RoundTripsTagOffsetAndFieldSize()
    {
        ushort tag = 5;
        uint offset = 99;
        byte[] data = new byte[7];

        var original = new TgaDevEntry(tag, offset, data);
        var roundTripped = new TgaDevEntry(original.ToBytes());

        Assert.AreEqual(original.Tag, roundTripped.Tag);
        Assert.AreEqual(original.Offset, roundTripped.Offset);
        Assert.AreEqual(7, roundTripped.FieldSize);
    }

    [TestMethod]
    public void ToString_KnownValues_ReturnsExpectedFormat()
    {
        var entry = new TgaDevEntry(7, 123, [10, 20, 30, 40]);

        string result = entry.ToString();

        Assert.AreEqual("Tag=7, Offset=123, FieldSize=4", result);
    }
}
