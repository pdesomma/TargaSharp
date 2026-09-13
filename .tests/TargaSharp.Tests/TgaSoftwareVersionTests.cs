using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Tests for <see cref="TgaSoftwareVersion"/>.
/// </summary>
[TestClass]
public class TgaSoftwareVersionTests
{
    [TestMethod]
    public void Ctor_ByteContract_Holds()
    {
        ByteSerializableContract.AssertByteCtorContract(
            bytes => new TgaSoftwareVersion(bytes),
            x => x.ToBytes(),
            TgaSoftwareVersion.Size,
            () => new TgaSoftwareVersion(117, 'b'));
    }
}
