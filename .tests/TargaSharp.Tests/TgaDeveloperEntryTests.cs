using System.Reflection;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaDeveloperEntry"/>.
/// </summary>
[TestClass]
public class TgaDeveloperEntryTests
{
    [TestMethod]
    public void Offset_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaDeveloperEntry).GetProperty(nameof(TgaDeveloperEntry.Offset))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void DefaultCtor_NewInstance_DataIsEmptyAndFieldSizeIsZero()
    {
        var entry = new TgaDeveloperEntry();

        Assert.IsNotNull(entry.Data);
        Assert.AreEqual(0, entry.Data.Length);
        Assert.AreEqual(0, entry.FieldSize);
    }

    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        // ToBytes() serializes only Tag/Offset/FieldSize (never Data itself - see TgaDeveloperEntry.ToBytes),
        // and reconstructing from those bytes leaves Data empty, so the sample must carry no Data for the
        // round-tripped instance to compare equal to it.
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaDeveloperEntry(bytes),
            x => x.ToBytes(),
            TgaDeveloperEntry.Size,
            () => new TgaDeveloperEntry(7, 123));
    }

    [TestMethod]
    public void Ctor_LargeDeclaredFieldSize_DoesNotAllocateAndRoundTripsBytes()
    {
        // Tag 1, Offset 2, FieldSize int.MaxValue (FF FF FF 7F) - used to allocate a 2 GB placeholder.
        byte[] bytes = [1, 0, 2, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0x7F];

        var entry = new TgaDeveloperEntry(bytes);

        Assert.AreEqual(0, entry.Data.Length);
        Assert.AreEqual(int.MaxValue, entry.FieldSize);
        CollectionAssert.AreEqual(bytes, entry.ToBytes());
    }

    [TestMethod]
    public void Ctor_DeclaredFieldSize_ThenDataSet_FieldSizeFollowsData()
    {
        var entry = new TgaDeveloperEntry([0, 0, 0, 0, 0, 0, 9, 0, 0, 0]);

        entry.Data = [1, 2, 3];

        Assert.AreEqual(3, entry.FieldSize);
        Assert.AreEqual(3u, TgaBinary.ReadUInt32(entry.ToBytes(), 6));
    }

    [TestMethod]
    public void ToBytes_KnownValues_WritesTagOffsetSizeAtFixedOffsets()
    {
        var entry = new TgaDeveloperEntry(0x1234, 0x01020304, new byte[0x0201]);

        byte[] bytes = entry.ToBytes();

        Assert.AreEqual(TgaDeveloperEntry.Size, bytes.Length);
        Assert.AreEqual((ushort)0x1234, TgaBinary.ReadUInt16(bytes, 0));
        Assert.AreEqual(0x01020304u, TgaBinary.ReadUInt32(bytes, 2));
        Assert.AreEqual(0x0201u, TgaBinary.ReadUInt32(bytes, 6));
        Assert.AreEqual(0x34, bytes[0]);
        Assert.AreEqual(0x04, bytes[2]);
        Assert.AreEqual(0x01, bytes[6]);
    }

    [TestMethod]
    public void Ctor_ExactSize_ConstructsWithEmptyData()
    {
        byte[] bytes = new byte[TgaDeveloperEntry.Size];

        var entry = new TgaDeveloperEntry(bytes);

        Assert.AreEqual(0, entry.FieldSize);
    }

    [TestMethod]
    public void Ctor_FieldSizeAboveIntMaxValue_ThrowsArgumentOutOfRangeException()
    {
        // FieldSize 0xFFFFFFFF used to wrap to -1 and surface as OverflowException from the placeholder allocation.
        byte[] bytes = [0, 0, 0, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaDeveloperEntry(bytes));
    }

    [TestMethod]
    public void CopyCtor_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaDeveloperEntry((TgaDeveloperEntry)null!));
    }

    [TestMethod]
    public void Ctor_LongerThanMinimum_RoundTripsThroughDataAndEquals()
    {
        ushort tag = 7;
        uint offset = 123;
        byte[] data = [10, 20, 30, 40];

        var original = new TgaDeveloperEntry(tag, offset, data);
        var roundTripped = new TgaDeveloperEntry(tag, offset, (byte[])original.Data.Clone());

        Assert.IsTrue(roundTripped.Equals(original));
    }

    [TestMethod]
    public void Ctor_FromToBytesOfEntryWithData_RoundTripsTagOffsetAndFieldSize()
    {
        ushort tag = 5;
        uint offset = 99;
        byte[] data = new byte[7];

        var original = new TgaDeveloperEntry(tag, offset, data);
        var roundTripped = new TgaDeveloperEntry(original.ToBytes());

        Assert.AreEqual(original.Tag, roundTripped.Tag);
        Assert.AreEqual(original.Offset, roundTripped.Offset);
        Assert.AreEqual(7, roundTripped.FieldSize);
    }

    [TestMethod]
    public void ToString_KnownValues_ReturnsExpectedFormat()
    {
        var entry = new TgaDeveloperEntry(7, 123, [10, 20, 30, 40]);

        string result = entry.ToString();

        Assert.AreEqual("Tag=7, Offset=123, FieldSize=4", result);
    }
}
