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
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width) + " and " + nameof(height) + " must be > 0!");

            int scanLineSize = width * bytesPerPixel;

            if (scanLineSize * height != imageData.Length)
                throw new ArgumentOutOfRangeException("ImageData has wrong Length!");

            int count = 0;
            int pos = 0;
            bool isRle = false;
            List<byte> encoded = new List<byte>();
            byte[] rowData = new byte[scanLineSize];

            for (int y = 0; y < height; y++)
            {
                pos = 0;
                Buffer.BlockCopy(imageData, y * scanLineSize, rowData, 0, scanLineSize);

                while (pos < scanLineSize)
                {
                    if (pos >= scanLineSize - bytesPerPixel)
                    {
                        encoded.Add(0);
                        encoded.AddRange(BitConverterHelper.GetElements(rowData, pos, bytesPerPixel));
                        pos += bytesPerPixel;
                        break;
                    }

                    count = 0; //1
                    isRle = BitConverterHelper.IsElementsEqual(rowData, pos, pos + bytesPerPixel, bytesPerPixel);

                    for (int i = pos + bytesPerPixel; i < Math.Min(pos + 128 * bytesPerPixel, scanLineSize) - bytesPerPixel; i += bytesPerPixel)
                    {
                        if (isRle ^ BitConverterHelper.IsElementsEqual(rowData, (isRle ? pos : i), i + bytesPerPixel, bytesPerPixel))
                        {
                            //count--;
                            break;
                        }
                        else
                            count++;
                    }

                    int countBpp = (count + 1) * bytesPerPixel;
                    encoded.Add((byte)(isRle ? count | 128 : count));
                    encoded.AddRange(BitConverterHelper.GetElements(rowData, pos, (isRle ? bytesPerPixel : countBpp)));
                    pos += countBpp;
                }
            }

            return encoded.ToArray();
        }

        /// <summary>
        /// Decodes TGA's per-scanline Run-Length Encoded packets back into raw pixel data.
        /// <para>Mirrors the loop <see cref="TgaFile"/>'s loader used before the split: it reads packets
        /// until <paramref name="expectedLength"/> bytes have been produced, via a do/while loop that
        /// always reads at least one packet - so a caller must not invoke this with
        /// <paramref name="expectedLength"/> &lt;= 0 unless the underlying reader still has a packet to
        /// consume, or it will throw trying to read past the intended data.</para>
        /// </summary>
        /// <param name="reader">Reader positioned at the start of the RLE packet stream.</param>
        /// <param name="bytesPerPixel">Number of bytes in one pixel.</param>
        /// <param name="expectedLength">Total number of raw bytes the decoded image data must contain
        /// (<c>width * height * bytesPerPixel</c>).</param>
        /// <returns>Bytes array of exactly <paramref name="expectedLength"/> decoded bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        internal static byte[] Decode(BinaryReader reader, int bytesPerPixel, int expectedLength)
        {
            ArgumentNullException.ThrowIfNull(reader);

            byte[] result = new byte[expectedLength];
            int dataOffset = 0;

            do
            {
                byte packetInfo = reader.ReadByte(); //1 type bit and 7 count bits. Len = Count + 1.
                int packetCount = (packetInfo & 127) + 1;
                byte[] chunk;

                if (packetInfo >= 128) // bit7 = 1, RLE
                {
                    chunk = new byte[packetCount * bytesPerPixel];
                    byte[] rlePart = reader.ReadBytes(bytesPerPixel);
                    for (int i = 0; i < chunk.Length; i++)
                        chunk[i] = rlePart[i % bytesPerPixel];
                }
                else // RAW format
                    chunk = reader.ReadBytes(packetCount * bytesPerPixel);

                Buffer.BlockCopy(chunk, 0, result, dataOffset, chunk.Length);
                dataOffset += chunk.Length;
            }
            while (dataOffset < expectedLength);

            return result;
        }
    }
}
