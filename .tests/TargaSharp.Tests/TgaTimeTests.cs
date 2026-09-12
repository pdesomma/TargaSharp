using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaTime"/>.
/// </summary>
[TestClass]
public class TgaTimeTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaTime((byte[])null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaTime.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaTime(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = BitConverter.GetBytes((ushort)10)
            .Concat(BitConverter.GetBytes((ushort)20))
            .Concat(BitConverter.GetBytes((ushort)30))
            .ToArray();

        var original = new TgaTime(bytes);
        var roundTripped = new TgaTime(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
