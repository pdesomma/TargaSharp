using TargaNet.Helpers;

namespace TargaNet
{
    /// <summary>
    /// Contains image origin bits and alpha channel bits (or number of overlay bits)
    /// </summary>
    public class ImageDescriptor : ByteSized, ICloneable, IEquatable<ImageDescriptor>, IByteConvert
    {
        /// <summary>
        /// Make empty <see cref="ImageDescriptor"/>.
        /// </summary>
        public ImageDescriptor() : base(1) { }

        /// <summary>
        /// Make <see cref="ImageDescriptor"/> from bytes.
        /// </summary>
        /// <param name="b">ImageDescriptor byte with reserved 7-6 bits, bits 5-4 used for
        /// <see cref="ImageOrigin"/>, 3-0 used as alpha channel bits or number of overlay bits.</param>
        public ImageDescriptor(byte b) : base(1)
        {
            ImageOrigin = (ImageOrigin)((b & 0x30) >> 4);
            AlphaChannelBits = (byte)(b & 0x0F);
        }

        public static bool operator ==(ImageDescriptor item1, ImageDescriptor item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(ImageDescriptor item1, ImageDescriptor item2) => !(item1 == item2);

        /// <summary>
        /// Gets or Sets alpha channel bits or number of overlay bits.
        /// </summary>
        public byte AlphaChannelBits { get; set; }

        /// <summary>
        /// Gets or Sets Image Origin bits (select from enum only, don'n use 5-4 bits!).
        /// </summary>
        public ImageOrigin ImageOrigin { get; set; } = 0;

        /// <summary>
        /// Make full copy of <see cref="ImageDescriptor"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="ImageDescriptor"/>.</returns>
        public ImageDescriptor Clone() => new ImageDescriptor(ToByte());
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is ImageDescriptor id ? Equals(id) : false;
        public bool Equals(ImageDescriptor? item) => item is not null && ImageOrigin == item.ImageOrigin && AlphaChannelBits == item.AlphaChannelBits;

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)ImageOrigin << 4 | AlphaChannelBits).GetHashCode();
            }
        }

        /// <summary>
        /// Gets ImageDescriptor byte.
        /// </summary>
        /// <returns>ImageDescriptor byte with reserved 7-6 bits, bits 5-4 used for imageOrigin,
        /// 3-0 used as alpha channel bits or number of overlay bits.</returns>
        public byte ToByte() => (byte)(((int)ImageOrigin << 4) | AlphaChannelBits);

        public byte[]? ToBytes() => [ToByte()]; 

        public override string ToString() => string.Format("{0}={1}, {2}={3}, ImageDescriptor_AsByte={4}", nameof(ImageOrigin), ImageOrigin, nameof(AlphaChannelBits), AlphaChannelBits, ToByte());
    }
}
