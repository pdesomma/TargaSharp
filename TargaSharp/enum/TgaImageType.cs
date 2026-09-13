namespace TargaSharp
{
    /// <summary>
    /// Truevision has currently defined seven image types:
    /// <para>0 - No Image Data Included;</para>
    /// <para>1 - Uncompressed, Color-mapped Image;</para>
    /// <para>2 - Uncompressed, True-color Image;</para>
    /// <para>3 - Uncompressed, Black-and-white Image;</para>
    /// <para>9 - Run-length encoded, Color-mapped Image;</para>
    /// <para>10 - Run-length encoded, True-color Image;</para>
    /// <para>11 - Run-length encoded, Black-and-white Image.</para>
    /// Image Data Type codes 4-8 and 12-127 are reserved for use by Truevision for general applications.
    /// Image Data Type codes 128 to 255 may be used for developer applications.
    /// </summary>
    public enum TgaImageType : byte
    {
        /// <summary>
        /// The file contains no image data.
        /// </summary>
        NoImageData = 0,

        /// <summary>
        /// An uncompressed, color-mapped image.
        /// </summary>
        UncompressedColorMapped = 1,

        /// <summary>
        /// An uncompressed, true-color image.
        /// </summary>
        UncompressedTrueColor,

        /// <summary>
        /// An uncompressed, black-and-white (grayscale) image.
        /// </summary>
        UncompressedGrayscale,

        /// <summary>
        /// A run-length encoded, color-mapped image.
        /// </summary>
        RleColorMapped = 9,

        /// <summary>
        /// A run-length encoded, true-color image.
        /// </summary>
        RleTrueColor,

        /// <summary>
        /// A run-length encoded, black-and-white (grayscale) image.
        /// </summary>
        RleGrayscale
    }
}
