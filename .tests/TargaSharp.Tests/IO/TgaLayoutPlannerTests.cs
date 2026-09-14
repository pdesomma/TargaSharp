using TargaSharp.IO;
using TargaSharp.Validation;

namespace TargaSharp.Tests.IO;

/// <summary>
/// Tests for <see cref="TgaLayoutPlanner"/>: section order matches the spec, offsets are contiguous, and every
/// writer-owned offset field on the file agrees with where the plan put that section.
/// </summary>
[TestClass]
public class TgaLayoutPlannerTests
{
    /// <summary>
    /// Builds a 2x2 24bpp v2.0 file with every optional section populated.
    /// </summary>
    /// <returns>A fully populated file.</returns>
    private static TgaFile CreateFullFile()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        file.ImageArea.ImageId = new TgaString("id", 2);
        file.ExtensionArea!.ScanLineTable = [1, 2];
        file.ExtensionArea.PostageStampImage = new TgaPostageStampImage(1, 1, new byte[3]);
        file.ExtensionArea.ColorCorrectionTable = new ushort[TgaExtensionArea.ColorCorrectionTableLength];
        file.DeveloperArea = new TgaDeveloperArea();
        file.DeveloperArea.Entries.Add(new TgaDeveloperEntry(7, 0, [1, 2, 3]));
        file.DeveloperArea.Entries.Add(new TgaDeveloperEntry(3, 0, [9]));
        return file;
    }

    [TestMethod]
    public void Plan_FullFile_SectionsAreInSpecOrder()
    {
        TgaLayout layout = new TgaLayoutPlanner().Plan(CreateFullFile());

        string[] expected =
        [
            "Header", "ImageArea.ImageId", "ImageArea.ImageData",
            "DeveloperArea.Entries[0].Data", "DeveloperArea.Entries[1].Data", "DeveloperArea.Directory",
            "ExtensionArea", "ExtensionArea.ScanLineTable", "ExtensionArea.PostageStampImage", "ExtensionArea.ColorCorrectionTable",
            "Footer",
        ];
        CollectionAssert.AreEqual(expected, layout.Sections.Select(s => s.Name).ToArray());
    }

    [TestMethod]
    public void Plan_FullFile_OffsetsAreContiguousFromZero()
    {
        TgaLayout layout = new TgaLayoutPlanner().Plan(CreateFullFile());

        uint expected = 0;
        foreach (TgaSection section in layout.Sections)
        {
            Assert.AreEqual(expected, section.Offset, section.Name);
            expected += (uint)section.Bytes.Length;
        }
        Assert.AreEqual(expected, layout.TotalSize);
    }

    [TestMethod]
    public void Plan_FullFile_FileOffsetFieldsMatchSectionOffsets()
    {
        TgaFile file = CreateFullFile();
        TgaLayout layout = new TgaLayoutPlanner().Plan(file);
        uint At(string name) => layout.Sections.Single(s => s.Name == name).Offset;

        Assert.AreEqual(At("DeveloperArea.Directory"), file.Footer!.DeveloperDirectoryOffset);
        Assert.AreEqual(At("ExtensionArea"), file.Footer.ExtensionAreaOffset);
        Assert.AreEqual(At("ExtensionArea.ScanLineTable"), file.ExtensionArea!.ScanLineOffset);
        Assert.AreEqual(At("ExtensionArea.PostageStampImage"), file.ExtensionArea.PostageStampOffset);
        Assert.AreEqual(At("ExtensionArea.ColorCorrectionTable"), file.ExtensionArea.ColorCorrectionTableOffset);
        // Sections are laid out in tag order (3 then 7) while the caller's list keeps its own order (7 then 3).
        TgaDeveloperEntry tag3 = file.DeveloperArea!.Entries.Single(e => e.Tag == 3);
        TgaDeveloperEntry tag7 = file.DeveloperArea.Entries.Single(e => e.Tag == 7);
        Assert.AreEqual(At("DeveloperArea.Entries[0].Data"), tag3.Offset);
        Assert.AreEqual(At("DeveloperArea.Entries[1].Data"), tag7.Offset);
        Assert.AreEqual((ushort)7, file.DeveloperArea[0].Tag);
    }

    [TestMethod]
    public void Plan_FullFile_DerivedLengthsAreAssigned()
    {
        TgaFile file = CreateFullFile();
        file.ExtensionArea!.OtherDataInExtensionArea = new byte[5];

        new TgaLayoutPlanner().Plan(file);

        Assert.AreEqual((byte)2, file.Header.IdLength);
        Assert.AreEqual((ushort)(TgaExtensionArea.MinSize + 5), file.ExtensionArea.ExtensionSize);
    }

    [TestMethod]
    public void Plan_LegacyFileWithoutFooter_HasOnlyHeaderAndImageData()
    {
        var file = new TgaFile(2, 2, newFormat: false);
        file.ImageArea.ImageData = new byte[12];

        TgaLayout layout = new TgaLayoutPlanner().Plan(file);

        CollectionAssert.AreEqual(new[] { "Header", "ImageArea.ImageData" }, layout.Sections.Select(s => s.Name).ToArray());
        Assert.AreEqual(30u, layout.TotalSize);
    }

    [TestMethod]
    public void Plan_NoOptionalSections_OffsetFieldsAreZero()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];

        new TgaLayoutPlanner().Plan(file);

        Assert.AreEqual(0u, file.Footer!.DeveloperDirectoryOffset);
        Assert.AreEqual(0u, file.ExtensionArea!.ScanLineOffset);
        Assert.AreEqual(0u, file.ExtensionArea.PostageStampOffset);
        Assert.AreEqual(0u, file.ExtensionArea.ColorCorrectionTableOffset);
    }

    [TestMethod]
    public void Plan_RleImage_ImageDataSectionIsEncoded()
    {
        var file = new TgaFile(16, 1, TgaPixelDepth.Bpp8, TgaImageType.RleGrayscale);
        file.ImageArea.ImageData = Enumerable.Repeat((byte)5, 16).ToArray();

        TgaLayout layout = new TgaLayoutPlanner().Plan(file);

        TgaSection data = layout.Sections.Single(s => s.Name == "ImageArea.ImageData");
        Assert.IsTrue(data.Bytes.Length < 16, "expected RLE to shrink a 16-byte run");
        Assert.IsTrue((data.Bytes[0] & 0x80) != 0, "first packet should be a run-length packet");
        CollectionAssert.AreEqual(file.ImageArea.ImageData, RleCodec.Decode(new BinaryReader(new MemoryStream(data.Bytes)), 1, 16));
    }

    [TestMethod]
    public void Plan_DuplicateDeveloperTags_ThrowsTgaValidationException()
    {
        TgaFile file = CreateFullFile();
        file.DeveloperArea!.Entries.Add(new TgaDeveloperEntry(7, 0, [1]));

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => new TgaLayoutPlanner().Plan(file));

        StringAssert.Contains(ex.Errors[0].Path, "DeveloperArea");
    }

    [TestMethod]
    public void Plan_OtherDataTooLargeForExtensionSize_ThrowsNamingTheField()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        file.ExtensionArea!.OtherDataInExtensionArea = new byte[ushort.MaxValue - TgaExtensionArea.MinSize + 1];

        var e = Assert.ThrowsExactly<TgaValidationException>(() => new TgaLayoutPlanner().Plan(file));

        Assert.AreEqual("ExtensionArea.OtherDataInExtensionArea", e.Errors[0].Path);
    }

    [TestMethod]
    public void Plan_EmptyDeveloperEntry_KeepsItsDirectorySlotWithZeroSize()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        file.DeveloperArea = new TgaDeveloperArea();
        file.DeveloperArea.Entries.Add(new TgaDeveloperEntry(1, 0, []));

        TgaLayout layout = new TgaLayoutPlanner().Plan(file);

        Assert.AreEqual(1, file.DeveloperArea.Count, "planning must not delete the caller's entry");
        TgaSection directory = layout.Sections.Single(s => s.Name == "DeveloperArea.Directory");
        Assert.AreEqual(directory.Offset, file.Footer!.DeveloperDirectoryOffset);
        Assert.AreEqual(1, TgaBinary.ReadUInt16(directory.Bytes, 0));
        Assert.AreEqual(0u, TgaBinary.ReadUInt32(directory.Bytes, 8), "field size");
    }

    [TestMethod]
    public void Plan_EmptyDeveloperEntry_SurvivesWriteThenRead()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        file.DeveloperArea = new TgaDeveloperArea([new TgaDeveloperEntry(1, 0, []), new TgaDeveloperEntry(2, 0, [9])]);

        TgaFile reloaded = new TgaReader().Read(file.ToBytes());

        Assert.AreEqual(2, reloaded.DeveloperArea!.Count);
        Assert.AreEqual(0, reloaded.DeveloperArea.Entries[0].FieldSize);
        CollectionAssert.AreEqual(new byte[] { 9 }, reloaded.DeveloperArea.Entries[1].Data);
    }

    [TestMethod]
    public void Plan_UnsortedDeveloperEntriesWithAnEmptyOne_WritesTagOrderedDirectoryWithoutMutatingCallerList()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        file.DeveloperArea = new TgaDeveloperArea([
            new TgaDeveloperEntry(7, 0, [1]),
            new TgaDeveloperEntry(3, 0, []),
            new TgaDeveloperEntry(5, 0, [2]),
        ]);

        TgaLayout layout = new TgaLayoutPlanner().Plan(file);

        // Caller's list: same three entries, same order (ToBytes() used to leave it as [5, 7]).
        CollectionAssert.AreEqual(new ushort[] { 7, 3, 5 }, file.DeveloperArea.Entries.Select(e => e.Tag).ToArray());

        // File: all three entries, tag-ordered, and the directory lists them in that order.
        TgaSection directory = layout.Sections.Single(s => s.Name == "DeveloperArea.Directory");
        Assert.AreEqual(3, TgaBinary.ReadUInt16(directory.Bytes, 0));
        Assert.AreEqual(3, TgaBinary.ReadUInt16(directory.Bytes, 2));
        Assert.AreEqual(5, TgaBinary.ReadUInt16(directory.Bytes, 2 + TgaDeveloperEntry.Size));
        Assert.AreEqual(7, TgaBinary.ReadUInt16(directory.Bytes, 2 + 2 * TgaDeveloperEntry.Size));
        Assert.AreEqual(file.DeveloperArea.Entries[2].Offset, TgaBinary.ReadUInt32(directory.Bytes, 4 + TgaDeveloperEntry.Size));
        Assert.AreEqual(file.DeveloperArea.Entries[0].Offset, TgaBinary.ReadUInt32(directory.Bytes, 4 + 2 * TgaDeveloperEntry.Size));
    }

    [TestMethod]
    public void Plan_PaddedImageId_HonorsFieldLength()
    {
        // A padded ID read from disk (Length 6, text "abc") used to be shortened to 3 on write.
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageId = new TgaString("abc", 6, true);

        new TgaLayoutPlanner().Plan(file);

        Assert.AreEqual((byte)6, file.Header.IdLength);
        Assert.AreEqual(6, file.ImageArea.ImageId.Length);
    }

    [TestMethod]
    public void Plan_ImageIdLongerThan255Bytes_ThrowsTgaValidationException()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageId = new TgaString("x", 256);

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => new TgaLayoutPlanner().Plan(file));
        Assert.AreEqual("ImageArea.ImageId", ex.Errors[0].Path);
    }

    [TestMethod]
    public void Plan_ColorMapDataLengthMismatch_ThrowsTgaValidationException()
    {
        var file = new TgaFile(2, 2, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped);
        file.Header.ColorMapSpec.ColorMapLength = 2;
        file.ImageArea.ColorMapData = new byte[5];

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => new TgaLayoutPlanner().Plan(file));
        Assert.AreEqual("ImageArea.ColorMapData", ex.Errors[0].Path);
    }

    [TestMethod]
    public void Plan_ZeroWidth_ThrowsTgaValidationException()
    {
        var file = new TgaFile(2, 2);
        file.Width = 0;
        file.ImageArea.ImageData = [];

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => new TgaLayoutPlanner().Plan(file));
        Assert.AreEqual("ImageArea.ImageData", ex.Errors[0].Path);
    }

    [TestMethod]
    public void Plan_HeaderDeclaringImageOverIntMaxWithEmptyData_ThrowsTgaValidationException()
    {
        // 32768 x 32768 x 4 wrapped to exactly 0 in int, so an empty ImageData used to pass this check.
        var file = new TgaFile(2, 2, TgaPixelDepth.Bpp32);
        file.Width = 32768;
        file.Height = 32768;
        file.ImageArea.ImageData = [];

        Assert.ThrowsExactly<TgaValidationException>(() => new TgaLayoutPlanner().Plan(file));
    }

    [TestMethod]
    [DataRow("ScanLineTable")]
    [DataRow("ColorCorrectionTable")]
    [DataRow("PostageStampImage.Data")]
    public void Plan_ExtensionTableLengthMismatch_ThrowsTgaValidationException(string table)
    {
        var file = new TgaFile(2, 2);
        switch (table)
        {
            case "ScanLineTable": file.ExtensionArea!.ScanLineTable = [1]; break;
            case "ColorCorrectionTable": file.ExtensionArea!.ColorCorrectionTable = [1]; break;
            default: file.ExtensionArea!.PostageStampImage = new TgaPostageStampImage(1, 1, new byte[1]); break;
        }

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => new TgaLayoutPlanner().Plan(file));
        Assert.AreEqual("ExtensionArea." + table, ex.Errors[0].Path);
    }

    [TestMethod]
    public void Plan_ColorMappedFile_ColorMapSectionFollowsImageId()
    {
        var file = new TgaFile(2, 2, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped);
        file.Header.ColorMapSpec.ColorMapLength = 2;
        file.ImageArea.ColorMapData = new byte[6];
        file.ImageArea.ImageId = new TgaString("id", 2);

        TgaLayout layout = new TgaLayoutPlanner().Plan(file);

        CollectionAssert.AreEqual(
            new[] { "Header", "ImageArea.ImageId", "ImageArea.ColorMapData", "ImageArea.ImageData", "ExtensionArea", "Footer" },
            layout.Sections.Select(s => s.Name).ToArray());
    }

    [TestMethod]
    public void Plan_NullFile_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaLayoutPlanner().Plan(null!));
    }

    [TestMethod]
    public void Plan_TotalSize_MatchesWriterOutputLength()
    {
        TgaFile file = CreateFullFile();

        TgaLayout layout = new TgaLayoutPlanner().Plan(file);
        byte[] written = new TgaWriter().Write(file);

        Assert.AreEqual(layout.TotalSize, (uint)written.Length);
    }

    [TestMethod]
    public void Plan_UnsetDateTimeStamp_IsStampedWithCurrentTime()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        file.ExtensionArea!.DateTimeStamp = new TgaDateTime();
        DateTime before = DateTime.UtcNow.AddSeconds(-1);

        new TgaLayoutPlanner().Plan(file);

        Assert.IsFalse(file.ExtensionArea.DateTimeStamp.IsUnset);
        Assert.IsTrue(file.ExtensionArea.DateTimeStamp.ToDateTime()!.Value >= new DateTime(before.Year, before.Month, before.Day, before.Hour, before.Minute, before.Second));
    }

    [TestMethod]
    public void Plan_SetDateTimeStamp_IsPreserved()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        var stamp = new TgaDateTime(new DateTime(1999, 12, 31, 23, 59, 58));
        file.ExtensionArea!.DateTimeStamp = stamp;

        new TgaLayoutPlanner().Plan(file);

        Assert.AreEqual(stamp, file.ExtensionArea.DateTimeStamp);
    }

    [TestMethod]
    public void Plan_SetDateTimeStamp_SurvivesWriteAndReload()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        var stamp = new TgaDateTime(new DateTime(2001, 2, 3, 4, 5, 6));
        file.ExtensionArea!.DateTimeStamp = stamp;

        TgaFile reloaded = new TgaReader().Read(new TgaWriter().Write(file));

        Assert.AreEqual(stamp, reloaded.ExtensionArea!.DateTimeStamp);
    }

    [TestMethod]
    public void Plan_CalledTwice_DateTimeStampIsStable()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        file.ExtensionArea!.DateTimeStamp = new TgaDateTime();
        var planner = new TgaLayoutPlanner();

        planner.Plan(file);
        TgaDateTime first = file.ExtensionArea.DateTimeStamp.Copy();
        planner.Plan(file);

        Assert.AreEqual(first, file.ExtensionArea.DateTimeStamp);
    }
}
