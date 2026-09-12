using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaComment"/>.
/// </summary>
[TestClass]
public class TgaCommentTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaComment((byte[])null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaComment.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaComment(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = new byte[TgaComment.Size];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)('A' + (i % 26));

        var original = new TgaComment(bytes);
        var roundTripped = new TgaComment(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
