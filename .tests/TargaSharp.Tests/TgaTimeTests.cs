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
}
