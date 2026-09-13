using TargaSharp.Validation;

namespace TargaSharp.Tests.Validation;

/// <summary>
/// Tests for <see cref="TgaValidationException"/>.
/// </summary>
[TestClass]
public class TgaValidationExceptionTests
{
    [TestMethod]
    public void Ctor_SingleError_ExposesItAndMentionsItsPathInMessage()
    {
        TgaValidationError[] errors = [new TgaValidationError("Header.ImageType", "Bad type.")];

        var exception = new TgaValidationException(errors);

        Assert.HasCount(1, exception.Errors);
        Assert.Contains("1", exception.Message);
        Assert.Contains("Header.ImageType", exception.Message);
    }

    [TestMethod]
    public void Ctor_FewerErrorsThanMax_MessageListsEveryPathAndOmitsMoreCount()
    {
        TgaValidationError[] errors =
        [
            new TgaValidationError("Header.ImageType", "Bad type."),
            new TgaValidationError("Header.ImageSpec.PixelDepth", "Bad depth."),
        ];

        var exception = new TgaValidationException(errors);

        Assert.Contains("2", exception.Message);
        Assert.Contains("Header.ImageType", exception.Message);
        Assert.Contains("Header.ImageSpec.PixelDepth", exception.Message);
        Assert.DoesNotContain("more", exception.Message);
    }

    [TestMethod]
    public void Ctor_MoreErrorsThanFitInMessage_MessageMentionsRemainingCount()
    {
        var errors = Enumerable.Range(0, 8).Select(i => new TgaValidationError($"Path{i}", "Bad.")).ToArray();

        var exception = new TgaValidationException(errors);

        Assert.HasCount(8, exception.Errors);
        Assert.Contains("8", exception.Message);
        Assert.Contains("more", exception.Message);
    }

    [TestMethod]
    public void Ctor_NullErrors_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaValidationException(null!));
    }

    [TestMethod]
    public void Ctor_EmptyErrors_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TgaValidationException([]));
    }
}
