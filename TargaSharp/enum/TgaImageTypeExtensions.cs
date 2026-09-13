namespace TargaSharp
{
    /// <summary>
    /// Extension methods for classifying <see cref="TgaImageType"/> values against the
    /// Truevision TGA File Format specification's image type ranges.
    /// </summary>
    public static class TgaImageTypeExtensions
    {
        /// <summary>
        /// Determines whether the image type is run-length encoded
        /// (<see cref="TgaImageType.RLE_ColorMapped"/>, <see cref="TgaImageType.RLE_TrueColor"/>
        /// or <see cref="TgaImageType.RLE_BlackWhite"/>, spec values 9-11).
        /// </summary>
        /// <param name="imageType">The image type to test.</param>
        /// <returns><see langword="true" /> if the image type is one of the three RLE variants; otherwise, <see langword="false" />.</returns>
        public static bool IsRunLengthEncoded(this TgaImageType imageType)
        {
            byte value = (byte)imageType;
            return (value & 0x08) != 0 && (value & 0x07) is >= 1 and <= 3;
        }

        /// <summary>
        /// Determines whether the image type stores color-mapped pixel data
        /// (<see cref="TgaImageType.Uncompressed_ColorMapped"/> or <see cref="TgaImageType.RLE_ColorMapped"/>, spec values 1 or 9).
        /// </summary>
        /// <param name="imageType">The image type to test.</param>
        /// <returns><see langword="true" /> if the image type is color-mapped; otherwise, <see langword="false" />.</returns>
        public static bool IsColorMapped(this TgaImageType imageType)
            => imageType is TgaImageType.Uncompressed_ColorMapped or TgaImageType.RLE_ColorMapped;

        /// <summary>
        /// Determines whether the image type stores true-color pixel data
        /// (<see cref="TgaImageType.Uncompressed_TrueColor"/> or <see cref="TgaImageType.RLE_TrueColor"/>, spec values 2 or 10).
        /// </summary>
        /// <param name="imageType">The image type to test.</param>
        /// <returns><see langword="true" /> if the image type is true-color; otherwise, <see langword="false" />.</returns>
        public static bool IsTrueColor(this TgaImageType imageType)
            => imageType is TgaImageType.Uncompressed_TrueColor or TgaImageType.RLE_TrueColor;

        /// <summary>
        /// Determines whether the image type stores black-and-white (grayscale) pixel data
        /// (<see cref="TgaImageType.Uncompressed_BlackWhite"/> or <see cref="TgaImageType.RLE_BlackWhite"/>, spec values 3 or 11).
        /// </summary>
        /// <param name="imageType">The image type to test.</param>
        /// <returns><see langword="true" /> if the image type is black-and-white; otherwise, <see langword="false" />.</returns>
        public static bool IsGrayscale(this TgaImageType imageType)
            => imageType is TgaImageType.Uncompressed_BlackWhite or TgaImageType.RLE_BlackWhite;

        /// <summary>
        /// Determines whether the image type is one of the seven values currently defined by the
        /// Truevision TGA File Format specification.
        /// </summary>
        /// <param name="imageType">The image type to test.</param>
        /// <returns><see langword="true" /> if the image type is a spec-defined value; otherwise, <see langword="false" />.</returns>
        public static bool IsKnown(this TgaImageType imageType)
            => imageType is TgaImageType.NoImageData or TgaImageType.Uncompressed_ColorMapped or TgaImageType.Uncompressed_TrueColor
                or TgaImageType.Uncompressed_BlackWhite or TgaImageType.RLE_ColorMapped or TgaImageType.RLE_TrueColor or TgaImageType.RLE_BlackWhite;

        /// <summary>
        /// Determines whether the image type falls in the range reserved by Truevision for general
        /// applications (spec values 4-8 or 12-127).
        /// </summary>
        /// <param name="imageType">The image type to test.</param>
        /// <returns><see langword="true" /> if the image type is Truevision-reserved; otherwise, <see langword="false" />.</returns>
        public static bool IsTruevisionReserved(this TgaImageType imageType)
        {
            byte value = (byte)imageType;
            return value is (>= 4 and <= 8) or (>= 12 and <= 127);
        }

        /// <summary>
        /// Determines whether the image type falls in the range available for developer applications
        /// (spec values 128-255).
        /// </summary>
        /// <param name="imageType">The image type to test.</param>
        /// <returns><see langword="true" /> if the image type is developer-defined; otherwise, <see langword="false" />.</returns>
        public static bool IsDeveloperDefined(this TgaImageType imageType)
            => (byte)imageType >= 128;
    }
}
