namespace TargaNet
{
    
    /// <summary>
    /// Number of bits per pixel. This number includes the Attribute or Alpha channel bits.
    /// Common values are 8, 16, 24 and 32 but other pixel depths could be used.
    /// </summary>
    public enum PixelDepth : byte
    {
        Other = 0,
        Bpp8 = 8,
        Bpp16 = 16,
        Bpp24 = 24,
        Bpp32 = 32
    }
}
