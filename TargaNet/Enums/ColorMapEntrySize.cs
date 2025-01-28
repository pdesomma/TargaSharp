namespace TargaNet
{   
    /// <summary>
    /// Establishes the number of bits per entry. Typically 15, 16, 24 or 32-bit values are used.
    /// <para>When working with VDA or VDA/D cards it is preferred that you select 16 bits(5 bits
    /// per primary with 1 bit to select interrupt control) and set the 16th bit to 0 so that the
    /// interrupt bit is disabled. Even if this field is set to 15 bits(5 bits per primary) you
    /// must still parse the color map data 16 bits at a time and ignore the 16th bit.</para>
    /// <para>When working with a TARGA M8 card you would select 24 bits (8 bits per primary)
    /// since the color map is defined as 256 entries of 24 bit color values.</para>
    /// When working with a TrueVista card(ATVista or NuVista) you would select 24-bit(8 bits per
    /// primary) or 32-bit(8 bits per primary including Alpha channel) depending on your
    /// application’s use of look-up tables. It is suggested that when working with 16-bit and
    /// 32-bit color images, you store them as True-Color images and do not use the color map 
    /// field to store look-up tables. Please refer to the TGA Extensions for fields better suited
    /// to storing look-up table information.
    /// </summary>
    public enum ColorMapEntrySize : byte
    {
        Other = 0,
        X1R5G5B5 = 15,
        A1R5G5B5 = 16,
        R8G8B8 = 24,
        A8R8G8B8 = 32
    }
}
