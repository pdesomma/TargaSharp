namespace TargaSharp
{
    /// <summary>
    /// Object that has a size in bytes.
    /// </summary>
    public interface IByteSized
    { 
        /// <summary>
        /// The size of this thing in bytes.
        /// </summary>
        uint ByteSize { get; }
    }
}
