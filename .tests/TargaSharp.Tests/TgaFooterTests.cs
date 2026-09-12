using System.Text;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaFooter"/>.
/// </summary>
[TestClass]
public class TgaFooterTests
{
    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaFooter(null!));
    }

    [TestMethod]
    public void Ctor_WrongLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaFooter.Size - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaFooter(bytes));
    }

    [TestMethod]
    public void Ctor_CorrectLength_RoundTripsThroughToBytesAndEquals()
    {
        byte[] bytes = BitConverter.GetBytes(1u)
            .Concat(BitConverter.GetBytes(2u))
            .Concat(Encoding.ASCII.GetBytes(TgaString.XFileSignatuteConst))
            .Concat([(byte)'.', (byte)0])
            .ToArray();

        var original = new TgaFooter(bytes);
        var roundTripped = new TgaFooter(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }

    [TestMethod]
    public void IsFooterCorrect_DefaultFooter_ReturnsTrue()
    {
        var footer = new TgaFooter();

        Assert.IsTrue(footer.IsFooterCorrect);
    }

    [TestMethod]
    public void IsFooterCorrect_WrongSignature_ReturnsFalse()
    {
        var footer = new TgaFooter { Signature = TgaString.Empty };

        Assert.IsFalse(footer.IsFooterCorrect);
    }

    [TestMethod]
    public void IsFooterCorrect_ReservedCharacterNotDot_ReturnsFalse()
    {
        var footer = new TgaFooter { ReservedCharacter = new TgaString("X", 1) };

        Assert.IsFalse(footer.IsFooterCorrect);
    }

    [TestMethod]
    public void IsFooterCorrect_TerminatorNotZero_ReturnsFalse()
    {
        var footer = new TgaFooter { BinaryZeroStringTerminator = new TgaString("X", 1) };

        Assert.IsFalse(footer.IsFooterCorrect);
    }

    [TestMethod]
    public void IsFooterCorrect_SignatureMutatedOnOneInstance_NewInstanceStillCorrect()
    {
        var footer = new TgaFooter();

        footer.Signature.OriginalString = "corrupted";

        Assert.IsTrue(new TgaFooter().IsFooterCorrect);
    }
}
