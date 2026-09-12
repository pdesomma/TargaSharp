namespace TargaSharp
{
    /// <summary>
    /// Image Specification - Field 5 (10 bytes):
    /// <para>This field and its sub-fields describe the image screen location, size and pixel depth.
    /// These information is always written to the file.</para>
    /// </summary>
    public class TgaImageSpec : ICloneable
    {
        /// <summary>
        /// Gets TGA Field size in bytes.
        /// </summary>
        public const int Size = 10;


        /// <summary>
        /// Create a new instance of the <see cref="TgaImageSpec"/> class.
        /// </summary>
        public TgaImageSpec() { }
        
        /// <summary>
        /// Make ImageSpec from values.
        /// </summary>
        /// <param name="X_Origin">These specify the absolute horizontal coordinate for the lower
        /// left corner of the image as it is positioned on a display device having an origin at
        /// the lower left of the screen(e.g., the TARGA series).</param>
        /// <param name="Y_Origin">These specify the absolute vertical coordinate for the lower
        /// left corner of the image as it is positioned on a display device having an origin at
        /// the lower left of the screen(e.g., the TARGA series).</param>
        /// <param name="ImageWidth">This field specifies the width of the image in pixels.</param>
        /// <param name="ImageHeight">This field specifies the height of the image in pixels.</param>
        /// <param name="PixelDepth">This field indicates the number of bits per pixel. This number
        /// includes the Attribute or Alpha channel bits. Common values are 8, 16, 24 and 32 but
        /// other pixel depths could be used.</param>
        /// <param name="ImageDescriptor">Contains image origin bits and alpha channel bits
        /// (or number of overlay bits).</param>
        public TgaImageSpec(ushort xOrigin, ushort yOrigin, ushort imageWidth, ushort imageHeight, TgaPixelDepth pixelDepth, TgaImageDescriptor imageDescriptor)
        {
            XOrigin = xOrigin;
            YOrigin = yOrigin;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            PixelDepth = pixelDepth;
            ImageDescriptor = imageDescriptor;
        }

        /// <summary>
        /// Make ImageSpec from bytes.
        /// </summary>
        /// <param name="Bytes">Array of bytes(byte[10]).</param>
        public TgaImageSpec(byte[] Bytes)
        {
            ArgumentNullException.ThrowIfNull(Bytes);
            if (Bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(Bytes), Bytes.Length, $"Length must be {Size}.");
            XOrigin = BitConverter.ToUInt16(Bytes, 0);
            YOrigin = BitConverter.ToUInt16(Bytes, 2);
            ImageWidth = BitConverter.ToUInt16(Bytes, 4);
            ImageHeight = BitConverter.ToUInt16(Bytes, 6);
            PixelDepth = (TgaPixelDepth)Bytes[8];
            ImageDescriptor = new TgaImageDescriptor(Bytes[9]);
        }

        public static bool operator ==(TgaImageSpec item1, TgaImageSpec item2)
        {
            if (item1 is null) return item2 is null;
            if (item2 is null) return item1 is null;
            return item1.Equals(item2);
        }
        public static bool operator !=(TgaImageSpec item1, TgaImageSpec item2) => !(item1 == item2);



        /// <summary>
        /// Contains image origin bits and alpha channel bits(or number of overlay bits).
        /// </summary>
        public TgaImageDescriptor ImageDescriptor { get; set; } = new TgaImageDescriptor();

        /// <summary>
        /// This field specifies the height of the image in pixels.
        /// </summary>
        public ushort ImageHeight { get; set; }

        /// <summary>
        /// This field specifies the width of the image in pixels.
        /// </summary>
        public ushort ImageWidth { get; set; }

        /// <summary>
        /// This field indicates the number of bits per pixel. This number includes the Attribute or
        /// Alpha channel bits. Common values are 8, 16, 24 and 32 but other pixel depths could be used.
        /// </summary>
        public TgaPixelDepth PixelDepth { get; set; }

        /// <summary>
        /// These specify the absolute horizontal coordinate for the lower left corner of the image
        /// as it is positioned on a display device having an origin at the lower left of the
        /// screen(e.g., the TARGA series).
        /// </summary>
        public ushort XOrigin { get;set;}

        /// <summary>
        /// These specify the absolute vertical coordinate for the lower left corner of the image
        /// as it is positioned on a display device having an origin at the lower left of the
        /// screen(e.g., the TARGA series).
        /// </summary>
        public ushort YOrigin { get; set; }
        


        /// <summary>
        /// Make full copy of <see cref="TgaImageDescriptor"/>.
        /// </summary>
        /// <returns></returns>
        public TgaImageSpec Clone() => new TgaImageSpec(ToBytes());
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => (obj is TgaImageSpec) ? Equals((TgaImageSpec)obj) : false;
        public bool Equals(TgaImageSpec item) => 
                XOrigin == item.XOrigin &&
                YOrigin == item.YOrigin &&
                ImageWidth == item.ImageWidth &&
                ImageHeight == item.ImageHeight &&
                PixelDepth == item.PixelDepth &&
                ImageDescriptor == item.ImageDescriptor;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + XOrigin.GetHashCode();
                hash = hash * 23 + YOrigin.GetHashCode();
                hash = hash * 23 + ImageWidth.GetHashCode();
                hash = hash * 23 + ImageHeight.GetHashCode();
                hash = hash * 23 + PixelDepth.GetHashCode();
                if (ImageDescriptor != null) hash = hash * 23 + ImageDescriptor.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return String.Format("{0}={1}, {2}={3}, {4}={5}, {6}={7}, {8}={9}, {10}={11}",
                nameof(XOrigin), XOrigin,
                nameof(YOrigin), YOrigin,
                nameof(ImageWidth), ImageWidth,
                nameof(ImageHeight), ImageHeight,
                nameof(PixelDepth), PixelDepth,
                nameof(ImageDescriptor), ImageDescriptor);
        }

        /// <summary>
        /// Convert <see cref="TgaImageSpec"/> to byte array.
        /// </summary>
        /// <returns>Byte array with length = 10.</returns>
        public byte[] ToBytes() => BitConverterHelper.ToBytes(XOrigin, YOrigin, ImageWidth, ImageHeight, (byte)PixelDepth, ImageDescriptor == null ? byte.MinValue : ImageDescriptor.ToByte());
    }
}