using TargaSharp.IO;
using TargaSharp.Validation;

namespace TargaSharp
{
    /// <summary>
    /// An in-memory representation of a TGA file: its header, image data, and optional
    /// developer area, extension area and footer.
    /// </summary>
    public class TgaFile : ICloneable
    {
        /// <summary>
        /// Gets the fixed-size TGA header (fields 1-8).
        /// </summary>
        public TgaHeader Header { get; internal set; } = new TgaHeader();

        /// <summary>
        /// Gets the image ID, color map data and pixel data (fields 6-8).
        /// </summary>
        public TgaImageArea ImageArea { get; internal set; } = new TgaImageArea();

        /// <summary>
        /// Gets or sets the optional developer area, or <see langword="null"/> when the file has none.
        /// It is only written for new-format files (see <see cref="ToNewFormat()"/>); <see cref="ToOldFormat"/> discards it.
        /// </summary>
        public TgaDeveloperArea? DeveloperArea { get; set; } = null;

        /// <summary>
        /// Gets the optional TGA 2.0 extension area, or <see langword="null"/> when the file has none.
        /// Created by <see cref="ToNewFormat()"/>, dropped by <see cref="ToOldFormat"/>.
        /// </summary>
        public TgaExtensionArea? ExtensionArea { get; internal set; } = null;

        /// <summary>
        /// Gets the optional TGA 2.0 footer, or <see langword="null"/> when the file is in the original (pre-2.0) format.
        /// Created by <see cref="ToNewFormat()"/>, dropped by <see cref="ToOldFormat"/>.
        /// </summary>
        public TgaFooter? Footer { get; internal set; } = null;

        /// <summary>
        /// Create new empty <see cref="TgaFile"/> instance.
        /// </summary>
        public TgaFile() { }

        /// <summary>
        /// Create <see cref="TgaFile"/> instance with some params. If it must have ColorMap,
        /// check all ColorMap fields and settings after. Color-mapped images (<see cref="TgaImageType.UncompressedColorMapped"/>
        /// or <see cref="TgaImageType.RleColorMapped"/>) default to 24-bit (<see cref="TgaColorMapEntrySize.R8G8B8"/>)
        /// palette entries; the palette length and entry data themselves are left for the caller to set afterward.
        /// </summary>
        /// <param name="width">Image Width.</param>
        /// <param name="height">Image Height.</param>
        /// <param name="pixDepth">Image Pixel Depth (bits / pixel), set ColorMap bpp after, if needed!</param>
        /// <param name="imgType">Image Type (is RLE compressed, ColorMapped or GrayScaled).</param>
        /// <param name="attrBits">Set number of Attribute bits (Alpha channel bits), default: 0, 1, 8.</param>
        /// <param name="newFormat">Use new 2.0 TGA XFile format?</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is 0,
        /// <paramref name="pixDepth"/> is <see cref="TgaPixelDepth.Other"/>, <paramref name="attrBits"/> exceeds
        /// <see cref="TgaImageDescriptor.MaxAlphaChannelBits"/>, or the image data would exceed <see cref="int.MaxValue"/> bytes.</exception>
        /// <exception cref="ArgumentException"><paramref name="imgType"/> is <see cref="TgaImageType.NoImageData"/>, which has no pixel data to size,
        /// or is not a spec-defined value (see <see cref="TgaImageTypeExtensions.IsKnown"/>).</exception>
        public TgaFile(ushort width, ushort height, TgaPixelDepth pixDepth = TgaPixelDepth.Bpp24, TgaImageType imgType = TgaImageType.UncompressedTrueColor, byte attrBits = 0, bool newFormat = true)
        {
            if (width == 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Must be > 0.");
            if (height == 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Must be > 0.");
            if (pixDepth == TgaPixelDepth.Other)
                throw new ArgumentOutOfRangeException(nameof(pixDepth), pixDepth, "Must be 8, 16, 24 or 32 bits per pixel.");
            if (imgType == TgaImageType.NoImageData)
                throw new ArgumentException($"{nameof(TgaImageType.NoImageData)} has no pixel data; use the parameterless constructor.", nameof(imgType));
            if (!imgType.IsKnown())
                throw new ArgumentException($"{imgType} is not a spec-defined image type.", nameof(imgType));
            if (attrBits > TgaImageDescriptor.MaxAlphaChannelBits)
                throw new ArgumentOutOfRangeException(nameof(attrBits), attrBits, $"Must be 0-{TgaImageDescriptor.MaxAlphaChannelBits}.");

            // ushort * ushort * 4 exceeds int.MaxValue, so size in long before allocating.
            long imageDataSize = (long)width * height * pixDepth.BytesPerPixel();
            if (imageDataSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(width), $"Image data of {imageDataSize} bytes ({width}x{height}x{(byte)pixDepth}bpp) exceeds the supported size.");
            ImageArea.ImageData = new byte[imageDataSize];

            if (imgType.IsColorMapped())
            {
                Header.ColorMapType = TgaColorMapType.ColorMap;
                Header.ColorMapSpec.FirstEntryIndex = 0;
                // Default color-mapped images to 24-bit (R8G8B8) palette entries, a valid TgaColorMapEntrySize.
                // pixDepth is the *indexed pixel* depth, not the palette entry depth, so it must not be used
                // here directly (e.g. Bpp8 => ceil(8/8) = 1, which is not a valid entry size). The palette's
                // length and actual entry data are left for the caller to fill in.
                Header.ColorMapSpec.ColorMapEntrySize = TgaColorMapEntrySize.R8G8B8;
            }

            Header.ImageType = imgType;
            Header.ImageSpec.ImageWidth = width;
            Header.ImageSpec.ImageHeight = height;
            Header.ImageSpec.PixelDepth = pixDepth;
            Header.ImageSpec.ImageDescriptor.AlphaChannelBits = attrBits;

            if (newFormat) ToNewFormat();
        }

        /// <summary>
        /// Make <see cref="TgaFile"/> from another <see cref="TgaFile"/> instance.
        /// Equal to <see cref="TgaFile.Clone()"/> function.
        /// </summary>
        /// <param name="tga">Original <see cref="TgaFile"/> instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tga"/> is <see langword="null"/>.</exception>
        public TgaFile(TgaFile tga)
        {
            ArgumentNullException.ThrowIfNull(tga);
            Header = tga.Header.Copy();
            ImageArea = tga.ImageArea.Copy();
            DeveloperArea = tga.DeveloperArea?.Copy();
            ExtensionArea = tga.ExtensionArea?.Copy();
            Footer = tga.Footer?.Copy();
        }
        /// <summary>
        /// Load <see cref="TgaFile"/> from file.
        /// </summary>
        /// <param name="filename">Full path to TGA file.</param>
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
        /// Gets or Sets Image Height (see <see cref="TgaImageSpec.ImageHeight"/>).
        /// </summary>
        public ushort Height
        {
            get { return Header.ImageSpec.ImageHeight; }
            set { Header.ImageSpec.ImageHeight = value; }
        }
        /// <summary>
        /// Gets or Sets Image Width (see <see cref="TgaImageSpec.ImageWidth"/>).
        /// </summary>
        public ushort Width
        {
            get { return Header.ImageSpec.ImageWidth; }
            set { Header.ImageSpec.ImageWidth = value; }
        }


        /// <summary>
        /// Make full independent copy of <see cref="TgaFile"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaFile"/>.</returns>
        public TgaFile Clone() => new TgaFile(this);
        /// <inheritdoc />
        object ICloneable.Clone() => Clone();

        /// <summary>
        /// Flip <see cref="TgaFile"/> directions, for more info see <see cref="TgaImageOrigin"/>.
        /// </summary>
        /// <param name="horizontal">Flip horizontal.</param>
        /// <param name="vertical">Flip vertical.</param>
        public void Flip(bool horizontal = false, bool vertical = false)
        {
            // TgaImageOrigin bit 0 = right (vs left), bit 1 = top (vs bottom); a flip toggles the matching bit.
            int newOrigin = (int)Header.ImageSpec.ImageDescriptor.ImageOrigin;
            newOrigin ^= (horizontal ? 0b01 : 0) | (vertical ? 0b10 : 0);
            Header.ImageSpec.ImageDescriptor.ImageOrigin = (TgaImageOrigin)newOrigin;
        }

        /// <summary>
        /// Runs semantic validation (spec value ranges and cross-field consistency) against this
        /// <see cref="TgaFile"/>, e.g. via <see cref="TgaWriter.Write(TgaFile, Stream)"/> before writing.
        /// </summary>
        /// <returns>Every rule violation found, or an empty list when this instance is valid.</returns>
        public IReadOnlyList<TgaValidationError> Validate() => new TgaValidator().Validate(this);

        /// <summary>
        /// Save the <see cref="TgaFile"/> to disk. Equivalent to <see cref="TgaWriter.Write(TgaFile, string)"/> with a default writer.
        /// </summary>
        /// <param name="filename">Full path to file.</param>
        /// <exception cref="TgaValidationException">This instance fails validation or layout computation.</exception>
        /// <exception cref="IOException">The file cannot be created or written.</exception>
        public void Save(string filename) => new TgaWriter().Write(this, filename);

        /// <summary>
        /// Save <see cref="TgaFile"/> to <see cref="Stream"/>. Equivalent to <see cref="TgaWriter.Write(TgaFile, Stream)"/> with a default writer.
        /// </summary>
        /// <param name="stream">A writable stream; it need not be seekable.</param>
        /// <exception cref="TgaValidationException">This instance fails validation or layout computation.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
        public void Save(Stream stream) => new TgaWriter().Write(this, stream);

        /// <summary>
        /// Convert <see cref="TgaFile"/> to bytes array (equal to the saved file, but in memory).
        /// </summary>
        /// <returns>The encoded file bytes.</returns>
        /// <exception cref="TgaValidationException">This instance fails validation or layout computation.</exception>
        public byte[] ToBytes() => new TgaWriter().Write(this);

        /// <summary>
        /// Convert TGA Image to new XFile format (v2.0).
        /// </summary>
        public void ToNewFormat() => ToNewFormat(true);

        /// <summary>
        /// Convert TGA Image to new XFile format (v2.0), with or without an extension area.
        /// Without one the file ends in the bare 26-byte footer, both offsets 0; an existing
        /// <see cref="ExtensionArea"/> is dropped so the footer stays consistent.
        /// </summary>
        /// <param name="withExtensionArea">Create the <see cref="ExtensionArea"/> when missing; false drops it.</param>
        public void ToNewFormat(bool withExtensionArea)
        {
            Footer ??= new TgaFooter();

            if (!withExtensionArea)
            {
                ExtensionArea = null;
                return;
            }

            if (ExtensionArea is null)
            {
                ExtensionArea = new TgaExtensionArea();
                ExtensionArea.DateTimeStamp = new TgaDateTime(DateTime.UtcNow);

                if (Header.ImageSpec.ImageDescriptor.AlphaChannelBits > 0)
                    ExtensionArea.AttributesType = TgaAttributeType.UsefulAlpha;
                else
                    ExtensionArea.AttributesType = TgaAttributeType.NoAlpha;
            }
        }

        /// <summary>
        /// Convert TGA Image to the original (pre-2.0) format: drops the <see cref="Footer"/>,
        /// <see cref="ExtensionArea"/> and <see cref="DeveloperArea"/> so <see cref="Save(string)"/>
        /// writes only the header, image ID, color map and image data. Inverse of <see cref="ToNewFormat()"/>;
        /// the dropped areas are not recoverable from this instance.
        /// </summary>
        public void ToOldFormat()
        {
            Footer = null;
            ExtensionArea = null;
            DeveloperArea = null;
        }

        /// <summary>
        /// Update Postage Stamp Image or set it.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="TgaImageArea.ImageData"/> is <see langword="null"/>, a dimension is 0,
        /// or the data length does not match <see cref="TgaHeader.ImageDataLength"/>.</exception>
        public void UpdatePostageStampImage()
        {
            if (Header.ImageType == TgaImageType.NoImageData)
            {
                if (ExtensionArea is not null) ExtensionArea.PostageStampImage = null;
                return;
            }

            byte[] imageData = ImageArea.ImageData ?? throw new InvalidOperationException("ImageArea.ImageData is null; nothing to build a postage stamp from.");
            if (Width == 0 || Height == 0)
                throw new InvalidOperationException($"Cannot build a postage stamp from a {Width}x{Height} image.");
            if (imageData.Length != Header.ImageDataLength)
                throw new InvalidOperationException($"ImageArea.ImageData is {imageData.Length} bytes but the header declares {Header.ImageDataLength}.");

            // ToNewFormat guarantees the extension area; the stamp is rebuilt from the main image data.
            ToNewFormat();
            TgaPostageStampImage stamp = ExtensionArea!.PostageStampImage ??= new TgaPostageStampImage();

            int psWidth = Header.ImageSpec.ImageWidth;
            int psHeight = Header.ImageSpec.ImageHeight;

            if (Width > TgaPostageStampImage.MaxSize || Height > TgaPostageStampImage.MaxSize)
            {
                float aspectRatio = Width / (float)Height;
                psWidth = (byte)(TgaPostageStampImage.MaxSize * (aspectRatio < 1f ? aspectRatio : 1f));
                psHeight = (byte)(TgaPostageStampImage.MaxSize / (aspectRatio > 1f ? aspectRatio : 1f));
            }
            psWidth = Math.Max(psWidth, 4);
            psHeight = Math.Max(psHeight, 4);

            stamp.Width = (byte)psWidth;
            stamp.Height = (byte)psHeight;

            int bytesPerPixel = Header.ImageSpec.PixelDepth.BytesPerPixel();
            stamp.Data = new byte[stamp.DataLength(Header.ImageSpec.PixelDepth)];

            float widthCoef = Width / (float)psWidth;
            float heightCoef = Height / (float)psHeight;

            for (int y = 0; y < psHeight; y++)
            {
                int sourceOffset = (int)(y * heightCoef) * Width * bytesPerPixel;
                int stampOffset = y * psWidth * bytesPerPixel;

                for (int x = 0; x < psWidth; x++)
                {
                    Buffer.BlockCopy(imageData, sourceOffset + (int)(x * widthCoef) * bytesPerPixel,
                        stamp.Data, stampOffset + x * bytesPerPixel, bytesPerPixel);
                }
            }
        }

        /// <summary>
        /// Removes the postage stamp image from the extension area, if one exists.
        /// </summary>
        public void DeletePostageStampImage()
        {
            if (ExtensionArea is not null) ExtensionArea.PostageStampImage = null;
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
            ImageArea = source.ImageArea;
            DeveloperArea = source.DeveloperArea;
            ExtensionArea = source.ExtensionArea;
            Footer = source.Footer;
        }
    }
}
