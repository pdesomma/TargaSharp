namespace TargaSharp
{
    /// <summary>
    /// TGA file field.
    /// </summary>
    public interface IFileField : IByteSized
    {
        /// <summary>
        /// The field number.
        /// </summary>
        uint Number { get; }

        /// <summary>
        /// The field offset position.
        /// </summary>
        uint Offset { get; set; }
    }
}
