using System.Drawing;
using System.Drawing.Imaging;

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
        /// <returns>The image as a <see cref="Bitmap"/>. When the extension area carries a non-zero
        /// <see cref="TgaExtensionArea.KeyColor"/> the key is applied with <see cref="Bitmap.MakeTransparent(Color)"/>,
        /// which converts the result to <see cref="PixelFormat.Format32bppArgb"/> regardless of the file's depth.
        /// A 16bpp grayscale image is returned as <see cref="PixelFormat.Format8bppIndexed"/> with an identity gray
        /// palette, keeping only each pixel's high byte: GDI+ cannot read, draw, clone or save
        /// <see cref="PixelFormat.Format16bppGrayScale"/>, so the mapping is lossy but usable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tga"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="tga"/> has no image: its
        /// <see cref="TgaHeader.ImageType"/> is <see cref="TgaImageType.NoImageData"/>, its width or height
        /// is 0, or <see cref="TgaImageArea.ImageData"/> is <see langword="null"/> or not Width * Height * bytes-per-pixel long;
        /// or it is color-mapped and <see cref="TgaImageArea.ColorMapData"/> is shorter than the header declares.</exception>
        /// <exception cref="NotSupportedException">The pixel depth or color map entry size has no GDI+ equivalent
        /// (GDI+ has no indexed format wider than 8 bits, so 16-bit color-mapped images are not supported).</exception>
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
            if (stamp.Data.Length != stamp.DataLength(tga.Header.ImageSpec.PixelDepth))
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
        /// <exception cref="InvalidOperationException"><paramref name="pixels"/> is not the expected length, or the color map data is missing/short.</exception>
        /// <exception cref="NotSupportedException">The pixel depth or color map entry size has no GDI+ equivalent, or a color-mapped image is not 8bpp.</exception>
        private static Bitmap ToBitmapCore(TgaFile tga, bool forceUseAlpha, int width, int height, byte[] pixels)
        {
            int bytesPerPixel = tga.Header.ImageSpec.PixelDepth.BytesPerPixel();
            long expected = (long)width * bytesPerPixel * height;
            // Marshal.Copy into Scan0 is unchecked: a too-long buffer corrupts the GDI+ heap and kills the process.
            if (pixels.Length != expected)
                throw new InvalidOperationException($"Image data is {pixels.Length} bytes but {width}x{height}x{bytesPerPixel} needs {expected}.");

            // Only the three "no / ignorable alpha" attribute types veto alpha; unknown values are treated as alpha like the spec's defaults.
            bool attributesAllowAlpha = tga.ExtensionArea is null || tga.ExtensionArea.AttributesType is not (TgaAttributeType.NoAlpha or TgaAttributeType.UndefinedAlphaCanBeIgnored or TgaAttributeType.UndefinedAlphaButShouldBeRetained);
            bool useAlpha = (tga.Header.ImageSpec.ImageDescriptor.AlphaChannelBits > 0 && attributesAllowAlpha) || forceUseAlpha;
            bool isGrayImage = tga.Header.ImageType.IsGrayscale();
            bool isColorMapped = tga.Header.ColorMapType == TgaColorMapType.ColorMap && tga.Header.ImageType.IsColorMapped();

            if (isColorMapped)
            {
                // GDI+ has no indexed format wider than 8 bits; a 16-bit index used to be rendered as RGB555 garbage.
                if (tga.Header.ImageSpec.PixelDepth != TgaPixelDepth.Bpp8)
                    throw new NotSupportedException($"Color-mapped images with {(byte)tga.Header.ImageSpec.PixelDepth}bpp indices have no {nameof(PixelFormat)} equivalent; only 8bpp is supported.");
                // ReadEntry indexes ColorMapData unchecked; a short or null palette used to NRE / IndexOutOfRange.
                int expectedPalette = tga.Header.ColorMapDataLength;
                if (tga.ImageArea.ColorMapData is null || tga.ImageArea.ColorMapData.Length < expectedPalette)
                    throw new InvalidOperationException($"{nameof(TgaFile)}.{nameof(TgaFile.ImageArea)}.{nameof(TgaImageArea.ColorMapData)} is {tga.ImageArea.ColorMapData?.Length.ToString() ?? "null"} bytes but the header declares {expectedPalette}.");
            }

            bool preMultiplied = tga.ExtensionArea?.AttributesType == TgaAttributeType.PreMultipliedAlpha;
            PixelFormat pixFormat = TgaPixelFormatMap.Resolve(tga.Header.ImageSpec.PixelDepth, useAlpha, isGrayImage, preMultiplied);
            // 16bpp grayscale has no usable GDI+ format: keep each little-endian pixel's high byte as an 8bpp gray index.
            if (isGrayImage && tga.Header.ImageSpec.PixelDepth == TgaPixelDepth.Bpp16)
                pixels = HighBytes(pixels);

            var bmp = new Bitmap(width, height, pixFormat);
            try
            {
                if (isColorMapped)
                {
                    // Palette alpha lives in the entry size, not the descriptor's attribute bits.
                    bool paletteAlpha = (tga.Header.ColorMapSpec.ColorMapEntrySize is TgaColorMapEntrySize.A1R5G5B5 or TgaColorMapEntrySize.A8R8G8B8 && attributesAllowAlpha) || forceUseAlpha;
                    ApplyColorMap(tga, bmp, paletteAlpha);
                }
                else if (pixFormat == PixelFormat.Format8bppIndexed)
                {
                    // 8bpp grayscale, 16bpp grayscale high bytes, or 8bpp true-color (no palette of its own): identity gray ramp.
                    ColorPalette grayPalette = bmp.Palette;
                    for (int i = 0; i < grayPalette.Entries.Length; i++)
                        grayPalette.Entries[i] = Color.FromArgb(i, i, i);
                    bmp.Palette = grayPalette;
                }

                TgaBitmapRows.Write(bmp, pixels);

                if (tga.ExtensionArea != null && tga.ExtensionArea.KeyColor.ToInt() != 0)
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
            catch
            {
                // The bitmap is unmanaged GDI+ memory; a throwing palette/copy step used to leak it.
                bmp.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Keeps the high byte of every little-endian 16-bit pixel.
        /// </summary>
        /// <param name="pixels16">16-bit pixels, low byte first.</param>
        /// <returns>Half-length buffer of high bytes.</returns>
        private static byte[] HighBytes(byte[] pixels16)
        {
            byte[] high = new byte[pixels16.Length / 2];
            for (int i = 0; i < high.Length; i++)
                high[i] = pixels16[i * 2 + 1];
            return high;
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
            // Slots the file's map does not cover used to keep GDI+'s default halftone colors.
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.Black;

            for (int i = 0; i < entryCount; i++)
            {
                // Used to silently fall back to GDI+'s default halftone palette on an unknown entry size.
                colors[first + i] = TgaColorMapDrawing.ReadEntry(spec.ColorMapEntrySize, tga.ImageArea.ColorMapData!, i, useAlpha)
                    ?? throw new NotSupportedException($"{nameof(TgaColorMapEntrySize)} {(byte)spec.ColorMapEntrySize} has no {nameof(PixelFormat)} equivalent.");
            }

            bmp.Palette = palette;
        }
    }
}
