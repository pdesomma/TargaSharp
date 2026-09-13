namespace TargaSharp
{
    /// <summary>
    /// Extension methods for classifying <see cref="TgaColorMapType"/> values against the
    /// Truevision TGA File Format specification's color map type ranges.
    /// </summary>
    public static class TgaColorMapTypeExtensions
    {
        /// <summary>
        /// Determines whether the color map type is one of the two values currently defined by the
        /// Truevision TGA File Format specification (<see cref="TgaColorMapType.NoColorMap"/> or <see cref="TgaColorMapType.ColorMap"/>).
        /// </summary>
        /// <param name="colorMapType">The color map type to test.</param>
        /// <returns><see langword="true" /> if the color map type is a spec-defined value; otherwise, <see langword="false" />.</returns>
        public static bool IsKnown(this TgaColorMapType colorMapType)
            => colorMapType is TgaColorMapType.NoColorMap or TgaColorMapType.ColorMap;

        /// <summary>
        /// Determines whether the color map type falls in the range reserved by Truevision
        /// (spec values 2-127).
        /// </summary>
        /// <param name="colorMapType">The color map type to test.</param>
        /// <returns><see langword="true" /> if the color map type is Truevision-reserved; otherwise, <see langword="false" />.</returns>
        public static bool IsTruevisionReserved(this TgaColorMapType colorMapType)
        {
            byte value = (byte)colorMapType;
            return value is >= 2 and <= 127;
        }

        /// <summary>
        /// Determines whether the color map type falls in the range available for developer applications
        /// (spec values 128-255).
        /// </summary>
        /// <param name="colorMapType">The color map type to test.</param>
        /// <returns><see langword="true" /> if the color map type is developer-defined; otherwise, <see langword="false" />.</returns>
        public static bool IsDeveloperDefined(this TgaColorMapType colorMapType)
            => (byte)colorMapType >= 128;
    }
}
