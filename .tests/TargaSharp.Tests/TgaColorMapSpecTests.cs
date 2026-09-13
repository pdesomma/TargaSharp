using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaColorMapSpec"/>.
/// </summary>
[TestClass]
public class TgaColorMapSpecTests
{
    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaColorMapSpec(bytes),
            x => x.ToBytes(),
            TgaColorMapSpec.Size,
            () => new TgaColorMapSpec([0x01, 0x02, 0x03, 0x04, 24]));
    }
}
