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
        // and reconstructing from those bytes always yields a zero-filled Data placeholder of FieldSize
        // length (see the byte[] constructor's remarks), so the sample's own Data must already be
        // zero-filled for the round-tripped instance to compare equal to it.
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaDeveloperEntry(bytes),
            x => x.ToBytes(),
            TgaDeveloperEntry.Size,
            () => new TgaDeveloperEntry(7, 123, new byte[4]));
    }

    [TestMethod]
    public void Ctor_ExactSize_ConstructsWithEmptyData()
    {
        byte[] bytes = new byte[TgaDeveloperEntry.Size];

        var entry = new TgaDeveloperEntry(bytes);

        Assert.AreEqual(0, entry.FieldSize);
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
