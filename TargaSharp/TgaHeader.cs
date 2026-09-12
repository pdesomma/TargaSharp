namespace TargaSharp
{
    /// <summary>
    /// File Header Area (18 bytes)
    /// </summary>
    public class TgaHeader : ICloneable
    {
        /// <summary>
        /// Gets TGA Header Section size in bytes.
        /// </summary>
        public const int Size = 18;


        /// <summary>
        /// Make empty <see cref="TgaHeader"/>.
        /// </summary>
        public TgaHeader() { }

        /// <summary>
        /// Make <see cref="TgaHeader"/> from bytes.
        /// </summary>
        /// <param name="bytes">Bytes array (byte[18]).</param>
        public TgaHeader(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length != Size)
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Length must be {Size}.");

            IdLength = bytes[0];
            ColorMapType = (TgaColorMapType)bytes[1];
            ImageType = (TgaImageType)bytes[2];
            ColorMapSpec = new TgaColorMapSpec(BitConverterHelper.GetElements(bytes, 3, TgaColorMapSpec.Size));
            ImageSpec = new TgaImageSpec(BitConverterHelper.GetElements(bytes, 8, TgaImageSpec.Size));
        }



        public static bool operator ==(TgaHeader item1, TgaHeader item2)
        {
            if (item1 is null) return item2 is null;
            if(item2 is null) return item1 is null;
            return item1.Equals(item2);
        }
        public static bool operator !=(TgaHeader item1, TgaHeader item2) => !(item1 == item2);



        /// <summary>
        /// Color Map Specification - Field 4 (5 bytes):
        /// <para>This field and its sub-fields describe the color map (if any) used for the image.
        /// If the Color Map Type field is set to zero, indicating that no color map exists, then
        /// these 5 bytes should be set to zero. These bytes always must be written to the file.</para>
        /// </summary>
        public TgaColorMapSpec ColorMapSpec { get; set; } = new();

        /// <summary>
        /// Color Map Type - Field 2 (1 byte):
        /// This field indicates the type of color map (if any) included with the image.
        /// There are currently 2 defined values for this field:
        /// <para>0 - indicates that no color-map data is included with this image;</para>
        /// <para>1 - indicates that a color-map is included with this image.</para>
        /// </summary>
        public TgaColorMapType ColorMapType { get; set; }

        /// <summary>
        /// ID Length - Field 1 (1 byte):
        /// This field identifies the number of bytes contained in the <see cref="ImageID"/> Field.
        /// The maximum number of characters is 255. A value of zero indicates that no Image ID
        /// field is included with the image.
        /// </summary>
        public byte IdLength { get; set; }

        /// <summary>
        /// Image Specification - Field 5 (10 bytes):
        /// <para>This field and its sub-fields describe the image screen location, size and pixel depth.
        /// These information is always written to the file.</para>
        /// </summary>
        public TgaImageSpec ImageSpec { get; set; } = new();

        /// <summary>
        /// Image Type - Field 3 (1 byte):
        /// <para>The TGA File Format can be used to store Pseudo-Color, True-Color and Direct-Color images
        /// of various pixel depths.</para>
        /// </summary>
        /// 
        public TgaImageType ImageType { get; set; }
        


        /// <summary>
        /// Make full copy of <see cref="TgaHeader"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaHeader"/>.</returns>
        public TgaHeader Clone() => new TgaHeader(ToBytes());
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is TgaHeader ? Equals((TgaHeader)obj) : false;
        public bool Equals(TgaHeader item) => 
            IdLength == item.IdLength &&
            ColorMapType == item.ColorMapType &&
            ImageType == item.ImageType &&
            ColorMapSpec == item.ColorMapSpec &&
            ImageSpec == item.ImageSpec;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (IdLength << 24 | (byte)ColorMapType << 8 | (byte)ImageType).GetHashCode();
                if (ColorMapSpec is not null) hash = hash * 23 + ColorMapSpec.GetHashCode();
                if (ImageSpec is not null) hash = hash * 23 + ImageSpec.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => string.Format("{0}={1}, {2}={3}, {4}={5}, {6}={7}, {8}={9}",
                nameof(IdLength), IdLength,
                nameof(ColorMapType), ColorMapType,
                nameof(ImageType), ImageType,
                nameof(ColorMapSpec), ColorMapSpec,
                nameof(ImageSpec), ImageSpec);        

        /// <summary>
        /// Convert <see cref="TgaHeader"/> to byte array.
        /// </summary>
        /// <returns>Byte array with size equal <see cref="Size"/>.</returns>
        public byte[] ToBytes() => BitConverterHelper.ToBytes(IdLength, (byte)ColorMapType, (byte)ImageType, ColorMapSpec?.ToBytes() ?? new byte[TgaColorMapSpec.Size], ImageSpec?.ToBytes() ?? new byte[TgaImageSpec.Size])!;
    }
}