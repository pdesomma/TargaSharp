using TargaSharp.Validation;

namespace TargaSharp.Tests.Validation;

/// <summary>
/// Tests for <see cref="TgaValidator"/>, exercised through the <see cref="ITgaValidator"/> interface.
/// Every "invalid" test starts from an otherwise-valid baseline and mutates exactly one field, so
/// exactly one <see cref="TgaValidationError"/> (at the expected <see cref="TgaValidationError.Path"/>)
/// is expected - any dependent field the mutation would otherwise also break is fixed up alongside it.
/// </summary>
[TestClass]
public class TgaValidatorTests
{
    private static ITgaValidator Validator => new TgaValidator();

    /// <summary>
    /// Builds a minimal valid 4x4, 24bpp, uncompressed true-color <see cref="TgaFile"/> (no color map).
    /// </summary>
    private static TgaFile CreateValidBaseline() => new TgaFile(4, 4);

    /// <summary>
    /// Builds a minimal valid 4x4, 8bpp, uncompressed color-mapped <see cref="TgaFile"/> with a
    /// properly sized 2-entry, 24bpp (<see cref="TgaColorMapEntrySize.R8G8B8"/>) palette.
    /// </summary>
    private static TgaFile CreateValidColorMappedBaseline()
    {
        var file = new TgaFile(4, 4, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped);
        file.Header.ColorMapSpec.ColorMapLength = 2;
        file.ImageArea.ColorMapData = new byte[2 * TgaColorMapEntrySize.R8G8B8.BytesPerPixel()];
        return file;
    }

    #region Baselines

    [TestMethod]
    public void Validate_ValidTrueColorBaseline_ReturnsNoErrors()
    {
        var errors = Validator.Validate(CreateValidBaseline());

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_ValidColorMappedBaseline_ReturnsNoErrors()
    {
        var errors = Validator.Validate(CreateValidColorMappedBaseline());

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_NullFile_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Validator.Validate(null!));
    }

    #endregion

    #region PixelDepth

    [TestMethod]
    public void Validate_PixelDepthNotAStandardValue_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.Header.ImageSpec.PixelDepth = (TgaPixelDepth)5;
        // Keep ImageData consistent with the mutated depth's bytes-per-pixel so only the PixelDepth
        // rule (not the ImageData-length rule) is violated.
        file.ImageArea.ImageData = new byte[file.Width * file.Height * 1];

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ImageSpec.PixelDepth", errors[0].Path);
    }

    [TestMethod]
    public void Validate_PixelDepthOtherWithNoImageData_ReturnsNoError()
    {
        var file = new TgaFile();
        file.ToNewFormat();

        var errors = Validator.Validate(file);

        Assert.AreEqual(0, errors.Count);
    }

    #endregion

    #region ColorMapSpec

    [TestMethod]
    public void Validate_NoColorMapWithNonZeroFirstEntryIndex_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.Header.ColorMapSpec.FirstEntryIndex = 5;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ColorMapSpec.FirstEntryIndex", errors[0].Path);
    }

    [TestMethod]
    public void Validate_NoColorMapWithNonZeroColorMapLength_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.Header.ColorMapSpec.ColorMapLength = 5;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ColorMapSpec.ColorMapLength", errors[0].Path);
    }

    [TestMethod]
    public void Validate_NoColorMapWithNonZeroEntrySize_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.Header.ColorMapSpec.ColorMapEntrySize = TgaColorMapEntrySize.R8G8B8;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ColorMapSpec.ColorMapEntrySize", errors[0].Path);
    }

    [TestMethod]
    public void Validate_ColorMapWithInvalidEntrySize_ReturnsSingleError()
    {
        var file = CreateValidColorMappedBaseline();
        file.Header.ColorMapSpec.ColorMapEntrySize = (TgaColorMapEntrySize)5;
        // Keep ColorMapData consistent with the mutated entry size so only the EntrySize rule fires.
        file.ImageArea.ColorMapData = new byte[file.Header.ColorMapSpec.ColorMapLength * 1];

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ColorMapSpec.ColorMapEntrySize", errors[0].Path);
    }

    [TestMethod]
    public void Validate_ColorMapWithZeroLength_ReturnsSingleError()
    {
        var file = CreateValidColorMappedBaseline();
        file.Header.ColorMapSpec.ColorMapLength = 0;
        // Keep ColorMapData consistent (0 entries = 0 bytes) so only the ColorMapLength rule fires.
        file.ImageArea.ColorMapData = [];

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ColorMapSpec.ColorMapLength", errors[0].Path);
    }

    #endregion

    #region ImageType / ColorMapType cross-checks

    [TestMethod]
    public void Validate_ColorMappedImageTypeWithoutColorMap_ReturnsSingleError()
    {
        var file = new TgaFile(4, 4, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped);
        file.Header.ColorMapType = TgaColorMapType.NoColorMap;
        file.Header.ColorMapSpec.ColorMapEntrySize = TgaColorMapEntrySize.Other;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ColorMapType", errors[0].Path);
    }

    [TestMethod]
    public void Validate_TrueColorImageTypeWithColorMap_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.Header.ColorMapType = TgaColorMapType.ColorMap;
        file.Header.ColorMapSpec.ColorMapEntrySize = TgaColorMapEntrySize.R8G8B8;
        file.Header.ColorMapSpec.ColorMapLength = 2;
        file.ImageArea.ColorMapData = new byte[2 * TgaColorMapEntrySize.R8G8B8.BytesPerPixel()];

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ColorMapType", errors[0].Path);
    }

    [TestMethod]
    public void Validate_UnknownImageType_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.Header.ImageType = (TgaImageType)200;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ImageType", errors[0].Path);
    }

    #endregion

    #region AlphaChannelBits

    [TestMethod]
    public void Validate_32BppWithInvalidAlphaBits_ReturnsSingleError()
    {
        var file = new TgaFile(4, 4, TgaPixelDepth.Bpp32, TgaImageType.UncompressedTrueColor);
        file.Header.ImageSpec.ImageDescriptor.AlphaChannelBits = 3;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", errors[0].Path);
    }

    [TestMethod]
    public void Validate_24BppWithInvalidAlphaBits_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.Header.ImageSpec.ImageDescriptor.AlphaChannelBits = 4;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", errors[0].Path);
    }

    [TestMethod]
    public void Validate_16BppTrueColorWithInvalidAlphaBits_ReturnsSingleError()
    {
        var file = new TgaFile(4, 4, TgaPixelDepth.Bpp16, TgaImageType.UncompressedTrueColor);
        file.Header.ImageSpec.ImageDescriptor.AlphaChannelBits = 3;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", errors[0].Path);
    }

    [TestMethod]
    public void Validate_16BppBlackAndWhiteWithInvalidAlphaBits_ReturnsSingleError()
    {
        var file = new TgaFile(4, 4, TgaPixelDepth.Bpp16, TgaImageType.UncompressedGrayscale);
        file.Header.ImageSpec.ImageDescriptor.AlphaChannelBits = 3;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", errors[0].Path);
    }

    /// <summary>
    /// 16bpp black-and-white images may legitimately pack 8 bits intensity + 8 bits alpha (a
    /// different, equally valid layout from 16bpp true-color's 5-5-5-1), as seen in the real-world
    /// <c>monochrome16_top_left*.tga</c> fixtures - so this must NOT be flagged.
    /// </summary>
    [TestMethod]
    public void Validate_16BppBlackAndWhiteWithEightAlphaBits_ReturnsNoError()
    {
        var file = new TgaFile(4, 4, TgaPixelDepth.Bpp16, TgaImageType.UncompressedGrayscale);
        file.Header.ImageSpec.ImageDescriptor.AlphaChannelBits = 8;

        var errors = Validator.Validate(file);

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_8BppWithInvalidAlphaBits_ReturnsSingleError()
    {
        var file = CreateValidColorMappedBaseline();
        file.Header.ImageSpec.ImageDescriptor.AlphaChannelBits = 1;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("Header.ImageSpec.ImageDescriptor.AlphaChannelBits", errors[0].Path);
    }

    #endregion

    #region ColorMapData / ImageData / ImageId

    [TestMethod]
    public void Validate_ColorMapDataWrongLength_ReturnsSingleError()
    {
        var file = CreateValidColorMappedBaseline();
        file.ImageArea.ColorMapData = new byte[5];

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ImageArea.ColorMapData", errors[0].Path);
    }

    [TestMethod]
    public void Validate_NoColorMapWithNonEmptyColorMapData_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ImageArea.ColorMapData = new byte[3];

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ImageArea.ColorMapData", errors[0].Path);
    }

    [TestMethod]
    public void Validate_ImageDataWrongLength_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ImageArea.ImageData = new byte[10];

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ImageArea.ImageData", errors[0].Path);
    }

    [TestMethod]
    public void Validate_ImageSizeOverflowsIntWithEmptyImageData_ReturnsSingleError()
    {
        // 32768 x 32768 x 4 wraps to 0 in int arithmetic, so a file with no pixel data used to validate clean.
        var file = new TgaFile(1, 1, TgaPixelDepth.Bpp32, TgaImageType.UncompressedTrueColor, attrBits: 8);
        file.Width = 32768;
        file.Height = 32768;
        file.ImageArea.ImageData = [];

        var errors = new TgaValidator().Validate(file);

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("ImageArea.ImageData", errors[0].Path);
    }

    [TestMethod]
    public void Validate_NoImageDataWithLeftoverImageData_ReturnsSingleError()
    {
        var file = new TgaFile();
        file.ToNewFormat();
        file.ImageArea.ImageData = new byte[5];

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ImageArea.ImageData", errors[0].Path);
    }

    [TestMethod]
    public void Validate_ImageIdTooLong_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        string tooLong = new string('a', 256);
        file.ImageArea.ImageId = new TgaString(tooLong, tooLong.Length);

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ImageArea.ImageId", errors[0].Path);
    }

    [TestMethod]
    public void Validate_ImageIdAtMaxLength_ReturnsNoError()
    {
        var file = CreateValidBaseline();
        string maxLength = new string('a', 255);
        file.ImageArea.ImageId = new TgaString(maxLength, maxLength.Length);

        var errors = Validator.Validate(file);

        Assert.AreEqual(0, errors.Count);
    }

    #endregion

    #region DeveloperArea

    [TestMethod]
    public void Validate_DeveloperAreaTagReservedForTruevision_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.DeveloperArea = new TgaDeveloperArea([new TgaDeveloperEntry(40000, 0, [1])]);

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("DeveloperArea.Entries[0].Tag", errors[0].Path);
    }

    [TestMethod]
    public void Validate_DeveloperAreaDuplicateTags_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.DeveloperArea = new TgaDeveloperArea([new TgaDeveloperEntry(100, 0, [1]), new TgaDeveloperEntry(100, 0, [2])]);

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("DeveloperArea.Entries[1].Tag", errors[0].Path);
    }

    [TestMethod]
    public void Validate_DeveloperAreaWithNullEntry_ReturnsSingleErrorInsteadOfThrowing()
    {
        var file = new TgaFile(1, 1);
        file.DeveloperArea = new TgaDeveloperArea([new TgaDeveloperEntry(1, 0, [1]), null!]);

        var errors = new TgaValidator().Validate(file);

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("DeveloperArea.Entries[1]", errors[0].Path);
    }

    [TestMethod]
    public void Validate_DeveloperAreaWithDistinctDeveloperTags_ReturnsNoError()
    {
        var file = CreateValidBaseline();
        file.DeveloperArea = new TgaDeveloperArea([new TgaDeveloperEntry(1, 0, [1]), new TgaDeveloperEntry(2, 0, [2])]);

        var errors = Validator.Validate(file);

        Assert.AreEqual(0, errors.Count);
    }

    #endregion

    #region DateTimeStamp

    [TestMethod]
    public void Validate_DateTimeStampAllZero_ReturnsNoError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.DateTimeStamp = new TgaDateTime(0, 0, 0, 0, 0, 0);

        var errors = Validator.Validate(file);

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_DateTimeStampMonthOutOfRange_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.DateTimeStamp = new TgaDateTime(0, 15, 2024, 10, 30, 15);

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.DateTimeStamp.Month", errors[0].Path);
    }

    [TestMethod]
    public void Validate_DateTimeStampDayOutOfRange_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.DateTimeStamp = new TgaDateTime(6, 32, 2024, 10, 30, 15);

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.DateTimeStamp.Day", errors[0].Path);
    }

    [TestMethod]
    public void Validate_DateTimeStampHourOutOfRange_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.DateTimeStamp = new TgaDateTime(6, 15, 2024, 24, 30, 15);

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.DateTimeStamp.Hour", errors[0].Path);
    }

    [TestMethod]
    public void Validate_DateTimeStampMinuteOutOfRange_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.DateTimeStamp = new TgaDateTime(6, 15, 2024, 10, 60, 15);

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.DateTimeStamp.Minute", errors[0].Path);
    }

    [TestMethod]
    public void Validate_DateTimeStampSecondOutOfRange_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.DateTimeStamp = new TgaDateTime(6, 15, 2024, 10, 30, 60);

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.DateTimeStamp.Second", errors[0].Path);
    }

    #endregion

    #region JobTime

    [TestMethod]
    public void Validate_JobTimeMinutesOutOfRange_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.JobTime = new TgaTime { Hours = 5, Minutes = 60, Seconds = 10 };

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.JobTime.Minutes", errors[0].Path);
    }

    [TestMethod]
    public void Validate_JobTimeSecondsOutOfRange_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.JobTime = new TgaTime { Hours = 5, Minutes = 10, Seconds = 60 };

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.JobTime.Seconds", errors[0].Path);
    }

    #endregion

    #region GammaValue

    [TestMethod]
    public void Validate_GammaValueOutOfRange_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.GammaValue = new TgaFraction(150, 10); // 15.0

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.GammaValue", errors[0].Path);
    }

    [TestMethod]
    public void Validate_GammaValueInRange_ReturnsNoError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.GammaValue = new TgaFraction(15, 10); // 1.5

        var errors = Validator.Validate(file);

        Assert.AreEqual(0, errors.Count);
    }

    #endregion

    #region AttributesType

    [TestMethod]
    public void Validate_AttributesTypeUnassigned_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.AttributesType = (TgaAttributeType)200;

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.AttributesType", errors[0].Path);
    }

    #endregion

    #region ScanLineTable / ColorCorrectionTable / PostageStampImage

    [TestMethod]
    public void Validate_ScanLineTableLengthMismatch_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.ScanLineTable = new uint[3]; // Height is 4

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.ScanLineTable", errors[0].Path);
    }

    [TestMethod]
    public void Validate_ScanLineTableLengthMatchesHeight_ReturnsNoError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.ScanLineTable = new uint[4]; // Height is 4

        var errors = Validator.Validate(file);

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_ColorCorrectionTableWrongLength_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.ColorCorrectionTable = new ushort[10];

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.ColorCorrectionTable", errors[0].Path);
    }

    [TestMethod]
    public void Validate_PostageStampImageDataWrongLength_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.PostageStampImage = new TgaPostageStampImage(4, 4, new byte[10]); // expects 4*4*3 = 48

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.PostageStampImage.Data", errors[0].Path);
    }

    #endregion

    #region SoftwareVersion

    [TestMethod]
    public void Validate_SoftwareVersionLetterIsDigit_ReturnsSingleError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.SoftwareVersion.VersionLetter = '5';

        var errors = Validator.Validate(file);

        Assert.HasCount(1, errors);
        Assert.AreEqual("ExtensionArea.SoftwareVersion.VersionLetter", errors[0].Path);
    }

    [TestMethod]
    public void Validate_SoftwareVersionLetterIsALetter_ReturnsNoError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.SoftwareVersion.VersionLetter = 'b';

        var errors = Validator.Validate(file);

        Assert.AreEqual(0, errors.Count);
    }

    /// <summary>
    /// Some real-world TGA writers zero-fill the unused release-letter byte instead of writing a
    /// space, matching this library's own '\0' blank-fill convention (see
    /// <see cref="TgaString.DefaultBlankSpaceChar"/>) - as seen in the <c>Alpha Premult.tga</c> and
    /// <c>Alpha Straight.tga</c> fixtures - so it must NOT be flagged.
    /// </summary>
    [TestMethod]
    public void Validate_SoftwareVersionLetterIsNul_ReturnsNoError()
    {
        var file = CreateValidBaseline();
        file.ExtensionArea!.SoftwareVersion.VersionLetter = '\0';

        var errors = Validator.Validate(file);

        Assert.AreEqual(0, errors.Count);
    }

    #endregion
}
