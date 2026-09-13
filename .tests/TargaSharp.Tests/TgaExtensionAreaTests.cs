using System.Reflection;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaExtensionArea"/>.
/// </summary>
[TestClass]
public class TgaExtensionAreaTests
{
    [TestMethod]
    public void ExtensionSize_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaExtensionArea).GetProperty(nameof(TgaExtensionArea.ExtensionSize))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void ColorCorrectionTableOffset_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaExtensionArea).GetProperty(nameof(TgaExtensionArea.ColorCorrectionTableOffset))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void PostageStampOffset_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaExtensionArea).GetProperty(nameof(TgaExtensionArea.PostageStampOffset))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void ScanLineOffset_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaExtensionArea).GetProperty(nameof(TgaExtensionArea.ScanLineOffset))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void DefaultCtor_NewInstance_ExtensionSizeEqualsMinSize()
    {
        var extArea = new TgaExtensionArea();

        Assert.AreEqual((ushort)495, extArea.ExtensionSize);
    }

    [TestMethod]
    public void DefaultCtor_NewInstance_AllReferenceFieldsAreNonNull()
    {
        var extArea = new TgaExtensionArea();

        Assert.IsNotNull(extArea.AuthorName);
        Assert.IsNotNull(extArea.JobNameOrId);
        Assert.IsNotNull(extArea.SoftwareId);
        Assert.IsNotNull(extArea.AuthorComments);
        Assert.IsNotNull(extArea.DateTimeStamp);
        Assert.IsNotNull(extArea.JobTime);
        Assert.IsNotNull(extArea.SoftwareVersion);
        Assert.IsNotNull(extArea.KeyColor);
        Assert.IsNotNull(extArea.PixelAspectRatio);
        Assert.IsNotNull(extArea.GammaValue);
    }

    [TestMethod]
    public void GetHashCode_DefaultInstance_DoesNotThrow()
    {
        var extArea = new TgaExtensionArea();

        _ = extArea.GetHashCode();
    }

    [TestMethod]
    public void ToBytes_DefaultInstance_LengthEqualsMinSize()
    {
        var extArea = new TgaExtensionArea();

        byte[] bytes = extArea.ToBytes();

        Assert.AreEqual(TgaExtensionArea.MinSize, bytes.Length);
    }

    [TestMethod]
    public void ToBytes_DefaultInstance_FirstTwoBytesAreLittleEndian495()
    {
        var extArea = new TgaExtensionArea();

        byte[] bytes = extArea.ToBytes();

        Assert.AreEqual(0xEF, bytes[0]);
        Assert.AreEqual(0x01, bytes[1]);
    }

    [TestMethod]
    public void Ctor_NullBytes_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaExtensionArea(null!));
    }

    [TestMethod]
    public void Ctor_BytesShorterThanMinSize_ThrowsArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[TgaExtensionArea.MinSize - 1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaExtensionArea(bytes));
    }

    [TestMethod]
    public void Ctor_FromBytesOfDefaultInstance_RoundTripsToEqualInstance()
    {
        var original = new TgaExtensionArea();

        var roundTripped = new TgaExtensionArea(original.ToBytes());

        Assert.IsTrue(roundTripped.Equals(original));
    }

    [TestMethod]
    public void PixelAspectRatio_MutatedOnOneInstance_DoesNotAffectNewInstance()
    {
        var ext1 = new TgaExtensionArea();

        ext1.PixelAspectRatio.Numerator = 42;

        Assert.AreEqual((ushort)0, new TgaExtensionArea().PixelAspectRatio.Numerator);
    }

    [TestMethod]
    public void GammaValue_MutatedOnOneInstance_DoesNotAffectNewInstance()
    {
        var ext1 = new TgaExtensionArea();

        ext1.GammaValue.Numerator = 42;

        Assert.AreEqual((ushort)0, new TgaExtensionArea().GammaValue.Numerator);
    }
}
