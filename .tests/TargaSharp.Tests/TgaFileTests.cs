using System.Reflection;
using TargaSharp;
using TargaSharp.Validation;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for the public surface of <see cref="TgaFile"/>. Only in-memory (<see cref="MemoryStream"/>)
/// I/O is exercised; no file-system access is performed.
/// </summary>
[TestClass]
public class TgaFileTests
{
    [TestMethod]
    public void Ctor_ColorMappedImage_ConstructsAndPopulatesColorMapSpec()
    {
        var tga = new TgaFile(4, 4, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped);

        Assert.IsNotNull(tga.Header.ColorMapSpec);
        Assert.AreEqual(TgaColorMapType.ColorMap, tga.Header.ColorMapType);
        // pixDepth (Bpp8) is the indexed *pixel* depth, not the palette *entry* depth: it must not
        // be reused to derive ColorMapEntrySize (ceil(8/8) = 1 is not a valid entry size). Regression
        // test for that bug: color-mapped images should default to 24-bit (R8G8B8) palette entries.
        Assert.AreEqual(TgaColorMapEntrySize.R8G8B8, tga.Header.ColorMapSpec.ColorMapEntrySize);
    }

    [TestMethod]
    public void Ctor_WidthAndHeight_PopulatesHeaderAndImageArea()
    {
        var tga = new TgaFile(2, 2);

        Assert.IsNotNull(tga.Header);
        Assert.IsNotNull(tga.ImageArea);
        Assert.AreEqual((ushort)2, tga.Width);
        Assert.AreEqual((ushort)2, tga.Height);
    }

    [TestMethod]
    public void Clone_DefaultFormatTga_DoesNotThrowAndHasNoExtensionAreaOrFooter()
    {
        var tga = new TgaFile(2, 2, newFormat: false);

        var clone = tga.Clone();

        Assert.IsNull(clone.ExtensionArea);
        Assert.IsNull(clone.Footer);
    }

    [TestMethod]
    public void Clone_MutatedClone_DoesNotAffectOriginal()
    {
        var tga = new TgaFile(2, 2, newFormat: false);

        var clone = tga.Clone();
        clone.Width = 9;

        Assert.AreEqual((ushort)2, tga.Width);
        Assert.AreEqual((ushort)9, clone.Width);
    }

    [TestMethod]
    public void UpdatePostageStampImage_ThenDeletePostageStampImage_ClearsPostageStampImage()
    {
        var tga = new TgaFile(4, 4, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor);

        tga.UpdatePostageStampImage();
        Assert.IsNotNull(tga.ExtensionArea?.PostageStampImage);

        tga.DeletePostageStampImage();
        Assert.IsNull(tga.ExtensionArea?.PostageStampImage);
    }

    [TestMethod]
    public void DeletePostageStampImage_NullExtensionArea_DoesNotThrow()
    {
        var tga = new TgaFile();

        tga.DeletePostageStampImage();
    }

    [TestMethod]
    public void Save_ThenLoadFromStream_RoundTripsToIdenticalBytes()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor);
        tga.ImageArea.ImageData = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

        using var firstStream = new MemoryStream();
        tga.Save(firstStream);
        byte[] firstBytes = firstStream.ToArray();

        var loaded = new TgaFile(new MemoryStream(firstBytes));

        using var secondStream = new MemoryStream();
        loaded.Save(secondStream);
        byte[] secondBytes = secondStream.ToArray();

        CollectionAssert.AreEqual(firstBytes, secondBytes);
    }

    [TestMethod]
    public void Save_ImageIdWithNChars_ReloadedHeaderIdLengthEqualsN()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor);
        tga.ImageArea.ImageData = new byte[2 * 2 * 3];
        tga.ImageArea.ImageId = new TgaString("Hello", 5);

        using var stream = new MemoryStream();
        tga.Save(stream);

        stream.Position = 0;
        var reloaded = new TgaFile(stream);

        Assert.AreEqual((byte)5, reloaded.Header.IdLength);
    }

    [TestMethod]
    public void Save_NullImageId_ReloadedHeaderIdLengthIsZero()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor);
        tga.ImageArea.ImageData = new byte[2 * 2 * 3];
        tga.ImageArea.ImageId = null;

        using var stream = new MemoryStream();
        tga.Save(stream);

        stream.Position = 0;
        var reloaded = new TgaFile(stream);

        Assert.AreEqual((byte)0, reloaded.Header.IdLength);
    }

    [TestMethod]
    public void Save_ExtensionAreaWithOtherData_ReloadedExtensionSizeEqualsMinSizePlusOtherDataLength()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor);
        tga.ImageArea.ImageData = new byte[2 * 2 * 3];
        tga.ExtensionArea!.OtherDataInExtensionArea = [1, 2, 3, 4];

        using var stream = new MemoryStream();
        tga.Save(stream);

        stream.Position = 0;
        var reloaded = new TgaFile(stream);

        Assert.AreEqual((ushort)(TgaExtensionArea.MinSize + 4), reloaded.ExtensionArea!.ExtensionSize);
    }

    [TestMethod]
    public void Header_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.Header))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void ImageArea_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.ImageArea))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void DeveloperArea_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.DeveloperArea))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void ExtensionArea_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.ExtensionArea))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void Footer_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.Footer))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void LoadFromStream_ExtensionAreaSizeBelowMinSize_LeavesExtensionAreaNullWithoutThrowing()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor);

        using var stream = new MemoryStream();
        tga.Save(stream);
        byte[] bytes = stream.ToArray();

        // Control: reloading the untouched bytes must find the (valid, 495-byte) v2.0 ext area.
        var control = new TgaFile(bytes);
        Assert.IsNotNull(control.ExtensionArea);

        // Per spec the Extension Area Size field must be 495 for a v2.0 ext area; a reader should
        // only parse what it understands. Patch the declared size (the first 2 bytes of the ext
        // area, pointed to by the footer's ExtensionAreaOffset) down to something below MinSize (495)
        // to simulate a file that either predates or otherwise doesn't declare a v2.0 ext area.
        uint extAreaOffset = BitConverter.ToUInt32(bytes, bytes.Length - TgaFooter.Size);
        byte[] patched = (byte[])bytes.Clone();
        BitConverter.GetBytes((ushort)100).CopyTo(patched, (int)extAreaOffset);

        var reloaded = new TgaFile(patched);

        Assert.IsNull(reloaded.ExtensionArea);
        Assert.AreEqual((ushort)2, reloaded.Width);
        Assert.AreEqual((ushort)2, reloaded.Height);
        Assert.AreEqual(TgaPixelDepth.Bpp24, reloaded.Header.ImageSpec.PixelDepth);
    }

    [TestMethod]
    public void ToBytes_TwoDeveloperAreaEntriesWithSameTag_ThrowsTgaValidationException()
    {
        TgaFile tga = LoadTgaWithDeveloperArea([5, 5]);

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => tga.ToBytes());

        Assert.IsTrue(ex.Errors.Any(e => e.Path.StartsWith("DeveloperArea", StringComparison.Ordinal)), ex.Message);
    }

    [TestMethod]
    public void ToBytes_TwoDeveloperAreaEntriesWithDifferentTags_Succeeds()
    {
        TgaFile tga = LoadTgaWithDeveloperArea([5, 6]);

        byte[] bytes = tga.ToBytes();

        Assert.IsTrue(bytes.Length > TgaHeader.Size);
    }

    /// <summary>
    /// <see cref="TgaFile.DeveloperArea"/> has an internal setter with no public constructor parameter to seed
    /// it directly, so the only public way to obtain a <see cref="TgaFile"/> with populated
    /// <see cref="TgaFile.DeveloperArea"/> entries is to load bytes containing a hand-built Developer Area
    /// directory (mirroring what <c>TargaSharp.IO.TgaReader.Read(Stream)</c> parses). This builds a
    /// minimal, otherwise-valid TGA (no image data, no dev area) via the public API, then splices in a
    /// Developer Directory with the given tags (each entry gets a 1-byte field, so it survives the
    /// "delete empty entries" step) between the ext area and the footer, patching only the footer's
    /// DeveloperDirectoryOffset to point at it.
    /// </summary>
    /// <param name="tags">Tag value for each Developer Area entry to create.</param>
    /// <returns>A <see cref="TgaFile"/> loaded from the crafted bytes, with DeveloperArea populated.</returns>
    private static TgaFile LoadTgaWithDeveloperArea(ushort[] tags)
    {
        var baseTga = new TgaFile(0, 0); // No image data; newFormat: true => Header + ExtensionArea + Footer only.
        using var baseStream = new MemoryStream();
        baseTga.Save(baseStream);
        byte[] baseBytes = baseStream.ToArray();

        int headerAndExtensionAreaLength = baseBytes.Length - TgaFooter.Size;
        byte[] headerAndExtensionArea = baseBytes[..headerAndExtensionAreaLength];
        byte[] originalFooter = baseBytes[headerAndExtensionAreaLength..];

        // One byte of field data per entry, placed right after the ext area.
        byte[] fieldData = new byte[tags.Length];
        uint devDirOffset = (uint)(headerAndExtensionAreaLength + fieldData.Length);

        using var devDirStream = new MemoryStream();
        using (var writer = new BinaryWriter(devDirStream))
        {
            writer.Write((ushort)tags.Length);
            for (int i = 0; i < tags.Length; i++)
            {
                writer.Write(tags[i]);
                writer.Write((uint)(headerAndExtensionAreaLength + i)); // Offset of this entry's 1-byte field data.
                writer.Write((uint)1); // Field size.
            }
        }
        byte[] devDirBytes = devDirStream.ToArray();

        // Patch DeveloperDirectoryOffset only (footer bytes 4-7); ExtensionAreaOffset (bytes 0-3) is
        // unaffected since the splice happens after the ext area.
        byte[] patchedFooter = (byte[])originalFooter.Clone();
        BitConverter.GetBytes(devDirOffset).CopyTo(patchedFooter, 4);

        byte[] full = [.. headerAndExtensionArea, .. fieldData, .. devDirBytes, .. patchedFooter];

        var tga = new TgaFile(full);
        Assert.IsNotNull(tga.DeveloperArea);
        Assert.AreEqual(tags.Length, tga.DeveloperArea!.Count);
        return tga;
    }
}
