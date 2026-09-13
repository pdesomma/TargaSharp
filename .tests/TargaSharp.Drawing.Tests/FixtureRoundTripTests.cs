using System.Drawing;
using System.Runtime.Versioning;
using TargaSharp;
using TargaSharp.Drawing;

namespace TargaSharp.Drawing.Tests;

/// <summary>
/// Smoke tests that load every real-world .tga fixture shipped with <c>.tests\Fixtures\</c> and
/// convert it to a <see cref="Bitmap"/>, asserting the bitmap dimensions match the loaded
/// <see cref="TgaFile"/>. See <c>TargaSharp.Tests</c>' own <c>FixtureRoundTripTests</c> for the
/// Bitmap-free load/save/reload/Header-equals round trip.
/// </summary>
[TestClass]
[SupportedOSPlatform("windows")]
public class FixtureRoundTripTests
{
    /// <summary>
    /// Enumerates every .tga fixture file, linked into this test assembly's output directory under
    /// <c>Fixtures\</c> (see <c>TargaSharp.Drawing.Tests.csproj</c>), as a separate <see cref="DynamicDataAttribute"/> case.
    /// </summary>
    /// <returns>A sequence of single-element object arrays, each containing a fixture file path.</returns>
    public static IEnumerable<object[]> GetFixtureFiles()
    {
        string fixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");

        // Windows' filesystem is case-insensitive, so "*.tga" alone would already match "*.TGA",
        // but Distinct() guards against double-matching on case-sensitive file systems too.
        var files = Directory.GetFiles(fixturesDir, "*.tga", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(fixturesDir, "*.TGA", SearchOption.TopDirectoryOnly))
            .Distinct()
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            yield return new object[] { file };
        }
    }

    /// <summary>
    /// Returns the display name for a fixture test case, shown in the test explorer.
    /// </summary>
    /// <param name="methodInfo">The test method being invoked.</param>
    /// <param name="data">The data row for this case (the fixture file path).</param>
    /// <returns>A human-readable test case name.</returns>
    public static string GetFixtureDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        $"{methodInfo.Name} ({Path.GetFileName((string)data[0])})";

    /// <summary>
    /// Loads a real-world .tga fixture and converts it to a bitmap, asserting the bitmap dimensions
    /// match the loaded <see cref="TgaFile"/>.
    /// </summary>
    /// <param name="filePath">Absolute path to the .tga fixture under test.</param>
    [TestMethod]
    [DynamicData(nameof(GetFixtureFiles), DynamicDataDisplayName = nameof(GetFixtureDisplayName))]
    public void LoadToBitmap_RealWorldFixture_ProducesValidBitmap(string filePath)
    {
        // Read the fixture bytes with a shared-read handle (File.ReadAllBytes) rather than
        // TgaFile(string), whose FileStream defaults to non-shared write access: with the sibling
        // TargaSharp.Tests fixture tests reading these same files from a separate test host process,
        // that non-shared handle intermittently collided with "file in use" IOExceptions.
        var tga = new TgaFile(File.ReadAllBytes(filePath));

        var bitmap = tga.ToBitmap();

        Assert.IsNotNull(bitmap);
        Assert.AreEqual(tga.Width, (ushort)bitmap.Width);
        Assert.AreEqual(tga.Height, (ushort)bitmap.Height);
    }
}
