using System.Buffers.Binary;
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
        /// Shrinks an 8-bit color component to 5 bits by dropping the low bits.
        /// </summary>
        /// <param name="value">8-bit component.</param>
        /// <returns>5-bit component (0-31).</returns>
        private static int To5Bit(byte value) => value >> 3;

        /// <summary>
        /// Grows a 5-bit color component to 8 bits by bit replication, so 0 maps to 0 and 31 to 255 exactly.
        /// </summary>
        /// <param name="value">5-bit component (0-31).</param>
        /// <returns>8-bit component.</returns>
        private static int To8Bit(int value) => (value << 3) | (value >> 2);

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
                {
                    // Spec layout ARRRRRGGGGGBBBBB (R in bits 10-14, B in bits 0-4), the mirror of ReadEntry.
                    int r = To5Bit(color.R) << 10;
                    int g = To5Bit(color.G) << 5;
                    int b = To5Bit(color.B);
                    // Source alpha's top bit (bit 7) becomes bit 15 of the packed value.
                    int a = entrySize == TgaColorMapEntrySize.A1R5G5B5 ? (color.A & 0x80) << 8 : 0;

                    // Explicitly little-endian like TgaBinary in the core, not host-endian BitConverter.
                    BinaryPrimitives.WriteUInt16LittleEndian(entry, (ushort)(a | r | g | b));
                    return entry;
                }

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
        /// <param name="colorMapData">Raw color map bytes (see <see cref="TgaImageArea.ColorMapData"/>).</param>
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
                        ushort packed = BinaryPrimitives.ReadUInt16LittleEndian(colorMapData.AsSpan(index * 2));
                        // X1R5G5B5 has no alpha: bit 15 is padding (WriteEntry always clears it) and must be ignored.
                        bool hasAlphaBit = entrySize == TgaColorMapEntrySize.A1R5G5B5 && useAlpha;
                        int a = hasAlphaBit ? ((packed & 0x8000) >> 15) * 255 : 255;
                        int r = To8Bit((packed & 0x7C00) >> 10);
                        int g = To8Bit((packed & 0x3E0) >> 5);
                        int b = To8Bit(packed & 0x1F);
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
                        int argb = BinaryPrimitives.ReadInt32LittleEndian(colorMapData.AsSpan(index * 4));
                        // Same rule as the 16-bit branch: keep the stored alpha only when it is meaningful.
                        return Color.FromArgb(useAlpha ? argb : argb | (0xFF << 24));
                    }

                default:
                    return null;
            }
        }
    }
}
