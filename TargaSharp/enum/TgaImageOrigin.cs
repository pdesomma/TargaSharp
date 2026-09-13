namespace TargaSharp
{
    /// <summary>
    /// Used to indicate the order in which pixel data is transferred from the file to the screen.
    /// (Bit 4 (bit 0 in enum) is for left-to-right ordering and bit 5 (bit 1 in enum) is for
    /// topto-bottom ordering as shown below.)
    /// </summary>
    public enum TgaImageOrigin : byte
    {
        /// <summary>
        /// Pixel data is transferred starting at the bottom-left corner of the image.
        /// </summary>
        BottomLeft = 0,

        /// <summary>
        /// Pixel data is transferred starting at the bottom-right corner of the image.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Pixel data is transferred starting at the top-left corner of the image.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Pixel data is transferred starting at the top-right corner of the image.
        /// </summary>
        TopRight
    }
}
