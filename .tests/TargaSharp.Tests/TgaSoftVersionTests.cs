using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaSoftVersion"/>.
/// </summary>
[TestClass]
public class TgaSoftVersionTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaSoftVersion((byte[])null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaSoftVersion.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaSoftVersion(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = [117, 0, (byte)'b'];

        var original = new TgaSoftVersion(bytes);
        var roundTripped = new TgaSoftVersion(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
