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
        var tga = new TgaFile(4, 4, TgaPixelDepth.Bpp8, TgaImageType.Uncompressed_ColorMapped);

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
        Assert.IsNotNull(tga.ImageOrColorMapArea);
        Assert.AreEqual((ushort)2, tga.Width);
        Assert.AreEqual((ushort)2, tga.Height);
    }

    [TestMethod]
    public void Clone_DefaultFormatTga_DoesNotThrowAndHasNoExtAreaOrFooter()
    {
        var tga = new TgaFile(2, 2, newFormat: false);

        var clone = tga.Clone();

        Assert.IsNull(clone.ExtArea);
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
        var tga = new TgaFile(4, 4, TgaPixelDepth.Bpp24, TgaImageType.Uncompressed_TrueColor);

        tga.UpdatePostageStampImage();
        Assert.IsNotNull(tga.ExtArea?.PostageStampImage);

        tga.DeletePostageStampImage();
        Assert.IsNull(tga.ExtArea?.PostageStampImage);
    }

    [TestMethod]
    public void DeletePostageStampImage_NullExtArea_DoesNotThrow()
    {
        var tga = new TgaFile();

        tga.DeletePostageStampImage();
    }

    [TestMethod]
    public void Save_ThenLoadFromStream_RoundTripsToIdenticalBytes()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.Uncompressed_TrueColor);
        tga.ImageOrColorMapArea.ImageData = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

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
    public void Save_ImageIDWithNChars_ReloadedHeaderIdLengthEqualsN()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.Uncompressed_TrueColor);
        tga.ImageOrColorMapArea.ImageData = new byte[2 * 2 * 3];
        tga.ImageOrColorMapArea.ImageID = new TgaString("Hello", 5);

        using var stream = new MemoryStream();
        tga.Save(stream);

        stream.Position = 0;
        var reloaded = new TgaFile(stream);

        Assert.AreEqual((byte)5, reloaded.Header.IdLength);
    }

    [TestMethod]
    public void Save_NullImageID_ReloadedHeaderIdLengthIsZero()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.Uncompressed_TrueColor);
        tga.ImageOrColorMapArea.ImageData = new byte[2 * 2 * 3];
        tga.ImageOrColorMapArea.ImageID = null;

        using var stream = new MemoryStream();
        tga.Save(stream);

        stream.Position = 0;
        var reloaded = new TgaFile(stream);

        Assert.AreEqual((byte)0, reloaded.Header.IdLength);
    }

    [TestMethod]
    public void Save_ExtAreaWithOtherData_ReloadedExtensionSizeEqualsMinSizePlusOtherDataLength()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.Uncompressed_TrueColor);
        tga.ImageOrColorMapArea.ImageData = new byte[2 * 2 * 3];
        tga.ExtArea!.OtherDataInExtensionArea = [1, 2, 3, 4];

        using var stream = new MemoryStream();
        tga.Save(stream);

        stream.Position = 0;
        var reloaded = new TgaFile(stream);

        Assert.AreEqual((ushort)(TgaExtArea.MinSize + 4), reloaded.ExtArea!.ExtensionSize);
    }

    [TestMethod]
    public void Header_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.Header))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void ImageOrColorMapArea_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.ImageOrColorMapArea))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void DevArea_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.DevArea))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void ExtArea_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.ExtArea))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void Footer_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaFile).GetProperty(nameof(TgaFile.Footer))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void LoadFromStream_ExtAreaSizeBelowMinSize_LeavesExtAreaNullWithoutThrowing()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.Uncompressed_TrueColor);

        using var stream = new MemoryStream();
        tga.Save(stream);
        byte[] bytes = stream.ToArray();

        // Control: reloading the untouched bytes must find the (valid, 495-byte) v2.0 ext area.
        var control = new TgaFile(bytes);
        Assert.IsNotNull(control.ExtArea);

        // Per spec the Extension Area Size field must be 495 for a v2.0 ext area; a reader should
        // only parse what it understands. Patch the declared size (the first 2 bytes of the ext
        // area, pointed to by the footer's ExtensionAreaOffset) down to something below MinSize (495)
        // to simulate a file that either predates or otherwise doesn't declare a v2.0 ext area.
        uint extAreaOffset = BitConverter.ToUInt32(bytes, bytes.Length - TgaFooter.Size);
        byte[] patched = (byte[])bytes.Clone();
        BitConverter.GetBytes((ushort)100).CopyTo(patched, (int)extAreaOffset);

        var reloaded = new TgaFile(patched);

        Assert.IsNull(reloaded.ExtArea);
        Assert.AreEqual((ushort)2, reloaded.Width);
        Assert.AreEqual((ushort)2, reloaded.Height);
        Assert.AreEqual(TgaPixelDepth.Bpp24, reloaded.Header.ImageSpec.PixelDepth);
    }

    [TestMethod]
    public void ToBytes_TwoDevAreaEntriesWithSameTag_ThrowsTgaValidationException()
    {
        TgaFile tga = LoadTgaWithDevArea([5, 5]);

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => tga.ToBytes());

        Assert.IsTrue(ex.Errors.Any(e => e.Path.StartsWith("DevArea", StringComparison.Ordinal)), ex.Message);
    }

    [TestMethod]
    public void ToBytes_TwoDevAreaEntriesWithDifferentTags_Succeeds()
    {
        TgaFile tga = LoadTgaWithDevArea([5, 6]);

        byte[] bytes = tga.ToBytes();

        Assert.IsTrue(bytes.Length > TgaHeader.Size);
    }

    /// <summary>
    /// <see cref="TgaFile.DevArea"/> has an internal setter with no public constructor parameter to seed
    /// it directly, so the only public way to obtain a <see cref="TgaFile"/> with populated
    /// <see cref="TgaFile.DevArea"/> entries is to load bytes containing a hand-built Developer Area
    /// directory (mirroring what <c>TargaSharp.IO.TgaReader.Read(Stream)</c> parses). This builds a
    /// minimal, otherwise-valid TGA (no image data, no dev area) via the public API, then splices in a
    /// Developer Directory with the given tags (each entry gets a 1-byte field, so it survives the
    /// "delete empty entries" step) between the ext area and the footer, patching only the footer's
    /// DeveloperDirectoryOffset to point at it.
    /// </summary>
    /// <param name="tags">Tag value for each Developer Area entry to create.</param>
    /// <returns>A <see cref="TgaFile"/> loaded from the crafted bytes, with DevArea populated.</returns>
    private static TgaFile LoadTgaWithDevArea(ushort[] tags)
    {
        var baseTga = new TgaFile(0, 0); // No image data; newFormat: true => Header + ExtArea + Footer only.
        using var baseStream = new MemoryStream();
        baseTga.Save(baseStream);
        byte[] baseBytes = baseStream.ToArray();

        int headerAndExtAreaLength = baseBytes.Length - TgaFooter.Size;
        byte[] headerAndExtArea = baseBytes[..headerAndExtAreaLength];
        byte[] originalFooter = baseBytes[headerAndExtAreaLength..];

        // One byte of field data per entry, placed right after the ext area.
        byte[] fieldData = new byte[tags.Length];
        uint devDirOffset = (uint)(headerAndExtAreaLength + fieldData.Length);

        using var devDirStream = new MemoryStream();
        using (var writer = new BinaryWriter(devDirStream))
        {
            writer.Write((ushort)tags.Length);
            for (int i = 0; i < tags.Length; i++)
            {
                writer.Write(tags[i]);
                writer.Write((uint)(headerAndExtAreaLength + i)); // Offset of this entry's 1-byte field data.
                writer.Write((uint)1); // Field size.
            }
        }
        byte[] devDirBytes = devDirStream.ToArray();

        // Patch DeveloperDirectoryOffset only (footer bytes 4-7); ExtensionAreaOffset (bytes 0-3) is
        // unaffected since the splice happens after the ext area.
        byte[] patchedFooter = (byte[])originalFooter.Clone();
        BitConverter.GetBytes(devDirOffset).CopyTo(patchedFooter, 4);

        byte[] full = [.. headerAndExtArea, .. fieldData, .. devDirBytes, .. patchedFooter];

        var tga = new TgaFile(full);
        Assert.IsNotNull(tga.DevArea);
        Assert.AreEqual(tags.Length, tga.DevArea!.Count);
        return tga;
    }
}
