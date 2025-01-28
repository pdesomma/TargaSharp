using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using TargaNet.Helpers;

namespace TargaNet
{
    /// <summary>
    /// Targa image file.
    /// </summary>
    public class Targa : ICloneable
    {
        #region TGA Creation, Loading, Saving (all are public, have reference to private metods).
        /// <summary>
        /// Create new empty <see cref="Targa"/> instance.
        /// </summary>
        public Targa() { }

        /// <summary>
        /// Create <see cref="Targa"/> instance with some params. If it must have ColorMap,
        /// check all ColorMap fields and settings after.
        /// </summary>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="PixDepth">Image Pixel Depth (bits / pixel), set ColorMap bpp after, if needed!</param>
        /// <param name="ImgType">Image Type (is RLE compressed, ColorMapped or GrayScaled).</param>
        /// <param name="AttrBits">Set number of attribute bits (Alpha channel bits), default: 0, 1, 8.</param>
        /// <param name="NewFormat">Use new 2.0 TGA XFile format?</param>
        public Targa(ushort Width, ushort Height, PixelDepth PixDepth = PixelDepth.Bpp24, ImageType ImgType = ImageType.Uncompressed_TrueColor, byte AttrBits = 0, bool NewFormat = true)
        {
            if (Width <= 0 || Height <= 0 || PixDepth == PixelDepth.Other)
            {
                Width = Height = 0;
                PixDepth = PixelDepth.Other;
                ImgType = ImageType.NoImageData;
                AttrBits = 0;
            }
            else
            {
                int BytesPerPixel = (int)Math.Ceiling((double)PixDepth / 8.0);
                ImageOrColorMapArea.ImageData = new byte[Width * Height * BytesPerPixel];

                if (ImgType == ImageType.Uncompressed_ColorMapped || ImgType == ImageType.RLE_ColorMapped)
                {
                    HeaderArea.ColorMapType = ColorMapType.ColorMap;
                    HeaderArea.ColorMapSpec.FirstEntryIndex = 0;
                    HeaderArea.ColorMapSpec.EntrySize = (ColorMapEntrySize)Math.Ceiling((double)PixDepth / 8);
                }
            }

            HeaderArea.ImageType = ImgType;
            HeaderArea.ImageSpec.ImageWidth = Width;
            HeaderArea.ImageSpec.ImageHeight = Height;
            HeaderArea.ImageSpec.PixelDepth = PixDepth;
            HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits = AttrBits;

            if (NewFormat)
            {
                FooterArea = new FooterArea();
                ExtensionArea = new ExtensionArea();
                ExtensionArea.DateTimeStamp = new TimeStamp(DateTime.UtcNow);
                ExtensionArea.AttributesType = (AttrBits > 0 ? AttributeType.UsefulAlpha : AttributeType.NoAlpha);
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
            DeveloperArea = tga.DeveloperArea.Clone();
            ExtensionArea = tga.ExtensionArea.Clone();
            FooterArea = tga.FooterArea.Clone();
        }

        /// <summary>
        /// Load <see cref="Targa"/> from file.
        /// </summary>
        /// <param name="filename">Full path to TGA file.</param>
        /// <returns>Loaded <see cref="Targa"/> file.</returns>
        public Targa(string filename) => LoadFunc(filename);

        /// <summary>
        /// Make <see cref="Targa"/> from bytes array.
        /// </summary>
        /// <param name="bytes">Bytes array (same like TGA File).</param>
        public Targa(byte[] bytes) => LoadFunc(bytes);

        /// <summary>
        /// Make <see cref="Targa"/> from <see cref="Stream"/>.
        /// For file opening better use <see cref="FromFile(string)"/>.
        /// </summary>
        /// <param name="stream">Some stream. You can use a lot of Stream types, but Stream must support:
        /// <see cref="Stream.CanSeek"/> and <see cref="Stream.CanRead"/>.</param>
        public Targa(Stream stream) => LoadFunc(stream);

        /// <summary>
        /// Make <see cref="Targa"/> from <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="bmp">Input Bitmap, supported a lot of bitmaps types: 8/15/16/24/32 Bpp's.</param>
        /// <param name="UseRLE">Use RLE Compression?</param>
        /// <param name="NewFormat">Use new 2.0 TGA XFile format?</param>
        /// <param name="ColorMap2BytesEntry">Is Color Map Entry size equal 15 or 16 Bpp, else - 24 or 32.</param>
        public Targa(Bitmap bmp, bool UseRLE = false, bool NewFormat = true, bool ColorMap2BytesEntry = false)
        {
            LoadFunc(bmp, UseRLE, NewFormat, ColorMap2BytesEntry);
        }

        /// <summary>
        /// Make <see cref="Targa"/> from <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="bmp">Input Bitmap, supported a lot of bitmaps types: 8/15/16/24/32 Bpp's.</param>
        /// <param name="UseRLE">Use RLE Compression?</param>
        /// <param name="NewFormat">Use new 2.0 TGA XFile format?</param>
        /// <param name="ColorMap2BytesEntry">Is Color Map Entry size equal 15 or 16 Bpp, else - 24 or 32.</param>
        public static Targa FromBitmap(Bitmap bmp, bool UseRLE = false, bool NewFormat = true, bool ColorMap2BytesEntry = false) => new Targa(bmp, UseRLE, NewFormat, ColorMap2BytesEntry);

        /// <summary>
        /// Make <see cref="Targa"/> from bytes array.
        /// </summary>
        /// <param name="bytes">Bytes array (same like TGA File).</param>
        public static Targa FromBytes(byte[] bytes) => new Targa(bytes);

        /// <summary>
        /// Load <see cref="Targa"/> from file.
        /// </summary>
        /// <param name="filename">Full path to TGA file.</param>
        /// <returns>Loaded <see cref="Targa"/> file.</returns>
        public static Targa FromFile(string filename) => new Targa(filename);

        /// <summary>
        /// Make <see cref="Targa"/> from <see cref="Stream"/>.
        /// For file opening better use <see cref="FromFile(string)"/>.
        /// </summary>
        /// <param name="stream">Some stream. You can use a lot of Stream types, but Stream must support:
        /// <see cref="Stream.CanSeek"/> and <see cref="Stream.CanRead"/>.</param>
        public static Targa FromStream(Stream stream) => new Targa(stream);

        /// <summary>
        /// Save <see cref="Targa"/> to file.
        /// </summary>
        /// <param name="filename">Full path to file.</param>
        /// <returns>Return "true", if all done or "false", if failed.</returns>
        public bool Save(string filename)
        {
            try
            {
                bool Result = false;
                using (FileStream Fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
                using (MemoryStream Ms = new MemoryStream())
                {
                    Result = SaveFunc(Ms);
                    Ms.WriteTo(Fs);
                    Fs.Flush();
                }
                return Result;
            }
            catch { return false; }
        }

        /// <summary>
        /// Save <see cref="Targa"/> to <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Some stream, it must support: <see cref="Stream.CanWrite"/>.</param>
        /// <returns>Return "true", if all done or "false", if failed.</returns>
        public bool Save(Stream stream) => SaveFunc(stream);
        #endregion


        public HeaderArea HeaderArea { get; set; }   = new HeaderArea();

        /// <summary>
        /// Gets or Sets Image Height (see <see cref="Header.ImageSpec.ImageHeight"/>).
        /// </summary>
        public ushort Height
        {
            get { return HeaderArea.ImageSpec.ImageHeight; }
            set { HeaderArea.ImageSpec.ImageHeight = value; }
        }
        public ImageOrColorMapArea ImageOrColorMapArea { get; set; } = new ImageOrColorMapArea();
        public DeveloperArea DeveloperArea { get; set; }
        public ExtensionArea ExtensionArea { get; set; }
        public FooterArea FooterArea { get; set; }

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
        /// Get information from TGA image.
        /// </summary>
        /// <returns>MultiLine string with info fields (one per line).</returns>
        public string GetInfo()
        {
            StringBuilder SB = new StringBuilder();

            SB.AppendLine("Header:");
            SB.AppendLine("\tID Length = " + HeaderArea.IdLength);
            SB.AppendLine("\tImage Type = " + HeaderArea.ImageType);
            SB.AppendLine("\tHeader -> ImageSpec:");
            SB.AppendLine("\t\tImage Width = " + HeaderArea.ImageSpec.ImageWidth);
            SB.AppendLine("\t\tImage Height = " + HeaderArea.ImageSpec.ImageHeight);
            SB.AppendLine("\t\tPixel Depth = " + HeaderArea.ImageSpec.PixelDepth);
            SB.AppendLine("\t\tImage Descriptor (AsByte) = " + HeaderArea.ImageSpec.ImageDescriptor.ToByte());
            SB.AppendLine("\t\tImage Descriptor -> AttributeBits = " + HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits);
            SB.AppendLine("\t\tImage Descriptor -> ImageOrigin = " + HeaderArea.ImageSpec.ImageDescriptor.ImageOrigin);
            SB.AppendLine("\t\tX_Origin = " + HeaderArea.ImageSpec.XOrigin);
            SB.AppendLine("\t\tY_Origin = " + HeaderArea.ImageSpec.YOrigin);
            SB.AppendLine("\tColorMap Type = " + HeaderArea.ColorMapType);
            SB.AppendLine("\tHeader -> ColorMapSpec:");
            SB.AppendLine("\t\tColorMap Entry Size = " + HeaderArea.ColorMapSpec.EntrySize);
            SB.AppendLine("\t\tColorMap Length = " + HeaderArea.ColorMapSpec.ColorMapLength);
            SB.AppendLine("\t\tFirstEntry Index = " + HeaderArea.ColorMapSpec.FirstEntryIndex);

            SB.AppendLine("\nImage / Color Map Area:");
            if (HeaderArea.IdLength > 0 && ImageOrColorMapArea.ImageId != null)
                SB.AppendLine("\tImage ID = \"" + ImageOrColorMapArea.ImageId.GetString() + "\"");
            else
                SB.AppendLine("\tImage ID = null");

            if (ImageOrColorMapArea.ImageData != null)
                SB.AppendLine("\tImage Data Length = " + ImageOrColorMapArea.ImageData.Length);
            else
                SB.AppendLine("\tImage Data = null");

            if (ImageOrColorMapArea.ColorMapData != null)
                SB.AppendLine("\tColorMap Data Length = " + ImageOrColorMapArea.ColorMapData.Length);
            else
                SB.AppendLine("\tColorMap Data = null");

            SB.AppendLine("\nDevelopers Area:");
            if (DeveloperArea != null)
                SB.AppendLine("\tCount = " + DeveloperArea.Count);
            else
                SB.AppendLine("\tDevArea = null");

            SB.AppendLine("\nExtension Area:");
            if (ExtensionArea != null)
            {
                SB.AppendLine("\tExtension Size = " + ExtensionArea.ExtensionSize);
                SB.AppendLine("\tAuthor Name = \"" + ExtensionArea.AuthorName.GetString() + "\"");
                SB.AppendLine("\tAuthor Comments = \"" + ExtensionArea.AuthorComments.GetString() + "\"");
                SB.AppendLine("\tDate / Time Stamp = " + ExtensionArea.DateTimeStamp);
                SB.AppendLine("\tJob Name / ID = \"" + ExtensionArea.JobNameOrID.GetString() + "\"");
                SB.AppendLine("\tJob Time = " + ExtensionArea.JobTime);
                SB.AppendLine("\tSoftware ID = \"" + ExtensionArea.SoftwareID.GetString() + "\"");
                SB.AppendLine("\tSoftware Version = \"" + ExtensionArea.SoftwareVersion + "\"");
                SB.AppendLine("\tKey Color = " + ExtensionArea.KeyColor);
                SB.AppendLine("\tPixel Aspect Ratio = " + ExtensionArea.PixelAspectRatio);
                SB.AppendLine("\tGamma Value = " + ExtensionArea.GammaValue);
                SB.AppendLine("\tColor Correction Table Offset = " + ExtensionArea.ColorCorrectionTableOffset);
                SB.AppendLine("\tPostage Stamp Offset = " + ExtensionArea.PostageStampOffset);
                SB.AppendLine("\tScan Line Offset = " + ExtensionArea.ScanLineOffset);
                SB.AppendLine("\tAttributes Type = " + ExtensionArea.AttributesType);

                if (ExtensionArea.ScanLineTable != null)
                    SB.AppendLine("\tScan Line Table = " + ExtensionArea.ScanLineTable.Length);
                else
                    SB.AppendLine("\tScan Line Table = null");

                if (ExtensionArea.PostageStampImage != null)
                    SB.AppendLine("\tPostage Stamp Image: " + ExtensionArea.PostageStampImage.ToString());
                else
                    SB.AppendLine("\tPostage Stamp Image = null");

                SB.AppendLine("\tColor Correction Table = " + (ExtensionArea.ColorCorrectionTable != null));
            }
            else
                SB.AppendLine("\tExtArea = null");

            SB.AppendLine("\nFooter:");
            if (FooterArea != null)
            {
                SB.AppendLine("\tExtension Area Offset = " + FooterArea.ExtensionAreaOffset);
                SB.AppendLine("\tDeveloper Directory Offset = " + FooterArea.DeveloperDirectoryOffset);
                SB.AppendLine("\tSignature (Full) = \"" + FooterArea.Signature.ToString() +
                    FooterArea.ReservedCharacter.ToString() + FooterArea.BinaryZeroStringTerminator.ToString() + "\"");
            }
            else
                SB.AppendLine("\tFooter = null");

            return SB.ToString();
        }

        /// <summary>
        /// Check and update all fields with data length and offsets.
        /// </summary>
        /// <returns>Return "true", if all OK or "false", if checking failed.</returns>
        public bool CheckAndUpdateOffsets(out string ErrorStr)
        {
            ErrorStr = String.Empty;

            if (HeaderArea == null)
            {
                ErrorStr = "Header = null";
                return false;
            }

            if (ImageOrColorMapArea == null)
            {
                ErrorStr = "ImageOrColorMapArea = null";
                return false;
            }

            uint Offset = HeaderArea.ByteSize; // Virtual Offset

            #region Header
            if (ImageOrColorMapArea.ImageId != null)
            {
                int StrMaxLen = 255;
                if (ImageOrColorMapArea.ImageId.UseEndingChar)
                    StrMaxLen--;

                HeaderArea.IdLength = (byte)Math.Min(ImageOrColorMapArea.ImageId.OriginalString.Length, StrMaxLen);
                ImageOrColorMapArea.ImageId.Length = HeaderArea.IdLength;
                Offset += HeaderArea.IdLength;
            }
            else
                HeaderArea.IdLength = 0;
            #endregion

            #region ColorMap
            if (HeaderArea.ColorMapType != ColorMapType.NoColorMap)
            {
                if (HeaderArea.ColorMapSpec == null)
                {
                    ErrorStr = "Header.ColorMapSpec = null";
                    return false;
                }

                if (HeaderArea.ColorMapSpec.ColorMapLength == 0)
                {
                    ErrorStr = "Header.ColorMapSpec.ColorMapLength = 0";
                    return false;
                }

                if (ImageOrColorMapArea.ColorMapData == null)
                {
                    ErrorStr = "ImageOrColorMapArea.ColorMapData = null";
                    return false;
                }

                int CmBytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ColorMapSpec.EntrySize / 8.0);
                int LenBytes = HeaderArea.ColorMapSpec.ColorMapLength * CmBytesPerPixel;

                if (LenBytes != ImageOrColorMapArea.ColorMapData.Length)
                {
                    ErrorStr = "ImageOrColorMapArea.ColorMapData.Length has wrong size!";
                    return false;
                }

                Offset += (uint)ImageOrColorMapArea.ColorMapData.Length;
            }
            #endregion

            #region Image Data
            int BytesPerPixel = 0;
            if (HeaderArea.ImageType != ImageType.NoImageData)
            {
                if (HeaderArea.ImageSpec == null)
                {
                    ErrorStr = "Header.ImageSpec = null";
                    return false;
                }

                if (HeaderArea.ImageSpec.ImageWidth == 0 || HeaderArea.ImageSpec.ImageHeight == 0)
                {
                    ErrorStr = "Header.ImageSpec.ImageWidth = 0 or Header.ImageSpec.ImageHeight = 0";
                    return false;
                }

                if (ImageOrColorMapArea.ImageData == null)
                {
                    ErrorStr = "ImageOrColorMapArea.ImageData = null";
                    return false;
                }

                BytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ImageSpec.PixelDepth / 8.0);
                if (Width * Height * BytesPerPixel != ImageOrColorMapArea.ImageData.Length)
                {
                    ErrorStr = "ImageOrColorMapArea.ImageData.Length has wrong size!";
                    return false;
                }

                if (HeaderArea.ImageType >= ImageType.RLE_ColorMapped &&
                    HeaderArea.ImageType <= ImageType.RLE_BlackWhite)
                {
                    byte[] RLE = RLE_Encode(ImageOrColorMapArea.ImageData, Width, Height);
                    if (RLE == null)
                    {
                        ErrorStr = "RLE Compressing error! Check Image Data size.";
                        return false;
                    }

                    Offset += (uint)RLE.Length;
                    RLE = null;
                }
                else
                    Offset += (uint)ImageOrColorMapArea.ImageData.Length;
            }
            #endregion

            #region Footer, DevArea, ExtArea
            if (FooterArea != null)
            {
                #region DevArea
                if (DeveloperArea != null)
                {
                    int DevAreaCount = DeveloperArea.Count;
                    for (int i = 0; i < DevAreaCount; i++)
                        if (DeveloperArea[i] == null || DeveloperArea[i].FieldSize <= 0) //Del Empty Entries
                        {
                            DeveloperArea.Entries.RemoveAt(i);
                            DevAreaCount--;
                            i--;
                        }

                    if (DeveloperArea.Count <= 0)
                        FooterArea.DeveloperDirectoryOffset = 0;

                    if (DeveloperArea.Count > 2)
                    {
                        DeveloperArea.Entries.Sort((a, b) => { return a.Tag.CompareTo(b.Tag); });
                        for (int i = 0; i < DeveloperArea.Count - 1; i++)
                            if (DeveloperArea[i].Tag == DeveloperArea[i + 1].Tag)
                            {
                                ErrorStr = "DevArea Enties has same Tags!";
                                return false;
                            }
                    }

                    for (int i = 0; i < DeveloperArea.Count; i++)
                    {
                        DeveloperArea[i].Offset = Offset;
                        Offset += (uint)DeveloperArea[i].FieldSize;
                    }

                    FooterArea.DeveloperDirectoryOffset = Offset;
                    Offset += (uint)(DeveloperArea.Count * 10 + 2);
                }
                else
                    FooterArea.DeveloperDirectoryOffset = 0;
                #endregion

                #region ExtArea
                if (ExtensionArea != null)
                {
                    ExtensionArea.ExtensionSize = ExtensionArea.MinSize;
                    if (ExtensionArea.OtherDataInExtensionArea != null)
                        ExtensionArea.ExtensionSize += (ushort)ExtensionArea.OtherDataInExtensionArea.Length;

                    ExtensionArea.DateTimeStamp = new TimeStamp(DateTime.UtcNow);

                    FooterArea.ExtensionAreaOffset = Offset;
                    Offset += ExtensionArea.ExtensionSize;

                    #region ScanLineTable
                    if (ExtensionArea.ScanLineTable == null)
                        ExtensionArea.ScanLineOffset = 0;
                    else
                    {
                        if (ExtensionArea.ScanLineTable.Length != Height)
                        {
                            ErrorStr = "ExtArea.ScanLineTable.Length != Height";
                            return false;
                        }

                        ExtensionArea.ScanLineOffset = Offset;
                        Offset += (uint)(ExtensionArea.ScanLineTable.Length * 4);
                    }
                    #endregion

                    #region PostageStampImage
                    if (ExtensionArea.PostageStampImage == null)
                        ExtensionArea.PostageStampOffset = 0;
                    else
                    {
                        if (ExtensionArea.PostageStampImage.Width == 0 || ExtensionArea.PostageStampImage.Height == 0)
                        {
                            ErrorStr = "ExtArea.PostageStampImage Width or Height is equal 0!";
                            return false;
                        }

                        if (ExtensionArea.PostageStampImage.Data == null)
                        {
                            ErrorStr = "ExtArea.PostageStampImage.Data == null";
                            return false;
                        }

                        int PImgSB = ExtensionArea.PostageStampImage.Width * ExtensionArea.PostageStampImage.Height * BytesPerPixel;
                        if (HeaderArea.ImageType != ImageType.NoImageData &&
                            ExtensionArea.PostageStampImage.Data.Length != PImgSB)
                        {
                            ErrorStr = "ExtArea.PostageStampImage.Data.Length is wrong!";
                            return false;
                        }


                        ExtensionArea.PostageStampOffset = Offset;
                        Offset += (uint)(ExtensionArea.PostageStampImage.Data.Length);
                    }
                    #endregion

                    #region ColorCorrectionTable
                    if (ExtensionArea.ColorCorrectionTable == null)
                        ExtensionArea.ColorCorrectionTableOffset = 0;
                    else
                    {
                        if (ExtensionArea.ColorCorrectionTable.Length != 1024)
                        {
                            ErrorStr = "ExtArea.ColorCorrectionTable.Length != 256 * 4";
                            return false;
                        }

                        ExtensionArea.ColorCorrectionTableOffset = Offset;
                        Offset += (uint)(ExtensionArea.ColorCorrectionTable.Length * 2);
                    }
                    #endregion
                }
                else
                    FooterArea.ExtensionAreaOffset = 0;
                #endregion

                #region Footer
                if (FooterArea.ToBytes().Length != FooterArea.Size)
                {
                    ErrorStr = "Footer.Length is wrong!";
                    return false;
                }

                Offset += FooterArea.Size;
                #endregion
            }
            #endregion

            return true;
        }

        #region Convert
        /// <summary>
        /// Convert <see cref="Targa"/> to <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="ForceUseAlpha">Force use alpha channel.</param>
        /// <returns>Bitmap or null, on error.</returns>
        public Bitmap ToBitmap(bool ForceUseAlpha = false)
        {
            return ToBitmapFunc(ForceUseAlpha, false);
        }

        /// <summary>
        /// Convert <see cref="Targa"/> to bytes array.
        /// </summary>
        /// <returns>Bytes array, (equal to saved file, but in memory) or null (on error).</returns>
        public byte[] ToBytes()
        {
            try
            {
                byte[] Bytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    Save(ms);
                    Bytes = ms.ToArray();
                    ms.Flush();
                }
                return Bytes;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert TGA Image to new XFile format (v2.0).
        /// </summary>
        public void ToNewFormat()
        {
            FooterArea ??= new FooterArea();
            if (ExtensionArea is null)
            {
                ExtensionArea = new ExtensionArea();
                ExtensionArea.DateTimeStamp = new TimeStamp(DateTime.UtcNow);
                ExtensionArea.AttributesType = HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits > 0 ? AttributeType.UsefulAlpha : AttributeType.NoAlpha;
            }
        }
        #endregion

        #region Private functions
        bool LoadFunc(string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("File: \"" + filename + "\" not found!");
            try
            {
                using (FileStream FS = new FileStream(filename, FileMode.Open))
                return LoadFunc(FS);
            }
            catch
            {
                return false;
            }
        }

        bool LoadFunc(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException();
            try
            {
                using (MemoryStream FS = new MemoryStream(bytes, false))
                return LoadFunc(FS);
            }
            catch
            {
                return false;
            }
        }

        bool LoadFunc(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException();
            if (!(stream.CanRead && stream.CanSeek)) throw new FileLoadException("Stream reading or seeking is not avaiable!");

            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                BinaryReader Br = new BinaryReader(stream);

                HeaderArea = new HeaderArea(Br.ReadBytes((int)new HeaderArea().ByteSize));

                if (HeaderArea.IdLength > 0)
                    ImageOrColorMapArea.ImageId = new TgaString(Br.ReadBytes(HeaderArea.IdLength));

                if (HeaderArea.ColorMapSpec.ColorMapLength > 0)
                {
                    int CmBytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ColorMapSpec.EntrySize / 8.0);
                    int LenBytes = HeaderArea.ColorMapSpec.ColorMapLength * CmBytesPerPixel;
                    ImageOrColorMapArea.ColorMapData = Br.ReadBytes(LenBytes);
                }

                #region Read Image Data
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
                                PacketInfo = Br.ReadByte(); //1 type bit and 7 count bits. Len = Count + 1.
                                PacketCount = (PacketInfo & 127) + 1;

                                if (PacketInfo >= 128) // bit7 = 1, RLE
                                {
                                    RLE_Bytes = new byte[PacketCount * BytesPerPixel];
                                    RLE_Part = Br.ReadBytes(BytesPerPixel);
                                    for (int i = 0; i < RLE_Bytes.Length; i++)
                                        RLE_Bytes[i] = RLE_Part[i % BytesPerPixel];
                                }
                                else // RAW format
                                    RLE_Bytes = Br.ReadBytes(PacketCount * BytesPerPixel);

                                Buffer.BlockCopy(RLE_Bytes, 0, ImageOrColorMapArea.ImageData, DataOffset, RLE_Bytes.Length);
                                DataOffset += RLE_Bytes.Length;
                            }
                            while (DataOffset < ImageDataSize);
                            RLE_Bytes = null;
                            break;

                        case ImageType.Uncompressed_ColorMapped:
                        case ImageType.Uncompressed_TrueColor:
                        case ImageType.Uncompressed_BlackWhite:
                            ImageOrColorMapArea.ImageData = Br.ReadBytes(ImageDataSize);
                            break;
                    }
                }
                #endregion

                #region Try parse Footer
                stream.Seek(-FooterArea.Size, SeekOrigin.End);
                uint FooterOffset = (uint)stream.Position;
                FooterArea MbFooter = new FooterArea(Br.ReadBytes(FooterArea.Size));
                if (MbFooter.IsFooterCorrect)
                {
                    FooterArea = MbFooter;
                    uint DevDirOffset = FooterArea.DeveloperDirectoryOffset;
                    uint ExtAreaOffset = FooterArea.ExtensionAreaOffset;

                    #region If Dev Area exist, read it.
                    if (DevDirOffset != 0)
                    {
                        stream.Seek(DevDirOffset, SeekOrigin.Begin);
                        DeveloperArea = new DeveloperArea();
                        uint NumberOfTags = Br.ReadUInt16();

                        ushort[] Tags = new ushort[NumberOfTags];
                        uint[] TagOffsets = new uint[NumberOfTags];
                        uint[] TagSizes = new uint[NumberOfTags];

                        for (int i = 0; i < NumberOfTags; i++)
                        {
                            Tags[i] = Br.ReadUInt16();
                            TagOffsets[i] = Br.ReadUInt32();
                            TagSizes[i] = Br.ReadUInt32();
                        }

                        for (int i = 0; i < NumberOfTags; i++)
                        {
                            stream.Seek(TagOffsets[i], SeekOrigin.Begin);
                            var Ent = new DeveloperTag(Tags[i], TagOffsets[i], Br.ReadBytes((int)TagSizes[i]));
                            DeveloperArea.Entries.Add(Ent);
                        }

                        Tags = null;
                        TagOffsets = null;
                        TagSizes = null;
                    }
                    #endregion

                    #region If Ext Area exist, read it.
                    if (ExtAreaOffset != 0)
                    {
                        stream.Seek(ExtAreaOffset, SeekOrigin.Begin);
                        ushort ExtAreaSize = Math.Max((ushort)ExtensionArea.MinSize, Br.ReadUInt16());
                        stream.Seek(ExtAreaOffset, SeekOrigin.Begin);
                        ExtensionArea = new ExtensionArea(Br.ReadBytes(ExtAreaSize));

                        if (ExtensionArea.ScanLineOffset > 0)
                        {
                            stream.Seek(ExtensionArea.ScanLineOffset, SeekOrigin.Begin);
                            ExtensionArea.ScanLineTable = new uint[Height];
                            for (int i = 0; i < ExtensionArea.ScanLineTable.Length; i++)
                                ExtensionArea.ScanLineTable[i] = Br.ReadUInt32();
                        }

                        if (ExtensionArea.PostageStampOffset > 0)
                        {
                            stream.Seek(ExtensionArea.PostageStampOffset, SeekOrigin.Begin);
                            byte W = Br.ReadByte();
                            byte H = Br.ReadByte();
                            int ImgDataSize = W * H * BytesPerPixel;
                            if (ImgDataSize > 0)
                                ExtensionArea.PostageStampImage = new PostageStampImage(W, H, Br.ReadBytes(ImgDataSize));
                        }

                        if (ExtensionArea.ColorCorrectionTableOffset > 0)
                        {
                            stream.Seek(ExtensionArea.ColorCorrectionTableOffset, SeekOrigin.Begin);
                            ExtensionArea.ColorCorrectionTable = new ushort[256 * 4];
                            for (int i = 0; i < ExtensionArea.ColorCorrectionTable.Length; i++)
                                ExtensionArea.ColorCorrectionTable[i] = Br.ReadUInt16();
                        }
                    }
                    #endregion
                }
                #endregion

                Br.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        bool LoadFunc(Bitmap bmp, bool UseRLE = false, bool NewFormat = true, bool ColorMap2BytesEntry = false)
        {
            if (bmp == null) throw new ArgumentNullException();
            try
            {
                HeaderArea.ImageSpec.ImageWidth = (ushort)bmp.Width;
                HeaderArea.ImageSpec.ImageHeight = (ushort)bmp.Height;
                HeaderArea.ImageSpec.ImageDescriptor.ImageOrigin = ImageOrigin.TopLeft;

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
                        throw new FormatException(nameof(PixelFormat) + " is not supported!");

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

                        int bpp = Math.Max(8, System.Drawing.Image.GetPixelFormatSize(bmp.PixelFormat));
                        int BytesPP = bpp / 8;

                        if (bmp.PixelFormat == PixelFormat.Format16bppRgb555)
                            bpp = 15;

                        bool IsAlpha = System.Drawing.Image.IsAlphaPixelFormat(bmp.PixelFormat);
                        bool IsPreAlpha = IsAlpha && bmp.PixelFormat.ToString().EndsWith("PArgb");
                        bool IsColorMapped = bmp.PixelFormat.ToString().EndsWith("Indexed");

                        HeaderArea.ImageSpec.PixelDepth = (PixelDepth)(BytesPP * 8);

                        if (IsAlpha)
                        {
                            HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits = (byte)(BytesPP * 2);

                            if (bmp.PixelFormat == PixelFormat.Format16bppArgb1555)
                                HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits = 1;
                        }

                        #region ColorMap
                        bool IsGrayImage = (bmp.PixelFormat == PixelFormat.Format16bppGrayScale | IsColorMapped);

                        if (IsColorMapped && bmp.Palette != null)
                        {
                            Color[] Colors = bmp.Palette.Entries;

                            #region Analyze ColorMapType
                            int AlphaSum = 0;
                            bool ColorMapUseAlpha = false;

                            for (int i = 0; i < Colors.Length; i++)
                            {
                                IsGrayImage &= (Colors[i].R == Colors[i].G && Colors[i].G == Colors[i].B);
                                ColorMapUseAlpha |= (Colors[i].A < 248);
                                AlphaSum |= Colors[i].A;
                            }
                            ColorMapUseAlpha &= (AlphaSum > 0);

                            int CMapBpp = (ColorMap2BytesEntry ? 15 : 24) + (ColorMapUseAlpha ? (ColorMap2BytesEntry ? 1 : 8) : 0);
                            int CMBytesPP = (int)Math.Ceiling(CMapBpp / 8.0);
                            #endregion

                            HeaderArea.ColorMapSpec.ColorMapLength = Math.Min((ushort)Colors.Length, ushort.MaxValue);
                            HeaderArea.ColorMapSpec.EntrySize = (ColorMapEntrySize)CMapBpp;
                            ImageOrColorMapArea.ColorMapData = new byte[HeaderArea.ColorMapSpec.ColorMapLength * CMBytesPP];

                            byte[] CMapEntry = new byte[CMBytesPP];

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

                                        if (HeaderArea.ColorMapSpec.EntrySize == ColorMapEntrySize.A1R5G5B5)
                                            A = ((Colors[i].A & 0x80) << 15);

                                        CMapEntry = BitConverter.GetBytes(A | R | G | B);
                                        break;

                                    case ColorMapEntrySize.R8G8B8:
                                        CMapEntry[0] = Colors[i].B;
                                        CMapEntry[1] = Colors[i].G;
                                        CMapEntry[2] = Colors[i].R;
                                        break;

                                    case ColorMapEntrySize.A8R8G8B8:
                                        CMapEntry[0] = Colors[i].B;
                                        CMapEntry[1] = Colors[i].G;
                                        CMapEntry[2] = Colors[i].R;
                                        CMapEntry[3] = Colors[i].A;
                                        break;

                                    case ColorMapEntrySize.Other:
                                    default:
                                        break;
                                }

                                Buffer.BlockCopy(CMapEntry, 0, ImageOrColorMapArea.ColorMapData, i * CMBytesPP, CMBytesPP);
                            }
                        }
                        #endregion

                        #region ImageType
                        if (UseRLE)
                        {
                            if (IsGrayImage)
                                HeaderArea.ImageType = ImageType.RLE_BlackWhite;
                            else if (IsColorMapped)
                                HeaderArea.ImageType = ImageType.RLE_ColorMapped;
                            else
                                HeaderArea.ImageType = ImageType.RLE_TrueColor;
                        }
                        else
                        {
                            if (IsGrayImage)
                                HeaderArea.ImageType = ImageType.Uncompressed_BlackWhite;
                            else if (IsColorMapped)
                                HeaderArea.ImageType = ImageType.Uncompressed_ColorMapped;
                            else
                                HeaderArea.ImageType = ImageType.Uncompressed_TrueColor;
                        }

                        HeaderArea.ColorMapType = (IsColorMapped ? ColorMapType.ColorMap : ColorMapType.NoColorMap);
                        #endregion

                        #region NewFormat
                        if (NewFormat)
                        {
                            FooterArea = new FooterArea();
                            ExtensionArea = new ExtensionArea();
                            ExtensionArea.DateTimeStamp = new TimeStamp(DateTime.UtcNow);

                            if (IsAlpha)
                            {
                                ExtensionArea.AttributesType = AttributeType.UsefulAlpha;

                                if (IsPreAlpha)
                                    ExtensionArea.AttributesType = AttributeType.PreMultipliedAlpha;
                            }
                            else
                            {
                                ExtensionArea.AttributesType = AttributeType.NoAlpha;

                                if (HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits > 0)
                                    ExtensionArea.AttributesType = AttributeType.UndefinedAlphaButShouldBeRetained;
                            }
                        }
                        #endregion

                        #region Bitmap width is aligned by 32 bits = 4 bytes! Delete it.
                        int StrideBytes = bmp.Width * BytesPP;
                        int PaddingBytes = (int)Math.Ceiling(StrideBytes / 4.0) * 4 - StrideBytes;

                        byte[] ImageData = new byte[(StrideBytes + PaddingBytes) * bmp.Height];

                        Rectangle Re = new Rectangle(0, 0, bmp.Width, bmp.Height);
                        BitmapData BmpData = bmp.LockBits(Re, ImageLockMode.ReadOnly, bmp.PixelFormat);
                        Marshal.Copy(BmpData.Scan0, ImageData, 0, ImageData.Length);
                        bmp.UnlockBits(BmpData);
                        BmpData = null;

                        if (PaddingBytes > 0) //Need delete bytes align
                        {
                            ImageOrColorMapArea.ImageData = new byte[StrideBytes * bmp.Height];
                            for (int i = 0; i < bmp.Height; i++)
                                Buffer.BlockCopy(ImageData, i * (StrideBytes + PaddingBytes),
                                    ImageOrColorMapArea.ImageData, i * StrideBytes, StrideBytes);
                        }
                        else
                            ImageOrColorMapArea.ImageData = ImageData;

                        ImageData = null;

                        // Not official supported, but works (tested on 16bpp GrayScale test images)!
                        if (bmp.PixelFormat == PixelFormat.Format16bppGrayScale)
                        {
                            for (long i = 0; i < ImageOrColorMapArea.ImageData.Length; i++)
                                ImageOrColorMapArea.ImageData[i] ^= byte.MaxValue;
                        }
                        #endregion

                        break;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        bool SaveFunc(Stream stream)
        {
            try
            {
                if (stream == null)
                    throw new ArgumentNullException();
                if (!(stream.CanWrite && stream.CanSeek))
                    throw new FileLoadException("Stream writing or seeking is not avaiable!");

                string CheckResult;
                if (!CheckAndUpdateOffsets(out CheckResult))
                    return false;

                BinaryWriter Bw = new BinaryWriter(stream);
                Bw.Write(HeaderArea.ToBytes());

                if (ImageOrColorMapArea.ImageId != null)
                    Bw.Write(ImageOrColorMapArea.ImageId.ToBytes());

                if (HeaderArea.ColorMapType != ColorMapType.NoColorMap)
                    Bw.Write(ImageOrColorMapArea.ColorMapData);

                #region ImageData
                if (HeaderArea.ImageType != ImageType.NoImageData)
                {
                    if (HeaderArea.ImageType >= ImageType.RLE_ColorMapped &&
                        HeaderArea.ImageType <= ImageType.RLE_BlackWhite)
                        Bw.Write(RLE_Encode(ImageOrColorMapArea.ImageData, Width, Height));
                    else
                        Bw.Write(ImageOrColorMapArea.ImageData);
                }
                #endregion

                if (FooterArea != null)
                {
                    if (DeveloperArea != null)
                    {
                        for (int i = 0; i < DeveloperArea.Count; i++)
                            Bw.Write(DeveloperArea[i].Data);

                        Bw.Write((ushort)DeveloperArea.Count);

                        for (int i = 0; i < DeveloperArea.Count; i++)
                        {
                            Bw.Write(DeveloperArea[i].Tag);
                            Bw.Write(DeveloperArea[i].Offset);
                            Bw.Write(DeveloperArea[i].FieldSize);
                        }
                    }

                    if (ExtensionArea != null)
                    {
                        Bw.Write(ExtensionArea.ToBytes());

                        if (ExtensionArea.ScanLineTable != null)
                            for (int i = 0; i < ExtensionArea.ScanLineTable.Length; i++)
                                Bw.Write(ExtensionArea.ScanLineTable[i]);

                        if (ExtensionArea.PostageStampImage != null)
                            Bw.Write(ExtensionArea.PostageStampImage.ToBytes());

                        if (ExtensionArea.ColorCorrectionTable != null)
                            for (int i = 0; i < ExtensionArea.ColorCorrectionTable.Length; i++)
                                Bw.Write(ExtensionArea.ColorCorrectionTable[i]);
                    }
                    Bw.Write(FooterArea.ToBytes());
                }

                Bw.Flush();
                stream.Flush();
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Encode image with RLE compression (used RLE per line)!
        /// </summary>
        /// <param name="ImageData">Image data, bytes array with size = Width * Height * BytesPerPixel.</param>
        /// <param name="Width">Image Width, must be > 0.</param>
        /// <param name="Height">Image Height, must be > 0.</param>
        /// <returns>Bytes array with RLE compressed image data.</returns>
        byte[] RLE_Encode(byte[] ImageData, int Width, int Height)
        {
            if (ImageData == null)
                throw new ArgumentNullException(nameof(ImageData) + "in null!");

            if (Width <= 0 || Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(Width) + " and " + nameof(Height) + " must be > 0!");

            int Bpp = ImageData.Length / Width / Height; // Bytes per pixel
            int ScanLineSize = Width * Bpp;

            if (ScanLineSize * Height != ImageData.Length)
                throw new ArgumentOutOfRangeException("ImageData has wrong Length!");

            try
            {
                int Count = 0;
                int Pos = 0;
                bool IsRLE = false;
                List<byte> Encoded = new List<byte>();
                byte[] RowData = new byte[ScanLineSize];

                for (int y = 0; y < Height; y++)
                {
                    Pos = 0;
                    Buffer.BlockCopy(ImageData, y * ScanLineSize, RowData, 0, ScanLineSize);

                    while (Pos < ScanLineSize)
                    {
                        if (Pos >= ScanLineSize - Bpp)
                        {
                            Encoded.Add(0);
                            Encoded.AddRange(EnumerableHelper.GetElements(RowData, (uint)Pos, (uint)Bpp));
                            Pos += Bpp;
                            break;
                        }

                        Count = 0; //1
                        IsRLE = EnumerableHelper.AreElementsEqual(RowData, Pos, Pos + Bpp, Bpp);

                        for (int i = Pos + Bpp; i < Math.Min(Pos + 128 * Bpp, ScanLineSize) - Bpp; i += Bpp)
                        {
                            if (IsRLE ^ EnumerableHelper.AreElementsEqual(RowData, (IsRLE ? Pos : i), i + Bpp, Bpp))
                            {
                                //Count--;
                                break;
                            }
                            else
                                Count++;
                        }

                        int CountBpp = (Count + 1) * Bpp;
                        Encoded.Add((byte)(IsRLE ? Count | 128 : Count));
                        Encoded.AddRange(EnumerableHelper.GetElements(RowData, (uint)Pos, (uint)(IsRLE ? Bpp : CountBpp)));
                        Pos += CountBpp;
                    }
                }

                return Encoded.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert <see cref="Targa"/> to <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="ForceUseAlpha">Force use alpha channel.</param>
        /// <param name="PostageStampImage">Get Postage Stamp Image (Thumb) or get main image?</param>
        /// <returns>Bitmap or null, on error.</returns>
        Bitmap ToBitmapFunc(bool ForceUseAlpha = false, bool PostageStampImage = false)
        {
            try
            {
                #region UseAlpha?
                bool UseAlpha = true;
                if (ExtensionArea != null)
                {
                    switch (ExtensionArea.AttributesType)
                    {
                        case AttributeType.NoAlpha:
                        case AttributeType.UndefinedAlphaCanBeIgnored:
                        case AttributeType.UndefinedAlphaButShouldBeRetained:
                            UseAlpha = false;
                            break;
                        case AttributeType.UsefulAlpha:
                        case AttributeType.PreMultipliedAlpha:
                        default:
                            break;
                    }
                }
                UseAlpha = (HeaderArea.ImageSpec.ImageDescriptor.AlphaChannelBits > 0 && UseAlpha) | ForceUseAlpha;
                #endregion

                #region IsGrayImage
                bool IsGrayImage = HeaderArea.ImageType == ImageType.RLE_BlackWhite ||
                    HeaderArea.ImageType == ImageType.Uncompressed_BlackWhite;
                #endregion

                #region Get PixelFormat
                PixelFormat PixFormat = PixelFormat.Format24bppRgb;

                switch (HeaderArea.ImageSpec.PixelDepth)
                {
                    case PixelDepth.Bpp8:
                        PixFormat = PixelFormat.Format8bppIndexed;
                        break;

                    case PixelDepth.Bpp16:
                        if (IsGrayImage)
                            PixFormat = PixelFormat.Format16bppGrayScale;
                        else
                            PixFormat = (UseAlpha ? PixelFormat.Format16bppArgb1555 : PixelFormat.Format16bppRgb555);
                        break;

                    case PixelDepth.Bpp24:
                        PixFormat = PixelFormat.Format24bppRgb;
                        break;

                    case PixelDepth.Bpp32:
                        if (UseAlpha)
                        {
                            var f = FooterArea;
                            if (ExtensionArea?.AttributesType == AttributeType.PreMultipliedAlpha)
                                PixFormat = PixelFormat.Format32bppPArgb;
                            else
                                PixFormat = PixelFormat.Format32bppArgb;
                        }
                        else
                            PixFormat = PixelFormat.Format32bppRgb;
                        break;

                    default:
                        PixFormat = PixelFormat.Undefined;
                        break;
                }
                #endregion

                ushort BMP_Width = (PostageStampImage ? ExtensionArea.PostageStampImage.Width : Width);
                ushort BMP_Height = (PostageStampImage ? ExtensionArea.PostageStampImage.Height : Height);
                Bitmap BMP = new Bitmap(BMP_Width, BMP_Height, PixFormat);

                #region ColorMap and GrayPalette
                if (HeaderArea.ColorMapType == ColorMapType.ColorMap &&
                   (HeaderArea.ImageType == ImageType.RLE_ColorMapped ||
                    HeaderArea.ImageType == ImageType.Uncompressed_ColorMapped))
                {

                    ColorPalette ColorMap = BMP.Palette;
                    Color[] CMapColors = ColorMap.Entries;

                    switch (HeaderArea.ColorMapSpec.EntrySize)
                    {
                        case ColorMapEntrySize.X1R5G5B5:
                        case ColorMapEntrySize.A1R5G5B5:
                            const float To8Bit = 255f / 31f; // Scale value from 5 to 8 bits.
                            for (int i = 0; i < Math.Min(CMapColors.Length, HeaderArea.ColorMapSpec.ColorMapLength); i++)
                            {
                                ushort A1R5G5B5 = BitConverter.ToUInt16(ImageOrColorMapArea.ColorMapData, i * 2);
                                int A = (UseAlpha ? (A1R5G5B5 & 0x8000) >> 15 : 1) * 255; // (0 or 1) * 255
                                int R = (int)(((A1R5G5B5 & 0x7C00) >> 10) * To8Bit);
                                int G = (int)(((A1R5G5B5 & 0x3E0) >> 5) * To8Bit);
                                int B = (int)((A1R5G5B5 & 0x1F) * To8Bit);
                                CMapColors[i] = Color.FromArgb(A, R, G, B);
                            }
                            break;

                        case ColorMapEntrySize.R8G8B8:
                            for (int i = 0; i < Math.Min(CMapColors.Length, HeaderArea.ColorMapSpec.ColorMapLength); i++)
                            {
                                int Index = i * 3; //RGB = 3 bytes
                                int R = ImageOrColorMapArea.ColorMapData[Index + 2];
                                int G = ImageOrColorMapArea.ColorMapData[Index + 1];
                                int B = ImageOrColorMapArea.ColorMapData[Index];
                                CMapColors[i] = Color.FromArgb(R, G, B);
                            }
                            break;

                        case ColorMapEntrySize.A8R8G8B8:
                            for (int i = 0; i < Math.Min(CMapColors.Length, HeaderArea.ColorMapSpec.ColorMapLength); i++)
                            {
                                int ARGB = BitConverter.ToInt32(ImageOrColorMapArea.ColorMapData, i * 4);
                                CMapColors[i] = Color.FromArgb(UseAlpha ? ARGB | (0xFF << 24) : ARGB);
                            }
                            break;

                        default:
                            ColorMap = null;
                            break;
                    }

                    if (ColorMap != null)
                        BMP.Palette = ColorMap;
                }

                if (PixFormat == PixelFormat.Format8bppIndexed && IsGrayImage)
                {
                    ColorPalette GrayPalette = BMP.Palette;
                    Color[] GrayColors = GrayPalette.Entries;
                    for (int i = 0; i < GrayColors.Length; i++)
                        GrayColors[i] = Color.FromArgb(i, i, i);
                    BMP.Palette = GrayPalette;
                }
                #endregion

                #region Bitmap width must by aligned (align value = 32 bits = 4 bytes)!
                byte[] ImageData;
                int BytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ImageSpec.PixelDepth / 8.0);
                int StrideBytes = BMP.Width * BytesPerPixel;
                int PaddingBytes = (int)Math.Ceiling(StrideBytes / 4.0) * 4 - StrideBytes;

                if (PaddingBytes > 0) //Need bytes align
                {
                    ImageData = new byte[(StrideBytes + PaddingBytes) * BMP.Height];
                    for (int i = 0; i < BMP.Height; i++)
                        Buffer.BlockCopy(PostageStampImage ? ExtensionArea.PostageStampImage.Data :
                            ImageOrColorMapArea.ImageData, i * StrideBytes, ImageData,
                            i * (StrideBytes + PaddingBytes), StrideBytes);
                }
                else
                    ImageData = ByteHelper.ToBytes(PostageStampImage ? ExtensionArea.PostageStampImage.Data : ImageOrColorMapArea.ImageData);

                // Not official supported, but works (tested on 2 test images)!
                if (PixFormat == PixelFormat.Format16bppGrayScale)
                {
                    for (long i = 0; i < ImageData.Length; i++)
                        ImageData[i] ^= byte.MaxValue;
                }
                #endregion

                Rectangle Re = new Rectangle(0, 0, BMP.Width, BMP.Height);
                BitmapData BmpData = BMP.LockBits(Re, ImageLockMode.WriteOnly, BMP.PixelFormat);
                Marshal.Copy(ImageData, 0, BmpData.Scan0, ImageData.Length);
                BMP.UnlockBits(BmpData);
                ImageData = null;
                BmpData = null;

                if (ExtensionArea != null && ExtensionArea.KeyColor.ToInt() != 0)
                    BMP.MakeTransparent(ExtensionArea.KeyColor.ToColor());

                #region Flip Image
                switch (HeaderArea.ImageSpec.ImageDescriptor.ImageOrigin)
                {
                    case ImageOrigin.BottomLeft:
                        BMP.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        break;
                    case ImageOrigin.BottomRight:
                        BMP.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                        break;
                    case ImageOrigin.TopLeft:
                    default:
                        break;
                    case ImageOrigin.TopRight:
                        BMP.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        break;
                }
                #endregion

                return BMP;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region Explicit
        public static explicit operator Bitmap(Targa tga)
        {
            return tga.ToBitmap();
        }

        public static explicit operator Targa(Bitmap bmp)
        {
            return FromBitmap(bmp);
        }
        #endregion

        #region PostageStamp Image
        /// <summary>
        /// Convert <see cref="PostageStampImage"/> to <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="ForceUseAlpha">Force use alpha channel.</param>
        /// <returns>Bitmap or null.</returns>
        public Bitmap GetPostageStampImage(bool ForceUseAlpha = false)
        {
            if (ExtensionArea?.PostageStampImage?.Data == null || (ExtensionArea?.PostageStampImage?.Width ?? 0) <= 0 || (ExtensionArea?.PostageStampImage?.Height ?? 0) <= 0)
                return null;
            return ToBitmapFunc(ForceUseAlpha, true);
        }

        /// <summary>
        /// Update Postage Stamp Image or set it.
        /// </summary>
        public void UpdatePostageStampImage()
        {
            if (HeaderArea.ImageType == ImageType.NoImageData)
            {
                if (ExtensionArea != null)
                    ExtensionArea.PostageStampImage = null;
                return;
            }

            ToNewFormat();
            if (ExtensionArea.PostageStampImage == null)
                ExtensionArea.PostageStampImage = new PostageStampImage();

            int PS_Width = HeaderArea.ImageSpec.ImageWidth;
            int PS_Height = HeaderArea.ImageSpec.ImageHeight;

            if (Width > 64 || Height > 64)
            {
                float AspectRatio = Width / (float)Height;
                PS_Width = (byte)(64f * (AspectRatio < 1f ? AspectRatio : 1f));
                PS_Height = (byte)(64f / (AspectRatio > 1f ? AspectRatio : 1f));
            }
            PS_Width = Math.Max(PS_Width, 4);
            PS_Height = Math.Max(PS_Height, 4);

            ExtensionArea.PostageStampImage.Width = (byte)PS_Width;
            ExtensionArea.PostageStampImage.Height = (byte)PS_Height;

            int BytesPerPixel = (int)Math.Ceiling((double)HeaderArea.ImageSpec.PixelDepth / 8.0);
            ExtensionArea.PostageStampImage.Data = new byte[PS_Width * PS_Height * BytesPerPixel];

            float WidthCoef = Width / (float)PS_Width;
            float HeightCoef = Height / (float)PS_Height;

            for (int y = 0; y < PS_Height; y++)
            {
                int Y_Offset = (int)(y * HeightCoef) * Width * BytesPerPixel;
                int y_Offset = y * PS_Width * BytesPerPixel;

                for (int x = 0; x < PS_Width; x++)
                {
                    Buffer.BlockCopy(ImageOrColorMapArea.ImageData, Y_Offset + (int)(x * WidthCoef) * BytesPerPixel,
                        ExtensionArea.PostageStampImage.Data, y_Offset + x * BytesPerPixel, BytesPerPixel);
                }
            }
        }

        public void DeletePostageStampImage()
        {
            if (ExtensionArea != null) ExtensionArea.PostageStampImage = null;
        }
        #endregion
    }
}