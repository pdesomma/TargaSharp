namespace TargaSharp.IO
{
    /// <summary>
    /// Encodes and decodes TGA's per-scanline Run-Length Encoding packet format.
    /// <para>Each packet starts with one header byte: bit 7 selects Run-Length (1) vs Raw (0)
    /// encoding, and bits 0-6 are (pixel count - 1). A Run-Length packet is followed by exactly
    /// one pixel (<c>bytesPerPixel</c> bytes) that is repeated <c>count</c> times; a Raw packet is
    /// followed by <c>count</c> distinct pixels. A packet never spans more than 128 pixels and
    /// never crosses a scanline boundary.</para>
    /// </summary>
    internal static class RleCodec
    {
        /// <summary>
        /// Maximum pixels one packet can carry (7-bit count + 1).
        /// </summary>
        internal const int MaxPacketPixels = 128;

        /// <summary>
        /// Packet header bit 7: set for a run-length packet, clear for a raw packet.
        /// </summary>
        private const int RunLengthFlag = 0x80;

        /// <summary>
        /// Encodes raw pixel data using TGA's per-scanline Run-Length Encoding.
        /// </summary>
        /// <param name="imageData">Image data, bytes array with size = <paramref name="width"/> * <paramref name="height"/> * <paramref name="bytesPerPixel"/>.</param>
        /// <param name="bytesPerPixel">Number of bytes in one pixel.</param>
        /// <param name="width">Image Width, must be > 0.</param>
        /// <param name="height">Image Height, must be > 0.</param>
        /// <returns>Bytes array with RLE compressed image data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="imageData"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is &lt;= 0,
        /// or <paramref name="imageData"/>'s length is not exactly <paramref name="width"/> * <paramref name="height"/> * <paramref name="bytesPerPixel"/>.</exception>
        internal static byte[] Encode(byte[] imageData, int bytesPerPixel, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(imageData);
            if (bytesPerPixel <= 0)
                throw new ArgumentOutOfRangeException(nameof(bytesPerPixel), bytesPerPixel, "Must be > 0.");
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Must be > 0.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Must be > 0.");

            // width * height * bpp overflows int (32768 x 32768 x 4 wraps to 0), so size in long.
            long expectedLength = (long)width * height * bytesPerPixel;
            if (expectedLength != imageData.Length)
                throw new ArgumentOutOfRangeException(nameof(imageData), imageData.Length, $"Length must be {expectedLength} ({width} x {height} x {bytesPerPixel}).");
            int scanLineSize = width * bytesPerPixel;

            // Worst case (no runs at all) is one header byte per 128 pixels on top of the raw data.
            var encoded = new List<byte>(imageData.Length + (width + MaxPacketPixels - 1) / MaxPacketPixels * height);

            for (int y = 0; y < height; y++)
                EncodeScanline(imageData.AsSpan(y * scanLineSize, scanLineSize), bytesPerPixel, width, encoded);

            return [.. encoded];
        }

        /// <summary>
        /// Encodes one scanline. A run becomes a run-length packet only when that is no larger than
        /// carrying the same pixels raw: a run packet costs 1 + bpp bytes, and splitting a raw packet
        /// to insert one costs a further header byte. Everything else is gathered into raw packets.
        /// Packets never exceed <see cref="MaxPacketPixels"/> pixels and never cross into the next scanline.
        /// </summary>
        /// <param name="row">Exactly one scanline of pixels.</param>
        /// <param name="bytesPerPixel">Bytes per pixel.</param>
        /// <param name="width">Pixels in the row.</param>
        /// <param name="encoded">Output sink.</param>
        private static void EncodeScanline(ReadOnlySpan<byte> row, int bytesPerPixel, int width, List<byte> encoded)
        {
            int pos = 0;
            while (pos < width)
            {
                int run = RunLength(row, bytesPerPixel, width, pos);
                if (RunPacketIsSmaller(run, bytesPerPixel, splitsRawPacket: false))
                {
                    encoded.Add((byte)(RunLengthFlag | (run - 1)));
                    encoded.AddRange(row.Slice(pos * bytesPerPixel, bytesPerPixel).ToArray());
                    pos += run;
                    continue;
                }

                // Raw span: advance until a run worth a packet of its own starts, the packet fills, or the row ends.
                int start = pos;
                pos++;
                while (pos < width && pos - start < MaxPacketPixels)
                {
                    int next = RunLength(row, bytesPerPixel, width, pos);
                    // A run that ends the row closes this raw packet for free; one mid-row costs another raw header after it.
                    if (RunPacketIsSmaller(next, bytesPerPixel, splitsRawPacket: pos + next < width))
                        break;
                    pos++;
                }

                int count = pos - start;
                encoded.Add((byte)(count - 1));
                encoded.AddRange(row.Slice(start * bytesPerPixel, count * bytesPerPixel).ToArray());
            }
        }

        /// <summary>
        /// Decides whether <paramref name="run"/> identical pixels are cheaper as a run packet (1 + bpp bytes)
        /// than carried raw (run * bpp bytes, plus one more raw header when the run splits an open raw packet).
        /// At 1 bpp a 2-pixel run only breaks even, so it stays raw; at 3+ bpp every run of 2 wins.
        /// </summary>
        /// <param name="run">Run length in pixels.</param>
        /// <param name="bytesPerPixel">Bytes per pixel.</param>
        /// <param name="splitsRawPacket">Whether raw pixels follow the run inside the same row.</param>
        /// <returns><see langword="true"/> when a run packet is strictly smaller.</returns>
        private static bool RunPacketIsSmaller(int run, int bytesPerPixel, bool splitsRawPacket) =>
            run * bytesPerPixel > bytesPerPixel + 1 + (splitsRawPacket ? 1 : 0);

        /// <summary>
        /// Counts how many consecutive pixels from <paramref name="pos"/> equal the pixel at <paramref name="pos"/>, capped at <see cref="MaxPacketPixels"/>.
        /// </summary>
        /// <param name="row">One scanline.</param>
        /// <param name="bytesPerPixel">Bytes per pixel.</param>
        /// <param name="width">Pixels in the row.</param>
        /// <param name="pos">Pixel index to start from.</param>
        /// <returns>Run length, at least 1.</returns>
        private static int RunLength(ReadOnlySpan<byte> row, int bytesPerPixel, int width, int pos)
        {
            ReadOnlySpan<byte> first = row.Slice(pos * bytesPerPixel, bytesPerPixel);
            int run = 1;
            while (pos + run < width && run < MaxPacketPixels && row.Slice((pos + run) * bytesPerPixel, bytesPerPixel).SequenceEqual(first))
                run++;
            return run;
        }

        /// <summary>
        /// Decodes TGA's per-scanline Run-Length Encoded packets back into raw pixel data, reading
        /// packets until exactly <paramref name="expectedLength"/> bytes have been produced. An
        /// <paramref name="expectedLength"/> of 0 consumes nothing and returns an empty array.
        /// </summary>
        /// <param name="reader">Reader positioned at the start of the RLE packet stream.</param>
        /// <param name="bytesPerPixel">Number of bytes in one pixel.</param>
        /// <param name="expectedLength">Total number of raw bytes the decoded image data must contain
        /// (<c>width * height * bytesPerPixel</c>).</param>
        /// <returns>Bytes array of exactly <paramref name="expectedLength"/> decoded bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bytesPerPixel"/> is &lt;= 0 or <paramref name="expectedLength"/> is &lt; 0.</exception>
        /// <exception cref="EndOfStreamException">The stream ends inside a packet.</exception>
        /// <exception cref="TgaFormatException">A packet would write past <paramref name="expectedLength"/>.</exception>
        internal static byte[] Decode(BinaryReader reader, int bytesPerPixel, int expectedLength)
        {
            ArgumentNullException.ThrowIfNull(reader);
            // A 0-byte pixel would never advance the output and read the stream to its end.
            if (bytesPerPixel <= 0)
                throw new ArgumentOutOfRangeException(nameof(bytesPerPixel), bytesPerPixel, "Must be > 0.");
            if (expectedLength < 0)
                throw new ArgumentOutOfRangeException(nameof(expectedLength), expectedLength, "Must be >= 0.");

            // A packet costs at least 1 + bytesPerPixel bytes and yields at most MaxPacketPixels pixels, so the
            // stream can never decode to more than MaxPacketPixels times its remaining length. Checking that
            // before allocating stops a small hostile file from declaring a multi-GB image.
            if (reader.BaseStream.CanSeek)
            {
                long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
                if (expectedLength > remaining * MaxPacketPixels)
                    throw new EndOfStreamException($"RLE image data declares {expectedLength} decoded bytes but only {Math.Max(remaining, 0)} encoded bytes remain in the stream.");
            }

            byte[] result = new byte[expectedLength];
            int dataOffset = 0;

            while (dataOffset < expectedLength)
            {
                byte packetInfo = reader.ReadByte(); //1 type bit and 7 count bits. Len = Count + 1.
                int packetCount = (packetInfo & (MaxPacketPixels - 1)) + 1;
                byte[] chunk;

                if ((packetInfo & RunLengthFlag) != 0)
                {
                    chunk = new byte[packetCount * bytesPerPixel];
                    byte[] rlePart = reader.ReadExactly(bytesPerPixel, "RLE run packet");
                    for (int i = 0; i < chunk.Length; i++)
                        chunk[i] = rlePart[i % bytesPerPixel];
                }
                else // RAW format
                    chunk = reader.ReadExactly(packetCount * bytesPerPixel, "RLE raw packet");

                if (dataOffset + chunk.Length > result.Length)
                    throw new TgaFormatException($"RLE packet of {packetCount} pixels at decoded offset {dataOffset} overruns the {expectedLength}-byte image data.");

                Buffer.BlockCopy(chunk, 0, result, dataOffset, chunk.Length);
                dataOffset += chunk.Length;
            }

            return result;
        }
    }
}
