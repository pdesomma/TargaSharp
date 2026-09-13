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
        /// Makes a new <see cref="TgaFile"/> from a <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="bmp">Input Bitmap, supported a lot of bitmaps types: 8/15/16/24/32 Bpp's.</param>
        /// <param name="useRle">Use RLE Compression?</param>
        /// <param name="newFormat">Use new 2.0 TGA XFile format?</param>
        /// <param name="colorMap2BytesEntry">Is Color Map Entry size equal 15 or 16 Bpp, else - 24 or 32.</param>
        /// <returns>New <see cref="TgaFile"/> built from <paramref name="bmp"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bmp"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="bmp"/>'s <see cref="PixelFormat"/> is not supported.</exception>
        public static TgaFile FromBitmap(Bitmap bmp, bool useRle = false, bool newFormat = true, bool colorMap2BytesEntry = false)
        {
            ArgumentNullException.ThrowIfNull(bmp);

            var tga = new TgaFile();
            tga.Header.ImageSpec.ImageWidth = (ushort)bmp.Width;
            tga.Header.ImageSpec.ImageHeight = (ushort)bmp.Height;
            tga.Header.ImageSpec.ImageDescriptor.ImageOrigin = TgaImageOrigin.TopLeft;

            switch (bmp.PixelFormat)
            {
                case PixelFormat.Indexed:
                case PixelFormat.Gdi:
                case PixelFormat.Alpha:
                case PixelFormat.Undefined:
                case PixelFormat.PAlpha:
                case PixelFormat.Extended:
                case PixelFormat.Max:
                case PixelFormat.Canonical:
                case PixelFormat.Format16bppRgb565:
                default:
                    throw new NotSupportedException($"{nameof(PixelFormat)} {bmp.PixelFormat} is not supported.");

                case PixelFormat.Format1bppIndexed:
                case PixelFormat.Format4bppIndexed:
                case PixelFormat.Format8bppIndexed:
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format48bppRgb:
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:

                    int bpp = Math.Max(8, Image.GetPixelFormatSize(bmp.PixelFormat));
                    int bytesPP = bpp / 8;

                    bool isAlpha = Image.IsAlphaPixelFormat(bmp.PixelFormat);
                    bool isPreAlpha = isAlpha && bmp.PixelFormat.ToString().EndsWith("PArgb");
                    bool isColorMapped = bmp.PixelFormat.ToString().EndsWith("Indexed");

                    tga.Header.ImageSpec.PixelDepth = (TgaPixelDepth)(bytesPP * 8);

                    if (isAlpha)
                    {
                        tga.Header.ImageSpec.ImageDescriptor.AlphaChannelBits = (byte)(bytesPP * 2);

                        if (bmp.PixelFormat == PixelFormat.Format16bppArgb1555)
                            tga.Header.ImageSpec.ImageDescriptor.AlphaChannelBits = 1;
                    }

                    #region ColorMap
                    bool isGrayImage = (bmp.PixelFormat == PixelFormat.Format16bppGrayScale | isColorMapped);

                    if (isColorMapped && bmp.Palette != null)
                    {
                        Color[] colors = bmp.Palette.Entries;

                        #region Analyze ColorMapType
                        int alphaSum = 0;
                        bool colorMapUseAlpha = false;

                        for (int i = 0; i < colors.Length; i++)
                        {
                            isGrayImage &= (colors[i].R == colors[i].G && colors[i].G == colors[i].B);
                            colorMapUseAlpha |= (colors[i].A < 248);
                            alphaSum |= colors[i].A;
                        }
                        colorMapUseAlpha &= (alphaSum > 0);

                        int cMapBpp = (colorMap2BytesEntry ? 15 : 24) + (colorMapUseAlpha ? (colorMap2BytesEntry ? 1 : 8) : 0);
                        int cmBytesPP = ((TgaColorMapEntrySize)cMapBpp).BytesPerPixel();
                        #endregion

                        tga.Header.ColorMapSpec.ColorMapLength = Math.Min((ushort)colors.Length, ushort.MaxValue);
                        tga.Header.ColorMapSpec.ColorMapEntrySize = (TgaColorMapEntrySize)cMapBpp;
                        tga.ImageArea.ColorMapData = new byte[tga.Header.ColorMapSpec.ColorMapLength * cmBytesPP];

                        for (int i = 0; i < colors.Length; i++)
                        {
                            byte[] cMapEntry = TgaColorMapDrawing.WriteEntry(tga.Header.ColorMapSpec.ColorMapEntrySize, colors[i], cmBytesPP);
                            Buffer.BlockCopy(cMapEntry, 0, tga.ImageArea.ColorMapData!, i * cmBytesPP, cmBytesPP);
                        }
                    }
                    #endregion

                    #region ImageType
                    if (useRle)
                    {
                        if (isGrayImage)
                            tga.Header.ImageType = TgaImageType.RleGrayscale;
                        else if (isColorMapped)
                            tga.Header.ImageType = TgaImageType.RleColorMapped;
                        else
                            tga.Header.ImageType = TgaImageType.RleTrueColor;
                    }
                    else
                    {
                        if (isGrayImage)
                            tga.Header.ImageType = TgaImageType.UncompressedGrayscale;
                        else if (isColorMapped)
                            tga.Header.ImageType = TgaImageType.UncompressedColorMapped;
                        else
                            tga.Header.ImageType = TgaImageType.UncompressedTrueColor;
                    }

                    tga.Header.ColorMapType = (isColorMapped ? TgaColorMapType.ColorMap : TgaColorMapType.NoColorMap);
                    #endregion

                    #region NewFormat
                    if (newFormat)
                    {
                        // ToNewFormat() creates Footer + a default ExtensionArea (DateTimeStamp = now, a simple
                        // Alpha-bits-based AttributesType); override AttributesType below with the fuller
                        // pre-multiplied / "should be retained" semantics ToNewFormat() doesn't know about.
                        tga.ToNewFormat();

                        if (isAlpha)
                        {
                            tga.ExtensionArea!.AttributesType = TgaAttributeType.UsefulAlpha;

                            if (isPreAlpha)
                                tga.ExtensionArea.AttributesType = TgaAttributeType.PreMultipliedAlpha;
                        }
                        else
                        {
                            tga.ExtensionArea!.AttributesType = TgaAttributeType.NoAlpha;

                            if (tga.Header.ImageSpec.ImageDescriptor.AlphaChannelBits > 0)
                                tga.ExtensionArea.AttributesType = TgaAttributeType.UndefinedAlphaButShouldBeRetained;
                        }
                    }
                    #endregion

                    #region Bitmap width is aligned by 32 bits = 4 bytes! Delete it.
                    int strideBytes = bmp.Width * bytesPP;
                    int paddingBytes = TgaColorMapDrawing.RowPadding(strideBytes);

                    byte[] imageData = new byte[(strideBytes + paddingBytes) * bmp.Height];

                    Rectangle re = new Rectangle(0, 0, bmp.Width, bmp.Height);
                    BitmapData bmpData = bmp.LockBits(re, ImageLockMode.ReadOnly, bmp.PixelFormat);
                    Marshal.Copy(bmpData.Scan0, imageData, 0, imageData.Length);
                    bmp.UnlockBits(bmpData);

                    if (paddingBytes > 0) // Need delete bytes align
                    {
                        tga.ImageArea.ImageData = new byte[strideBytes * bmp.Height];
                        for (int i = 0; i < bmp.Height; i++)
                            Buffer.BlockCopy(imageData, i * (strideBytes + paddingBytes),
                                tga.ImageArea.ImageData, i * strideBytes, strideBytes);
                    }
                    else
                        tga.ImageArea.ImageData = imageData;

                    // Not official supported, but works (tested on 16bpp GrayScale test images)!
                    if (bmp.PixelFormat == PixelFormat.Format16bppGrayScale)
                    {
                        for (long i = 0; i < tga.ImageArea.ImageData.Length; i++)
                            tga.ImageArea.ImageData[i] ^= byte.MaxValue;
                    }
                    #endregion

                    break;
            }

            return tga;
        }
    }
}
