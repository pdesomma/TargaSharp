namespace TargaSharp.IO
{
    /// <summary>
    /// Reads <see cref="TgaFile"/> instances from TGA-formatted data.
    /// </summary>
    public interface ITgaReader
    {
        /// <summary>
        /// Reads a <see cref="TgaFile"/> from a stream.
        /// </summary>
        /// <param name="stream">Stream whose entire content is one TGA file; it is read from offset 0
        /// regardless of its current position (file offsets in the footer are absolute) and left open.
        /// Must support <see cref="Stream.CanRead"/> and <see cref="Stream.CanSeek"/>.</param>
        /// <returns>The parsed <see cref="TgaFile"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> is not readable or not seekable.</exception>
        /// <exception cref="TgaFormatException">The data is not a well-formed TGA file (truncated, sizes that
        /// don't add up, or offsets pointing outside the file).</exception>
        TgaFile Read(Stream stream);

        /// <summary>
        /// Reads a <see cref="TgaFile"/> from an in-memory byte array.
        /// </summary>
        /// <param name="bytes">Bytes array containing a full TGA file.</param>
        /// <returns>The parsed <see cref="TgaFile"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <see langword="null"/>.</exception>
        /// <exception cref="TgaFormatException">The data is not a well-formed TGA file.</exception>
        TgaFile Read(byte[] bytes);

        /// <summary>
        /// Reads a <see cref="TgaFile"/> from disk.
        /// </summary>
        /// <param name="path">Full path to the TGA file.</param>
        /// <returns>The parsed <see cref="TgaFile"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="FileNotFoundException">No file exists at <paramref name="path"/>.</exception>
        /// <exception cref="TgaFormatException">The file is not a well-formed TGA file.</exception>
        TgaFile Read(string path);
    }
}
