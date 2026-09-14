using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaColorKey"/>.
/// </summary>
[TestClass]
public class TgaColorKeyTests
{
    [TestMethod]
    public void Ctor_Int_UnpacksArgbChannels()
    {
        var key = new TgaColorKey(unchecked((int)0x11223344));

        Assert.AreEqual((byte)0x11, key.A);
        Assert.AreEqual((byte)0x22, key.R);
        Assert.AreEqual((byte)0x33, key.G);
        Assert.AreEqual((byte)0x44, key.B);
    }

    [TestMethod]
    public void ToInt_Channels_PacksArgb()
    {
        Assert.AreEqual(unchecked((int)0x11223344), new TgaColorKey(0x11, 0x22, 0x33, 0x44).ToInt());
    }

    [TestMethod]
    public void ToBytes_Channels_WritesLittleEndianArgb()
    {
        // Spec Field 21 stores A:R:G:B as one little-endian LONG with A the most significant byte, i.e. B G R A in file order.
        CollectionAssert.AreEqual(new byte[] { 0x44, 0x33, 0x22, 0x11 }, new TgaColorKey(0x11, 0x22, 0x33, 0x44).ToBytes());
    }

    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaColorKey(bytes),
            x => x.ToBytes(),
            TgaColorKey.Size,
            () => new TgaColorKey(0x11, 0x22, 0x33, 0x44));
    }
}
