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
    public void Ctor_ByteContract_Holds()
    {
        // Unlike the fixed-size field types, TgaString(byte[], useEnding: true) has no exact/upper
        // length bound - only the useEnding: true lower bound of 1 byte (room for the mandatory ending
        // character) - so this uses the minLength variant with a wrapper pinning useEnding to true.
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaString(bytes, true),
            x => x.ToBytes(),
            size: 3,
            sample: () => new TgaString(Encoding.ASCII.GetBytes("AB\0"), true),
            minLength: 1);
    }

    [TestMethod]
    public void Empty_CalledTwice_ReturnsDistinctReferences()
    {
        TgaString first = TgaString.Empty;
        TgaString second = TgaString.Empty;

        Assert.AreNotSame(first, second);
    }

    [TestMethod]
    public void Empty_MutatedOnOneReference_DoesNotAffectNextAccess()
    {
        TgaString first = TgaString.Empty;

        // Length must grow before OriginalString can, since TgaString now enforces
        // OriginalString.Length <= Length - (UseEndingChar ? 1 : 0) as a structural invariant.
        first.Length = "mutated".Length;
        first.OriginalString = "mutated";

        Assert.AreEqual(string.Empty, TgaString.Empty.OriginalString);
    }

    [TestMethod]
    public void Ctor_EmptyBytesWithUseEnding_ThrowsArgumentOutOfRangeExceptionNamingBytes()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaString(Array.Empty<byte>(), true));

        Assert.AreEqual("bytes", ex.ParamName);
    }

    [TestMethod]
    public void Ctor_Bytes_SpacePadded_RoundTripsBytesAndEquals()
    {
        var original = new TgaString("ab", 5, true, ' ');
        byte[] bytes = original.ToBytes();

        var parsed = new TgaString(bytes, true);

        Assert.AreEqual(original, parsed);
        CollectionAssert.AreEqual(bytes, parsed.ToBytes());
    }

    [TestMethod]
    public void Ctor_NonAsciiBytes_DecodesLeniently()
    {
        var parsed = new TgaString([0x41, 0xE9, 0x42]);

        Assert.AreEqual("A?B", parsed.OriginalString);
    }

    [TestMethod]
    public void Ctor_StringAndLength_ValidCombination_SetsProperties()
    {
        var str = new TgaString("ABC", 4, true);

        Assert.AreEqual("ABC", str.OriginalString);
        Assert.AreEqual(4, str.Length);
    }

    [TestMethod]
    public void Ctor_NullString_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaString((string)null!, 4));
    }

    [TestMethod]
    public void Ctor_NonAsciiString_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TgaString("café", 10));
    }

    [TestMethod]
    public void Ctor_StringLongerThanLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaString("ABCDE", 4));
    }

    [TestMethod]
    public void Ctor_StringLongerThanLengthMinusEndingChar_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaString("ABCD", 4, true));
    }

    [TestMethod]
    public void Ctor_LengthTooSmallForUseEndingChar_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaString(0, true));
    }

    [TestMethod]
    public void Ctor_BoolUseEndingCharTrue_ThrowsArgumentOutOfRangeException()
    {
        // Default Length is 0, which leaves no room for the mandatory ending character.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaString(true));
    }

    [TestMethod]
    public void OriginalStringSetter_NonAsciiValue_ThrowsArgumentException()
    {
        var str = new TgaString("ABC", 10);

        Assert.ThrowsExactly<ArgumentException>(() => str.OriginalString = "café");
    }

    [TestMethod]
    public void OriginalStringSetter_NullValue_ThrowsArgumentNullException()
    {
        var str = new TgaString("ABC", 10);

        Assert.ThrowsExactly<ArgumentNullException>(() => str.OriginalString = null!);
    }

    [TestMethod]
    public void LengthSetter_TooSmallForExistingOriginalString_ThrowsArgumentOutOfRangeException()
    {
        var str = new TgaString("ABCDE", 10);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => str.Length = 3);
    }

    [TestMethod]
    public void UseEndingCharSetter_TrueWithNoRoomForEndingChar_ThrowsArgumentOutOfRangeException()
    {
        var str = new TgaString("ABCDE", 5);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => str.UseEndingChar = true);
    }

    [TestMethod]
    public void ZeroTerminator_HasLengthOneAndProducesOneByte()
    {
        TgaString zeroTerminator = TgaString.ZeroTerminator;

        Assert.AreEqual(1, zeroTerminator.Length);
        Assert.AreEqual(1, zeroTerminator.ToBytes().Length);
        Assert.AreEqual((byte)0, zeroTerminator.ToBytes()[0]);
    }
}
