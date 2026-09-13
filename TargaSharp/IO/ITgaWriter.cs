namespace TargaSharp.IO
{
    /// <summary>
    /// Writes <see cref="TgaFile"/> instances as TGA-formatted data.
    /// </summary>
    public interface ITgaWriter
    {
        /// <summary>
        /// Writes <paramref name="file"/> to a stream.
        /// </summary>
        /// <param name="file">The <see cref="TgaFile"/> to write.</param>
        /// <param name="stream">Destination stream. Must support <see cref="Stream.CanWrite"/>.</param>
        void Write(TgaFile file, Stream stream);

        /// <summary>
        /// Writes <paramref name="file"/> to an in-memory byte array.
        /// </summary>
        /// <param name="file">The <see cref="TgaFile"/> to write.</param>
        /// <returns>Bytes array containing the encoded TGA file.</returns>
        byte[] Write(TgaFile file);

        /// <summary>
        /// Writes <paramref name="file"/> to disk.
        /// </summary>
        /// <param name="file">The <see cref="TgaFile"/> to write.</param>
        /// <param name="path">Full path to the destination file.</param>
        void Write(TgaFile file, string path);
    }
}
