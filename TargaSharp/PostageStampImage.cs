using System;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// Postage Stamp Image (MaxSize 64x64, uncompressed, PixelDepth like in full image).
    /// </summary>
    public class PostageStampImage : FileField, ICloneable, IEquatable<PostageStampImage>
    {
        public PostageStampImage() : base(0, 0, 26) { }

        /// <summary>
        /// Make <see cref="PostageStampImage"/> from bytes array.
        /// </summary>
        /// <param name="bytes">Bytes array, first 2 bytes are <see cref="Width"/> and <see cref="Height"/>,
        /// next bytes - image data.</param>
        public PostageStampImage(byte[] bytes) : this()
        {
        }

        /// <summary>
        /// Make <see cref="PostageStampImage"/> from bytes and size.
        /// </summary>
        /// <param name="width">Image Width.</param>
        /// <param name="height">Image Height.</param>
        /// <param name="bytes">Postage Stamp Image Data.</param>
        public PostageStampImage(byte width, byte height, byte[] bytes) : this()
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bytes);
#else 
            if (bytes is null) throw new ArgumentNullException("bytes");
#endif
            Width = width;
            Height = height;
            Data = bytes;
        }

        /// <summary>
        /// Postage Stamp Image Data
        /// </summary>
        public byte[]? Data { get; set; }

        /// <summary>
        /// Postage Stamp Image Height (maximum = 64).
        /// </summary>
        public byte Height { get; set; }

        /// <summary>
        /// Postage Stamp Image Width (maximum = 64).
        /// </summary>
        public byte Width { get; set; }


        public static bool operator ==(PostageStampImage item1, PostageStampImage item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(PostageStampImage item1, PostageStampImage item2) => !(item1 == item2);

        /// <summary>
        /// Make full copy of <see cref="PostageStampImage"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="PostageStampImage"/>.</returns>
        public PostageStampImage Clone() => new(Width, Height, ByteHelper.ToBytes(Data));
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is PostageStampImage post && Equals(post);
        public bool Equals(PostageStampImage? item) => item is not null && Width == item.Width && Height == item.Height && EnumerableHelper.AreEnumerablesEqual(Data, item.Data);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                hash = (13 * hash) + Width.GetHashCode();
                hash = (13 * hash) + Height.GetHashCode();
                if (Data != null)
                    for (int i = 0; i < Data.Length; i++)
                        hash = (13 * hash) + Data[i].GetHashCode();
                return hash;
            }
        }

        public override string ToString() => string.Format("{0}={1}, {2}={3}, DataLength={4}", nameof(Width), Width, nameof(Height), Height, (Data == null ? -1 : Data.Length));

        /// <summary>
        /// Convert <see cref="PostageStampImage"/> to byte array.
        /// </summary>
        /// <returns>Byte array.</returns>
        public override byte[]? ToBytes() => ByteHelper.ToBytes(Width, Height, Data);




        /// <summary>
        /// How to create this object from a byte array that's been validated.
        /// </summary>
        /// <param name="bytes"></param>
        protected override void FromBytes(byte[] bytes)
        {
            Width = bytes[0];
            Height = bytes[1];

            if (bytes.Length > 2) Data = EnumerableHelper.GetElements(bytes, 2, (uint)bytes.Length - 2);
        }

        /// <summary>
        /// Valid byte array length in constructor
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected override bool ValidateByteLength(byte[] bytes) => bytes.Length >= 2;
    }
}
