using System.Linq;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaComment"/>.
/// </summary>
[TestClass]
public class TgaCommentTests
{
    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaComment(bytes),
            x => x.ToBytes(),
            TgaComment.Size,
            () => new TgaComment("line 1", "line 2"));
    }

    [TestMethod]
    public void Ctor_NoArgs_HasFourEmptyLines()
    {
        var comment = new TgaComment();

        Assert.AreEqual(4, comment.Lines.Count);
        Assert.IsTrue(comment.Lines.All(l => l == string.Empty));
    }

    [TestMethod]
    public void Ctor_UpToFourLines_SetsLinesInOrder()
    {
        var comment = new TgaComment("line 1", "line 2");

        Assert.AreEqual("line 1", comment.Lines[0]);
        Assert.AreEqual("line 2", comment.Lines[1]);
        Assert.AreEqual(string.Empty, comment.Lines[2]);
        Assert.AreEqual(string.Empty, comment.Lines[3]);
    }

    [TestMethod]
    public void Ctor_MoreThanFourLines_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaComment("1", "2", "3", "4", "5"));
    }

    [TestMethod]
    public void Ctor_LineLongerThan80Chars_ThrowsArgumentOutOfRangeException()
    {
        string tooLong = new string('a', 81);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaComment(tooLong));
    }

    [TestMethod]
    public void Ctor_NonAsciiLine_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TgaComment("café"));
    }

    [TestMethod]
    public void Ctor_NullLinesArray_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaComment((string[])null!));
    }

    [TestMethod]
    public void SetLine_ValidIndexAndText_UpdatesLine()
    {
        var comment = new TgaComment();

        comment.SetLine(2, "hello");

        Assert.AreEqual("hello", comment.Lines[2]);
    }

    [TestMethod]
    public void SetLine_IndexOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var comment = new TgaComment();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => comment.SetLine(4, "hello"));
    }

    [TestMethod]
    public void SetLine_NullText_ThrowsArgumentNullException()
    {
        var comment = new TgaComment();

        Assert.ThrowsExactly<ArgumentNullException>(() => comment.SetLine(0, null!));
    }

    [TestMethod]
    public void SetLine_NonAsciiText_ThrowsArgumentException()
    {
        var comment = new TgaComment();

        Assert.ThrowsExactly<ArgumentException>(() => comment.SetLine(0, "café"));
    }

    [TestMethod]
    public void Copy_MutatingCopyLines_DoesNotAffectOriginal()
    {
        var original = new TgaComment("original");
        var copy = original.Copy();

        copy.SetLine(0, "changed");

        Assert.AreEqual("original", original.Lines[0]);
        Assert.AreEqual("changed", copy.Lines[0]);
    }

    [TestMethod]
    public void ToBytes_LineShorterThan80Chars_PadsWithBlankSpaceCharAndNulTerminates()
    {
        var comment = new TgaComment("AB");

        byte[] bytes = comment.ToBytes();

        Assert.AreEqual((byte)'A', bytes[0]);
        Assert.AreEqual((byte)'B', bytes[1]);
        Assert.AreEqual((byte)0, bytes[2]);
        Assert.AreEqual((byte)0, bytes[80]);
    }
}
