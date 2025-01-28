namespace TargaSharp
{
    /// <summary>
    /// Object that can be converted to a byte array
    /// </summary>
    public interface IByteConvert
    {
        /// <summary>
        /// Convert to byte array.
        /// </summary>
        /// <returns>Byte array with length = 12.</returns>
        byte[]? ToBytes();
    }
}
