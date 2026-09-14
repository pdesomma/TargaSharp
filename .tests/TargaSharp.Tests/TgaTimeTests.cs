using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaTime"/>.
/// </summary>
[TestClass]
public class TgaTimeTests
{
    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaTime(bytes),
            x => x.ToBytes(),
            TgaTime.Size,
            () => new TgaTime(10, 20, 30));
    }

    [TestMethod]
    public void Ctor_Ints_PopulatesFields()
    {
        var time = new TgaTime(65535, 59, 59);

        Assert.AreEqual((ushort)65535, time.Hours);
        Assert.AreEqual((ushort)59, time.Minutes);
        Assert.AreEqual((ushort)59, time.Seconds);
    }

    [TestMethod]
    [DataRow(-1, 0, 0)]
    [DataRow(65536, 0, 0)]
    [DataRow(0, 60, 0)]
    [DataRow(0, -1, 0)]
    [DataRow(0, 0, 60)]
    public void Ctor_IntsOutOfRange_ThrowsArgumentOutOfRangeException(int hours, int minutes, int seconds)
    {
        // Used to silently narrow: new TgaTime(70000, 0, 0).Hours == 4464.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaTime(hours, minutes, seconds));
    }

    [TestMethod]
    public void Ctor_TimeSpan_RoundTripsThroughToTimeSpan()
    {
        var span = new TimeSpan(100, 5, 6);

        var time = new TgaTime(span);

        Assert.AreEqual((ushort)100, time.Hours);
        Assert.AreEqual(span, time.ToTimeSpan());
    }

    [TestMethod]
    public void Ctor_NegativeTimeSpan_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaTime(TimeSpan.FromHours(-1)));
    }

    [TestMethod]
    public void Ctor_TimeSpanOver65535Hours_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaTime(TimeSpan.FromHours(65536)));
    }

    [TestMethod]
    public void Ctor_NegativeSubHourTimeSpan_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TgaTime(TimeSpan.FromMinutes(-30)));
    }
}
