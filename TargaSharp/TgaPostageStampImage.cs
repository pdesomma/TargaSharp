namespace TargaSharp
{
    /// <summary>
    /// Postage Stamp Image (MaxSize 64x64, uncompressed, PixelDepth like in full image).
    /// </summary>
    public class TgaPostageStampImage : ICloneable
    {
        public TgaPostageStampImage() { }

        /// <summary>
        /// Make <see cref="TgaPostageStampImage"/> from bytes array.
        /// </summary>
        /// <param name="bytes">Bytes array, first 2 bytes are <see cref="Width"/> and <see cref="Height"/>,
        /// next bytes - image data.</param>
        public TgaPostageStampImage(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length < 2)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, "Length must be >= 2.");
            Width = bytes[0];
            Height = bytes[1];
            if (bytes.Length > 2) Data = BitConverterHelper.GetElements(bytes, 2, bytes.Length - 2);
        }

        /// <summary>
        /// Make <see cref="TgaPostageStampImage"/> from bytes and size.
        /// </summary>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Bytes">Postage Stamp Image Data.</param>
        public TgaPostageStampImage(byte width, byte height, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes) + " = null!");

            Width = width;
            Height = height;
            Data = bytes;
        }


        public static bool operator ==(TgaPostageStampImage item1, TgaPostageStampImage item2)
        {
            if (item1 is null) return item2 is null;
            if (item2 is null) return item1 is null;
            return item1.Equals(item2);
        }
        public static bool operator !=(TgaPostageStampImage item1, TgaPostageStampImage item2) => !(item1 == item2);


        /// <summary>
        /// Postage Stamp Image Data
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Postage Stamp Image Height (maximum = 64).
        /// </summary>
        public byte Height { get; set; }

        /// <summary>
        /// Postage Stamp Image Width (maximum = 64).
        /// </summary>
        public byte Width { get; set; }


        /// <summary>
        /// Make full copy of <see cref="TgaPostageStampImage"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaPostageStampImage"/>.</returns>
        public TgaPostageStampImage Clone() => new TgaPostageStampImage(Width, Height, BitConverterHelper.ToBytes(Data));
        object ICloneable.Clone() => Clone();
        
        public override bool Equals(object? obj) => obj is TgaPostageStampImage ? Equals((TgaPostageStampImage)obj) : false;
        public bool Equals(TgaPostageStampImage item) => Width == item.Width && Height == item.Height && BitConverterHelper.IsArraysEqual(Data, item.Data);

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

        /// <summary>
        /// Convert <see cref="TgaPostageStampImage"/> to byte array.
        /// </summary>
        /// <returns>Byte array.</returns>
        public byte[] ToBytes() => BitConverterHelper.ToBytes(Width, Height, Data);

        public override string ToString() => string.Format("{0}={1}, {2}={3}, DataLength={4}", nameof(Width), Width, nameof(Height), Height, Data.Length);
    }
}