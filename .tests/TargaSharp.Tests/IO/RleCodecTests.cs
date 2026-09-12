using TargaSharp.IO;

namespace TargaSharp.Tests.IO;

/// <summary>
/// Tests for <see cref="RleCodec"/>, the TGA per-scanline Run-Length Encoding packet codec
/// extracted from <see cref="TgaFile"/>'s original loader/saver.
/// </summary>
[TestClass]
public class RleCodecTests
{
    /// <summary>
    /// Parses an RLE-encoded packet stream into (IsRun, PixelCount) tuples, one per packet, without
    /// expanding pixel data. Used to inspect packet boundaries/sizes directly, independent of
    /// <see cref="RleCodec.Decode(BinaryReader, int, int)"/>, which only reconstructs raw bytes.
    /// </summary>
    /// <param name="encoded">Encoded byte stream produced by <see cref="RleCodec.Encode"/>.</param>
    /// <param name="bytesPerPixel">Number of bytes in one pixel.</param>
    /// <returns>One (IsRun, PixelCount) tuple per packet, in stream order.</returns>
    private static List<(bool IsRun, int PixelCount)> ParsePackets(byte[] encoded, int bytesPerPixel)
    {
        var packets = new List<(bool, int)>();
        int i = 0;
        while (i < encoded.Length)
        {
            byte header = encoded[i++];
            int pixelCount = (header & 127) + 1;
            bool isRun = header >= 128;
            packets.Add((isRun, pixelCount));
            i += (isRun ? 1 : pixelCount) * bytesPerPixel;
        }
        return packets;
    }

    /// <summary>
    /// Decodes an <see cref="RleCodec.Encode"/> result back with <see cref="RleCodec.Decode"/>
    /// via a <see cref="BinaryReader"/> over the encoded bytes.
    /// </summary>
    /// <param name="encoded">Encoded bytes.</param>
    /// <param name="bytesPerPixel">Number of bytes in one pixel.</param>
    /// <param name="expectedLength">Total raw byte length expected back.</param>
    /// <returns>Decoded raw pixel bytes.</returns>
    private static byte[] DecodeAll(byte[] encoded, int bytesPerPixel, int expectedLength)
    {
        using var stream = new MemoryStream(encoded);
        using var reader = new BinaryReader(stream);
        return RleCodec.Decode(reader, bytesPerPixel, expectedLength);
    }

    [TestMethod]
    public void EncodeThenDecode_OneBytePerPixelWithRunsAndRawSpans_RoundTrips()
    {
        byte[] imageData = [1, 1, 1, 2, 3, 4, 4, 4, 4, 4];
        byte[]? encoded = RleCodec.Encode(imageData, bytesPerPixel: 1, width: imageData.Length, height: 1);

        Assert.IsNotNull(encoded);
        CollectionAssert.AreEqual(imageData, DecodeAll(encoded, 1, imageData.Length));
    }

    [TestMethod]
    public void EncodeThenDecode_TwoBytesPerPixelWithRunsAndRawSpans_RoundTrips()
    {
        byte[] imageData = [1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4];
        byte[]? encoded = RleCodec.Encode(imageData, bytesPerPixel: 2, width: imageData.Length / 2, height: 1);

        Assert.IsNotNull(encoded);
        CollectionAssert.AreEqual(imageData, DecodeAll(encoded, 2, imageData.Length));
    }

    [TestMethod]
    public void EncodeThenDecode_ThreeBytesPerPixelWithRunsAndRawSpans_RoundTrips()
    {
        byte[] imageData =
        [
            10, 10, 10, 10, 10, 10, // one BGR pixel repeated twice
            1, 2, 3, 4, 5, 6, // two distinct BGR pixels
            7, 7, 7, 7, 7, 7, 7, 7, 7, // one BGR pixel repeated three times
        ];
        byte[]? encoded = RleCodec.Encode(imageData, bytesPerPixel: 3, width: imageData.Length / 3, height: 1);

        Assert.IsNotNull(encoded);
        CollectionAssert.AreEqual(imageData, DecodeAll(encoded, 3, imageData.Length));
    }

    [TestMethod]
    public void EncodeThenDecode_FourBytesPerPixelWithRunsAndRawSpans_RoundTrips()
    {
        byte[] imageData =
        [
            1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, // one BGRA pixel repeated three times
            5, 6, 7, 8, 9, 10, 11, 12, // two distinct BGRA pixels
        ];
        byte[]? encoded = RleCodec.Encode(imageData, bytesPerPixel: 4, width: imageData.Length / 4, height: 1);

        Assert.IsNotNull(encoded);
        CollectionAssert.AreEqual(imageData, DecodeAll(encoded, 4, imageData.Length));
    }

    [TestMethod]
    public void Encode_RowOf300IdenticalPixels_SplitsIntoPacketsOfAtMost128Pixels()
    {
        const int width = 300;
        byte[] imageData = [.. Enumerable.Repeat((byte)42, width)];

        byte[]? encoded = RleCodec.Encode(imageData, bytesPerPixel: 1, width, height: 1);

        Assert.IsNotNull(encoded);
        var packets = ParsePackets(encoded, 1);

        Assert.IsTrue(packets.Count > 1, "300 identical pixels must not fit in a single packet (max 128 pixels/packet).");
        Assert.IsTrue(packets.All(p => p.PixelCount <= 128), "Every packet must cover at most 128 pixels.");
        Assert.AreEqual(width, packets.Sum(p => p.PixelCount));
        CollectionAssert.AreEqual(imageData, DecodeAll(encoded, 1, imageData.Length));
    }

    [TestMethod]
    public void Encode_TwoRowsSharingBoundaryPixelValue_NeverPacksAcrossTheScanline()
    {
        const int width = 5;
        // Row 0 is one uniform run; row 1 starts with the same pixel value as row 0 ends with,
        // so a naive flat (non-scanline-aware) encoder would merge them into a single run.
        byte[] row0 = [9, 9, 9, 9, 9];
        byte[] row1 = [9, 2, 3, 4, 5];
        byte[] imageData = [.. row0, .. row1];

        byte[]? encoded = RleCodec.Encode(imageData, bytesPerPixel: 1, width, height: 2);

        Assert.IsNotNull(encoded);
        var packets = ParsePackets(encoded, 1);

        // Walk packet pixel-count boundaries and assert one falls exactly at the start of row 1
        // (global pixel index == width): that proves some packet ends at row 0's last pixel and a
        // different packet begins at row 1's first pixel - i.e. two runs, not one run spanning both.
        int offset = 0;
        bool boundaryIsPacketStart = false;
        foreach (var packet in packets)
        {
            if (offset == width)
            {
                boundaryIsPacketStart = true;
                break;
            }
            offset += packet.PixelCount;
        }

        Assert.IsTrue(boundaryIsPacketStart, "A packet must start exactly at the row boundary; no packet may straddle it.");
        CollectionAssert.AreEqual(imageData, DecodeAll(encoded, 1, imageData.Length));
    }

    [TestMethod]
    public void Encode_NullImageData_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => RleCodec.Encode(null!, bytesPerPixel: 1, width: 2, height: 2));
    }

    [TestMethod]
    public void Encode_ZeroWidth_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RleCodec.Encode([1, 2, 3, 4], bytesPerPixel: 1, width: 0, height: 4));
    }

    [TestMethod]
    public void Encode_ZeroHeight_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RleCodec.Encode([1, 2, 3, 4], bytesPerPixel: 1, width: 4, height: 0));
    }

    [TestMethod]
    public void Encode_ImageDataLengthMismatch_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RleCodec.Encode([1, 2, 3], bytesPerPixel: 1, width: 2, height: 2));
    }

    /// <summary>
    /// Documents the inherited (pre-split) contract: with <c>bytesPerPixel = 0</c> the per-scanline
    /// size is 0, so the length check trivially passes for an empty <paramref name="imageData"/>) and
    /// every row's packet loop never runs - <see cref="RleCodec.Encode"/> returns an empty (not null)
    /// array rather than throwing.
    /// </summary>
    [TestMethod]
    public void Encode_ZeroBytesPerPixelWithEmptyImageData_ReturnsEmptyArray()
    {
        byte[]? encoded = RleCodec.Encode([], bytesPerPixel: 0, width: 2, height: 2);

        Assert.IsNotNull(encoded);
        Assert.AreEqual(0, encoded.Length);
    }

    [TestMethod]
    public void Decode_NullReader_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => RleCodec.Decode(null!, bytesPerPixel: 1, expectedLength: 4));
    }
}
