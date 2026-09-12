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
        var file = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.Uncompressed_TrueColor);
        file.ImageOrColorMapArea.ImageData = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
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
        CollectionAssert.AreEqual(original.ImageOrColorMapArea.ImageData, loaded.ImageOrColorMapArea.ImageData);
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
        CollectionAssert.AreEqual(original.ImageOrColorMapArea.ImageData, loaded.ImageOrColorMapArea.ImageData);
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
            CollectionAssert.AreEqual(original.ImageOrColorMapArea.ImageData, loaded.ImageOrColorMapArea.ImageData);
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
}
