using System.Reflection;
using TargaSharp;

namespace TargaSharp.Tests;

/// <summary>
/// Smoke tests that load every real-world .tga fixture shipped with <c>TestWpfApp\Examples\</c> and
/// round-trip it through <see cref="TgaFile.Save(Stream)"/> / <see cref="TgaFile(Stream)"/> to make sure
/// the header survives a save/reload cycle unchanged. See <c>TargaSharp.Drawing.Tests</c>' own
/// <c>FixtureRoundTripTests</c> for the equivalent <see cref="System.Drawing.Bitmap"/> conversion assertions.
/// </summary>
[TestClass]
public class FixtureRoundTripTests
{
    /// <summary>
    /// Locates the repository's <c>TestWpfApp\Examples</c> directory by walking up from the test
    /// assembly's output directory until the repo root (identified by <c>TargaSharp.sln</c>) is found.
    /// </summary>
    /// <returns>The absolute path to the fixture directory.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the repo root or fixture directory cannot be located.</exception>
    private static string GetExamplesDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TargaSharp.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new DirectoryNotFoundException("Could not locate repo root (TargaSharp.sln) above " + AppContext.BaseDirectory);
        }

        var examplesDir = Path.Combine(dir.FullName, "TestWpfApp", "Examples");
        if (!Directory.Exists(examplesDir))
        {
            throw new DirectoryNotFoundException("Could not locate fixture directory at " + examplesDir);
        }

        return examplesDir;
    }

    /// <summary>
    /// Enumerates every .tga fixture file as a separate <see cref="DataTestMethodAttribute"/> case.
    /// </summary>
    /// <returns>A sequence of single-element object arrays, each containing a fixture file path.</returns>
    public static IEnumerable<object[]> GetFixtureFiles()
    {
        var examplesDir = GetExamplesDirectory();

        // Windows' filesystem is case-insensitive, so "*.tga" alone would already match "*.TGA",
        // but Distinct() guards against double-matching on case-sensitive file systems too.
        var files = Directory.GetFiles(examplesDir, "*.tga", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(examplesDir, "*.TGA", SearchOption.TopDirectoryOnly))
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
    public static string GetFixtureDisplayName(MethodInfo methodInfo, object[] data) =>
        $"{methodInfo.Name} ({Path.GetFileName((string)data[0])})";

    /// <summary>
    /// Loads a real-world .tga fixture and round-trips it through a <see cref="MemoryStream"/>
    /// save/reload, asserting the header survives unchanged.
    /// </summary>
    /// <param name="filePath">Absolute path to the .tga fixture under test.</param>
    [TestMethod]
    [DynamicData(nameof(GetFixtureFiles), DynamicDataDisplayName = nameof(GetFixtureDisplayName))]
    public void LoadAndRoundTrip_RealWorldFixture_ProducesMatchingHeader(string filePath)
    {
        // Read the fixture bytes with a shared-read handle (File.ReadAllBytes) rather than
        // TgaFile(string), whose FileStream defaults to non-shared write access: with the sibling
        // TargaSharp.Drawing.Tests fixture tests reading these same files from a separate test host
        // process, that non-shared handle intermittently collided with "file in use" IOExceptions.
        var tga = new TgaFile(File.ReadAllBytes(filePath));

        using var stream = new MemoryStream();
        tga.Save(stream);

        stream.Position = 0;
        var reloaded = new TgaFile(stream);

        Assert.AreEqual(tga.Header, reloaded.Header);
    }

    /// <summary>
    /// Loads every real-world .tga fixture and asserts <see cref="TgaFile.Validate"/> reports no
    /// semantic errors, i.e. that <see cref="TargaSharp.Validation.TgaValidator"/>'s rules (spec
    /// value ranges plus a couple of documented real-world relaxations - see
    /// <c>TgaValidatorTests</c>) are permissive enough to accept every shipped fixture.
    /// </summary>
    /// <param name="filePath">Absolute path to the .tga fixture under test.</param>
    [TestMethod]
    [DynamicData(nameof(GetFixtureFiles), DynamicDataDisplayName = nameof(GetFixtureDisplayName))]
    public void Validate_RealWorldFixture_ReturnsNoErrors(string filePath)
    {
        var tga = new TgaFile(File.ReadAllBytes(filePath));

        var errors = tga.Validate();

        Assert.AreEqual(0, errors.Count, $"Unexpected validation error(s): {string.Join("; ", errors.Select(e => $"{e.Path}: {e.Message}"))}");
    }
}
