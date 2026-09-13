using TargaSharp.IO;
using TargaSharp.Validation;

namespace TargaSharp.Tests.IO;

/// <summary>
/// Tests for <see cref="TgaWriter"/>, exercised through the <see cref="ITgaWriter"/> interface.
/// </summary>
[TestClass]
public class TgaWriterTests
{
    /// <summary>
    /// A hand-written <see cref="ITgaValidator"/> test double that always reports the same fixed
    /// error, used to prove <see cref="TgaWriter"/> actually calls the validator it was given
    /// rather than always constructing its own <see cref="TgaValidator"/>.
    /// </summary>
    private sealed class AlwaysFailingValidator : ITgaValidator
    {
        /// <inheritdoc />
        public IReadOnlyList<TgaValidationError> Validate(TgaFile file) => [new TgaValidationError("Test.Path", "Test message.")];
    }

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
    public void Write_Stream_OutputIsIdenticalToFileToBytes()
    {
        TgaFile file = CreateSmall24BppFile();
        ITgaWriter writer = new TgaWriter();

        using var stream = new MemoryStream();
        writer.Write(file, stream);

        CollectionAssert.AreEqual(file.ToBytes(), stream.ToArray());
    }

    [TestMethod]
    public void Write_Bytes_OutputIsIdenticalToFileToBytes()
    {
        TgaFile file = CreateSmall24BppFile();
        ITgaWriter writer = new TgaWriter();

        byte[] written = writer.Write(file);

        CollectionAssert.AreEqual(file.ToBytes(), written);
    }

    [TestMethod]
    public void Write_Path_OutputIsIdenticalToFileToBytes()
    {
        TgaFile file = CreateSmall24BppFile();
        ITgaWriter writer = new TgaWriter();
        string path = Path.Combine(Path.GetTempPath(), $"{nameof(TgaWriterTests)}_{Guid.NewGuid():N}.tga");

        try
        {
            writer.Write(file, path);
            byte[] written = File.ReadAllBytes(path);

            CollectionAssert.AreEqual(file.ToBytes(), written);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void Write_ThenRead_RoundTripsViaInterfaces()
    {
        TgaFile file = CreateSmall24BppFile();
        ITgaWriter writer = new TgaWriter();
        ITgaReader reader = new TgaReader();

        byte[] bytes = writer.Write(file);
        TgaFile reloaded = reader.Read(bytes);

        Assert.AreEqual(file.Header, reloaded.Header);
        CollectionAssert.AreEqual(file.ImageOrColorMapArea.ImageData, reloaded.ImageOrColorMapArea.ImageData);
    }

    [TestMethod]
    public void Write_NullFileToStream_ThrowsArgumentNullException()
    {
        ITgaWriter writer = new TgaWriter();
        using var stream = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() => writer.Write(null!, stream));
    }

    [TestMethod]
    public void Write_NullFileToBytes_ThrowsArgumentNullException()
    {
        ITgaWriter writer = new TgaWriter();

        Assert.ThrowsExactly<ArgumentNullException>(() => writer.Write(null!));
    }

    [TestMethod]
    public void Write_NullFileToPath_ThrowsArgumentNullException()
    {
        ITgaWriter writer = new TgaWriter();

        Assert.ThrowsExactly<ArgumentNullException>(() => writer.Write(null!, "unused.tga"));
    }

    [TestMethod]
    public void Write_NullStream_ThrowsArgumentNullException()
    {
        ITgaWriter writer = new TgaWriter();
        TgaFile file = CreateSmall24BppFile();

        Assert.ThrowsExactly<ArgumentNullException>(() => writer.Write(file, (Stream)null!));
    }

    [TestMethod]
    public void Write_ValidFile_Succeeds()
    {
        TgaFile file = CreateSmall24BppFile();
        ITgaWriter writer = new TgaWriter();

        byte[] written = writer.Write(file);

        Assert.IsTrue(written.Length > 0);
    }

    [TestMethod]
    public void Write_InvalidFile_ThrowsTgaValidationExceptionWithErrors()
    {
        TgaFile file = CreateSmall24BppFile();
        file.ImageOrColorMapArea.ImageData = [1, 2, 3]; // Wrong length: expects 2*2*3 = 12 bytes.
        ITgaWriter writer = new TgaWriter();

        var exception = Assert.ThrowsExactly<TgaValidationException>(() => writer.Write(file));

        Assert.HasCount(1, exception.Errors);
        Assert.AreEqual("ImageOrColorMapArea.ImageData", exception.Errors[0].Path);
    }

    [TestMethod]
    public void Write_ValidFileButCustomValidatorReportsErrors_ThrowsTgaValidationException()
    {
        TgaFile file = CreateSmall24BppFile();
        ITgaWriter writer = new TgaWriter(new AlwaysFailingValidator());

        var exception = Assert.ThrowsExactly<TgaValidationException>(() => writer.Write(file));

        Assert.HasCount(1, exception.Errors);
        Assert.AreEqual("Test.Path", exception.Errors[0].Path);
    }

    [TestMethod]
    public void Ctor_NullValidator_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaWriter(null!));
    }
}
