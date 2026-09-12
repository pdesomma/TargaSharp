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
        /// <returns>Bitmap or null, on error.</returns>
        public static Bitmap ToBitmap(this TgaFile tga, bool forceUseAlpha = false) => ToBitmapCore(tga, forceUseAlpha, false);

        /// <summary>
        /// Converts <paramref name="tga"/>'s postage stamp (thumbnail) image to a <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="tga">Source <see cref="TgaFile"/>.</param>
        /// <param name="forceUseAlpha">Force use alpha channel.</param>
        /// <returns>Bitmap or null.</returns>
        public static Bitmap? GetPostageStampBitmap(this TgaFile tga, bool forceUseAlpha = false)
        {
            ArgumentNullException.ThrowIfNull(tga);

            if (tga.ExtArea?.PostageStampImage is null || tga.ExtArea.PostageStampImage.Data is null ||
                tga.ExtArea.PostageStampImage.Width <= 0 || tga.ExtArea.PostageStampImage.Height <= 0)
                return null;

            return ToBitmapCore(tga, forceUseAlpha, true);
        }

        /// <summary>
        /// Shared implementation behind <see cref="ToBitmap(TgaFile, bool)"/> and <see cref="GetPostageStampBitmap(TgaFile, bool)"/>.
        /// </summary>
        /// <param name="tga">Source <see cref="TgaFile"/>.</param>
        /// <param name="forceUseAlpha">Force use alpha channel.</param>
        /// <param name="postageStampImage">Get Postage Stamp Image (Thumb) or get main image?</param>
        /// <returns>Bitmap or null, on error.</returns>
        private static Bitmap ToBitmapCore(TgaFile tga, bool forceUseAlpha, bool postageStampImage)
        {
            ArgumentNullException.ThrowIfNull(tga);

            try
            {
                #region UseAlpha?
                bool useAlpha = true;
                if (tga.ExtArea != null)
                {
                    switch (tga.ExtArea.AttributesType)
                    {
                        case TgaAttributeType.NoAlpha:
                        case TgaAttributeType.UndefinedAlphaCanBeIgnored:
                        case TgaAttributeType.UndefinedAlphaButShouldBeRetained:
                            useAlpha = false;
                            break;
                        case TgaAttributeType.UsefulAlpha:
                        case TgaAttributeType.PreMultipliedAlpha:
                        default:
                            break;
                    }
                }
                useAlpha = (tga.Header.ImageSpec.ImageDescriptor.AlphaChannelBits > 0 && useAlpha) | forceUseAlpha;
                #endregion

                #region IsGrayImage
                bool isGrayImage = tga.Header.ImageType == TgaImageType.RLE_BlackWhite ||
                    tga.Header.ImageType == TgaImageType.Uncompressed_BlackWhite;
                #endregion

                #region Get PixelFormat
                PixelFormat pixFormat;

                switch (tga.Header.ImageSpec.PixelDepth)
                {
                    case TgaPixelDepth.Bpp8:
                        pixFormat = PixelFormat.Format8bppIndexed;
                        break;

                    case TgaPixelDepth.Bpp16:
                        if (isGrayImage)
                            pixFormat = PixelFormat.Format16bppGrayScale;
                        else
                            pixFormat = (useAlpha ? PixelFormat.Format16bppArgb1555 : PixelFormat.Format16bppRgb555);
                        break;

                    case TgaPixelDepth.Bpp24:
                        pixFormat = PixelFormat.Format24bppRgb;
                        break;

                    case TgaPixelDepth.Bpp32:
                        if (useAlpha)
                            pixFormat = (tga.ExtArea?.AttributesType == TgaAttributeType.PreMultipliedAlpha
                                ? PixelFormat.Format32bppPArgb
                                : PixelFormat.Format32bppArgb);
                        else
                            pixFormat = PixelFormat.Format32bppRgb;
                        break;

                    default:
                        pixFormat = PixelFormat.Undefined;
                        break;
                }
                #endregion

                ushort bmpWidth = (postageStampImage ? tga.ExtArea!.PostageStampImage!.Width : tga.Width);
                ushort bmpHeight = (postageStampImage ? tga.ExtArea!.PostageStampImage!.Height : tga.Height);
                Bitmap bmp = new Bitmap(bmpWidth, bmpHeight, pixFormat);

                #region ColorMap and GrayPalette
                if (tga.Header.ColorMapType == TgaColorMapType.ColorMap &&
                   (tga.Header.ImageType == TgaImageType.RLE_ColorMapped ||
                    tga.Header.ImageType == TgaImageType.Uncompressed_ColorMapped))
                {
                    ColorPalette? colorMap = bmp.Palette;
                    Color[] cMapColors = colorMap.Entries;
                    int entryCount = Math.Min(cMapColors.Length, tga.Header.ColorMapSpec.ColorMapLength);

                    for (int i = 0; i < entryCount; i++)
                    {
                        Color? entry = TgaColorMapDrawing.ReadEntry(tga.Header.ColorMapSpec.ColorMapEntrySize, tga.ImageOrColorMapArea.ColorMapData!, i, useAlpha);
                        if (entry is null)
                        {
                            colorMap = null;
                            break;
                        }
                        cMapColors[i] = entry.Value;
                    }

                    if (colorMap != null)
                        bmp.Palette = colorMap;
                }

                if (pixFormat == PixelFormat.Format8bppIndexed && isGrayImage)
                {
                    ColorPalette grayPalette = bmp.Palette;
                    Color[] grayColors = grayPalette.Entries;
                    for (int i = 0; i < grayColors.Length; i++)
                        grayColors[i] = Color.FromArgb(i, i, i);
                    bmp.Palette = grayPalette;
                }
                #endregion

                #region Bitmap width must by aligned (align value = 32 bits = 4 bytes)!
                byte[] imageData;
                int bytesPerPixel = (int)Math.Ceiling((double)tga.Header.ImageSpec.PixelDepth / 8.0);
                int strideBytes = bmp.Width * bytesPerPixel;
                int paddingBytes = (int)Math.Ceiling(strideBytes / 4.0) * 4 - strideBytes;

                byte[] sourceData = (postageStampImage ? tga.ExtArea!.PostageStampImage!.Data : tga.ImageOrColorMapArea.ImageData)!;

                if (paddingBytes > 0) // Need bytes align
                {
                    imageData = new byte[(strideBytes + paddingBytes) * bmp.Height];
                    for (int i = 0; i < bmp.Height; i++)
                        Buffer.BlockCopy(sourceData, i * strideBytes, imageData, i * (strideBytes + paddingBytes), strideBytes);
                }
                else
                    imageData = BitConverterHelper.ToBytes(sourceData);

                // Not official supported, but works (tested on 2 test images)!
                if (pixFormat == PixelFormat.Format16bppGrayScale)
                {
                    for (long i = 0; i < imageData.Length; i++)
                        imageData[i] ^= byte.MaxValue;
                }
                #endregion

                Rectangle re = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData bmpData = bmp.LockBits(re, ImageLockMode.WriteOnly, bmp.PixelFormat);
                Marshal.Copy(imageData, 0, bmpData.Scan0, imageData.Length);
                bmp.UnlockBits(bmpData);

                if (tga.ExtArea != null && tga.ExtArea.KeyColor.ToInt() != 0)
                    bmp.MakeTransparent(tga.ExtArea.KeyColor.ToColor());

                #region Flip Image
                switch (tga.Header.ImageSpec.ImageDescriptor.ImageOrigin)
                {
                    case TgaImageOrigin.BottomLeft:
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        break;
                    case TgaImageOrigin.BottomRight:
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                        break;
                    case TgaImageOrigin.TopLeft:
                    default:
                        break;
                    case TgaImageOrigin.TopRight:
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        break;
                }
                #endregion

                return bmp;
            }
            catch
            {
                return null!;
            }
        }
    }
}
