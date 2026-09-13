using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaSoftwareVersion"/>.
/// </summary>
[TestClass]
public class TgaSoftwareVersionTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaSoftwareVersion((byte[])null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaSoftwareVersion.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaSoftwareVersion(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = [117, 0, (byte)'b'];

        var original = new TgaSoftwareVersion(bytes);
        var roundTripped = new TgaSoftwareVersion(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
