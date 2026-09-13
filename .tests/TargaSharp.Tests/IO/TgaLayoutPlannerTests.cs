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
        // Entries are sorted by tag during planning, so [0] is tag 3 and [1] is tag 7.
        Assert.AreEqual(At("DeveloperArea.Entries[0].Data"), file.DeveloperArea![0].Offset);
        Assert.AreEqual(At("DeveloperArea.Entries[1].Data"), file.DeveloperArea[1].Offset);
        Assert.AreEqual((ushort)3, file.DeveloperArea[0].Tag);
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
    public void Plan_EmptyDeveloperEntries_AreDroppedAndDirectoryOffsetIsZero()
    {
        var file = new TgaFile(2, 2);
        file.ImageArea.ImageData = new byte[12];
        file.DeveloperArea = new TgaDeveloperArea();
        file.DeveloperArea.Entries.Add(new TgaDeveloperEntry(1, 0, []));

        TgaLayout layout = new TgaLayoutPlanner().Plan(file);

        Assert.AreEqual(0, file.DeveloperArea.Count);
        Assert.AreEqual(0u, file.Footer!.DeveloperDirectoryOffset);
        Assert.IsFalse(layout.Sections.Any(s => s.Name.StartsWith("DeveloperArea", StringComparison.Ordinal)));
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
}
