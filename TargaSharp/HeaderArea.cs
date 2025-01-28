using System;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// File Header Area (18 bytes)
    /// </summary>
    public class HeaderArea : ByteSized, ICloneable, IEquatable<HeaderArea>
    {
        /// <summary>
        /// Make empty <see cref="HeaderArea"/>.
        /// </summary>
        public HeaderArea() : base(18) { }

        /// <summary>
        /// Make <see cref="HeaderArea"/> from bytes.
        /// </summary>
        /// <param name="bytes">Bytes array (byte[18]).</param>
        public HeaderArea(byte[] bytes) : this()
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bytes);
#else 
            if (bytes is null) throw new ArgumentNullException("bytes");
#endif
            if (bytes.Length != ByteSize) throw new ArgumentOutOfRangeException(nameof(bytes.Length) + " must be equal " + ByteSize + "!");

            IdLength = bytes[0];
            ColorMapType = (ColorMapType)bytes[1];
            ImageType = (ImageType)bytes[2];
            ColorMapSpec = new ColorMapSpec(EnumerableHelper.GetElements(bytes, 3, FieldSize.ColorMapSpec));
            ImageSpec = new ImageSpec(EnumerableHelper.GetElements(bytes, 8, FieldSize.ImageSpec));
        }

        public static bool operator ==(HeaderArea item1, HeaderArea item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(HeaderArea item1, HeaderArea item2) => !(item1 == item2);

        /// <summary>
        /// Color Map Specification - Field 4 (5 bytes):
        /// <para>This field and its sub-fields describe the color map (if any) used for the image.
        /// If the Color Map Type field is set to zero, indicating that no color map exists, then
        /// these 5 bytes should be set to zero. These bytes always must be written to the file.</para>
        /// </summary>
        public ColorMapSpec ColorMapSpec { get; set; } = new();

        /// <summary>
        /// Color Map Type - Field 2 (1 byte):
        /// This field indicates the type of color map (if any) included with the image.
        /// There are currently 2 defined values for this field:
        /// <para>0 - indicates that no color-map data is included with this image;</para>
        /// <para>1 - indicates that a color-map is included with this image.</para>
        /// </summary>
        public ColorMapType ColorMapType { get; set; } = ColorMapType.NoColorMap;

        /// <summary>
        /// ID Length - Field 1 (1 byte):
        /// This field identifies the number of bytes contained in the <see cref="ImageID"/> Field.
        /// The maximum number of characters is 255. A value of zero indicates that no Image ID
        /// field is included with the image.
        /// </summary>
        public byte IdLength { get; set; } = 0;

        /// <summary>
        /// Image Specification - Field 5 (10 bytes):
        /// <para>This field and its sub-fields describe the image screen location, size and pixel depth.
        /// These information is always written to the file.</para>
        /// </summary>
        public ImageSpec ImageSpec { get; set; } = new();

        /// <summary>
        /// Image Type - Field 3 (1 byte):
        /// <para>The TGA File Format can be used to store Pseudo-Color, True-Color and Direct-Color images
        /// of various pixel depths.</para>
        /// </summary>
        public ImageType ImageType { get; set; } = ImageType.NoImageData;

        /// <summary>
        /// Make full copy of <see cref="HeaderArea"/>.
        /// </summary>
        /// <returns>Full independent copy of <see cref="HeaderArea"/>.</returns>
        public HeaderArea Clone() => new HeaderArea(ToBytes()!);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is HeaderArea header && Equals(header);
        public bool Equals(HeaderArea? item) =>
            item is not null &&
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

        /// <summary>
        /// Convert <see cref="HeaderArea"/> to byte array.
        /// </summary>
        /// <returns>Byte array with size equal <see cref="Size"/>.</returns>
        public byte[]? ToBytes() =>
            ByteHelper.ToBytes(IdLength, (byte)ColorMapType, (byte)ImageType, 
                ColorMapSpec?.ToBytes() ?? new byte[new ColorMapSpec().ByteSize], 
                ImageSpec?.ToBytes() ?? new byte[new ImageSpec().ByteSize]);

        public override string ToString() =>
            string.Format("{0}={1}, {2}={3}, {4}={5}, {6}={7}, {8}={9}",
                nameof(IdLength), IdLength,
                nameof(ColorMapType), ColorMapType,
                nameof(ImageType), ImageType,
                nameof(ColorMapSpec), ColorMapSpec,
                nameof(ImageSpec), ImageSpec);
    }
}