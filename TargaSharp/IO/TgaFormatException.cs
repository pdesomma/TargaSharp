namespace TargaSharp.IO
{
    /// <summary>
    /// Thrown by <see cref="ITgaReader"/> when the input is not a well-formed TGA file (truncated, or sizes that don't add up).
    /// </summary>
    public sealed class TgaFormatException : TgaException
    {
        /// <summary>
        /// Make a <see cref="TgaFormatException"/> with a message.
        /// </summary>
        /// <param name="message">Description of what was malformed.</param>
        public TgaFormatException(string message) : base(message) { }

        /// <summary>
        /// Make a <see cref="TgaFormatException"/> with a message and the underlying cause.
        /// </summary>
        /// <param name="message">Description of what was malformed.</param>
        /// <param name="innerException">The exception that caused this one.</param>
        public TgaFormatException(string message, Exception? innerException) : base(message, innerException) { }
    }
}
