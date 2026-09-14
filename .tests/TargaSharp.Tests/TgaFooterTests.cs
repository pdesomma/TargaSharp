using System.Reflection;
using System.Text;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaFooter"/>.
/// </summary>
[TestClass]
public class TgaFooterTests
{
    /// <summary>
    /// Builds a valid 26-byte TGA v2.0 footer with the given offsets.
    /// </summary>
    /// <param name="extOff">Extension Area Offset to embed (bytes 0-3).</param>
    /// <param name="devDirOff">Developer Directory Offset to embed (bytes 4-7).</param>
    /// <returns>A byte[26] valid TGA v2.0 footer.</returns>
    private static byte[] BuildValidFooterBytes(uint extOff = 1u, uint devDirOff = 2u) =>
        BitConverter.GetBytes(extOff)
            .Concat(BitConverter.GetBytes(devDirOff))
            .Concat(Encoding.ASCII.GetBytes(TgaString.XFileSignatureText))
            .Concat([(byte)'.', (byte)0])
            .ToArray();

    [TestMethod]
    public void TryParse_ValidFooter_ReturnsTrueAndPopulatesOffsets()
    {
        byte[] bytes = BuildValidFooterBytes(1u, 2u);

        bool result = TgaFooter.TryParse(bytes, out TgaFooter? footer);

        Assert.IsTrue(result);
        Assert.IsNotNull(footer);
        Assert.AreEqual(1u, footer!.ExtensionAreaOffset);
        Assert.AreEqual(2u, footer.DeveloperDirectoryOffset);
    }

    [TestMethod]
    public void TryParse_NullBytes_ReturnsFalse()
    {
        bool result = TgaFooter.TryParse(null, out TgaFooter? footer);

        Assert.IsFalse(result);
        Assert.IsNull(footer);
    }

    [TestMethod]
    public void TryParse_WrongLength_ReturnsFalse()
    {
        byte[] bytes = new byte[TgaFooter.Size - 1];

        bool result = TgaFooter.TryParse(bytes, out TgaFooter? footer);

        Assert.IsFalse(result);
        Assert.IsNull(footer);
    }

    [TestMethod]
    public void TryParse_WrongSignature_ReturnsFalse()
    {
        byte[] bytes = BuildValidFooterBytes();
        bytes[8] = (byte)'X'; // corrupt first signature byte

        bool result = TgaFooter.TryParse(bytes, out TgaFooter? footer);

        Assert.IsFalse(result);
        Assert.IsNull(footer);
    }

    [TestMethod]
    public void TryParse_ReservedCharacterNotDot_ReturnsFalse()
    {
        byte[] bytes = BuildValidFooterBytes();
        bytes[24] = (byte)'X';

        bool result = TgaFooter.TryParse(bytes, out TgaFooter? footer);

        Assert.IsFalse(result);
        Assert.IsNull(footer);
    }

    [TestMethod]
    public void TryParse_TerminatorNotZero_ReturnsFalse()
    {
        byte[] bytes = BuildValidFooterBytes();
        bytes[25] = (byte)'X';

        bool result = TgaFooter.TryParse(bytes, out TgaFooter? footer);

        Assert.IsFalse(result);
        Assert.IsNull(footer);
    }

    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaFooter(null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeExceptionNamingBytes()
    {
        byte[] bytes = new byte[TgaFooter.Size - 1];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaFooter(bytes));

        Assert.AreEqual("bytes", ex.ParamName);
    }

    [TestMethod]
    public void Ctor_WrongSignature_ThrowsFormatException()
    {
        byte[] bytes = BuildValidFooterBytes();
        bytes[8] = (byte)'X';

        Assert.ThrowsExactly<FormatException>(() => new TgaFooter(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = BuildValidFooterBytes(1u, 2u);

        var original = new TgaFooter(bytes);
        var roundTripped = new TgaFooter(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }

    [TestMethod]
    public void ToBytes_DefaultFooter_RoundTripsThroughTryParse()
    {
        var footer = new TgaFooter();

        bool result = TgaFooter.TryParse(footer.ToBytes(), out TgaFooter? parsed);

        Assert.IsTrue(result);
        Assert.AreEqual(footer.ExtensionAreaOffset, parsed!.ExtensionAreaOffset);
        Assert.AreEqual(footer.DeveloperDirectoryOffset, parsed.DeveloperDirectoryOffset);
    }

    [TestMethod]
    public void ExtensionAreaOffset_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFooter).GetProperty(nameof(TgaFooter.ExtensionAreaOffset))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void DeveloperDirectoryOffset_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFooter).GetProperty(nameof(TgaFooter.DeveloperDirectoryOffset))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }
}
