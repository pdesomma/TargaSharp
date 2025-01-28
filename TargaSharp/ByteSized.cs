namespace TargaSharp
{
    /// <summary>
    /// Object that has a size in bytes.
    /// </summary>
    public abstract class ByteSized : IByteSized
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ByteSized" class./>
        /// </summary>
        /// <param name="size"></param>
        public ByteSized(uint size)
        {
            ByteSize = size;
        }

        /// <summary>
        /// The size of the field in bytes.
        /// </summary>
        public uint ByteSize { get; private set; } = 0;
    }
}
