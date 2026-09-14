using System.Drawing;
using System.Drawing.Imaging;

namespace TargaSharp.Drawing
{
    /// <summary>
    /// Builds <see cref="TgaFile"/> instances from GDI+ <see cref="Bitmap"/> images.
    /// </summary>
    public static class TgaDrawing
    {
        /// <summary>
        /// Makes a new <see cref="TgaFile"/> from a <see cref="Bitmap"/>. An 8bpp indexed bitmap whose
        /// palette is the identity gray ramp becomes a black-and-white image without a color map; any
        /// other 8bpp palette is written as a color-mapped image.
        /// </summary>
        /// <param name="bmp">Input Bitmap. Supported <see cref="PixelFormat"/>s are those in <see cref="TgaPixelFormatMap"/>:
        /// 8bpp indexed, 16bpp grayscale/RGB555/ARGB1555, 24bpp RGB and 32bpp RGB/ARGB/PARGB.</param>
        /// <param name="useRle">Use RLE Compression?</param>
        /// <param name="newFormat">Use new 2.0 TGA XFile format?</param>
        /// <param name="colorMap2BytesEntry">Is Color Map Entry size equal 15 or 16 Bpp, else - 24 or 32.</param>
        /// <returns>New <see cref="TgaFile"/> built from <paramref name="bmp"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bmp"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bmp"/> is wider or taller than <see cref="ushort.MaxValue"/> pixels.</exception>
        /// <exception cref="NotSupportedException"><paramref name="bmp"/>'s <see cref="PixelFormat"/> is not supported, or it is
        /// <see cref="PixelFormat.Format32bppPArgb"/> with <paramref name="newFormat"/> <see langword="false"/> (only the
        /// extension area's <see cref="TgaExtensionArea.AttributesType"/> can record that the alpha is pre-multiplied).</exception>
        public static TgaFile FromBitmap(Bitmap bmp, bool useRle = false, bool newFormat = true, bool colorMap2BytesEntry = false)
        {
            ArgumentNullException.ThrowIfNull(bmp);
            // The header's dimensions are ushort; a silent cast produced a file whose pixel data disagreed with its header.
            if (bmp.Width > ushort.MaxValue || bmp.Height > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(bmp), $"{bmp.Width}x{bmp.Height} exceeds the {ushort.MaxValue}x{ushort.MaxValue} TGA maximum.");
            if (!TgaPixelFormatMap.TryGet(bmp.PixelFormat, out TgaPixelFormatMapping mapping))
                throw new NotSupportedException($"{nameof(PixelFormat)} {bmp.PixelFormat} is not supported.");
            // Without an extension area the pre-multiplied flag is lost and ToBitmap would read the bytes as straight alpha.
            if (mapping.PreMultiplied && !newFormat)
                throw new NotSupportedException($"{nameof(PixelFormat.Format32bppPArgb)} requires {nameof(newFormat)} = true so the extension area can record {nameof(TgaAttributeType.PreMultipliedAlpha)}.");

            bool isIndexed = bmp.PixelFormat == PixelFormat.Format8bppIndexed;
            // An identity gray ramp palette is the TGA black-and-white image type; every other palette needs a color map.
            bool isGrayImage = mapping.Grayscale || (isIndexed && IsIdentityGrayPalette(bmp.Palette.Entries));
            bool isColorMapped = isIndexed && !isGrayImage;

            TgaImageType imageType = (useRle, isGrayImage, isColorMapped) switch
            {
                (true, true, _) => TgaImageType.RleGrayscale,
                (true, false, true) => TgaImageType.RleColorMapped,
                (true, false, false) => TgaImageType.RleTrueColor,
                (false, true, _) => TgaImageType.UncompressedGrayscale,
                (false, false, true) => TgaImageType.UncompressedColorMapped,
                (false, false, false) => TgaImageType.UncompressedTrueColor,
            };

            // The sizing ctor owns header consistency (ColorMapType, FirstEntryIndex, alpha bits, footer / extension area).
            var tga = new TgaFile((ushort)bmp.Width, (ushort)bmp.Height, mapping.Depth, imageType, mapping.AlphaBits, newFormat);
            tga.Header.ImageSpec.ImageDescriptor.ImageOrigin = TgaImageOrigin.TopLeft;
            tga.ImageArea.ImageData = TgaBitmapRows.Read(bmp);

            bool colorMapUseAlpha = isColorMapped && WriteColorMap(tga, bmp.Palette.Entries, colorMap2BytesEntry);
            if (tga.ExtensionArea is not null)
            {
                // The ctor only knows the descriptor's alpha bits; pre-multiplied and palette alpha need the fuller semantics.
                tga.ExtensionArea.AttributesType = (mapping.HasAlpha, mapping.PreMultiplied, colorMapUseAlpha) switch
                {
                    (true, true, _) => TgaAttributeType.PreMultipliedAlpha,
                    (true, false, _) or (false, _, true) => TgaAttributeType.UsefulAlpha,
                    _ => TgaAttributeType.NoAlpha,
                };
            }

            return tga;
        }

        /// <summary>
        /// Determines whether <paramref name="colors"/> is exactly the 256-entry opaque gray ramp (entry i == (i, i, i)),
        /// i.e. a palette that carries no information beyond the pixel index itself.
        /// </summary>
        /// <param name="colors">Palette entries.</param>
        /// <returns><see langword="true"/> for the identity gray ramp.</returns>
        private static bool IsIdentityGrayPalette(Color[] colors)
        {
            if (colors.Length != 256) return false;
            for (int i = 0; i < colors.Length; i++)
                if (colors[i].A != byte.MaxValue || colors[i].R != i || colors[i].G != i || colors[i].B != i)
                    return false;
            return true;
        }

        /// <summary>
        /// Writes <paramref name="colors"/> into <paramref name="tga"/>'s color map spec and data, choosing an
        /// alpha-carrying entry size when any entry is not fully opaque.
        /// </summary>
        /// <param name="tga">File to populate.</param>
        /// <param name="colors">Palette entries.</param>
        /// <param name="twoByteEntries">Use 15/16-bit entries instead of 24/32-bit.</param>
        /// <returns>Whether the written entries carry alpha.</returns>
        private static bool WriteColorMap(TgaFile tga, Color[] colors, bool twoByteEntries)
        {
            // Any non-opaque entry (even all-transparent or barely translucent) is information the palette must keep.
            bool useAlpha = colors.Any(c => c.A != byte.MaxValue);

            var entrySize = (twoByteEntries, useAlpha) switch
            {
                (true, false) => TgaColorMapEntrySize.X1R5G5B5,
                (true, true) => TgaColorMapEntrySize.A1R5G5B5,
                (false, false) => TgaColorMapEntrySize.R8G8B8,
                (false, true) => TgaColorMapEntrySize.A8R8G8B8,
            };
            int bytesPerEntry = entrySize.BytesPerPixel();

            tga.Header.ColorMapSpec.ColorMapLength = (ushort)Math.Min(colors.Length, ushort.MaxValue);
            tga.Header.ColorMapSpec.ColorMapEntrySize = entrySize;
            byte[] data = new byte[tga.Header.ColorMapSpec.ColorMapLength * bytesPerEntry];
            for (int i = 0; i < tga.Header.ColorMapSpec.ColorMapLength; i++)
                Buffer.BlockCopy(TgaColorMapDrawing.WriteEntry(entrySize, colors[i], bytesPerEntry), 0, data, i * bytesPerEntry, bytesPerEntry);
            tga.ImageArea.ColorMapData = data;

            return useAlpha;
        }
    }
}
