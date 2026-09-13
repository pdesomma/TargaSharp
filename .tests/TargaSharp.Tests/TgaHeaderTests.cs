using System.Reflection;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaHeader"/>.
/// </summary>
[TestClass]
public class TgaHeaderTests
{
    [TestMethod]
    public void IdLength_Property_HasInternalSetter()
    {
        PropertyInfo property = typeof(TgaHeader).GetProperty(nameof(TgaHeader.IdLength))!;

        Assert.IsTrue(property.SetMethod!.IsAssembly);
    }

    [TestMethod]
    public void DefaultCtor_NewInstance_ColorMapSpecAndImageSpecAreNonNull()
    {
        var header = new TgaHeader();

        Assert.IsNotNull(header.ColorMapSpec);
        Assert.IsNotNull(header.ImageSpec);
    }

    [TestMethod]
    public void ToBytes_DefaultInstance_LengthEqualsSize()
    {
        var header = new TgaHeader();

        byte[] bytes = header.ToBytes();

        Assert.AreEqual(TgaHeader.Size, bytes.Length);
    }

    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaHeader(bytes),
            x => x.ToBytes(),
            TgaHeader.Size,
            () => new TgaHeader());
    }
}
