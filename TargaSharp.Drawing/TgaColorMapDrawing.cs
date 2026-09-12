using System.Drawing;

namespace TargaSharp.Drawing
{
    /// <summary>
    /// Houses the two mirrored byte&lt;-&gt;<see cref="Color"/> conversions shared by <see cref="TgaDrawing.FromBitmap(Bitmap, bool, bool, bool)"/>
    /// (palette -&gt; TGA color map bytes) and <see cref="TgaFileDrawingExtensions"/> (TGA color map bytes -&gt; palette),
    /// so the packed-pixel byte layouts for each <see cref="TgaColorMapEntrySize"/> are defined in exactly one place.
    /// </summary>
    internal static class TgaColorMapDrawing
    {
        /// <summary>
        /// Scale factor used to shrink an 8 bit color component down to 5 bits.
        /// </summary>
        private const float To5Bit = 32f / 256f;

        /// <summary>
        /// Scale factor used to grow a 5 bit color component up to 8 bits.
        /// </summary>
        private const float To8Bit = 255f / 31f;

        /// <summary>
        /// Packs <paramref name="color"/> into the byte layout for one TGA color map entry.
        /// </summary>
        /// <param name="entrySize">Target packed layout (bits per entry).</param>
        /// <param name="color">Source color to pack.</param>
        /// <param name="bytesPerEntry">Number of bytes in one color map entry (see <see cref="TgaColorMapEntrySize"/>).</param>
        /// <returns>Byte array of length <paramref name="bytesPerEntry"/> holding the packed entry. All zero bytes for <see cref="TgaColorMapEntrySize.Other"/>.</returns>
        internal static byte[] WriteEntry(TgaColorMapEntrySize entrySize, Color color, int bytesPerEntry)
        {
            byte[] entry = new byte[bytesPerEntry];

            switch (entrySize)
            {
                case TgaColorMapEntrySize.A1R5G5B5:
                case TgaColorMapEntrySize.X1R5G5B5:
                    int r = (int)(color.R * To5Bit);
                    int g = (int)(color.G * To5Bit) << 5;
                    int b = (int)(color.B * To5Bit) << 10;
                    int a = 0;

                    if (entrySize == TgaColorMapEntrySize.A1R5G5B5)
                        // Move the source alpha's top bit (bit 7) into bit 15 of the packed
                        // A1R5G5B5 value, mirroring ReadEntry's "(packed & 0x8000) >> 15".
                        a = (color.A & 0x80) << 8;

                    return BitConverter.GetBytes(a | r | g | b);

                case TgaColorMapEntrySize.R8G8B8:
                    entry[0] = color.B;
                    entry[1] = color.G;
                    entry[2] = color.R;
                    return entry;

                case TgaColorMapEntrySize.A8R8G8B8:
                    entry[0] = color.B;
                    entry[1] = color.G;
                    entry[2] = color.R;
                    entry[3] = color.A;
                    return entry;

                case TgaColorMapEntrySize.Other:
                default:
                    return entry;
            }
        }

        /// <summary>
        /// Unpacks the color map entry at <paramref name="index"/> of <paramref name="colorMapData"/> into a <see cref="Color"/>.
        /// </summary>
        /// <param name="entrySize">Packed layout of each entry (bits per entry).</param>
        /// <param name="colorMapData">Raw color map bytes (see <see cref="TgaImgOrColMap.ColorMapData"/>).</param>
        /// <param name="index">Zero-based entry index.</param>
        /// <param name="useAlpha">Whether the unpacked color should carry a meaningful alpha channel.</param>
        /// <returns>The unpacked <see cref="Color"/>, or <see langword="null"/> for an unsupported <paramref name="entrySize"/>.</returns>
        internal static Color? ReadEntry(TgaColorMapEntrySize entrySize, byte[] colorMapData, int index, bool useAlpha)
        {
            switch (entrySize)
            {
                case TgaColorMapEntrySize.X1R5G5B5:
                case TgaColorMapEntrySize.A1R5G5B5:
                    {
                        ushort packed = BitConverter.ToUInt16(colorMapData, index * 2);
                        int a = (useAlpha ? (packed & 0x8000) >> 15 : 1) * 255; // (0 or 1) * 255
                        int r = (int)(((packed & 0x7C00) >> 10) * To8Bit);
                        int g = (int)(((packed & 0x3E0) >> 5) * To8Bit);
                        int b = (int)((packed & 0x1F) * To8Bit);
                        return Color.FromArgb(a, r, g, b);
                    }

                case TgaColorMapEntrySize.R8G8B8:
                    {
                        int offset = index * 3; // RGB = 3 bytes
                        int r = colorMapData[offset + 2];
                        int g = colorMapData[offset + 1];
                        int b = colorMapData[offset];
                        return Color.FromArgb(r, g, b);
                    }

                case TgaColorMapEntrySize.A8R8G8B8:
                    {
                        int argb = BitConverter.ToInt32(colorMapData, index * 4);
                        return Color.FromArgb(useAlpha ? argb | (0xFF << 24) : argb);
                    }

                default:
                    return null;
            }
        }
    }
}
