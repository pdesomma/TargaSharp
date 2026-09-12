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
            Header = tga.Header.Copy();
            ImageOrColorMapArea = tga.ImageOrColorMapArea.Copy();
            DevArea = tga.DevArea?.Copy();
            ExtArea = tga.ExtArea?.Copy();
            Footer = tga.Footer?.Copy();
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
        /// Gets or Sets Image Height (see <see cref="Header.ImageSpec.ImageHeight"/>).
        /// </summary>
        public ushort Height
        {
            get { return Header.ImageSpec.ImageHeight; }
            set { Header.ImageSpec.ImageHeight = value; }
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
                    #endregion


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