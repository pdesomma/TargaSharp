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
    /// An <see cref="ITgaLayoutPlanner"/> test double that emits one fixed section, proving
    /// <see cref="TgaWriter"/> writes whatever layout the planner it was given produces.
    /// </summary>
    private sealed class FixedLayoutPlanner : ITgaLayoutPlanner
    {
        /// <inheritdoc />
        public TgaLayout Plan(TgaFile file) => new([new TgaSection("Test", 0, [1, 2, 3])]);
    }

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
    public void Write_InjectedPlanner_WritesItsSections()
    {
        var writer = new TgaWriter(new TgaValidator(), new FixedLayoutPlanner());

        byte[] bytes = writer.Write(CreateSmall24BppFile());

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, bytes);
    }

    [TestMethod]
    public void Ctor_NullPlanner_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaWriter(new TgaValidator(), null!));
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
        CollectionAssert.AreEqual(file.ImageArea.ImageData, reloaded.ImageArea.ImageData);
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
        file.ImageArea.ImageData = [1, 2, 3]; // Wrong length: expects 2*2*3 = 12 bytes.
        ITgaWriter writer = new TgaWriter();

        var exception = Assert.ThrowsExactly<TgaValidationException>(() => writer.Write(file));

        Assert.HasCount(1, exception.Errors);
        Assert.AreEqual("ImageArea.ImageData", exception.Errors[0].Path);
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

    [TestMethod]
    public void Write_NonSeekableStream_WritesSameBytesAsSeekable()
    {
        var file = new TgaFile(2, 2);
        using var nonSeekable = new WriteOnlyStream();

        new TgaWriter().Write(file, nonSeekable);

        CollectionAssert.AreEqual(new TgaWriter().Write(file), nonSeekable.ToArray());
    }

    [TestMethod]
    public void Write_ReadOnlyStream_ThrowsArgumentException()
    {
        using var readOnly = new MemoryStream(new byte[16], writable: false);

        Assert.ThrowsExactly<ArgumentException>(() => new TgaWriter().Write(new TgaFile(2, 2), readOnly));
    }

    [TestMethod]
    public void Write_LayoutFailure_ThrowsTgaValidationExceptionDerivedFromTgaException()
    {
        var file = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.UncompressedTrueColor);
        file.ImageArea.ImageData = new byte[1]; // wrong length: caught by validation first

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => new TgaWriter().Write(file));

        Assert.IsInstanceOfType<TgaException>(ex);
    }

    /// <summary>
    /// A writable stream that reports <see cref="Stream.CanSeek"/> as false, to exercise the writer's argument check.
    /// </summary>
    private sealed class WriteOnlyStream : MemoryStream
    {
        /// <inheritdoc />
        public override bool CanSeek => false;
    }

    [TestMethod]
    public void Write_NoImageDataWithPostageStamp_ThrowsInsteadOfDroppingTheStamp()
    {
        // Used to validate, write, and read back with PostageStampImage == null.
        var file = new TgaFile();
        file.ToNewFormat();
        file.ExtensionArea!.PostageStampImage = new TgaPostageStampImage(1, 1, []);

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => new TgaWriter().Write(file));

        Assert.AreEqual("ExtensionArea.PostageStampImage", ex.Errors[0].Path);
    }

    [TestMethod]
    public void Write_DeveloperAreaWithoutFooter_ThrowsInsteadOfDroppingIt()
    {
        var file = new TgaFile(2, 2, newFormat: false);
        file.DeveloperArea = new TgaDeveloperArea([new TgaDeveloperEntry(1, 0, [1])]);

        var ex = Assert.ThrowsExactly<TgaValidationException>(() => new TgaWriter().Write(file));

        Assert.AreEqual("DeveloperArea", ex.Errors[0].Path);
    }

    [TestMethod]
    public void Write_NoImageDataWithoutPostageStamp_RoundTrips()
    {
        var file = new TgaFile();
        file.ToNewFormat();

        TgaFile read = new TgaReader().Read(new TgaWriter().Write(file));

        Assert.AreEqual(TgaImageType.NoImageData, read.Header.ImageType);
        Assert.IsNotNull(read.ExtensionArea);
        Assert.IsNull(read.ExtensionArea.PostageStampImage);
    }
}
