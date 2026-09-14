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
    public void ExtensionSize_Property_IsReadOnly()
    {
        PropertyInfo property = typeof(TgaExtensionArea).GetProperty(nameof(TgaExtensionArea.ExtensionSize))!;

        Assert.IsNull(property.SetMethod);
    }

    [TestMethod]
    public void ExtensionSize_WithOtherData_IsMinSizePlusOtherDataLength()
    {
        var extArea = new TgaExtensionArea { OtherDataInExtensionArea = new byte[7] };

        Assert.AreEqual((ushort)(TgaExtensionArea.MinSize + 7), extArea.ExtensionSize);
    }

    [TestMethod]
    public void ToBytes_WithOtherData_WritesComputedExtensionSizeAndRoundTrips()
    {
        var original = new TgaExtensionArea { OtherDataInExtensionArea = [1, 2, 3] };

        byte[] bytes = original.ToBytes();
        var parsed = new TgaExtensionArea(bytes);

        Assert.AreEqual(TgaExtensionArea.MinSize + 3, bytes.Length);
        Assert.AreEqual((ushort)(TgaExtensionArea.MinSize + 3), TgaBinary.ReadUInt16(bytes, 0));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, parsed.OtherDataInExtensionArea);
        Assert.AreEqual(original, parsed);
    }

    [TestMethod]
    public void Ctor_StaleSizeFieldWithTrailingBytes_KeepsTrailingBytes()
    {
        byte[] bytes = new TgaExtensionArea { OtherDataInExtensionArea = [9, 9] }.ToBytes();
        TgaBinary.WriteUInt16(bytes, 0, TgaExtensionArea.MinSize);

        var parsed = new TgaExtensionArea(bytes);

        CollectionAssert.AreEqual(new byte[] { 9, 9 }, parsed.OtherDataInExtensionArea);
        Assert.AreEqual((ushort)(TgaExtensionArea.MinSize + 2), parsed.ExtensionSize);
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

    [TestMethod]
    public void AuthorNameSetter_Null_ThrowsArgumentNullException()
    {
        var extArea = new TgaExtensionArea();

        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.AuthorName = null!);
    }

    [TestMethod]
    public void ReferenceSetters_Null_ThrowArgumentNullException()
    {
        var extArea = new TgaExtensionArea();

        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.AuthorComments = null!);
        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.DateTimeStamp = null!);
        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.JobNameOrId = null!);
        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.JobTime = null!);
        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.SoftwareId = null!);
        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.SoftwareVersion = null!);
        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.KeyColor = null!);
        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.PixelAspectRatio = null!);
        Assert.ThrowsExactly<ArgumentNullException>(() => extArea.GammaValue = null!);
    }

    [TestMethod]
    public void ReferenceSetters_NonNull_AreStored()
    {
        var extArea = new TgaExtensionArea();
        var time = new TgaTime(1, 2, 3);

        extArea.JobTime = time;

        Assert.AreSame(time, extArea.JobTime);
    }

    [TestMethod]
    public void OtherDataInExtensionAreaSetter_MaxOtherDataLength_IsAcceptedAndExtensionSizeIsUShortMax()
    {
        var extArea = new TgaExtensionArea();

        extArea.OtherDataInExtensionArea = new byte[TgaExtensionArea.MaxOtherDataLength];

        Assert.AreEqual(ushort.MaxValue, extArea.ExtensionSize);
    }

    [TestMethod]
    public void OtherDataInExtensionAreaSetter_LongerThanMax_ThrowsArgumentOutOfRangeException()
    {
        var extArea = new TgaExtensionArea();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => extArea.OtherDataInExtensionArea = new byte[TgaExtensionArea.MaxOtherDataLength + 1]);
    }

    [TestMethod]
    public void OtherDataInExtensionAreaSetter_Null_IsAccepted()
    {
        var extArea = new TgaExtensionArea { OtherDataInExtensionArea = [1] };

        extArea.OtherDataInExtensionArea = null;

        Assert.IsNull(extArea.OtherDataInExtensionArea);
        Assert.AreEqual((ushort)TgaExtensionArea.MinSize, extArea.ExtensionSize);
    }

    [TestMethod]
    public void Ctor_BytesLongerThanUShortMax_ThrowsArgumentOutOfRangeExceptionNamingBytes()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaExtensionArea(new byte[ushort.MaxValue + 1]));

        Assert.AreEqual("bytes", ex.ParamName);
    }

    [TestMethod]
    public void ToBytes_PopulatedFields_WritesEachFieldAtSpecOffset()
    {
        var extArea = new TgaExtensionArea
        {
            AuthorName = new TgaString("author", TgaExtensionArea.NameFieldLength, true),
            AuthorComments = new TgaComment("c1", "c2", "c3", "c4"),
            DateTimeStamp = new TgaDateTime(5, 6, 2024, 7, 8, 9),
            JobNameOrId = new TgaString("job", TgaExtensionArea.NameFieldLength, true),
            JobTime = new TgaTime(1, 2, 3),
            SoftwareId = new TgaString("soft", TgaExtensionArea.NameFieldLength, true),
            SoftwareVersion = new TgaSoftwareVersion(123, 'b'),
            KeyColor = new TgaColorKey(1, 2, 3, 4),
            PixelAspectRatio = new TgaFraction(4, 3),
            GammaValue = new TgaFraction(22, 10),
            ColorCorrectionTableOffset = 0x11223344,
            PostageStampOffset = 0x55667788,
            ScanLineOffset = 0x99AABBCC,
            AttributesType = TgaAttributeType.PreMultipliedAlpha,
        };

        byte[] bytes = extArea.ToBytes();

        Assert.AreEqual(TgaExtensionArea.MinSize, bytes.Length);
        Assert.AreEqual((ushort)TgaExtensionArea.MinSize, TgaBinary.ReadUInt16(bytes, 0));
        CollectionAssert.AreEqual(extArea.AuthorName.ToBytes(), bytes.AsSpan(2, 41).ToArray());
        CollectionAssert.AreEqual(extArea.AuthorComments.ToBytes(), bytes.AsSpan(43, TgaComment.Size).ToArray());
        CollectionAssert.AreEqual(extArea.DateTimeStamp.ToBytes(), bytes.AsSpan(367, TgaDateTime.Size).ToArray());
        CollectionAssert.AreEqual(extArea.JobNameOrId.ToBytes(), bytes.AsSpan(379, 41).ToArray());
        CollectionAssert.AreEqual(extArea.JobTime.ToBytes(), bytes.AsSpan(420, TgaTime.Size).ToArray());
        CollectionAssert.AreEqual(extArea.SoftwareId.ToBytes(), bytes.AsSpan(426, 41).ToArray());
        CollectionAssert.AreEqual(extArea.SoftwareVersion.ToBytes(), bytes.AsSpan(467, TgaSoftwareVersion.Size).ToArray());
        CollectionAssert.AreEqual(extArea.KeyColor.ToBytes(), bytes.AsSpan(470, TgaColorKey.Size).ToArray());
        CollectionAssert.AreEqual(extArea.PixelAspectRatio.ToBytes(), bytes.AsSpan(474, TgaFraction.Size).ToArray());
        CollectionAssert.AreEqual(extArea.GammaValue.ToBytes(), bytes.AsSpan(478, TgaFraction.Size).ToArray());
        Assert.AreEqual(0x11223344u, TgaBinary.ReadUInt32(bytes, 482));
        Assert.AreEqual(0x55667788u, TgaBinary.ReadUInt32(bytes, 486));
        Assert.AreEqual(0x99AABBCCu, TgaBinary.ReadUInt32(bytes, 490));
        Assert.AreEqual((byte)TgaAttributeType.PreMultipliedAlpha, bytes[494]);
        // Spot-check raw bytes so the sub-field ToBytes() cannot mask a shared offset error.
        Assert.AreEqual((byte)'a', bytes[2]);
        Assert.AreEqual((byte)'c', bytes[43]);
        Assert.AreEqual(5, bytes[367]);
        Assert.AreEqual((byte)'j', bytes[379]);
        Assert.AreEqual(1, bytes[420]);
        Assert.AreEqual((byte)'s', bytes[426]);
        Assert.AreEqual(123, bytes[467]);
        Assert.AreEqual(0x44, bytes[482]);
    }
}
