using TargaSharp.IO;
using TargaSharp.Validation;

namespace TargaSharp
{
    public class TgaFile : ICloneable
    {
        public TgaHeader Header { get; internal set; } = new TgaHeader();
        public TgaImageArea ImageArea { get; internal set; } = new TgaImageArea();
        public TgaDeveloperArea? DeveloperArea { get; internal set; } = null;
        public TgaExtensionArea? ExtensionArea { get; internal set; } = null;
        public TgaFooter? Footer { get; internal set; } = null;

        /// <summary>
        /// Create new empty <see cref="TgaFile"/> istance.
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
        /// <param name="attrBits">Set numder of Attrbute bits (Alpha channel bits), default: 0, 1, 8.</param>
        /// <param name="newFormat">Use new 2.0 TGA XFile format?</param>
        public TgaFile(ushort width, ushort height, TgaPixelDepth pixDepth = TgaPixelDepth.Bpp24, TgaImageType imgType = TgaImageType.UncompressedTrueColor, byte attrBits = 0, bool newFormat = true)
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
                int bytesPerPixel = (int)Math.Ceiling((double)pixDepth / 8.0);
                ImageArea.ImageData = new byte[width * height * bytesPerPixel];

                if (imgType == TgaImageType.UncompressedColorMapped || imgType == TgaImageType.RleColorMapped)
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
                ExtensionArea = new TgaExtensionArea
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
            ImageArea = tga.ImageArea.Copy();
            DeveloperArea = tga.DeveloperArea?.Copy();
            ExtensionArea = tga.ExtensionArea?.Copy();
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
        /// <param name="horizontal">Flip horizontal.</param>
        /// <param name="vertical">Flip vertical.</param>
        public void Flip(bool horizontal = false, bool vertical = false)
        {
            int newOrigin = (int)Header.ImageSpec.ImageDescriptor.ImageOrigin;
            newOrigin = newOrigin ^ ((vertical ? 0x20 : 0) | (horizontal ? 0x10 : 0));
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
        /// <param name="stream">A writable, seekable stream.</param>
        /// <exception cref="TgaValidationException">This instance fails validation or layout computation.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable or not seekable.</exception>
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
        public void ToNewFormat()
        {
            Footer ??= new TgaFooter();

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
        /// Update Postage Stamp Image or set it.
        /// </summary>
        public void UpdatePostageStampImage()
        {
            if (Header.ImageType == TgaImageType.NoImageData)
            {
                if (ExtensionArea is not null) ExtensionArea.PostageStampImage = null;
                return;
            }

            // ToNewFormat guarantees the extension area; the stamp is rebuilt from the main image data.
            ToNewFormat();
            TgaPostageStampImage stamp = ExtensionArea!.PostageStampImage ??= new TgaPostageStampImage();
            byte[] imageData = ImageArea.ImageData ?? throw new InvalidOperationException("ImageArea.ImageData is null; nothing to build a postage stamp from.");

            int psWidth = Header.ImageSpec.ImageWidth;
            int psHeight = Header.ImageSpec.ImageHeight;

            if (Width > 64 || Height > 64)
            {
                float aspectRatio = Width / (float)Height;
                psWidth = (byte)(64f * (aspectRatio < 1f ? aspectRatio : 1f));
                psHeight = (byte)(64f / (aspectRatio > 1f ? aspectRatio : 1f));
            }
            psWidth = Math.Max(psWidth, 4);
            psHeight = Math.Max(psHeight, 4);

            stamp.Width = (byte)psWidth;
            stamp.Height = (byte)psHeight;

            int bytesPerPixel = Header.ImageSpec.PixelDepth.BytesPerPixel();
            stamp.Data = new byte[psWidth * psHeight * bytesPerPixel];

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
