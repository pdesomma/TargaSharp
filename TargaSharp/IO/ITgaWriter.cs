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
        /// <param name="stream">Destination stream, written from its current position. File offsets embedded in
        /// the footer and extension area are relative to that position, so it should be the start of the stream
        /// (offset 0) for the result to be a standalone TGA file. Must support <see cref="Stream.CanWrite"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="file"/> or <paramref name="stream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
        /// <exception cref="Validation.TgaValidationException"><paramref name="file"/> fails validation or layout computation.</exception>
        void Write(TgaFile file, Stream stream);

        /// <summary>
        /// Writes <paramref name="file"/> to an in-memory byte array.
        /// </summary>
        /// <param name="file">The <see cref="TgaFile"/> to write.</param>
        /// <returns>Bytes array containing the encoded TGA file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
        /// <exception cref="Validation.TgaValidationException"><paramref name="file"/> fails validation or layout computation.</exception>
        byte[] Write(TgaFile file);

        /// <summary>
        /// Writes <paramref name="file"/> to disk.
        /// </summary>
        /// <param name="file">The <see cref="TgaFile"/> to write.</param>
        /// <param name="path">Full path to the destination file; the file is only created once the whole
        /// image has been encoded, so a validation failure leaves nothing behind.</param>
        /// <exception cref="ArgumentNullException"><paramref name="file"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="Validation.TgaValidationException"><paramref name="file"/> fails validation or layout computation.</exception>
        void Write(TgaFile file, string path);
    }
}
