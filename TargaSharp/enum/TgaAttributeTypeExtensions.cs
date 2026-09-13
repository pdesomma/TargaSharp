namespace TargaSharp
{
    /// <summary>
    /// Extension methods for classifying <see cref="TgaAttributeType"/> values against the
    /// Truevision TGA File Format specification's attribute type ranges.
    /// </summary>
    public static class TgaAttributeTypeExtensions
    {
        /// <summary>
        /// Determines whether the attribute type is one of the five values currently defined by the
        /// Truevision TGA File Format specification (spec values 0-4).
        /// </summary>
        /// <param name="attributeType">The attribute type to test.</param>
        /// <returns><see langword="true" /> if the attribute type is a spec-defined value; otherwise, <see langword="false" />.</returns>
        public static bool IsKnown(this TgaAttributeType attributeType)
            => (byte)attributeType <= 4;

        /// <summary>
        /// Determines whether the attribute type falls in the range reserved by the specification
        /// (spec values 5-127).
        /// </summary>
        /// <param name="attributeType">The attribute type to test.</param>
        /// <returns><see langword="true" /> if the attribute type is reserved; otherwise, <see langword="false" />.</returns>
        public static bool IsReserved(this TgaAttributeType attributeType)
        {
            byte value = (byte)attributeType;
            return value is >= 5 and <= 127;
        }

        /// <summary>
        /// Determines whether the attribute type falls in the un-assigned range
        /// (spec values 128-255).
        /// </summary>
        /// <param name="attributeType">The attribute type to test.</param>
        /// <returns><see langword="true" /> if the attribute type is un-assigned; otherwise, <see langword="false" />.</returns>
        public static bool IsUnassigned(this TgaAttributeType attributeType)
            => (byte)attributeType >= 128;
    }
}
