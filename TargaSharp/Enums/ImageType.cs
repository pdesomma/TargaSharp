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
    /// Image Data Type codes 0 to 127 are reserved for use by Truevision for general applications.
    /// Image Data Type codes 128 to 255 may be used for developer applications.
    /// </summary>
    public enum ImageType : byte
    {
        NoImageData = 0,
        Uncompressed_ColorMapped = 1,
        Uncompressed_TrueColor = 2,
        Uncompressed_BlackWhite = 3,
        RLE_ColorMapped = 9,
        RLE_TrueColor = 10,
        RLE_BlackWhite = 11
    }
}
