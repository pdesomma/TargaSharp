using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaImageTypeExtensions"/>.
/// </summary>
[TestClass]
public class TgaImageTypeExtensionsTests
{
    [TestMethod]
    [DataRow((byte)9, true)]
    [DataRow((byte)10, true)]
    [DataRow((byte)11, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)1, false)]
    [DataRow((byte)2, false)]
    [DataRow((byte)3, false)]
    [DataRow((byte)4, false)]
    [DataRow((byte)8, false)]
    [DataRow((byte)12, false)]
    [DataRow((byte)127, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsRunLengthEncoded_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var imageType = (TgaImageType)value;

        Assert.AreEqual(expected, imageType.IsRunLengthEncoded());
    }

    [TestMethod]
    [DataRow((byte)1, true)]
    [DataRow((byte)9, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)2, false)]
    [DataRow((byte)3, false)]
    [DataRow((byte)4, false)]
    [DataRow((byte)8, false)]
    [DataRow((byte)10, false)]
    [DataRow((byte)11, false)]
    [DataRow((byte)12, false)]
    [DataRow((byte)127, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsColorMapped_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var imageType = (TgaImageType)value;

        Assert.AreEqual(expected, imageType.IsColorMapped());
    }

    [TestMethod]
    [DataRow((byte)2, true)]
    [DataRow((byte)10, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)1, false)]
    [DataRow((byte)3, false)]
    [DataRow((byte)4, false)]
    [DataRow((byte)8, false)]
    [DataRow((byte)9, false)]
    [DataRow((byte)11, false)]
    [DataRow((byte)12, false)]
    [DataRow((byte)127, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsTrueColor_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var imageType = (TgaImageType)value;

        Assert.AreEqual(expected, imageType.IsTrueColor());
    }

    [TestMethod]
    [DataRow((byte)3, true)]
    [DataRow((byte)11, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)1, false)]
    [DataRow((byte)2, false)]
    [DataRow((byte)4, false)]
    [DataRow((byte)8, false)]
    [DataRow((byte)9, false)]
    [DataRow((byte)10, false)]
    [DataRow((byte)12, false)]
    [DataRow((byte)127, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsGrayscale_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var imageType = (TgaImageType)value;

        Assert.AreEqual(expected, imageType.IsGrayscale());
    }

    [TestMethod]
    [DataRow((byte)0, true)]
    [DataRow((byte)1, true)]
    [DataRow((byte)2, true)]
    [DataRow((byte)3, true)]
    [DataRow((byte)9, true)]
    [DataRow((byte)10, true)]
    [DataRow((byte)11, true)]
    [DataRow((byte)4, false)]
    [DataRow((byte)8, false)]
    [DataRow((byte)12, false)]
    [DataRow((byte)127, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsKnown_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var imageType = (TgaImageType)value;

        Assert.AreEqual(expected, imageType.IsKnown());
    }

    [TestMethod]
    [DataRow((byte)4, true)]
    [DataRow((byte)8, true)]
    [DataRow((byte)12, true)]
    [DataRow((byte)127, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)1, false)]
    [DataRow((byte)3, false)]
    [DataRow((byte)9, false)]
    [DataRow((byte)11, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsTruevisionReserved_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var imageType = (TgaImageType)value;

        Assert.AreEqual(expected, imageType.IsTruevisionReserved());
    }

    [TestMethod]
    [DataRow((byte)128, true)]
    [DataRow((byte)200, true)]
    [DataRow((byte)255, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)1, false)]
    [DataRow((byte)3, false)]
    [DataRow((byte)4, false)]
    [DataRow((byte)8, false)]
    [DataRow((byte)9, false)]
    [DataRow((byte)11, false)]
    [DataRow((byte)12, false)]
    [DataRow((byte)127, false)]
    public void IsDeveloperDefined_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var imageType = (TgaImageType)value;

        Assert.AreEqual(expected, imageType.IsDeveloperDefined());
    }
}
