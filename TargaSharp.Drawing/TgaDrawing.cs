using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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
        /// <param name="bmp">Input Bitmap. Supported <see cref="PixelFormat"/>s: 8bpp indexed, 16bpp grayscale/RGB555/ARGB1555,
        /// 24bpp RGB and 32bpp RGB/ARGB/PARGB.</param>
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

            switch (bmp.PixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    break;

                // 1/4bpp pack several pixels per byte and 48/64bpp use 16-bit channels; neither maps onto a
                // TGA pixel depth (8/16/24/32) and the byte-per-pixel copy below assumes >= 8bpp.
                default:
                    throw new NotSupportedException($"{nameof(PixelFormat)} {bmp.PixelFormat} is not supported.");
            }

            var tga = new TgaFile();
            tga.Header.ImageSpec.ImageWidth = (ushort)bmp.Width;
            tga.Header.ImageSpec.ImageHeight = (ushort)bmp.Height;
            tga.Header.ImageSpec.ImageDescriptor.ImageOrigin = TgaImageOrigin.TopLeft;

            int bytesPP = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            bool isAlpha = Image.IsAlphaPixelFormat(bmp.PixelFormat);
            bool isPreAlpha = bmp.PixelFormat == PixelFormat.Format32bppPArgb;
            // Without an extension area the pre-multiplied flag is lost and ToBitmap would read the bytes as straight alpha.
            if (isPreAlpha && !newFormat)
                throw new NotSupportedException($"{nameof(PixelFormat.Format32bppPArgb)} requires {nameof(newFormat)} = true so the extension area can record {nameof(TgaAttributeType.PreMultipliedAlpha)}.");
            bool isIndexed = bmp.PixelFormat == PixelFormat.Format8bppIndexed;

            tga.Header.ImageSpec.PixelDepth = (TgaPixelDepth)(bytesPP * 8);
            tga.Header.ImageSpec.ImageDescriptor.AlphaChannelBits = bmp.PixelFormat switch
            {
                PixelFormat.Format16bppArgb1555 => 1,
                PixelFormat.Format32bppArgb or PixelFormat.Format32bppPArgb => 8,
                _ => 0,
            };

            // An identity gray ramp palette is the TGA black-and-white image type; every other palette needs a color map.
            bool isGrayImage = bmp.PixelFormat == PixelFormat.Format16bppGrayScale || (isIndexed && IsIdentityGrayPalette(bmp.Palette.Entries));
            bool isColorMapped = isIndexed && !isGrayImage;
            bool colorMapUseAlpha = isColorMapped && WriteColorMap(tga, bmp.Palette.Entries, colorMap2BytesEntry);

            tga.Header.ImageType = (useRle, isGrayImage, isColorMapped) switch
            {
                (true, true, _) => TgaImageType.RleGrayscale,
                (true, false, true) => TgaImageType.RleColorMapped,
                (true, false, false) => TgaImageType.RleTrueColor,
                (false, true, _) => TgaImageType.UncompressedGrayscale,
                (false, false, true) => TgaImageType.UncompressedColorMapped,
                (false, false, false) => TgaImageType.UncompressedTrueColor,
            };
            tga.Header.ColorMapType = isColorMapped ? TgaColorMapType.ColorMap : TgaColorMapType.NoColorMap;

            if (newFormat)
            {
                // ToNewFormat() creates Footer + a default ExtensionArea; override AttributesType with the fuller
                // pre-multiplied / palette-alpha semantics ToNewFormat() doesn't know about.
                tga.ToNewFormat();
                tga.ExtensionArea!.AttributesType = (isAlpha, isPreAlpha, colorMapUseAlpha) switch
                {
                    (true, true, _) => TgaAttributeType.PreMultipliedAlpha,
                    (true, false, _) or (false, _, true) => TgaAttributeType.UsefulAlpha,
                    _ => TgaAttributeType.NoAlpha,
                };
            }

            tga.ImageArea.ImageData = ReadRows(bmp, bytesPP);

            // Not officially supported by GDI+, but round-trips (tested on 16bpp grayscale images).
            if (bmp.PixelFormat == PixelFormat.Format16bppGrayScale)
            {
                for (int i = 0; i < tga.ImageArea.ImageData.Length; i++)
                    tga.ImageArea.ImageData[i] ^= byte.MaxValue;
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
        /// alpha-carrying entry size when any entry is meaningfully translucent.
        /// </summary>
        /// <param name="tga">File to populate.</param>
        /// <param name="colors">Palette entries.</param>
        /// <param name="twoByteEntries">Use 15/16-bit entries instead of 24/32-bit.</param>
        /// <returns>Whether the written entries carry alpha.</returns>
        private static bool WriteColorMap(TgaFile tga, Color[] colors, bool twoByteEntries)
        {
            int alphaSum = 0;
            bool useAlpha = false;
            for (int i = 0; i < colors.Length; i++)
            {
                useAlpha |= colors[i].A < 248;
                alphaSum |= colors[i].A;
            }
            useAlpha &= alphaSum > 0;

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

        /// <summary>
        /// Reads the bitmap's pixels into an unpadded row-major buffer, honoring GDI+'s own stride
        /// (padding, or a negative stride for bottom-up surfaces) instead of assuming it.
        /// </summary>
        /// <param name="bmp">Source bitmap.</param>
        /// <param name="bytesPerPixel">Bytes per pixel of its format.</param>
        /// <returns>Width * Height * <paramref name="bytesPerPixel"/> bytes.</returns>
        private static byte[] ReadRows(Bitmap bmp, int bytesPerPixel)
        {
            int strideBytes = bmp.Width * bytesPerPixel;
            byte[] pixels = new byte[strideBytes * bmp.Height];

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            try
            {
                for (int y = 0; y < bmp.Height; y++)
                    Marshal.Copy(bmpData.Scan0 + (nint)y * bmpData.Stride, pixels, y * strideBytes, strideBytes);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            return pixels;
        }
    }
}
