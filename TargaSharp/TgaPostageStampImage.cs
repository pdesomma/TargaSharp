namespace TargaSharp
{
    /// <summary>
    /// Postage Stamp Image (MaxSize 64x64, uncompressed, PixelDepth like in full image).
    /// </summary>
    public sealed record TgaPostageStampImage : ICloneable
    {
        /// <summary>
        /// Postage Stamp Image maximum width/height, per spec (the reader relies on this bound
        /// to size its read buffer).
        /// </summary>
        public const byte MaxSize = 64;

        /// <summary>
        /// Bytes preceding <see cref="Data"/> on disk: one each for <see cref="Width"/> and <see cref="Height"/>.
        /// </summary>
        public const int HeaderSize = 2;

        /// <summary>
        /// Backing field for <see cref="Width"/>.
        /// </summary>
        private byte _width = 1;

        /// <summary>
        /// Backing field for <see cref="Height"/>.
        /// </summary>
        private byte _height = 1;

        /// <summary>
        /// Backing field for <see cref="Data"/>.
        /// </summary>
        private byte[] _data = Array.Empty<byte>();

        /// <summary>
        /// Make a 1x1 <see cref="TgaPostageStampImage"/> with empty <see cref="Data"/> (the smallest size the setters allow).
        /// </summary>
        public TgaPostageStampImage() { }

        /// <summary>
        /// Make <see cref="TgaPostageStampImage"/> from bytes array.
        /// </summary>
        /// <param name="bytes">Bytes array, first 2 bytes are <see cref="Width"/> and <see cref="Height"/>,
        /// next bytes - image data.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bytes"/>.Length &lt; 2,
        /// or when the encoded width or height is 0 or greater than <see cref="MaxSize"/> (a corrupt stamp).</exception>
        public TgaPostageStampImage(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length < HeaderSize)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be >= {HeaderSize}.");
            // Checked here so the exception names bytes rather than the setters' value.
            if (bytes[0] == 0 || bytes[0] > MaxSize)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes[0], $"Width (byte 0) must be in range 1-{MaxSize}.");
            if (bytes[1] == 0 || bytes[1] > MaxSize)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes[1], $"Height (byte 1) must be in range 1-{MaxSize}.");
            _width = bytes[0];
            _height = bytes[1];
            _data = bytes.AsSpan(HeaderSize, bytes.Length - HeaderSize).ToArray();
        }

        /// <summary>
        /// Make <see cref="TgaPostageStampImage"/> from bytes and size.
        /// </summary>
        /// <param name="width">Image Width.</param>
        /// <param name="height">Image Height.</param>
        /// <param name="bytes">Postage Stamp Image Data.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="width"/> or
        /// <paramref name="height"/> is 0 or greater than <see cref="MaxSize"/> (a corrupt stamp).</exception>
        public TgaPostageStampImage(byte width, byte height, byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            Width = width;
            Height = height;
            Data = bytes;
        }


        /// <summary>
        /// Postage Stamp Image Data
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
        public byte[] Data
        {
            get => _data;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _data = value;
            }
        }

        /// <summary>
        /// Postage Stamp Image Height (must be 1-<see cref="MaxSize"/>, inclusive).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to 0 or a value greater
        /// than <see cref="MaxSize"/>, per spec ("must be" &lt;= 64x64; 0 is a meaningless stamp size).</exception>
        public byte Height
        {
            get => _height;
            set
            {
                if (value == 0 || value > MaxSize)
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"Height must be in range 1-{MaxSize} (TGA postage stamp image maximum size).");
                _height = value;
            }
        }

        /// <summary>
        /// Postage Stamp Image Width (must be 1-<see cref="MaxSize"/>, inclusive).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to 0 or a value greater
        /// than <see cref="MaxSize"/>, per spec ("must be" &lt;= 64x64; 0 is a meaningless stamp size).</exception>
        public byte Width
        {
            get => _width;
            set
            {
                if (value == 0 || value > MaxSize)
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"Width must be in range 1-{MaxSize} (TGA postage stamp image maximum size).");
                _width = value;
            }
        }


        /// <summary>
        /// Byte length <see cref="Data"/> must have for a stamp of this size at <paramref name="pixelDepth"/> (the main image's depth).
        /// </summary>
        /// <param name="pixelDepth">Pixel depth of the parent image; the stamp shares it.</param>
        /// <returns><see cref="Width"/> * <see cref="Height"/> * bytes per pixel.</returns>
        public int DataLength(TgaPixelDepth pixelDepth) => Width * Height * pixelDepth.BytesPerPixel();

        /// <summary>
        /// Make full copy of <see cref="TgaPostageStampImage"/>. Named <c>Copy</c> rather than <c>Clone</c>
        /// because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaPostageStampImage"/>.</returns>
        public TgaPostageStampImage Copy() => this with { Data = (byte[])Data.Clone() };

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();

        /// <inheritdoc />
        public bool Equals(TgaPostageStampImage? other)
        {
            if (other is null) return false;
            return Width == other.Width && Height == other.Height &&
                TgaArrayEquality.Equals(Data, other.Data);
        }

        /// <summary>
        /// Gets a hash code derived from <see cref="Width"/>, <see cref="Height"/> and <see cref="Data"/>.
        /// </summary>
        /// <returns>A hash code for this <see cref="TgaPostageStampImage"/>.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                hash = (13 * hash) + Width.GetHashCode();
                hash = (13 * hash) + Height.GetHashCode();
                hash = TgaArrayEquality.Hash(hash, Data);
                return hash;
            }
        }

        /// <summary>
        /// Convert <see cref="TgaPostageStampImage"/> to byte array.
        /// </summary>
        /// <returns>Byte array.</returns>
        public byte[] ToBytes() => new TgaByteBuilder(HeaderSize + Data.Length).Add(Width).Add(Height).Add(Data).ToArray();

        /// <summary>
        /// Gets <see cref="TgaPostageStampImage"/> like string.
        /// </summary>
        /// <returns>String in "Width=0, Height=1, DataLength=2" format.</returns>
        public override string ToString() => string.Format("{0}={1}, {2}={3}, DataLength={4}", nameof(Width), Width, nameof(Height), Height, Data.Length);
    }
}