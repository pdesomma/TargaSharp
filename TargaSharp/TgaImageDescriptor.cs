namespace TargaSharp
{
    /// <summary>
    /// Contains image origin bits and alpha channel bits(or number of overlay bits)
    /// </summary>
    /// <remarks>
    /// ImageDescriptor byte bit layout (spec Field 5.6):
    /// bit 7 6 5 4 3 2 1 0
    ///     0 0 O O A A A A
    /// Bits 7-6 are reserved and must be 0. Bits 5-4 hold <see cref="ImageOrigin"/>.
    /// Bits 3-0 hold <see cref="AlphaChannelBits"/> (or number of overlay bits).
    /// </remarks>
    public sealed record TgaImageDescriptor : ICloneable
    {
        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 1;

        /// <summary>
        /// Backing field for <see cref="AlphaChannelBits"/>.
        /// </summary>
        private byte _alphaChannelBits;


        /// <summary>
        /// Make empty <see cref="TgaImageDescriptor"/>.
        /// </summary>
        public TgaImageDescriptor() { }

        /// <summary>
        /// Make <see cref="TgaImageDescriptor"/> from bytes.
        /// </summary>
        /// <param name="b">ImageDescriptor byte with reserved 7-6 bits, bits 5-4 used for
        /// <see cref="ImageOrigin"/>, 3-0 used as alpha channel bits or number of overlay bits.</param>
        /// <remarks>
        /// This is the file-reader path: reserved bits 7-6 are read from bits 5-4 and 3-0 only
        /// (via the <c>0x30</c>/<c>0x0F</c> masks below), so any stray bits 7-6 set by malformed
        /// files in the wild are silently ignored rather than throwing.
        /// </remarks>
        public TgaImageDescriptor(byte b)
        {
            ImageOrigin = (TgaImageOrigin)((b & 0x30) >> 4);
            AlphaChannelBits = (byte)(b & 0x0F);
        }


        /// <summary>
        /// Gets or Sets Image Origin bits (select from enum only, don'n use 5-4 bits!).
        /// </summary>
        public TgaImageOrigin ImageOrigin { get;set; }

        /// <summary>
        /// Gets or Sets alpha channel bits or number of overlay bits (bits 3-0 of the
        /// ImageDescriptor byte). Spec-valid range is 0-15.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value being set is
        /// greater than 15, since it would bleed into the <see cref="ImageOrigin"/> bits.</exception>
        public byte AlphaChannelBits
        {
            get => _alphaChannelBits;
            set
            {
                if (value > 15)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "AlphaChannelBits must be in range 0-15 (bits 3-0 of the ImageDescriptor byte).");
                _alphaChannelBits = value;
            }
        }

        /// <summary>
        /// Make full copy of <see cref="TgaImageDescriptor"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaImageDescriptor"/>.</returns>
        public TgaImageDescriptor Copy() => this with { };

        /// <summary>
        /// Gets <see cref="TgaImageDescriptor"/> like string.
        /// </summary>
        /// <returns>String in "ImageOrigin=0, AlphaChannelBits=1, ImageDescriptor_AsByte=2" format.</returns>
        public override string ToString() => string.Format("{0}={1}, {2}={3}, ImageDescriptor_AsByte={4}", nameof(ImageOrigin), ImageOrigin, nameof(AlphaChannelBits), AlphaChannelBits, ToByte());

        /// <summary>
        /// Gets ImageDescriptor byte.
        /// </summary>
        /// <returns>ImageDescriptor byte with reserved 7-6 bits, bits 5-4 used for imageOrigin,
        /// 3-0 used as alpha channel bits or number of overlay bits.</returns>
        /// <remarks>
        /// <see cref="ImageOrigin"/> is masked to its 2 valid bits (like <see cref="AlphaChannelBits"/>
        /// already is) so reserved bits 7-6 stay 0 even if the enum somehow holds an out-of-range
        /// value from an unchecked cast.
        /// </remarks>
        public byte ToByte() => (byte)((((int)ImageOrigin & 0x03) << 4) | (AlphaChannelBits & 0x0F));

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
