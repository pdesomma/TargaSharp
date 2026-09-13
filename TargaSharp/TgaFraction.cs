namespace TargaSharp
{
    /// <summary>
    /// A numerator/denominator ratio used for the TGA extension area's pixel aspect ratio
    /// (spec field 19) and gamma value (spec field 20) fields.
    /// </summary>
    public sealed record TgaFraction : ICloneable
    {
        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 4;

        /// <summary>
        /// Gets a new Empty <see cref="TgaFraction"/>, all values are 0. A new instance is
        /// returned on every access so callers cannot mutate a shared default.
        /// </summary>
        public static TgaFraction Empty => new TgaFraction();

        /// <summary>
        /// Gets a new One <see cref="TgaFraction"/>, all values are 1 (ones, 1 / 1 = 1). A new
        /// instance is returned on every access so callers cannot mutate a shared default.
        /// </summary>
        public static TgaFraction One => new TgaFraction(1, 1);


        /// <summary>
        /// Make <see cref="TgaFraction"/> from <see cref="Numerator"/> and <see cref="Denominator"/>.
        /// </summary>
        /// <param name="numerator">Numerator value.</param>
        /// <param name="denominator">Denominator value.</param>
        public TgaFraction(ushort numerator = 0, ushort denominator = 0)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <summary>
        /// Make <see cref="TgaFraction"/> from bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes(byte[4]).</param>
        public TgaFraction(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");
            Numerator = TgaBinary.ReadUInt16(bytes, 0);
            Denominator = TgaBinary.ReadUInt16(bytes, 2);
        }


        /// <summary>
        /// Gets or sets numerator value.
        /// </summary>
        public ushort Numerator { get; set; }

        /// <summary>
        /// Gets or sets denominator value.
        /// </summary>
        public ushort Denominator { get; set; }

        /// <summary>
        /// Gets whether this <see cref="TgaFraction"/> represents an unspecified ratio. Per the
        /// TGA spec, a zero <see cref="Denominator"/> indicates no ratio is specified.
        /// </summary>
        public bool IsUnspecified => Denominator == 0;

        /// <summary>
        /// Get the ratio value = <see cref="Numerator"/> / <see cref="Denominator"/>, or null when
        /// <see cref="IsUnspecified"/> is true (<see cref="Denominator"/> is 0). Used for both the
        /// pixel aspect ratio and the gamma value fields.
        /// </summary>
        public float? Value => IsUnspecified ? null : Numerator == Denominator ? 1f : Numerator / (float)Denominator;


        /// <summary>
        /// Make full independent copy of <see cref="TgaFraction"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Copy of <see cref="TgaFraction"/></returns>
        public TgaFraction Copy() => this with { };

        /// <summary>
        /// Convert <see cref="TgaFraction"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 4.</returns>
        public byte[] ToBytes() => new TgaByteBuilder(Size).Add(Numerator).Add(Denominator).ToArray();

        /// <summary>
        /// Gets <see cref="TgaFraction"/> like string.
        /// </summary>
        /// <returns>String in "Numerator=1, Denominator=2" format.</returns>
        public override string ToString() => string.Format("{0}={1}, {2}={3}", nameof(Numerator), Numerator, nameof(Denominator), Denominator);

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
