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
        /// <param name="stream">Stream positioned to read a full TGA file. Must support
        /// <see cref="Stream.CanRead"/> and <see cref="Stream.CanSeek"/>.</param>
        /// <returns>The parsed <see cref="TgaFile"/>.</returns>
        TgaFile Read(Stream stream);

        /// <summary>
        /// Reads a <see cref="TgaFile"/> from an in-memory byte array.
        /// </summary>
        /// <param name="bytes">Bytes array containing a full TGA file.</param>
        /// <returns>The parsed <see cref="TgaFile"/>.</returns>
        TgaFile Read(byte[] bytes);

        /// <summary>
        /// Reads a <see cref="TgaFile"/> from disk.
        /// </summary>
        /// <param name="path">Full path to the TGA file.</param>
        /// <returns>The parsed <see cref="TgaFile"/>.</returns>
        TgaFile Read(string path);
    }
}
