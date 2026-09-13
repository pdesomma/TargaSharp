using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaColorKey"/>.
/// </summary>
[TestClass]
public class TgaColorKeyTests
{
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
