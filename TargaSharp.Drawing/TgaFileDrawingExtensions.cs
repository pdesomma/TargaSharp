using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TargaSharp.Drawing
{
    /// <summary>
    /// GDI+ <see cref="Bitmap"/> bridge for <see cref="TgaFile"/>.
    /// </summary>
    public static class TgaFileDrawingExtensions
    {
        /// <summary>
        /// Converts <paramref name="tga"/> to a <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="tga">Source <see cref="TgaFile"/>.</param>
        /// <param name="forceUseAlpha">Force use alpha channel.</param>
        /// <returns>The image as a <see cref="Bitmap"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tga"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="tga"/> has no image: its
        /// <see cref="TgaHeader.ImageType"/> is <see cref="TgaImageType.NoImageData"/>, its width or height
        /// is 0, or <see cref="TgaImageArea.ImageData"/> is <see langword="null"/> or not Width * Height * bytes-per-pixel long.</exception>
        /// <exception cref="NotSupportedException">The pixel depth or color map entry size has no GDI+ equivalent.</exception>
        public static Bitmap ToBitmap(this TgaFile tga, bool forceUseAlpha = false)
        {
            ArgumentNullException.ThrowIfNull(tga);

            // GDI+ would otherwise fail with an opaque "Parameter is not valid" from new Bitmap(0, 0, ...),
            // or the copy below would NRE on a null ImageData.
            if (tga.Header.ImageType == TgaImageType.NoImageData)
                throw new InvalidOperationException($"{nameof(TgaFile)} has {nameof(TgaImageType.NoImageData)}; there is no image to convert.");
            if (tga.Width == 0 || tga.Height == 0)
                throw new InvalidOperationException($"{nameof(TgaFile)} is {tga.Width}x{tga.Height}; a Bitmap needs both dimensions > 0.");
            if (tga.ImageArea.ImageData is null)
                throw new InvalidOperationException($"{nameof(TgaFile)}.{nameof(TgaFile.ImageArea)}.{nameof(TgaImageArea.ImageData)} is null; there is no image to convert.");

            return ToBitmapCore(tga, forceUseAlpha, tga.Width, tga.Height, tga.ImageArea.ImageData);
        }

        /// <summary>
        /// Converts <paramref name="tga"/>'s postage stamp (thumbnail) image to a <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="tga">Source <see cref="TgaFile"/>.</param>
        /// <param name="forceUseAlpha">Force use alpha channel.</param>
        /// <returns>The stamp as a <see cref="Bitmap"/>, or <see langword="null"/> when the file has no
        /// image, no stamp, or a stamp whose data does not match its declared size.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tga"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">The pixel depth or color map entry size has no GDI+ equivalent.</exception>
        public static Bitmap? GetPostageStampBitmap(this TgaFile tga, bool forceUseAlpha = false)
        {
            ArgumentNullException.ThrowIfNull(tga);

            TgaPostageStampImage? stamp = tga.ExtensionArea?.PostageStampImage;
            if (tga.Header.ImageType == TgaImageType.NoImageData || stamp is null || stamp.Data is null || stamp.Width == 0 || stamp.Height == 0)
                return null;
            // A stamp shorter than its declared size used to yield a half-filled or throwing bitmap.
            if (stamp.Data.Length != stamp.Width * stamp.Height * tga.Header.ImageSpec.PixelDepth.BytesPerPixel())
                return null;

            return ToBitmapCore(tga, forceUseAlpha, stamp.Width, stamp.Height, stamp.Data);
        }

        /// <summary>
        /// Shared implementation behind <see cref="ToBitmap(TgaFile, bool)"/> and <see cref="GetPostageStampBitmap(TgaFile, bool)"/>:
        /// resolves the pixel format from <paramref name="tga"/>'s header, applies the palette, copies
        /// <paramref name="pixels"/> row by row into the GDI+ buffer and flips per the image origin.
        /// </summary>
        /// <param name="tga">Source <see cref="TgaFile"/> (header, color map and extension area are read from it).</param>
        /// <param name="forceUseAlpha">Force use alpha channel.</param>
        /// <param name="width">Width in pixels of <paramref name="pixels"/>.</param>
        /// <param name="height">Height in pixels of <paramref name="pixels"/>.</param>
        /// <param name="pixels">Unpadded row-major pixel bytes, exactly <paramref name="width"/> * <paramref name="height"/> * bytes-per-pixel long.</param>
        /// <returns>The new <see cref="Bitmap"/>; the caller owns it.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="pixels"/> is not the expected length.</exception>
        /// <exception cref="NotSupportedException">The pixel depth or color map entry size has no GDI+ equivalent.</exception>
        private static Bitmap ToBitmapCore(TgaFile tga, bool forceUseAlpha, int width, int height, byte[] pixels)
        {
            int bytesPerPixel = tga.Header.ImageSpec.PixelDepth.BytesPerPixel();
            int strideBytes = width * bytesPerPixel;
            long expected = (long)strideBytes * height;
            // Marshal.Copy into Scan0 is unchecked: a too-long buffer corrupts the GDI+ heap and kills the process.
            if (pixels.Length != expected)
                throw new InvalidOperationException($"Image data is {pixels.Length} bytes but {width}x{height}x{bytesPerPixel} needs {expected}.");

            // Only the three "no / ignorable alpha" attribute types veto alpha; unknown values are treated as alpha like the spec's defaults.
            bool attributesAllowAlpha = tga.ExtensionArea is null || tga.ExtensionArea.AttributesType is not (TgaAttributeType.NoAlpha or TgaAttributeType.UndefinedAlphaCanBeIgnored or TgaAttributeType.UndefinedAlphaButShouldBeRetained);
            bool useAlpha = (tga.Header.ImageSpec.ImageDescriptor.AlphaChannelBits > 0 && attributesAllowAlpha) | forceUseAlpha;
            bool isGrayImage = tga.Header.ImageType.IsGrayscale();
            bool isColorMapped = tga.Header.ColorMapType == TgaColorMapType.ColorMap && tga.Header.ImageType.IsColorMapped();

            PixelFormat pixFormat = ResolvePixelFormat(tga, useAlpha, isGrayImage);
            var bmp = new Bitmap(width, height, pixFormat);

            if (isColorMapped)
            {
                // Palette alpha lives in the entry size, not the descriptor's attribute bits.
                bool paletteAlpha = (tga.Header.ColorMapSpec.ColorMapEntrySize is TgaColorMapEntrySize.A1R5G5B5 or TgaColorMapEntrySize.A8R8G8B8 && attributesAllowAlpha) | forceUseAlpha;
                ApplyColorMap(tga, bmp, paletteAlpha);
            }
            else if (pixFormat == PixelFormat.Format8bppIndexed)
            {
                // 8bpp grayscale (or 8bpp true-color, which has no palette of its own): identity gray ramp.
                ColorPalette grayPalette = bmp.Palette;
                for (int i = 0; i < grayPalette.Entries.Length; i++)
                    grayPalette.Entries[i] = Color.FromArgb(i, i, i);
                bmp.Palette = grayPalette;
            }

            CopyRows(pixels, bmp, strideBytes, pixFormat == PixelFormat.Format16bppGrayScale);

            // GDI+ has no key-color notion for Format16bppGrayScale (MakeTransparent throws a generic GDI+ error).
            if (tga.ExtensionArea != null && tga.ExtensionArea.KeyColor.ToInt() != 0 && pixFormat != PixelFormat.Format16bppGrayScale)
                bmp.MakeTransparent(tga.ExtensionArea.KeyColor.ToColor());

            // TGA rows are stored from the origin corner; GDI+ is always top-left.
            switch (tga.Header.ImageSpec.ImageDescriptor.ImageOrigin)
            {
                case TgaImageOrigin.BottomLeft:
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    break;
                case TgaImageOrigin.BottomRight:
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                    break;
                case TgaImageOrigin.TopRight:
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    break;
                case TgaImageOrigin.TopLeft:
                default:
                    break;
            }

            return bmp;
        }

        /// <summary>
        /// Maps the file's pixel depth (and image type / alpha usage) to the GDI+ <see cref="PixelFormat"/> holding the same byte layout.
        /// </summary>
        /// <param name="tga">Source <see cref="TgaFile"/>.</param>
        /// <param name="useAlpha">Whether the per-pixel attribute bits are meaningful alpha.</param>
        /// <param name="isGrayImage">Whether the image type is black-and-white.</param>
        /// <returns>The matching <see cref="PixelFormat"/>.</returns>
        /// <exception cref="NotSupportedException">The pixel depth has no GDI+ equivalent.</exception>
        private static PixelFormat ResolvePixelFormat(TgaFile tga, bool useAlpha, bool isGrayImage)
        {
            switch (tga.Header.ImageSpec.PixelDepth)
            {
                case TgaPixelDepth.Bpp8:
                    return PixelFormat.Format8bppIndexed;

                case TgaPixelDepth.Bpp16:
                    if (isGrayImage) return PixelFormat.Format16bppGrayScale;
                    return useAlpha ? PixelFormat.Format16bppArgb1555 : PixelFormat.Format16bppRgb555;

                case TgaPixelDepth.Bpp24:
                    return PixelFormat.Format24bppRgb;

                case TgaPixelDepth.Bpp32:
                    if (!useAlpha) return PixelFormat.Format32bppRgb;
                    return tga.ExtensionArea?.AttributesType == TgaAttributeType.PreMultipliedAlpha
                        ? PixelFormat.Format32bppPArgb
                        : PixelFormat.Format32bppArgb;

                default:
                    // Used to hand PixelFormat.Undefined to new Bitmap, which fails with "Parameter is not valid".
                    throw new NotSupportedException($"{nameof(TgaPixelDepth)} {(byte)tga.Header.ImageSpec.PixelDepth} has no {nameof(PixelFormat)} equivalent.");
            }
        }

        /// <summary>
        /// Copies the file's color map into <paramref name="bmp"/>'s palette, honoring
        /// <see cref="TgaColorMapSpec.FirstEntryIndex"/> (pixel index N addresses palette slot N, so
        /// entry i is stored at FirstEntryIndex + i).
        /// </summary>
        /// <param name="tga">Source <see cref="TgaFile"/>.</param>
        /// <param name="bmp">Destination bitmap; must have an indexed <see cref="PixelFormat"/>.</param>
        /// <param name="useAlpha">Whether stored entry alpha is meaningful.</param>
        /// <exception cref="NotSupportedException">The color map entry size has no GDI+ equivalent.</exception>
        private static void ApplyColorMap(TgaFile tga, Bitmap bmp, bool useAlpha)
        {
            ColorPalette palette = bmp.Palette;
            Color[] colors = palette.Entries;
            if (colors.Length == 0) return;

            TgaColorMapSpec spec = tga.Header.ColorMapSpec;
            int first = spec.FirstEntryIndex;
            int entryCount = Math.Min(spec.ColorMapLength, Math.Max(colors.Length - first, 0));

            for (int i = 0; i < entryCount; i++)
            {
                // Used to silently fall back to GDI+'s default halftone palette on an unknown entry size.
                colors[first + i] = TgaColorMapDrawing.ReadEntry(spec.ColorMapEntrySize, tga.ImageArea.ColorMapData!, i, useAlpha)
                    ?? throw new NotSupportedException($"{nameof(TgaColorMapEntrySize)} {(byte)spec.ColorMapEntrySize} has no {nameof(PixelFormat)} equivalent.");
            }

            bmp.Palette = palette;
        }

        /// <summary>
        /// Writes unpadded rows into the bitmap's buffer one row at a time so GDI+'s own stride (padding,
        /// or a negative stride for bottom-up surfaces) is respected instead of assumed.
        /// </summary>
        /// <param name="pixels">Unpadded row-major pixel bytes.</param>
        /// <param name="bmp">Destination bitmap.</param>
        /// <param name="strideBytes">Unpadded bytes per row.</param>
        /// <param name="invert">Whether to invert every byte (GDI+'s 16bpp grayscale stores inverted intensities).</param>
        private static void CopyRows(byte[] pixels, Bitmap bmp, int strideBytes, bool invert)
        {
            byte[] source = pixels;
            if (invert)
            {
                // Not officially supported by GDI+, but round-trips (tested on 16bpp grayscale images).
                source = (byte[])pixels.Clone();
                for (int i = 0; i < source.Length; i++)
                    source[i] ^= byte.MaxValue;
            }

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            try
            {
                for (int y = 0; y < bmp.Height; y++)
                    Marshal.Copy(source, y * strideBytes, bmpData.Scan0 + (nint)y * bmpData.Stride, strideBytes);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }
        }
    }
}
