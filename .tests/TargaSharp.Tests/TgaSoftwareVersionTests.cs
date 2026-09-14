using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaSoftwareVersion"/>.
/// </summary>
[TestClass]
public class TgaSoftwareVersionTests
{
    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaSoftwareVersion(bytes),
            x => x.ToBytes(),
            TgaSoftwareVersion.Size,
            () => new TgaSoftwareVersion(117, 'b'));
    }

    [TestMethod]
    [DataRow("117", (ushort)117, ' ')]
    [DataRow("117b", (ushort)117, 'b')]
    [DataRow("000", (ushort)0, ' ')]
    public void Ctor_String_ParsesDigitsAndOptionalLetter(string input, ushort expectedNumber, char expectedLetter)
    {
        var version = new TgaSoftwareVersion(input);

        Assert.AreEqual(expectedNumber, version.VersionNumber);
        Assert.AreEqual(expectedLetter, version.VersionLetter);
    }

    [TestMethod]
    [DataRow("1.0a")]
    [DataRow("abc")]
    [DataRow("-12")]
    [DataRow("+12")]
    public void Ctor_String_NonNumericPrefix_ThrowsFormatException(string input)
    {
        // Used to silently yield "000" with the letter dropped.
        Assert.ThrowsExactly<FormatException>(() => new TgaSoftwareVersion(input));
    }

    [TestMethod]
    [DataRow("12")]
    [DataRow("12345")]
    public void Ctor_String_WrongLength_ThrowsArgumentOutOfRangeException(string input)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaSoftwareVersion(input));
    }

    [TestMethod]
    public void Ctor_String_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaSoftwareVersion((string)null!));
    }

    [TestMethod]
    public void ToString_ThenCtorString_RoundTrips()
    {
        var original = new TgaSoftwareVersion(117, 'b');

        Assert.AreEqual(original, new TgaSoftwareVersion(original.ToString()));
    }
}
