using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaColorMapTypeExtensions"/>.
/// </summary>
[TestClass]
public class TgaColorMapTypeExtensionsTests
{
    [TestMethod]
    [DataRow((byte)0, true)]
    [DataRow((byte)1, true)]
    [DataRow((byte)2, false)]
    [DataRow((byte)127, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsKnown_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var colorMapType = (TgaColorMapType)value;

        Assert.AreEqual(expected, colorMapType.IsKnown());
    }

    [TestMethod]
    [DataRow((byte)2, true)]
    [DataRow((byte)127, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)1, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsTruevisionReserved_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var colorMapType = (TgaColorMapType)value;

        Assert.AreEqual(expected, colorMapType.IsTruevisionReserved());
    }

    [TestMethod]
    [DataRow((byte)128, true)]
    [DataRow((byte)200, true)]
    [DataRow((byte)255, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)1, false)]
    [DataRow((byte)2, false)]
    [DataRow((byte)127, false)]
    public void IsDeveloperDefined_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var colorMapType = (TgaColorMapType)value;

        Assert.AreEqual(expected, colorMapType.IsDeveloperDefined());
    }
}
