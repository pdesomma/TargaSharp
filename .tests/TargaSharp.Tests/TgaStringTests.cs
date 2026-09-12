using System.Text;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaString"/>. Unlike the other fixed-size TGA field types,
/// <see cref="TgaString"/> is variable-length and has no <c>Size</c> constant, so its
/// "invalid length" case is an empty array combined with <c>useEnding: true</c>, which
/// leaves no room for the mandatory ending character.
/// </summary>
[TestClass]
public class TgaStringTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaString((byte[])null!));
    }

    [TestMethod]
    public void Ctor_EmptyBytesWithUseEndingChar_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaString(Array.Empty<byte>(), true));
    }

    [TestMethod]
    public void Ctor_ValidBytes_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("AB\0");

        var original = new TgaString(bytes, true);
        var roundTripped = new TgaString(original.ToBytes(), true);

        Assert.IsTrue(roundTripped.Equals(original));
    }
}
