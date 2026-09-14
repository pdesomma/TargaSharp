namespace TargaSharp.Tests;

[TestClass]
public class TgaArrayEqualityTests
{
    [TestMethod]
    public void Equals_SameReference_ReturnsTrue()
    {
        byte[] a = [1, 2];

        Assert.IsTrue(TgaArrayEquality.Equals(a, a));
    }

    [TestMethod]
    public void Equals_BothNull_ReturnsTrue()
    {
        Assert.IsTrue(TgaArrayEquality.Equals<byte>(null, null));
    }

    [TestMethod]
    public void Equals_OneNull_ReturnsFalse()
    {
        Assert.IsFalse(TgaArrayEquality.Equals(new byte[] { 1 }, null));
        Assert.IsFalse(TgaArrayEquality.Equals(null, new byte[] { 1 }));
    }

    [TestMethod]
    public void Equals_SameElements_ReturnsTrue()
    {
        Assert.IsTrue(TgaArrayEquality.Equals(new uint[] { 1, 2, 3 }, new uint[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void Equals_DifferentElementsOrLength_ReturnsFalse()
    {
        Assert.IsFalse(TgaArrayEquality.Equals(new ushort[] { 1, 2 }, new ushort[] { 1, 3 }));
        Assert.IsFalse(TgaArrayEquality.Equals(new ushort[] { 1, 2 }, new ushort[] { 1 }));
    }

    [TestMethod]
    public void Hash_Null_LeavesHashUnchanged()
    {
        Assert.AreEqual(27, TgaArrayEquality.Hash<byte>(27, null));
    }

    [TestMethod]
    public void Hash_EqualArrays_ProduceEqualHashes()
    {
        Assert.AreEqual(TgaArrayEquality.Hash(27, new byte[] { 1, 2 }), TgaArrayEquality.Hash(27, new byte[] { 1, 2 }));
        Assert.AreNotEqual(TgaArrayEquality.Hash(27, new byte[] { 1, 2 }), TgaArrayEquality.Hash(27, new byte[] { 2, 1 }));
    }
}
