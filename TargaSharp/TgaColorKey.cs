namespace TargaSharp
{
    public sealed record TgaColorKey : ICloneable
    {
        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 4;


        /// <summary>
        /// Empty constructor (for serialization?)
        /// </summary>
        public TgaColorKey() { }

        /// <summary>
        /// Make <see cref="TgaColorKey"/> from ARGB bytes.
        /// </summary>
        /// <param name="A">Alpha value.</param>
        /// <param name="R">Red value.</param>
        /// <param name="G">Green value.</param>
        /// <param name="B">Blue value.</param>
        public TgaColorKey(byte A, byte R, byte G, byte B)
        {
            this.A = A;
            this.R = R;
            this.G = G;
            this.B = B;
        }

        /// <summary>
        /// Make <see cref="TgaColorKey"/> from ARGB bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[4]).</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TgaColorKey(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");
            int argb = unchecked((int)TgaBinary.ReadUInt32(bytes, 0));
            A = (byte)((argb >> 24) & 0xFF);
            R = (byte)((argb >> 16) & 0xFF);
            G = (byte)((argb >> 8) & 0xFF);
            B = (byte)(argb & 0xFF);
        }

        /// <summary>
        /// Make <see cref="TgaColorKey"/> from <see cref="int"/>.
        /// </summary>
        /// <param name="ARGB">32bit ARGB integer color value.</param>
        public TgaColorKey(int ARGB) : this((byte)((ARGB >> 24) & 0xFF), (byte)((ARGB >> 16) & 0xFF), (byte)((ARGB >> 8) & 0xFF), (byte)(ARGB & 0xFF)) { }


        /// <summary>
        /// Gets or sets alpha color value.
        /// </summary>
        public byte A { get; set; }

        /// <summary>
        /// Gets or sets red color value.
        /// </summary>
        public byte R { get; set; }

        /// <summary>
        /// Gets or sets green color value.
        /// </summary>
        public byte G { get; set; }

        /// <summary>
        /// Gets or sets blue color value.
        /// </summary>
        public byte B { get; set; }

        /// <summary>
        /// Make full independed copy of <see cref="TgaColorKey"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Copy of <see cref="TgaColorKey"/></returns>
        public TgaColorKey Copy() => this with { };

        /// <summary>
        /// Convert <see cref="TgaColorKey"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 4.</returns>
        public byte[] ToBytes() => new TgaByteBuilder(Size).Add(unchecked((uint)ToInt())).ToArray();

        /// <summary>
        /// Gets <see cref="TgaColorKey"/> like ARGB <see cref="int"/>.
        /// </summary>
        /// <returns>ARGB <see cref="int"/> value of <see cref="TgaColorKey"/>.</returns>
        public int ToInt() => (A << 24) | (R << 16) | (G << 8) | B;

        /// <summary>
        /// Gets <see cref="TgaColorKey"/> like string.
        /// </summary>
        /// <returns>String in ARGB format.</returns>
        public override string ToString() => string.Format("{0}={1}, {2}={3}, {4}={5}, {6}={7}", nameof(A), A, nameof(R), R, nameof(G), G, nameof(B), B);

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
