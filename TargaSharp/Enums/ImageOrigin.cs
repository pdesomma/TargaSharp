namespace TargaSharp
{
    /// <summary>
    /// Used to indicate the order in which pixel data is transferred from the file to the screen.
    /// (Bit 4 (bit 0 in enum) is for left-to-right ordering and bit 5 (bit 1 in enum) is for
    /// top-to-bottom ordering as shown below.)
    /// </summary>
    public enum ImageOrigin : byte
    {
        BottomLeft = 0,
        BottomRight,
        TopLeft,
        TopRight
    }
}
