namespace TargaSharp
{
    /// <summary>
    /// File Header Area (18 bytes)
    /// </summary>
    public sealed record TgaHeader : ICloneable
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
            ColorMapSpec = new TgaColorMapSpec(bytes.AsSpan(3, TgaColorMapSpec.Size).ToArray());
            ImageSpec = new TgaImageSpec(bytes.AsSpan(8, TgaImageSpec.Size).ToArray());
        }

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
        /// This field identifies the number of bytes contained in the <see cref="TgaImageArea.ImageId"/> Field.
        /// The maximum number of characters is 255. A value of zero indicates that no Image ID
        /// field is included with the image. This is a derived field: it is computed by
        /// <see cref="TargaSharp.IO.TgaWriter"/> from the Image ID string's length during layout
        /// (or read from the file by <see cref="TargaSharp.IO.TgaReader"/>), so consumers cannot
        /// set it directly and make it inconsistent with the actual Image ID.
        /// </summary>
        public byte IdLength { get; internal set; }

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
        /// Make full copy of <see cref="TgaHeader"/>. Named <c>Copy</c> rather than
        /// <c>Clone</c> because records reserve the member name <c>Clone</c> for the compiler-synthesized copy constructor.
        /// </summary>
        /// <returns>Full independent copy of <see cref="TgaHeader"/>.</returns>
        public TgaHeader Copy() => this with { ImageSpec = ImageSpec.Copy(), ColorMapSpec = ColorMapSpec.Copy() };

        /// <summary>
        /// Gets <see cref="TgaHeader"/> like string.
        /// </summary>
        /// <returns>String in "IdLength=0, ColorMapType=1, ImageType=2, ColorMapSpec=3, ImageSpec=4" format.</returns>
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
        public byte[] ToBytes() => new TgaByteBuilder(Size)
            .Add(IdLength)
            .Add((byte)ColorMapType)
            .Add((byte)ImageType)
            .Add(ColorMapSpec?.ToBytes() ?? new byte[TgaColorMapSpec.Size])
            .Add(ImageSpec?.ToBytes() ?? new byte[TgaImageSpec.Size])
            .ToArray();

        /// <inheritdoc />
        object ICloneable.Clone() => Copy();
    }
}
