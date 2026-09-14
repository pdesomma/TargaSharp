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
    public void Ctor_NullEntries_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TgaDeveloperArea((List<TgaDeveloperEntry>)null!));
    }

    [TestMethod]
    public void Copy_MutatedCopy_DoesNotAffectOriginal()
    {
        var area = new TgaDeveloperArea([new TgaDeveloperEntry(1, 0, [1, 2, 3])]);

        TgaDeveloperArea copy = area.Copy();
        copy[0].Data[0] = 99;
        copy.Entries.Add(new TgaDeveloperEntry(2, 0, [4]));

        Assert.AreEqual((byte)1, area[0].Data[0]);
        Assert.AreEqual(1, area.Count);
        Assert.AreNotEqual(area, copy);
    }

    [TestMethod]
    public void Equals_SameEntries_ReturnsTrueWithEqualHashCodes()
    {
        var a = new TgaDeveloperArea([new TgaDeveloperEntry(1, 0, [1, 2, 3])]);
        var b = new TgaDeveloperArea([new TgaDeveloperEntry(1, 0, [1, 2, 3])]);

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_NullEntry_DoesNotThrow()
    {
        var area = new TgaDeveloperArea([null!]);

        _ = area.GetHashCode();
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

    [TestMethod]
    public void Copy_NullEntry_PreservesNullWithoutThrowing()
    {
        var area = new TgaDeveloperArea([new TgaDeveloperEntry(1, 0, [9]), null!]);

        TgaDeveloperArea copy = area.Copy();

        Assert.AreEqual(2, copy.Count);
        Assert.IsNull(copy[1]);
        Assert.AreEqual(area[0], copy[0]);
        Assert.AreNotSame(area[0], copy[0]);
    }

    [TestMethod]
    public void ToBytes_NullEntry_ThrowsInvalidOperationExceptionNamingIndex()
    {
        var area = new TgaDeveloperArea([new TgaDeveloperEntry(1, 0, [9]), null!]);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => area.ToBytes());

        StringAssert.Contains(ex.Message, "[1]");
    }
}
