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
    public class TgaImageDescriptor : ICloneable
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
        public TgaImageDescriptor(byte b)
        {
            ImageOrigin = (TgaImageOrigin)((b & 0x30) >> 4);
            AlphaChannelBits = (byte)(b & 0x0F);
        }



        public static bool operator ==(TgaImageDescriptor item1, TgaImageDescriptor item2)
        {
            if (item1 is null) return item2 is null;
            if (item2 is null) return item1 is null;
            return item1.Equals(item2);
        }
        public static bool operator !=(TgaImageDescriptor item1, TgaImageDescriptor item2) => !(item1 == item2);


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
        /// Make full copy of <see cref="TgaImageDescriptor"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaImageDescriptor"/>.</returns>
        public TgaImageDescriptor Clone() => new TgaImageDescriptor(ToByte());
        object ICloneable.Clone() => Clone();

        public override bool Equals(object obj) => obj is TgaImageDescriptor ? Equals((TgaImageDescriptor)obj) : false;
        public bool Equals(TgaImageDescriptor item) => ImageOrigin == item.ImageOrigin && AlphaChannelBits == item.AlphaChannelBits;

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)ImageOrigin << 4 | AlphaChannelBits).GetHashCode();
            }
        }

        public override string ToString() => string.Format("{0}={1}, {2}={3}, ImageDescriptor_AsByte={4}", nameof(ImageOrigin), ImageOrigin, nameof(AlphaChannelBits), AlphaChannelBits, ToByte());

        /// <summary>
        /// Gets ImageDescriptor byte.
        /// </summary>
        /// <returns>ImageDescriptor byte with reserved 7-6 bits, bits 5-4 used for imageOrigin,
        /// 3-0 used as alpha channel bits or number of overlay bits.</returns>
        public byte ToByte() => (byte)(((int)ImageOrigin << 4) | (AlphaChannelBits & 0x0F));
    }
}