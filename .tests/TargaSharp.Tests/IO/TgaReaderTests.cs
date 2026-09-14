using TargaSharp.IO;

namespace TargaSharp.Tests.IO;

/// <summary>
/// Tests for <see cref="TgaReader"/>, exercised through the <see cref="ITgaReader"/> interface.
/// </summary>
[TestClass]
public class TgaReaderTests
{
    /// <summary>
    /// Builds a small, otherwise-valid 24bpp <see cref="TgaFile"/> with populated image data.
    /// </summary>
    /// <returns>A small 24bpp <see cref="TgaFile"/>.</returns>
    private static TgaFile CreateSmall24BppFile()
    {
        var file = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor);
        file.ImageArea.ImageData = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        return file;
    }

    [TestMethod]
    public void Read_Stream_RoundTripsSmall24BppImage()
    {
        ITgaWriter writer = new TgaWriter();
        ITgaReader reader = new TgaReader();
        TgaFile original = CreateSmall24BppFile();
        byte[] bytes = writer.Write(original);

        TgaFile loaded = reader.Read(new MemoryStream(bytes));

        Assert.AreEqual(original.Header, loaded.Header);
        CollectionAssert.AreEqual(original.ImageArea.ImageData, loaded.ImageArea.ImageData);
    }

    [TestMethod]
    public void Read_Bytes_RoundTripsSmall24BppImage()
    {
        ITgaWriter writer = new TgaWriter();
        ITgaReader reader = new TgaReader();
        TgaFile original = CreateSmall24BppFile();
        byte[] bytes = writer.Write(original);

        TgaFile loaded = reader.Read(bytes);

        Assert.AreEqual(original.Header, loaded.Header);
        CollectionAssert.AreEqual(original.ImageArea.ImageData, loaded.ImageArea.ImageData);
    }

    [TestMethod]
    public void Read_Path_RoundTripsSmall24BppImage()
    {
        ITgaWriter writer = new TgaWriter();
        ITgaReader reader = new TgaReader();
        TgaFile original = CreateSmall24BppFile();
        string path = Path.Combine(Path.GetTempPath(), $"{nameof(TgaReaderTests)}_{Guid.NewGuid():N}.tga");

        try
        {
            writer.Write(original, path);
            TgaFile loaded = reader.Read(path);

            Assert.AreEqual(original.Header, loaded.Header);
            CollectionAssert.AreEqual(original.ImageArea.ImageData, loaded.ImageArea.ImageData);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void Read_NullStream_ThrowsArgumentNullException()
    {
        ITgaReader reader = new TgaReader();

        Assert.ThrowsExactly<ArgumentNullException>(() => reader.Read((Stream)null!));
    }

    [TestMethod]
    public void Read_NullBytes_ThrowsArgumentNullException()
    {
        ITgaReader reader = new TgaReader();

        Assert.ThrowsExactly<ArgumentNullException>(() => reader.Read((byte[])null!));
    }

    [TestMethod]
    public void Read_NullPath_ThrowsArgumentNullException()
    {
        ITgaReader reader = new TgaReader();

        Assert.ThrowsExactly<ArgumentNullException>(() => reader.Read((string)null!));
    }

    [TestMethod]
    public void Read_PathDoesNotExist_ThrowsFileNotFoundException()
    {
        ITgaReader reader = new TgaReader();
        string path = Path.Combine(Path.GetTempPath(), $"{nameof(TgaReaderTests)}_{Guid.NewGuid():N}_missing.tga");

        Assert.ThrowsExactly<FileNotFoundException>(() => reader.Read(path));
    }

    [TestMethod]
    public void Read_TruncatedStream_ThrowsTgaFormatException()
    {
        byte[] bytes = new TgaWriter().Write(CreateSmall24BppFile());
        byte[] truncated = bytes.Take(TgaHeader.Size + 3).ToArray();

        var ex = Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(truncated));

        Assert.IsInstanceOfType<TgaException>(ex);
    }

    [TestMethod]
    public void Read_HeaderWhoseImageSizeOverflowsInt_ThrowsTgaFormatException()
    {
        // 32768 x 32768 x 4 == 2^32, which wraps to 0 in int arithmetic: the reader used to accept this
        // header with zero image bytes as a successfully loaded 32768x32768 image.
        var header = new TgaHeader { ImageType = TgaImageType.UncompressedTrueColor };
        header.ImageSpec.ImageWidth = 32768;
        header.ImageSpec.ImageHeight = 32768;
        header.ImageSpec.PixelDepth = TgaPixelDepth.Bpp32;
        header.ImageSpec.ImageDescriptor.AlphaChannelBits = 8;

        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(header.ToBytes()));
    }

    [TestMethod]
    public void Read_StreamShorterThanHeader_ThrowsTgaFormatException()
    {
        Assert.ThrowsExactly<TgaFormatException>(() => new TgaReader().Read(new byte[5]));
    }

    [TestMethod]
    public void Read_NonSeekableStream_ThrowsArgumentException()
    {
        using var nonSeekable = new NonSeekableStream();

        Assert.ThrowsExactly<ArgumentException>(() => new TgaReader().Read(nonSeekable));
    }

    [TestMethod]
    public void Read_Stream_LeavesStreamOpen()
    {
        using var stream = new MemoryStream(new TgaWriter().Write(CreateSmall24BppFile()));

        new TgaReader().Read(stream);

        Assert.IsTrue(stream.CanRead);
    }

    /// <summary>
    /// A readable stream that reports <see cref="Stream.CanSeek"/> as false, to exercise the reader's argument check.
    /// </summary>
    private sealed class NonSeekableStream : MemoryStream
    {
        /// <inheritdoc />
        public override bool CanSeek => false;
    }
}
