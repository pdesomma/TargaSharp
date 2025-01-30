using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// Targa image file.
    /// </summary>
    public class Targa : ICloneable
    {
        /// <summary>
        /// Create new empty <see cref="Targa"/> instance.
        /// </summary>
        public Targa() { }

        /// <summary>
        /// Create <see cref="Targa"/> instance with some params. If it must have ColorMap,
        /// check all ColorMap fields and settings after.
        /// </summary>
        /// <param name="width">Image Width.</param>
        /// <param name="height">Image Height.</param>
        /// <param name="pixelDepth">Image Pixel Depth (bits / pixel), set ColorMap bpp after, if needed!</param>
        /// <param name="imgType">Image Type (is RLE compressed, ColorMapped or GrayScaled).</param>
        /// <param name="attrBits">Set number of attribute bits (Alpha channel bits), default: 0, 1, 8.</param>
        /// <param name="newFormat">Use new 2.0 TGA XFile format?</param>
        public Targa(ushort width, ushort height, PixelDepth pixelDepth = PixelDepth.Bpp24, ImageType imgType = ImageType.Uncompressed_TrueColor, byte attrBits = 0, bool newFormat = true)
        {
            if (width <= 0 || height <= 0 || pixelDepth == PixelDepth.Other)
            {
                width = height = 0;
                pixelDepth = PixelDepth.Other;
                imgType = ImageType.NoImageData;
                attrBits = 0;
            }
            else
            {
                int bytesPerPixel = (int)Math.Ceiling((double)pixelDepth / 8.0);
                ImageOrColorMapArea.ImageData = new byte[width * height * bytesPerPixel];

                if (imgType == ImageType.Uncompressed_ColorMapped || imgType == ImageType.RLE_ColorMapped)
                {
                    HeaderArea.ColorMapType = ColorMapType.ColorMap;
                    HeaderArea.ColorMapSpec.FirstEntryIndex = 0;
                    HeaderArea.ColorMapSpec.EntrySize = (ColorMapEntrySize)Math.Ceiling((double)pixelDepth / 8);
                }
            }

            HeaderArea.ImageType = imgType;
            HeaderArea.ImageSpec.ImageWidth = width;
            HeaderArea.ImageSpec.ImageHeight = height;
            HeaderArea.ImageSpec.PixelDepth = pixelDepth;
            HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits = attrBits;

            if (newFormat)
            {
                FooterArea = new FooterArea();
                ExtensionArea = new ExtensionArea();
                ExtensionArea.DateTimeStamp = new TimeStamp(DateTime.UtcNow);
                ExtensionArea.AttributesType = (attrBits > 0 ? AttributeType.UsefulAlpha : AttributeType.NoAlpha);
            }
        }

        /// <summary>
        /// Make <see cref="Targa"/> from some <see cref="Targa"/> instance.
        /// Equal to <see cref="Targa.Clone()"/> function.
        /// </summary>
        /// <param name="tga">Original <see cref="Targa"/> instance.</param>
        public Targa(Targa tga)
        {
            HeaderArea = tga.HeaderArea.Clone();
            ImageOrColorMapArea = tga.ImageOrColorMapArea.Clone();
            DeveloperArea = tga.DeveloperArea?.Clone();
            ExtensionArea = tga.ExtensionArea?.Clone();
            FooterArea = tga.FooterArea?.Clone();
        }

        /// <summary>
        /// Load <see cref="Targa"/> from file.
        /// </summary>
        /// <param name="filename">Full path to TGA file.</param>
        /// <returns>Loaded <see cref="Targa"/> file.</returns>
        public Targa(string filename)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(filename);
#else
            if (filename is null) throw new ArgumentNullException("filename");
#endif
            if (!File.Exists(filename)) throw new FileNotFoundException("File: \"" + filename + "\" not found!");

            try
            {
                using var stream = new FileStream(filename, FileMode.Open);
                CreateFromStream(stream);
            }
            finally { }
        }

        /// <summary>
        /// Make <see cref="Targa"/> from bytes array.
        /// </summary>
        /// <param name="bytes">Bytes array (same like TGA File).</param>
        public Targa(byte[] bytes)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bytes);
#else
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
#endif
            try
            {
                using var stream = new MemoryStream(bytes, false);
                CreateFromStream(stream);
            }
            finally { }
        }

        /// <summary>
        /// Make <see cref="Targa"/> from <see cref="Stream"/>.
        /// For file opening better use <see cref="FromFile(string)"/>.
        /// </summary>
        /// <param name="stream">Some stream. You can use a lot of Stream types, but Stream must support: <see cref="Stream.CanSeek"/> and <see cref="Stream.CanRead"/>.</param>
        public Targa(Stream stream) => CreateFromStream(stream);

        /// <summary>
        /// Make a <see cref="Targa"/> from a <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="bmp">Input Bitmap, supported a lot of bitmaps types: 8/15/16/24/32 bits per pixel.</param>
        /// <param name="useRle">Use RLE Compression?</param>
        /// <param name="newFormat">Use new 2.0 TGA XFile format?</param>
        /// <param name="colorMapToBytesEntry">Is Color Map Entry size equal 15 or 16 bits per pixel, else - 24 or 32.</param>
        [SupportedOSPlatform("windows")]
        public Targa(Bitmap bmp, bool useRle = false, bool newFormat = true, bool colorMapToBytesEntry = false)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bmp);
#else
            if (bmp is null) throw new ArgumentNullException(nameof(bmp));
#endif
            HeaderArea.ImageSpec.ImageWidth = (ushort)bmp.Width;
            HeaderArea.ImageSpec.ImageHeight = (ushort)bmp.Height;
            HeaderArea.ImageSpec.ImageDescriptor.ImageOrigin = ImageOrigin.TopLeft;

            switch (bmp.PixelFormat)
            {
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
                    int bytesPerPixel = bpp / 8;

                    if (bmp.PixelFormat == PixelFormat.Format16bppRgb555) bpp = 15;

                    bool isAlpha = Image.IsAlphaPixelFormat(bmp.PixelFormat);
                    bool isPreAlpha = isAlpha && bmp.PixelFormat.ToString().EndsWith("PArgb");
                    bool isColorMapped = bmp.PixelFormat.ToString().EndsWith("Indexed");

                    HeaderArea.ImageSpec.PixelDepth = (PixelDepth)(bytesPerPixel * 8);

                    if (isAlpha)
                    {
                        HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits = (byte)(bytesPerPixel * 2);
                        if (bmp.PixelFormat == PixelFormat.Format16bppArgb1555) HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits = 1;
                    }

                    // color map
                    bool isGrayImage = (bmp.PixelFormat == PixelFormat.Format16bppGrayScale | isColorMapped);
                    if (isColorMapped && bmp.Palette is not null)
                    {
                        Color[] Colors = bmp.Palette.Entries;

                        // color map type
                        int alphaSum = 0;
                        bool colorMapUseAlpha = false;
                        for (int i = 0; i < Colors.Length; i++)
                        {
                            isGrayImage &= (Colors[i].R == Colors[i].G && Colors[i].G == Colors[i].B);
                            colorMapUseAlpha |= (Colors[i].A < 248);
                            alphaSum |= Colors[i].A;
                        }
                        colorMapUseAlpha &= (alphaSum > 0);
                        int cmpaBpp = (colorMapToBytesEntry ? 15 : 24) + (colorMapUseAlpha ? (colorMapToBytesEntry ? 1 : 8) : 0);
                        int cmBpp = (int)Math.Ceiling(cmpaBpp / 8.0);

                        HeaderArea.ColorMapSpec.ColorMapLength = Math.Min((ushort)Colors.Length, ushort.MaxValue);
                        HeaderArea.ColorMapSpec.EntrySize = (ColorMapEntrySize)cmpaBpp;
                        ImageOrColorMapArea.ColorMapData = new byte[HeaderArea.ColorMapSpec.ColorMapLength * cmBpp];

                        var colorMapEntry = new byte[cmBpp];

                        const float To5Bit = 32f / 256f; // Scale value from 8 to 5 bits.
                        for (int i = 0; i < Colors.Length; i++)
                        {
                            switch (HeaderArea.ColorMapSpec.EntrySize)
                            {
                                case ColorMapEntrySize.A1R5G5B5:
                                case ColorMapEntrySize.X1R5G5B5:
                                    int R = (int)(Colors[i].R * To5Bit);
                                    int G = (int)(Colors[i].G * To5Bit) << 5;
                                    int B = (int)(Colors[i].B * To5Bit) << 10;
                                    int A = 0;

                                    if (HeaderArea.ColorMapSpec.EntrySize == ColorMapEntrySize.A1R5G5B5) A = (Colors[i].A & 0x80) << 15;

                                    colorMapEntry = BitConverter.GetBytes(A | R | G | B);
                                    break;

                                case ColorMapEntrySize.R8G8B8:
                                    colorMapEntry[0] = Colors[i].B;
                                    colorMapEntry[1] = Colors[i].G;
                                    colorMapEntry[2] = Colors[i].R;
                                    break;

                                case ColorMapEntrySize.A8R8G8B8:
                                    colorMapEntry[0] = Colors[i].B;
                                    colorMapEntry[1] = Colors[i].G;
                                    colorMapEntry[2] = Colors[i].R;
                                    colorMapEntry[3] = Colors[i].A;
                                    break;

                                case ColorMapEntrySize.Other:
                                default:
                                    break;
                            }
                            Buffer.BlockCopy(colorMapEntry, 0, ImageOrColorMapArea.ColorMapData, i * cmBpp, cmBpp);
                        }
                    }

                    // image type
                    if (useRle)
                    {
                        if (isGrayImage)
                            HeaderArea.ImageType = ImageType.RLE_BlackWhite;
                        else if (isColorMapped)
                            HeaderArea.ImageType = ImageType.RLE_ColorMapped;
                        else
                            HeaderArea.ImageType = ImageType.RLE_TrueColor;
                    }
                    else
                    {
                        if (isGrayImage)
                            HeaderArea.ImageType = ImageType.Uncompressed_BlackWhite;
                        else if (isColorMapped)
                            HeaderArea.ImageType = ImageType.Uncompressed_ColorMapped;
                        else
                            HeaderArea.ImageType = ImageType.Uncompressed_TrueColor;
                    }

                    HeaderArea.ColorMapType = (isColorMapped ? ColorMapType.ColorMap : ColorMapType.NoColorMap);

                    // new format
                    if (newFormat)
                    {
                        FooterArea = new FooterArea();
                        ExtensionArea = new ExtensionArea();
                        ExtensionArea.DateTimeStamp = new TimeStamp(DateTime.UtcNow);

                        if (isAlpha)
                        {
                            ExtensionArea.AttributesType = AttributeType.UsefulAlpha;
                            if (isPreAlpha) ExtensionArea.AttributesType = AttributeType.PreMultipliedAlpha;
                        }
                        else
                        {
                            ExtensionArea.AttributesType = AttributeType.NoAlpha;
                            if (HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits > 0) ExtensionArea.AttributesType = AttributeType.UndefinedAlphaButShouldBeRetained;
                        }
                    }

                    // Bitmap width is aligned by 32 bits = 4 bytes! Delete it.
                    int strideBytes = bmp.Width * bytesPerPixel;
                    int paddingBytes = (int)Math.Ceiling(strideBytes / 4.0) * 4 - strideBytes;

                    var imageData = new byte[(strideBytes + paddingBytes) * bmp.Height];

                    Rectangle Re = new Rectangle(0, 0, bmp.Width, bmp.Height);
                    var bitmaptData = bmp.LockBits(Re, ImageLockMode.ReadOnly, bmp.PixelFormat);
                    Marshal.Copy(bitmaptData.Scan0, imageData, 0, imageData.Length);
                    bmp.UnlockBits(bitmaptData);
                    bitmaptData = null;

                    if (paddingBytes > 0) //Need delete bytes align
                    {
                        ImageOrColorMapArea.ImageData = new byte[strideBytes * bmp.Height];
                        for (int i = 0; i < bmp.Height; i++)
                            Buffer.BlockCopy(imageData, i * (strideBytes + paddingBytes),
                                ImageOrColorMapArea.ImageData, i * strideBytes, strideBytes);
                    }
                    else
                        ImageOrColorMapArea.ImageData = imageData;

                    imageData = null;

                    // Not official supported, but works (tested on 16bpp GrayScale test images)!
                    if (bmp.PixelFormat == PixelFormat.Format16bppGrayScale)
                    {
                        for (long i = 0; i < ImageOrColorMapArea.ImageData.Length; i++)
                            ImageOrColorMapArea.ImageData[i] ^= byte.MaxValue;
                    }

                    break;
                default:
                    throw new FormatException(nameof(PixelFormat) + " is not supported!");
            }
        }


        public DeveloperArea? DeveloperArea { get; set; }
        public ExtensionArea? ExtensionArea { get; set; }
        public FooterArea? FooterArea { get; set; }
        public HeaderArea HeaderArea { get; set; } = new HeaderArea();

        /// <summary>
        /// Gets or Sets Image Height (see <see cref="Header.ImageSpec.ImageHeight"/>).
        /// </summary>
        public ushort Height
        {
            get => HeaderArea.ImageSpec.ImageHeight;
            set => HeaderArea.ImageSpec.ImageHeight = value;
        }
        public ImageOrColorMapArea ImageOrColorMapArea { get; set; } = new ImageOrColorMapArea();
        
        /// <summary>
        /// Gets or Sets <see cref="Targa"/> image Size.
        /// </summary>
        public Size Size
        {
            get => HeaderArea.ImageSpec.Size;
            set
            {
                HeaderArea.ImageSpec.ImageWidth = (ushort)value.Width;
                HeaderArea.ImageSpec.ImageHeight = (ushort)value.Height;
            }
        }

        /// <summary>
        /// Gets or Sets Image Width (see <see cref="Header.ImageSpec.ImageWidth"/>).
        /// </summary>
        public ushort Width
        {
            get => HeaderArea.ImageSpec.ImageWidth; 
            set => HeaderArea.ImageSpec.ImageWidth = value;
        }

        /// <summary>
        /// Make full independent copy of <see cref="Targa"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="Targa"/>.</returns>
        public Targa Clone() => new Targa(this);
        object ICloneable.Clone() => Clone();

        /// <summary>
        /// Convert TGA Image to new XFile format (v2.0).
        /// </summary>
        public void ConvertToNewFormat()
        {
            FooterArea ??= new();
            ExtensionArea ??= new() {
                    DateTimeStamp = new TimeStamp(DateTime.UtcNow),
                    AttributesType = HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits > 0 ? AttributeType.UsefulAlpha : AttributeType.NoAlpha
                };
        }

        /// <summary>
        /// Remove the postage stamp image.
        /// </summary>
        public void DeletePostageStampImage()
        {
            if (ExtensionArea is not null) ExtensionArea.PostageStampImage = null;
        }

        /// <summary>
        /// Flip <see cref="Targa"/> directions, for more info see <see cref="ImageOrigin"/>.
        /// </summary>
        /// <param name="Horizontal">Flip horizontal.</param>
        /// <param name="Vertical">Flip vertical.</param>
        public void Flip(bool Horizontal = false, bool Vertical = false)
        {
            int NewOrigin = (int)HeaderArea.ImageSpec.ImageDescriptor.ImageOrigin;
            NewOrigin = NewOrigin ^ ((Vertical ? 0x20 : 0) | (Horizontal ? 0x10 : 0));
            HeaderArea.ImageSpec.ImageDescriptor.ImageOrigin = (ImageOrigin)NewOrigin;
        }

        /// <summary>
        /// Save <see cref="Targa"/> to file.
        /// </summary>
        /// <param name="filename">Full path to file.</param>
        /// <returns>Return "true", if all done or "false", if failed.</returns>
        public bool Save(string filename)
        {
            try
            {
                using var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                using var memoryStream = new MemoryStream();
                var result = SaveFunc(memoryStream);
                memoryStream.WriteTo(fileStream);
                fileStream.Flush();
                return result;
            }
            catch { return false; }
        }

        /// <summary>
        /// Save <see cref="Targa"/> to <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Some stream, it must support: <see cref="Stream.CanWrite"/>.</param>
        /// <returns>Return "true", if all done or "false", if failed.</returns>
        public bool Save(Stream stream) => SaveFunc(stream);

        /// <summary>
        /// Convert <see cref="Targa"/> to <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="forceUseAlpha">Force use alpha channel.</param>
        /// <returns>Bitmap or null, on error.</returns>
        [SupportedOSPlatform("windows")]
        public Bitmap? ToBitmap(bool forceUseAlpha = false) => ToBitmapFunc(forceUseAlpha, false);

        /// <summary>
        /// Convert <see cref="Targa"/> to bytes array.
        /// </summary>
        /// <returns>Bytes array, (equal to saved file, but in memory) or null (on error).</returns>
        public byte[]? ToBytes()
        {
            try
            {
                using var ms = new MemoryStream();
                Save(ms);
                var bytes = ms.ToArray();
                ms.Flush();
                return bytes;
            }
            catch { return null; }
        }




        /// <summary>
        /// Check and update all fields with data length and offsets.
        /// </summary>
        /// <returns>Return "true", if all OK or "false", if checking failed.</returns>
        private bool CheckAndUpdateOffsets()
        {
            if (HeaderArea is null || ImageOrColorMapArea is null) return false;

            uint offsets = HeaderArea.ByteSize; // Virtual Offset

            if (ImageOrColorMapArea?.ImageId is not null)
            {
                int maxLength = 255;
                if (ImageOrColorMapArea.ImageId.UseEndingChar) maxLength--;

                HeaderArea.IdLength = (byte)Math.Min(ImageOrColorMapArea.ImageId.OriginalString.Length, maxLength);
                ImageOrColorMapArea.ImageId.Length = HeaderArea.IdLength;
                offsets += HeaderArea.IdLength;
            }
            else HeaderArea.IdLength = 0;

            // check color map
            if (HeaderArea.ColorMapType != ColorMapType.NoColorMap)
            {
                if (HeaderArea.ColorMapSpec is null ||
                    HeaderArea.ColorMapSpec.ColorMapLength == 0 ||
                    ImageOrColorMapArea?.ColorMapData is null)
                {
                    return false;
                }

                int cmBytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ColorMapSpec.EntrySize / 8.0);
                int bytesLength = HeaderArea.ColorMapSpec.ColorMapLength * cmBytesPerPixel;

                if (bytesLength != ImageOrColorMapArea.ColorMapData.Length) return false;

                offsets += (uint)ImageOrColorMapArea.ColorMapData.Length;
            }

            // image data
            int bytesPerPixel = 0;
            if (HeaderArea.ImageType != ImageType.NoImageData)
            {
                if (HeaderArea.ImageSpec is null) return false;
                if (HeaderArea.ImageSpec.ImageWidth == 0 || HeaderArea.ImageSpec.ImageHeight == 0) return false;
                if (ImageOrColorMapArea?.ImageData is null) return false;

                bytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ImageSpec.PixelDepth / 8.0);

                if (Width * Height * bytesPerPixel != ImageOrColorMapArea.ImageData.Length) return false;

                if (HeaderArea.ImageType >= ImageType.RLE_ColorMapped && HeaderArea.ImageType <= ImageType.RLE_BlackWhite)
                {
                    byte[]? rle = RleEncode(ImageOrColorMapArea.ImageData, Width, Height);
                    if (rle is null) return false;
                    offsets += (uint)rle.Length;
                }
                else offsets += (uint)ImageOrColorMapArea.ImageData.Length;
            }

            if (FooterArea is not null)
            {
                if (DeveloperArea is not null)
                {
                    // developer area
                    int DevAreaCount = DeveloperArea.Count;
                    for (int i = 0; i < DevAreaCount; i++)
                        if (DeveloperArea[i] is null || DeveloperArea[i].FieldSize <= 0) //Del Empty Entries
                        {
                            DeveloperArea.Entries.RemoveAt(i);
                            DevAreaCount--;
                            i--;
                        }

                    if (DeveloperArea.Count <= 0) FooterArea.DeveloperDirectoryOffset = 0;

                    if (DeveloperArea.Count > 2)
                    {
                        DeveloperArea.Entries.Sort((a, b) => { return a.Tag.CompareTo(b.Tag); });
                        for (int i = 0; i < DeveloperArea.Count - 1; i++)
                            if (DeveloperArea[i].Tag == DeveloperArea[i + 1].Tag)
                            {
                                return false;
                            }
                    }

                    for (int i = 0; i < DeveloperArea.Count; i++)
                    {
                        DeveloperArea[i].Offset = offsets;
                        offsets += (uint)DeveloperArea[i].FieldSize;
                    }

                    FooterArea.DeveloperDirectoryOffset = offsets;
                    offsets += (uint)(DeveloperArea.Count * 10 + 2);
                }
                else FooterArea.DeveloperDirectoryOffset = 0;

                // extension area
                if (ExtensionArea is not null)
                {
                    ExtensionArea.ExtensionSize = ExtensionArea.MinSize;
                    if (ExtensionArea.OtherDataInExtensionArea is not null)
                        ExtensionArea.ExtensionSize += (ushort)ExtensionArea.OtherDataInExtensionArea.Length;

                    ExtensionArea.DateTimeStamp = new TimeStamp(DateTime.UtcNow);

                    FooterArea.ExtensionAreaOffset = offsets;
                    offsets += ExtensionArea.ExtensionSize;

                    // scan line table
                    if (ExtensionArea.ScanLineTable is null) ExtensionArea.ScanLineOffset = 0;
                    else
                    {
                        if (ExtensionArea.ScanLineTable.Length != Height) return false;
                        ExtensionArea.ScanLineOffset = offsets;
                        offsets += (uint)(ExtensionArea.ScanLineTable.Length * 4);
                    }

                    // postage stamp image
                    if (ExtensionArea.PostageStampImage is null) ExtensionArea.PostageStampOffset = 0;
                    else
                    {
                        if (ExtensionArea.PostageStampImage.Width == 0 || ExtensionArea.PostageStampImage.Height == 0) return false;
                        if (ExtensionArea.PostageStampImage.Data is null) return false;

                        int PImgSB = ExtensionArea.PostageStampImage.Width * ExtensionArea.PostageStampImage.Height * bytesPerPixel;
                        if (HeaderArea.ImageType != ImageType.NoImageData && ExtensionArea.PostageStampImage.Data.Length != PImgSB) return false;

                        ExtensionArea.PostageStampOffset = offsets;
                        offsets += (uint)(ExtensionArea.PostageStampImage.Data.Length);
                    }

                    // color correction table
                    if (ExtensionArea.ColorCorrectionTable is null) ExtensionArea.ColorCorrectionTableOffset = 0;
                    else
                    {
                        if (ExtensionArea.ColorCorrectionTable.Length != 1024) return false;
                        ExtensionArea.ColorCorrectionTableOffset = offsets;
                        offsets += (uint)(ExtensionArea.ColorCorrectionTable.Length * 2);
                    }
                }
                else FooterArea.ExtensionAreaOffset = 0;

                if ((FooterArea?.ToBytes()?.Length ?? -1) != FooterArea.Size) return false;
                offsets += FooterArea.Size;
            }
            return true;
        }

        /// <summary>
        /// Create from a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileLoadException"></exception>
        private void CreateFromStream(Stream stream)
        {
            if (stream is null) throw new ArgumentNullException();
            if (!(stream.CanRead && stream.CanSeek)) throw new FileLoadException("Stream reading or seeking is not available!");

            stream.Seek(0, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(stream);

            HeaderArea = new HeaderArea(binaryReader.ReadBytes((int)new HeaderArea().ByteSize));

            if (HeaderArea.IdLength > 0) ImageOrColorMapArea.ImageId = new TgaString(binaryReader.ReadBytes(HeaderArea.IdLength));

            if (HeaderArea.ColorMapSpec.ColorMapLength > 0)
            {
                int CmBytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ColorMapSpec.EntrySize / 8.0);
                int LenBytes = HeaderArea.ColorMapSpec.ColorMapLength * CmBytesPerPixel;
                ImageOrColorMapArea.ColorMapData = binaryReader.ReadBytes(LenBytes);
            }

            // read image data
            int BytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ImageSpec.PixelDepth / 8.0);
            if (HeaderArea.ImageType != ImageType.NoImageData)
            {
                int ImageDataSize = Width * Height * BytesPerPixel;
                switch (HeaderArea.ImageType)
                {
                    case ImageType.RLE_ColorMapped:
                    case ImageType.RLE_TrueColor:
                    case ImageType.RLE_BlackWhite:

                        int DataOffset = 0;
                        byte PacketInfo;
                        int PacketCount;
                        byte[] RLE_Bytes, RLE_Part;
                        ImageOrColorMapArea.ImageData = new byte[ImageDataSize];

                        do
                        {
                            PacketInfo = binaryReader.ReadByte(); //1 type bit and 7 count bits. Len = Count + 1.
                            PacketCount = (PacketInfo & 127) + 1;

                            if (PacketInfo >= 128) // bit7 = 1, RLE
                            {
                                RLE_Bytes = new byte[PacketCount * BytesPerPixel];
                                RLE_Part = binaryReader.ReadBytes(BytesPerPixel);
                                for (int i = 0; i < RLE_Bytes.Length; i++)
                                    RLE_Bytes[i] = RLE_Part[i % BytesPerPixel];
                            }
                            else // RAW format
                                RLE_Bytes = binaryReader.ReadBytes(PacketCount * BytesPerPixel);

                            Buffer.BlockCopy(RLE_Bytes, 0, ImageOrColorMapArea.ImageData, DataOffset, RLE_Bytes.Length);
                            DataOffset += RLE_Bytes.Length;
                        }
                        while (DataOffset < ImageDataSize);
                        RLE_Bytes = null;
                        break;

                    case ImageType.Uncompressed_ColorMapped:
                    case ImageType.Uncompressed_TrueColor:
                    case ImageType.Uncompressed_BlackWhite:
                        ImageOrColorMapArea.ImageData = binaryReader.ReadBytes(ImageDataSize);
                        break;
                }
            }

            // try and parse footer
            stream.Seek(-FooterArea.Size, SeekOrigin.End);
            uint FooterOffset = (uint)stream.Position;
            FooterArea MbFooter = new FooterArea(binaryReader.ReadBytes(FooterArea.Size));
            if (MbFooter.IsFooterCorrect)
            {
                FooterArea = MbFooter;
                uint DevDirOffset = FooterArea.DeveloperDirectoryOffset;
                uint ExtAreaOffset = FooterArea.ExtensionAreaOffset;

                // if dev area exist, read it.
                if (DevDirOffset != 0)
                {
                    stream.Seek(DevDirOffset, SeekOrigin.Begin);
                    DeveloperArea = new();
                    uint numberOfTags = binaryReader.ReadUInt16();

                    ushort[] tags = new ushort[numberOfTags];
                    uint[] tagOffsets = new uint[numberOfTags];
                    uint[] tagSizes = new uint[numberOfTags];

                    for (int i = 0; i < numberOfTags; i++)
                    {
                        tags[i] = binaryReader.ReadUInt16();
                        tagOffsets[i] = binaryReader.ReadUInt32();
                        tagSizes[i] = binaryReader.ReadUInt32();
                    }

                    for (int i = 0; i < numberOfTags; i++)
                    {
                        stream.Seek(tagOffsets[i], SeekOrigin.Begin);
                        DeveloperArea.Entries.Add(new DeveloperTag(tags[i], tagOffsets[i], binaryReader.ReadBytes((int)tagSizes[i])));
                    }
                }

                // if extension area exists, read it.
                if (ExtAreaOffset != 0)
                {
                    stream.Seek(ExtAreaOffset, SeekOrigin.Begin);
                    ushort ExtAreaSize = Math.Max((ushort)ExtensionArea.MinSize, binaryReader.ReadUInt16());
                    stream.Seek(ExtAreaOffset, SeekOrigin.Begin);
                    ExtensionArea = new ExtensionArea(binaryReader.ReadBytes(ExtAreaSize));

                    if (ExtensionArea.ScanLineOffset > 0)
                    {
                        stream.Seek(ExtensionArea.ScanLineOffset, SeekOrigin.Begin);
                        ExtensionArea.ScanLineTable = new uint[Height];
                        for (int i = 0; i < ExtensionArea.ScanLineTable.Length; i++)
                            ExtensionArea.ScanLineTable[i] = binaryReader.ReadUInt32();
                    }

                    if (ExtensionArea.PostageStampOffset > 0)
                    {
                        stream.Seek(ExtensionArea.PostageStampOffset, SeekOrigin.Begin);
                        byte W = binaryReader.ReadByte();
                        byte H = binaryReader.ReadByte();
                        int ImgDataSize = W * H * BytesPerPixel;
                        if (ImgDataSize > 0)
                            ExtensionArea.PostageStampImage = new PostageStampImage(W, H, binaryReader.ReadBytes(ImgDataSize));
                    }

                    if (ExtensionArea.ColorCorrectionTableOffset > 0)
                    {
                        stream.Seek(ExtensionArea.ColorCorrectionTableOffset, SeekOrigin.Begin);
                        ExtensionArea.ColorCorrectionTable = new ushort[256 * 4];
                        for (int i = 0; i < ExtensionArea.ColorCorrectionTable.Length; i++)
                            ExtensionArea.ColorCorrectionTable[i] = binaryReader.ReadUInt16();
                    }
                }
            }
            binaryReader.Close();
        }

        bool SaveFunc(Stream stream)
        {
            try
            {
                if (stream is null) throw new ArgumentNullException();
                if (!(stream.CanWrite && stream.CanSeek)) throw new FileLoadException("Stream writing or seeking is not available");

                if (!CheckAndUpdateOffsets()) return false;

                var binaryWriter = new BinaryWriter(stream);
                binaryWriter.Write(HeaderArea.ToBytes()!);

                if (ImageOrColorMapArea.ImageId is not null) binaryWriter.Write(ImageOrColorMapArea.ImageId.ToBytes());

                if (HeaderArea.ColorMapType != ColorMapType.NoColorMap) binaryWriter.Write(ImageOrColorMapArea.ColorMapData);

                // image data
                if (HeaderArea.ImageType != ImageType.NoImageData)
                {
                    if (HeaderArea.ImageType >= ImageType.RLE_ColorMapped && HeaderArea.ImageType <= ImageType.RLE_BlackWhite)
                        binaryWriter.Write(RleEncode(ImageOrColorMapArea.ImageData, Width, Height));
                    else
                        binaryWriter.Write(ImageOrColorMapArea.ImageData);
                }

                if (FooterArea is not null)
                {
                    if (DeveloperArea is not null)
                    {
                        for (int i = 0; i < DeveloperArea.Count; i++) binaryWriter.Write(DeveloperArea[i].Data);

                        binaryWriter.Write((ushort)DeveloperArea.Count);
                        for (int i = 0; i < DeveloperArea.Count; i++)
                        {
                            binaryWriter.Write(DeveloperArea[i].Tag);
                            binaryWriter.Write(DeveloperArea[i].Offset);
                            binaryWriter.Write(DeveloperArea[i].FieldSize);
                        }
                    }

                    if (ExtensionArea is not null)
                    {
                        binaryWriter.Write(ExtensionArea.ToBytes());

                        if (ExtensionArea.ScanLineTable is not null)
                            for (int i = 0; i < ExtensionArea.ScanLineTable.Length; i++)
                                binaryWriter.Write(ExtensionArea.ScanLineTable[i]);

                        if (ExtensionArea.PostageStampImage is not null)
                            binaryWriter.Write(ExtensionArea.PostageStampImage.ToBytes());

                        if (ExtensionArea.ColorCorrectionTable is not null)
                            for (int i = 0; i < ExtensionArea.ColorCorrectionTable.Length; i++)
                                binaryWriter.Write(ExtensionArea.ColorCorrectionTable[i]);
                    }
                    binaryWriter.Write(FooterArea.ToBytes());
                }

                binaryWriter.Flush();
                stream.Flush();
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Encode image with RLE compression (used RLE per line)!
        /// </summary>
        /// <param name="imageData">Image data, bytes array with size = Width * Height * BytesPerPixel.</param>
        /// <param name="width">Image Width, must be > 0.</param>
        /// <param name="height">Image Height, must be > 0.</param>
        /// <returns>Bytes array with RLE compressed image data.</returns>
        byte[]? RleEncode(byte[] imageData, int width, int height)
        {
            if (imageData is null) throw new ArgumentNullException(nameof(imageData));
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width) + " and " + nameof(height) + " must be > 0!");

            int bpp = imageData.Length / width / height; // Bytes per pixel
            int scanLineSize = width * bpp;
            if (scanLineSize * height != imageData.Length) throw new ArgumentOutOfRangeException("ImageData has wrong Length!");

            int count, pos;
            bool isRle = false;
            List<byte> encoded = new();
            byte[] rowData = new byte[scanLineSize];
            try
            {
                for (int y = 0; y < height; y++)
                {
                    pos = 0;
                    Buffer.BlockCopy(imageData, y * scanLineSize, rowData, 0, scanLineSize);

                    while (pos < scanLineSize)
                    {
                        if (pos >= scanLineSize - bpp)
                        {
                            encoded.Add(0);
                            encoded.AddRange(EnumerableHelper.GetElements(rowData, (uint)pos, (uint)bpp));
                            pos += bpp;
                            break;
                        }

                        count = 0; //1
                        isRle = EnumerableHelper.AreElementsEqual(rowData, pos, pos + bpp, bpp);

                        for (int i = pos + bpp; i < Math.Min(pos + 128 * bpp, scanLineSize) - bpp; i += bpp)
                        {
                            if (isRle ^ EnumerableHelper.AreElementsEqual(rowData, (isRle ? pos : i), i + bpp, bpp))
                            {
                                break;
                            }
                            else count++;
                        }

                        int countBpp = (count + 1) * bpp;
                        encoded.Add((byte)(isRle ? count | 128 : count));
                        encoded.AddRange(EnumerableHelper.GetElements(rowData, (uint)pos, (uint)(isRle ? bpp : countBpp)));
                        pos += countBpp;
                    }
                }
                return encoded.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert <see cref="Targa"/> to <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="forceUseAlpha">Force use alpha channel.</param>
        /// <param name="postageStampImage">Get Postage Stamp Image (Thumb) or get main image?</param>
        /// <returns>Bitmap or null, on error.</returns>
        [SupportedOSPlatform("windows")]
        Bitmap? ToBitmapFunc(bool forceUseAlpha = false, bool postageStampImage = false)
        {
            try
            {
                bool useAlpha = true;
                if (ExtensionArea is not null)
                {
                    useAlpha = ExtensionArea.AttributesType switch
                    {
                        AttributeType.NoAlpha or AttributeType.UndefinedAlphaCanBeIgnored or AttributeType.UndefinedAlphaButShouldBeRetained => false,
                        _ => true
                    };
                }
                useAlpha = (HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits > 0 && useAlpha) | forceUseAlpha;
                bool isGrayImage = HeaderArea.ImageType == ImageType.RLE_BlackWhite || HeaderArea.ImageType == ImageType.Uncompressed_BlackWhite;

                // pixel format
                var pixelFormat = PixelFormat.Format24bppRgb;
                switch (HeaderArea.ImageSpec.PixelDepth)
                {
                    case PixelDepth.Bpp8:
                        pixelFormat = PixelFormat.Format8bppIndexed;
                        break;
                    case PixelDepth.Bpp16:
                        if (isGrayImage)
                            pixelFormat = PixelFormat.Format16bppGrayScale;
                        else
                            pixelFormat = (useAlpha ? PixelFormat.Format16bppArgb1555 : PixelFormat.Format16bppRgb555);
                        break;
                    case PixelDepth.Bpp24:
                        pixelFormat = PixelFormat.Format24bppRgb;
                        break;
                    case PixelDepth.Bpp32:
                        if (useAlpha)
                        {
                            var f = FooterArea;
                            if (ExtensionArea?.AttributesType == AttributeType.PreMultipliedAlpha)
                                pixelFormat = PixelFormat.Format32bppPArgb;
                            else
                                pixelFormat = PixelFormat.Format32bppArgb;
                        }
                        else
                            pixelFormat = PixelFormat.Format32bppRgb;
                        break;
                    default:
                        pixelFormat = PixelFormat.Undefined;
                        break;
                }

                ushort bmpWidth = (postageStampImage ? ExtensionArea.PostageStampImage.Width : Width);
                ushort bmpHeight = (postageStampImage ? ExtensionArea.PostageStampImage.Height : Height);
                var bitmap = new Bitmap(bmpWidth, bmpHeight, pixelFormat);

                // ColorMap and GrayPalette
                if (HeaderArea.ColorMapType == ColorMapType.ColorMap && (HeaderArea.ImageType == ImageType.RLE_ColorMapped || HeaderArea.ImageType == ImageType.Uncompressed_ColorMapped))
                {
                    var colorMap = bitmap.Palette;
                    var colorMapColors = colorMap.Entries;

                    switch (HeaderArea.ColorMapSpec.EntrySize)
                    {
                        case ColorMapEntrySize.X1R5G5B5:
                        case ColorMapEntrySize.A1R5G5B5:
                            const float To8Bit = 255f / 31f; // Scale value from 5 to 8 bits.
                            for (int i = 0; i < Math.Min(colorMapColors.Length, HeaderArea.ColorMapSpec.ColorMapLength); i++)
                            {
                                ushort A1R5G5B5 = BitConverter.ToUInt16(ImageOrColorMapArea.ColorMapData, i * 2);
                                int A = (useAlpha ? (A1R5G5B5 & 0x8000) >> 15 : 1) * 255; // (0 or 1) * 255
                                int R = (int)(((A1R5G5B5 & 0x7C00) >> 10) * To8Bit);
                                int G = (int)(((A1R5G5B5 & 0x3E0) >> 5) * To8Bit);
                                int B = (int)((A1R5G5B5 & 0x1F) * To8Bit);
                                colorMapColors[i] = Color.FromArgb(A, R, G, B);
                            }
                            break;
                        case ColorMapEntrySize.R8G8B8:
                            for (int i = 0; i < Math.Min(colorMapColors.Length, HeaderArea.ColorMapSpec.ColorMapLength); i++)
                            {
                                int Index = i * 3; //RGB = 3 bytes
                                int R = ImageOrColorMapArea.ColorMapData[Index + 2];
                                int G = ImageOrColorMapArea.ColorMapData[Index + 1];
                                int B = ImageOrColorMapArea.ColorMapData[Index];
                                colorMapColors[i] = Color.FromArgb(R, G, B);
                            }
                            break;
                        case ColorMapEntrySize.A8R8G8B8:
                            for (int i = 0; i < Math.Min(colorMapColors.Length, HeaderArea.ColorMapSpec.ColorMapLength); i++)
                            {
                                int ARGB = BitConverter.ToInt32(ImageOrColorMapArea.ColorMapData, i * 4);
                                colorMapColors[i] = Color.FromArgb(useAlpha ? ARGB | (0xFF << 24) : ARGB);
                            }
                            break;
                        default:
                            colorMap = null;
                            break;
                    }

                    if (colorMap is not null) bitmap.Palette = colorMap;
                }

                if (pixelFormat == PixelFormat.Format8bppIndexed && isGrayImage)
                {
                    ColorPalette GrayPalette = bitmap.Palette;
                    Color[] GrayColors = GrayPalette.Entries;
                    for (int i = 0; i < GrayColors.Length; i++)
                        GrayColors[i] = Color.FromArgb(i, i, i);
                    bitmap.Palette = GrayPalette;
                }

                // Bitmap width must by aligned (align value = 32 bits = 4 bytes)!
                byte[]? imgData;
                int bytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ImageSpec.PixelDepth / 8.0);
                int strideBytes = bitmap.Width * bytesPerPixel;
                int paddingBytes = (int)Math.Ceiling(strideBytes / 4.0) * 4 - strideBytes;

                if (paddingBytes > 0) //Need bytes align
                {
                    imgData = new byte[(strideBytes + paddingBytes) * bitmap.Height];
                    for (int i = 0; i < bitmap.Height; i++)
                    {
                        Buffer.BlockCopy(postageStampImage ? ExtensionArea.PostageStampImage.Data :
                            ImageOrColorMapArea.ImageData, i * strideBytes, imgData,
                            i * (strideBytes + paddingBytes), strideBytes);
                    }
                }
                else imgData = ByteHelper.ToBytes(postageStampImage ? ExtensionArea.PostageStampImage.Data : ImageOrColorMapArea.ImageData);

                // Not official supported, but works (tested on 2 test images)!
                if (pixelFormat == PixelFormat.Format16bppGrayScale)
                {
                    for (long i = 0; i < imgData.Length; i++) imgData[i] ^= byte.MaxValue;
                }

                Rectangle Re = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bitmapData = bitmap.LockBits(Re, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                Marshal.Copy(imgData, 0, bitmapData.Scan0, imgData.Length);
                bitmap.UnlockBits(bitmapData);
                imgData = null;
                bitmapData = null;

                if (ExtensionArea is not null && ExtensionArea.KeyColor.ToInt() != 0) bitmap.MakeTransparent(ExtensionArea.KeyColor.ToColor());

                // Flip Image
                switch (HeaderArea.ImageSpec.ImageDescriptor.ImageOrigin)
                {
                    case ImageOrigin.BottomLeft:
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        break;
                    case ImageOrigin.BottomRight:
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                        break;
                    case ImageOrigin.TopLeft:
                    default:
                        break;
                    case ImageOrigin.TopRight:
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        break;
                }
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        [SupportedOSPlatform("windows")]
        public static explicit operator Bitmap?(Targa tga) => tga.ToBitmap();
        [SupportedOSPlatform("windows")]
        public static explicit operator Targa(Bitmap bmp) => new(bmp);

        // PostageStamp Image
        /// <summary>
        /// Convert <see cref="PostageStampImage"/> to <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="ForceUseAlpha">Force use alpha channel.</param>
        /// <returns>Bitmap or null.</returns>

        [SupportedOSPlatform("windows")]
        public Bitmap? GetPostageStampImage(bool ForceUseAlpha = false) =>
            (ExtensionArea?.PostageStampImage?.Data is null) || ((ExtensionArea?.PostageStampImage?.Width ?? 0) <= 0) || ((ExtensionArea?.PostageStampImage?.Height ?? 0) <= 0 )
                ? null : ToBitmapFunc(ForceUseAlpha, true);

        /// <summary>
        /// Update Postage Stamp Image or set it.
        /// </summary>
        public void UpdatePostageStampImage()
        {
            if (HeaderArea.ImageType == ImageType.NoImageData && ExtensionArea is not null)
            {
                ExtensionArea.PostageStampImage = null;
                return;
            }

            ConvertToNewFormat();
            if (ExtensionArea is not null && ExtensionArea.PostageStampImage is null) ExtensionArea.PostageStampImage = new PostageStampImage();

            int psHeight = HeaderArea.ImageSpec.ImageHeight;
            int psWidth = HeaderArea.ImageSpec.ImageWidth;

            if (Width > 64 || Height > 64)
            {
                float AspectRatio = Width / (float)Height;
                psWidth = (byte)(64f * (AspectRatio < 1f ? AspectRatio : 1f));
                psHeight = (byte)(64f / (AspectRatio > 1f ? AspectRatio : 1f));
            }
            psWidth = Math.Max(psWidth, 4);
            psHeight = Math.Max(psHeight, 4);

            ExtensionArea.PostageStampImage.Width = (byte)psWidth;
            ExtensionArea.PostageStampImage.Height = (byte)psHeight;

            int BytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ImageSpec.PixelDepth / 8.0);
            ExtensionArea.PostageStampImage.Data = new byte[psWidth * psHeight * BytesPerPixel];

            float WidthCoefficient = Width / (float)psWidth;
            float HeightCoefficient = Height / (float)psHeight;

            for (int y = 0; y < psHeight; y++)
            {
                int Y_Offset = (int)(y * HeightCoefficient) * Width * BytesPerPixel;
                int y_Offset = y * psWidth * BytesPerPixel;

                for (int x = 0; x < psWidth; x++)
                {
                    Buffer.BlockCopy(ImageOrColorMapArea.ImageData, Y_Offset + (int)(x * WidthCoefficient) * BytesPerPixel,
                        ExtensionArea.PostageStampImage.Data, y_Offset + x * BytesPerPixel, BytesPerPixel);
                }
            }
        }
    }
}