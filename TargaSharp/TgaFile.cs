using TargaSharp.IO;
using TargaSharp.Validation;

namespace TargaSharp
{
    public class TgaFile : ICloneable
    {
        public TgaHeader Header { get; internal set; } = new TgaHeader();
        public TgaImgOrColMap ImageOrColorMapArea { get; internal set; } = new TgaImgOrColMap();
        public TgaDevArea? DevArea { get; internal set; } = null;
        public TgaExtArea? ExtArea { get; internal set; } = null;
        public TgaFooter? Footer { get; internal set; } = null;

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
        public TgaFile(string filename) => CopyAreasFrom(new TgaReader().Read(filename));
        /// <summary>
        /// Make <see cref="TgaFile"/> from bytes array.
        /// </summary>
        /// <param name="bytes">Bytes array (same like TGA File).</param>
        public TgaFile(byte[] bytes) => CopyAreasFrom(new TgaReader().Read(bytes));
        /// <summary>
        /// Make <see cref="TgaFile"/> from <see cref="Stream"/>.
        /// For file opening better use <see cref="TgaFile(string)"/>.
        /// </summary>
        /// <param name="stream">Some stream. You can use a lot of Stream types, but Stream must support:
        /// <see cref="Stream.CanSeek"/> and <see cref="Stream.CanRead"/>.</param>
        public TgaFile(Stream stream) => CopyAreasFrom(new TgaReader().Read(stream));


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
        /// <inheritdoc />
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
        /// <param name="ErrorStr">Description of the failure, or <see cref="string.Empty"/> on success.</param>
        /// <returns>Return "true", if all OK or "false", if checking failed.</returns>
        public bool CheckAndUpdateOffsets(out string ErrorStr) => new TgaWriter().TryComputeLayout(this, out ErrorStr);

        /// <summary>
        /// Runs semantic validation (spec value ranges and cross-field consistency) against this
        /// <see cref="TgaFile"/>, e.g. via <see cref="TgaWriter.Write(TgaFile, Stream)"/> before writing.
        /// </summary>
        /// <returns>Every rule violation found, or an empty list when this instance is valid.</returns>
        public IReadOnlyList<TgaValidationError> Validate() => new TgaValidator().Validate(this);

        /// <summary>
        /// Save the <see cref="TgaFile"/> to disk.
        /// </summary>
        /// <param name="filename">Full path to file.</param>
        /// <returns>Return "true", if all done or "false", if failed.</returns>
        public bool Save(string filename)
        {
            try
            {
                new TgaWriter().Write(this, filename);
                return true;
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
        public bool Save(Stream stream)
        {
            try
            {
                new TgaWriter().Write(this, stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convert <see cref="TgaFile"/> to bytes array.
        /// </summary>
        /// <returns>Bytes array, (equal to saved file, but in memory) or null (on error).</returns>
        public byte[]? ToBytes()
        {
            try
            {
                return new TgaWriter().Write(this);
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

        /// <summary>
        /// Copies the 5 parsed areas from a <see cref="TgaFile"/> produced by <see cref="TgaReader"/>
        /// into this instance. Used by the <see cref="string"/>/<see cref="byte"/>[]/<see cref="Stream"/>
        /// loading constructors, which read into a throwaway instance via <see cref="ITgaReader"/> and
        /// then adopt its areas here.
        /// </summary>
        /// <param name="source"><see cref="TgaFile"/> instance freshly populated by <see cref="TgaReader"/>.</param>
        private void CopyAreasFrom(TgaFile source)
        {
            Header = source.Header;
            ImageOrColorMapArea = source.ImageOrColorMapArea;
            DevArea = source.DevArea;
            ExtArea = source.ExtArea;
            Footer = source.Footer;
        }
    }
}
