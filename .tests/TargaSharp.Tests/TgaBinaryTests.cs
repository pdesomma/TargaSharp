using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaBinary"/> and <see cref="TgaByteBuilder"/>.
/// </summary>
[TestClass]
public class TgaBinaryTests
{
    [TestMethod]
    public void ReadUInt16_LittleEndianBytes_ReturnsDecodedValue()
    {
        byte[] bytes = [0x34, 0x12];

        ushort result = TgaBinary.ReadUInt16(bytes, 0);

        Assert.AreEqual((ushort)0x1234, result);
    }

    [TestMethod]
    public void ReadUInt16_NonZeroOffset_ReadsAtOffset()
    {
        byte[] bytes = [0xFF, 0x34, 0x12, 0xFF];

        ushort result = TgaBinary.ReadUInt16(bytes, 1);

        Assert.AreEqual((ushort)0x1234, result);
    }

    [TestMethod]
    public void ReadUInt16_NegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = [0x00, 0x00];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TgaBinary.ReadUInt16(bytes, -1));
    }

    [TestMethod]
    public void ReadUInt16_OffsetLeavesTooFewBytes_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = [0x00, 0x00, 0x00];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TgaBinary.ReadUInt16(bytes, 2));
    }

    [TestMethod]
    public void ReadUInt32_LittleEndianBytes_ReturnsDecodedValue()
    {
        byte[] bytes = [0x78, 0x56, 0x34, 0x12];

        uint result = TgaBinary.ReadUInt32(bytes, 0);

        Assert.AreEqual(0x12345678u, result);
    }

    [TestMethod]
    public void ReadUInt32_NonZeroOffset_ReadsAtOffset()
    {
        byte[] bytes = [0xFF, 0x78, 0x56, 0x34, 0x12, 0xFF];

        uint result = TgaBinary.ReadUInt32(bytes, 1);

        Assert.AreEqual(0x12345678u, result);
    }

    [TestMethod]
    public void ReadUInt32_NegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = [0x00, 0x00, 0x00, 0x00];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TgaBinary.ReadUInt32(bytes, -1));
    }

    [TestMethod]
    public void ReadUInt32_OffsetLeavesTooFewBytes_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = [0x00, 0x00, 0x00, 0x00, 0x00];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TgaBinary.ReadUInt32(bytes, 2));
    }

    [TestMethod]
    public void WriteUInt16_ThenReadUInt16_RoundTrips()
    {
        byte[] buffer = new byte[4];

        TgaBinary.WriteUInt16(buffer, 1, 0x1234);

        Assert.AreEqual((ushort)0x1234, TgaBinary.ReadUInt16(buffer, 1));
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x34, 0x12, 0x00 }, buffer);
    }

    [TestMethod]
    public void WriteUInt16_NegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[2];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TgaBinary.WriteUInt16(buffer, -1, 1));
    }

    [TestMethod]
    public void WriteUInt16_OffsetLeavesTooFewBytes_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TgaBinary.WriteUInt16(buffer, 0, 1));
    }

    [TestMethod]
    public void WriteUInt32_ThenReadUInt32_RoundTrips()
    {
        byte[] buffer = new byte[6];

        TgaBinary.WriteUInt32(buffer, 1, 0x12345678u);

        Assert.AreEqual(0x12345678u, TgaBinary.ReadUInt32(buffer, 1));
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x78, 0x56, 0x34, 0x12, 0x00 }, buffer);
    }

    [TestMethod]
    public void WriteUInt32_NegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[4];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TgaBinary.WriteUInt32(buffer, -1, 1u));
    }

    [TestMethod]
    public void WriteUInt32_OffsetLeavesTooFewBytes_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TgaBinary.WriteUInt32(buffer, 0, 1u));
    }

    [TestMethod]
    public void TgaByteBuilder_AddSequence_YieldsExactBytes()
    {
        byte[] result = new TgaByteBuilder()
            .Add((byte)0xAA)
            .Add((ushort)0x1234)
            .Add(0x12345678u)
            .Add(new byte[] { 0x01, 0x02 })
            .ToArray();

        CollectionAssert.AreEqual(new byte[] { 0xAA, 0x34, 0x12, 0x78, 0x56, 0x34, 0x12, 0x01, 0x02 }, result);
    }

    [TestMethod]
    public void TgaByteBuilder_AddNullByteArray_AddsNothing()
    {
        byte[] result = new TgaByteBuilder()
            .Add((byte)0x01)
            .Add((byte[]?)null)
            .Add((byte)0x02)
            .ToArray();

        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02 }, result);
    }

    [TestMethod]
    public void TgaByteBuilder_Empty_ReturnsEmptyArray()
    {
        byte[] result = new TgaByteBuilder().ToArray();

        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void RequireLength_ExactLength_DoesNotThrow()
    {
        TgaBinary.RequireLength(new byte[4], 4);
    }

    [TestMethod]
    public void RequireLength_Null_ThrowsArgumentNullExceptionNamingArgument()
    {
        byte[] bytes = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() => TgaBinary.RequireLength(bytes, 4));

        Assert.AreEqual("bytes", ex.ParamName);
    }

    [TestMethod]
    [DataRow(3)]
    [DataRow(5)]
    public void RequireLength_WrongLength_ThrowsArgumentOutOfRangeExceptionNamingArgument(int length)
    {
        byte[] bytes = new byte[length];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TgaBinary.RequireLength(bytes, 4));

        Assert.AreEqual("bytes", ex.ParamName);
        Assert.AreEqual(length, ex.ActualValue);
    }
}
