namespace TargaSharp
{
    /// <summary>
    /// Extension helpers for the bit-depth enums used to size pixel and color-map entry data.
    /// </summary>
    internal static class TgaPixelDepthExtensions
    {
        /// <summary>
        /// Gets the number of bytes required to store one pixel of the given <see cref="TgaPixelDepth"/>.
        /// </summary>
        /// <param name="depth">Pixel depth, in bits per pixel.</param>
        /// <returns>Number of bytes per pixel, rounded up to the nearest whole byte.</returns>
        internal static int BytesPerPixel(this TgaPixelDepth depth) => (int)Math.Ceiling((double)depth / 8.0);

        /// <summary>
        /// Gets the number of bytes required to store one color-map entry of the given <see cref="TgaColorMapEntrySize"/>.
        /// </summary>
        /// <param name="entrySize">Color-map entry size, in bits per entry.</param>
        /// <returns>Number of bytes per entry, rounded up to the nearest whole byte.</returns>
        internal static int BytesPerPixel(this TgaColorMapEntrySize entrySize) => (int)Math.Ceiling((double)entrySize / 8.0);
    }
}
