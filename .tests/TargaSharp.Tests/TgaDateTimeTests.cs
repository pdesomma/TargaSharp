using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaDateTime"/>.
/// </summary>
[TestClass]
public class TgaDateTimeTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaDateTime((byte[])null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaDateTime.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaDateTime(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = BitConverter.GetBytes((ushort)1)
            .Concat(BitConverter.GetBytes((ushort)2))
            .Concat(BitConverter.GetBytes((ushort)1989))
            .Concat(BitConverter.GetBytes((ushort)3))
            .Concat(BitConverter.GetBytes((ushort)4))
            .Concat(BitConverter.GetBytes((ushort)5))
            .ToArray();

        var original = new TgaDateTime(bytes);
        var roundTripped = new TgaDateTime(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
