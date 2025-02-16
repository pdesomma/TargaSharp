using System.IO;

namespace TargaSharp.Extensions
{
    /// <summary>
    /// Helpful extension functions
    /// </summary>
    public static class TgaExtensions
    {
        /// <summary>
        /// Make <see cref="Targa"/> from <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Some stream. You can use a lot of Stream types, but Stream must support:
        /// <see cref="Stream.CanSeek"/> and <see cref="Stream.CanRead"/>.</param>
        public static Targa ToTarga(this Stream stream) => new(stream);

        /// <summary>
        /// Make <see cref="Targa"/> from bytes array.
        /// </summary>
        /// <param name="bytes">Bytes array (same like TGA File).</param>
        public static Targa ToTarga(this byte[] bytes) => new(bytes);
    }
}
