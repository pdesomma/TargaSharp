namespace TargaSharp
{
    /// <summary>
    /// Base type for every TargaSharp-specific failure, so callers can catch TGA problems separately from I/O or argument errors.
    /// </summary>
    public class TgaException : Exception
    {
        /// <summary>
        /// Make a <see cref="TgaException"/> with a message.
        /// </summary>
        /// <param name="message">Description of the failure.</param>
        public TgaException(string message) : base(message) { }

        /// <summary>
        /// Make a <see cref="TgaException"/> with a message and the underlying cause.
        /// </summary>
        /// <param name="message">Description of the failure.</param>
        /// <param name="innerException">The exception that caused this one.</param>
        public TgaException(string message, Exception? innerException) : base(message, innerException) { }
    }
}
