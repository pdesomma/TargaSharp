using TargaSharp.IO;

namespace TargaSharp.Tests.IO;

/// <summary>
/// Hand-built malformed inputs for <see cref="TgaReader"/>. Every header-declared size is attacker
/// controlled, so each case checks that the reader fails with <see cref="TgaFormatException"/> (not an
/// unrelated exception, a huge allocation, or a silently truncated field).
/// </summary>
[TestClass]
public class TgaReaderMalformedInputTests
{
    /// <summary>
    /// Serializes a header for an uncompressed or RLE true-color image of the given size and depth, with no
    /// image bytes after it.
    /// </summary>
    private static byte[] HeaderOnly(ushort width, ushort height, TgaPixelDepth depth, TgaImageType imageType)
    {
        var header = new TgaHeader { ImageType = imageType };
        header.ImageSpec.ImageWidth = width;
        header.ImageSpec.ImageHeight = height;
        header.ImageSpec.PixelDepth = depth;
        header.ImageSpec.ImageDescriptor.AlphaChannelBits = (byte)(depth == TgaPixelDepth.Bpp32 ? 8 : 0);
        return header.ToBytes();
    }

    /// <summary>
    /// Takes a valid v2.0 file, replaces its footer with one pointing at an appended developer directory
    /// holding a single entry, and returns the result.
    /// </summary>
    private static byte[] WithDeveloperEntry(ushort tag, uint offset, uint size)
    {
        byte[] valid = new TgaFile(1, 1).ToBytes();
        using var ms = new MemoryStream();
        ms.Write(valid, 0, valid.Length - TgaFooter.Size);
        uint directoryOffset = (uint)ms.Position;
        using (var w = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            w.Write((ushort)1);
            w.Write(tag);
            w.Write(offset);
            w.Write(size);
        }
        byte[] footer = new TgaFooter(0, directoryOffset).ToBytes();
        ms.Write(footer, 0, footer.Length);
        return ms.ToArray();
    }

    /// <summary>
    /// A valid 2x2 24bpp v2.0 file with a color map, an image ID and a postage stamp, so every
    /// variable-length section the reader parses is present.
    /// </summary>
    private static TgaFile CreateFileWithEverySection()
    {
        var file = new TgaFile(2, 2, TgaPixelDepth.Bpp8, TgaImageType.UncompressedColorMapped);
        file.Header.ColorMapSpec.ColorMapLength = 4;
        file.ImageArea.ColorMapData = new byte[4 * 3];
        file.ImageArea.ImageId = new TgaString("identifier", 10);
        file.UpdatePostageStampImage();
        return file;
    }

    [TestMethod]
    public void Read_NoColorMapWithStaleColorMapLength_DoesNotConsumePaletteBytes()
    {
        // Spec Field 7: the color map is present only when ColorMapType says so. A stale length on a
        // NoColorMap header used to swallow the first pixel bytes as palette and misalign the image.
        var file = new TgaFile(2, 1, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor, newFormat: false);
        file.ImageArea.ImageData = [1, 2, 3, 4, 5, 6];
        byte[] bytes = file.ToBytes();
        TgaBinary.WriteUInt16(bytes, 5, 256); // ColorMapLength
        bytes[7] = 24;                        // ColorMapEntrySize

        TgaFile read = new TgaReader().Read(bytes);

        Assert.IsNull(read.ImageArea.ColorMapData);
        CollectionAssert.AreEqual(file.ImageArea.ImageData, read.ImageArea.ImageData);
    }

    [TestMethod]
    public void Read_RleImageWithZeroHeight_LoadsEmptyImageDataWithoutConsumingFooter()
    {
        // 0xN RLE image: the decoder used to read one packet regardless, eating the first footer/extension byte.
        var file = new TgaFile(2, 1, TgaPixelDepth.Bpp24, TgaImageType.RleTrueColor, newFormat: false);
        byte[] bytes = file.ToBytes();
        TgaBinary.WriteUInt16(bytes, 14, 0); // ImageHeight
        byte[] headerOnly = bytes.Take(TgaHeader.Size).ToArray();

        TgaFile read = new TgaReader().Read(headerOnly);

        Assert.AreEqual(0, read.ImageArea.ImageData!.Length);
    }

    [TestMethod]
    public void Read_ValidLegacyFileShorterThanFooter_LoadsWithoutFooter()
    {
        // 1x1 8bpp grayscale legacy file is 19 bytes, shorter than the 26-byte footer the reader probes for.
        var file = new TgaFile(1, 1, TgaPixelDepth.Bpp8, TgaImageType.UncompressedGrayscale, newFormat: false);
        byte[] bytes = file.ToBytes();
        Assert.IsTrue(bytes.Length < TgaFooter.Size);

        TgaFile read = new TgaReader().Read(bytes);

        Assert.IsNull(read.Footer);
        Assert.AreEqual((ushort)1, read.Width);
    }

    [TestMethod]
    public void Read_FooterOffsetsBeyondEndOfFile_ThrowsTgaFormatException()
    {
        TgaFile file = CreateFileWithEverySection();
        byte[] bytes = file.ToBytes();
        TgaBinary.WriteUInt32(bytes, bytes.Length - TgaFooter.Size, 0x7FFFFFFF); // ExtensionAreaOffset

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(bytes));
    }

    [TestMethod]
    public void Read_TruncatedScanLineTable_ThrowsTgaFormatException()
    {
        TgaFile file = CreateFileWithEverySection();
        file.ExtensionArea!.ScanLineTable = new uint[file.Height];
        byte[] bytes = file.ToBytes();
        // Point the scan-line table at the last 2 bytes so the second uint cannot be read.
        TgaBinary.WriteUInt32(bytes, (int)file.Footer!.ExtensionAreaOffset + 490, (uint)(bytes.Length - 2));

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(bytes));
    }

    [TestMethod]
    public void Read_PostageStampLargerThanMaxSize_IsSkipped()
    {
        TgaFile file = CreateFileWithEverySection();
        byte[] bytes = file.ToBytes();
        bytes[file.ExtensionArea!.PostageStampOffset] = TgaPostageStampImage.MaxSize + 1; // stamp width

        TgaFile read = new TgaReader().Read(bytes);

        Assert.IsNull(read.ExtensionArea!.PostageStampImage);
    }

    [TestMethod]
    public void Read_FileCutInsideImageId_ThrowsTgaFormatException()
    {
        byte[] bytes = CreateFileWithEverySection().ToBytes();
        byte[] truncated = bytes.Take(TgaHeader.Size + 3).ToArray();

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(truncated));
    }

    [TestMethod]
    public void Read_FileCutInsideColorMap_ThrowsTgaFormatException()
    {
        // Used to load with a silently shortened ColorMapData, failing much later in ToBitmap or Save.
        byte[] bytes = CreateFileWithEverySection().ToBytes();
        byte[] truncated = bytes.Take(TgaHeader.Size + 10 + 5).ToArray();

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(truncated));
    }

    [TestMethod]
    public void Read_DeveloperFieldRunningPastEndOfFile_ThrowsTgaFormatException()
    {
        // Directory entry of 10 bytes whose offset leaves only the footer's last 4 bytes; used to load a
        // silently 4-byte entry.
        int valid = new TgaFile(1, 1).ToBytes().Length;
        int total = valid + sizeof(ushort) + TgaDeveloperEntry.Size; // WithDeveloperEntry appends one directory
        byte[] bytes = WithDeveloperEntry(tag: 5, offset: (uint)(total - 4), size: 10);

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(bytes));
    }

    [TestMethod]
    public void Read_ExtensionSizeLargerThanBytesRemaining_ThrowsTgaFormatException()
    {
        // Declare 495 + 200 bytes of extension area when far fewer remain before end of file.
        TgaFile file = CreateFileWithEverySection();
        byte[] bytes = file.ToBytes();
        uint extOffset = file.Footer!.ExtensionAreaOffset;
        TgaBinary.WriteUInt16(bytes, (int)extOffset, (ushort)(TgaExtensionArea.MinSize + 200));
        byte[] cut = bytes.Take((int)(extOffset + TgaExtensionArea.MinSize + 10)).ToArray();

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read([.. cut, .. file.Footer.ToBytes()]));
    }

    [TestMethod]
    public void Read_DeveloperEntryDeclaringNearIntMaxSize_ThrowsTgaFormatExceptionWithoutAllocating()
    {
        // Used to allocate a ~2 GB buffer for the declared size and die with OutOfMemoryException.
        byte[] bytes = WithDeveloperEntry(tag: 5, offset: 0, size: 0x7FFFFFF0);

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(bytes));
    }

    [TestMethod]
    public void Read_UncompressedHeaderDeclaringGigabyteImageWithNoData_ThrowsTgaFormatException()
    {
        // 40000 x 40000 x 1 = 1.6 GB fits in int, so this is not the overflow case: the reader must
        // refuse to allocate it because the stream cannot possibly supply that many bytes.
        byte[] bytes = HeaderOnly(40000, 40000, TgaPixelDepth.Bpp8, TgaImageType.UncompressedGrayscale);

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(bytes));
    }

    [TestMethod]
    public void Read_RleStreamEndsInsideRunPacket_ThrowsTgaFormatException()
    {
        // 2x1 24bpp RLE: run packet header then 1 of the 3 pixel bytes. Leaked IndexOutOfRangeException before.
        byte[] bytes = [.. HeaderOnly(2, 1, TgaPixelDepth.Bpp24, TgaImageType.RleTrueColor), 0x81, 0x11];

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(bytes));
    }

    [TestMethod]
    public void Read_RlePacketOverrunsImage_ThrowsTgaFormatException()
    {
        // 2x1 24bpp RLE: a run of 4 pixels into a 2-pixel image.
        byte[] bytes = [.. HeaderOnly(2, 1, TgaPixelDepth.Bpp24, TgaImageType.RleTrueColor), 0x83, 1, 2, 3];

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(bytes));
    }

    [TestMethod]
    public void Read_RleHeaderDeclaringGigabyteImageWithTwoEncodedBytes_ThrowsTgaFormatException()
    {
        // RLE can expand, but never by more than 128x, so 2 encoded bytes cannot decode to 1.6 GB.
        byte[] bytes = [.. HeaderOnly(40000, 40000, TgaPixelDepth.Bpp8, TgaImageType.RleGrayscale), 0x80, 0x00];

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(bytes));
    }

    [TestMethod]
    public void Read_RleImageWithUnknownPixelDepth_LoadsEmptyImageDataForValidatorToReport()
    {
        byte[] bytes = HeaderOnly(2, 2, TgaPixelDepth.Other, TgaImageType.RleTrueColor);

        TgaFile file = new TgaReader().Read(bytes);

        Assert.AreEqual(0, file.ImageArea.ImageData!.Length);
        Assert.IsTrue(file.Validate().Any(e => e.Path == "Header.ImageSpec.PixelDepth"));
    }
}
