using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaDeveloperArea"/>.
/// </summary>
[TestClass]
public class TgaDeveloperAreaTests
{
    [TestMethod]
    public void ToBytes_TwoEntries_FirstTwoBytesAreLittleEndianCount()
    {
        var area = new TgaDeveloperArea(new List<TgaDeveloperEntry>
        {
            new TgaDeveloperEntry(1, 0, Array.Empty<byte>()),
            new TgaDeveloperEntry(2, 0, Array.Empty<byte>()),
        });

        byte[] bytes = area.ToBytes();

        Assert.AreEqual((byte)2, bytes[0]);
        Assert.AreEqual((byte)0, bytes[1]);
    }

    [TestMethod]
    public void ToBytes_MoreThanUshortMaxEntries_ThrowsInvalidOperationException()
    {
        var entries = new List<TgaDeveloperEntry>(ushort.MaxValue + 1);
        for (int i = 0; i <= ushort.MaxValue; i++)
            entries.Add(new TgaDeveloperEntry((ushort)i, 0, Array.Empty<byte>()));

        var area = new TgaDeveloperArea(entries);

        Assert.ThrowsExactly<InvalidOperationException>(() => area.ToBytes());
    }
}
