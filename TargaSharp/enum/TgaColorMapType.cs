namespace TargaSharp
{
    /// <summary>
    /// <para>Color Map Type codes 2 to 127 are reserved for use by Truevision, while codes 128 to 255
    /// may be used for developer applications.</para>
    /// True-Color images do not normally make use of the color map field, but some current
    /// applications store palette information or developer-defined information in this field.
    /// It is best to check Field 3, Image Type, to make sure you have a file which can use the
    /// data stored in the Color Map Field.
    /// Otherwise ignore the information. When saving or creating files for True-Color
    /// images do not use this field and set it to Zero to ensure compatibility. Please refer
    /// to the Developer Area specification for methods of storing developer defined information.
    /// </summary>
    public enum TgaColorMapType : byte
    {
        /// <summary>
        /// The file does not contain a color map.
        /// </summary>
        NoColorMap = 0,

        /// <summary>
        /// The file contains a color map.
        /// </summary>
        ColorMap = 1
    }
}
