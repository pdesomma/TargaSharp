using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaImageArea"/>.
/// </summary>
[TestClass]
public class TgaImageAreaTests
{
    [TestMethod]
    public void Equals_SameContentDifferentArrays_ReturnsTrueWithEqualHashCodes()
    {
        var a = new TgaImageArea(new TgaString("id", 2), [1, 2, 3], [4, 5, 6]);
        var b = new TgaImageArea(new TgaString("id", 2), [1, 2, 3], [4, 5, 6]);

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Equals_DifferentImageData_ReturnsFalse()
    {
        var a = new TgaImageArea(null, null, [4, 5, 6]);
        var b = new TgaImageArea(null, null, [4, 5, 7]);

        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    public void Equals_NullVersusEmptyArray_ReturnsFalse()
    {
        var a = new TgaImageArea(null, null, null);
        var b = new TgaImageArea(null, null, []);

        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    public void Equals_Null_ReturnsFalse()
    {
        Assert.IsFalse(new TgaImageArea().Equals(null));
    }

    [TestMethod]
    public void Copy_MutatedCopy_DoesNotAffectOriginal()
    {
        var original = new TgaImageArea(new TgaString("id", 2), [1, 2, 3], [4, 5, 6]);

        TgaImageArea copy = original.Copy();
        copy.ColorMapData![0] = 9;
        copy.ImageData![0] = 9;
        copy.ImageId!.OriginalString = "xx";

        Assert.AreEqual((byte)1, original.ColorMapData![0]);
        Assert.AreEqual((byte)4, original.ImageData![0]);
        Assert.AreEqual("id", original.ImageId!.OriginalString);
    }

    [TestMethod]
    public void Copy_NullMembers_StayNull()
    {
        TgaImageArea copy = new TgaImageArea().Copy();

        Assert.IsNull(copy.ImageId);
        Assert.IsNull(copy.ColorMapData);
        Assert.IsNull(copy.ImageData);
    }
}
