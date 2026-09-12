using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaDevArea"/>.
/// </summary>
[TestClass]
public class TgaDevAreaTests
{
    [TestMethod]
    public void ToBytes_TwoEntries_FirstTwoBytesAreLittleEndianCount()
    {
        var area = new TgaDevArea(new List<TgaDevEntry>
        {
            new TgaDevEntry(1, 0, Array.Empty<byte>()),
            new TgaDevEntry(2, 0, Array.Empty<byte>()),
        });

        byte[] bytes = area.ToBytes();

        Assert.AreEqual((byte)2, bytes[0]);
        Assert.AreEqual((byte)0, bytes[1]);
    }

    [TestMethod]
    public void ToBytes_MoreThanUshortMaxEntries_ThrowsInvalidOperationException()
    {
        var entries = new List<TgaDevEntry>(ushort.MaxValue + 1);
        for (int i = 0; i <= ushort.MaxValue; i++)
            entries.Add(new TgaDevEntry((ushort)i, 0, Array.Empty<byte>()));

        var area = new TgaDevArea(entries);

        Assert.ThrowsExactly<InvalidOperationException>(() => area.ToBytes());
    }
}
