namespace TargaSharp
{
    /// <summary>
    /// Contains a value which specifies the type of Alpha channel
    /// data contained in the file. Value Meaning:
    /// <para>0: no Alpha data included (bits 3-0 of field 5.6 should also be set to zero)</para>
    /// <para>1: undefined data in the Alpha field, can be ignored</para>
    /// <para>2: undefined data in the Alpha field, but should be retained</para>
    /// <para>3: useful Alpha channel data is present</para>
    /// <para>4: pre-multiplied Alpha(see description below)</para>
    /// <para>5 -127: RESERVED</para>
    /// <para>128-255: Un-assigned</para>
    /// <para>Pre-multiplied Alpha Example: Suppose the Alpha channel data is being used to specify the
    /// opacity of each pixel(for use when the image is overlaid on another image), where 0 indicates
    /// that the pixel is completely transparent and a value of 1 indicates that the pixel is
    /// completely opaque(assume all component values have been normalized).</para>
    /// <para>A quadruple(a, r, g, b) of( 0.5, 1, 0, 0) would indicate that the pixel is pure red with a
    /// transparency of one-half. For numerous reasons(including image compositing) is is better to
    /// pre-multiply the individual color components with the value in the Alpha channel.</para>
    /// A pre-multiplication of the above would produce a quadruple(0.5, 0.5, 0, 0).
    /// A value of 3 in the Attributes Type Field(field 23) would indicate that the color components
    /// of the pixel have already been scaled by the value in the Alpha channel.
    /// </summary>
    public enum AttributeType : byte
    {
        NoAlpha = 0,
        UndefinedAlphaCanBeIgnored,
        UndefinedAlphaButShouldBeRetained,
        UsefulAlpha,
        PreMultipliedAlpha
    }
}
