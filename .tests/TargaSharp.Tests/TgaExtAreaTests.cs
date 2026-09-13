using System.Reflection;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaExtArea"/>.
/// </summary>
[TestClass]
public class TgaExtAreaTests
{
    [TestMethod]
    public void ExtensionSize_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaExtArea).GetProperty(nameof(TgaExtArea.ExtensionSize))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void ColorCorrectionTableOffset_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaExtArea).GetProperty(nameof(TgaExtArea.ColorCorrectionTableOffset))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void PostageStampOffset_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaExtArea).GetProperty(nameof(TgaExtArea.PostageStampOffset))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void ScanLineOffset_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaExtArea).GetProperty(nameof(TgaExtArea.ScanLineOffset))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void DefaultCtor_NewInstance_ExtensionSizeEqualsMinSize()
    {
        var extArea = new TgaExtArea();

        Assert.AreEqual((ushort)495, extArea.ExtensionSize);
    }

    [TestMethod]
    public void DefaultCtor_NewInstance_AllReferenceFieldsAreNonNull()
    {
        var extArea = new TgaExtArea();

        Assert.IsNotNull(extArea.AuthorName);
        Assert.IsNotNull(extArea.JobNameOrID);
        Assert.IsNotNull(extArea.SoftwareID);
        Assert.IsNotNull(extArea.AuthorComments);
        Assert.IsNotNull(extArea.DateTimeStamp);
        Assert.IsNotNull(extArea.JobTime);
        Assert.IsNotNull(extArea.SoftVersion);
        Assert.IsNotNull(extArea.KeyColor);
        Assert.IsNotNull(extArea.PixelAspectRatio);
        Assert.IsNotNull(extArea.GammaValue);
    }

    [TestMethod]
    public void GetHashCode_DefaultInstance_DoesNotThrow()
    {
        var extArea = new TgaExtArea();

        _ = extArea.GetHashCode();
    }

    [TestMethod]
    public void ToBytes_DefaultInstance_LengthEqualsMinSize()
    {
        var extArea = new TgaExtArea();

        byte[] bytes = extArea.ToBytes();

        Assert.AreEqual(TgaExtArea.MinSize, bytes.Length);
    }

    [TestMethod]
    public void ToBytes_DefaultInstance_FirstTwoBytesAreLittleEndian495()
    {
        var extArea = new TgaExtArea();

        byte[] bytes = extArea.ToBytes();

        Assert.AreEqual(0xEF, bytes[0]);
        Assert.AreEqual(0x01, bytes[1]);
    }

    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaExtArea(null!));
    }

    [TestMethod]
    public void Ctor_BytesShorterThanMinSize_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaExtArea.MinSize - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaExtArea(bytes));
    }

    [TestMethod]
    public void Ctor_FromBytesOfDefaultInstance_RoundTripsToEqualInstance()
    {
        var original = new TgaExtArea();

        var roundTripped = new TgaExtArea(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }

    [TestMethod]
    public void PixelAspectRatio_MutatedOnOneInstance_DoesNotAffectNewInstance()
    {
        var ext1 = new TgaExtArea();

        ext1.PixelAspectRatio.Numerator = 42;

        Assert.AreEqual((ushort)0, new TgaExtArea().PixelAspectRatio.Numerator);
    }

    [TestMethod]
    public void GammaValue_MutatedOnOneInstance_DoesNotAffectNewInstance()
    {
        var ext1 = new TgaExtArea();

        ext1.GammaValue.Numerator = 42;

        Assert.AreEqual((ushort)0, new TgaExtArea().GammaValue.Numerator);
    }
}
