namespace TargaSharp.IO
{
    /// <summary>
    /// <see cref="BinaryReader"/> helpers shared by <see cref="TgaReader"/> and <see cref="RleCodec"/>.
    /// </summary>
    internal static class BinaryReaderExtensions
    {
        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes or throws. The count comes from a field in the
        /// file itself, so on a seekable stream it is checked against what remains <em>before</em> anything
        /// is allocated (a hostile file must not force a multi-GB allocation); otherwise the short read is
        /// detected afterwards. Either way a truncated field fails here instead of coming back silently shortened.
        /// </summary>
        /// <param name="reader">Reader positioned at the start of the field.</param>
        /// <param name="count">Declared byte length of the field.</param>
        /// <param name="what">Field name for the error message.</param>
        /// <returns>Exactly <paramref name="count"/> bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        /// <exception cref="EndOfStreamException">Fewer than <paramref name="count"/> bytes remain, or the count exceeds a single array.</exception>
        internal static byte[] ReadExactly(this BinaryReader reader, long count, string what)
        {
            ArgumentNullException.ThrowIfNull(reader);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Must be >= 0.");

            if (reader.BaseStream.CanSeek)
            {
                long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
                if (count > remaining)
                    throw new EndOfStreamException($"{what} declares {count} bytes but only {Math.Max(remaining, 0)} remain in the stream.");
            }
            else if (count > int.MaxValue)
                throw new EndOfStreamException($"{what} declares {count} bytes, more than a single read can supply.");

            byte[] bytes = reader.ReadBytes((int)count);
            if (bytes.Length != count)
                throw new EndOfStreamException($"{what} truncated: expected {count} bytes, got {bytes.Length}.");
            return bytes;
        }
    }
}
