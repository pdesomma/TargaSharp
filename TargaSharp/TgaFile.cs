using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace TargaSharp
{
    public class TgaFile : ICloneable
    {
        public TgaHeader Header { get; private set; } = new TgaHeader();
        public TgaImgOrColMap ImageOrColorMapArea { get; private set; } = new TgaImgOrColMap();
        public TgaDevArea? DevArea { get; private set; } = null;
        public TgaExtArea? ExtArea { get; private set; } = null;
        public TgaFooter? Footer { get; private set; } = null;

        /// <summary>
        /// Create new empty <see cref="TgaFile"/> istance.
        /// </summary>
        public TgaFile() { }

        /// <summary>
        /// Create <see cref="TgaFile"/> instance with some params. If it must have ColorMap,
        /// check all ColorMap fields and settings after. Color-mapped images (<see cref="TgaImageType.Uncompressed_ColorMapped"/>
        /// or <see cref="TgaImageType.RLE_ColorMapped"/>) default to 24-bit (<see cref="TgaColorMapEntrySize.R8G8B8"/>)
        /// palette entries; the palette length and entry data themselves are left for the caller to set afterward.
        /// </summary>
        /// <param name="width">Image Width.</param>
        /// <param name="height">Image Height.</param>
        /// <param name="pixDepth">Image Pixel Depth (bits / pixel), set ColorMap bpp after, if needed!</param>
        /// <param name="imgType">Image Type (is RLE compressed, ColorMapped or GrayScaled).</param>
        /// <param name="attrBits">Set numder of Attrbute bits (Alpha channel bits), default: 0, 1, 8.</param>
        /// <param name="newFormat">Use new 2.0 TGA XFile format?</param>
        public TgaFile(ushort width, ushort height, TgaPixelDepth pixDepth = TgaPixelDepth.Bpp24, TgaImageType imgType = TgaImageType.Uncompressed_TrueColor, byte attrBits = 0, bool newFormat = true)
        {
            if (width <= 0 || height <= 0 || pixDepth == TgaPixelDepth.Other)
            {
                width = height = 0;
                pixDepth = TgaPixelDepth.Other;
                imgType = TgaImageType.NoImageData;
                attrBits = 0;
            }
            else
            {
                int BytesPerPixel = (int)Math.Ceiling((double)pixDepth / 8.0);
                ImageOrColorMapArea.ImageData = new byte[width * height * BytesPerPixel];

                if (imgType == TgaImageType.Uncompressed_ColorMapped || imgType == TgaImageType.RLE_ColorMapped)
                {
                    Header.ColorMapType = TgaColorMapType.ColorMap;
                    Header.ColorMapSpec.FirstEntryIndex = 0;
                    // Default color-mapped images to 24-bit (R8G8B8) palette entries, a valid TgaColorMapEntrySize.
                    // pixDepth is the *indexed pixel* depth, not the palette entry depth, so it must not be used
                    // here directly (e.g. Bpp8 => ceil(8/8) = 1, which is not a valid entry size). The palette's
                    // length and actual entry data are left for the caller to fill in.
                    Header.ColorMapSpec.ColorMapEntrySize = TgaColorMapEntrySize.R8G8B8;
                }
            }

            Header.ImageType = imgType;
            Header.ImageSpec.ImageWidth = width;
            Header.ImageSpec.ImageHeight = height;
            Header.ImageSpec.PixelDepth = pixDepth;
            Header.ImageSpec.ImageDescriptor.AlphaChannelBits = attrBits;

            if (newFormat)
            {
                Footer = new TgaFooter();
                ExtArea = new TgaExtArea
                {
                    DateTimeStamp = new TgaDateTime(DateTime.UtcNow),
                    AttributesType = (attrBits > 0 ? TgaAttributeType.UsefulAlpha : TgaAttributeType.NoAlpha)
                };
            }
        }

        /// <summary>
        /// Make <see cref="TgaFile"/> from another <see cref="TgaFile"/> instance.
        /// Equal to <see cref="TgaFile.Clone()"/> function.
        /// </summary>
        /// <param name="tga">Original <see cref="TgaFile"/> instance.</param>
        public TgaFile(TgaFile tga)
        {
            Header = tga.Header.Clone();
            ImageOrColorMapArea = tga.ImageOrColorMapArea.Clone();
            DevArea = tga.DevArea?.Clone();
            ExtArea = tga.ExtArea?.Clone();
            Footer = tga.Footer?.Clone();
        }
        /// <summary>
        /// Load <see cref="TgaFile"/> from file.
        /// </summary>
        /// <param name="filename">Full path to TGA file.</param>
        /// <returns>Loaded <see cref="TgaFile"/> file.</returns>
        public TgaFile(string filename) => LoadFunc(filename);
        /// <summary>
        /// Make <see cref="TgaFile"/> from bytes array.
        /// </summary>
        /// <param name="bytes">Bytes array (same like TGA File).</param>
        public TgaFile(byte[] bytes) => LoadFunc(bytes);
        /// <summary>
        /// Make <see cref="TgaFile"/> from <see cref="Stream"/>.
        /// For file opening better use <see cref="TgaFile(string)"/>.
        /// </summary>
        /// <param name="stream">Some stream. You can use a lot of Stream types, but Stream must support:
        /// <see cref="Stream.CanSeek"/> and <see cref="Stream.CanRead"/>.</param>
        public TgaFile(Stream stream) => LoadFunc(stream);
        /// <summary>
        /// Make <see cref="TgaFile"/> from <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="bmp">Input Bitmap, supported a lot of bitmaps types: 8/15/16/24/32 Bpp's.</param>
        /// <param name="UseRLE">Use RLE Compression?</param>
        /// <param name="NewFormat">Use new 2.0 TGA XFile format?</param>
        /// <param name="ColorMap2BytesEntry">Is Color Map Entry size equal 15 or 16 Bpp, else - 24 or 32.</param>
        public TgaFile(Bitmap bmp, bool UseRLE = false, bool NewFormat = true, bool ColorMap2BytesEntry = false) => LoadFunc(bmp, UseRLE, NewFormat, ColorMap2BytesEntry);


        /// <summary>
        /// Gets or Sets Image Height (see <see cref="Header.ImageSpec.ImageHeight"/>).
        /// </summary>
        public ushort Height
        {
            get { return Header.ImageSpec.ImageHeight; }
            set { Header.ImageSpec.ImageHeight = value; }
        }
        /// <summary>
        /// Gets or Sets <see cref="TgaFile"/> image Size.
        /// </summary>
        public Size Size
        {
            get { return new Size(Header.ImageSpec.ImageWidth, Header.ImageSpec.ImageHeight); }
            set
            {
                Header.ImageSpec.ImageWidth = (ushort)value.Width;
                Header.ImageSpec.ImageHeight = (ushort)value.Height;
            }
        }
        /// <summary>
        /// Gets or Sets Image Width (see <see cref="Header.ImageSpec.ImageWidth"/>).
        /// </summary>
        public ushort Width
        {
            get { return Header.ImageSpec.ImageWidth; }
            set { Header.ImageSpec.ImageWidth = value; }
        }


        /// <summary>
        /// Make full independed copy of <see cref="TgaFile"/>.
        /// </summary>
        /// <returns>Full independed copy of <see cref="TgaFile"/>.</returns>
        public TgaFile Clone() => new TgaFile(this);
        object ICloneable.Clone() => Clone();

        /// <summary>
        /// Flip <see cref="TgaFile"/> directions, for more info see <see cref="TgaImageOrigin"/>.
        /// </summary>
        /// <param name="Horizontal">Flip horizontal.</param>
        /// <param name="Vertical">Flip vertical.</param>
        public void Flip(bool Horizontal = false, bool Vertical = false)
        {
            int NewOrigin = (int)Header.ImageSpec.ImageDescriptor.ImageOrigin;
            NewOrigin = NewOrigin ^ ((Vertical ? 0x20 : 0) | (Horizontal ? 0x10 : 0));
            Header.ImageSpec.ImageDescriptor.ImageOrigin = (TgaImageOrigin)NewOrigin;
        }

        /// <summary>
        /// Get information from TGA image.
        /// </summary>
        /// <returns>MultiLine string with info fields (one per line).</returns>
        public string GetInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Header:");
            sb.AppendLine("\tID Length = " + Header.IdLength);
            sb.AppendLine("\tImage Type = " + Header.ImageType);
            sb.AppendLine("\tHeader -> ImageSpec:");
            sb.AppendLine("\t\tImage Width = " + Header.ImageSpec.ImageWidth);
            sb.AppendLine("\t\tImage Height = " + Header.ImageSpec.ImageHeight);
            sb.AppendLine("\t\tPixel Depth = " + Header.ImageSpec.PixelDepth);
            sb.AppendLine("\t\tImage Descriptor (AsByte) = " + Header.ImageSpec.ImageDescriptor.ToByte());
            sb.AppendLine("\t\tImage Descriptor -> AttributeBits = " + Header.ImageSpec.ImageDescriptor.AlphaChannelBits);
            sb.AppendLine("\t\tImage Descriptor -> ImageOrigin = " + Header.ImageSpec.ImageDescriptor.ImageOrigin);
            sb.AppendLine("\t\tX_Origin = " + Header.ImageSpec.XOrigin);
            sb.AppendLine("\t\tY_Origin = " + Header.ImageSpec.YOrigin);
            sb.AppendLine("\tColorMap Type = " + Header.ColorMapType);
            sb.AppendLine("\tHeader -> ColorMapSpec:");
            sb.AppendLine("\t\tColorMap Entry Size = " + Header.ColorMapSpec.ColorMapEntrySize);
            sb.AppendLine("\t\tColorMap Length = " + Header.ColorMapSpec.ColorMapLength);
            sb.AppendLine("\t\tFirstEntry Index = " + Header.ColorMapSpec.FirstEntryIndex);

            sb.AppendLine("\nImage / Color Map Area:");
            if (Header.IdLength > 0 && ImageOrColorMapArea?.ImageID is not null)
                sb.AppendLine("\tImage ID = \"" + ImageOrColorMapArea.ImageID.GetString() + "\"");
            else
                sb.AppendLine("\tImage ID = null");

            if (ImageOrColorMapArea?.ImageData is not null)
                sb.AppendLine("\tImage Data Length = " + ImageOrColorMapArea.ImageData.Length);
            else
                sb.AppendLine("\tImage Data = null");

            if (ImageOrColorMapArea?.ColorMapData != null)
                sb.AppendLine("\tColorMap Data Length = " + ImageOrColorMapArea.ColorMapData.Length);
            else
                sb.AppendLine("\tColorMap Data = null");

            sb.AppendLine("\nDevelopers Area:\tCount = " + DevArea?.Count ?? "null");

            sb.AppendLine("\nExtension Area:");
            if (ExtArea is not null)
            {
                sb.AppendLine("\tExtension Size = " + ExtArea.ExtensionSize);
                sb.AppendLine("\tAuthor Name = \"" + ExtArea.AuthorName.GetString() + "\"");
                sb.AppendLine("\tAuthor Comments = \"" + ExtArea.AuthorComments.GetString() + "\"");
                sb.AppendLine("\tDate / Time Stamp = " + ExtArea.DateTimeStamp);
                sb.AppendLine("\tJob Name / ID = \"" + ExtArea.JobNameOrID.GetString() + "\"");
                sb.AppendLine("\tJob Time = " + ExtArea.JobTime);
                sb.AppendLine("\tSoftware ID = \"" + ExtArea.SoftwareID.GetString() + "\"");
                sb.AppendLine("\tSoftware Version = \"" + ExtArea.SoftVersion + "\"");
                sb.AppendLine("\tKey Color = " + ExtArea.KeyColor);
                sb.AppendLine("\tPixel Aspect Ratio = " + ExtArea.PixelAspectRatio);
                sb.AppendLine("\tGamma Value = " + ExtArea.GammaValue);
                sb.AppendLine("\tColor Correction Table Offset = " + ExtArea.ColorCorrectionTableOffset);
                sb.AppendLine("\tPostage Stamp Offset = " + ExtArea.PostageStampOffset);
                sb.AppendLine("\tScan Line Offset = " + ExtArea.ScanLineOffset);
                sb.AppendLine("\tAttributes Type = " + ExtArea.AttributesType);
                sb.AppendLine("\tScan Line Table = " + ExtArea.ScanLineTable?.Length ?? "null");
                sb.AppendLine("\tPostage Stamp Image = " + ExtArea.PostageStampImage?.ToString() ?? "null");
                sb.AppendLine("\tColor Correction Table = " + (ExtArea.ColorCorrectionTable != null));
            }
            else
                sb.AppendLine("\tExtArea = null");

            sb.AppendLine("\nFooter:");
            if (Footer is not null)
            {
                sb.AppendLine("\tExtension Area Offset = " + Footer.ExtensionAreaOffset);
                sb.AppendLine("\tDeveloper Directory Offset = " + Footer.DeveloperDirectoryOffset);
                sb.AppendLine("\tSignature (Full) = \"" + Footer.Signature.ToString() +
                    Footer.ReservedCharacter.ToString() + Footer.BinaryZeroStringTerminator.ToString() + "\"");
            }
            else
                sb.AppendLine("\tFooter = null");

            return sb.ToString();
        }

        /// <summary>
        /// Check and update all fields with data length and offsets.
        /// </summary>
        /// <returns>Return "true", if all OK or "false", if checking failed.</returns>
        public bool CheckAndUpdateOffsets(out string ErrorStr)
        {
            ErrorStr = string.Empty;
            if (Header is null)
            {
                ErrorStr = "Header = null";
                return false;
            }

            if (ImageOrColorMapArea is null)
            {
                ErrorStr = "ImageOrColorMapArea = null";
                return false;
            }

            uint Offset = TgaHeader.Size; // Virtual Offset

            if (ImageOrColorMapArea?.ImageID is not null)
            {
                int StrMaxLen = 255;
                if (ImageOrColorMapArea.ImageID.UseEndingChar)
                    StrMaxLen--;

                Header.IdLength = (byte)Math.Min(ImageOrColorMapArea.ImageID.OriginalString.Length, StrMaxLen);
                ImageOrColorMapArea.ImageID.Length = Header.IdLength;
                Offset += Header.IdLength;
            }
            else
                Header.IdLength = 0;
            

            
            if (Header.ColorMapType != TgaColorMapType.NoColorMap)
            {
                if (Header.ColorMapSpec is null)
                {
                    ErrorStr = "Header.ColorMapSpec = null";
                    return false;
                }

                if (Header.ColorMapSpec.ColorMapLength == 0)
                {
                    ErrorStr = "Header.ColorMapSpec.ColorMapLength = 0";
                    return false;
                }

                if (ImageOrColorMapArea?.ColorMapData == null)
                {
                    ErrorStr = "ImageOrColorMapArea.ColorMapData = null";
                    return false;
                }

                int CmBytesPerPixel = (int)Math.Ceiling((double)Header.ColorMapSpec.ColorMapEntrySize / 8.0);
                int LenBytes = Header.ColorMapSpec.ColorMapLength * CmBytesPerPixel;

                if (LenBytes != ImageOrColorMapArea.ColorMapData.Length)
                {
                    ErrorStr = "ImageOrColorMapArea.ColorMapData.Length has wrong size!";
                    return false;
                }

                Offset += (uint)ImageOrColorMapArea.ColorMapData.Length;
            }
            
            int BytesPerPixel = 0;
            if (Header.ImageType != TgaImageType.NoImageData)
            {
                if (Header.ImageSpec is null)
                {
                    ErrorStr = "Header.ImageSpec = null";
                    return false;
                }

                if (Header.ImageSpec.ImageWidth == 0 || Header.ImageSpec.ImageHeight == 0)
                {
                    ErrorStr = "Header.ImageSpec.ImageWidth = 0 or Header.ImageSpec.ImageHeight = 0";
                    return false;
                }

                if (ImageOrColorMapArea?.ImageData == null)
                {
                    ErrorStr = "ImageOrColorMapArea.ImageData = null";
                    return false;
                }

                BytesPerPixel = (int)Math.Ceiling((double)Header.ImageSpec.PixelDepth / 8.0);
                if (Width * Height * BytesPerPixel != ImageOrColorMapArea.ImageData.Length)
                {
                    ErrorStr = "ImageOrColorMapArea.ImageData.Length has wrong size!";
                    return false;
                }

                if (Header.ImageType >= TgaImageType.RLE_ColorMapped &&
                    Header.ImageType <= TgaImageType.RLE_BlackWhite)
                {
                    byte[]? RLE = RLE_Encode(ImageOrColorMapArea.ImageData, Width, Height);
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
            
            
            if (Footer is not null)
            {
                if (DevArea is not null)
                {
                    int DevAreaCount = DevArea.Count;
                    for (int i = 0; i < DevAreaCount; i++)
                        if (DevArea[i] is null || DevArea[i].FieldSize <= 0) //Del Empty Entries
                        {
                            DevArea.Entries.RemoveAt(i);
                            DevAreaCount--;
                            i--;
                        }

                    if (DevArea.Count <= 0) Footer.DeveloperDirectoryOffset = 0;

                    // Need at least 2 entries for a duplicate Tag to even be possible; the loop below compares
                    // each adjacent (sorted) pair, so it also correctly catches a duplicate in a 2-entry directory.
                    if (DevArea.Count > 1)
                    {
                        DevArea.Entries.Sort((a, b) => { return a.Tag.CompareTo(b.Tag); });
                        for (int i = 0; i < DevArea.Count - 1; i++)
                            if (DevArea[i].Tag == DevArea[i + 1].Tag)
                            {
                                ErrorStr = "DevArea Enties has same Tags!";
                                return false;
                            }
                    }

                    for (int i = 0; i < DevArea.Count; i++)
                    {
                        DevArea[i].Offset = Offset;
                        Offset += (uint)DevArea[i].FieldSize;
                    }

                    Footer.DeveloperDirectoryOffset = Offset;
                    Offset += (uint)(DevArea.Count * 10 + 2);
                }
                else
                    Footer.DeveloperDirectoryOffset = 0;



                if (ExtArea is not null)
                {
                    ExtArea.ExtensionSize = TgaExtArea.MinSize;
                    if (ExtArea.OtherDataInExtensionArea != null)
                        ExtArea.ExtensionSize += (ushort)ExtArea.OtherDataInExtensionArea.Length;

                    ExtArea.DateTimeStamp = new TgaDateTime(DateTime.UtcNow);

                    Footer.ExtensionAreaOffset = Offset;
                    Offset += ExtArea.ExtensionSize;

                    #region ScanLineTable
                    if (ExtArea.ScanLineTable == null)
                        ExtArea.ScanLineOffset = 0;
                    else
                    {
                        if (ExtArea.ScanLineTable.Length != Height)
                        {
                            ErrorStr = "ExtArea.ScanLineTable.Length != Height";
                            return false;
                        }

                        ExtArea.ScanLineOffset = Offset;
                        Offset += (uint)(ExtArea.ScanLineTable.Length * 4);
                    }


                    if (ExtArea.PostageStampImage is null)
                        ExtArea.PostageStampOffset = 0;
                    else
                    {
                        if (ExtArea.PostageStampImage.Width == 0 || ExtArea.PostageStampImage.Height == 0)
                        {
                            ErrorStr = "ExtArea.PostageStampImage Width or Height is equal 0!";
                            return false;
                        }

                        if (ExtArea.PostageStampImage.Data == null)
                        {
                            ErrorStr = "ExtArea.PostageStampImage.Data == null";
                            return false;
                        }

                        int PImgSB = ExtArea.PostageStampImage.Width * ExtArea.PostageStampImage.Height * BytesPerPixel;
                        if (Header.ImageType != TgaImageType.NoImageData &&
                            ExtArea.PostageStampImage.Data.Length != PImgSB)
                        {
                            ErrorStr = "ExtArea.PostageStampImage.Data.Length is wrong!";
                            return false;
                        }


                        ExtArea.PostageStampOffset = Offset;
                        Offset += (uint)(ExtArea.PostageStampImage.Data.Length);
                    }


                    if (ExtArea.ColorCorrectionTable == null)
                        ExtArea.ColorCorrectionTableOffset = 0;
                    else
                    {
                        if (ExtArea.ColorCorrectionTable.Length != 1024)
                        {
                            ErrorStr = "ExtArea.ColorCorrectionTable.Length != 256 * 4";
                            return false;
                        }

                        ExtArea.ColorCorrectionTableOffset = Offset;
                        Offset += (uint)(ExtArea.ColorCorrectionTable.Length * 2);
                    }
                }
                else
                    Footer.ExtensionAreaOffset = 0;
                
                
                
                if (Footer.ToBytes().Length != TgaFooter.Size)
                {
                    ErrorStr = "Footer.Length is wrong!";
                    return false;
                }
                Offset += TgaFooter.Size;                
            }
            return true;
        }

        /// <summary>
        /// Save the <see cref="TgaFile"/> to disk.
        /// </summary>
        /// <param name="filename">Full path to file.</param>
        /// <returns>Return "true", if all done or "false", if failed.</returns>
        public bool Save(string filename)
        {
            try
            {
                using var Fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                using var Ms = new MemoryStream();
                var result = SaveFunc(Ms);
                Ms.WriteTo(Fs);
                Fs.Flush();
                return result;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Save <see cref="TgaFile"/> to <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Some stream, it must support: <see cref="Stream.CanWrite"/>.</param>
        /// <returns>Return "true", if all done or "false", if failed.</returns>
        public bool Save(Stream stream) => SaveFunc(stream);

        /// <summary>
        /// Convert <see cref="TgaFile"/> to <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="forceUseAlpha">Force use alpha channel.</param>
        /// <returns>Bitmap or null, on error.</returns>
        public Bitmap ToBitmap(bool forceUseAlpha = false) => ToBitmapFunc(forceUseAlpha, false);

        /// <summary>
        /// Convert <see cref="TgaFile"/> to bytes array.
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
            Footer ??= new TgaFooter();

            if (ExtArea is null)
            {
                ExtArea = new TgaExtArea();
                ExtArea.DateTimeStamp = new TgaDateTime(DateTime.UtcNow);

                if (Header.ImageSpec.ImageDescriptor.AlphaChannelBits > 0)
                    ExtArea.AttributesType = TgaAttributeType.UsefulAlpha;
                else
                    ExtArea.AttributesType = TgaAttributeType.NoAlpha;
            }
        }
        
        
        bool LoadFunc(string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("File: \"" + filename + "\" not found!");

            using (FileStream FS = new FileStream(filename, FileMode.Open))
                return LoadFunc(FS);
        }

        bool LoadFunc(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException();

            using (MemoryStream FS = new MemoryStream(bytes, false))
                return LoadFunc(FS);
        }

        bool LoadFunc(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException();
            if (!(stream.CanRead && stream.CanSeek)) throw new FileLoadException("Stream reading or seeking is not avaiable!");

            stream.Seek(0, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(stream);

            Header = new TgaHeader(binaryReader.ReadBytes(TgaHeader.Size));

            if (Header.IdLength > 0)
                ImageOrColorMapArea.ImageID = new TgaString(binaryReader.ReadBytes(Header.IdLength));

            if (Header.ColorMapSpec.ColorMapLength > 0)
            {
                int CmBytesPerPixel = (int)Math.Ceiling((double)Header.ColorMapSpec.ColorMapEntrySize / 8.0);
                int LenBytes = Header.ColorMapSpec.ColorMapLength * CmBytesPerPixel;
                ImageOrColorMapArea.ColorMapData = binaryReader.ReadBytes(LenBytes);
            }

            #region Read Image Data
            int BytesPerPixel = (int)Math.Ceiling((double)Header.ImageSpec.PixelDepth / 8.0);
            if (Header.ImageType != TgaImageType.NoImageData)
            {
                int ImageDataSize = Width * Height * BytesPerPixel;
                switch (Header.ImageType)
                {
                    case TgaImageType.RLE_ColorMapped:
                    case TgaImageType.RLE_TrueColor:
                    case TgaImageType.RLE_BlackWhite:

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

                    case TgaImageType.Uncompressed_ColorMapped:
                    case TgaImageType.Uncompressed_TrueColor:
                    case TgaImageType.Uncompressed_BlackWhite:
                        ImageOrColorMapArea.ImageData = binaryReader.ReadBytes(ImageDataSize);
                        break;
                }
            }
            #endregion

            #region Try parse Footer
            stream.Seek(-TgaFooter.Size, SeekOrigin.End);
            uint FooterOffset = (uint)stream.Position;
            TgaFooter MbFooter = new TgaFooter(binaryReader.ReadBytes(TgaFooter.Size));
            if (MbFooter.IsFooterCorrect)
            {
                Footer = MbFooter;
                uint DevDirOffset = Footer.DeveloperDirectoryOffset;
                uint ExtAreaOffset = Footer.ExtensionAreaOffset;

                #region If Dev Area exist, read it.
                if (DevDirOffset != 0)
                {
                    stream.Seek(DevDirOffset, SeekOrigin.Begin);
                    DevArea = new TgaDevArea();
                    uint NumberOfTags = binaryReader.ReadUInt16();

                    ushort[] Tags = new ushort[NumberOfTags];
                    uint[] TagOffsets = new uint[NumberOfTags];
                    uint[] TagSizes = new uint[NumberOfTags];

                    for (int i = 0; i < NumberOfTags; i++)
                    {
                        Tags[i] = binaryReader.ReadUInt16();
                        TagOffsets[i] = binaryReader.ReadUInt32();
                        TagSizes[i] = binaryReader.ReadUInt32();
                    }

                    for (int i = 0; i < NumberOfTags; i++)
                    {
                        stream.Seek(TagOffsets[i], SeekOrigin.Begin);
                        var Ent = new TgaDevEntry(Tags[i], TagOffsets[i], binaryReader.ReadBytes((int)TagSizes[i]));
                        DevArea.Entries.Add(Ent);
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
                    ushort ExtAreaSize = binaryReader.ReadUInt16();

                    // Per spec the Extension Area Size field must be 495 for a TGA 2.0 extension area.
                    // A reader should only parse what it understands: a declared size smaller than
                    // TgaExtArea.MinSize is not a (valid or forward-compatible) v2.0 ext area, so skip
                    // parsing it instead of forcing a read past what was actually declared/written.
                    if (ExtAreaSize >= TgaExtArea.MinSize)
                    {
                        stream.Seek(ExtAreaOffset, SeekOrigin.Begin);
                        ExtArea = new TgaExtArea(binaryReader.ReadBytes(ExtAreaSize));

                        if (ExtArea.ScanLineOffset > 0)
                        {
                            stream.Seek(ExtArea.ScanLineOffset, SeekOrigin.Begin);
                            ExtArea.ScanLineTable = new uint[Height];
                            for (int i = 0; i < ExtArea.ScanLineTable.Length; i++)
                                ExtArea.ScanLineTable[i] = binaryReader.ReadUInt32();
                        }

                        if (ExtArea.PostageStampOffset > 0)
                        {
                            stream.Seek(ExtArea.PostageStampOffset, SeekOrigin.Begin);
                            byte W = binaryReader.ReadByte();
                            byte H = binaryReader.ReadByte();
                            int ImgDataSize = W * H * BytesPerPixel;
                            if (ImgDataSize > 0)
                                ExtArea.PostageStampImage = new TgaPostageStampImage(W, H, binaryReader.ReadBytes(ImgDataSize));
                        }

                        if (ExtArea.ColorCorrectionTableOffset > 0)
                        {
                            stream.Seek(ExtArea.ColorCorrectionTableOffset, SeekOrigin.Begin);
                            ExtArea.ColorCorrectionTable = new ushort[256 * 4];
                            for (int i = 0; i < ExtArea.ColorCorrectionTable.Length; i++)
                                ExtArea.ColorCorrectionTable[i] = binaryReader.ReadUInt16();
                        }
                    }
                }
                #endregion
            }
            #endregion

            binaryReader.Close();
            return true;
        }

        bool LoadFunc(Bitmap bmp, bool useRle = false, bool newFormat = true, bool colorMap2BytesEntry = false)
        {
            if (bmp == null) throw new ArgumentNullException();

            try
            {
                Header.ImageSpec.ImageWidth = (ushort)bmp.Width;
                Header.ImageSpec.ImageHeight = (ushort)bmp.Height;
                Header.ImageSpec.ImageDescriptor.ImageOrigin = TgaImageOrigin.TopLeft;

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

                        int bpp = Math.Max(8, Image.GetPixelFormatSize(bmp.PixelFormat));
                        int BytesPP = bpp / 8;

                        if (bmp.PixelFormat == PixelFormat.Format16bppRgb555)
                            bpp = 15;

                        bool IsAlpha = Image.IsAlphaPixelFormat(bmp.PixelFormat);
                        bool IsPreAlpha = IsAlpha && bmp.PixelFormat.ToString().EndsWith("PArgb");
                        bool IsColorMapped = bmp.PixelFormat.ToString().EndsWith("Indexed");

                        Header.ImageSpec.PixelDepth = (TgaPixelDepth)(BytesPP * 8);

                        if (IsAlpha)
                        {
                            Header.ImageSpec.ImageDescriptor.AlphaChannelBits = (byte)(BytesPP * 2);

                            if (bmp.PixelFormat == PixelFormat.Format16bppArgb1555)
                                Header.ImageSpec.ImageDescriptor.AlphaChannelBits = 1;
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

                            int CMapBpp = (colorMap2BytesEntry ? 15 : 24) + (ColorMapUseAlpha ? (colorMap2BytesEntry ? 1 : 8) : 0);
                            int CMBytesPP = (int)Math.Ceiling(CMapBpp / 8.0);
                            #endregion

                            Header.ColorMapSpec.ColorMapLength = Math.Min((ushort)Colors.Length, ushort.MaxValue);
                            Header.ColorMapSpec.ColorMapEntrySize = (TgaColorMapEntrySize)CMapBpp;
                            ImageOrColorMapArea.ColorMapData = new byte[Header.ColorMapSpec.ColorMapLength * CMBytesPP];

                            byte[] CMapEntry = new byte[CMBytesPP];

                            const float To5Bit = 32f / 256f; // Scale value from 8 to 5 bits.
                            for (int i = 0; i < Colors.Length; i++)
                            {
                                switch (Header.ColorMapSpec.ColorMapEntrySize)
                                {
                                    case TgaColorMapEntrySize.A1R5G5B5:
                                    case TgaColorMapEntrySize.X1R5G5B5:
                                        int R = (int)(Colors[i].R * To5Bit);
                                        int G = (int)(Colors[i].G * To5Bit) << 5;
                                        int B = (int)(Colors[i].B * To5Bit) << 10;
                                        int A = 0;

                                        if (Header.ColorMapSpec.ColorMapEntrySize == TgaColorMapEntrySize.A1R5G5B5)
                                            // Move the source alpha's top bit (bit 7) into bit 15 of the packed
                                            // A1R5G5B5 value, mirroring the read path's "(A1R5G5B5 & 0x8000) >> 15".
                                            A = (Colors[i].A & 0x80) << 8;

                                        CMapEntry = BitConverter.GetBytes(A | R | G | B);
                                        break;

                                    case TgaColorMapEntrySize.R8G8B8:
                                        CMapEntry[0] = Colors[i].B;
                                        CMapEntry[1] = Colors[i].G;
                                        CMapEntry[2] = Colors[i].R;
                                        break;

                                    case TgaColorMapEntrySize.A8R8G8B8:
                                        CMapEntry[0] = Colors[i].B;
                                        CMapEntry[1] = Colors[i].G;
                                        CMapEntry[2] = Colors[i].R;
                                        CMapEntry[3] = Colors[i].A;
                                        break;

                                    case TgaColorMapEntrySize.Other:
                                    default:
                                        break;
                                }

                                Buffer.BlockCopy(CMapEntry, 0, ImageOrColorMapArea.ColorMapData, i * CMBytesPP, CMBytesPP);
                            }
                        }
                        #endregion

                        #region ImageType
                        if (useRle)
                        {
                            if (IsGrayImage)
                                Header.ImageType = TgaImageType.RLE_BlackWhite;
                            else if (IsColorMapped)
                                Header.ImageType = TgaImageType.RLE_ColorMapped;
                            else
                                Header.ImageType = TgaImageType.RLE_TrueColor;
                        }
                        else
                        {
                            if (IsGrayImage)
                                Header.ImageType = TgaImageType.Uncompressed_BlackWhite;
                            else if (IsColorMapped)
                                Header.ImageType = TgaImageType.Uncompressed_ColorMapped;
                            else
                                Header.ImageType = TgaImageType.Uncompressed_TrueColor;
                        }

                        Header.ColorMapType = (IsColorMapped ? TgaColorMapType.ColorMap : TgaColorMapType.NoColorMap);
                        #endregion

                        #region NewFormat
                        if (newFormat)
                        {
                            Footer = new TgaFooter();
                            ExtArea = new TgaExtArea();
                            ExtArea.DateTimeStamp = new TgaDateTime(DateTime.UtcNow);

                            if (IsAlpha)
                            {
                                ExtArea.AttributesType = TgaAttributeType.UsefulAlpha;

                                if (IsPreAlpha)
                                    ExtArea.AttributesType = TgaAttributeType.PreMultipliedAlpha;
                            }
                            else
                            {
                                ExtArea.AttributesType = TgaAttributeType.NoAlpha;

                                if (Header.ImageSpec.ImageDescriptor.AlphaChannelBits > 0)
                                    ExtArea.AttributesType = TgaAttributeType.UndefinedAlphaButShouldBeRetained;
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
                Bw.Write(Header.ToBytes());

                if (ImageOrColorMapArea.ImageID != null)
                    Bw.Write(ImageOrColorMapArea.ImageID.ToBytes());

                if (Header.ColorMapType != TgaColorMapType.NoColorMap)
                    Bw.Write(ImageOrColorMapArea.ColorMapData);

                #region ImageData
                if (Header.ImageType != TgaImageType.NoImageData)
                {
                    if (Header.ImageType >= TgaImageType.RLE_ColorMapped &&
                        Header.ImageType <= TgaImageType.RLE_BlackWhite)
                        Bw.Write(RLE_Encode(ImageOrColorMapArea.ImageData, Width, Height));
                    else
                        Bw.Write(ImageOrColorMapArea.ImageData);
                }
                #endregion

                #region Footer
                if (Footer != null)
                {
                    #region DevArea
                    if (DevArea != null)
                    {
                        for (int i = 0; i < DevArea.Count; i++)
                            Bw.Write(DevArea[i].Data);

                        Bw.Write((ushort)DevArea.Count);

                        for (int i = 0; i < DevArea.Count; i++)
                        {
                            Bw.Write(DevArea[i].Tag);
                            Bw.Write(DevArea[i].Offset);
                            Bw.Write(DevArea[i].FieldSize);
                        }
                    }
                    #endregion

                    #region ExtArea
                    if (ExtArea != null)
                    {
                        Bw.Write(ExtArea.ToBytes());

                        if (ExtArea.ScanLineTable != null)
                            for (int i = 0; i < ExtArea.ScanLineTable.Length; i++)
                                Bw.Write(ExtArea.ScanLineTable[i]);

                        if (ExtArea.PostageStampImage != null)
                            Bw.Write(ExtArea.PostageStampImage.ToBytes());

                        if (ExtArea.ColorCorrectionTable != null)
                            for (int i = 0; i < ExtArea.ColorCorrectionTable.Length; i++)
                                Bw.Write(ExtArea.ColorCorrectionTable[i]);
                    }
                    #endregion

                    Bw.Write(Footer.ToBytes());
                }
                #endregion

                Bw.Flush();
                stream.Flush();
                return true;
            }
            catch
            {
                return false;
            }
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
                            Encoded.AddRange(BitConverterHelper.GetElements(RowData, Pos, Bpp));
                            Pos += Bpp;
                            break;
                        }

                        Count = 0; //1
                        IsRLE = BitConverterHelper.IsElementsEqual(RowData, Pos, Pos + Bpp, Bpp);

                        for (int i = Pos + Bpp; i < Math.Min(Pos + 128 * Bpp, ScanLineSize) - Bpp; i += Bpp)
                        {
                            if (IsRLE ^ BitConverterHelper.IsElementsEqual(RowData, (IsRLE ? Pos : i), i + Bpp, Bpp))
                            {
                                //Count--;
                                break;
                            }
                            else
                                Count++;
                        }

                        int CountBpp = (Count + 1) * Bpp;
                        Encoded.Add((byte)(IsRLE ? Count | 128 : Count));
                        Encoded.AddRange(BitConverterHelper.GetElements(RowData, Pos, (IsRLE ? Bpp : CountBpp)));
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
        /// Convert <see cref="TgaFile"/> to <see cref="Bitmap"/>.
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
                if (ExtArea != null)
                {
                    switch (ExtArea.AttributesType)
                    {
                        case TgaAttributeType.NoAlpha:
                        case TgaAttributeType.UndefinedAlphaCanBeIgnored:
                        case TgaAttributeType.UndefinedAlphaButShouldBeRetained:
                            UseAlpha = false;
                            break;
                        case TgaAttributeType.UsefulAlpha:
                        case TgaAttributeType.PreMultipliedAlpha:
                        default:
                            break;
                    }
                }
                UseAlpha = (Header.ImageSpec.ImageDescriptor.AlphaChannelBits > 0 && UseAlpha) | ForceUseAlpha;
                #endregion

                #region IsGrayImage
                bool IsGrayImage = Header.ImageType == TgaImageType.RLE_BlackWhite ||
                    Header.ImageType == TgaImageType.Uncompressed_BlackWhite;
                #endregion

                #region Get PixelFormat
                PixelFormat PixFormat = PixelFormat.Format24bppRgb;

                switch (Header.ImageSpec.PixelDepth)
                {
                    case TgaPixelDepth.Bpp8:
                        PixFormat = PixelFormat.Format8bppIndexed;
                        break;

                    case TgaPixelDepth.Bpp16:
                        if (IsGrayImage)
                            PixFormat = PixelFormat.Format16bppGrayScale;
                        else
                            PixFormat = (UseAlpha ? PixelFormat.Format16bppArgb1555 : PixelFormat.Format16bppRgb555);
                        break;

                    case TgaPixelDepth.Bpp24:
                        PixFormat = PixelFormat.Format24bppRgb;
                        break;

                    case TgaPixelDepth.Bpp32:
                        if (UseAlpha)
                        {
                            var f = Footer;
                            if (ExtArea?.AttributesType == TgaAttributeType.PreMultipliedAlpha)
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

                ushort BMP_Width = (PostageStampImage ? ExtArea.PostageStampImage.Width : Width);
                ushort BMP_Height = (PostageStampImage ? ExtArea.PostageStampImage.Height : Height);
                Bitmap BMP = new Bitmap(BMP_Width, BMP_Height, PixFormat);

                #region ColorMap and GrayPalette
                if (Header.ColorMapType == TgaColorMapType.ColorMap &&
                   (Header.ImageType == TgaImageType.RLE_ColorMapped ||
                    Header.ImageType == TgaImageType.Uncompressed_ColorMapped))
                {

                    ColorPalette ColorMap = BMP.Palette;
                    Color[] CMapColors = ColorMap.Entries;

                    switch (Header.ColorMapSpec.ColorMapEntrySize)
                    {
                        case TgaColorMapEntrySize.X1R5G5B5:
                        case TgaColorMapEntrySize.A1R5G5B5:
                            const float To8Bit = 255f / 31f; // Scale value from 5 to 8 bits.
                            for (int i = 0; i < Math.Min(CMapColors.Length, Header.ColorMapSpec.ColorMapLength); i++)
                            {
                                ushort A1R5G5B5 = BitConverter.ToUInt16(ImageOrColorMapArea.ColorMapData, i * 2);
                                int A = (UseAlpha ? (A1R5G5B5 & 0x8000) >> 15 : 1) * 255; // (0 or 1) * 255
                                int R = (int)(((A1R5G5B5 & 0x7C00) >> 10) * To8Bit);
                                int G = (int)(((A1R5G5B5 & 0x3E0) >> 5) * To8Bit);
                                int B = (int)((A1R5G5B5 & 0x1F) * To8Bit);
                                CMapColors[i] = Color.FromArgb(A, R, G, B);
                            }
                            break;

                        case TgaColorMapEntrySize.R8G8B8:
                            for (int i = 0; i < Math.Min(CMapColors.Length, Header.ColorMapSpec.ColorMapLength); i++)
                            {
                                int Index = i * 3; //RGB = 3 bytes
                                int R = ImageOrColorMapArea.ColorMapData[Index + 2];
                                int G = ImageOrColorMapArea.ColorMapData[Index + 1];
                                int B = ImageOrColorMapArea.ColorMapData[Index];
                                CMapColors[i] = Color.FromArgb(R, G, B);
                            }
                            break;

                        case TgaColorMapEntrySize.A8R8G8B8:
                            for (int i = 0; i < Math.Min(CMapColors.Length, Header.ColorMapSpec.ColorMapLength); i++)
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
                int BytesPerPixel = (int)Math.Ceiling((double)Header.ImageSpec.PixelDepth / 8.0);
                int StrideBytes = BMP.Width * BytesPerPixel;
                int PaddingBytes = (int)Math.Ceiling(StrideBytes / 4.0) * 4 - StrideBytes;

                if (PaddingBytes > 0) //Need bytes align
                {
                    ImageData = new byte[(StrideBytes + PaddingBytes) * BMP.Height];
                    for (int i = 0; i < BMP.Height; i++)
                        Buffer.BlockCopy(PostageStampImage ? ExtArea.PostageStampImage.Data :
                            ImageOrColorMapArea.ImageData, i * StrideBytes, ImageData,
                            i * (StrideBytes + PaddingBytes), StrideBytes);
                }
                else
                    ImageData = BitConverterHelper.ToBytes(PostageStampImage ? ExtArea.PostageStampImage.Data :
                        ImageOrColorMapArea.ImageData);

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

                if (ExtArea != null && ExtArea.KeyColor.ToInt() != 0)
                    BMP.MakeTransparent(ExtArea.KeyColor.ToColor());

                #region Flip Image
                switch (Header.ImageSpec.ImageDescriptor.ImageOrigin)
                {
                    case TgaImageOrigin.BottomLeft:
                        BMP.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        break;
                    case TgaImageOrigin.BottomRight:
                        BMP.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                        break;
                    case TgaImageOrigin.TopLeft:
                    default:
                        break;
                    case TgaImageOrigin.TopRight:
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

        /// <summary>
        /// Convert <see cref="TgaPostageStampImage"/> to <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="ForceUseAlpha">Force use alpha channel.</param>
        /// <returns>Bitmap or null.</returns>
        public Bitmap? GetPostageStampImage(bool ForceUseAlpha = false)
        {
            if (ExtArea?.PostageStampImage is null || ExtArea.PostageStampImage.Data is null || ExtArea.PostageStampImage.Width <= 0 || ExtArea.PostageStampImage.Height <= 0) return null;
            return ToBitmapFunc(ForceUseAlpha, true);
        }

        /// <summary>
        /// Update Postage Stamp Image or set it.
        /// </summary>
        public void UpdatePostageStampImage()
        {
            if (Header.ImageType == TgaImageType.NoImageData)
            {
                if (ExtArea is not null) ExtArea.PostageStampImage = null;
                return;
            }

            ToNewFormat();
            if (ExtArea is not null) ExtArea.PostageStampImage ??= new TgaPostageStampImage();

            int PS_Width = Header.ImageSpec.ImageWidth;
            int PS_Height = Header.ImageSpec.ImageHeight;

            if (Width > 64 || Height > 64)
            {
                float AspectRatio = Width / (float)Height;
                PS_Width = (byte)(64f * (AspectRatio < 1f ? AspectRatio : 1f));
                PS_Height = (byte)(64f / (AspectRatio > 1f ? AspectRatio : 1f));
            }
            PS_Width = Math.Max(PS_Width, 4);
            PS_Height = Math.Max(PS_Height, 4);

            ExtArea.PostageStampImage.Width = (byte)PS_Width;
            ExtArea.PostageStampImage.Height = (byte)PS_Height;

            int BytesPerPixel = (int)Math.Ceiling((double)Header.ImageSpec.PixelDepth / 8.0);
            ExtArea.PostageStampImage.Data = new byte[PS_Width * PS_Height * BytesPerPixel];

            float WidthCoef = Width / (float)PS_Width;
            float HeightCoef = Height / (float)PS_Height;

            for (int y = 0; y < PS_Height; y++)
            {
                int Y_Offset = (int)(y * HeightCoef) * Width * BytesPerPixel;
                int y_Offset = y * PS_Width * BytesPerPixel;

                for (int x = 0; x < PS_Width; x++)
                {
                    Buffer.BlockCopy(ImageOrColorMapArea.ImageData, Y_Offset + (int)(x * WidthCoef) * BytesPerPixel,
                        ExtArea.PostageStampImage.Data, y_Offset + x * BytesPerPixel, BytesPerPixel);
                }
            }
        }

        /// <summary>
        /// Removes the postage stamp image from the extension area, if one exists.
        /// </summary>
        public void DeletePostageStampImage()
        {
            if (ExtArea is not null) ExtArea.PostageStampImage = null;
        }
    }
}