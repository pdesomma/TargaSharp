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
    public void Read_RleHeaderDeclaringGigabyteImageWithTwoEncodedBytes_ThrowsTgaFormatException()
    {
        // RLE can expand, but never by more than 128x, so 2 encoded bytes cannot decode to 1.6 GB.
        byte[] bytes = [.. HeaderOnly(40000, 40000, TgaPixelDepth.Bpp8, TgaImageType.RleGrayscale), 0x80, 0x00];

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(bytes));
    }
}
