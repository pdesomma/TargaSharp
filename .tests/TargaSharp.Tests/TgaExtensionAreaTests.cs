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
    public void GetHashCode_TwoDefaultInstances_ReturnEqualHashCodes()
    {
        var first = new TgaExtensionArea();
        var second = new TgaExtensionArea();

        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
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
    public void ToBytes_ThenCtor_PopulatedFieldsRoundTripAndCompareEqual()
    {
        var original = new TgaExtensionArea
        {
            AuthorName = new TgaString("author", TgaExtensionArea.NameFieldLength, true),
            AuthorComments = new TgaComment("line one", "line two"),
            DateTimeStamp = new TgaDateTime(5, 6, 2024, 7, 8, 9),
            JobNameOrId = new TgaString("job", TgaExtensionArea.NameFieldLength, true),
            JobTime = new TgaTime(1, 2, 3),
            SoftwareId = new TgaString("soft", TgaExtensionArea.NameFieldLength, true),
            SoftwareVersion = new TgaSoftwareVersion(123, 'b'),
            KeyColor = new TgaColorKey(1, 2, 3, 4),
            PixelAspectRatio = new TgaFraction(4, 3),
            GammaValue = new TgaFraction(22, 10),
            AttributesType = TgaAttributeType.PreMultipliedAlpha,
            OtherDataInExtensionArea = [7, 8, 9],
        };
        original.ExtensionSize = (ushort)(TgaExtensionArea.MinSize + 3);

        var parsed = new TgaExtensionArea(original.ToBytes());

        Assert.AreEqual(original, parsed);
        Assert.AreEqual("author", parsed.AuthorName.OriginalString);
        Assert.AreEqual("line two", parsed.AuthorComments.Lines[1]);
        CollectionAssert.AreEqual(new byte[] { 7, 8, 9 }, parsed.OtherDataInExtensionArea);
    }

    [TestMethod]
    public void Copy_MutatedTablesAndStamp_DoNotAffectOriginal()
    {
        var original = new TgaExtensionArea
        {
            ScanLineTable = [1, 2],
            ColorCorrectionTable = new ushort[TgaExtensionArea.ColorCorrectionTableLength],
            PostageStampImage = new TgaPostageStampImage(1, 1, [5]),
        };

        TgaExtensionArea copy = original.Copy();
        copy.ScanLineTable![0] = 9;
        copy.ColorCorrectionTable![0] = 9;
        copy.PostageStampImage!.Data[0] = 9;

        Assert.AreEqual(1u, original.ScanLineTable![0]);
        Assert.AreEqual((ushort)0, original.ColorCorrectionTable![0]);
        Assert.AreEqual((byte)5, original.PostageStampImage!.Data[0]);
        Assert.AreNotEqual(original, copy);
    }

    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        // OtherDataInExtensionArea is variable-length trailing data beyond the fixed 495-byte body,
        // so an over-long array is valid, not an error - hence the minLength variant.
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaExtensionArea(bytes),
            x => x.ToBytes(),
            size: TgaExtensionArea.MinSize,
            sample: () => new TgaExtensionArea(),
            minLength: TgaExtensionArea.MinSize);
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
