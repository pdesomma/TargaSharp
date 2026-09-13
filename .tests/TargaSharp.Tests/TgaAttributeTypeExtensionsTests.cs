using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaAttributeTypeExtensions"/>.
/// </summary>
[TestClass]
public class TgaAttributeTypeExtensionsTests
{
    [TestMethod]
    [DataRow((byte)0, true)]
    [DataRow((byte)1, true)]
    [DataRow((byte)2, true)]
    [DataRow((byte)3, true)]
    [DataRow((byte)4, true)]
    [DataRow((byte)5, false)]
    [DataRow((byte)127, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsKnown_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var attributeType = (TgaAttributeType)value;

        Assert.AreEqual(expected, attributeType.IsKnown());
    }

    [TestMethod]
    [DataRow((byte)5, true)]
    [DataRow((byte)127, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)4, false)]
    [DataRow((byte)128, false)]
    [DataRow((byte)200, false)]
    [DataRow((byte)255, false)]
    public void IsReserved_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var attributeType = (TgaAttributeType)value;

        Assert.AreEqual(expected, attributeType.IsReserved());
    }

    [TestMethod]
    [DataRow((byte)128, true)]
    [DataRow((byte)200, true)]
    [DataRow((byte)255, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)4, false)]
    [DataRow((byte)5, false)]
    [DataRow((byte)127, false)]
    public void IsUnassigned_BoundaryValues_ReturnsExpected(byte value, bool expected)
    {
        var attributeType = (TgaAttributeType)value;

        Assert.AreEqual(expected, attributeType.IsUnassigned());
    }
}
