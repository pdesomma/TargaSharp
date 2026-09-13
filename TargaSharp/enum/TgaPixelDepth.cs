namespace TargaSharp
{

    /// <summary>
    /// Number of bits per pixel. This number includes the Attribute or Alpha channel bits.
    /// Common values are 8, 16, 24 and 32 but other pixel depths could be used.
    /// </summary>
    public enum TgaPixelDepth : byte
    {
        /// <summary>
        /// A pixel depth that does not match one of the standard values below.
        /// </summary>
        Other = 0,

        /// <summary>
        /// 8 bits per pixel.
        /// </summary>
        Bpp8 = 8,

        /// <summary>
        /// 16 bits per pixel.
        /// </summary>
        Bpp16 = 16,

        /// <summary>
        /// 24 bits per pixel.
        /// </summary>
        Bpp24 = 24,

        /// <summary>
        /// 32 bits per pixel.
        /// </summary>
        Bpp32 = 32
    }
}
