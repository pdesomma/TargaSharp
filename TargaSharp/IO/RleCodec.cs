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

            int scanLineSize = width * bytesPerPixel;
            if (scanLineSize * height != imageData.Length)
                throw new ArgumentOutOfRangeException(nameof(imageData), imageData.Length, $"Length must be {scanLineSize * height} ({width} x {height} x {bytesPerPixel}).");

            // Worst case (no runs at all) is one header byte per 128 pixels on top of the raw data.
            var encoded = new List<byte>(imageData.Length + (width + MaxPacketPixels - 1) / MaxPacketPixels * height);

            for (int y = 0; y < height; y++)
                EncodeScanline(imageData.AsSpan(y * scanLineSize, scanLineSize), bytesPerPixel, width, encoded);

            return [.. encoded];
        }

        /// <summary>
        /// Encodes one scanline. Runs of 2+ identical pixels become run-length packets; everything else is
        /// gathered into raw packets that end where the next run begins. Packets never exceed
        /// <see cref="MaxPacketPixels"/> pixels and never cross into the next scanline.
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
                if (run >= 2)
                {
                    encoded.Add((byte)(RunLengthFlag | (run - 1)));
                    encoded.AddRange(row.Slice(pos * bytesPerPixel, bytesPerPixel).ToArray());
                    pos += run;
                    continue;
                }

                // Raw span: advance until a run of 2+ starts, the packet fills, or the row ends.
                int start = pos;
                pos++;
                while (pos < width && pos - start < MaxPacketPixels && RunLength(row, bytesPerPixel, width, pos) < 2)
                    pos++;

                int count = pos - start;
                encoded.Add((byte)(count - 1));
                encoded.AddRange(row.Slice(start * bytesPerPixel, count * bytesPerPixel).ToArray());
            }
        }

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
        /// <exception cref="EndOfStreamException">The stream ends inside a packet.</exception>
        /// <exception cref="TgaFormatException">A packet would write past <paramref name="expectedLength"/>.</exception>
        internal static byte[] Decode(BinaryReader reader, int bytesPerPixel, int expectedLength)
        {
            ArgumentNullException.ThrowIfNull(reader);

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
                    byte[] rlePart = ReadPacketBytes(reader, bytesPerPixel);
                    for (int i = 0; i < chunk.Length; i++)
                        chunk[i] = rlePart[i % bytesPerPixel];
                }
                else // RAW format
                    chunk = ReadPacketBytes(reader, packetCount * bytesPerPixel);

                if (dataOffset + chunk.Length > result.Length)
                    throw new TgaFormatException($"RLE packet of {packetCount} pixels at decoded offset {dataOffset} overruns the {expectedLength}-byte image data.");

                Buffer.BlockCopy(chunk, 0, result, dataOffset, chunk.Length);
                dataOffset += chunk.Length;
            }

            return result;
        }

        /// <summary>
        /// Reads the payload of one packet, failing loudly when the stream ends inside it. A short
        /// <see cref="BinaryReader.ReadBytes"/> used to surface as <see cref="IndexOutOfRangeException"/>
        /// (run packet) or a confusing error from a later read (raw packet).
        /// </summary>
        /// <param name="reader">Packet stream.</param>
        /// <param name="count">Bytes the packet must supply.</param>
        /// <returns>Exactly <paramref name="count"/> bytes.</returns>
        /// <exception cref="EndOfStreamException">Fewer than <paramref name="count"/> bytes remain.</exception>
        private static byte[] ReadPacketBytes(BinaryReader reader, int count)
        {
            byte[] bytes = reader.ReadBytes(count);
            if (bytes.Length != count)
                throw new EndOfStreamException($"RLE packet truncated: expected {count} bytes, got {bytes.Length}.");
            return bytes;
        }
    }
}
