using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaDateTime"/>.
/// </summary>
[TestClass]
public class TgaDateTimeTests
{
    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaDateTime(bytes),
            x => x.ToBytes(),
            TgaDateTime.Size,
            () => new TgaDateTime(1, 2, 1989, 3, 4, 5));
    }

    [TestMethod]
    public void IsUnset_DefaultInstance_ReturnsTrue()
    {
        Assert.IsTrue(new TgaDateTime().IsUnset);
    }

    [TestMethod]
    public void ToDateTime_UnsetInstance_ReturnsNull()
    {
        // Used to throw ArgumentOutOfRangeException from the DateTime ctor (year/month/day 0).
        Assert.IsNull(new TgaDateTime().ToDateTime());
    }

    [TestMethod]
    public void ToDateTime_SetInstance_RoundTripsThroughDateTimeCtor()
    {
        var expected = new DateTime(1989, 1, 2, 3, 4, 5);

        Assert.AreEqual(expected, new TgaDateTime(expected).ToDateTime());
    }

    [TestMethod]
    public void IsUnset_AnyFieldNonZero_ReturnsFalse()
    {
        Assert.IsFalse(new TgaDateTime { Second = 1 }.IsUnset);
        Assert.IsFalse(new TgaDateTime(new DateTime(2020, 5, 6)).IsUnset);
    }
}
